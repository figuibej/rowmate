using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;
using RowMate.Core.Scene.Water;

namespace RowMate.Tests.EditMode.Scene.Water
{
    // CFM · La espuma de las crestas de la costa (#31; AC-6B-19; DT6b-06): solo arriba de la ola, ≤ 35 %, apagada de 15 a 150 m.
    [TestFixture]
    public sealed class CrestFoamTests
    {
        [Test, Category("AC-6B-19")]
        public void OnlyOnTheCrestsAndNeverMoreThanThirtyFivePercent()
        {
            var max = 0.0;
            for (var h = -1.0; h <= 1.0; h += 0.01)
            {
                for (var n = 0.0; n <= 1.0; n += 0.05)
                {
                    for (var z = 0.0; z <= 200; z += 5)
                    {
                        var foam = CrestFoam.Amount(h, n, z);
                        Assert.That(foam, Is.InRange(0.0, 0.35));
                        max = Math.Max(max, foam);
                        if (h <= WaterLookArt.CrestFoamHeightFrom)
                        {
                            Assert.That(foam, Is.EqualTo(0), "en el valle y a media ola, nada");
                        }

                        if (z >= WaterLookArt.CrestFoamFar)
                        {
                            Assert.That(foam, Is.EqualTo(0), "lejos la reemplaza el aclarado subpíxel");
                        }
                    }
                }
            }

            Assert.That(max, Is.EqualTo(0.35).Within(1e-9), "en la cresta, con el ruido, de cerca: el tope");
        }
    }

    // CBS · La boya de la costa sobre el mar de fondo (AC-6B-19; DT6b-15, DT6b-21): la misma ola que el casco, 0,15·h.
    [TestFixture]
    public sealed class CoastBuoySwellTests
    {
        [Test, Category("AC-6B-19")]
        public void TheBuoyRidesTheSameWaveAsTheHull()
        {
            var coast = WaterProfiles.For(EnvironmentId.Coast);
            var lake = WaterProfiles.For(EnvironmentId.Lake);
            for (var d = 0.0; d < 2000; d += 37.3)
            {
                var ph = default(WaterPhaseSet);
                WavePhases.Compute(d, d / 4, coast, ref ph);
                for (var relZ = 5.0; relZ <= 100; relZ += 5)
                {
                    var heave = CoastBuoySwell.Heave(BuoyTracker.X, relZ, ph, coast);
                    Assert.That(Math.Abs(heave), Is.LessThanOrEqualTo(CoastSwell.HullScale + 1e-12), "± 0,15 m");
                    var water = CoastSwell.HullScale * CoastSwell.Height(BuoyTracker.X, relZ + d, d / 4, coast.Scale);
                    Assert.That(heave, Is.EqualTo(water).Within(1e-6), "sobre el agua dibujada: ni hundida ni en el aire");
                }

                // El casco toma la misma ola en el centro (CoastSwell.SampleHull, x 0): en fase.
                CoastSwell.SampleHull(d, d / 4, coast.Scale, out _, out _);
                var center = CoastBuoySwell.Heave(0, CoastSwell.CenterZ, ph, coast);
                Assert.That(center, Is.EqualTo(CoastSwell.HullScale * CoastSwell.HeightAt(0, CoastSwell.CenterZ, coast.Scale, ph.Phase1, ph.Phase2)).Within(1e-12));
                Assert.That(CoastBuoySwell.Heave(BuoyTracker.X, 30, ph, lake), Is.EqualTo(0), "fuera de la costa, quieta");
            }

            Assert.That(CoastBuoySwell.SwellFade(400), Is.EqualTo(0), "B0 se apaga a 300 m");
        }
    }

    // SWO · El par de remolinos con el eje de la pala en el finish (#12; AC-6B-17): ≈ 40° respecto de x.
    [TestFixture]
    public sealed class SwirlOrientationTests
    {
        [Test, Category("AC-6B-17")]
        public void TheSwirlsFollowTheBladeAtTheFinish()
        {
            var fit = RiggingFit.Solve(BoatSpec.Real, RowerSpec.Mannequin);
            foreach (var effort in new[] { 0.0, 0.6, 1.0 })
            {
                var pose = ScullingPose.Create();
                ScullingKinematics.Evaluate(0.36, effort, BoatSpec.Real, RowerSpec.Mannequin, fit, RowerIdleSample.None, ref pose);
                Assert.That(SwirlOrientation.AngleDeg(pose), Is.EqualTo(WaterLookArt.SwirlAngleDeg).Within(WaterLookArt.SwirlToleranceDeg),
                    "esfuerzo " + effort);
            }
        }
    }
}
