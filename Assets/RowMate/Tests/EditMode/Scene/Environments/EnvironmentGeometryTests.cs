using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // EGE · El catálogo en 3D (AC-SCN-20-03, -16-04, -17-04, -18-04): medidas ± 10 % sobre la malla sin recortes, triángulos por
    // LOD con los topes del arte, paletas por variante, y el río sin cruces en 100 km con el ancla del legacy (DT8-19, §2.4.1),
    // con su control positivo.
    [TestFixture]
    public sealed class EnvironmentGeometryTests
    {
        public delegate void Builder(int lod, MeshData mesh);

        private static IEnumerable<TestCaseData> Meshes()
        {
            // nombre, armado, capacidades, tope de triángulos por LOD, medidas esperadas (ancho x, alto y, fondo z) del LOD0.
            yield return Case("building", (l, m) => BuildingMesh.Build(4, false, 0, l, m), BuildingMesh.VertexCapacity, BuildingMesh.IndexCapacity,
                RiverArt.BuildingTris, 6, 12.5, 10);
            yield return Case("boathouse", (l, m) => BoathouseMesh.Build(l, m), BoathouseMesh.VertexCapacity, BoathouseMesh.IndexCapacity, RiverArt.BoathouseTris,
                9, 6, 13);
            yield return Case("lamp", (l, m) => LampMesh.Build(l, m), LampMesh.VertexCapacity, LampMesh.IndexCapacity, RiverArt.LampTris, 0.6, 4.4, 0.6);
            yield return Case("laneBuoy", (l, m) => LaneBuoyMesh.Build(l, true, m), LaneBuoyMesh.VertexCapacity, LaneBuoyMesh.IndexCapacity,
                RegattaArt.BuoyTris, 0.5, 0.45, 0.5);
            yield return Case("bridge", (l, m) => BridgeMesh.Build(l, m), BridgeMesh.VertexCapacity, BridgeMesh.IndexCapacity, RiverArt.BridgeTris, 25,
                7.7, 10);
            yield return Case("cliff", (l, m) => CliffMesh.Build(42, l, m, new DartRandom(0)), CliffMesh.VertexCapacity, CliffMesh.IndexCapacity,
                CoastArt.CliffTris, 7.6, 8.5, 28);
            yield return Case("lighthouse", (l, m) => LighthouseMesh.Build(l, m), LighthouseMesh.VertexCapacity, LighthouseMesh.IndexCapacity,
                CoastArt.LighthouseTris, 2.6, 10.8, 2.6);
            yield return Case("beachUmbrella", (l, m) => UmbrellaMesh.Build(l, m), UmbrellaMesh.VertexCapacity, UmbrellaMesh.IndexCapacity,
                CoastArt.UmbrellaTris, 2.4, 3.1, 1.8);
            yield return Case("grandstand", (l, m) => GrandstandMesh.Build(l, m), GrandstandMesh.VertexCapacity, GrandstandMesh.IndexCapacity,
                RegattaArt.GrandstandTris, 6.8, 5.3, 13);
            yield return Case("finishTower", (l, m) => FinishTowerMesh.Build(l, m), FinishTowerMesh.VertexCapacity, FinishTowerMesh.IndexCapacity,
                RegattaArt.TowerTris, 3.05, 11.5, 3.05);
            yield return Case("flag", (l, m) => FlagMesh.Build(l, m), FlagMesh.VertexCapacity, FlagMesh.IndexCapacity, RegattaArt.FlagTris, 1.65, 5, 0.1);
            yield return Case("distanceMarker", (l, m) => MarkerMesh.Build(l, m), MarkerMesh.VertexCapacity, MarkerMesh.IndexCapacity, RegattaArt.MarkerTris,
                1.8, 2.5, 0.1);
            yield return Case("poplar", (l, m) => PoplarMesh.Build(l, m), PoplarMesh.VertexCapacity, PoplarMesh.IndexCapacity, RegattaArt.PoplarTris, 3,
                16, 3);
        }

        private static TestCaseData Case(string name, Builder build, Func<int, int> vertices, Func<int, int> indices, int[] tris, double w, double h,
                                         double d)
        {
            return new TestCaseData(build, vertices, indices, tris, w, h, d).SetName("catálogo " + name).SetCategory("AC-SCN-20-03");
        }

        [TestCaseSource(nameof(Meshes))]
        public void SizesWithinTenPercentAndTrianglesWithinTheBudget(Builder build, Func<int, int> vertices, Func<int, int> indices, int[] tris, double w,
                                                                     double h, double d)
        {
            for (var lod = 0; lod < 3; lod++)
            {
                var mesh = new MeshData(vertices(lod), indices(lod));
                build(lod, mesh);
                Assert.That(mesh.TriangleCount, Is.GreaterThan(0).And.LessThanOrEqualTo(tris[lod]), "LOD" + lod);
                if (lod == 0)
                {
                    mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
                    Assert.That(maxX - minX, Is.EqualTo(w).Within(w * 0.1 + 0.02), "ancho");
                    Assert.That(maxY, Is.EqualTo(h).Within(h * 0.1 + 0.02), "alto");
                    Assert.That(maxZ - minZ, Is.EqualTo(d).Within(d * 0.1 + 0.02), "fondo");
                }
            }
        }

        [Test, Category("AC-SCN-20-03"), Category("AC-SCN-16-03")]
        public void TheBridgeArchHasItsCrownAtFiveMeters()
        {
            BridgeMesh.ArchPoint(0.5, out var x, out var y);
            Assert.That(x, Is.EqualTo(0).Within(1e-12));
            Assert.That(y, Is.EqualTo(5.0).Within(1e-12));
            BridgeMesh.ArchPoint(0, out var x0, out var y0);
            Assert.That((x0, y0), Is.EqualTo((-8.5, 1.5)));
            var mesh = new MeshData(BridgeMesh.VertexCapacity(0), BridgeMesh.IndexCapacity(0));
            BridgeMesh.Build(0, mesh);
            mesh.Bounds(out var minX, out _, out _, out var maxX, out var maxY, out _);
            Assert.That((minX, maxX), Is.EqualTo((-12.5, 12.5)), "estribos hasta |x| 12,5 (DT8-19 R4)");
            Assert.That(maxY, Is.EqualTo(7.7).Within(1e-6), "parapeto hasta 7,7");
        }

        [Test, Category("AC-SCN-20-03")]
        public void PalettesPerVariantAreTheLegacy()
        {
            Assert.That(CoastArt.UmbrellaTones, Is.EqualTo(new[] { 0xFFF06292u, 0xFF4FC3F7u, 0xFFFFD54Fu }));
            Assert.That(RiverArt.BuoyTones, Is.EqualTo(RegattaArt.BuoyTones));
            Assert.That(RegattaArt.FlagTones.Length, Is.EqualTo(4));
            Assert.That(RiverArt.BuildingTones.Length, Is.EqualTo(3));
            Assert.That(RiverArt.BoathouseStripe, Is.EqualTo(0xFF1565C0u));
            Assert.That((CoastArt.LighthouseTower, CoastArt.LighthouseStripe, CoastArt.Lantern), Is.EqualTo((0xFFECEFF1u, 0xFFD32F2Fu, 0xFF37474Fu)));
            // Pisos por variante (3 + seed mod 4) y edificio de 3 m por piso: 3 a 6 pisos, de 9 a 18 m.
            for (var floors = 3; floors <= 6; floors++)
            {
                var mesh = new MeshData(BuildingMesh.VertexCapacity(1), BuildingMesh.IndexCapacity(1));
                BuildingMesh.Build(floors, true, 1, 1, mesh);
                mesh.Bounds(out _, out _, out _, out _, out var maxY, out _);
                Assert.That(maxY, Is.EqualTo(floors * 3 + RiverArt.MansardHeight).Within(1e-5));
            }
        }

        [Test, Category("AC-SCN-17-03"), Category("AC-SCN-17-04")]
        public void TheCliffEdgeFollowsTheLegacyRandom()
        {
            Span<double> edge = stackalloc double[6];
            CliffMesh.EdgeHeights(77, new DartRandom(0), edge);
            var legacy = new DartRandom(77);
            Assert.That(edge[0], Is.EqualTo(8));
            for (var i = 1; i <= 5; i++)
            {
                Assert.That(edge[i], Is.EqualTo(8 + legacy.NextDouble() * 1.5 - 0.4 * i), "escalón " + i);
            }
        }

        // ── El río sin cruces (DT8-19; gate QA-G8-01) ──

        [Test, Category("AC-SCN-16-04"), Category("AC-SCN-14-10")]
        public void TheRiverHasNoCrossingsIn100KmAndAnchorsAreTheLegacy()
        {
            var report = Sweep(true, out var clubs, out var ramps);
            Assert.That(report, Is.Empty, string.Join("\n", report.Take(20)));
            TestContext.WriteLine("clubes con rampa: " + ramps + " de " + clubs);
            Assert.That(ramps / (double)clubs, Is.GreaterThanOrEqualTo(0.95), "≥ 95 % de los clubes con rampa");
        }

        [Test, Category("AC-SCN-16-04")]
        public void PositiveControlWithoutTheRulesFindsTheGateCrossings()
        {
            var report = Sweep(false, out _, out _);
            Assert.That(report.Count(r => r.StartsWith("cruce", StringComparison.Ordinal)), Is.GreaterThan(100), "sin R1 a R6 aparecen los cruces de la sonda del gate");
        }

        private static List<string> Sweep(bool rules, out int clubs, out int ramps)
        {
            var segments = new ShoreSegments(new RiverEnvironment());
            var placement = new RiverPlacement(segments) { Rules = rules };
            var problems = new List<string>();
            var volumes = new PropVolume[RiverPlacement.MaxVolumesPerSegment];
            var next = new PropVolume[RiverPlacement.MaxVolumesPerSegment];
            var houses = new HouseLayout[RiverPlacement.MaxHousesPerSegment];
            var rampBuffer = new RampCut[RiverPlacement.MaxRampsPerSegment];
            clubs = 0;
            ramps = 0;
            var n = placement.Volumes(0, volumes);
            for (long k = 0; k < 2000; k++)
            {
                var m = placement.Volumes(k + 1, next);
                // Dentro del segmento y contra el siguiente.
                for (var i = 0; i < n; i++)
                {
                    var a = volumes[i];
                    for (var j = i + 1; j < n; j++)
                    {
                        Check(a, volumes[j], k, problems);
                    }

                    for (var j = 0; j < m; j++)
                    {
                        Check(a, next[j], k, problems);
                    }

                    if (rules)
                    {
                        CheckWall(a, k, problems);
                    }
                }

                var h = placement.Houses(k, houses);
                for (var i = 0; i < h; i++)
                {
                    var p = houses[i].Prop;
                    var v = houses[i].Volume;
                    Assert.That(v.MinZ, Is.EqualTo(p.Z), "R1: la cara a la cámara en la z del legacy");
                    Assert.That((v.MinX + v.MaxX) / 2, Is.Not.NaN);
                    clubs += p.Kind == PropKind.Boathouse ? 1 : 0;
                }

                ramps += rules ? placement.Ramps(k, rampBuffer) : 0;
                Array.Copy(next, volumes, m);
                n = m;
            }

            return problems;
        }

        private static void Check(in PropVolume a, in PropVolume b, long k, List<string> problems)
        {
            if (a.Overlaps(b, 1e-3) && !Allowed(a, b))
            {
                problems.Add("cruce en el segmento " + k + ": " + a.Kind + " [" + a.MinX + "; " + a.MaxX + "]×[" + a.MinZ + "; " + a.MaxZ + "] y " + b.Kind
                    + " [" + b.MinX + "; " + b.MaxX + "]×[" + b.MinZ + "; " + b.MaxZ + "]");
            }
        }

        // Las piezas del mismo puente se tocan por construcción.
        private static bool Allowed(in PropVolume a, in PropVolume b)
        {
            bool Bridge(PropVolumeKind kind) => kind == PropVolumeKind.BridgeDeck || kind == PropVolumeKind.BridgeAbutment || kind == PropVolumeKind.BridgePier;
            return Bridge(a.Kind) && Bridge(b.Kind) && a.MinZ == b.MinZ;
        }

        // El muro (|x| 9) solo lo cruzan la rampa (en su corte) y los pilares del puente; en |x| < 8,5 solo el puente y las boyas.
        private static void CheckWall(in PropVolume v, long k, List<string> problems)
        {
            var crossesWall = v.MinX < -RiverArt.WallX && v.MaxX > -RiverArt.WallX || v.MinX < RiverArt.WallX && v.MaxX > RiverArt.WallX;
            if (crossesWall && v.Kind != PropVolumeKind.Ramp && v.Kind != PropVolumeKind.BridgePier && v.Kind != PropVolumeKind.BridgeDeck)
            {
                problems.Add("muro en el segmento " + k + ": " + v.Kind);
            }

            var inner = Math.Min(Math.Abs(v.MinX), Math.Abs(v.MaxX));
            var straddles = v.MinX < 0 && v.MaxX > 0;
            if ((inner < RiverArt.RampMinX || straddles) && v.Kind != PropVolumeKind.Buoy && v.Kind != PropVolumeKind.BridgeDeck
                && v.Kind != PropVolumeKind.BridgePier && v.Kind != PropVolumeKind.BridgeAbutment)
            {
                problems.Add("canal en el segmento " + k + ": " + v.Kind + " en |x| " + inner);
            }
        }
    }
}
