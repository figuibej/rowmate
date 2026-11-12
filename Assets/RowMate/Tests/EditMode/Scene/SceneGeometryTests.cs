using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;

namespace RowMate.Tests.EditMode.Scene
{
    // GEO · Las mallas puras (06a-escena-lago/02-tech-design §2.14; AC-SCN-15-06, AC-SCN-01-08): las medidas de la receta por
    // defecto (Core/Scene/Art/LakeArt), los presupuestos de triángulos por LOD y los bordes iguales entre trozos de terreno.
    [TestFixture]
    public sealed class SceneGeometryTests
    {
        [TestCase(0, Category = "AC-SCN-15-06")]
        [TestCase(1, Category = "AC-SCN-15-06")]
        [TestCase(2, Category = "AC-SCN-15-06")]
        public void ThePineMeasuresSixMetersAndHasItsBudget(int lod)
        {
            for (var form = 0; form < PineMesh.Forms; form++)
            {
                var mesh = new MeshData(PineMesh.VertexCapacity(lod), PineMesh.IndexCapacity(lod));
                PineMesh.Build(form, lod, mesh);
                mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
                Assert.That(maxY - Math.Min(0, minY), Is.EqualTo(LakeArt.PineHeight).Within(0.6), "alto 6,0 m × escala ± 10 %");
                var width = Math.Max(maxX - minX, maxZ - minZ);
                Assert.That(width, Is.EqualTo(2 * LakeArt.PineMaxHalfWidth).Within(0.26), "ancho máximo ≈ 2,6 m × escala");
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.PineTriangleBudget[lod]));
                Assert.That(mesh.TriangleCount, Is.GreaterThan(LakeArt.PineTriangleBudget[lod] * 0.6), "el LOD usa su presupuesto");
            }
        }

        [Test, Category("AC-SCN-15-06")]
        public void ThePineHasThreeTiersWithItsPaletteChannelAndAnIrregularSilhouette()
        {
            var mesh = new MeshData(PineMesh.VertexCapacity(0), PineMesh.IndexCapacity(0));
            PineMesh.Build(1, 0, mesh);
            var tiers = new HashSet<float>();
            var foliage = 0;
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.Colors[i * 4 + 3] > 0.5f)
                {
                    foliage++;
                    tiers.Add(mesh.Colors[i * 4 + 2]);
                    Assert.That(mesh.Colors[i * 4], Is.InRange(LakeArt.PineOcclusionInner - 1e-6, 1 + 1e-6), "oclusión de 0,55 a 1");
                }
            }

            Assert.That(tiers.Count, Is.EqualTo(3), "3 gradas: la paleta de la variante (semilla mod 3) va por instancia");
            Assert.That(foliage, Is.GreaterThan(0));
            Assert.That(PineMesh.PaletteOf(5), Is.EqualTo(2));
            Assert.That(PineMesh.FormOf(5), Is.EqualTo(1));
            Assert.That(LakeArt.PineGreens.Length, Is.EqualTo(9), "3 paletas × 3 verdes del legacy");

            // Ningún radio igual en dos lóbulos vecinos (también el último con el primero).
            var rnd = new DartRandom(3);
            for (var trial = 0; trial < 200; trial++)
            {
                var lobes = LakeArt.PineLobesMin + rnd.NextInt(4);
                var factors = new double[lobes];
                PineMesh.LobeFactors(rnd, factors);
                for (var i = 0; i < lobes; i++)
                {
                    Assert.That(Math.Abs(factors[i] - factors[(i + 1) % lobes]), Is.GreaterThan(1e-9), "lóbulos vecinos distintos");
                    Assert.That(factors[i], Is.InRange(0.82, LakeArt.PineLobeMax));
                }
            }
        }

        [Test, Category("AC-SCN-15-06")]
        public void ThePrunedPineStartsItsTiersAboveTheEave()
        {
            var mesh = new MeshData(PineMesh.VertexCapacity(0), PineMesh.IndexCapacity(0));
            PineMesh.Build(0, 0, mesh, LakeArt.CabinPrunedTierBase);
            var lowestFoliage = double.MaxValue;
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                if (mesh.Colors[i * 4 + 3] > 0.5f)
                {
                    lowestFoliage = Math.Min(lowestFoliage, mesh.Y(i));
                }
            }

            Assert.That(lowestFoliage, Is.GreaterThan(LakeArt.CabinWallHeight), "sobre el alero de 2,6 m");
        }

        [TestCase(0, Category = "AC-SCN-15-07")]
        [TestCase(1, Category = "AC-SCN-15-07")]
        public void TheForestRecipeIsNarrowerWithItsBudget(int lod)
        {
            var budget = new[] { 400, 120 };
            for (var form = 0; form < 6; form++)
            {
                var mesh = new MeshData(PineMesh.ForestVertexCapacity(lod), PineMesh.ForestIndexCapacity(lod));
                PineMesh.BuildForest(form, lod, mesh);
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(budget[lod]));
                mesh.Bounds(out var minX, out _, out var minZ, out var maxX, out _, out var maxZ);
                Assert.That(Math.Max(maxX - minX, maxZ - minZ), Is.LessThanOrEqualTo(2 * LakeArt.PineMaxHalfWidth * 0.8 * LakeArt.PineLobeMax + 0.1));
            }
        }

        [TestCase(4.0, 0, Category = "AC-SCN-15-06")]
        [TestCase(3.0, 0, Category = "AC-SCN-15-06")]
        [TestCase(2.5, 1, Category = "AC-SCN-15-06")]
        public void TheCabinMeasures(double depth, int lod)
        {
            var mesh = new MeshData(CabinMesh.VertexCapacity(lod), CabinMesh.IndexCapacity(lod));
            CabinMesh.Build(depth, 1.5, lod, mesh);
            Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(lod == 0 ? 1200 : 300));
            Assert.That(mesh.SubmeshCount, Is.EqualTo(2), "la ventana como submalla aparte (se enciende)");
            mesh.Bounds(out var minX, out _, out var minZ, out var maxX, out var maxY, out var maxZ);
            Assert.That(maxY, Is.EqualTo(4.2).Within(0.42), "techo hasta 4,2 m");
            Assert.That(maxX - minX, Is.EqualTo(4 + 2 * 0.4).Within(0.01), "4 m con el alero de 0,4 a los costados");
            Assert.That(maxZ, Is.EqualTo(depth + 0.5).Within(1e-6), "la profundidad hacia +z con el alero de 0,5");

            // La ventana: 0,8 × 0,7 (± 10 %).
            mesh.Submesh(1, out var start, out var count);
            Bounds(mesh, start, count, out var wx0, out var wy0, out var wx1, out var wy1);
            Assert.That(wx1 - wx0, Is.EqualTo(0.8).Within(0.08));
            Assert.That(wy1 - wy0, Is.EqualTo(0.7).Within(0.07));
            Assert.That(wx0, Is.EqualTo(0.7).Within(1e-6), "de x +0,7 a +1,5 (la del legacy)");
            Assert.That(wy0, Is.EqualTo(1.3).Within(1e-6));

            // La puerta: los vértices con el color de la puerta, 0,8 × 1,4.
            var door = SceneColor.FromArgb(LakeArt.CabinDoor);
            double dx0 = double.MaxValue, dx1 = double.MinValue, dy1 = double.MinValue;
            var body = SceneColor.FromArgb(LakeArt.CabinWood);
            double bx0 = double.MaxValue, bx1 = double.MinValue, by1 = double.MinValue;
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                if (Near(mesh, i, door))
                {
                    dx0 = Math.Min(dx0, mesh.X(i));
                    dx1 = Math.Max(dx1, mesh.X(i));
                    dy1 = Math.Max(dy1, mesh.Y(i));
                }

                if (Near(mesh, i, body) && Math.Abs(mesh.Z(i)) < 1e-6)
                {
                    bx0 = Math.Min(bx0, mesh.X(i));
                    bx1 = Math.Max(bx1, mesh.X(i));
                    by1 = Math.Max(by1, mesh.Y(i));
                }
            }

            Assert.That(dx1 - dx0, Is.EqualTo(0.8).Within(0.08));
            Assert.That(dy1, Is.EqualTo(1.4).Within(0.14));
            Assert.That(bx1 - bx0, Is.EqualTo(4.0).Within(1e-6), "cuerpo de 4 m de frente");
            Assert.That(by1, Is.EqualTo(4.2).Within(1e-6), "el hastial hasta la cumbrera");
        }

        [Test, Category("AC-SCN-15-06")]
        public void ThePierReachesThreeMetersIntoTheLakeAtFortyCentimeters()
        {
            var segments = new ShoreSegments(new LakeEnvironment());
            var pier = Array.Find(segments.PropsForSegment(3).ToArray(), p => p.Kind == PropKind.Pier);
            var layout = PropPlacement.Pier(EnvironmentId.Lake, pier);
            foreach (var lod in new[] { 0, 1 })
            {
                var mesh = new MeshData(PierMesh.VertexCapacity(lod), PierMesh.IndexCapacity(lod));
                PierMesh.Build(layout, lod, mesh);
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.PierTriangleBudget[lod]));
                mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
                Assert.That(maxY, Is.EqualTo(0.40).Within(1e-6), "la cubierta a 0,4 m");
                Assert.That(maxX, Is.EqualTo(3.0).Within(1e-6), "tablones de 3 m hacia el centro (x −6)");
                Assert.That(minX, Is.EqualTo(layout.DeckFromX - layout.AnchorX).Within(1e-6), "hasta la playa");
                Assert.That(minY, Is.LessThan(0), "los pilotes hasta el fondo");
                Assert.That(maxZ - minZ, Is.EqualTo(1.6).Within(1e-6));
            }
        }

        [Test, Category("AC-SCN-01-08")]
        public void TheHullIsASingleOfEightPointTwoMeters()
        {
            var mesh = new MeshData(HullMesh.VertexCapacity, HullMesh.IndexCapacity);
            HullMesh.Build(mesh);
            mesh.Bounds(out var minX, out var minY, out var minZ, out var maxX, out var maxY, out var maxZ);
            Assert.That(minZ, Is.EqualTo(5.3).Within(1e-6), "la popa a 5,3 m de la cámara");
            Assert.That(maxZ, Is.EqualTo(13.5).Within(1e-6), "la proa a 13,5");
            Assert.That(maxZ - minZ, Is.EqualTo(8.2).Within(1e-6));
            Assert.That(maxX, Is.EqualTo(-minX).Within(1e-6), "centrado en x 0");
            Assert.That(maxX * 2, Is.EqualTo(LakeArt.HullBeamGunwale).Within(0.01), "manga en la borda");
            Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.HullTriangleBudget));
            Assert.That(HullMesh.SectionZ(11), Is.EqualTo(9.5), "la sección del remero");
            var onCenterline = false;
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                onCenterline |= Math.Abs(mesh.X(i)) < 1e-6 && Math.Abs(mesh.Z(i) - 9.5) < 1e-5 && mesh.Y(i) > 0;
            }

            Assert.That(onCenterline, Is.True, "el punto a 9,5 m en la línea de crujía");
        }

        // WP-D2 (B6a-22, punto 5): la cubierta es blanca de borda a borda (fuera del cockpit); la línea de borda celeste queda en el
        // costado. Con la cubierta tendida a los vértices de la borda, el degradé celeste la cubría y el casco se leía como un palo celeste.
        [Test, Category("AC-SCN-01-08")]
        public void TheDeckIsWhiteAndTheStripeStaysOnTheSide()
        {
            var mesh = new MeshData(HullMesh.VertexCapacity, HullMesh.IndexCapacity);
            HullMesh.Build(mesh);
            var white = SceneColor.FromArgb(LakeArt.HullWhite);
            var cockpit = SceneColor.FromArgb(LakeArt.HullCockpit);
            var deckTriangles = 0;
            for (var t = 0; t < mesh.TriangleCount; t++)
            {
                var corners = new[] { mesh.Indices[3 * t], mesh.Indices[3 * t + 1], mesh.Indices[3 * t + 2] };
                var onDeck = false;
                foreach (var v in corners)
                {
                    // It. 6b (#85): la cubierta baja 0,03 m en la punta de la popa; se la reconoce por la normal hacia arriba en la crujía.
                    onDeck |= Math.Abs(mesh.X(v)) < 1e-6 && mesh.Normals[3 * v + 1] > 0.99f;
                }

                if (!onDeck)
                {
                    continue;
                }

                deckTriangles++;
                foreach (var v in corners)
                {
                    // Blanca (con el degradé hacia la popa de la It. 6b, #85) o el cockpit; nunca la franja celeste.
                    double r = mesh.Colors[4 * v], b = mesh.Colors[4 * v + 2];
                    var deck = HullMesh.DeckColor(mesh.Z(v));
                    var isWhite = Math.Abs(r - deck.R) < 1e-6 && Math.Abs(b - deck.B) < 1e-6;
                    var isCockpit = Math.Abs(r - cockpit.R) < 1e-6 && Math.Abs(b - cockpit.B) < 1e-6;
                    Assert.That(isWhite || isCockpit, Is.True, "un vértice de la cubierta con el color de la franja en z " + mesh.Z(v));
                }
            }

            Assert.That(deckTriangles, Is.EqualTo(4 * (LakeArt.HullSections - 1)), "la cubierta entera");
        }

        [Test, Category("AC-SCN-15-07")]
        public void TerrainChunksShareTheirEdges()
        {
            var a = new MeshData(TerrainChunkMesh.VertexCapacity(50, false), TerrainChunkMesh.IndexCapacity(50, false));
            var b = new MeshData(TerrainChunkMesh.VertexCapacity(50, false), TerrainChunkMesh.IndexCapacity(50, false));
            foreach (var side in new[] { -1, 1 })
            {
                TerrainChunkMesh.BuildSegment(EnvironmentId.Lake, side, 18, 1, false, 900, a);
                TerrainChunkMesh.BuildSegment(EnvironmentId.Lake, side, 19, 1, false, 900, b);
                var cols = TerrainChunkMesh.Columns(false);
                var lastRow = a.VertexCount - cols;
                for (var c = 0; c < cols; c++)
                {
                    Assert.That(Math.Abs(a.Y(lastRow + c) - b.Y(c)), Is.LessThan(0.001), "costura < 1 mm");
                    Assert.That(a.X(lastRow + c), Is.EqualTo(b.X(c)));
                    Assert.That(a.Z(lastRow + c), Is.EqualTo(b.Z(c)));
                }

                Assert.That(a.Z(0), Is.EqualTo(18 * 50 - 900).Within(1e-4), "coordenadas de cinta");
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void TheFarChunkHasNoSubmergedStripAndItsShoreIsLifted()
        {
            var rows = (int)Math.Ceiling((1500 - 450) / 8.0);
            var mesh = new MeshData(TerrainChunkMesh.VertexCapacity(rows, true), TerrainChunkMesh.IndexCapacity(rows, true));
            foreach (var side in new[] { -1, 1 })
            {
                TerrainChunkMesh.Build(EnvironmentId.Lake, side, 1450, 2500, 8, true, true, LakeArt.ForestOuterX, 1000, mesh);
                for (var i = 0; i < mesh.VertexCount; i++)
                {
                    Assert.That(mesh.Y(i), Is.GreaterThanOrEqualTo(LakeArt.FarShoreLift - 1e-6), "la orilla a +0,4 m (DT6a-18)");
                    Assert.That(Math.Abs(mesh.X(i)), Is.GreaterThanOrEqualTo(9.0 - 1e-6), "sin la franja sumergida");
                }
            }
        }

        [Test, Category("AC-SCN-19-01")]
        public void TheHorizonStripHas260Columns()
        {
            var mesh = new MeshData(HorizonStripMesh.VertexCapacity, HorizonStripMesh.IndexCapacity);
            HorizonStripMesh.Build(true, mesh);
            Assert.That(mesh.TriangleCount, Is.EqualTo(2 * LakeArt.StripColumns));
            Assert.That(mesh.X(0), Is.EqualTo(SceneMath.DegToRad(-65)).Within(1e-6));
            Assert.That(mesh.X(mesh.VertexCount - 1), Is.EqualTo(SceneMath.DegToRad(65)).Within(1e-6));
            var snowy = 0;
            for (var i = 1; i < mesh.VertexCount; i += 2)
            {
                snowy += mesh.Colors[i * 4] > 0.5f ? 1 : 0;
            }

            Assert.That(snowy, Is.GreaterThan(0), "la lejana con nieve");
            HorizonStripMesh.Build(false, mesh);
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                Assert.That(mesh.Colors[i * 4], Is.EqualTo(0f), "la cercana sin nieve");
            }
        }

        [Test, Category("AC-SCN-15-07")]
        public void SmallMeshesWithinTheirBudgets()
        {
            var mesh = new MeshData(400, 400);
            for (var v = 0; v < LakeArt.RockVariants; v++)
            {
                SmallMeshes.Rock(v, 0, mesh);
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.RockTriangleBudget[0]));
                mesh.Bounds(out _, out var minY, out _, out _, out var maxY, out _);
                Assert.That(maxY - minY, Is.InRange(0.7, 1.3), "a escala 1 (el alto va por instancia)");
                SmallMeshes.Rock(v, 1, mesh);
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.RockTriangleBudget[1]));
                SmallMeshes.ReedTuft(v, 0, mesh);
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.ReedTriangleBudget[0]));
                Assert.That(mesh.TriangleCount / 2, Is.GreaterThanOrEqualTo(LakeArt.ReedBladesMin), "de 10 a 14 hojas");
                SmallMeshes.ReedTuft(v, 1, mesh);
                Assert.That(mesh.TriangleCount, Is.LessThanOrEqualTo(LakeArt.ReedTriangleBudget[1]));
                SmallMeshes.GrassTuft(v, 0, mesh);
                Assert.That(mesh.TriangleCount, Is.EqualTo(LakeArt.GrassTriangleBudget[0]));
                SmallMeshes.GrassTuft(v, 1, mesh);
                Assert.That(mesh.TriangleCount, Is.EqualTo(LakeArt.GrassTriangleBudget[1]));
            }
        }

        private static bool Near(MeshData mesh, int i, in SceneColor c)
        {
            return Math.Abs(mesh.Colors[i * 4] - c.R) < 1e-5 && Math.Abs(mesh.Colors[i * 4 + 1] - c.G) < 1e-5 && Math.Abs(mesh.Colors[i * 4 + 2] - c.B) < 1e-5;
        }

        private static void Bounds(MeshData mesh, int start, int count, out double x0, out double y0, out double x1, out double y1)
        {
            x0 = y0 = double.MaxValue;
            x1 = y1 = double.MinValue;
            for (var k = start; k < start + count; k++)
            {
                var i = mesh.Indices[k];
                x0 = Math.Min(x0, mesh.X(i));
                y0 = Math.Min(y0, mesh.Y(i));
                x1 = Math.Max(x1, mesh.X(i));
                y1 = Math.Max(y1, mesh.Y(i));
            }
        }
    }
}
