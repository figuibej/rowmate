using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // CBT · La cámara suma el bote (AC-SCN-06-03, AC-SCN-02-04; ANIMATION_BIBLE §5.2; DT7-06): 25 % de la arfada, 30 % del surge y
    // 15 % del cabeceo con el tope suave; el horizonte dentro de ± 3 dp en los siete tamaños durante 10 min de escena; pausa × 0,6 y
    // reducir movimiento × 0 (el remero sigue remando).
    [TestFixture]
    public sealed class CameraBoatTermsTests
    {
        private static readonly (string Name, double W, double H)[] Sizes =
        {
            ("V390", 390, 844), ("H844", 844, 390), ("TV", 411, 914), ("P360", 360, 800), ("TH", 914, 411), ("L640", 640, 360), ("W1280", 1280, 720),
        };

        [Test, Category("AC-SCN-06-03"), Category("AC-SCN-02-04")]
        public void TenMinutesInTheSevenSizesKeepTheHorizonWithinThreeDp()
        {
            foreach (var size in Sizes)
            {
                var cam = new SceneCamera(size.W, size.H);
                var filter = default(BoatCameraFilter);
                var max = 0.0;
                const double dt = 1.0 / 30;
                for (var i = 0; i < 600 * 30; i++)
                {
                    var t = i * dt;
                    var phi = (t * 30 / 60) % 1.0;
                    var r = StrokeRhythm.DriveRatio(30);
                    var boat = new BoatMotionSample(BoatMotion.Pitch(phi, t), BoatMotion.Heave(phi), 0,
                        BoatSurge.OffsetMeters(phi, r, BoatSurge.Amplitude(30, 1), 5, 30));
                    filter.Step(dt, boat);
                    var pose = ChaseCameraMotion.AddBoatTerms(ChaseCameraMotion.Breathing(cam, t, 1, BreathingSpec.Lake), cam, filter.Value, 1);
                    max = Math.Max(max, ChaseCameraMotion.HorizonDeviationDp(cam, pose));
                }

                Assert.That(max, Is.LessThanOrEqualTo(3), size.Name);
            }
        }

        [Test, Category("AC-SCN-06-03")]
        public void ThePitchEntersWholeInPortraitAndAlmostZeroInLandscape()
        {
            // DT7-06: el 15 % del cabeceo máximo (0,0039 rad) en TV entra casi entero; en W1280 la respiración ya usa ≈ 2,93 de 3 dp.
            var pitch = 0.15 * 0.026;
            var tv = new SceneCamera(411, 914);
            var w = new SceneCamera(1280, 720);
            var tvFraction = ChaseCameraMotion.SoftPitchRad(CameraPose.Contract(tv), tv, pitch) / pitch;
            Assert.That(tvFraction, Is.GreaterThan(0.6), "TV en reposo de la respiración");
            var worst = WorstBreathing(w);
            var wFraction = ChaseCameraMotion.SoftPitchRad(worst, w, pitch) / pitch;
            Assert.That(wFraction, Is.LessThan(0.1), "W1280 en el peor momento de la respiración");
            Assert.That(ChaseCameraMotion.HorizonDeviationDp(w, worst), Is.LessThan(2.95));
        }

        [Test, Category("AC-SCN-06-03")]
        public void TheTranslationsDoNotMoveTheHorizonAndTheSharesAreRight()
        {
            var cam = new SceneCamera(411, 914);
            var contract = CameraPose.Contract(cam);
            var boat = new BoatMotionSample(0, 0.006, 0, 0.1);
            var pose = ChaseCameraMotion.AddBoatTerms(contract, cam, boat, 1);
            Assert.That(pose.Y - contract.Y, Is.EqualTo(0.25 * 0.006).Within(1e-15));
            Assert.That(pose.Z - contract.Z, Is.EqualTo(0.30 * 0.1).Within(1e-15));
            Assert.That(ChaseCameraMotion.HorizonDeviationDp(cam, pose), Is.EqualTo(0).Within(1e-9), "las traslaciones no lo mueven");
            var paused = ChaseCameraMotion.AddBoatTerms(contract, cam, boat, 0.6);
            Assert.That(paused.Z - contract.Z, Is.EqualTo(0.6 * 0.30 * 0.1).Within(1e-15), "pausa × 0,6");
            var frozen = ChaseCameraMotion.AddBoatTerms(contract, cam, boat, 0);
            Assert.That(frozen.Y == contract.Y && frozen.Z == contract.Z && frozen.PitchDeg == contract.PitchDeg, Is.True, "reducir movimiento × 0");
        }

        [Test, Category("AC-SCN-06-03")]
        public void InTheSessionReduceMotionFreezesTheCameraButTheRowerKeepsRowing()
        {
            var s = new StrokeTestSession { ReduceMotion = true };
            s.Session.Camera.ShowInPlace(0);
            var contract = CameraPose.Contract(new SceneCamera(411, 914));
            var phases = 0.0;
            for (var i = 0; i < 600; i++)
            {
                s.Step(StrokeTestSession.Rowing(24));
                Assert.That(s.Frame.Pose.Y, Is.EqualTo(contract.Y));
                Assert.That(s.Frame.Pose.Z, Is.EqualTo(contract.Z));
                Assert.That(s.Frame.Pose.PitchDeg, Is.EqualTo(contract.PitchDeg));
                phases = Math.Max(phases, s.Rower.Phase);
            }

            Assert.That(phases, Is.GreaterThan(0.9), "el remero rema (D7-09)");

            var moving = new StrokeTestSession();
            moving.Session.Camera.ShowInPlace(0);
            var maxDev = 0.0;
            var moved = false;
            for (var i = 0; i < 1200; i++)
            {
                moving.Step(StrokeTestSession.Rowing(24));
                maxDev = Math.Max(maxDev, moving.Frame.HorizonDeviationDp);
                moved |= Math.Abs(moving.Frame.Pose.Z - contract.Z) > 0.01;
            }

            Assert.That(maxDev, Is.LessThanOrEqualTo(3));
            Assert.That(moved, Is.True, "la cámara sigue el surge");
        }

        private static CameraPose WorstBreathing(in SceneCamera cam)
        {
            var worst = CameraPose.Contract(cam);
            var max = -1.0;
            for (var i = 0; i < 600 * 30; i++)
            {
                var pose = ChaseCameraMotion.Breathing(cam, i / 30.0, 1, BreathingSpec.Lake);
                var d = ChaseCameraMotion.HorizonDeviationDp(cam, pose);
                if (d > max)
                {
                    max = d;
                    worst = pose;
                }
            }

            return worst;
        }
    }
}
