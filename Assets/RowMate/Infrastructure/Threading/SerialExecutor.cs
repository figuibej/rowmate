using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Infrastructure.Threading
{
    // El hilo de la base en la app (02-tech-design §2.6; ADR 005): un hilo propio (IsBackground) con una cola FIFO. El orden
    // de llegada es el orden de ejecución, así "cargar" y después "crear" nunca se cruzan. Cada trabajo completa una tarea con
    // RunContinuationsAsynchronously: ninguna continuación corre en el hilo de la base (vuelve al hilo principal por el
    // SynchronizationContext de quien hizo await). La pantalla sigue animando mientras la base trabaja (AC-HIS-17-06).
    // - Dispose: no acepta más trabajos, termina lo encolado (un guardado en vuelo se completa) y espera el hilo con un
    //   tope de 5 s (si no termina, "[DB] …" en el registro). Es idempotente y se desuscribe de la descarga del dominio.
    // - También se dispone en AppDomain.DomainUnload: una recarga de dominio del Editor no deja el hilo vivo (QA-G3-09).
    // - Run después de Dispose devuelve una tarea fallada con ObjectDisposedException (no lanza en línea).
    public sealed class SerialExecutor : IDbExecutor
    {
        private const string Tag = "DB";
        private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(5);

        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
        private readonly object _gate = new object();
        private readonly Thread _thread;
        private readonly IAppLogger _logger;
        private readonly IUnloadHook _unloadHook;
        private readonly EventHandler _onUnload;
        private bool _disposed;

        public SerialExecutor(string threadName, IAppLogger logger)
            : this(threadName, logger, AppDomainUnloadHook.Instance)
        {
        }

        // Con un gancho de descarga propio (SerialExecutorTests cuenta las suscripciones).
        internal SerialExecutor(string threadName, IAppLogger logger, IUnloadHook unloadHook)
        {
            if (threadName == null)
            {
                throw new ArgumentNullException(nameof(threadName));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unloadHook = unloadHook ?? throw new ArgumentNullException(nameof(unloadHook));
            _thread = new Thread(Loop) { IsBackground = true, Name = threadName };
            ThreadId = _thread.ManagedThreadId;
            _onUnload = OnUnload;
            _unloadHook.Subscribe(_onUnload);
            _thread.Start();
        }

        // El hilo de la base (para las pruebas de hilos).
        public int ThreadId { get; }

        public Task<T> Run<T>(Func<T> work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            var source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!TryEnqueue(() => Execute(work, source)))
            {
                return Task.FromException<T>(new ObjectDisposedException(nameof(SerialExecutor), "La base ya se cerró."));
            }

            return source.Task;
        }

        public Task Run(Action work)
        {
            if (work == null)
            {
                throw new ArgumentNullException(nameof(work));
            }

            return Run(() =>
            {
                work();
                return true;
            });
        }

        // It. 4a (AC-WKT-06-15): un trabajo marcador al final de la cola; como la cola es FIFO, cuando corre ya terminó todo lo
        // anterior. Sin trabajos pendientes (o cerrada, o desde el propio hilo de la base) vuelve en el acto.
        public bool WaitIdle(TimeSpan timeout)
        {
            if (Environment.CurrentManagedThreadId == ThreadId)
            {
                return true;
            }

            // Sin Dispose: si vence el tope, el marcador igual corre después (y no debe encontrar el evento dispuesto).
            var done = new ManualResetEventSlim(false);
            if (!TryEnqueue(done.Set))
            {
                return !_thread.IsAlive || _thread.Join(timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
            }

            return done.Wait(timeout < TimeSpan.Zero ? TimeSpan.Zero : timeout);
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _queue.CompleteAdding();
            }

            _unloadHook.Unsubscribe(_onUnload);
            if (Environment.CurrentManagedThreadId == ThreadId)
            {
                return; // desde un trabajo de la propia base: el hilo termina solo después de este trabajo
            }

            if (_thread.Join(JoinTimeout))
            {
                _queue.Dispose();
            }
            else
            {
                _logger.Log(LogLevel.Warning, Tag, "el hilo de la base no terminó en 5 s");
            }
        }

        private static void Execute<T>(Func<T> work, TaskCompletionSource<T> source)
        {
            T result;
            try
            {
                result = work();
            }
            catch (Exception error)
            {
                source.TrySetException(error);
                return;
            }

            source.TrySetResult(result);
        }

        private bool TryEnqueue(Action item)
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return false;
                }

                try
                {
                    _queue.Add(item);
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        private void Loop()
        {
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                try
                {
                    item();
                }
                catch (Exception error)
                {
                    // Execute ya captura todo; esto solo protege el hilo de un error del propio ejecutor.
                    _logger.Log(LogLevel.Error, Tag, "error en el hilo de la base: " + error.Message, error);
                }
            }
        }

        private void OnUnload(object? sender, EventArgs args)
        {
            Dispose();
        }
    }

    // Suscripción a la descarga del dominio (Subscribe/Unsubscribe de un EventHandler). El de producción usa
    // AppDomain.CurrentDomain.DomainUnload; SerialExecutorTests pasa uno que cuenta.
    internal interface IUnloadHook
    {
        void Subscribe(EventHandler handler);

        void Unsubscribe(EventHandler handler);
    }

    internal sealed class AppDomainUnloadHook : IUnloadHook
    {
        public static readonly AppDomainUnloadHook Instance = new AppDomainUnloadHook();

        private AppDomainUnloadHook()
        {
        }

        public void Subscribe(EventHandler handler)
        {
            AppDomain.CurrentDomain.DomainUnload += handler;
        }

        public void Unsubscribe(EventHandler handler)
        {
            AppDomain.CurrentDomain.DomainUnload -= handler;
        }
    }
}
