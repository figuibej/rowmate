using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // Las mallas del Río urbano (02-tech-design §2.6; 00-arte-entornos §2.3; AC-SCN-20-03, -16-03, -16-04). Convenciones de todas
    // las mallas de la It. 8: origen local en el ancla del legacy a la altura del suelo (y = 0 = el pie; lo sube la colocación),
    // −z mira a la cámara, metros a escala 1 (la escala del legacy y los recortes de colocación van en la instancia, por eje).
    // Las casas se arman para la orilla DERECHA (el agua hacia −x); la izquierda las espeja en x. Color de vértice: rgb = el color
    // de referencia del legacy (antes de la luz de la hora), a = material (0 rugoso, 0,5 liso, 1 emisivo de noche).
    // Triángulos por LOD con los topes de RiverArt.

    // Edificio (shore_painter.dart:133-148): 6 de frente (x −3..3) × 10 de fondo (z 0..10), pisos de 3 m, zócalo de 0,6 más
    // oscuro, cornisa, y techo plano con pretil o mansarda de pizarra. Las ventanas las dibuja el shader Facade con la máscara
    // de FacadeWindows; el LOD0 suma los alféizares (3 por piso al frente, 5 al río).
    public static class BuildingMesh
    {
        public const double Rough = 0, Smooth = 0.5;

        public static int VertexCapacity(int lod) => lod == 0 ? 3000 : (lod == 1 ? 600 : 80);

        public static int IndexCapacity(int lod) => 3 * RiverArt.BuildingTris[Clamp(lod)];

        public static void Build(int floors, bool mansard, int tone, int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var w = RiverArt.BuildingWidth / 2;
            var d = RiverArt.BuildingDepth;
            var h = floors * RiverArt.FloorHeight;
            var color = RiverArt.BuildingTones[((tone % 3) + 3) % 3];
            var plinth = Scale(color, RiverArt.PlinthShade);
            if (lod >= 2)
            {
                MeshShapes.Box(mesh, -w, 0, 0, w, h, d, color, Rough);
                if (mansard)
                {
                    Mansard(mesh, w, h, d, 0);
                }

                return;
            }

            MeshShapes.Box(mesh, -w, 0, 0, w, RiverArt.PlinthHeight, d, plinth, Rough);
            MeshShapes.Box(mesh, -w, RiverArt.PlinthHeight, 0, w, h, d, color, Rough, true, lod == 0 ? 2 : 1);
            // La cornisa, apenas afuera del muro.
            var o = RiverArt.CorniceOut;
            MeshShapes.Box(mesh, -w - o, h - RiverArt.CorniceHeight, -o, w + o, h, d + o, Scale(color, 1.08), Smooth, false);
            if (mansard)
            {
                Mansard(mesh, w, h, d, lod);
            }
            else
            {
                // El pretil: cuatro muretes de 0,2 de espesor.
                var p = RiverArt.ParapetHeight;
                const double T = 0.2;
                MeshShapes.Box(mesh, -w, h, 0, w, h + p, T, color, Rough);
                MeshShapes.Box(mesh, -w, h, d - T, w, h + p, d, color, Rough);
                MeshShapes.Box(mesh, -w, h, T, -w + T, h + p, d - T, color, Rough);
                MeshShapes.Box(mesh, w - T, h, T, w, h + p, d - T, color, Rough);
            }

            if (lod == 0)
            {
                // Los alféizares: al frente x = −2,4 + 1,8·i (+0,5 del centro de la ventana), al río 5 repartidas en el fondo.
                for (var f = 0; f < floors; f++)
                {
                    var sill = h - RiverArt.WindowSill - f * RiverArt.FloorHeight - RiverArt.WindowHeight;
                    for (var i = 0; i < RiverArt.WindowsPerFloor; i++)
                    {
                        var x0 = RiverArt.WindowX0 + i * RiverArt.WindowStepX;
                        MeshShapes.Box(mesh, x0 - 0.05, sill - 0.08, -0.12, x0 + RiverArt.WindowWidth + 0.05, sill, 0, 0xFFB0A898, Smooth);
                    }

                    for (var i = 0; i < RiverArt.RiverWindowsPerFloor; i++)
                    {
                        var zc = d * (i + 0.5) / RiverArt.RiverWindowsPerFloor;
                        MeshShapes.Box(mesh, -w - 0.12, sill - 0.08, zc - 0.55, -w, sill, zc + 0.55, 0xFFB0A898, Smooth);
                    }
                }
            }
        }

        // La mansarda: cuatro faldones de pizarra de 2,2 m con el 60 % del ancho arriba (techo plano chico encima).
        private static void Mansard(MeshData mesh, double w, double h, double d, int lod)
        {
            var top = h + RiverArt.MansardHeight;
            var inset = 0.9;
            var c = RiverArt.MansardSlate;
            MeshShapes.Quad(mesh, -w, h, 0, w, h, 0, w - inset, top, inset, -w + inset, top, inset, 0, 0.6, -1, c, Smooth);
            MeshShapes.Quad(mesh, w, h, d, -w, h, d, -w + inset, top, d - inset, w - inset, top, d - inset, 0, 0.6, 1, c, Smooth);
            MeshShapes.Quad(mesh, -w, h, d, -w, h, 0, -w + inset, top, inset, -w + inset, top, d - inset, -1, 0.6, 0, c, Smooth);
            MeshShapes.Quad(mesh, w, h, 0, w, h, d, w - inset, top, d - inset, w - inset, top, inset, 1, 0.6, 0, c, Smooth);
            MeshShapes.Quad(mesh, -w + inset, top, inset, w - inset, top, inset, w - inset, top, d - inset, -w + inset, top, d - inset, 0, 1, 0, c, Smooth);
        }

        internal static uint Scale(uint argb, double k)
        {
            var c = SceneColor.FromArgb(argb);
            uint Ch(double v) => (uint)Math.Min(255, Math.Max(0, (int)(v * k * 255 + 0.5)));
            return 0xFF000000u | (Ch(c.R) << 16) | (Ch(c.G) << 8) | Ch(c.B);
        }

        internal static int Clamp(int lod) => lod < 0 ? 0 : (lod > 2 ? 2 : lod);
    }

    // Club de remo (shore_painter.dart:150-162): 8 de frente × 12 de fondo, paredes de 4, techo a dos aguas hasta 6 con el hastial
    // a la cámara y alero de 0,5; franja #1565C0 de 3,6 a 4,0 en todo el perímetro; portón de 3 × 2,8 hacia el río (−x) en el
    // medio del fondo (submalla 1: se ilumina de noche).
    public static class BoathouseMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 1600 : (lod == 1 ? 500 : 80);

        public static int IndexCapacity(int lod) => 3 * RiverArt.BoathouseTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var w = RiverArt.BoathouseWidth / 2;
            var d = RiverArt.BoathouseDepth;
            var wall = RiverArt.BoathouseWall;
            var ridge = RiverArt.BoathouseRidge;
            var e = RiverArt.BoathouseEave;
            var n = lod == 0 ? 3 : 1;
            MeshShapes.Box(mesh, -w, 0, 0, w, wall, d, RiverArt.BoathouseWallTone, BuildingMesh.Rough, true, n);
            // Los dos hastiales.
            MeshShapes.Triangle(mesh, -w, wall, 0, w, wall, 0, 0, ridge, 0, 0, 0, -1, RiverArt.BoathouseWallTone, BuildingMesh.Rough);
            MeshShapes.Triangle(mesh, w, wall, d, -w, wall, d, 0, ridge, d, 0, 0, 1, RiverArt.BoathouseWallTone, BuildingMesh.Rough);
            // El techo con alero.
            var slope = (ridge - wall) / w;
            var eaveY = wall - e * slope;
            MeshShapes.Quad(mesh, -w - e, eaveY, -e, 0, ridge, -e, 0, ridge, d + e, -w - e, eaveY, d + e, -slope, 1, 0, RiverArt.BoathouseRoof,
                BuildingMesh.Smooth, n, n);
            MeshShapes.Quad(mesh, 0, ridge, -e, w + e, eaveY, -e, w + e, eaveY, d + e, 0, ridge, d + e, slope, 1, 0, RiverArt.BoathouseRoof,
                BuildingMesh.Smooth, n, n);
            if (lod <= 1)
            {
                // La franja azul, 2 cm afuera de las paredes.
                const double O = 0.02;
                MeshShapes.Box(mesh, -w - O, RiverArt.StripeBottom, -O, w + O, RiverArt.StripeTop, d + O, RiverArt.BoathouseStripe, BuildingMesh.Smooth,
                    true);
            }

            // El portón hacia el río (submalla aparte).
            mesh.BeginSubmesh();
            var gz = d / 2;
            MeshShapes.Quad(mesh, -w - 0.03, 0, gz + RiverArt.GateWidth / 2, -w - 0.03, 0, gz - RiverArt.GateWidth / 2, -w - 0.03, RiverArt.GateHeight,
                gz - RiverArt.GateWidth / 2, -w - 0.03, RiverArt.GateHeight, gz + RiverArt.GateWidth / 2, -1, 0, 0, RiverArt.BoathouseGate, 1);
        }
    }

    // Farol (shore_painter.dart:191-200): base de 0,3, poste octogonal de 4,0 (Ø 0,12 → 0,08) y globo de r 0,30 a 4,1 (submalla
    // 1: se enciende).
    public static class LampMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 200 : 80;

        public static int IndexCapacity(int lod) => 3 * RiverArt.LampTris[BuildingMesh.Clamp(lod)];

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            if (lod >= 2)
            {
                // Dos tablas cruzadas con el poste.
                MeshShapes.Quad(mesh, -0.06, 0, 0, 0.06, 0, 0, 0.06, RiverArt.LampGlobeY + 0.3, 0, -0.06, RiverArt.LampGlobeY + 0.3, 0, 0, 0, -1,
                    RiverArt.LampPost, 0.5);
                MeshShapes.Quad(mesh, 0, 0, 0.06, 0, 0, -0.06, 0, RiverArt.LampGlobeY + 0.3, -0.06, 0, RiverArt.LampGlobeY + 0.3, 0.06, -1, 0, 0,
                    RiverArt.LampPost, 0.5);
                return;
            }

            var sides = lod == 0 ? 8 : 4;
            if (lod == 0)
            {
                MeshShapes.Box(mesh, -0.12, 0, -0.12, 0.12, RiverArt.LampBaseHeight, 0.12, RiverArt.LampPost, 0.5);
            }

            MeshShapes.Frustum(mesh, 0, 0, RiverArt.LampPostRadiusBottom, RiverArt.LampPostRadiusTop, 0, RiverArt.LampHeight, sides, RiverArt.LampPost, 0.5);
            mesh.BeginSubmesh();
            MeshShapes.Ellipsoid(mesh, 0, RiverArt.LampGlobeY, 0, RiverArt.LampGlobeRadius, RiverArt.LampGlobeRadius, RiverArt.LampGlobeRadius,
                lod == 0 ? 6 : 4, lod == 0 ? 8 : 4, RiverArt.LampGlobeDay, 1);
        }
    }

    // Boya de carril (shore_painter.dart:184-189): esfera de r 0,25 con el centro a 0,2 sobre el agua; el color es el de la
    // variante (seed mod 3) y va por instancia (la malla, blanca); la franja de flotación por shader.
    public static class LaneBuoyMesh
    {
        public static int VertexCapacity(int lod) => 120;

        public static int IndexCapacity(int lod) => 3 * RegattaArt.BuoyTris[BuildingMesh.Clamp(lod)];

        // neck = true: la boya Albano de la regata (cuello de 0,1 × 0,15 bajo la esfera).
        public static void Build(int lod, bool neck, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var bands = lod == 0 ? 5 : (lod == 1 ? 2 : 1);
            var slices = lod == 2 ? 4 : 8;
            MeshShapes.Ellipsoid(mesh, 0, RiverArt.BuoyCenterY, 0, RiverArt.BuoyRadius, RiverArt.BuoyRadius, RiverArt.BuoyRadius, bands, slices,
                0xFFFFFFFF, 0.5);
            if (neck && lod == 0)
            {
                MeshShapes.Cylinder(mesh, 0, 0, 0.05, -0.1, RiverArt.BuoyCenterY - RiverArt.BuoyRadius + 0.05, 8, 0xFFFFFFFF, 0.5);
            }
        }
    }

    // El muro y el malecón de un segmento (prop.quayWall, §2.1): la cara plomo en x = 9 (de −0,5 a 1,20) y el remate de piedra
    // clara (de 8,92 a 9,35, de 1,20 a 1,35), 50 m en z, para la orilla derecha; desagües cada 10 m en el LOD0.
    public static class QuayWallMesh
    {
        public const int MaxTris = 400;

        public static int VertexCapacity => 400;

        public static int IndexCapacity => 3 * MaxTris;

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var x = RiverArt.WallX;
            var len = ShoreEnvironment.SegmentLength;
            var n = lod == 0 ? 10 : 1;
            MeshShapes.Quad(mesh, x, -RiverArt.WallUnderwater, len, x, -RiverArt.WallUnderwater, 0, x, RiverArt.QuayHeight, 0, x, RiverArt.QuayHeight, len,
                -1, 0, 0, RiverArt.WallStone, 0, n, 1);
            MeshShapes.Box(mesh, x - RiverArt.CopingOverhang, RiverArt.QuayHeight, 0, x + RiverArt.CopingWidth, RiverArt.QuayHeight + RiverArt.CopingHeight,
                len, RiverArt.CopingStone, 0, false);
            if (lod == 0)
            {
                for (var i = 0; i < 5; i++)
                {
                    var z = 5 + i * 10.0;
                    MeshShapes.Box(mesh, x - 0.05, 0.25, z - 0.15, x, 0.45, z + 0.15, 0xFF3A3A3A, 0);
                }
            }
        }
    }

    // El puente de piedra (shore_painter.dart:164-182; AC-SCN-16-03; DT8-19 R4): la cara del legacy con el arco (la Bézier de
    // (±8,5; 1,5) con el control en (0; 8,5): la clave a 5,0 m), pilares de ±8,5 a ±9,5, el tablero de 24 m entre 6,0 y 7,2, el
    // parapeto hasta 7,7; 10 m de profundidad (z 0..10); estribos hasta |x| 12,5 sobre el arco lateral de 3 m del paseo.
    public static class BridgeMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 4000 : 1500;

        public static int IndexCapacity(int lod) => 3 * RiverArt.BridgeTris[BuildingMesh.Clamp(lod)];

        // El punto del arco en t ∈ [0; 1] (la Bézier cuadrática del legacy).
        public static void ArchPoint(double t, out double x, out double y)
        {
            var u = 1 - t;
            x = u * u * -RiverArt.BridgeSpringX + 2 * u * t * 0 + t * t * RiverArt.BridgeSpringX;
            y = u * u * RiverArt.BridgeSpringY + 2 * u * t * RiverArt.BridgeCrownControlY + t * t * RiverArt.BridgeSpringY;
        }

        public static void Build(int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var d = RiverArt.BridgeDepth;
            var top = RiverArt.DeckBottom;
            var segs = lod == 0 ? 24 : (lod == 1 ? 12 : 6);
            // Las dos caras con el arco recortado: franjas verticales del arco a la línea de 6,0; y el intradós.
            for (var i = 0; i < segs; i++)
            {
                ArchPoint(i / (double)segs, out var x0, out var y0);
                ArchPoint((i + 1) / (double)segs, out var x1, out var y1);
                MeshShapes.Quad(mesh, x0, y0, 0, x1, y1, 0, x1, top, 0, x0, top, 0, 0, 0, -1, RiverArt.BridgeFace, 0);
                MeshShapes.Quad(mesh, x1, y1, d, x0, y0, d, x0, top, d, x1, top, d, 0, 0, 1, RiverArt.BridgeFace, 0);
                MeshShapes.Quad(mesh, x1, y1, 0, x0, y0, 0, x0, y0, d, x1, y1, d, 0, -1, 0, RiverArt.BridgeFace, 0, 1, lod == 0 ? 2 : 1);
            }

            for (var s = -1; s <= 1; s += 2)
            {
                // Los pilares (del agua a 1,5) y el tímpano sobre el pilar hasta 6,0.
                Box(mesh, s, RiverArt.BridgeSpringX, RiverArt.BridgePierOuterX, -RiverArt.WallUnderwater, RiverArt.BridgeSpringY, 0, d, RiverArt.BridgePier);
                Box(mesh, s, RiverArt.BridgeSpringX, RiverArt.BridgePierOuterX, RiverArt.BridgeSpringY, top, 0, d, RiverArt.BridgeFace);
                // El estribo sobre el arco del paseo (9,5 a 12,5, de 4,2 a 6,0) y el cierre de 12 a 12,5 del malecón al tablero.
                Box(mesh, s, RiverArt.BridgePierOuterX, RiverArt.DeckHalfWidth, RiverArt.QuayHeight + RiverArt.BridgeSideArch, top, 0, d, RiverArt.BridgeFace);
                Box(mesh, s, RiverArt.DeckHalfWidth, RiverArt.BridgeEndX, RiverArt.QuayHeight, RiverArt.DeckTop, 0, d, RiverArt.BridgeFace);
            }

            // El tablero y los dos parapetos (al frente y al fondo).
            MeshShapes.Box(mesh, -RiverArt.DeckHalfWidth, top, 0, RiverArt.DeckHalfWidth, RiverArt.DeckTop, d, RiverArt.BridgeDeck, 0, false, lod == 0 ? 4 : 1);
            MeshShapes.Box(mesh, -RiverArt.DeckHalfWidth, RiverArt.DeckTop, 0, RiverArt.DeckHalfWidth, RiverArt.ParapetTop, 0.3, RiverArt.BridgeParapet, 0);
            MeshShapes.Box(mesh, -RiverArt.DeckHalfWidth, RiverArt.DeckTop, d - 0.3, RiverArt.DeckHalfWidth, RiverArt.ParapetTop, d, RiverArt.BridgeParapet, 0);
        }

        private static void Box(MeshData mesh, int side, double ax0, double ax1, double y0, double y1, double z0, double z1, uint color)
        {
            var minX = side < 0 ? -ax1 : ax0;
            var maxX = side < 0 ? -ax0 : ax1;
            MeshShapes.Box(mesh, minX, y0, z0, maxX, y1, z1, color, 0);
        }
    }

    // Una manzana de relleno (§2.2 M): una caja de 1 × 1 × 1 (la escalan el ancho, el alto y el frente de la instancia) con un
    // tanque en el techo; un solo LOD de ≤ 24 triángulos. Ventanas por Facade.
    public static class CityBlockMesh
    {
        public static int VertexCapacity => 64;

        public static int IndexCapacity => 3 * 24;

        public static void Build(MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            MeshShapes.Box(mesh, -0.5, 0, -0.5, 0.5, 1, 0.5, 0xFFFFFFFF, BuildingMesh.Rough);
            MeshShapes.Box(mesh, 0.1, 1, 0.1, 0.25, 1.06, 0.25, 0xFF5A5F66, BuildingMesh.Rough);
        }
    }
}
