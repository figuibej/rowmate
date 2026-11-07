using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // EFI · El esfuerzo visual (AC-SCN-08-12; ANIMATION_BIBLE §3.8) y el reposo (AC-SCN-05-10; §3.9).
    [TestFixture]
    public sealed class EffortAndIdleTests
    {
        [TestCase(60, 0.0, Category = "AC-SCN-08-12")]
        [TestCase(300, 1.0, Category = "AC-SCN-08-12")]
        [TestCase(150, 0.375, Category = "AC-SCN-08-12")]
        [TestCase(100, 0.1666666, Category = "AC-SCN-08-12")]
        [TestCase(280, 0.9166666, Category = "AC-SCN-08-12")]
        [TestCase(0, 0.0, Category = "AC-SCN-08-12")]
        [TestCase(900, 1.0, Category = "AC-SCN-08-12")]
        public void TheEffortOfTheWatts(int watts, double effort)
        {
            Assert.That(EffortModel.Effort(watts), Is.EqualTo(effort).Within(1e-6));
        }

        [Test, Category("AC-SCN-08-12")]
        public void TheEffortIsSmoothedWithOneSecond()
        {
            var e = default(EffortFilter);
            for (var i = 0; i < 60; i++)
            {
                e.Step(1.0 / 60, 300);
            }

            Assert.That(e.Value, Is.EqualTo(1 - Math.Exp(-1)).Within(1e-9), "τ 1 s");
        }

        [Test, Category("AC-SCN-05-10")]
        public void TheIdleBreathsAfterOneAndAHalfSecondsStill()
        {
            var idle = new RowerIdle();
            var t = 0.0;
            for (var i = 0; i < 89; i++)
            {
                t += 1.0 / 60;
                idle.Step(1.0 / 60, t, t);
            }

            Assert.That(idle.Weight, Is.EqualTo(0), "antes de 1,5 s quieto");
            for (var i = 0; i < 30; i++)
            {
                t += 1.0 / 60;
                idle.Step(1.0 / 60, t, t);
            }

            Assert.That(idle.Weight, Is.EqualTo(1).Within(1e-9), "entra en 0,4 s");
            var maxChest = 0.0;
            var maxGaze = 0.0;
            for (var i = 0; i < 60 * 20; i++)
            {
                t += 1.0 / 60;
                idle.Step(1.0 / 60, t, t);
                maxChest = Math.Max(maxChest, Math.Abs(idle.Sample.ChestOffsetM));
                maxGaze = Math.Max(maxGaze, Math.Abs(idle.Sample.GazeYawDeg));
            }

            Assert.That(maxChest, Is.EqualTo(0.006).Within(0.0002), "pecho ± 0,6 cm");
            Assert.That(maxGaze, Is.GreaterThan(0.2).And.LessThanOrEqualTo(2.05), "mira de a ratos ± 2°");
            for (var i = 0; i < 12; i++)
            {
                idle.Step(1.0 / 60, 0, t);
            }

            Assert.That(idle.Weight, Is.EqualTo(0).Within(1e-9), "sale en 0,2 s al moverse");
        }

        [Test, Category("AC-SCN-05-10")]
        public void TheIdleNeverTouchesThePhase()
        {
            // En la sesión: la fase del contrato es la de un SceneState solo, con reposo o sin él; en pausa el remero respira desde ≈ 1,8 s.
            var s = new StrokeTestSession();
            var bare = new SceneState();
            for (var i = 0; i < 300; i++)
            {
                s.Step(StrokeTestSession.Rowing(24));
                bare.Update(s.LastDt, 125, 24, true);
            }

            var breathingAt = double.NaN;
            for (var i = 0; i < 300; i++)
            {
                s.Step(StrokeTestSession.Pause());
                bare.Update(s.LastDt, 125, 24, false);
                Assert.That(s.Session.State.StrokePhase, Is.EqualTo(bare.StrokePhase));
                if (double.IsNaN(breathingAt) && s.Rower.IdleWeight > 0)
                {
                    breathingAt = (i + 1) / 60.0;
                }
            }

            Assert.That(breathingAt, Is.EqualTo(1.8).Within(0.3), "≈ 1,5 s después de quedar quieto (VID-04: ≈ 4,8 s con la pausa a los 3)");
        }
    }
}
