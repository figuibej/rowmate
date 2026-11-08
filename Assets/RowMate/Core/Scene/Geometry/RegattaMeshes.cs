using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // Las mallas del Canal de regata (02-tech-design §2.6; 00-arte-entornos §4.3; AC-SCN-18-02 a -04). Las convenciones, las de
    // RiverMeshes; tribuna y torre se arman para su orilla del legacy (tribuna a la derecha, torre a la izquierda).

    // Tribuna (shore_painter.dart:236-242; DT8-03): 12 m a lo largo del canal (z −6..6) y 6 de fondo hacia +x desde el ancla (el
    // borde del agua), 4 gradas de 1,2 de alto y 1,5 de huella en colores alternados, techo de 0,5 a 5,3 sobre 5 columnas atrás,
    // en voladizo hacia el agua.
    public static class GrandstandMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 900 : 300;

        public static int IndexCapacity(int lod) => 3 * RegattaArt.GrandstandTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var half = RegattaArt.StandLength / 2;
            var tread = RegattaArt.StandDepth / RegattaArt.Steps;
            if (lod >= 2)
            {
                // De lejos, un bloque con el alto de la grada de arriba.
                MeshShapes.Box(mesh, 0, 0, -half, RegattaArt.StandDepth, RegattaArt.StepHeight * RegattaArt.Steps, half, RegattaArt.StandOdd, BuildingMesh.Smooth);
            }
            else
            {
                for (var i = 0; i < RegattaArt.Steps; i++)
                {
                    var color = i % 2 == 0 ? RegattaArt.StandEven : RegattaArt.StandOdd;
                    MeshShapes.Box(mesh, i * tread, 0, -half, RegattaArt.StandDepth, RegattaArt.StepHeight * (i + 1), half, color, BuildingMesh.Smooth, true,
                        lod == 0 ? 4 : 1);
                }
            }

            var roofBottom = RegattaArt.RoofY - RegattaArt.RoofThickness;
            MeshShapes.Box(mesh, -RegattaArt.RoofOverhang, roofBottom, -half - RegattaArt.RoofOverhang, RegattaArt.StandDepth + 0.3, RegattaArt.RoofY,
                half + RegattaArt.RoofOverhang, RegattaArt.StandRoof, BuildingMesh.Smooth, false, lod == 0 ? 2 : 1);
            if (lod <= 1)
            {
                // Las 5 columnas de atrás (desde lo alto de la última grada hasta el techo).
                for (var c = 0; c < 5; c++)
                {
                    var z = -half + RegattaArt.StandLength * c / 4.0;
                    MeshShapes.Box(mesh, RegattaArt.StandDepth, 0, z - 0.1, RegattaArt.StandDepth + 0.2, roofBottom, z + 0.1, RegattaArt.StandRoof,
                        BuildingMesh.Smooth);
                }
            }
        }
    }

    // Torre de llegada (shore_painter.dart:244-251): 3 × 3 × 10, banda #263238 de 7,8 a 9,0 (la sala de control: submalla 1, se
    // enciende), reloj de r 0,6 a 6,5 con dos caras (a la cámara, −z, y al canal, +x) y mástil de 1,5.
    public static class FinishTowerMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 500 : 200;

        public static int IndexCapacity(int lod) => 3 * RegattaArt.TowerTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var w = RegattaArt.TowerWidth / 2;
            MeshShapes.Box(mesh, -w, 0, -w, w, RegattaArt.TowerHeight, w, RegattaArt.Tower, BuildingMesh.Smooth, true, lod == 0 ? 3 : 1);
            if (lod <= 1)
            {
                MeshShapes.Cylinder(mesh, 0, 0, 0.05, RegattaArt.TowerHeight, RegattaArt.TowerHeight + RegattaArt.Antenna, lod == 0 ? 6 : 3, RegattaArt.Mast,
                    0.5);
                // Las dos caras del reloj: discos de 12 (LOD0) o 6 gajos, 2 cm afuera.
                Disk(mesh, 0, RegattaArt.ClockY, -w - 0.02, 0, 0, -1, lod == 0 ? 12 : 6);
                Disk(mesh, w + 0.02, RegattaArt.ClockY, 0, 1, 0, 0, lod == 0 ? 12 : 6);
            }

            mesh.BeginSubmesh();
            MeshShapes.Box(mesh, -w - 0.02, RegattaArt.BandBottom, -w - 0.02, w + 0.02, RegattaArt.BandTop, w + 0.02, RegattaArt.TowerBand, 1, true);
        }

        private static void Disk(MeshData mesh, double cx, double cy, double cz, double nx, double ny, double nz, int gores)
        {
            var r = RegattaArt.ClockRadius;
            for (var i = 0; i < gores; i++)
            {
                var a0 = 2 * Math.PI * i / gores;
                var a1 = 2 * Math.PI * (i + 1) / gores;
                if (nz < 0)
                {
                    // Mira a −z: antihorario en (x; y), como el frente de MeshShapes.Box.
                    MeshShapes.Triangle(mesh, cx, cy, cz, cx + r * Math.Cos(a0), cy + r * Math.Sin(a0), cz, cx + r * Math.Cos(a1), cy + r * Math.Sin(a1), cz,
                        nx, ny, nz, RegattaArt.Clock, 0.5);
                }
                else
                {
                    // Mira a +x: en (z; y).
                    MeshShapes.Triangle(mesh, cx, cy, cz, cx, cy + r * Math.Sin(a0), cz + r * Math.Cos(a0), cx, cy + r * Math.Sin(a1), cz + r * Math.Cos(a1),
                        nx, ny, nz, RegattaArt.Clock, 0.5);
                }
            }
        }
    }

    // Bandera (shore_painter.dart:253-265): mástil de 5 (Ø 0,1) y el banderín triangular del legacy hacia +x (de (0; 5) a
    // (1,6; 4,6) y (0; 4,0)) en una grilla de 8 × 3 (LOD0); u = 0 en el mástil, 1 en la punta (el vértice lo hace flamear).
    public static class FlagMesh
    {
        public static int VertexCapacity(int lod) => 120;

        public static int IndexCapacity(int lod) => 3 * RegattaArt.FlagTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            if (lod >= 2)
            {
                MeshShapes.Quad(mesh, -0.05, 0, 0, 0.05, 0, 0, 0.05, RegattaArt.FlagMast, 0, -0.05, RegattaArt.FlagMast, 0, 0, 0, -1, RegattaArt.MarkerPostTone,
                    0.5);
                MeshShapes.Triangle(mesh, 0, RegattaArt.FlagBottom, 0, RegattaArt.FlagLength, RegattaArt.FlagMast - 0.4, 0, 0, RegattaArt.FlagMast, 0, 0, 0, -1,
                    0xFFFFFFFF, 0.5);
                return;
            }

            MeshShapes.Cylinder(mesh, 0, 0, 0.05, 0, RegattaArt.FlagMast, lod == 0 ? 8 : 4, RegattaArt.MarkerPostTone, 0.5);
            // El paño: columnas en u (hacia la punta), filas en v; la punta colapsa al vértice (1,6; 4,6).
            var nu = lod == 0 ? 8 : 3;
            var nv = lod == 0 ? 3 : 1;
            var c = SceneColor.White;
            var first = mesh.VertexCount;
            for (var j = 0; j <= nv; j++)
            {
                var v = j / (double)nv;
                for (var i = 0; i <= nu; i++)
                {
                    var u = i / (double)nu;
                    var top = RegattaArt.FlagMast + (RegattaArt.FlagMast - 0.4 - RegattaArt.FlagMast) * u;
                    var bottom = RegattaArt.FlagBottom + (RegattaArt.FlagMast - 0.4 - RegattaArt.FlagBottom) * u;
                    mesh.AddVertex(RegattaArt.FlagLength * u, bottom + (top - bottom) * v, 0, 0, 0, -1, c.R, c.G, c.B, 0.5, u, v);
                }
            }

            for (var j = 0; j < nv; j++)
            {
                for (var i = 0; i < nu; i++)
                {
                    var a = first + j * (nu + 1) + i;
                    var b = a + 1;
                    var d = a + nu + 1;
                    var e = d + 1;
                    mesh.AddTriangle(a, d, b);
                    mesh.AddTriangle(b, d, e);
                }
            }
        }
    }

    // Cartel de distancia (shore_painter.dart:266-285): poste de 1,5 (Ø 0,1) y tablero de 1,8 × 1,0 × 0,06 de 1,5 a 2,5, de frente
    // a la cámara (−z), con borde negro de 0,04; el número lo dibuja MarkerTextRenderer (DistanceMarkerText, MarkerGlyphs).
    public static class MarkerMesh
    {
        public static int VertexCapacity(int lod) => 140;

        public static int IndexCapacity(int lod) => 3 * RegattaArt.MarkerTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var w = RegattaArt.MarkerBoardWidth / 2;
            var b0 = RegattaArt.MarkerBoardBottom;
            var b1 = b0 + RegattaArt.MarkerBoardHeight;
            if (lod >= 2)
            {
                MeshShapes.Quad(mesh, -w, b0, 0, w, b0, 0, w, b1, 0, -w, b1, 0, 0, 0, -1, RegattaArt.MarkerBoard, 0.5);
                return;
            }

            MeshShapes.Cylinder(mesh, 0, RegattaArt.MarkerBoardThickness, 0.05, 0, b0, lod == 0 ? 6 : 3, RegattaArt.MarkerPostTone, 0.5);
            MeshShapes.Box(mesh, -w, b0, 0, w, b1, RegattaArt.MarkerBoardThickness, RegattaArt.MarkerBoard, 0.5, false);
            if (lod == 0)
            {
                // El borde negro, 4 listones apenas delante del tablero.
                const double E = 0.04, F = -0.005;
                MeshShapes.Quad(mesh, -w, b0, F, w, b0, F, w, b0 + E, F, -w, b0 + E, F, 0, 0, -1, 0xFF111111, 0.5);
                MeshShapes.Quad(mesh, -w, b1 - E, F, w, b1 - E, F, w, b1, F, -w, b1, F, 0, 0, -1, 0xFF111111, 0.5);
                MeshShapes.Quad(mesh, -w, b0 + E, F, -w + E, b0 + E, F, -w + E, b1 - E, F, -w, b1 - E, F, 0, 0, -1, 0xFF111111, 0.5);
                MeshShapes.Quad(mesh, w - E, b0 + E, F, w, b0 + E, F, w, b1 - E, F, w - E, b1 - E, F, 0, 0, -1, 0xFF111111, 0.5);
            }
        }
    }

    // Álamo (vegetation.poplar, §4.3): columna de 6 lóbulos elipsoides de Ø 3 sobre un tronco, de 16 m (el alto de 14 a 18 va en
    // la escala y de la instancia); LOD2 = dos tablas cruzadas.
    public static class PoplarMesh
    {
        public const double Height = 16;

        public static int VertexCapacity(int lod) => lod == 0 ? 500 : 160;

        public static int IndexCapacity(int lod) => 3 * RegattaArt.PoplarTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            const uint Trunk = 0xFF5D4037, Leaves = 0xFF3F6B35;
            if (lod >= 2)
            {
                MeshShapes.Quad(mesh, -1.5, 0, 0, 1.5, 0, 0, 1.5, Height, 0, -1.5, Height, 0, 0, 0, -1, Leaves, 0);
                MeshShapes.Quad(mesh, 0, 0, 1.5, 0, 0, -1.5, 0, Height, -1.5, 0, Height, 1.5, -1, 0, 0, Leaves, 0);
                return;
            }

            MeshShapes.Cylinder(mesh, 0, 0, 0.25, 0, 3, lod == 0 ? 8 : 4, Trunk, 0);
            var lobes = lod == 0 ? 6 : 3;
            var span = (Height - 2.5) / lobes;
            for (var i = 0; i < lobes; i++)
            {
                var cy = 2.5 + span * (i + 0.5);
                var r = 1.5 * (1 - 0.35 * Math.Abs((i + 0.5) / lobes - 0.4));
                MeshShapes.Ellipsoid(mesh, 0, cy, 0, r, span * 0.75, r, lod == 0 ? 4 : 2, 8, Leaves, 0);
            }
        }
    }
}
