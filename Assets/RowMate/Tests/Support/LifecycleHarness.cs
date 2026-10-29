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
    public sealed class LifecycleHarness
    {
        private readonly DateTimeOffset _zeroUtc;
        private readonly double _frame;
        private double _now;

        // sessionStartUtc: la hora de pared del instante 0 (2026-10-28T13:00:00.000Z en la spec).
        public LifecycleHarness(DateTimeOffset sessionStartUtc, double fps = 60, IAppLogger? logger = null)
        {
            if (fps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fps));
            }

            _zeroUtc = sessionStartUtc.ToUniversalTime().AddSeconds(-0.5);
            _frame = 1.0 / fps;
            Logger = logger ?? new RecordingLogger();
            Scheduler = new FrameScheduler(0, _zeroUtc, Offset, Logger);
            Lifecycle = new FakeAppLifecycle(Logger);
        }

        // La hora local de las pruebas (UTC−3, como FakeScheduler).
        public static TimeSpan Offset => TimeSpan.FromHours(-3);

        public IAppLogger Logger { get; }

        public FrameScheduler Scheduler { get; }

        public FakeAppLifecycle Lifecycle { get; }

        // El instante actual de la spec (monótono − 0,5).
        public double Now => _now - 0.5;

        // Monótono del instante k de la spec.
        public static double Instant(double k)
        {
            return k + 0.5;
        }

        // Cuadros cada 1/fps hasta ese instante (inclusive).
        public void RunUntil(double instant)
        {
            var target = Instant(instant);
            while (_now + _frame <= target + 1e-9)
            {
                _now += _frame;
                Tick();
            }

            if (_now < target - 1e-9)
            {
                _now = target;
                Tick();
            }
        }

        // Un cuadro que llega tarde (AC-WKT-02-04: arrastrar la ventana en Windows): el loop no corre durante `seconds` y
        // después llega un solo cuadro.
        public void Stall(double seconds)
        {
            _now += seconds;
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
            _now = Instant(instant);
            Scheduler.ResumeFromBackground(_now, Utc(_now), Offset);
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
            var utc = Scheduler.UtcNow.AddSeconds(utcSeconds);
            _now += monotonicSeconds;
            Scheduler.ResumeFromBackground(_now, utc, Offset);
            Lifecycle.SetBackground(false);
        }

        private void Tick()
        {
            Scheduler.Tick(_now, Utc(_now), Offset);
        }

        private DateTimeOffset Utc(double monotonic)
        {
            return _zeroUtc.AddTicks((long)(monotonic * TimeSpan.TicksPerSecond + 0.5));
        }
    }
}
