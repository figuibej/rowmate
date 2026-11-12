using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // MRF · Los micro-anillos (#13; AC-6B-16; 00-arte-agua §6.2): cada gota que toca el agua deja un anillo y cada ataque dos por pala,
    // como mucho 24 vivos, sin asignar.
    [TestFixture]
    public sealed class MicroRingFieldTests
    {
        private static ScullingPose CatchPose()
        {
            var pose = ScullingPose.Create();
            var fit = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);
            ScullingKinematics.Evaluate(0.97, 0.6, BoatSpec.Real, RowerSpec.Mannequin, fit, RowerIdleSample.None, ref pose);
            return pose;
        }

        [Test, Category("AC-6B-16")]
        public void TheCatchLeavesTwoRingsPerBlade()
        {
            var spray = new StrokeSpray();
            var rings = new MicroRingField();
            spray.OnBladeEntry(CatchPose(), 100, 0.6, 1);
            rings.Step(1.0 / 60, spray, StrokeEvents.BladeEntry);
            var catches = 0;
            for (var i = 0; i < rings.Count; i++)
            {
                if (rings[i].Kind == 1)
                {
                    catches++;
                }
            }

            Assert.That(catches, Is.EqualTo(2 * MicroRingField.CatchRingsPerBlade), "dos por pala");
            rings.Step(1.0 / 60, spray, StrokeEvents.None);
            var after = 0;
            for (var i = 0; i < rings.Count; i++)
            {
                after += rings[i].Kind == 1 ? 1 : 0;
            }

            Assert.That(after, Is.EqualTo(4), "la corona de antes no vuelve a sumar");
        }

        [Test, Category("AC-6B-16")]
        public void DropsThatTouchTheWaterLeaveRings()
        {
            var spray = new StrokeSpray();
            var rings = new MicroRingField();
            spray.OnBladeEntry(CatchPose(), 100, 0.6, 1);
            var drops = spray.CountOf(0);
            Assert.That(drops, Is.GreaterThan(0));
            var landed = 0;
            for (var f = 0; f < 60; f++)
            {
                var before = spray.CountOf(0);
                rings.Step(1.0 / 60, spray, f == 0 ? StrokeEvents.BladeEntry : StrokeEvents.None);
                spray.Step(1.0 / 60);
                landed += before - spray.CountOf(0);
            }

            var dropRings = 0;
            for (var i = 0; i < rings.Count; i++)
            {
                dropRings += rings[i].Kind == 0 ? 1 : 0;
            }

            Assert.That(dropRings, Is.GreaterThan(0), "las gotas del ataque que caen al agua");
            Assert.That(dropRings, Is.LessThanOrEqualTo(landed), "a lo sumo una por gota");
            Assert.That(rings.Count, Is.LessThanOrEqualTo(MicroRingField.Capacity));
        }

        [Test, Category("AC-6B-16")]
        public void RadiusWidthAlphaAndLife()
        {
            // Gota: radio 0,05 + 0,35·a, ancho 0,02 + 0,03·a, 1,2 s, alfa 0,5·(1 − a/1,2)². Ataque: 0,15 → 0,9 m en 1,0 s.
            Assert.That(MicroRingField.Radius(0, 0), Is.EqualTo(0.05).Within(1e-12));
            Assert.That(MicroRingField.Radius(0, 0.5), Is.EqualTo(0.225).Within(1e-12));
            Assert.That(MicroRingField.Width(0.5), Is.EqualTo(0.035).Within(1e-12));
            Assert.That(MicroRingField.Alpha(0, 0), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(MicroRingField.Alpha(0, 0.6), Is.EqualTo(0.125).Within(1e-12));
            Assert.That(MicroRingField.Alpha(0, 1.2), Is.EqualTo(0));
            Assert.That(MicroRingField.Radius(1, 0), Is.EqualTo(0.15).Within(1e-12));
            Assert.That(MicroRingField.Radius(1, 1.0), Is.EqualTo(0.9).Within(1e-12));
            Assert.That(MicroRingField.Radius(1, -0.1), Is.EqualTo(0), "el segundo anillo del ataque todavía no nació");
            Assert.That(MicroRingField.LifeOf(0), Is.EqualTo(WaterLookArt.DropRingLife));

            var spray = new StrokeSpray();
            var rings = new MicroRingField();
            spray.OnBladeEntry(CatchPose(), 100, 0.6, 1);
            rings.Step(1.0 / 60, spray, StrokeEvents.BladeEntry);
            for (var f = 0; f < 120; f++)
            {
                rings.Step(1.0 / 60, spray, StrokeEvents.None);
            }

            var catches = 0;
            for (var i = 0; i < rings.Count; i++)
            {
                catches += rings[i].Kind == 1 ? 1 : 0;
            }

            Assert.That(catches, Is.EqualTo(0), "a los 2 s los del ataque ya vencieron");
        }

        [Test, Category("AC-6B-16")]
        public void AtMostTwentyFour()
        {
            var spray = new StrokeSpray();
            var rings = new MicroRingField();
            for (var stroke = 0; stroke < 10; stroke++)
            {
                spray.OnBladeEntry(CatchPose(), 100 + stroke, 1, 1);
                rings.Step(1.0 / 60, spray, StrokeEvents.BladeEntry);
            }

            Assert.That(rings.Count, Is.EqualTo(MicroRingField.Capacity), "se pisa el más viejo");
            rings.Clear();
            Assert.That(rings.Count, Is.EqualTo(0));
        }
    }
}
