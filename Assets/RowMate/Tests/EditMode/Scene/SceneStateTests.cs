using System;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // SST · Velocidad, distancia y tiempo de la escena (scene/scene_state.dart:13-57; F-SCN-03; LT-66, LT-67, LT-68).
    [TestFixture]
    public sealed class SceneStateTests
    {
        // Como el run() de scene_state_test.dart:5-13: pasos de 0,05 s.
        private static void Run(SceneState s, double seconds, double pace = 107, double spm = 24, bool active = true)
        {
            var steps = (int)Math.Floor(seconds / 0.05 + 0.5);
            for (var i = 0; i < steps; i++)
            {
                s.Update(0.05, pace, spm, active);
            }
        }

        private static void RunFrames(SceneState s, double seconds, double pace, double spm, bool active)
        {
            var frames = (int)Math.Floor(seconds * 60 + 0.5);
            for (var i = 0; i < frames; i++)
            {
                s.Update(1.0 / 60, pace, spm, active);
            }
        }

        // legacy: test/scene/scene_state_test.dart:14 'la velocidad sale del split y se alcanza en unos segundos'
        [TestCase(TestName = "la velocidad sale del split y se alcanza en unos segundos", Category = "LT-66,AC-SCN-03-01,AC-SCN-03-02")]
        public void SpeedComesFromTheSplitAndIsReachedInSeconds()
        {
            var s = new SceneState();
            Assert.That(SceneState.TargetSpeedFor(107, 24, true), Is.EqualTo(4.67).Within(0.01));
            Assert.That(SceneState.TargetSpeedFor(107, 24, false), Is.EqualTo(0));
            Assert.That(SceneState.TargetSpeedFor(0, 24, true), Is.EqualTo(0));
            Assert.That(SceneState.TargetSpeedFor(107, 0, true), Is.EqualTo(0), "sin paladas no hay avance");
            Run(s, 8);
            Assert.That(s.Speed, Is.EqualTo(4.67).Within(0.05));
            Assert.That(s.Distance, Is.GreaterThan(25));
        }

        // legacy: test/scene/scene_state_test.dart:26 'al dejar de remar desacelera hasta detenerse'
        [TestCase(TestName = "al dejar de remar desacelera hasta detenerse", Category = "LT-67,AC-SCN-03-03")]
        public void StoppingDeceleratesToRest()
        {
            var s = new SceneState();
            Run(s, 8);
            var d = s.Distance;
            Run(s, 6, pace: 0);
            Assert.That(s.Speed, Is.LessThan(0.1));
            Assert.That(s.Distance, Is.GreaterThan(d));
            Run(s, 10, pace: 0);
            Assert.That(s.Speed, Is.EqualTo(0));
        }

        // legacy: test/scene/scene_state_test.dart:55 'shaderDistance se acota a 4096 m'
        [TestCase(TestName = "shaderDistance se acota a 4096 m", Category = "LT-68,AC-SCN-03-06")]
        public void ShaderDistanceWrapsAt4096()
        {
            var s = new SceneState();
            s.ForceForTest(5000, 0, 0);
            Assert.That(s.ShaderDistance, Is.EqualTo(904));
        }

        [TestCase(107.0, 4.672897196261682, Category = "AC-SCN-03-01")]
        [TestCase(125.0, 4.0, Category = "AC-SCN-03-01")]
        [TestCase(120.0, 4.166666666666667, Category = "AC-SCN-03-01")]
        public void TargetIsFiveHundredOverTheSplit(double pace, double speed)
        {
            Assert.That(SceneState.TargetSpeedFor(pace, 24, true), Is.EqualTo(speed).Within(1e-12));
        }

        [Test, Category("AC-SCN-03-02"), Category("AC-SCN-03-05")]
        public void R4AfterSixtySecondsHasRowedAbout234Meters()
        {
            // R-4 (split 125 s, 24 spm) desde el inicio, a 60 fps: ≈ 234 m mientras el monitor sumó 240.
            var s = new SceneState();
            RunFrames(s, 60, 125, 24, true);
            Assert.That(s.Distance, Is.EqualTo(234).Within(0.5));
            Assert.That(s.Speed, Is.EqualTo(4.0).Within(1e-6));
            Assert.That(s.Time, Is.EqualTo(60).Within(1e-9));
        }

        [Test, Category("AC-SCN-03-03")]
        public void PauseAtSixtyGlidesAboutSixMetersAndStopsAtAboutNineSeconds()
        {
            var s = new SceneState();
            RunFrames(s, 60.2, 125, 24, true);
            var d0 = s.Distance;
            RunFrames(s, 2, 125, 24, false);
            Assert.That(s.Speed, Is.EqualTo(1.05).Within(0.03), "a los 2 s va a ≈ 1,05 m/s");
            var stopped = double.NaN;
            for (var i = 1; i <= 20 * 60; i++)
            {
                s.Update(1.0 / 60, 125, 24, false);
                if (s.Speed == 0 && double.IsNaN(stopped))
                {
                    stopped = 2 + i / 60.0;
                }
            }

            Assert.That(stopped, Is.EqualTo(9).Within(0.5), "queda quieto a los ≈ 9 s");
            Assert.That(s.Distance - d0, Is.EqualTo(6).Within(0.2), "se desliza ≈ 6 m");
        }

        [Test, Category("AC-SCN-03-03")]
        public void HalfASecondRowingThenTwoPausedIsUnderHalfAMeterPerSecond()
        {
            // La parte de velocidad de scene_state_test.dart:60 (el caso entero es de la It. 7).
            var s = new SceneState();
            Run(s, 0.5);
            Run(s, 2, active: false);
            Assert.That(s.Speed, Is.LessThan(0.5));
        }

        [Test, Category("AC-SCN-03-03")]
        public void ResumingAcceleratesWithTheSameInertia()
        {
            var s = new SceneState();
            RunFrames(s, 30, 125, 24, false);
            Assert.That(s.Speed, Is.EqualTo(0));
            RunFrames(s, 1.5, 125, 24, true);
            Assert.That(s.Speed, Is.EqualTo(4 * (1 - Math.Exp(-1))).Within(0.03), "τ = 1,5 s");
        }

        [TestCase(0.5, 0.1, Category = "AC-SCN-03-04")]
        [TestCase(60.0, 0.1, Category = "AC-SCN-03-04")]
        [TestCase(0.016, 0.016, Category = "AC-SCN-03-04")]
        [TestCase(-1.0, 0.0, Category = "AC-SCN-03-04")]
        public void TheStepIsClampedToATenthOfASecond(double dt, double expected)
        {
            var s = new SceneState();
            s.Update(dt, 125, 24, true);
            Assert.That(s.Time, Is.EqualTo(expected).Within(1e-12), "un cuadro lento no hace saltar la escena");
        }

        [Test, Category("AC-SCN-03-04")]
        public void BelowTenFpsTheSceneRunsSlowerThanTheClock()
        {
            var s = new SceneState();
            for (var i = 0; i < 10; i++)
            {
                s.Update(0.25, 125, 24, true);
            }

            Assert.That(s.Time, Is.EqualTo(1.0).Within(1e-12), "2,5 s de reloj: 1 s de escena");
        }

        [Test, Category("AC-SCN-03-06")]
        public void ShaderDistanceIsOnlyAContract()
        {
            var s = new SceneState();
            s.ForceForTest(42195, 0, 0);
            Assert.That(s.ShaderDistance, Is.EqualTo(42195 % 4096.0));
            Assert.That(s.Distance, Is.EqualTo(42195), "la distancia de escena sigue en double, sin acotar");
            s.Reset();
            Assert.That(s.Distance, Is.EqualTo(0));
            Assert.That(s.Speed, Is.EqualTo(0));
            Assert.That(s.Time, Is.EqualTo(0));
        }
    }
}
