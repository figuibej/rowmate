using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;

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
    // - Hora local del sistema: solo acá (el Core no toca el reloj del sistema). El offset se vuelve a leer cada 5 s.
    public sealed class UnityScheduler : IScheduler
    {
        private const double OffsetRefreshSeconds = 5;

        private readonly Func<double> _monotonic;
        private readonly Func<DateTimeOffset> _utc;
        private readonly Func<DateTimeOffset, TimeSpan> _offsetFor;
        private readonly FrameScheduler _frames;
        private TimeSpan _offset;
        private double _offsetReadAt;

        public UnityScheduler(IAppLogger logger)
            : this(logger, StopwatchSeconds(), () => DateTimeOffset.UtcNow, utc => TimeZoneInfo.Local.GetUtcOffset(utc))
        {
        }

        // Con un reloj inyectado (UnitySchedulerTests): el monótono en segundos, la hora UTC y el offset local de una hora.
        public UnityScheduler(IAppLogger logger, Func<double> monotonic, Func<DateTimeOffset> utc, Func<DateTimeOffset, TimeSpan> offsetFor)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _monotonic = monotonic ?? throw new ArgumentNullException(nameof(monotonic));
            _utc = utc ?? throw new ArgumentNullException(nameof(utc));
            _offsetFor = offsetFor ?? throw new ArgumentNullException(nameof(offsetFor));
            var now = _utc();
            _offset = _offsetFor(now);
            _offsetReadAt = _monotonic();
            _frames = new FrameScheduler(_offsetReadAt, now, _offset, logger);
        }

        // El programador puro que hay adentro (las pruebas miran Pending, InBackground).
        public FrameScheduler Frames => _frames;

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
            var monotonic = _monotonic();
            var utc = _utc();
            return _frames.Tick(monotonic, utc, Offset(monotonic, utc));
        }

        // OnApplicationPause(true) (Android e iOS): solo anota; los timers quedan como están.
        public void EnterBackground()
        {
            _frames.EnterBackground();
        }

        // OnApplicationPause(false): el alcance en tiempo virtual hasta ahora (sin un EnterBackground antes, nada).
        public int ResumeFromBackground()
        {
            var monotonic = _monotonic();
            var utc = _utc();
            return _frames.ResumeFromBackground(monotonic, utc, Offset(monotonic, utc));
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
