using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Platform.Power;

namespace RowMate.Platform.Time
{
    // IScheduler de la app (ADR 005; ADR 017, decisión 3; 02-tech-design §4.1 de la It. 4a). Desde la It. 4a es un adaptador
    // de FrameScheduler (Core, puro: el mismo código que corren las pruebas de dotnet): le pasa en cada cuadro la hora del
    // Stopwatch (monótona), DateTimeOffset.UtcNow y el offset local, y expone el paso a segundo plano y la vuelta con alcance,
    // que llama AppRoot desde OnApplicationPause (§4.2).
    // - En primer plano, la conducta de la It. 2 (QA-G2-15): un cuadro = RunDue, a lo sumo un disparo de cada periódico y,
    //   tras un atraso de un período o más (arrastrar la ventana en Windows), se reancla en ahora + período.
    // - La hora es la del CUADRO (la fija Tick), no la del instante en que se lee: todo el cuadro ve la misma hora. Ninguna
    //   medición de rendimiento usa el programador (QA-G3-21: usan su propio Stopwatch).
    // - Volver sin haberse ido (Unity llama OnApplicationPause(false) al arrancar): no hace nada (FrameScheduler).
    // - RA-19 (R4a-12): en Android el Stopwatch no cuenta el sueño profundo; la fuente se elige en la 10a y cambia SOLO acá
    //   (la función monotonic que recibe el constructor de prueba es ese punto).
    // - Hora local del sistema: solo acá (el Core no toca el reloj del sistema).
    // It. 4b (04b-hud-exr/02-tech-design §2.8.3, §2.9; ADR 020):
    // - P-17: arma el FrameClock con la fuente de sueño (ISystemSleepSource: lo dormido según el sistema y los avisos de "se
    //   suspende" y "reanuda") en cada cuadro Y en la vuelta del segundo plano; FrameScheduler descuenta lo dormido (el reloj de la
    //   app es el despierto) y avisa Woke, que este reenvía (AppRoot lo pasa a AppLifecycle.SignalWake).
    // - El offset de la zona se vuelve a leer cada 0,5 s (antes 5 s), sin la caché de TimeZoneInfo (LocalOffsetSource): con el
    //   periódico de 250 ms del reloj de pared, la hora nueva se ve antes de 1 s (AC-WKT-19-04).
    public sealed class UnityScheduler : IScheduler, IDisposable
    {
        public const double OffsetRefreshSeconds = 0.5;

        private readonly Func<double> _monotonic;
        private readonly Func<DateTimeOffset> _utc;
        private readonly Func<DateTimeOffset, TimeSpan> _offsetFor;
        private readonly ISystemSleepSource _sleep;
        private readonly FrameScheduler _frames;
        private readonly LocalOffsetSource? _ownedOffset;
        private TimeSpan _offset;
        private double _offsetReadAt;

        public UnityScheduler(IAppLogger logger)
            : this(logger, NullSleepSource.Instance)
        {
        }

        // La de la app: el Stopwatch, la hora UTC, el offset sin caché y la fuente de sueño de la plataforma.
        public UnityScheduler(IAppLogger logger, ISystemSleepSource sleep)
            : this(logger, StopwatchSeconds(), () => DateTimeOffset.UtcNow, null, sleep)
        {
        }

        // Con un reloj inyectado (UnitySchedulerTests): el monótono en segundos, la hora UTC y el offset local de una hora.
        public UnityScheduler(IAppLogger logger, Func<double> monotonic, Func<DateTimeOffset> utc, Func<DateTimeOffset, TimeSpan> offsetFor)
            : this(logger, monotonic, utc, offsetFor, NullSleepSource.Instance)
        {
        }

        // Con todo inyectado (UnitySchedulerTests de la 4b: la fuente de sueño de prueba).
        public UnityScheduler(IAppLogger logger, Func<double> monotonic, Func<DateTimeOffset> utc, Func<DateTimeOffset, TimeSpan>? offsetFor,
                              ISystemSleepSource sleep)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _monotonic = monotonic ?? throw new ArgumentNullException(nameof(monotonic));
            _utc = utc ?? throw new ArgumentNullException(nameof(utc));
            _sleep = sleep ?? throw new ArgumentNullException(nameof(sleep));
            if (offsetFor == null)
            {
                _ownedOffset = new LocalOffsetSource();
                offsetFor = _ownedOffset.OffsetFor;
            }

            _offsetFor = offsetFor;
            var now = _utc();
            _offset = _offsetFor(now);
            _offsetReadAt = _monotonic();
            _frames = new FrameScheduler(_offsetReadAt, now, _offset, logger);
            _frames.Woke += wake => Woke?.Invoke(wake);
        }

        // El programador puro que hay adentro (las pruebas miran Pending, InBackground).
        public FrameScheduler Frames => _frames;

        // El equipo despertó (P-17): en el cuadro (o la vuelta) que lo trae, después de correr lo vencido.
        public event Action<SystemWake>? Woke;

        public ISystemSleepSource SleepSource => _sleep;

        public DateTimeOffset UtcNow => _frames.UtcNow;

        public DateTimeOffset LocalNow => _frames.LocalNow;

        public double MonotonicSeconds => _frames.MonotonicSeconds;

        public bool IsCatchingUp => _frames.IsCatchingUp;

        public bool InBackground => _frames.InBackground;

        public int Pending => _frames.Pending;

        public IDisposable Schedule(TimeSpan delay, Action callback)
        {
            return _frames.Schedule(delay, callback);
        }

        public IDisposable SchedulePeriodic(TimeSpan period, Action callback)
        {
            return _frames.SchedulePeriodic(period, callback);
        }

        public Task Delay(TimeSpan delay, CancellationToken ct = default)
        {
            return _frames.Delay(delay, ct);
        }

        // Un cuadro (AppRoot.Update): fija la hora del cuadro y corre lo vencido. Devuelve cuántos callbacks corrieron.
        public int Tick()
        {
            var clock = Clock();
            return _frames.Tick(in clock);
        }

        // OnApplicationPause(true) (Android e iOS): solo anota; los timers quedan como están.
        public void EnterBackground()
        {
            _frames.EnterBackground();
        }

        // OnApplicationPause(false): el alcance en tiempo virtual hasta el monótono DESPIERTO (QA-G4bT-05 (a)); sin un
        // EnterBackground antes, nada.
        public int ResumeFromBackground()
        {
            var clock = Clock();
            return _frames.ResumeFromBackground(in clock);
        }

        public void Dispose()
        {
            _ownedOffset?.Dispose();
        }

        private FrameClock Clock()
        {
            var monotonic = _monotonic();
            var utc = _utc();
            return new FrameClock(monotonic, utc, Offset(monotonic, utc), _sleep.AsleepSeconds, _sleep.SuspendSignals, _sleep.ResumeSignals);
        }

        private TimeSpan Offset(double monotonic, DateTimeOffset utc)
        {
            if (monotonic - _offsetReadAt >= OffsetRefreshSeconds || monotonic < _offsetReadAt)
            {
                _offsetReadAt = monotonic;
                _offset = _offsetFor(utc);
            }

            return _offset;
        }

        private static Func<double> StopwatchSeconds()
        {
            var stopwatch = Stopwatch.StartNew();
            return () => stopwatch.Elapsed.TotalSeconds;
        }
    }
}
