using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // BMO · El cabeceo, la arfada, el rolido y el surge del bote (AC-SCN-06-01, -02, AC-SCN-08-12; ANIMATION_BIBLE §4.5, §4.6).
    [TestFixture]
    public sealed class BoatMotionTests
    {
        [Test, Category("AC-SCN-06-01")]
        public void ThePitchOfTheLegacyOnTheVisualPhase()
        {
            Assert.That(BoatMotion.Pitch(0.25, 0), Is.EqualTo(0.02).Within(1e-15), "máximo en φ = 0,25 (± 1,15°)");
            Assert.That(BoatMotion.Pitch(0.25, 0) * 180 / Math.PI, Is.EqualTo(1.15).Within(0.01));
            Assert.That(BoatMotion.Pitch(0.6, 3.7), Is.EqualTo(0.02 * Math.Sin(0.6 * 2 * Math.PI) + 0.006 * Math.Sin(3.7 * 0.8)));
        }

        [Test, Category("AC-SCN-06-02")]
        public void HeaveAndRoll()
        {
            Assert.That(BoatMotion.Heave(0.08), Is.EqualTo(0).Within(1e-15));
            Assert.That(BoatMotion.Heave(0.33), Is.EqualTo(0.006).Within(1e-12));
            Assert.That(BoatMotion.Roll(2, BoatMotion.LakeRollK), Is.EqualTo(0.004 * Math.Sin(0.9 * 2 + 1.3) * 0.5));
            Assert.That(BoatMotion.LakeRollK, Is.EqualTo(0.5));
        }

        [TestCase(0.22, Category = "AC-SCN-06-02")]
        [TestCase(0.337, Category = "AC-SCN-06-02")]
        [TestCase(0.409, Category = "AC-SCN-06-02")]
        [TestCase(0.48, Category = "AC-SCN-06-02")]
        public void TheSurgeHasMeanZeroAndZeroOffsetAtCatchAndFinish(double r)
        {
            const int n = 200000;
            var sum = 0.0;
            for (var i = 0; i < n; i++)
            {
                sum += BoatSurge.SpeedFactor((i + 0.5) / n, r);
            }

            Assert.That(sum / n, Is.EqualTo(0).Within(1e-6), "media exacta 0 en el ciclo");
            Assert.That(BoatSurge.SpeedFactor(0, r), Is.EqualTo(-1), "mínimo en el catch");
            Assert.That(BoatSurge.SpeedFactor(r, r), Is.EqualTo(1), "máximo en el finish");
            Assert.That(BoatSurge.OffsetMeters(0, r, 0.07, 4, 24), Is.EqualTo(0).Within(1e-15));
            Assert.That(BoatSurge.OffsetMeters(r, r, 0.07, 4, 24), Is.EqualTo(0).Within(1e-12));
            Assert.That(BoatSurge.OffsetMeters(0.999999, r, 0.07, 4, 24), Is.EqualTo(0).Within(1e-6));

            // La derivada del desfase es v̄·A·g (integral analítica, sin acumular).
            var dphi = 1e-6;
            var period = 60.0 / 24;
            foreach (var phi in new[] { 0.1 * r, 0.5 * r, r + 0.3 * (1 - r), r + 0.8 * (1 - r) })
            {
                var slope = (BoatSurge.OffsetMeters(phi + dphi, r, 0.07, 4, 24) - BoatSurge.OffsetMeters(phi - dphi, r, 0.07, 4, 24)) / (2 * dphi * period);
                Assert.That(slope, Is.EqualTo(4 * 0.07 * BoatSurge.SpeedFactor(phi, r)).Within(1e-5));
            }
        }

        // ANIMATION_BIBLE §4.5 con e = 0,5 (QA7-G-06): SPM, split (s), atrás en el drive y adelante en la recuperación (cm).
        [TestCase(18, 140, 9.0, 19.6, Category = "AC-SCN-06-02")]
        [TestCase(24, 120, 8.6, 14.5, Category = "AC-SCN-06-02")]
        [TestCase(28, 110, 8.1, 12.2, Category = "AC-SCN-06-02")]
        [TestCase(36, 100, 6.5, 8.9, Category = "AC-SCN-06-02")]
        public void TheSurgeTable(double spm, double split, double back, double forward)
        {
            var r = StrokeRhythm.DriveRatio(spm);
            var a = BoatSurge.Amplitude(spm, 0.5);
            var min = 0.0;
            var max = 0.0;
            for (var i = 0; i <= 10000; i++)
            {
                var o = BoatSurge.OffsetMeters(i / 10000.0, r, a, 500 / split, spm);
                min = Math.Min(min, o);
                max = Math.Max(max, o);
            }

            Assert.That(-min * 100, Is.EqualTo(back).Within(0.2));
            Assert.That(max * 100, Is.EqualTo(forward).Within(0.2));
        }

        [Test, Category("AC-SCN-06-02"), Category("AC-SCN-08-12")]
        public void TheAmplitudeFollowsSpmAndEffort()
        {
            Assert.That(BoatSurge.Amplitude(20, 0.5), Is.EqualTo(0.075).Within(1e-12));
            Assert.That(BoatSurge.Amplitude(5, 0.5), Is.EqualTo(0.08).Within(1e-12));
            Assert.That(BoatSurge.Amplitude(60, 0.5), Is.EqualTo(0.05).Within(1e-12));
            Assert.That(BoatSurge.Amplitude(30, 0.917), Is.GreaterThan(BoatSurge.Amplitude(30, 0.167)), "Fuerte contra Suave");
        }

        [Test, Category("AC-SCN-06-01"), Category("AC-SCN-06-02")]
        public void InPauseTheStrokePartStopsTheSlowSwaySurvivesAndTheSurgeFades()
        {
            var s = new StrokeTestSession();
            s.Run(6, StrokeTestSession.Rowing(24));
            s.Run(1, StrokeTestSession.Pause());
            var phi = s.Rower.Phase;
            var strokePart = s.Rower.Boat.PitchRad - 0.006 * Math.Sin(s.Frame.SceneTime * 0.8);
            s.Run(2, StrokeTestSession.Pause());
            Assert.That(s.Rower.Phase, Is.EqualTo(phi), "φ quieta en pausa");
            Assert.That(s.Rower.Boat.PitchRad - 0.006 * Math.Sin(s.Frame.SceneTime * 0.8), Is.EqualTo(strokePart).Within(1e-12),
                "la parte de la palada queda fija; el balanceo lento sigue");
            s.Run(12, StrokeTestSession.Pause());
            Assert.That(s.Session.State.Speed, Is.EqualTo(0));
            Assert.That(s.Rower.Boat.SurgeM, Is.EqualTo(0), "el surge escala con la velocidad media y se apaga con ella");
        }

        [Test, Category("AC-SCN-06-02"), Category("AC-SCN-08-11")]
        public void TheSurgeDoesNotTouchTheSceneDistance()
        {
            var s = new StrokeTestSession();
            var bare = new SceneState();
            for (var i = 0; i < 600; i++)
            {
                s.Step(StrokeTestSession.Rowing(24));
                bare.Update(s.LastDt, 125, 24, true);
                Assert.That(s.Frame.Distance, Is.EqualTo(bare.Distance));
                Assert.That(s.Rower.BoatWorldZ, Is.EqualTo(bare.Distance + SceneCamera.BoatZ + s.Rower.Boat.SurgeM).Within(1e-9));
            }
        }
    }
}
