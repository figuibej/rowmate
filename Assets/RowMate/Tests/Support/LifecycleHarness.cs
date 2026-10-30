using System;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // El "ciclo de vida de prueba" de la spec §0.4 (02-tech-design §4.5 de la It. 4a): el FrameScheduler de la app con cuadros
    // a 60 fps y un FakeAppLifecycle, con el orden de AppRoot (§4.2): al pasar a segundo plano, InBackground = true y después
    // EnterBackground; al volver, ResumeFromBackground (el alcance) y después InBackground = false. El reloj monótono 0 es el
    // instante −0,5 s (la primera lectura de R-4): Instant(k) = k + 0,5. Sin pedido, adelante y con foco. Existe para probar
    // el camino de la app (un disparo por cuadro y el alcance al volver) con el mismo FrameScheduler que corre en Unity; con
    // FakeScheduler el alcance es implícito (Advance ya dispara todo en orden).
    // It. 4b (P-17 de la 4a; 02-tech-design §2.9, §6.1): cada cuadro le pasa al programador un FrameClock con el monótono
    // CRUDO, la hora de pared, el offset local, la medida del sueño del sistema y los avisos de "se suspende" y "reanuda".
    // Suspend / Wake simulan la suspensión del equipo en los dos casos del monótono (el crudo cuenta el sueño o no), con aviso,
    // con medida o con los dos, con el "reanuda" antes o después del primer cuadro despierto y con la vuelta por el segundo
    // plano. Woke del programador va a FakeAppLifecycle.SignalWake, como en AppRoot. Los instantes de la spec son los del reloj
    // DESPIERTO (el de la sesión).
    public sealed class LifecycleHarness
    {
        private readonly DateTimeOffset _zeroUtc;
        private readonly double _frame;
        private double _raw;
        private double _wallExtra;
        private double _asleep;
        private int _suspends;
        private int _resumes;
        private TimeSpan _offset;

        // sessionStartUtc: la hora de pared del instante 0 (2026-10-28T13:00:00.000Z en la spec).
        public LifecycleHarness(DateTimeOffset sessionStartUtc, double fps = 60, IAppLogger? logger = null)
        {
            if (fps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fps));
            }

            _zeroUtc = sessionStartUtc.ToUniversalTime().AddSeconds(-0.5);
            _frame = 1.0 / fps;
            _offset = Offset;
            Logger = logger ?? new RecordingLogger();
            Scheduler = new FrameScheduler(0, _zeroUtc, _offset, Logger);
            Lifecycle = new FakeAppLifecycle(Logger);
            Scheduler.Woke += wake => Lifecycle.SignalWake(wake);
        }

        // La hora local de las pruebas (UTC−3, como FakeScheduler).
        public static TimeSpan Offset => TimeSpan.FromHours(-3);

        public IAppLogger Logger { get; }

        public FrameScheduler Scheduler { get; }

        public FakeAppLifecycle Lifecycle { get; }

        // El instante actual de la spec (el monótono despierto − 0,5).
        public double Now => Scheduler.MonotonicSeconds - 0.5;

        // El monótono crudo (el Stopwatch) del último cuadro.
        public double RawMonotonic => _raw;

        // Monótono del instante k de la spec.
        public static double Instant(double k)
        {
            return k + 0.5;
        }

        // Cuadros cada 1/fps hasta ese instante (inclusive), en el reloj despierto.
        public void RunUntil(double instant)
        {
            var target = Instant(instant);
            while (Awake(_raw + _frame) <= target + 1e-9)
            {
                _raw += _frame;
                Tick();
            }

            if (Awake(_raw) < target - 1e-9)
            {
                _raw = target + Scheduler.SleepBias;
                Tick();
            }
        }

        // Un cuadro que llega tarde (AC-WKT-02-04: arrastrar la ventana en Windows): el loop no corre durante `seconds` y
        // después llega un solo cuadro. El crudo cuenta; sin evidencia de sueño, el lapso cuenta (la 4a).
        public void Stall(double seconds)
        {
            _raw += seconds;
            Tick();
        }

        // Corre hasta ahí, InBackground = true y EnterBackground; sin cuadros mientras tanto.
        public void Background(double instant)
        {
            RunUntil(instant);
            Lifecycle.SetBackground(true);
            Scheduler.EnterBackground();
        }

        // Vuelve en ese instante: el alcance y después InBackground = false.
        public void Foreground(double instant)
        {
            _raw = Instant(instant) + Scheduler.SleepBias;
            Scheduler.ResumeFromBackground(Clock());
            Lifecycle.SetBackground(false);
        }

        // Corre hasta ahí y saca el foco (Windows minimizada: el loop sigue).
        public void Unfocus(double instant)
        {
            RunUntil(instant);
            Lifecycle.SetFocus(false);
        }

        // Corre hasta ahí y devuelve el foco.
        public void Focus(double instant)
        {
            RunUntil(instant);
            Lifecycle.SetFocus(true);
        }

        // RA-19: vuelve con un salto monótono distinto del de la hora de pared (Android: el Stopwatch no cuenta el sueño).
        public void ResumeWithMonotonicJump(double monotonicSeconds, double utcSeconds)
        {
            var wall = _raw + _wallExtra + utcSeconds;
            _raw += monotonicSeconds;
            _wallExtra = wall - _raw;
            Scheduler.ResumeFromBackground(Clock());
            Lifecycle.SetBackground(false);
        }

        // ── It. 4b: P-17 de la 4a (la suspensión del equipo) ──

        // Corre hasta ese instante y el sistema avisa "se suspende" (signal); la app alcanza a procesar un cuadro más con el
        // aviso (si framesAfterSignal > 0) y el equipo se duerme: no corre ningún cuadro hasta Wake.
        public void Suspend(double instant, bool signal = true, int framesAfterSignal = 1)
        {
            RunUntil(instant);
            if (signal)
            {
                _suspends++;
            }

            for (var i = 0; i < framesAfterSignal; i++)
            {
                _raw += _frame;
                Tick();
            }
        }

        // El equipo despierta después de `seconds` dormido. monotonicCounts: el crudo contó el sueño (o no: QA-V4a-ROB-05, se
        // prueban los dos casos). measured: la contabilidad de sueño del sistema lo contó. resumeSignal: llega el "reanuda";
        // resumeBeforeFirstFrame: antes o después del primer cuadro despierto (los avisos corren en un hilo del sistema).
        // viaBackground: la suspensión fue con la app en segundo plano y vuelve por ResumeFromBackground (QA-G4bT-05 (a)).
        public void Wake(double seconds, bool monotonicCounts, bool measured = true, bool resumeSignal = true, bool resumeBeforeFirstFrame = true,
                         bool viaBackground = false)
        {
            if (seconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            if (monotonicCounts)
            {
                _raw += seconds;
            }
            else
            {
                _wallExtra += seconds;
            }

            if (measured)
            {
                _asleep += seconds;
            }

            if (resumeSignal && resumeBeforeFirstFrame)
            {
                _resumes++;
            }

            _raw += _frame;
            if (viaBackground)
            {
                Scheduler.ResumeFromBackground(Clock());
                Lifecycle.SetBackground(false);
            }
            else
            {
                Tick();
            }

            if (resumeSignal && !resumeBeforeFirstFrame)
            {
                _resumes++;
                _raw += _frame;
                Tick();
            }
        }

        // La zona horaria del equipo cambia (AC-WKT-19-04): desde el próximo cuadro.
        public void SetLocalOffset(TimeSpan offset)
        {
            _offset = offset;
        }

        // El usuario cambia la hora a mano (AC-WKT-19-04): la hora de pared salta, el monótono no.
        public void ShiftWallClock(double seconds)
        {
            _wallExtra += seconds;
        }

        private double Awake(double raw)
        {
            return raw - Scheduler.SleepBias;
        }

        private FrameClock Clock()
        {
            return new FrameClock(_raw, Utc(), _offset, _asleep, _suspends, _resumes);
        }

        private void Tick()
        {
            Scheduler.Tick(Clock());
        }

        private DateTimeOffset Utc()
        {
            return _zeroUtc.AddTicks((long)((_raw + _wallExtra) * TimeSpan.TicksPerSecond + 0.5));
        }
    }
}
