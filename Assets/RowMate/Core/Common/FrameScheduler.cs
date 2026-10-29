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
    public sealed class FrameScheduler : IScheduler
    {
        public const string Tag = RealtimeTimerQueue.Tag;

        // Tope de disparos de un alcance, como FakeScheduler.MaxCallbacksPerAdvance: un timer de 0 que se reagenda no cuelga.
        public const int MaxCatchUpCallbacks = 1000000;

        private readonly RealtimeTimerQueue _queue;
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
        }

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
            SetFrame(monotonicNow, utcNow, localOffset);
            return _queue.RunDue(_frameMonotonic);
        }

        public void EnterBackground()
        {
            InBackground = true;
        }

        // Vuelve del segundo plano: alcance desde el último cuadro hasta monotonicNow. Devuelve cuántos disparos hubo.
        public int ResumeFromBackground(double monotonicNow, DateTimeOffset utcNow, TimeSpan localOffset)
        {
            if (!InBackground)
            {
                return 0;
            }

            InBackground = false;
            var from = _frameMonotonic;
            _utcFloor = _frameUtc;
            SetFrame(monotonicNow, utcNow, localOffset);
            _virtualMonotonic = from;
            _catchingUp = true;
            try
            {
                return _queue.CatchUp(from, _frameMonotonic, SetVirtual, MaxCatchUpCallbacks);
            }
            finally
            {
                _catchingUp = false;
            }
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
