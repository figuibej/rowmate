using System;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Core.Scene.Geometry
{
    // El remo de sculling (arte §5.1): en su marco, con el origen en el perno y la caña sobre +x hacia la pala; el extremo del puño en
    // −0,88 (interior), el centro del puño en −0,82, el collarín en 0, el centro de la pala en +1,76 y la punta en +2,00. Con el plumeo
    // en 0 la pala es vertical (se extiende en y: borde de arriba recto en +0,09 y panza abajo hasta −0,13) con una concavidad de
    // 2,5 cm hacia −z. Partes en el rojo del color de vértice (parte / 4: puño, caña, manguito, pala, punta). Submalla 0: la caña;
    // submalla 1: la pala (el único que proyecta sombra, DA7-06). ≤ 1 200 triángulos.
    public static class OarMesh
    {
        public const int VertexCapacity = 600;
        public const int IndexCapacity = 3 * 1200;
        public const int Sides = 8;

        public static void Build(in BoatSpec boat, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var handleEnd = -boat.Inboard;
            var bladeStart = boat.BladeFromPin - boat.BladeLength / 2;
            var tip = boat.OarLength - boat.Inboard;

            // La caña como un torno: (x, radio, parte).
            Lathe(mesh, handleEnd, 0.012, RowerArt.OarGrip, handleEnd + 0.005, 0.018, RowerArt.OarGrip, true);
            Lathe(mesh, handleEnd + 0.005, 0.018, RowerArt.OarGrip, handleEnd + 0.12, 0.018, RowerArt.OarGrip, false);
            Lathe(mesh, handleEnd + 0.12, 0.016, RowerArt.OarShaft, -0.12, 0.016, RowerArt.OarShaft, false);
            Lathe(mesh, -0.12, 0.023, RowerArt.OarSleeve, -0.0125, 0.023, RowerArt.OarSleeve, false);
            Lathe(mesh, -0.0125, 0.035, RowerArt.OarSleeve, 0.0125, 0.035, RowerArt.OarSleeve, false);
            Lathe(mesh, 0.0125, 0.023, RowerArt.OarSleeve, 0.12, 0.023, RowerArt.OarSleeve, false);
            Lathe(mesh, 0.12, 0.019, RowerArt.OarShaft, bladeStart, 0.015, RowerArt.OarShaft, false);

            // La pala: dos caras (frente cóncavo y dorso) de 6 × 5 celdas.
            mesh.BeginSubmesh();
            Blade(mesh, bladeStart, tip, true);
            Blade(mesh, bladeStart, tip, false);
        }

        // El contorno de la pala en x (0 → 1 a lo largo): el alto de arriba (recto) y la panza de abajo.
        public static double BladeTop(double s) => 0.09 - 0.01 * s;

        public static double BladeBottom(double s) => -0.06 - 0.07 * Math.Sin(Math.PI * Math.Min(1, s * 1.15));

        private static void Blade(MeshData mesh, double x0, double x1, bool front)
        {
            const int nu = 6;
            const int nv = 5;
            var first = mesh.VertexCount;
            for (var j = 0; j <= nv; j++)
            {
                var v = j / (double)nv;
                for (var i = 0; i <= nu; i++)
                {
                    var s = i / (double)nu;
                    var x = x0 + (x1 - x0) * s;
                    var y = BladeBottom(s) + (BladeTop(s) - BladeBottom(s)) * v;
                    var cup = -0.025 * (1 - (2 * v - 1) * (2 * v - 1));
                    var z = cup + (front ? -0.003 : 0.003);
                    var part = x1 - x < 0.03 ? RowerArt.OarTip : RowerArt.OarBlade;
                    mesh.AddVertex(x, y, z, 0, 0, front ? -1 : 1, part / 4.0, 0, 0, 0, s, v);
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
                    if (front)
                    {
                        mesh.AddTriangle(a, b, d);
                        mesh.AddTriangle(b, e, d);
                    }
                    else
                    {
                        mesh.AddTriangle(a, d, b);
                        mesh.AddTriangle(b, d, e);
                    }
                }
            }
        }

        private static void Lathe(MeshData mesh, double x0, double r0, int part0, double x1, double r1, int part1, bool capStart)
        {
            var first = mesh.VertexCount;
            for (var ring = 0; ring < 2; ring++)
            {
                var x = ring == 0 ? x0 : x1;
                var r = ring == 0 ? r0 : r1;
                var part = ring == 0 ? part0 : part1;
                for (var j = 0; j <= Sides; j++)
                {
                    var t = 2 * Math.PI * j / Sides;
                    mesh.AddVertex(x, Math.Cos(t) * r, Math.Sin(t) * r, 0, Math.Cos(t), Math.Sin(t), part / 4.0, 0, 0, 0, j / (double)Sides, ring);
                }
            }

            for (var j = 0; j < Sides; j++)
            {
                var a = first + j;
                var b = a + 1;
                var d = first + Sides + 1 + j;
                var e = d + 1;
                mesh.AddTriangle(a, b, d);
                mesh.AddTriangle(b, e, d);
            }

            if (capStart)
            {
                var c = mesh.AddVertex(x0, 0, 0, -1, 0, 0, part0 / 4.0, 0, 0, 0, 0.5, 0.5);
                for (var j = 0; j < Sides; j++)
                {
                    mesh.AddTriangle(first + j, c, first + j + 1);
                }
            }
        }
    }

    // Los herrajes del single (arte §5.2), en el marco del bote: un ala de carbono por lado de la borda (z −0,20 a +0,20) al perno
    // (± 0,80; 0,31) con su tirante a proa, la chumacera blanca, los rieles a ± 0,09 (0,72 m centrados en el recorrido del carro) y la
    // pedalera a 42° con las zapatillas, donde la pone RiggingFit. El carro, que se mueve, es aparte (SeatMesh). Color de vértice RGB
    // (shader Hull). ≤ 2 000 triángulos.
    public static class BoatFittingsMesh
    {
        public const int VertexCapacity = 1200;
        public const int IndexCapacity = 3 * 2000;

        public static void Build(in BoatSpec boat, in RowerSpec rower, in RigFit fit, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var gunwale = LakeArt.HullBeamGunwale / 2;
            var deck = LakeArt.HullFreeboard;
            for (var side = -1; side <= 1; side += 2)
            {
                var s = (double)side;

                // El ala: de la borda (z ± 0,20) al perno (z ± 0,03), arriba y abajo.
                MeshShapes.Quad(mesh, s * gunwale, deck, -0.20, s * boat.PinX, boat.PinY - 0.01, -0.03, s * boat.PinX, boat.PinY - 0.01, 0.03,
                    s * gunwale, deck, 0.20, 0, 1, 0, RowerArt.Carbon, 0, 4, 2);
                MeshShapes.Quad(mesh, s * gunwale, deck - 0.012, 0.20, s * boat.PinX, boat.PinY - 0.022, 0.03, s * boat.PinX, boat.PinY - 0.022, -0.03,
                    s * gunwale, deck - 0.012, -0.20, 0, -1, 0, RowerArt.Carbon, 0, 4, 2);

                // El tirante a proa.
                MeshShapes.Quad(mesh, s * gunwale, deck, 0.55, s * (boat.PinX - 0.02), boat.PinY - 0.01, 0.02, s * (boat.PinX - 0.02), boat.PinY - 0.025, 0.02,
                    s * gunwale, deck - 0.015, 0.55, 0, 0, 1, RowerArt.Carbon, 0, 3, 1);

                // La chumacera y el perno.
                MeshShapes.Box(mesh, s * boat.PinX - 0.03, boat.PinY - 0.03, -0.02, s * boat.PinX + 0.03, boat.PinY + 0.03, 0.02, RowerArt.Oarlock, 0, false);

                // El riel.
                var railCenter = (fit.HipZCatch + fit.HipZFinish) / 2;
                MeshShapes.Box(mesh, s * 0.09 - 0.008, deck - 0.01, railCenter - boat.RailLength / 2, s * 0.09 + 0.008, deck + 0.005,
                    railCenter + boat.RailLength / 2, RowerArt.Rails, 0);

                // La zapatilla sobre la placa.
                Shoe(mesh, boat, rower, fit, s * rower.FootHalfSpread);
            }

            // La placa de la pedalera.
            var sole = RiggingFit.SoleDirection(boat);
            var normal = RiggingFit.SoleNormal(boat);
            var heel = new SceneVec3(0, fit.HeelY, fit.HeelZ) - sole * 0.03 - normal * 0.01;
            var top = heel + sole * (rower.FootLength + 0.05);
            MeshShapes.Quad(mesh, -0.16, heel.Y, heel.Z, 0.16, heel.Y, heel.Z, 0.16, top.Y, top.Z, -0.16, top.Y, top.Z, normal.X, normal.Y, normal.Z,
                RowerArt.Stretcher, 0, 2, 3);
        }

        private static void Shoe(MeshData mesh, in BoatSpec boat, in RowerSpec rower, in RigFit fit, double x)
        {
            var sole = RiggingFit.SoleDirection(boat);
            var normal = RiggingFit.SoleNormal(boat);
            var heel = new SceneVec3(x, fit.HeelY, fit.HeelZ);
            var toe = heel + sole * rower.FootLength;
            var up = normal * 0.06;
            var w = 0.05;
            // Las dos caras de los costados, la de arriba y la de atrás (talón).
            MeshShapes.Quad(mesh, x - w, heel.Y, heel.Z, x - w, toe.Y, toe.Z, x - w, toe.Y + up.Y, toe.Z + up.Z, x - w, heel.Y + up.Y, heel.Z + up.Z, -1, 0, 0,
                RowerArt.Shoes, 0, 2, 1);
            MeshShapes.Quad(mesh, x + w, toe.Y, toe.Z, x + w, heel.Y, heel.Z, x + w, heel.Y + up.Y, heel.Z + up.Z, x + w, toe.Y + up.Y, toe.Z + up.Z, 1, 0, 0,
                RowerArt.Shoes, 0, 2, 1);
            MeshShapes.Quad(mesh, x - w, heel.Y + up.Y, heel.Z + up.Z, x - w, toe.Y + up.Y, toe.Z + up.Z, x + w, toe.Y + up.Y, toe.Z + up.Z, x + w,
                heel.Y + up.Y, heel.Z + up.Z, normal.X, normal.Y, normal.Z, RowerArt.Shoes, 0, 2, 1);
            MeshShapes.Quad(mesh, x + w, heel.Y, heel.Z, x - w, heel.Y, heel.Z, x - w, heel.Y + up.Y, heel.Z + up.Z, x + w, heel.Y + up.Y, heel.Z + up.Z,
                -sole.X, -sole.Y, -sole.Z, RowerArt.Shoes, 0, 1, 1);
        }
    }

    // El carro (arte §5.2): el asiento negro de 0,24 × 0,20 m con la cara de arriba en y = 0 (WP-B lo pone en SeatPosition). Se mueve.
    public static class SeatMesh
    {
        public const int VertexCapacity = 64;
        public const int IndexCapacity = 3 * 24;

        public static void Build(MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            MeshShapes.Box(mesh, -0.12, -0.03, -0.10, 0.12, 0, 0.10, RowerArt.Seat, 0);
        }
    }

    // La boya de 500 m (arte §9): esfera de Ø 0,50 m que asoma 0,35 m (centro en y = 0,10 sobre el agua), con la franja blanca de
    // 0,06 m arriba del ecuador marcada en el rojo del color de vértice (1 franja, 0 naranja). ≤ 400 triángulos.
    public static class BuoyMesh
    {
        public const int Segments = 16, Rings = 12;
        public const int VertexCapacity = (Segments + 1) * (Rings + 1);
        public const int IndexCapacity = 3 * 2 * Segments * Rings;

        public static double CenterY => RowerArt.BuoyAboveWater - RowerArt.BuoyRadius;

        public static void Build(MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var r = RowerArt.BuoyRadius;
            var cy = CenterY;
            for (var i = 0; i <= Rings; i++)
            {
                var phi = Math.PI * i / Rings - Math.PI / 2;
                for (var j = 0; j <= Segments; j++)
                {
                    var th = 2 * Math.PI * j / Segments;
                    var nx = Math.Cos(phi) * Math.Cos(th);
                    var ny = Math.Sin(phi);
                    var nz = Math.Cos(phi) * Math.Sin(th);
                    var y = ny * r;
                    var stripe = y >= 0.02 && y <= 0.02 + RowerArt.BuoyStripe ? 1.0 : 0.0;
                    mesh.AddVertex(nx * r, cy + y, nz * r, nx, ny, nz, stripe, 0, 0, 0, j / (double)Segments, i / (double)Rings);
                }
            }

            for (var i = 0; i < Rings; i++)
            {
                for (var j = 0; j < Segments; j++)
                {
                    var a = i * (Segments + 1) + j;
                    var b = a + 1;
                    var d = a + Segments + 1;
                    var e = d + 1;
                    mesh.AddTriangle(a, d, b);
                    mesh.AddTriangle(b, d, e);
                }
            }
        }
    }
}
