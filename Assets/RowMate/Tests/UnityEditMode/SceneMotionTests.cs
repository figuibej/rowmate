using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // SMO · Los IMotion de la escena (06a-escena-lago/02-tech-design §7.4; 04-design-notes §11.6, §11.7, §12.4; WP-D1), con el reloj
    // de prueba y cuadros de 1/60 s. Lo que se mira en cada uno: los valores que le pasa a la vista cuadro a cuadro son los de su línea
    // de tiempo pura (SceneTimelines, WP-A) con los ms enteros de HudTimelines.Ms; IsFinite mientras corre; reducir movimiento; Instant
    // y sin armar (estado final en el acto); y Stop: nada queda animando (ni en el MotionDriver ni pidiendo 60 fps) y ningún aviso
    // después (ROB-02).
    public sealed class SceneMotionTests
    {
        private const double Eps = 1e-9;

        // ── La escena demorada (AC-SCN-01-07): 0 → 1 en 0,6 s con ease.enter, y avisa una vez al terminar ──

        [Test]
        public void TheLateSceneFadesInSixTenthsAndNotifiesOnce()
        {
            foreach (var reduced in new[] { false, true })
            {
                var rig = new Rig(reduceMotion: reduced);
                var opacity = -1.0;
                var finished = 0;
                long finishedAt = -1;
                var start = rig.Now;
                var motion = new SceneFadeMotion(rig.Driver, o => opacity = o, () =>
                {
                    finished++;
                    finishedAt = HudTimelines.Ms(rig.Now - start);
                });
                motion.Play();
                Assert.That(opacity, Is.EqualTo(0), "arranca invisible");
                Assert.That(motion.IsFinite, Is.True, "bloquea una toma mientras entra");
                for (var k = 1; k <= 40; k++)
                {
                    rig.Frame();
                    var t = HudTimelines.Ms(rig.Now - start);
                    Assert.That(opacity, Is.EqualTo(t >= MotionSpecs.SceneLateFadeMs ? 1.0 : SceneTimelines.LateScene(t)).Within(Eps),
                        "en " + t + " ms" + (reduced ? " (reducido: es un fundido, igual)" : ""));
                    Assert.That(motion.IsFinite, Is.EqualTo(t < MotionSpecs.SceneLateFadeMs), "en " + t);
                }

                Assert.That(finished, Is.EqualTo(1), "avisa una sola vez");
                Assert.That(finishedAt, Is.InRange((long)MotionSpecs.SceneLateFadeMs, (long)MotionSpecs.SceneLateFadeMs + 17));
                Assert.That(motion.Opacity, Is.EqualTo(1));
                rig.AssertIdle("terminó la entrada de la escena");
            }
        }

        [Test]
        public void TheLateSceneWithInstantOrUnarmedIsShownAtOnce()
        {
            var rig = new Rig();
            var opacity = -1.0;
            var finished = 0;
            var motion = new SceneFadeMotion(rig.Driver, o => opacity = o, () => finished++);
            motion.Play(true);
            Assert.That(opacity, Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(1));
            rig.AssertIdle("Instant");

            var unarmed = new Rig(armed: false);
            var other = new SceneFadeMotion(unarmed.Driver, o => opacity = o);
            opacity = -1;
            other.Play();
            Assert.That(opacity, Is.EqualTo(1), "sin armar: en el acto");
            unarmed.AssertIdle("sin armar");
        }

        [Test]
        public void StoppingTheLateSceneLeavesNothingAnimating()
        {
            var rig = new Rig();
            var applied = 0;
            var finished = 0;
            var motion = new SceneFadeMotion(rig.Driver, _ => applied++, () => finished++);
            motion.Play();
            rig.RunFor(200);
            motion.Stop();
            var before = applied;
            rig.AssertIdle("Stop");
            rig.RunFor(1000);
            Assert.That(applied, Is.EqualTo(before), "ningún cuadro después de Stop");
            Assert.That(finished, Is.EqualTo(0), "sin aviso después de Stop");
            Assert.That(motion.IsRunning, Is.False);
        }

        // ── El HUD al cambiar de clase de ventana (DT6a-06; AC-WKT-17-07): 0 → 1 en 200 con ease.enter, desde donde esté ──

        [Test]
        public void TheHudEntersInTwoHundredAfterAClassChange()
        {
            var rig = new Rig();
            var opacity = -1.0;
            var motion = new HudReflowMotion(rig.Driver, (g, o, _) => { if (g == 0) { opacity = o; } });
            Assert.That(motion.Opacity, Is.EqualTo(1), "quieto: a pleno");
            var start = rig.Now;
            motion.OnOrientationClassChanged();
            Assert.That(opacity, Is.EqualTo(0), "las piezas quedan en 0 en el layout nuevo");
            for (var k = 1; k <= 22; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                var expected = t >= MotionSpecs.HudReflowMs ? 1.0 : SceneTimelines.HudReflow(t, false);
                Assert.That(opacity, Is.EqualTo(expected).Within(Eps), "en " + t + " ms");
                // It. 15 (AC-D15-32): el grupo 0 sigue la curva de siempre; la cascada termina con el grupo 2 a los 280.
                Assert.That(motion.IsFinite, Is.EqualTo(t < MotionSpecs.HudReflowMs + 2 * MotionSpecs.HudReflowGroupDelayMs));
            }

            rig.AssertIdle("terminó el reacomodo");
        }

        // It. 14 (T-07, la regla de los 400 ms; reemplaza "arranca desde donde está" de la 6a): otro cambio de clase antes de 400 ms
        // del anterior no repite la entrada (arrastrar el borde): el HUD queda a pleno en el acto. Pasados los 400, entra otra vez en 200.
        [Test, Category("AC-APP-15-10")]
        public void AnotherClassChangeWithinFourHundredIsAtOnce()
        {
            var rig = new Rig();
            var opacity = -1.0;
            var motion = new HudReflowMotion(rig.Driver, (g, o, _) => { if (g == 0) { opacity = o; } });
            motion.OnOrientationClassChanged();
            rig.RunFor(100);
            Assert.That(opacity, Is.GreaterThan(0).And.LessThan(1));
            motion.OnOrientationClassChanged();
            Assert.That(opacity, Is.EqualTo(1), "a los 100 ms del anterior: en el acto");
            rig.AssertIdle("sin entrada repetida");
            rig.RunFor(350);
            motion.OnOrientationClassChanged();
            Assert.That(opacity, Is.EqualTo(1), "a los ~350 ms del anterior: sigue en el acto");
            rig.RunFor(450);
            var start = rig.Now;
            motion.OnOrientationClassChanged();
            Assert.That(opacity, Is.EqualTo(0), "espaciado más de 400 ms: entra otra vez");
            rig.Frame();
            var t = HudTimelines.Ms(rig.Now - start);
            Assert.That(opacity, Is.EqualTo(SceneTimelines.HudReflow(t, false)).Within(Eps));
            rig.RunFor(300);
            Assert.That(opacity, Is.EqualTo(1));
            rig.AssertIdle("terminó");
        }

        [Test]
        public void WithReducedMotionTheHudIsThereAtOnce()
        {
            var rig = new Rig(reduceMotion: true);
            var opacity = -1.0;
            var motion = new HudReflowMotion(rig.Driver, (g, o, _) => { if (g == 0) { opacity = o; } });
            motion.OnOrientationClassChanged();
            Assert.That(opacity, Is.EqualTo(1));
            rig.AssertIdle("reducido: en el acto");

            var other = new Rig();
            var instant = new HudReflowMotion(other.Driver, (g, o, _) => { if (g == 0) { opacity = o; } });
            opacity = -1;
            instant.OnOrientationClassChanged(true);
            Assert.That(opacity, Is.EqualTo(1));
            other.AssertIdle("Instant");
        }

        [Test]
        public void StoppingTheReflowLeavesNothingAnimating()
        {
            var rig = new Rig();
            var applied = 0;
            var motion = new HudReflowMotion(rig.Driver, (_, _, _) => applied++);
            motion.OnOrientationClassChanged();
            rig.RunFor(50);
            motion.Stop();
            var before = applied;
            rig.RunFor(500);
            Assert.That(applied, Is.EqualTo(before));
            rig.AssertIdle("Stop");
        }

        // ── El velo de pausa también sobre la escena (AC-WKT-13-12; DT6a-10): el peso alfa/0,15, en el mismo cuadro ──

        [Test]
        public void ThePauseVeilAlsoWeighsTheScene()
        {
            var rig = new Rig();
            var alpha = -1.0;
            var weight = -1.0;
            var calls = 0;
            var motion = new PauseVeilMotion(rig.Driver, a => alpha = a, w =>
            {
                weight = w;
                calls++;
            });
            motion.SetPaused(false, false);
            Assert.That(weight, Is.EqualTo(0));
            var start = rig.Now;
            motion.SetPaused(true, false);
            for (var k = 1; k <= 20; k++)
            {
                rig.Frame();
                var t = HudTimelines.Ms(rig.Now - start);
                Assert.That(alpha, Is.EqualTo(PauseTimeline.Veil(Math.Min(t, 300), true, 0, false)).Within(Eps), "en " + t);
                Assert.That(weight, Is.EqualTo(SceneTimelines.VeilWeight(alpha)).Within(Eps), "el mismo cuadro, el mismo valor");
                Assert.That(motion.SceneWeight, Is.EqualTo(weight).Within(Eps));
            }

            Assert.That(weight, Is.EqualTo(1), "en pausa, a pleno");
            start = rig.Now;
            motion.SetPaused(false, false);
            rig.RunFor(240);
            Assert.That(weight, Is.EqualTo(0), "vuelve en 240");
            Assert.That(calls, Is.GreaterThan(30));
            rig.AssertIdle("velo quieto");

            // Sin el segundo callback (la 4b y la 5, sin escena) sigue igual.
            var old = new PauseVeilMotion(rig.Driver, a => alpha = a);
            old.SetPaused(true, true);
            Assert.That(alpha, Is.EqualTo(MotionSpecs.PauseVeilAlpha));
            Assert.That(old.SceneWeight, Is.EqualTo(1));
        }

        private sealed class Rig
        {
            private const long FrameTicks = 166667;

            public Rig(bool reduceMotion = false, bool armed = true)
            {
                Settings.ReduceMotion = reduceMotion;
                Driver = new MotionDriver(Clock, Pacing, Settings);
                if (armed)
                {
                    Driver.Tick();
                }
            }

            public FakeScheduler Clock { get; } = new FakeScheduler();

            public FramePacingPolicy Pacing { get; } = new FramePacingPolicy();

            public MotionSettings Settings { get; } = new MotionSettings();

            public MotionDriver Driver { get; }

            public double Now => Clock.MonotonicSeconds;

            public void Frame()
            {
                Clock.Advance(TimeSpan.FromTicks(FrameTicks));
                Driver.Tick();
            }

            public void RunFor(double ms)
            {
                var end = Clock.Elapsed.Ticks + (long)Math.Round(ms * TimeSpan.TicksPerMillisecond);
                while (Clock.Elapsed.Ticks + FrameTicks <= end)
                {
                    Frame();
                }

                if (Clock.Elapsed.Ticks < end)
                {
                    Clock.Advance(TimeSpan.FromTicks(end - Clock.Elapsed.Ticks));
                    Driver.Tick();
                }
            }

            public void AssertIdle(string because)
            {
                Assert.That(Driver.ActiveCount, Is.EqualTo(0), "nada en el MotionDriver: " + because);
                Assert.That(Pacing.Demands, Is.EqualTo(0), "nadie pide 60 fps: " + because);
            }
        }
    }
}
