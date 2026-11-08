using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // Los tres generadores nuevos con los casos del legacy tal cual (environment_test.dart; LT-99 a LT-110) y los metros de cada
    // hito (AC-SCN-16-01 a -03, -17-02, -17-03, -18-01 a -03). La paridad bit a bit está en EnvironmentGoldenTests.
    [TestFixture]
    public sealed class RiverEnvironmentTests
    {
        // legacy: test/scene/environment_test.dart:10 'EnvironmentId.river: propsForSegment es determinista'
        [TestCase(TestName = "EnvironmentId.river: propsForSegment es determinista", Category = "LT-99,AC-SCN-14-08")]
        public void Deterministic() => Cases.Deterministic(EnvironmentId.River);

        // legacy: test/scene/environment_test.dart:17 'EnvironmentId.river: cada segmento tiene objetos y quedan dentro del segmento'
        [TestCase(TestName = "EnvironmentId.river: cada segmento tiene objetos y quedan dentro del segmento", Category = "LT-100,AC-SCN-14-08")]
        public void InsideTheirSegment() => Cases.InsideTheirSegment(EnvironmentId.River);

        // legacy: test/scene/environment_test.dart:30 'EnvironmentId.river: visibleProps ordena de lejos a cerca y respeta el máximo'
        [TestCase(TestName = "EnvironmentId.river: visibleProps ordena de lejos a cerca y respeta el máximo", Category = "LT-101,AC-SCN-14-08")]
        public void VisibleSortedAndCapped() => Cases.VisibleSortedAndCapped(EnvironmentId.River);

        // legacy: test/scene/environment_test.dart:60 'el río urbano tiene un puente cada 800 m'
        [TestCase(TestName = "el río urbano tiene un puente cada 800 m", Category = "LT-110,AC-SCN-16-03")]
        public void BridgeEvery800m()
        {
            var segments = new ShoreSegments(new RiverEnvironment());
            var withBridge = Enumerable.Range(0, 32).Where(k => segments.PropsForSegment(k).ToArray().Any(p => p.Kind == PropKind.Bridge)).ToList();
            Assert.That(withBridge, Is.EqualTo(new[] { 5, 21 }));
        }

        [Test, Category("AC-SCN-16-03")]
        public void BridgesAt270And1070And1870()
        {
            var bridges = Cases.All(EnvironmentId.River, 40).Where(p => p.Kind == PropKind.Bridge).ToList();
            Assert.That(bridges.Select(p => p.Z), Is.EqualTo(new[] { 270.0, 1070.0, 1870.0 }));
            Assert.That(bridges.Select(p => p.X), Is.All.EqualTo(0.0));
            Assert.That(bridges.Select(p => p.Seed), Is.EqualTo(new[] { 5, 21, 37 }), "variante = k");
        }

        [Test, Category("AC-SCN-16-02")]
        public void LampsEvery25mAndYellowBuoysAt25PlusEvery50()
        {
            var all = Cases.All(EnvironmentId.River, 20);
            var lamps = all.Where(p => p.Kind == PropKind.Lamp).ToList();
            Assert.That(lamps.Count, Is.EqualTo(20 * 4));
            Assert.That(lamps.Select(p => Math.Abs(p.X)), Is.All.EqualTo(9.5));
            Assert.That(lamps.Where(p => p.X > 0).Select(p => p.Z).Take(3), Is.EqualTo(new[] { 12.5, 37.5, 62.5 }));
            var buoys = all.Where(p => p.Kind == PropKind.LaneBuoy).ToList();
            Assert.That(buoys.Select(p => Math.Abs(p.X)), Is.All.EqualTo(3.0));
            Assert.That(buoys.Select(p => p.Seed), Is.All.EqualTo(1), "amarillas (#FDD835)");
            Assert.That(buoys.Where(p => p.X > 0).Select(p => p.Z).Take(3), Is.EqualTo(new[] { 25.0, 75.0, 125.0 }));
            Assert.That(Core.Scene.Art.RiverArt.BuoyTones[1], Is.EqualTo(0xFFFDD835u));
        }

        [Test, Category("AC-SCN-16-01")]
        public void HousesBetween13And17WithTheirScaleSeedAndShare()
        {
            var houses = Cases.All(EnvironmentId.River, 2000).Where(p => p.Kind == PropKind.Building || p.Kind == PropKind.Boathouse).ToList();
            Assert.That(houses.Select(p => Math.Abs(p.X)), Is.All.InRange(13.0, 17.0));
            Assert.That(houses.Select(p => p.Scale), Is.All.InRange(0.6, 1.0));
            Assert.That(houses.Select(p => p.Seed), Is.All.InRange(0, 999));
            var clubs = houses.Count(p => p.Kind == PropKind.Boathouse) / (double)houses.Count;
            Assert.That(clubs, Is.EqualTo(0.3).Within(0.03), "30 % de clubes");
            Assert.That(new RiverEnvironment().WaveAmplitude, Is.EqualTo(0.35));
            Assert.That(new RiverEnvironment().WaveScale, Is.EqualTo(2.0));
            var c = SceneColor.FromHex("#123456");
            Assert.That(new RiverEnvironment().TintWater(c), Is.EqualTo(c), "sin tinte");
        }
    }

    [TestFixture]
    public sealed class CoastEnvironmentTests
    {
        // legacy: test/scene/environment_test.dart:10 'EnvironmentId.coast: propsForSegment es determinista'
        [TestCase(TestName = "EnvironmentId.coast: propsForSegment es determinista", Category = "LT-102,AC-SCN-14-08")]
        public void Deterministic() => Cases.Deterministic(EnvironmentId.Coast);

        // legacy: test/scene/environment_test.dart:17 'EnvironmentId.coast: cada segmento tiene objetos y quedan dentro del segmento'
        [TestCase(TestName = "EnvironmentId.coast: cada segmento tiene objetos y quedan dentro del segmento", Category = "LT-103,AC-SCN-14-08")]
        public void InsideTheirSegment() => Cases.InsideTheirSegment(EnvironmentId.Coast);

        // legacy: test/scene/environment_test.dart:30 'EnvironmentId.coast: visibleProps ordena de lejos a cerca y respeta el máximo'
        [TestCase(TestName = "EnvironmentId.coast: visibleProps ordena de lejos a cerca y respeta el máximo", Category = "LT-104,AC-SCN-14-08,AC-SCN-14-09")]
        public void VisibleSortedAndCapped() => Cases.VisibleSortedAndCapped(EnvironmentId.Coast);

        [Test, Category("AC-SCN-17-02")]
        public void BeachesEvery250mAndCliffsElsewhereOnlyOnTheLeft()
        {
            var segments = new ShoreSegments(new CoastEnvironment());
            for (var k = 0; k < 200; k++)
            {
                var props = segments.PropsForSegment(k).ToArray();
                var z0 = k * 50.0;
                Assert.That(props.Select(p => p.X), Is.All.LessThan(0), "solo la orilla izquierda (segmento " + k + ")");
                if (k % 5 == 2)
                {
                    var umbrellas = props.Where(p => p.Kind == PropKind.BeachUmbrella).ToList();
                    Assert.That(umbrellas.Count, Is.EqualTo(3));
                    for (var i = 0; i < 3; i++)
                    {
                        Assert.That(umbrellas[i].X, Is.InRange(-13.0, -10.0));
                        Assert.That(umbrellas[i].Z, Is.InRange(z0 + 8 + i * 14, z0 + 14 + i * 14));
                    }

                    Assert.That(props.Any(p => p.Kind == PropKind.Cliff), Is.False);
                }
                else
                {
                    var cliffs = props.Where(p => p.Kind == PropKind.Cliff).ToList();
                    Assert.That(cliffs.Select(p => p.Z), Is.EqualTo(new[] { z0, z0 + 25 }));
                    Assert.That(cliffs.Select(p => p.X), Is.All.EqualTo(-14.0));
                    Assert.That(cliffs.Select(p => p.Scale), Is.All.InRange(0.8, 1.3));
                }
            }

            Assert.That(TerrainProfile.IsBeach(2) && TerrainProfile.IsBeach(7) && TerrainProfile.IsBeach(12), Is.True, "100–150, 350–400, 600–650 m");
        }

        [Test, Category("AC-SCN-17-03")]
        public void LighthouseAt530And1530OnACliffSegment()
        {
            var houses = Cases.All(EnvironmentId.Coast, 60).Where(p => p.Kind == PropKind.Lighthouse).ToList();
            Assert.That(houses.Select(p => p.Z), Is.EqualTo(new[] { 530.0, 1530.0, 2530.0 }));
            Assert.That(houses.Select(p => p.X), Is.All.EqualTo(-14.0));
            Assert.That(TerrainProfile.IsBeach(10), Is.False, "siempre sobre un tramo de acantilado");
        }

        [Test, Category("AC-SCN-17-01")]
        public void TheSeaIsTintedThirtyPercent()
        {
            var env = new CoastEnvironment();
            Assert.That(env.WaveAmplitude, Is.EqualTo(0.8));
            Assert.That(env.WaveScale, Is.EqualTo(4.0));
            Assert.That(env.MaxVisibleProps, Is.EqualTo(60));
            var black = SceneColor.Black;
            var t = env.TintWater(black);
            Assert.That(t.R, Is.EqualTo(0.3 * 0x0E / 255.0).Within(1e-12));
            Assert.That(t.G, Is.EqualTo(0.3 * 0x6B / 255.0).Within(1e-12));
            Assert.That(t.B, Is.EqualTo(0.3 * 0x7A / 255.0).Within(1e-12));
        }
    }

    [TestFixture]
    public sealed class RegattaEnvironmentTests
    {
        // legacy: test/scene/environment_test.dart:10 'EnvironmentId.regatta: propsForSegment es determinista'
        [TestCase(TestName = "EnvironmentId.regatta: propsForSegment es determinista", Category = "LT-105,AC-SCN-14-08")]
        public void Deterministic() => Cases.Deterministic(EnvironmentId.Regatta);

        // legacy: test/scene/environment_test.dart:17 'EnvironmentId.regatta: cada segmento tiene objetos y quedan dentro del segmento'
        [TestCase(TestName = "EnvironmentId.regatta: cada segmento tiene objetos y quedan dentro del segmento", Category = "LT-106,AC-SCN-14-08")]
        public void InsideTheirSegment() => Cases.InsideTheirSegment(EnvironmentId.Regatta);

        // legacy: test/scene/environment_test.dart:30 'EnvironmentId.regatta: visibleProps ordena de lejos a cerca y respeta el máximo'
        [TestCase(TestName = "EnvironmentId.regatta: visibleProps ordena de lejos a cerca y respeta el máximo", Category = "LT-107,AC-SCN-14-08,AC-SCN-14-09")]
        public void VisibleSortedAndCapped() => Cases.VisibleSortedAndCapped(EnvironmentId.Regatta);

        // legacy: test/scene/environment_test.dart:42 'el canal de regata tiene boyas cada 10 m'
        [TestCase(TestName = "el canal de regata tiene boyas cada 10 m", Category = "LT-108,AC-SCN-18-01")]
        public void BuoysEvery10m()
        {
            var segments = new ShoreSegments(new RegattaEnvironment());
            var zs = segments.PropsForSegment(3).ToArray().Where(p => p.Kind == PropKind.LaneBuoy).Select(p => p.Z).Distinct().OrderBy(z => z).ToList();
            Assert.That(zs.Count, Is.EqualTo(5));
            Assert.That(zs[1] - zs[0], Is.EqualTo(10));
        }

        // legacy: test/scene/environment_test.dart:54 'en la regata los hitos lejanos se ven aunque haya muchas boyas'
        [TestCase(TestName = "en la regata los hitos lejanos se ven aunque haya muchas boyas", Category = "LT-109,AC-SCN-14-09,AC-SCN-14-05")]
        public void FarLandmarksVisible()
        {
            var env = new RegattaEnvironment();
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            new ShoreSegments(env).VisibleProps(0, into);
            var landmarks = new[] { PropKind.Grandstand, PropKind.FinishTower, PropKind.DistanceMarker };
            Assert.That(into.Items.ToArray().Any(p => landmarks.Contains(p.Kind) && p.Z > 150), Is.True);
        }

        [Test, Category("AC-SCN-18-01")]
        public void FourLanesTwentyPerSegmentColorPer250m()
        {
            var segments = new ShoreSegments(new RegattaEnvironment());
            for (var k = 0; k < 60; k++)
            {
                var buoys = segments.PropsForSegment(k).ToArray().Where(p => p.Kind == PropKind.LaneBuoy).ToList();
                Assert.That(buoys.Count, Is.EqualTo(20));
                Assert.That(buoys.Select(p => p.X).Distinct().OrderBy(x => x), Is.EqualTo(new[] { -9.0, -3.0, 3.0, 9.0 }));
                Assert.That(buoys.Select(p => p.Seed), Is.All.EqualTo((k / 5) % 3));
            }

            // Rojo de 0 a 249 m, amarillo de 250 a 499, blanco de 500 a 749, rojo de 750 a 999.
            Assert.That(new[] { RegattaEnvironment.BuoyColor(0), RegattaEnvironment.BuoyColor(4), RegattaEnvironment.BuoyColor(5), RegattaEnvironment.BuoyColor(10),
                RegattaEnvironment.BuoyColor(15) }, Is.EqualTo(new[] { 0, 0, 1, 2, 0 }));
            Assert.That(Core.Scene.Art.RegattaArt.BuoyTones, Is.EqualTo(new[] { 0xFFE53935u, 0xFFFDD835u, 0xFFECEFF1u }));
            var env = new RegattaEnvironment();
            Assert.That(env.WaveAmplitude, Is.EqualTo(0.3));
            Assert.That(env.WaveScale, Is.EqualTo(1.5));
            Assert.That(env.MaxVisibleProps, Is.EqualTo(200));
        }

        [Test, Category("AC-SCN-18-02")]
        public void MarkersEvery250mWithTheirMeters()
        {
            var markers = Cases.All(EnvironmentId.Regatta, 41).Where(p => p.Kind == PropKind.DistanceMarker).ToList();
            Assert.That(markers.Select(p => p.Z), Is.EqualTo(Enumerable.Range(0, 9).Select(i => i * 250.0)));
            Assert.That(markers.Select(p => p.Seed), Is.EqualTo(Enumerable.Range(0, 9).Select(i => i * 250)));
            Assert.That(markers.Select(p => p.X), Is.All.EqualTo(-10.5));
        }

        [Test, Category("AC-SCN-18-03")]
        public void GrandstandTowerAndAlternatingFlags()
        {
            var all = Cases.All(EnvironmentId.Regatta, 40);
            Assert.That(all.Where(p => p.Kind == PropKind.Grandstand).Select(p => (p.X, p.Z)), Is.EqualTo(new[] { (12.0, 210.0), (12.0, 1210.0) }));
            Assert.That(all.Where(p => p.Kind == PropKind.FinishTower).Select(p => (p.X, p.Z)), Is.EqualTo(new[] { (-12.0, 610.0), (-12.0, 1610.0) }));
            var flags = all.Where(p => p.Kind == PropKind.Flag).Take(4).ToList();
            Assert.That(flags.Select(p => (p.X, p.Z, p.Seed % 4)), Is.EqualTo(new[] { (10.5, 30.0, 0), (-10.5, 80.0, 1), (10.5, 130.0, 2), (-10.5, 180.0, 3) }));
            Assert.That(Core.Scene.Art.RegattaArt.FlagTones, Is.EqualTo(new[] { 0xFFE53935u, 0xFFFFB300u, 0xFF1E88E5u, 0xFF43A047u }));
        }
    }

    // Lo común de los tres (las mismas aserciones que el legacy, por escenario).
    internal static class Cases
    {
        public static List<ShoreProp> All(EnvironmentId id, int segments)
        {
            var s = new ShoreSegments(EnvironmentCatalog.Create(id));
            var all = new List<ShoreProp>();
            for (var k = 0; k < segments; k++)
            {
                all.AddRange(s.PropsForSegment(k).ToArray());
            }

            return all;
        }

        public static void Deterministic(EnvironmentId id)
        {
            var a = new ShoreSegments(EnvironmentCatalog.Create(id));
            var b = new ShoreSegments(EnvironmentCatalog.Create(id));
            var first = a.PropsForSegment(7).ToArray();
            Assert.That(a.PropsForSegment(7).ToArray(), Is.EqualTo(first));
            Assert.That(b.PropsForSegment(7).ToArray(), Is.EqualTo(first));
            Assert.That(a.PropsForSegment(8).ToArray(), Is.Not.EqualTo(first));
        }

        public static void InsideTheirSegment(EnvironmentId id)
        {
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            for (var k = 0; k < 30; k++)
            {
                var props = segments.PropsForSegment(k);
                Assert.That(props.Length, Is.GreaterThan(0), "segmento " + k + " vacío");
                foreach (var p in props)
                {
                    Assert.That(p.Z, Is.InRange(k * ShoreEnvironment.SegmentLength, (k + 1) * ShoreEnvironment.SegmentLength));
                    if (p.Kind != PropKind.Bridge && p.Kind != PropKind.LaneBuoy)
                    {
                        Assert.That(Math.Abs(p.X), Is.GreaterThanOrEqualTo(ShoreEnvironment.BankX), p.Kind + " en el agua");
                    }
                }
            }
        }

        public static void VisibleSortedAndCapped(EnvironmentId id)
        {
            var env = EnvironmentCatalog.Create(id);
            var into = new ShorePropBuffer(env.MaxVisibleProps);
            new ShoreSegments(env).VisibleProps(1234, into);
            var props = into.Items;
            Assert.That(props.Length, Is.LessThanOrEqualTo(env.MaxVisibleProps));
            for (var i = 1; i < props.Length; i++)
            {
                Assert.That(props[i].Z, Is.LessThanOrEqualTo(props[i - 1].Z));
            }

            foreach (var p in props)
            {
                Assert.That(p.Z, Is.InRange(1235, 1234 + ShoreEnvironment.VisibleRange));
            }
        }
    }
}
