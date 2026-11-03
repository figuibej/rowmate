using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // MDI · El MotionDriver aísla cada animación (It. 5; O-4 de la lente de motion de la 4b; 05-series-parciales/00-deuda-4b §4):
    // una que lanza sale del driver y queda en el registro como error, y las demás del mismo cuadro siguen (la transición de la
    // página incluida: la salida del inmersivo no queda trabada en negro por otra animación). Las demandas de 60 fps vuelven.
    public sealed class MotionDriverIsolationTests
    {
        private sealed class Counting : IMotion
        {
            public int Ticks;

            public bool IsFinite => true;

            public bool Tick(double nowSeconds)
            {
                Ticks++;
                return true;
            }
        }

        private sealed class Throwing : IMotion
        {
            public int Ticks;

            public bool IsFinite => true;

            public bool Tick(double nowSeconds)
            {
                Ticks++;
                throw new InvalidOperationException("prueba O-4");
            }
        }

        [Test, Category("AC-WKT-13-09"), Category("QA-V4b-O-4")]
        public void AThrowingMotionLeavesAndTheOthersKeepGoing()
        {
            var logger = new RecordingLogger();
            var clock = new FakeScheduler();
            var pacing = new FramePacingPolicy();
            var driver = new MotionDriver(clock, pacing, new MotionSettings(), logger);
            var before = new Counting();
            var bad = new Throwing();
            var after = new Counting();
            driver.Add(before);
            driver.Add(bad);
            driver.Add(after);
            Assert.That(pacing.Demands, Is.EqualTo(3));

            Assert.DoesNotThrow(() => driver.Tick(), "el cuadro no se corta");
            Assert.That(before.Ticks, Is.EqualTo(1));
            Assert.That(after.Ticks, Is.EqualTo(1), "la que viene después también avanza en ese cuadro");
            Assert.That(driver.Contains(bad), Is.False, "la que lanzó sale");
            Assert.That(driver.Faulted, Is.EqualTo(1));
            Assert.That(driver.ActiveCount, Is.EqualTo(2));
            Assert.That(pacing.Demands, Is.EqualTo(2), "y suelta su pedido de 60 fps");
            Assert.That(logger.Contains("[UI] Error: una animación lanzó InvalidOperationException y sale del cuadro (Throwing)"), Is.True,
                string.Join("\n", logger.Lines));

            clock.Advance(TimeSpan.FromMilliseconds(16));
            driver.Tick();
            Assert.That(bad.Ticks, Is.EqualTo(1), "no vuelve a correr");
            Assert.That(before.Ticks + after.Ticks, Is.EqualTo(4));
        }

        // Una HudMotion que lanza una vez en su cuadro (LM5-03 de la verificación de la 5).
        private sealed class ThrowsOnce : RowMate.Presentation.Motion.Hud.HudMotion
        {
            public int Advances;
            public bool Throw = true;

            public ThrowsOnce(MotionDriver driver)
                : base(driver)
            {
            }

            public override bool IsFinite => IsRunning;

            public void Start()
            {
                Run();
            }

            protected override bool Advance(double nowSeconds)
            {
                Advances++;
                if (Throw)
                {
                    Throw = false;
                    throw new InvalidOperationException("prueba LM5-03");
                }

                return true;
            }
        }

        // LM5-03: la HudMotion que lanzó sale del driver y lo sabe: IsRunning en false (SeriesColumn.IsAnimating y la regla de toma
        // no esperan sin fin) y la llamada siguiente la vuelve a agregar y anima (antes quedaba "adentro" para siempre: Run volvía
        // sin agregarla y el panel quedaba congelado en el primer cuadro de cada animación).
        [Test, Category("AC-WKT-13-16"), Category("QA-V4b-O-4")]
        public void AHudMotionThatThrewRecovers()
        {
            var logger = new RecordingLogger();
            var clock = new FakeScheduler();
            var pacing = new FramePacingPolicy();
            var driver = new MotionDriver(clock, pacing, new MotionSettings(), logger);
            var motion = new ThrowsOnce(driver);
            motion.Start();
            Assert.That(motion.IsRunning, Is.True);
            Assert.That(driver.Contains(motion), Is.True);

            Assert.DoesNotThrow(() => driver.Tick());
            Assert.That(driver.Faulted, Is.EqualTo(1));
            Assert.That(driver.Contains(motion), Is.False, "sale del driver");
            Assert.That(motion.IsRunning, Is.False, "y no dice que sigue corriendo");
            Assert.That(pacing.Demands, Is.EqualTo(0), "sin pedido de 60 fps colgado");

            motion.Start();
            Assert.That(driver.Contains(motion), Is.True, "la llamada siguiente la vuelve a agregar");
            clock.Advance(TimeSpan.FromMilliseconds(16));
            driver.Tick();
            Assert.That(motion.Advances, Is.EqualTo(2), "y anima");
            Assert.That(motion.IsRunning, Is.True);
        }

        [Test, Category("QA-V4b-O-4")]
        public void WithoutLoggerItStillIsolates()
        {
            var driver = new MotionDriver(new FakeScheduler(), new FramePacingPolicy(), new MotionSettings());
            var ok = new Counting();
            driver.Add(new Throwing());
            driver.Add(ok);
            Assert.DoesNotThrow(() => driver.Tick());
            Assert.That(ok.Ticks, Is.EqualTo(1));
            Assert.That(driver.ActiveCount, Is.EqualTo(1));
        }
    }
}
