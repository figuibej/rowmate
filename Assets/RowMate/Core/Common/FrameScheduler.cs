using System;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Text;

namespace RowMate.Core.Common
{
    // El IScheduler de la app, puro (ADR 005; ADR 017, decisión 3; 02-tech-design §4.1 de la It. 4a). Quien lo avanza le pasa
    // la hora: UnityScheduler (Platform) en cada cuadro, con el Stopwatch y DateTimeOffset.UtcNow; las pruebas, con números.
    // Es el mismo código en los dos mundos. Hilo principal, sin locks.
    // - La hora es la del CUADRO: MonotonicSeconds, UtcNow y LocalNow son los que fijó el último Tick (no los del instante
    //   en que se leen): todo el cuadro ve la misma hora. Ninguna medición de rendimiento usa el programador (QA-G3-21).
    // - Tick: un cuadro en primer plano = RealtimeTimerQueue.RunDue, la conducta de la It. 2 (QA-G2-15): a lo sumo un
    //   disparo de cada periódico y, tras un atraso de un período o más, se reancla en ahora + período.
    // - EnterBackground: la app pasa a segundo plano (Android e iOS: el loop se detiene). Solo anota.
    // - ResumeFromBackground: el alcance (RealtimeTimerQueue.CatchUp) desde el último cuadro hasta ahora, en tiempo virtual
    //   y con IsCatchingUp: el remo simulado y el motor disparan en orden todo lo vencido, con la hora de su instante. Sin un
    //   EnterBackground antes (Unity llama OnApplicationPause(false) al arrancar) no hace nada.
    // - Hora virtual durante el alcance: MonotonicSeconds = el vencimiento en curso; UtcNow = hora de la vuelta − (ahora −
    //   virtual), nunca antes de la del último cuadro (RA-19: en Android el monótono puede saltar menos que la hora de pared;
    //   así la hora virtual no retrocede); LocalNow = UtcNow con el offset de la vuelta.
    // - It. 4b (P-17 de la 4a; 02-tech-design §2.9; ADR 020): el monótono del programador es el DESPIERTO. Tick(in FrameClock)
    //   y ResumeFromBackground(in FrameClock) le pasan el crudo, la medida del sueño y los avisos del sistema a SuspendAccounting,
    //   que resta lo dormido que el crudo contó; el alcance va hasta el despierto. Si hubo despertar, Woke avisa DESPUÉS de
    //   correr lo vencido. Tick(double, …) y ResumeFromBackground(double, …) siguen: son FrameClock sin medida ni avisos.
    public sealed class FrameScheduler : IScheduler
    {
        public const string Tag = RealtimeTimerQueue.Tag;

        // Tope de disparos de un alcance, como FakeScheduler.MaxCallbacksPerAdvance: un timer de 0 que se reagenda no cuelga.
        public const int MaxCatchUpCallbacks = 1000000;

        private readonly RealtimeTimerQueue _queue;
        private readonly SuspendAccounting _accounting;
        private double _frameMonotonic;
        private DateTimeOffset _frameUtc;
        private TimeSpan _localOffset;
        private double _virtualMonotonic;
        private DateTimeOffset _utcFloor;
        private bool _catchingUp;

        public FrameScheduler(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset, IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _queue = new RealtimeTimerQueue(logger);
            _frameMonotonic = monotonicNow;
            _frameUtc = utcNow.ToUniversalTime();
            _localOffset = localOffset;
            _accounting = new SuspendAccounting(FrameClock.Plain(monotonicNow, _frameUtc, localOffset), logger);
        }

        // El despertar del equipo (P-17 de la 4a), en el cuadro (o la vuelta del segundo plano) que lo trae, después de correr lo
        // vencido. UnityScheduler lo pasa a AppLifecycle.SignalWake (IAppLifecycle.Woke).
        public event Action<SystemWake>? Woke;

        // Lo dormido que se le restó al monótono crudo, en total.
        public double SleepBias => _accounting.Bias;

        // Llegó un "se suspende" sin su "reanuda" (para el registro y las pruebas).
        public bool SuspendPending => _accounting.SuspendPending;

        public DateTimeOffset UtcNow
        {
            get
            {
                if (!_catchingUp)
                {
                    return _frameUtc;
                }

                var virtualUtc = _frameUtc - TimeSpan.FromTicks(DartMath.Round((_frameMonotonic - _virtualMonotonic) * TimeSpan.TicksPerSecond));
                return virtualUtc < _utcFloor ? _utcFloor : virtualUtc;
            }
        }

        public DateTimeOffset LocalNow => UtcNow.ToOffset(_localOffset);

        public double MonotonicSeconds => _catchingUp ? _virtualMonotonic : _frameMonotonic;

        public bool IsCatchingUp => _catchingUp;

        public bool InBackground { get; private set; }

        // Timers vivos.
        public int Pending => _queue.Pending;

        // Un cuadro: fija la hora del cuadro (el monótono nunca retrocede) y corre lo vencido. Devuelve cuántos disparos hubo.
        public int Tick(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset)
        {
            return Tick(FrameClock.Plain(monotonicNow, utcNow, localOffset));
        }

        // It. 4b: un cuadro con el reloj despierto (la conducta de la 4a con el monótono despierto) y, si hubo despertar, Woke.
        public int Tick(in FrameClock clock)
        {
            var awake = _accounting.Awake(clock, out var woke);
            SetFrame(awake, clock.Utc, clock.LocalOffset);
            var fired = _queue.RunDue(_frameMonotonic);
            if (woke.HasValue)
            {
                Woke?.Invoke(woke.Value);
            }

            return fired;
        }

        public void EnterBackground()
        {
            InBackground = true;
        }

        // Vuelve del segundo plano: alcance desde el último cuadro hasta monotonicNow. Devuelve cuántos disparos hubo.
        public int ResumeFromBackground(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset)
        {
            return ResumeFromBackground(FrameClock.Plain(monotonicNow, utcNow, localOffset));
        }

        // It. 4b (QA-G4bT-05 (a)): el alcance HASTA EL MONÓTONO DESPIERTO, con el mismo Awake del cuadro: si una suspensión volviera
        // por este camino, el lapso dormido tampoco se procesa. Woke sale después del alcance.
        public int ResumeFromBackground(in FrameClock clock)
        {
            if (!InBackground)
            {
                return 0;
            }

            InBackground = false;
            var awake = _accounting.Awake(clock, out var woke, true);
            var from = _frameMonotonic;
            _utcFloor = _frameUtc;
            SetFrame(awake, clock.Utc, clock.LocalOffset);
            _virtualMonotonic = from;
            _catchingUp = true;
            int fired;
            try
            {
                fired = _queue.CatchUp(from, _frameMonotonic, SetVirtual, MaxCatchUpCallbacks);
            }
            finally
            {
                _catchingUp = false;
            }

            if (woke.HasValue)
            {
                Woke?.Invoke(woke.Value);
            }

            return fired;
        }

        public IDisposable Schedule(TimeSpan delay, Action callback)
        {
            return _queue.Schedule(MonotonicSeconds, delay, callback);
        }

        public IDisposable SchedulePeriodic(TimeSpan period, Action callback)
        {
            return _queue.SchedulePeriodic(MonotonicSeconds, period, callback);
        }

        public Task Delay(TimeSpan delay, CancellationToken ct = default)
        {
            return _queue.Delay(MonotonicSeconds, delay, ct);
        }

        private void SetFrame(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset)
        {
            if (monotonicNow > _frameMonotonic)
            {
                _frameMonotonic = monotonicNow;
            }

            _frameUtc = utcNow.ToUniversalTime();
            _localOffset = localOffset;
        }

        private void SetVirtual(double seconds)
        {
            _virtualMonotonic = seconds;
        }
    }
}
