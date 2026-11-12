using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // La cabaña del legacy en 3D (shore_painter.dart:106-123; 04-design-notes §7.3; AC-SCN-15-06). Origen local: el ancla del
    // legacy (el centro del frente, que mira a la cámara) a la altura del piso; x de −2 a 2, la profundidad hacia +z, techo a
    // dos aguas con la cumbrera a lo largo de z hasta 4,2 m y alero de 0,4 m a los costados y 0,5 m al frente; puerta de 0,8 × 1,4,
    // ventana de 0,8 × 0,7 (de x +0,7 a +1,5 y de y 1,3 a 2,0) en una submalla aparte (se enciende), zócalo de piedra hacia abajo.
    // Color de vértice: rgb = el color de referencia (antes de la luz de la hora), a = material (0 tablas, 0,5 liso).
    // LOD 0 ≤ 1 200 triángulos, LOD 1 ≤ 300.
    public static class CabinMesh
    {
        public const double Wood = 0;
        public const double Plain = 0.5;

        public static int VertexCapacity(int lod) => lod == 0 ? 1400 : 400;

        public static int IndexCapacity(int lod) => lod == 0 ? 3 * 1200 : 3 * 300;

        public static void Build(double depth, double plinthDepth, int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var n = lod == 0 ? 6 : 1;
            var hw = LakeArt.CabinFrontWidth / 2;
            var wall = LakeArt.CabinWallHeight;
            var ridge = LakeArt.CabinRidgeHeight;
            // Cuerpo (sin la cara de abajo).
            MeshShapes.Box(mesh, -hw, 0, 0, hw, wall, depth, LakeArt.CabinWood, Wood, true, n);
            // Los dos hastiales (frente y fondo), triángulos de la pared hasta la cumbrera.
            Gable(mesh, hw, wall, ridge, 0, -1);
            Gable(mesh, hw, wall, ridge, depth, 1);
            // El techo: dos aguas con alero.
            var eave = LakeArt.CabinEaveSide;
            var front = -LakeArt.CabinEaveFront;
            var back = depth + LakeArt.CabinEaveFront;
            var slope = (ridge - wall) / hw;
            var eaveY = wall - eave * slope;
            MeshShapes.Quad(mesh, -hw - eave, eaveY, front, 0, ridge, front, 0, ridge, back, -hw - eave, eaveY, back, -slope, 1, 0, LakeArt.CabinRoof,
                Plain, n, n);
            MeshShapes.Quad(mesh, 0, ridge, front, hw + eave, eaveY, front, hw + eave, eaveY, back, 0, ridge, back, slope, 1, 0, LakeArt.CabinRoof,
                Plain, n, n);
            // El zócalo de piedra hasta el terreno.
            if (plinthDepth > 0)
            {
                MeshShapes.Box(mesh, -hw, -plinthDepth, 0, hw, 0, depth, LakeArt.CabinPlinth, Plain, true, lod == 0 ? 2 : 1);
            }

            // La puerta, apenas delante del frente.
            const double Inset = 0.02;
            MeshShapes.Quad(mesh, -LakeArt.DoorWidth / 2, 0, -Inset, LakeArt.DoorWidth / 2, 0, -Inset, LakeArt.DoorWidth / 2, LakeArt.DoorHeight, -Inset,
                -LakeArt.DoorWidth / 2, LakeArt.DoorHeight, -Inset, 0, 0, -1, LakeArt.CabinDoor, Plain);
            // El marco de la ventana (4 cm), claro.
            const double F = 0.04;
            double l = LakeArt.WindowLeft, r = LakeArt.WindowRight, b = LakeArt.WindowBottom, t = LakeArt.WindowTop;
            MeshShapes.Quad(mesh, l - F, b - F, -Inset, r + F, b - F, -Inset, r + F, b, -Inset, l - F, b, -Inset, 0, 0, -1, LakeArt.CabinFrame, Plain);
            MeshShapes.Quad(mesh, l - F, t, -Inset, r + F, t, -Inset, r + F, t + F, -Inset, l - F, t + F, -Inset, 0, 0, -1, LakeArt.CabinFrame, Plain);
            MeshShapes.Quad(mesh, l - F, b, -Inset, l, b, -Inset, l, t, -Inset, l - F, t, -Inset, 0, 0, -1, LakeArt.CabinFrame, Plain);
            MeshShapes.Quad(mesh, r, b, -Inset, r + F, b, -Inset, r + F, t, -Inset, r, t, -Inset, 0, 0, -1, LakeArt.CabinFrame, Plain);
            // La ventana: su propia submalla (el color de día; de noche el shader la enciende con #FFE082).
            mesh.BeginSubmesh();
            MeshShapes.Quad(mesh, l, b, -Inset - 0.005, r, b, -Inset - 0.005, r, t, -Inset - 0.005, l, t, -Inset - 0.005, 0, 0, -1, LakeArt.WindowDay, Plain);
        }

        private static void Gable(MeshData mesh, double hw, double wall, double ridge, double z, double nz)
        {
            var c = SceneColor.FromArgb(LakeArt.CabinWood);
            var a = mesh.AddVertex(-hw, wall, z, 0, 0, nz, c.R, c.G, c.B, Wood, 0, 0);
            var b = mesh.AddVertex(hw, wall, z, 0, 0, nz, c.R, c.G, c.B, Wood, 1, 0);
            var top = mesh.AddVertex(0, ridge, z, 0, 0, nz, c.R, c.G, c.B, Wood, 0.5, 1);
            if (nz < 0)
            {
                mesh.AddTriangle(a, top, b);
            }
            else
            {
                mesh.AddTriangle(b, top, a);
            }
        }
    }

    // El muelle del legacy en 3D (shore_painter.dart:125-131; 04-design-notes §7.3; DT6a-08): el tablero de 1,6 m a 0,40 m de x −6
    // hasta la playa (las tablas de 0,20 m las dibuja el shader), los pilotes en pares (x −8,7, −7,5 y −6,3) hasta el fondo. Origen
    // local: el ancla del legacy (−9; z₀ + 25) a nivel del agua. LOD 0 ≤ 600 triángulos, LOD 1 ≤ 150.
    public static class PierMesh
    {
        public static int VertexCapacity(int lod) => lod == 0 ? 500 : 200;

        public static int IndexCapacity(int lod) => lod == 0 ? 3 * 600 : 3 * 150;

        public static void Build(in PierLayout layout, int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var from = layout.DeckFromX - layout.AnchorX;
            var to = layout.DeckToX - layout.AnchorX;
            var half = LakeArt.PierDeckWidth / 2;
            MeshShapes.Box(mesh, from, layout.DeckY - 0.1, -half, to, layout.DeckY, half, LakeArt.PierDeck, CabinMesh.Wood, false, lod == 0 ? 3 : 1);
            var sides = lod == 0 ? 12 : 5;
            var piles = LakeArt.PierPileX;
            for (var i = 0; i < piles.Length; i++)
            {
                var x = piles[i] - layout.AnchorX;
                var bottom = layout.PileBottom(i);
                MeshShapes.Cylinder(mesh, x, -LakeArt.PierPileZOffset, LakeArt.PierPileRadius, bottom, layout.DeckY - 0.1, sides, LakeArt.PierPile, CabinMesh.Plain);
                MeshShapes.Cylinder(mesh, x, LakeArt.PierPileZOffset, LakeArt.PierPileRadius, bottom, layout.DeckY - 0.1, sides, LakeArt.PierPile, CabinMesh.Plain);
            }
        }
    }

    // El casco provisorio (ART_BIBLE §7.8; 04-design-notes §9; AC-SCN-01-08): un loft de 24 secciones de z 5,3 (popa) a 13,5 (proa),
    // centrado en x 0, con la sección 11 exactamente en z 9,5 (el punto del remero, en la línea de crujía); manga de 0,29 m en la
    // flotación y 0,33 en la borda, francobordo de 0,10 m. Color de vértice: blanco arriba, gris en la obra viva, la línea de borda
    // celeste y el cockpit oscuro de z 9,05 a 10,45 (It. 7, DA7-02). ≤ 6 000 triángulos. It. 6b (#85): la cubierta con el degradé hacia
    // la popa y los últimos 0,6 m afinados y más bajos (SternBeamFactor, SternDrop, DeckColor).
    public static class HullMesh
    {
        private const int Profile = 12;

        public static int VertexCapacity => LakeArt.HullSections * (Profile + 4);

        public static int IndexCapacity => 3 * (2 * (LakeArt.HullSections - 1) * Profile + 4 * (LakeArt.HullSections - 1));

        // La z de la sección i: 12 tramos de la popa al remero y 12 del remero a la proa.
        public static double SectionZ(int i)
        {
            var mid = LakeArt.HullSections / 2 - 1;
            return i <= mid
                ? LakeArt.HullStern + i * (SceneCamera.BoatZ - LakeArt.HullStern) / mid
                : SceneCamera.BoatZ + (i - mid) * (LakeArt.HullBow - SceneCamera.BoatZ) / (LakeArt.HullSections - 1 - mid);
        }

        // La manga natural del casco en la borda (m) a esa z (el afinado del loft, sin el de la popa).
        public static double NaturalGunwaleBeam(double z)
        {
            return LakeArt.HullBeamGunwale * Taper(z);
        }

        // El factor de la manga en los últimos 0,6 m: la borda va lineal de la manga natural a 0,6 m de la punta a 0,06 m en la punta.
        public static double SternBeamFactor(double z)
        {
            var end = LakeArt.HullStern + LakeArt.HullSternTaperLength;
            if (z >= end)
            {
                return 1;
            }

            var t = SceneMath.Clamp01((z - LakeArt.HullStern) / LakeArt.HullSternTaperLength);
            var target = SceneMath.Lerp(LakeArt.HullSternTipBeam, NaturalGunwaleBeam(end), t);
            return Math.Min(1, target / NaturalGunwaleBeam(z));
        }

        // Cuánto baja la cubierta: 0,03 m en la punta, 0 a 0,6 m de ella.
        public static double SternDrop(double z)
        {
            var t = SceneMath.Clamp01((z - LakeArt.HullStern) / LakeArt.HullSternTaperLength);
            return LakeArt.HullSternDeckDrop * (1 - t);
        }

        // El color de la cubierta fuera del cockpit: #F5F5F5 hasta 2,5 m de la popa y lineal a #C9D1D6 en la punta.
        public static SceneColor DeckColor(double z)
        {
            var t = SceneMath.Clamp01((LakeArt.HullStern + LakeArt.HullDeckGradientLength - z) / LakeArt.HullDeckGradientLength);
            return SceneColor.Lerp(SceneColor.FromArgb(LakeArt.HullWhite), SceneColor.FromArgb(LakeArt.HullDeckStern), t);
        }

        private static double Taper(double z)
        {
            var s = (z - LakeArt.HullStern) / (LakeArt.HullBow - LakeArt.HullStern);
            return Math.Pow(Math.Sin(Math.PI * Math.Min(1, s * 0.85 + 0.075)), 0.55);
        }

        public static void Build(MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var sections = LakeArt.HullSections;
            var white = SceneColor.FromArgb(LakeArt.HullWhite);
            var bottom = SceneColor.FromArgb(LakeArt.HullBottom);
            var stripe = SceneColor.FromArgb(LakeArt.HullStripe);
            var cockpit = SceneColor.FromArgb(LakeArt.HullCockpit);
            for (var i = 0; i < sections; i++)
            {
                var z = SectionZ(i);
                var s = (z - LakeArt.HullStern) / (LakeArt.HullBow - LakeArt.HullStern);
                // Afinado en las puntas: más fino en la proa que en la popa; en los últimos 0,6 m, además, el de la popa (It. 6b).
                var taper = Taper(z);
                var stern = SternBeamFactor(z);
                var wl = LakeArt.HullBeamWaterline / 2 * taper * stern;
                var gw = LakeArt.HullBeamGunwale / 2 * taper * stern;
                var freeboard = LakeArt.HullFreeboard - SternDrop(z);
                for (var j = 0; j <= Profile; j++)
                {
                    // De la borda izquierda, por la quilla, a la borda derecha: un perfil en U.
                    var a = Math.PI * j / Profile;
                    var side = -Math.Cos(a);
                    var down = Math.Sin(a);
                    var x = side * (wl + (gw - wl) * (1 - down));
                    var y = freeboard - (freeboard + LakeArt.HullDraft) * down * taper;
                    var isCockpit = LakeArt.IsCockpit(z) && (j == 0 || j == Profile);
                    var below = y < 0.0;
                    var c = isCockpit ? cockpit : (j == 0 || j == Profile ? stripe : (below ? bottom : white));
                    mesh.AddVertex(x, y, z, side, -down * 0.6, 0, c.R, c.G, c.B, 0, j / (double)Profile, s);
                }
            }

            // El costado: el perfil en U de cada sección.
            for (var i = 0; i < sections - 1; i++)
            {
                var r0 = i * (Profile + 1);
                var r1 = (i + 1) * (Profile + 1);
                for (var j = 0; j < Profile; j++)
                {
                    mesh.AddTriangle(r0 + j, r0 + j + 1, r1 + j);
                    mesh.AddTriangle(r0 + j + 1, r1 + j + 1, r1 + j);
                }
            }

            // La cubierta: una línea de crujía a la altura de la borda (con el punto del remero en z 9,5) y sus propios vértices en las dos
            // bordas, blancos (o del cockpit): la línea de borda celeste queda en el costado y no tiñe la cubierta (WP-D2, B6a-22).
            var deckFirst = mesh.VertexCount;
            for (var i = 0; i < sections; i++)
            {
                var z = SectionZ(i);
                var c = LakeArt.IsCockpit(z) ? cockpit : DeckColor(z);
                mesh.AddVertex(0, LakeArt.HullFreeboard + 0.02 - SternDrop(z), z, 0, 1, 0, c.R, c.G, c.B, 0, 0.5, i / (double)(sections - 1));
            }

            var edgeFirst = mesh.VertexCount;
            for (var i = 0; i < sections; i++)
            {
                var z = SectionZ(i);
                var c = LakeArt.IsCockpit(z) ? cockpit : DeckColor(z);
                var left = i * (Profile + 1);
                var right = left + Profile;
                var s = (z - LakeArt.HullStern) / (LakeArt.HullBow - LakeArt.HullStern);
                mesh.AddVertex(mesh.X(left), mesh.Y(left), z, 0, 1, 0, c.R, c.G, c.B, 0, 0, s);
                mesh.AddVertex(mesh.X(right), mesh.Y(right), z, 0, 1, 0, c.R, c.G, c.B, 0, 1, s);
            }

            for (var i = 0; i < sections - 1; i++)
            {
                var l0 = edgeFirst + 2 * i;
                var l1 = edgeFirst + 2 * (i + 1);
                mesh.AddTriangle(l0, deckFirst + i, l1);
                mesh.AddTriangle(deckFirst + i, deckFirst + i + 1, l1);
                mesh.AddTriangle(l0 + 1, l1 + 1, deckFirst + i);
                mesh.AddTriangle(deckFirst + i, l1 + 1, deckFirst + i + 1);
            }
        }
    }
}
