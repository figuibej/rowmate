using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RowMate.Tests.Support
{
    // SynchronizationContext de un solo hilo para tests de `dotnet` (QA-P-26, ARCHITECTURE §6): hace el papel del
    // UnitySynchronizationContext, así la continuación de un await vuelve siempre al hilo del test.
    // Uso: TestMainThreadContext.Run(async () => { … }); o instalarlo y bombear con RunUntilIdle().
    public sealed class TestMainThreadContext : SynchronizationContext
    {
        // Tiempo máximo de espera de un trabajo posteado desde otro hilo antes de declarar un bloqueo.
        private static readonly TimeSpan IdleTimeout = TimeSpan.FromSeconds(10);

        private readonly object _gate = new object();
        private readonly Queue<KeyValuePair<SendOrPostCallback, object?>> _queue = new Queue<KeyValuePair<SendOrPostCallback, object?>>();
        private readonly int _threadId = Environment.CurrentManagedThreadId;

        public int PendingCount
        {
            get
            {
                lock (_gate)
                {
                    return _queue.Count;
                }
            }
        }

        // Instala el contexto, corre body y bombea la cola hasta que la tarea termina. Propaga su excepción.
        // maxSteps acota los callbacks ejecutados (un bucle de continuaciones sin fin no cuelga el test).
        public static void Run(Func<Task> body, int maxSteps = 100000)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var previous = Current;
            var context = new TestMainThreadContext();
            SetSynchronizationContext(context);
            try
            {
                var task = body();
                if (task == null)
                {
                    throw new InvalidOperationException("TestMainThreadContext.Run: el cuerpo devolvió una tarea nula.");
                }

                var steps = 0;
                while (!task.IsCompleted)
                {
                    if (!context.TryRunOne(IdleTimeout))
                    {
                        throw new TimeoutException("TestMainThreadContext.Run: la tarea no termina y no hay trabajo pendiente "
                            + "(¿falta un FakeScheduler.Advance?).");
                    }

                    if (++steps > maxSteps)
                    {
                        throw new InvalidOperationException("TestMainThreadContext.Run: más de " + maxSteps + " pasos.");
                    }
                }

                context.RunUntilIdle(maxSteps);
                task.GetAwaiter().GetResult();
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }

        // Ejecuta lo posteado (incluido lo que se postea mientras tanto) hasta vaciar la cola.
        public void RunUntilIdle()
        {
            RunUntilIdle(100000);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            if (d == null)
            {
                throw new ArgumentNullException(nameof(d));
            }

            lock (_gate)
            {
                _queue.Enqueue(new KeyValuePair<SendOrPostCallback, object?>(d, state));
                Monitor.PulseAll(_gate);
            }
        }

        // Un solo hilo: Send ejecuta en el acto.
        public override void Send(SendOrPostCallback d, object? state)
        {
            if (d == null)
            {
                throw new ArgumentNullException(nameof(d));
            }

            d(state);
        }

        public override SynchronizationContext CreateCopy()
        {
            return this;
        }

        private void RunUntilIdle(int maxSteps)
        {
            var steps = 0;
            while (TryRunOne(TimeSpan.Zero))
            {
                if (++steps > maxSteps)
                {
                    throw new InvalidOperationException("TestMainThreadContext.RunUntilIdle: más de " + maxSteps + " pasos.");
                }
            }
        }

        private bool TryRunOne(TimeSpan wait)
        {
            KeyValuePair<SendOrPostCallback, object?> item;
            lock (_gate)
            {
                if (_queue.Count == 0 && wait > TimeSpan.Zero)
                {
                    Monitor.Wait(_gate, wait);
                }

                if (_queue.Count == 0)
                {
                    return false;
                }

                item = _queue.Dequeue();
            }

            var previous = Current;
            SetSynchronizationContext(this);
            try
            {
                item.Key(item.Value);
            }
            finally
            {
                SetSynchronizationContext(previous);
            }

            return true;
        }

        // Hilo en el que se creó el contexto (el del test).
        public int ThreadId => _threadId;
    }
}
