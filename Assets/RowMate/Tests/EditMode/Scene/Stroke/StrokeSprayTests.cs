using System;
using NUnit.Framework;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SPR · La salpicadura del ataque y las gotas de la recuperación (AC-SCN-08-04, -05, -12; ANIMATION_BIBLE §4.1, §4.2; DT7-05).
    [TestFixture]
    public sealed class StrokeSprayTests
    {
        private static ScullingPose EntryPose()
        {
            var pose = ScullingPose.Create();
            ScullingKinematics.Evaluate(0.995, 0, BoatSpec.Real, RowerSpec.Mannequin, RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin),
                RowerIdleSample.None, ref pose);
            return pose;
        }

        [TestCase(0.0, 1.0, 8, Category = "AC-SCN-08-04")]
        [TestCase(1.0, 1.0, 22, Category = "AC-SCN-08-04,AC-SCN-08-12")]
        [TestCase(0.375, 1.0, 13, Category = "AC-SCN-08-04")]
        [TestCase(0.0, 0.5, 6, Category = "AC-SCN-08-04")]
        public void DropsPerBladeAtLeastSix(double effort, double scale, int drops)
        {
            Assert.That(StrokeSpray.DropsPerBlade(effort, scale), Is.EqualTo(drops));
            var spray = new StrokeSpray();
            spray.OnBladeEntry(EntryPose(), 100, effort, scale);
            var left = 0;
            var right = 0;
            for (var i = 0; i < spray.Count; i++)
            {
                if (spray.Items[i].Kind == 0)
                {
                    if (spray.Items[i].X > 0)
                    {
                        left++;
                    }
                    else
                    {
                        right++;
                    }
                }
            }

            Assert.That(left, Is.EqualTo(drops));
            Assert.That(right, Is.EqualTo(drops));
            Assert.That(spray.CountOf(1), Is.EqualTo(2), "una corona por pala");
        }

        [TestCase(0.0, Category = "AC-SCN-08-04")]
        [TestCase(1.0, Category = "AC-SCN-08-04,AC-SCN-08-12")]
        public void TheyRiseFallAndStayUnderTheMaxHeightTowardTheBow(double effort)
        {
            var spray = new StrokeSpray();
            spray.OnBladeEntry(EntryPose(), 100, effort, 1);
            var meanVz = 0.0;
            for (var i = 0; i < spray.Count; i++)
            {
                var p = spray.Items[i];
                if (p.Kind == 0)
                {
                    Assert.That(p.Vy * p.Vy / (2 * StrokeSpray.Gravity), Is.LessThanOrEqualTo(StrokeSpray.CrownHeight(effort) + 1e-9), "altura máx.");
                    meanVz += p.Vz;
                    Assert.That(p.Size, Is.InRange(0.02, 0.05));
                }
            }

            Assert.That(meanVz, Is.GreaterThan(0), "hacia proa (en promedio: el cono de 35° abre alrededor de 0,6 m/s)");
            var steps = 0;
            while (spray.Count > 0 && steps < 120)
            {
                spray.Step(1.0 / 60);
                steps++;
            }

            Assert.That(spray.Count, Is.EqualTo(0), "suben, caen y se apagan");
            Assert.That(steps, Is.LessThanOrEqualTo(40), "en ≤ 0,6 s");
        }

        [TestCase(18, Category = "AC-SCN-08-04")]
        [TestCase(24, Category = "AC-SCN-08-04")]
        [TestCase(36, Category = "AC-SCN-08-04")]
        public void VisibleWhileCIsUnderEightHundredths(double spm)
        {
            var s = new StrokeTestSession();
            s.Run(4, StrokeTestSession.Rowing(spm, 60));
            while ((s.Rower.Events & StrokeEvents.BladeEntry) == 0)
            {
                s.Step(StrokeTestSession.Rowing(spm, 60));
            }

            while (s.Rower.Canonical > 0.5 || s.Rower.Canonical < 0.079)
            {
                Assert.That(s.Rower.Spray.CountOf(0), Is.GreaterThanOrEqualTo(2 * StrokeSpray.MinDropsPerBlade - 2), "c " + s.Rower.Canonical);
                s.Step(StrokeTestSession.Rowing(spm, 60));
            }

            Assert.That(s.Rower.Spray.CountOf(0), Is.GreaterThan(0), "todavía hay gotas en c 0,08");
        }

        [Test, Category("AC-SCN-08-05")]
        public void DripsFromTheExitToFiftyFiveHundredths()
        {
            var s = new StrokeTestSession();
            s.Run(4, StrokeTestSession.Rowing(20));
            var seen = false;
            for (var i = 0; i < 300; i++)
            {
                var before = s.Rower.Spray.CountOf(2);
                var c = s.Rower.Canonical;
                s.Step(StrokeTestSession.Rowing(20));
                var born = s.Rower.Spray.CountOf(2) > before;
                if (born)
                {
                    seen = true;
                    Assert.That(s.Rower.Canonical, Is.InRange(0.381, 0.56), "solo entre la salida y c 0,55");
                }
            }

            Assert.That(seen, Is.True);
        }

        [Test, Category("AC-SCN-08-04"), Category("AC-SCN-08-05")]
        public void NeverInPauseAndDeterministic()
        {
            var a = new StrokeTestSession();
            var b = new StrokeTestSession();
            for (var i = 0; i < 400; i++)
            {
                a.Step(StrokeTestSession.Rowing(30, 280));
                b.Step(StrokeTestSession.Rowing(30, 280));
                Assert.That(a.Rower.Spray.Count, Is.EqualTo(b.Rower.Spray.Count));
                for (var k = 0; k < a.Rower.Spray.Count; k++)
                {
                    Assert.That(a.Rower.Spray.Items[k].X, Is.EqualTo(b.Rower.Spray.Items[k].X), "dos corridas, las mismas gotas");
                    Assert.That(a.Rower.Spray.Items[k].Vy, Is.EqualTo(b.Rower.Spray.Items[k].Vy));
                }
            }

            a.Run(1.5, StrokeTestSession.Pause());
            for (var i = 0; i < 300; i++)
            {
                a.Step(StrokeTestSession.Pause());
                Assert.That(a.Rower.Spray.Count, Is.EqualTo(0), "nunca nacen en pausa");
            }
        }
    }
}
