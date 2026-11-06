using System;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // LAK · El Lago alpino (scene/environments/lake_environment.dart:1-40; F-SCN-15; AC-SCN-15-01 a -04): pinos en las dos orillas
    // con las tiradas en el orden del legacy, muelles cada 400 m, cabañas cada 600 m, el oleaje y el tope que nunca actúa.
    [TestFixture]
    public sealed class LakeEnvironmentTests
    {
        [Test, Category("AC-SCN-15-01")]
        public void CalmWaterThatIsNotTinted()
        {
            var lake = new LakeEnvironment();
            Assert.That(lake.Id, Is.EqualTo(EnvironmentId.Lake));
            Assert.That(lake.WaveAmplitude, Is.EqualTo(0.25));
            Assert.That(lake.WaveScale, Is.EqualTo(2.5));
            var c = SceneColor.FromHex("#0E4F8F");
            Assert.That(lake.TintWater(c), Is.EqualTo(c));
            Assert.That(lake.MaxVisibleProps, Is.EqualTo(120));
        }

        [Test, Category("AC-SCN-15-02")]
        public void PinesFollowTheLegacyDrawOrder()
        {
            // Segmento 0 (00-deuda-diseno §6.1): izquierda (−15,899; 0,565; 0,760; 992), (−12,258; 9,748; 1,265; 197),
            // (−11,767; 19,441; 0,784; 234); derecha (12,552; 0,720; 1,001; 329), (13,236; 12,068; 0,706; 340).
            var props = new ShoreSegments(new LakeEnvironment()).PropsForSegment(0).ToArray();
            Assert.That(props[0].X, Is.EqualTo(-15.899).Within(0.0005));
            Assert.That(props[0].Z, Is.EqualTo(0.565).Within(0.0005));
            Assert.That(props[0].Scale, Is.EqualTo(0.760).Within(0.0005));
            Assert.That(props[0].Seed, Is.EqualTo(992));
            Assert.That(props[1].X, Is.EqualTo(-12.258).Within(0.0005));
            Assert.That(props[1].Z, Is.EqualTo(9.748).Within(0.0005));
            Assert.That(props[1].Seed, Is.EqualTo(197));
            Assert.That(props[2].Seed, Is.EqualTo(234));
            var firstRight = Array.FindIndex(props, p => p.X > 0);
            Assert.That(props[firstRight].X, Is.EqualTo(12.552).Within(0.0005));
            Assert.That(props[firstRight].Z, Is.EqualTo(0.720).Within(0.0005));
            Assert.That(props[firstRight].Seed, Is.EqualTo(329));
            Assert.That(props[firstRight + 1].Seed, Is.EqualTo(340));
        }

        [Test, Category("AC-SCN-15-02")]
        public void PinesStayInTheirRanges()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            var min = int.MaxValue;
            var max = 0;
            for (var k = 0; k <= 2000; k++)
            {
                var props = segments.PropsForSegment(k);
                var z0 = k * ShoreEnvironment.SegmentLength;
                var pines = 0;
                var lastSide = 0;
                var lastZ = double.MinValue;
                foreach (var p in props)
                {
                    if (p.Kind != PropKind.Pine)
                    {
                        continue;
                    }

                    pines++;
                    var side = Math.Sign(p.X);
                    Assert.That(side, Is.GreaterThanOrEqualTo(lastSide == 0 ? -1 : lastSide), "primero la izquierda y después la derecha");
                    if (side != lastSide)
                    {
                        Assert.That(p.Z - z0, Is.InRange(0.0, 6.0), "el primero en z₀ + u·6");
                        lastZ = p.Z;
                    }
                    else
                    {
                        Assert.That(p.Z - lastZ, Is.InRange(6.0, 12.0), "cada 6 + u·6 m");
                        lastZ = p.Z;
                    }

                    lastSide = side;
                    Assert.That(p.Z, Is.LessThan(z0 + 50));
                    Assert.That(Math.Abs(p.X), Is.InRange(10.0, 16.0));
                    Assert.That(p.Scale, Is.InRange(0.7, 1.3));
                    Assert.That(p.Seed, Is.InRange(0, 999));
                }

                Assert.That(pines, Is.GreaterThan(0), "todos los segmentos tienen pinos");
                min = Math.Min(min, pines);
                max = Math.Max(max, pines);
            }

            Assert.That(min, Is.GreaterThanOrEqualTo(8));
            Assert.That(max, Is.LessThanOrEqualTo(new LakeEnvironment().MaxPropsPerSegment - 2), "el pool alcanza (los pinos, el muelle y la cabaña)");
        }

        [Test, Category("AC-SCN-15-03")]
        public void PiersAndCabinsAtTheExactMeters()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            var piers = new System.Collections.Generic.List<double>();
            var cabins = new System.Collections.Generic.List<double>();
            for (var k = 0; k < 40; k++)
            {
                foreach (var p in segments.PropsForSegment(k))
                {
                    if (p.Kind == PropKind.Pier)
                    {
                        Assert.That(p.X, Is.EqualTo(-9.0));
                        Assert.That(k % 8, Is.EqualTo(3));
                        piers.Add(p.Z);
                    }

                    if (p.Kind == PropKind.Cabin)
                    {
                        Assert.That(p.X, Is.EqualTo(13.0));
                        Assert.That(p.Seed, Is.EqualTo(k), "variante k");
                        Assert.That(k % 12, Is.EqualTo(7));
                        cabins.Add(p.Z);
                    }
                }
            }

            Assert.That(piers, Is.EqualTo(new[] { 175.0, 575.0, 975.0, 1375.0, 1775.0 }));
            Assert.That(cabins, Is.EqualTo(new[] { 360.0, 960.0, 1560.0 }));
        }

        [Test, Category("AC-SCN-15-04")]
        public void TheCapOfTheLakeNeverActs()
        {
            var env = new LakeEnvironment();
            var segments = new ShoreSegments(env);
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            Assert.That(segments.VisibleProps(0, into), Is.EqualTo(92), "90 pinos, el muelle y la cabaña");
            Assert.That(segments.VisibleProps(1234, into), Is.EqualTo(95));
            var max = 0;
            for (var d = 0.0; d <= 100000; d += 10)
            {
                max = Math.Max(max, segments.VisibleProps(d, into));
            }

            Assert.That(max, Is.LessThanOrEqualTo(102), "el máximo en 100 km");
            Assert.That(max, Is.LessThan(env.MaxVisibleProps));
        }

        [Test, Category("AC-SCN-14-01")]
        public void SeedsAreTheLegacyFormula()
        {
            Assert.That(ShoreEnvironment.SeedFor(EnvironmentId.Lake, 0), Is.EqualTo(17L));
            Assert.That(ShoreEnvironment.SeedFor(EnvironmentId.Lake, 2000), Is.EqualTo(2000L * 7919 + 17));
            Assert.That(ShoreEnvironment.SeedFor(EnvironmentId.Regatta, 5), Is.EqualTo(3L * 1000003 + 5 * 7919 + 17));
            Assert.That((int)EnvironmentId.Lake, Is.EqualTo(0));
            Assert.That((int)PropKind.LaneBuoy, Is.EqualTo(6), "el orden de environment.dart:12-15");
            Assert.That((int)PropKind.DistanceMarker, Is.EqualTo(14));
        }
    }
}
