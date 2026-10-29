using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Text;

namespace RowMate.Core.Workout
{
    // Lo que el motor le pide a la base durante UNA sesión, en orden y sin frenar el HUD (Q-12, Q-13; 02-tech-design §3.2 de la
    // It. 4a). Hilo principal. Uno por sesión: lo crea el motor al iniciar.
    // - Alta en el acto (BeginAsync). Los puntos que llegan con el alta en vuelo esperan en el lote pendiente.
    // - Un solo lote de puntos en vuelo: los que llegan mientras tanto se juntan y salen en el lote siguiente, en una
    //   transacción (DT4a-08). Con la base al día, cada lote es un punto y sale en el mismo cuadro (AC-WKT-06-05: "a más
    //   tardar 1 s").
    // - Cierre: espera el alta y los lotes en vuelo; los puntos de los lotes que fallaron (una vez más), los pendientes, el
    //   punto final y el cierre van juntos, en UNA transacción, DESPUÉS del último lote (lo garantiza el orden del puerto). Si
    //   esa transacción falla, la sesión queda cortada: una sesión cerrada siempre tiene todos sus puntos (AC-WKT-06-12;
    //   QA-G4aT-13).
    // - Una falla no corta nada: se registra (Warning, tag WORKOUT), HadFailure queda en true y Failed avisa una sola vez por
    //   sesión (del alta o de un lote; la del cierre solo se ve en el aviso del final). Lo que ya se escribió queda escrito y
    //   los puntos del lote que falló se guardan para el reintento del cierre. Si el alta falla, los puntos de esa sesión se
    //   descartan (no hay sesión).
    // - Con el alta en vuelo y el hilo principal que no puede esperar su vuelta (FlushAndWait, Abandon), el lote pendiente va
    //   con AddPointsAfterCreateAsync: detrás del alta, en el hilo de la base (AC-WKT-06-15; QA-G4aT-06).
    // - FlushAndWait y Abandon mandan TODO lo pendiente, también los puntos de los lotes que fallaron (QA-V4a-ROB-03): si el
    //   sistema cierra la app, quedan escritos aunque la sesión quede cortada.
    // - Terminar ya confirmado con el cierre todavía sin encolar (espera, en el hilo principal, la vuelta del alta o de un lote:
    //   en segundo plano o al cerrar la app esa continuación puede no correr nunca): FlushAndWait y Abandon esperan la base con
    //   tope y, si lo que estaba en vuelo ya volvió, lo resuelven ahí y encolan el cierre en el acto (QA-V4a-ROB-04). El total
    //   de FlushAndWait sigue acotado por su tope (AC-WKT-06-15). La continuación del cierre, cuando corra, espera ese mismo.
    // - Dispose: lo que vuelve después no cambia nada.
    public sealed class SessionRecorder : IDisposable
    {
        public const string Tag = "WORKOUT";

        private readonly ISessionRepository _repository;
        private readonly IAppLogger _logger;
        private readonly List<DataPoint> _pending = new List<DataPoint>();
        private readonly List<DataPoint> _retry = new List<DataPoint>();
        private readonly List<Batch> _inFlight = new List<Batch>();
        private Task<long>? _creation;
        private DataPoint? _finalPoint;
        private WorkoutSession? _closedSession;
        // El cierre que encolaron FlushAndWait o Abandon (ROB-04), ya observado: la continuación de FinishAsync espera este.
        private Task<bool>? _queuedClose;
        private bool _finishing;
        private bool _closeQueued;
        private bool _abandoned;
        private bool _failureReported;
        private bool _disposed;

        public SessionRecorder(ISessionRepository repository, IAppLogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event Action? Failed;

        // El id de la sesión, cuando volvió el alta.
        public long? SessionId { get; private set; }

        public bool IsCreating { get; private set; }

        // El alta falló: no hay sesión (ni puntos ni cierre).
        public bool CreateFailed { get; private set; }

        public bool HadFailure { get; private set; }

        // Los puntos de lotes que fallaron, para el reintento del cierre.
        public int PointsToRetry => _retry.Count;

        // Los puntos tomados que todavía no salieron hacia la base.
        public int PendingPoints => _pending.Count;

        // Lotes enviados que todavía no volvieron.
        public int BatchesInFlight => _inFlight.Count;

        // true = creada. Una sola vez por grabador.
        public Task<bool> BeginAsync(WorkoutSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (_creation != null)
            {
                throw new InvalidOperationException("SessionRecorder: una sesión por grabador.");
            }

            IsCreating = true;
            Task<long> creation;
            try
            {
                creation = _repository.CreateAsync(session);
            }
            catch (Exception error)
            {
                creation = Task.FromException<long>(error);
            }

            _creation = creation;
            return ObserveCreation(creation);
        }

        // Un punto tomado (el SessionId lo pone el grabador o, con el alta en vuelo, el repositorio detrás del alta).
        public void Add(DataPoint point)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            if (_disposed || CreateFailed || _closeQueued)
            {
                return;
            }

            _pending.Add(point);
            if (SessionId.HasValue && _inFlight.Count == 0 && !_finishing)
            {
                SendPending();
            }
        }

        // true = la sesión quedó cerrada con todos sus puntos. Sin alta (o si falló), false sin escribir nada (:256).
        public async Task<bool> FinishAsync(DataPoint finalPoint, WorkoutSession closed)
        {
            if (finalPoint == null)
            {
                throw new ArgumentNullException(nameof(finalPoint));
            }

            if (closed == null)
            {
                throw new ArgumentNullException(nameof(closed));
            }

            if (_finishing)
            {
                throw new InvalidOperationException("SessionRecorder: el cierre ya empezó.");
            }

            _finishing = true;
            _finalPoint = finalPoint;
            _closedSession = closed;
            var creation = _creation;
            if (creation == null || CreateFailed)
            {
                return false;
            }

            long id;
            try
            {
                id = await creation;
            }
            catch (Exception)
            {
                return false;
            }

            if (_disposed)
            {
                return false;
            }

            if (!SessionId.HasValue)
            {
                SessionId = id;
                IsCreating = false;
            }

            // Detrás del último lote: se esperan todos (FlushAndWait puede sumar uno mientras tanto). Si mientras tanto FlushAndWait
            // o Abandon ya encolaron el cierre (ROB-04), se espera ese.
            while (_inFlight.Count > 0 && !_closeQueued)
            {
                var waiting = new Task[_inFlight.Count];
                for (var i = 0; i < waiting.Length; i++)
                {
                    waiting[i] = _inFlight[i].Task;
                }

                try
                {
                    await Task.WhenAll(waiting);
                }
                catch (Exception)
                {
                    // Cada lote fallado lo atiende Settle (sus puntos van al reintento).
                }

                if (_disposed)
                {
                    return false;
                }

                for (var i = _inFlight.Count - 1; i >= 0; i--)
                {
                    if (_inFlight[i].Task.IsCompleted)
                    {
                        Settle(_inFlight[i]);
                    }
                }
            }

            if (_closeQueued)
            {
                return await _queuedClose!;
            }

            try
            {
                await QueueClose(id);
            }
            catch (Exception error)
            {
                if (!_disposed)
                {
                    Fail("cierre de la sesión " + Fmt.Integer(id), error, false);
                }

                return false;
            }

            return true;
        }

        // Al pasar a segundo plano: manda TODO lo pendiente aunque haya otro lote en vuelo (o el alta), también los puntos de
        // los lotes que fallaron (ROB-03), y espera la base, con tope. Con Terminar confirmado y el cierre sin encolar (ROB-04):
        // la mitad del tope para lo que está en vuelo y, si volvió, el cierre en el acto y la otra mitad para él; nunca más del
        // tope en total. true = quedó todo escrito antes del tope.
        public bool FlushAndWait(TimeSpan timeout)
        {
            if (_disposed)
            {
                return true;
            }

            RetryWithThePending();
            FlushPending();
            try
            {
                if (!_finishing || _closeQueued)
                {
                    return _repository.WaitForPendingWork(timeout);
                }

                var half = TimeSpan.FromTicks(timeout.Ticks / 2);
                if (!_repository.WaitForPendingWork(half))
                {
                    return false;
                }

                // Sin el cierre (sin alta, o el alta falló) no queda nada más por escribir.
                return !TryQueueCloseNow() || _repository.WaitForPendingWork(timeout - half);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "espera de la base al pasar a segundo plano: " + error.GetType().Name, error);
                return false;
            }
        }

        // La app se cierra: manda lo pendiente (también detrás del alta, y los puntos a reintentar) y no cierra la sesión: queda
        // cortada, como un corte de luz (P-03). Salvo que Terminar ya estuviera confirmado con el cierre esperando la vuelta de
        // un lote o del alta (ROB-04): se espera la base, con tope, y el cierre se encola acá; LocalStore.Dispose, que va
        // después, termina lo encolado antes de cerrar la base.
        public void Abandon()
        {
            if (_disposed || _abandoned)
            {
                return;
            }

            _abandoned = true;
            RetryWithThePending();
            FlushPending();
            if (!_finishing || _closeQueued)
            {
                return;
            }

            try
            {
                if (_repository.WaitForPendingWork(WorkoutTimings.BackgroundFlushTimeout))
                {
                    TryQueueCloseNow();
                }
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "espera de la base al cerrar la app: " + error.GetType().Name, error);
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private async Task<bool> ObserveCreation(Task<long> creation)
        {
            long id;
            try
            {
                id = await creation;
            }
            catch (Exception error)
            {
                if (_disposed)
                {
                    return false;
                }

                IsCreating = false;
                CreateFailed = true;
                _pending.Clear();
                _retry.Clear();
                Fail("alta de la sesión", error, true);
                return false;
            }

            if (_disposed)
            {
                return true;
            }

            IsCreating = false;
            SessionId = id;
            if (!_finishing && _inFlight.Count == 0)
            {
                SendPending();
            }

            return true;
        }

        // Los puntos de los lotes que fallaron vuelven adelante de los pendientes (son más viejos) para salir con ellos
        // (FlushAndWait, Abandon; ROB-03). Si el lote vuelve a fallar, Settle los deja otra vez para el cierre. Con el cierre ya
        // encolado van en él.
        private void RetryWithThePending()
        {
            if (_retry.Count == 0 || _closeQueued || CreateFailed)
            {
                return;
            }

            _pending.InsertRange(0, _retry);
            _retry.Clear();
        }

        // ROB-04: Terminar confirmado y el cierre que espera, en el hilo principal, la vuelta del alta o de un lote. Si la base ya
        // terminó esos trabajos (sus tareas están completas aunque sus continuaciones del hilo principal no corrieron), se
        // resuelven acá, sin bloquear (IsCompleted antes de Result), y el cierre se encola en el acto. true = quedó encolado.
        private bool TryQueueCloseNow()
        {
            var creation = _creation;
            if (!_finishing || _closeQueued || _disposed || CreateFailed || creation == null || !creation.IsCompleted
                || creation.IsFaulted || creation.IsCanceled)
            {
                return false;
            }

            for (var i = 0; i < _inFlight.Count; i++)
            {
                if (!_inFlight[i].Task.IsCompleted)
                {
                    return false;
                }
            }

            var id = creation.Result;
            if (!SessionId.HasValue)
            {
                SessionId = id;
                IsCreating = false;
            }

            for (var i = _inFlight.Count - 1; i >= 0; i--)
            {
                Settle(_inFlight[i]);
            }

            _queuedClose = ObserveClose(QueueClose(id), id);
            return true;
        }

        // El cierre, en UNA transacción, detrás de todo lo enviado (FIFO): los puntos de los lotes que fallaron (una vez más), los
        // pendientes y el final, al último. Una sola vez por grabador. Devuelve la escritura del puerto.
        private Task QueueClose(long id)
        {
            var final = _finalPoint!;
            var points = new List<DataPoint>(_retry.Count + _pending.Count + 1);
            Stamp(_retry, id, points);
            Stamp(_pending, id, points);
            points.Add(final.SessionId == id ? final : final with { SessionId = id });
            _retry.Clear();
            _pending.Clear();
            _closeQueued = true;
            try
            {
                return _repository.FinishAsync(points, _closedSession! with { Id = id });
            }
            catch (Exception error)
            {
                return Task.FromException(error);
            }
        }

        private async Task<bool> ObserveClose(Task write, long id)
        {
            try
            {
                await write;
            }
            catch (Exception error)
            {
                if (!_disposed)
                {
                    Fail("cierre de la sesión " + Fmt.Integer(id), error, false);
                }

                return false;
            }

            return true;
        }

        private void SendPending()
        {
            if (_pending.Count == 0 || !SessionId.HasValue)
            {
                return;
            }

            var id = SessionId.Value;
            var batch = new List<DataPoint>(_pending.Count);
            Stamp(_pending, id, batch);
            _pending.Clear();
            Send(batch, () => _repository.AddPointsAsync(batch));
        }

        // Lo pendiente sale ya, aunque haya otro lote en vuelo: con el id si volvió el alta; si no, detrás del alta.
        private void FlushPending()
        {
            if (CreateFailed || _closeQueued || _pending.Count == 0)
            {
                return;
            }

            if (SessionId.HasValue)
            {
                SendPending();
                return;
            }

            var creation = _creation;
            if (creation == null)
            {
                return;
            }

            var batch = new List<DataPoint>(_pending);
            _pending.Clear();
            Send(batch, () => _repository.AddPointsAfterCreateAsync(creation, batch));
        }

        private void Send(IReadOnlyList<DataPoint> points, Func<Task> write)
        {
            Task task;
            try
            {
                task = write();
            }
            catch (Exception error)
            {
                task = Task.FromException(error);
            }

            var batch = new Batch(task, points);
            _inFlight.Add(batch);
            _ = ObserveBatch(batch);
        }

        private async Task ObserveBatch(Batch batch)
        {
            try
            {
                await batch.Task;
            }
            catch (Exception)
            {
                // Settle lo atiende.
            }

            Settle(batch);
            if (_disposed || _finishing)
            {
                return;
            }

            if (_inFlight.Count == 0)
            {
                SendPending();
            }
        }

        // Un lote que volvió: sale de los en vuelo y, si falló, sus puntos quedan para el reintento del cierre (una vez).
        private void Settle(Batch batch)
        {
            if (batch.Settled)
            {
                return;
            }

            batch.Settled = true;
            _inFlight.Remove(batch);
            if (_disposed || (!batch.Task.IsFaulted && !batch.Task.IsCanceled))
            {
                return;
            }

            if (CreateFailed)
            {
                return;
            }

            _retry.AddRange(batch.Points);
            var error = batch.Task.Exception?.InnerException ?? (Exception)new TaskCanceledException(batch.Task);
            Fail("lote de " + Fmt.Integer(batch.Points.Count) + " puntos", error, true);
        }

        private void Fail(string what, Exception error, bool notify)
        {
            HadFailure = true;
            _logger.Log(LogLevel.Warning, Tag, what + ": " + error.GetType().Name + ": " + error.Message, error);
            if (!notify || _failureReported)
            {
                return;
            }

            _failureReported = true;
            try
            {
                Failed?.Invoke();
            }
            catch (Exception subscriber)
            {
                _logger.Log(LogLevel.Error, Tag, "aviso de falla: " + subscriber.Message, subscriber);
            }
        }

        private static void Stamp(List<DataPoint> source, long id, List<DataPoint> into)
        {
            for (var i = 0; i < source.Count; i++)
            {
                var point = source[i];
                into.Add(point.SessionId == id ? point : point with { SessionId = id });
            }
        }

        private sealed class Batch
        {
            public Batch(Task task, IReadOnlyList<DataPoint> points)
            {
                Task = task;
                Points = points;
            }

            public Task Task { get; }

            public IReadOnlyList<DataPoint> Points { get; }

            public bool Settled { get; set; }
        }
    }
}
