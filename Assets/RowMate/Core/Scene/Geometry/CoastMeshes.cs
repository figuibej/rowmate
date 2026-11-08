using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // Las mallas de la Costa (02-tech-design §2.6; 00-arte-entornos §3.3; AC-SCN-17-02 a -04). Las convenciones, las de
    // RiverMeshes; el acantilado y el faro se arman para la orilla IZQUIERDA (el agua hacia +x), la única que tiene objetos.

    // Acantilado (shore_painter.dart:202-215): la pared en x = 0 de 0 a 8 m (la escala del legacy va en la instancia), 28 m en z
    // (25 + 3 de solape), 7 m de fondo hacia −x con el borde de arriba en los 5 escalones del legacy (DartRandom(variante):
    // y = 8 + r·1,5 − 0,4·i en x = −1,4·i), la cara corrida ± 0,6 m por ruido, pasto arriba y la grieta oscura en la base.
    public static class CliffMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 1400 : 400;

        public static int IndexCapacity(int lod) => 3 * CoastArt.CliffTris[BuildingMesh.Clamp(lod)];

        // El alto del borde de arriba en el escalón i (0 = la cara, 1 a 5 hacia adentro), con el mismo orden de tiradas que el
        // legacy (una por escalón).
        public static void EdgeHeights(int seed, DartRandom rnd, Span<double> into6)
        {
            if (rnd == null)
            {
                throw new ArgumentNullException(nameof(rnd));
            }

            rnd.Reseed(seed);
            into6[0] = CoastArt.CliffHeight;
            for (var i = 1; i <= 5; i++)
            {
                into6[i] = CoastArt.CliffHeight + rnd.NextDouble() * CoastArt.CliffEdgeNoise - CoastArt.CliffEdgeRise * i;
            }
        }

        public static void Build(int seed, int lod, MeshData mesh, DartRandom rnd)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            Span<double> edge = stackalloc double[6];
            EdgeHeights(seed, rnd, edge);
            var cols = lod == 0 ? 28 : (lod == 1 ? 8 : 2);
            var rows = lod == 0 ? 8 : (lod == 1 ? 3 : 1);
            var len = CoastArt.CliffLength;
            var rock = SceneColor.FromArgb(CoastArt.CliffRock);
            var grass = SceneColor.FromArgb(CoastArt.CliffGrass);
            // La cara (x = 0 + ruido), mirando al agua (+x).
            var first = mesh.VertexCount;
            for (var j = 0; j <= rows; j++)
            {
                var y = CoastArt.CliffHeight * j / rows;
                for (var i = 0; i <= cols; i++)
                {
                    var z = len * i / cols;
                    var n = lod == 2 || j == rows ? 0 : (SceneHash.Value2(seed, z / 2.3, y / 1.7) * 2 - 1) * 0.6;
                    var c = j == rows ? grass : rock;
                    mesh.AddVertex(n, y, z, 1, 0, 0, c.R, c.G, c.B, 0, i / (double)cols, j / (double)rows);
                }
            }

            Grid(mesh, first, cols, rows);
            // Arriba: el borde en escalones hacia −x (pasto).
            for (var step = 0; step < 5; step++)
            {
                var x0 = -CoastArt.CliffStep * step;
                var x1 = -CoastArt.CliffStep * (step + 1);
                MeshShapes.Quad(mesh, x0, edge[step], 0, x0, edge[step], len, x1, edge[step + 1], len, x1, edge[step + 1], 0, 0, 1, 0, CoastArt.CliffGrass, 0,
                    1, lod == 0 ? 7 : 1);
            }

            // Los extremos en z (cerrados) y el fondo.
            var back = -CoastArt.CliffDepth;
            MeshShapes.Quad(mesh, back, 0, 0, 0, 0, 0, 0, edge[0], 0, back, edge[5], 0, 0, 0, -1, CoastArt.CliffRock, 0);
            MeshShapes.Quad(mesh, 0, 0, len, back, 0, len, back, edge[5], len, 0, edge[0], len, 0, 0, 1, CoastArt.CliffRock, 0);
            if (lod <= 1)
            {
                // La grieta de la base (0,6 × 1,2), en el medio.
                var zc = len / 2;
                MeshShapes.Quad(mesh, 0.62, 0, zc - CoastArt.CrackWidth / 2, 0.62, 0, zc + CoastArt.CrackWidth / 2, 0.62, CoastArt.CrackHeight,
                    zc + CoastArt.CrackWidth / 2, 0.62, CoastArt.CrackHeight, zc - CoastArt.CrackWidth / 2, 1, 0, 0, CoastArt.CliffCrack, 0);
            }
        }

        // La grilla de la cara (u = z, v = y), con el orden de MeshShapes.Quad para una cara que mira a +x.
        private static void Grid(MeshData mesh, int first, int cols, int rows)
        {
            for (var j = 0; j < rows; j++)
            {
                for (var i = 0; i < cols; i++)
                {
                    var a = first + j * (cols + 1) + i;
                    var b = a + 1;
                    var d = a + cols + 1;
                    var e = d + 1;
                    mesh.AddTriangle(a, d, b);
                    mesh.AddTriangle(b, d, e);
                }
            }
        }
    }

    // Faro (shore_painter.dart:217-227): torre troncocónica de r 1,1 a 0,9 y 9 m con 3 franjas rojas de 1 m (a 1–2, 4–5 y 7–8),
    // balcón a 9, linterna de 1,6 de 9 a 10,2 (submalla 1: el vidrio se enciende) y cúpula.
    public static class LighthouseMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 600 : 200;

        public static int IndexCapacity(int lod) => 3 * CoastArt.LighthouseTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var sides = lod == 0 ? 16 : (lod == 1 ? 8 : 4);
            var h = CoastArt.LighthouseHeight;
            double R(double y) => CoastArt.LighthouseRadius + (CoastArt.LighthouseTopRadius - CoastArt.LighthouseRadius) * y / h;
            var y0 = 0.0;
            if (lod <= 1)
            {
                foreach (var stripe in CoastArt.StripeBottoms)
                {
                    MeshShapes.Frustum(mesh, 0, 0, R(y0), R(stripe), y0, stripe, sides, CoastArt.LighthouseTower, 0.5);
                    MeshShapes.Frustum(mesh, 0, 0, R(stripe), R(stripe + 1), stripe, stripe + 1, sides, CoastArt.LighthouseStripe, 0.5);
                    y0 = stripe + 1;
                }
            }

            // LOD2: la torre de una pieza (las franjas no se distinguen a esa distancia).
            MeshShapes.Frustum(mesh, 0, 0, R(y0), R(h), y0, h, sides, CoastArt.LighthouseTower, 0.5);
            if (lod <= 1)
            {
                // El balcón: un aro de 1,3 de radio y 0,15 de alto.
                MeshShapes.Cylinder(mesh, 0, 0, 1.3, h, h + 0.15, sides, CoastArt.Lantern, 0.5);
            }

            mesh.BeginSubmesh();
            var lr = CoastArt.LanternWidth / 2;
            MeshShapes.Cylinder(mesh, 0, 0, lr, CoastArt.LanternBottom, CoastArt.LanternTop, lod == 2 ? 4 : 8, CoastArt.Lantern, 1);
            mesh.BeginSubmesh();
            MeshShapes.Frustum(mesh, 0, 0, lr, 0.05, CoastArt.LanternTop, CoastArt.LanternTop + 0.6, lod == 2 ? 4 : 8, CoastArt.Lantern, 0.5);
        }
    }

    // Sombrilla (shore_painter.dart:229-234): mástil de 2,2, tela de 8 gajos de r 0,9 (de 2,2 a 3,1; el color de la variante va
    // por instancia, la malla en blanco) y toalla de 1,2 × 0,6 de x 0,3 a 1,5.
    public static class UmbrellaMesh
    {
        public static int VertexCapacity(int lod) => 160;

        public static int IndexCapacity(int lod) => 3 * CoastArt.UmbrellaTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            // (8 gajos en LOD0 y LOD1, 3 en el LOD2.)
            if (lod <= 1)
            {
                MeshShapes.Cylinder(mesh, 0, 0, 0.02, 0, CoastArt.UmbrellaPole, lod == 0 ? 6 : 3, CoastArt.UmbrellaPoleTone, 0.5);
            }

            // La tela: un domo de 3 bandas (LOD0) desde el borde (r 0,9 a 2,2) hasta la punta (3,1), por arriba y por abajo.
            var bands = lod == 0 ? 3 : 1;
            var gores = lod == 2 ? 3 : 8;
            for (var b = 0; b < bands; b++)
            {
                var t0 = b / (double)bands;
                var t1 = (b + 1) / (double)bands;
                var r0 = CoastArt.UmbrellaRadius * Math.Cos(t0 * Math.PI / 2);
                var r1 = CoastArt.UmbrellaRadius * Math.Cos(t1 * Math.PI / 2);
                var ya = CoastArt.UmbrellaPole + (CoastArt.UmbrellaTop - CoastArt.UmbrellaPole) * Math.Sin(t0 * Math.PI / 2);
                var yb = CoastArt.UmbrellaPole + (CoastArt.UmbrellaTop - CoastArt.UmbrellaPole) * Math.Sin(t1 * Math.PI / 2);
                MeshShapes.Frustum(mesh, 0, 0, r0, r1, ya, yb, gores, 0xFFFFFFFF, 0.5);
            }

            if (lod <= 1)
            {
                MeshShapes.Quad(mesh, 0.3, 0.02, -CoastArt.TowelDepth / 2, 1.5, 0.02, -CoastArt.TowelDepth / 2, 1.5, 0.02, CoastArt.TowelDepth / 2, 0.3, 0.02,
                    CoastArt.TowelDepth / 2, 0, 1, 0, CoastArt.Towel, 0);
            }

        }
    }

    // Gaviota low-poly (prop.seagull): cuerpo, dos alas de 2 tramos (envergadura 1,1) y cola; ≤ 300 triángulos, un LOD. El
    // aleteo lo hace el vértice con SeagullInstance.Flap (el tramo de afuera de cada ala tiene v = 1).
    public static class SeagullMesh
    {
        public static int VertexCapacity => 120;

        public static int IndexCapacity => 3 * CoastArt.SeagullTris;

        public static void Build(MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            const uint White = 0xFFE8E8E4;
            MeshShapes.Ellipsoid(mesh, 0, 0, 0, 0.06, 0.06, 0.2, 3, 6, White, 0.5);
            var half = CoastArt.SeagullWingspan / 2;
            // El ala derecha y su espejo (el espejo invierte el orden para seguir mirando hacia arriba).
            MeshShapes.Quad(mesh, 0.05, 0, -0.08, half * 0.5, 0.03, -0.06, half * 0.5, 0.03, 0.08, 0.05, 0, 0.1, 0, 1, 0, White, 0.5);
            MeshShapes.Quad(mesh, half * 0.5, 0.03, -0.06, half, 0, -0.02, half, 0, 0.04, half * 0.5, 0.03, 0.08, 0, 1, 0, 0xFF2B2B2B, 0.5);
            MeshShapes.Quad(mesh, -0.05, 0, 0.1, -half * 0.5, 0.03, 0.08, -half * 0.5, 0.03, -0.06, -0.05, 0, -0.08, 0, 1, 0, White, 0.5);
            MeshShapes.Quad(mesh, -half * 0.5, 0.03, 0.08, -half, 0, 0.04, -half, 0, -0.02, -half * 0.5, 0.03, -0.06, 0, 1, 0, 0xFF2B2B2B, 0.5);

            MeshShapes.Triangle(mesh, -0.05, 0, 0.18, 0.05, 0, 0.18, 0, 0, 0.3, 0, 1, 0, White, 0.5);
        }
    }

    // La espuma de la orilla (§3.4): una tira de 50 m en z (un segmento) de `width` m de ancho sobre el agua, con v de 0 (agua) a
    // 1 (tierra) y u = z/50; el latido lo hace el shader Foam. ≤ 250 triángulos por segmento (≤ 2 000 en los 400 m).
    public static class FoamStripMesh
    {
        public const int Segments = 50;

        public static int VertexCapacity => 2 * (Segments + 1);

        public static int IndexCapacity => 3 * 2 * Segments;

        public static void Build(double width, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            MeshShapes.Quad(mesh, 0, 0.02, 0, 0, 0.02, ShoreEnvironment.SegmentLength, -width, 0.02, ShoreEnvironment.SegmentLength, -width, 0.02, 0, 0, 1, 0,
                0xFFF4F6F2, 0, Segments, 1);
        }
    }
}
