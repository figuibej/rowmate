using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // TEN · Los cortes del terreno (AC-SCN-16-04, -17-04, -18-04; DT8-13): continuos entre segmentos (costura < 1 mm), nada flota
    // y las rampas cortan el muro y el malecón sin escalones entre segmentos.
    [TestFixture]
    public sealed class TerrainEnvironmentTests
    {
        [Test, Category("AC-SCN-16-04")]
        public void TheRiverHasItsWallQuayAndCity()
        {
            Assert.That(TerrainProfile.Height(EnvironmentId.River, 0, 10), Is.EqualTo(-0.5), "0,5 m de muro bajo el agua");
            Assert.That(TerrainProfile.Height(EnvironmentId.River, 8.99, 10), Is.EqualTo(-0.5));
            Assert.That(TerrainProfile.Height(EnvironmentId.River, 9.0, 10), Is.EqualTo(1.2), "el muro, plomo");
            Assert.That(TerrainProfile.Height(EnvironmentId.River, -11, 10), Is.EqualTo(1.2), "el malecón");
            Assert.That(TerrainProfile.Height(EnvironmentId.River, 60, 10), Is.EqualTo(1.2), "la ciudad");
        }

        [Test, Category("AC-SCN-18-04")]
        public void TheRegattaBankRisesTwentyFiveDegreesFromNinePointEight()
        {
            Assert.That(TerrainProfile.Height(EnvironmentId.Regatta, 9.8, 0), Is.EqualTo(0).Within(1e-12));
            Assert.That(TerrainProfile.RegattaBankTopX, Is.EqualTo(11.94).Within(0.01));
            Assert.That(TerrainProfile.Height(EnvironmentId.Regatta, -11, 0), Is.EqualTo((11 - 9.8) * Math.Tan(25 * Math.PI / 180)).Within(1e-12));
            Assert.That(TerrainProfile.Height(EnvironmentId.Regatta, 13, 0), Is.EqualTo(1.0));
            Assert.That(TerrainProfile.Height(EnvironmentId.Regatta, 0, 0), Is.EqualTo(RegattaArt.ChannelBed));
        }

        [Test, Category("AC-SCN-17-04")]
        public void TheCoastHasCliffsBeachesAndTheSea()
        {
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, 30, 20), Is.EqualTo(CoastArt.SeaFloor), "a la derecha, el mar");
            // Playa (k = 2: 100–150 m) lejos de las transiciones: 0 en −9,5, 6° hasta −20 y la duna a 2 m en −30.
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, -9.5, 125), Is.EqualTo(0).Within(1e-12));
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, -20, 125), Is.EqualTo(10.5 * Math.Tan(6 * Math.PI / 180)).Within(1e-9));
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, -29.999, 125), Is.EqualTo(2).Within(1e-3));
            // Acantilado (k = 0): la meseta al alto del acantilado de esa z (8·escala).
            var segments = new ShoreSegments(new CoastEnvironment());
            var cliffs = segments.PropsForSegment(0).ToArray();
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, -25, 10), Is.EqualTo(8 * cliffs[0].Scale).Within(1e-12));
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, -25, 35), Is.EqualTo(8 * cliffs[1].Scale).Within(1e-12));
            // El segmento del faro (k = 10): el bloque de z₀ + 25 a 2,5 m (DT8-10).
            Assert.That(TerrainProfile.Height(EnvironmentId.Coast, -25, 530), Is.EqualTo(CoastArt.Promontory));
        }

        [TestCase(EnvironmentId.River, Category = "AC-SCN-16-04")]
        [TestCase(EnvironmentId.Coast, Category = "AC-SCN-17-04")]
        [TestCase(EnvironmentId.Regatta, Category = "AC-SCN-18-04")]
        public void SeamsBetweenSegmentsAreBelowOneMillimeter(EnvironmentId id)
        {
            for (var k = 1; k < 400; k++)
            {
                var z = k * 50.0;
                for (var x = -60.0; x <= 60; x += 0.73)
                {
                    var a = TerrainProfile.Height(id, x, z - 1e-6);
                    var b = TerrainProfile.Height(id, x, z);
                    Assert.That(Math.Abs(a - b), Is.LessThan(1e-3), id + " en z " + z + ", x " + x);
                }
            }
        }

        [Test, Category("AC-SCN-16-04")]
        public void RampsCutTheWallAndTheQuayContinuouslyAcrossSegments()
        {
            var placement = new RiverPlacement(new ShoreSegments(new RiverEnvironment()));
            var buffer = new RampCut[3 * RiverPlacement.MaxRampsPerSegment];
            var cuts = 0;
            for (var k = 0; k < 400; k++)
            {
                var n = placement.Ramps(k, buffer);
                for (var i = 0; i < n; i++)
                {
                    var r = buffer[i];
                    Assert.That(r.ZEnd - r.ZStart, Is.EqualTo(RiverArt.RampLength).Within(1e-9));
                    var x = r.Side * 9.2;
                    var mid = (r.ZStart + r.ZEnd) / 2;
                    var around = new RampCut[3 * RiverPlacement.MaxRampsPerSegment];
                    var count = placement.RampsAround(mid, around);
                    var h = TerrainProfile.RiverHeight(x, mid, new ReadOnlySpan<RampCut>(around, 0, count));
                    Assert.That(h, Is.InRange(RiverArt.RampBottomY, RiverArt.QuayHeight).And.Not.EqualTo(RiverArt.QuayHeight), "el muro cortado");
                    // Continuidad a lo largo de la rampa (también si cruza al segmento siguiente): pasos de 1 cm, saltos < 1 cm.
                    var prev = r.SurfaceAt(r.ZStart);
                    for (var z = r.ZStart; z <= r.ZEnd; z += 0.01)
                    {
                        var s = r.SurfaceAt(z);
                        Assert.That(Math.Abs(s - prev), Is.LessThan(0.01));
                        prev = s;
                    }

                    cuts++;
                }
            }

            Assert.That(cuts, Is.GreaterThan(50));
        }

        [Test, Category("AC-SCN-16-04"), Category("AC-SCN-17-04"), Category("AC-SCN-18-04")]
        public void NothingFloatsIn100Km()
        {
            // El pie de cada objeto del legacy sobre el terreno, a lo sumo 2,5 m sobre el agua (el muro del río, 1,2).
            foreach (var id in new[] { EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
                for (var k = 0; k < 2000; k++)
                {
                    foreach (var p in segments.PropsForSegment(k))
                    {
                        if (p.Kind == PropKind.LaneBuoy || p.Kind == PropKind.Bridge)
                        {
                            continue;
                        }

                        var ground = PropPlacement.FootY(id, p);
                        Assert.That(ground, Is.InRange(-0.7, 2.5), id + " " + p);
                        if (p.Kind != PropKind.Lighthouse && p.Kind != PropKind.Cliff)
                        {
                            Assert.That(ground, Is.EqualTo(TerrainProfile.Height(id, p.X, p.Z)), "apoya en el terreno: " + id + " " + p);
                        }
                    }
                }
            }
        }
    }

    // EFI · El relleno de los tres escenarios (DT8-12; 02-tech-design §1.1).
    [TestFixture]
    public sealed class EnvironmentFillTests
    {
        [TestCase(EnvironmentId.River, Category = "AC-SCN-16-04")]
        [TestCase(EnvironmentId.Coast, Category = "AC-SCN-17-04")]
        [TestCase(EnvironmentId.Regatta, Category = "AC-SCN-18-04")]
        public void DeterministicInAnyOrderAndItNeverConsumesTheGenerator(EnvironmentId id)
        {
            var env = EnvironmentCatalog.Create(id);
            var segments = new ShoreSegments(env);
            var before = new List<ShoreProp>[60];
            for (var k = 0; k < 60; k++)
            {
                before[k] = segments.PropsForSegment(k).ToArray().ToList();
            }

            var forward = Fill(id, segments, Enumerable.Range(0, 60));
            var backward = Fill(id, segments, Enumerable.Range(0, 60).Reverse());
            for (var k = 0; k < 60; k++)
            {
                Assert.That(backward[k], Is.EqualTo(forward[k]), "segmento " + k + " en otro orden");
                Assert.That(new ShoreSegments(EnvironmentCatalog.Create(id)).PropsForSegment(k).ToArray(), Is.EqualTo(before[k].ToArray()),
                    "el generador del legacy no cambia");
            }

            Assert.That(forward.Sum(f => f.Count), Is.GreaterThan(60), id + ": hay relleno");
        }

        [TestCase(EnvironmentId.River, Category = "AC-SCN-16-04")]
        [TestCase(EnvironmentId.Coast, Category = "AC-SCN-17-04")]
        [TestCase(EnvironmentId.Regatta, Category = "AC-SCN-18-04")]
        public void InsideEighteenMetersOnlyLowThings(EnvironmentId id)
        {
            var segments = new ShoreSegments(EnvironmentCatalog.Create(id));
            var fill = Fill(id, segments, Enumerable.Range(0, 400));
            foreach (var items in fill)
            {
                foreach (var item in items)
                {
                    if (Math.Abs(item.X) < EnvironmentFill.LowZoneX && item.Kind != FillKind.Spectator)
                    {
                        Assert.That(item.Height, Is.LessThanOrEqualTo(EnvironmentFill.LowMaxHeight + 1e-9), item.Kind + " en |x| " + Math.Abs(item.X));
                    }

                    if (item.Kind == FillKind.CityBlock)
                    {
                        Assert.That(Math.Abs(item.X) - item.SizeX / 2, Is.GreaterThanOrEqualTo(RiverArt.FillFromX - 1e-9), "manzanas desde |x| 22");
                        Assert.That(item.Height, Is.InRange(9.0, 27.0));
                    }

                    if (item.Kind == FillKind.Poplar)
                    {
                        Assert.That(Math.Abs(item.X), Is.InRange(30.0, 40.0));
                        Assert.That(item.Height, Is.InRange(14.0, 18.0));
                    }
                }
            }
        }

        [Test, Category("AC-SCN-18-04"), Category("AC-SCN-20-06")]
        public void AboutThirtyFourSpectatorsPerGrandstand()
        {
            var segments = new ShoreSegments(new RegattaEnvironment());
            var into = new FillBuffer(EnvironmentFill.SegmentCapacity);
            EnvironmentFill.Spectators(segments.PropsForSegment(4), into);
            var count = into.Items.ToArray().Count(i => i.Kind == FillKind.Spectator);
            Assert.That(count, Is.InRange(26, 42), "4 × 12 × 0,7 ≈ 34");
            foreach (var s in into.Items)
            {
                Assert.That(s.X, Is.InRange(12.0, 18.0), "sobre las gradas, hacia afuera del agua");
                Assert.That(s.Rank, Is.InRange(1.5, 4.0), "período");
            }
        }

        [Test, Category("AC-SCN-16-04")]
        public void CityBlocksNeverOverlapInsideASegment()
        {
            var into = new FillBuffer(EnvironmentFill.SegmentCapacity);
            var rnd = new DartRandom(0);
            for (var k = 0; k < 200; k++)
            {
                into.Clear();
                EnvironmentFill.CityBlocks(k, rnd, into);
                var items = into.Items.ToArray();
                for (var i = 0; i < items.Length; i++)
                {
                    for (var j = i + 1; j < items.Length; j++)
                    {
                        var a = items[i];
                        var b = items[j];
                        var ox = Math.Min(a.X + a.SizeX / 2, b.X + b.SizeX / 2) - Math.Max(a.X - a.SizeX / 2, b.X - b.SizeX / 2);
                        var oz = Math.Min(a.Z + a.SizeZ / 2, b.Z + b.SizeZ / 2) - Math.Max(a.Z - a.SizeZ / 2, b.Z - b.SizeZ / 2);
                        Assert.That(ox <= 1e-9 || oz <= 1e-9, Is.True, "manzanas cruzadas en el segmento " + k);
                    }

                    Assert.That(items[i].Z - items[i].SizeZ / 2, Is.GreaterThanOrEqualTo(k * 50.0 - 1e-9));
                    Assert.That(items[i].Z + items[i].SizeZ / 2, Is.LessThanOrEqualTo(k * 50.0 + 50 + 1e-9));
                }
            }
        }

        private static List<FillItem>[] Fill(EnvironmentId id, ShoreSegments segments, IEnumerable<int> order)
        {
            var result = new List<FillItem>[order.Max() + 1];
            var rnd = new DartRandom(0);
            var into = new FillBuffer(EnvironmentFill.SegmentCapacity);
            foreach (var k in order)
            {
                into.Clear();
                EnvironmentFill.Segment(id, k, segments.PropsForSegment(k), rnd, into);
                result[k] = into.Items.ToArray().ToList();
            }

            return result;
        }
    }
}
