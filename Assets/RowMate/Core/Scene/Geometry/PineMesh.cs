using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // El pino del legacy en 3D (shore_painter.dart:89-104; 04-design-notes §7.3; AC-SCN-15-06): tronco cónico y 3 "faldas" de
    // 2,2 m con 9 a 12 lóbulos, de radio el medio ancho del legacy × (0,85 a 1,08), con la punta del lóbulo caída 0,25 m; nunca
    // dos lóbulos vecinos iguales; inclinación ≤ 2°. Alto 6 m y ancho ≈ 2,6 m a escala 1 (la escala va por instancia). La forma
    // sale de la semilla de forma ((semilla / 3) mod 3: 3 mallas); la paleta (semilla mod 3) va por instancia.
    // Color de vértice: r = oclusión (0,55 adentro y abajo → 1 en las puntas), g = peso del viento (altura²), b = grada (0; 0,5;
    // 1), a = 1 follaje / 0 tronco. LOD 0, 1 y 2: ≤ 1 200, 400 y 120 triángulos.
    public static class PineMesh
    {
        private static readonly int[] Around = { 48, 32, 18 };
        private static readonly int[] Rings = { 4, 2, 1 };
        private static readonly int[] TrunkSides = { 12, 6, 4 };

        // La receta "bosque" (04-design-notes §7.4): de 3 a 5 gradas más angostas (radio × 0,8), LOD 400 / 120.
        private static readonly int[] ForestAround = { 16, 8 };
        private static readonly int[] ForestRings = { 2, 1 };
        private static readonly int[] ForestTrunkSides = { 6, 4 };
        private const double ForestRadiusScale = 0.8;

        public static int Forms => 3;

        public static int FormOf(int seed)
        {
            return (seed / 3) % 3;
        }

        public static int PaletteOf(int seed)
        {
            return seed % 3;
        }

        public static int VertexCapacity(int lod)
        {
            return 5 * (2 + Around[lod] * Rings[lod]) + 2 * (TrunkSides[lod] + 1);
        }

        public static int IndexCapacity(int lod)
        {
            return 3 * (5 * 2 * Rings[lod] * Around[lod] + 2 * TrunkSides[lod]);
        }

        // tierBaseMin > 0: la poda (DT6a-08): las gradas arrancan a esa altura (3,0 m, sobre el alero de la cabaña).
        public static void Build(int form, int lod, MeshData mesh, double tierBaseMin = 0)
        {
            Check(lod, 3, mesh);
            mesh.Clear();
            var rnd = new DartRandom(0x50494E45L + form);
            var tiltDeg = rnd.NextDouble() * LakeArt.PineMaxTiltDeg;
            var tiltDir = rnd.NextDouble() * 2 * Math.PI;
            var tiers = LakeArt.PineTierBase.Length;
            var top = LakeArt.PineTierTip[tiers - 1];
            Trunk(mesh, TrunkSides[lod], top, tiltDeg, tiltDir);
            for (var t = 0; t < tiers; t++)
            {
                var b = Math.Max(LakeArt.PineTierBase[t], tierBaseMin);
                var tip = LakeArt.PineTierTip[t];
                if (tip - b < 0.5)
                {
                    rnd.NextDouble();
                    continue;
                }

                Tier(mesh, rnd, t / (double)(tiers - 1), b, tip, LakeArt.PineTierHalfWidth[t], Around[lod], Rings[lod], top, tiltDeg, tiltDir);
            }
        }

        public static void BuildForest(int form, int lod, MeshData mesh)
        {
            Check(lod, 2, mesh);
            mesh.Clear();
            var rnd = new DartRandom(0x464F5245L + form);
            var tiers = 3 + rnd.NextInt(3);
            var tiltDeg = rnd.NextDouble() * LakeArt.PineMaxTiltDeg;
            var tiltDir = rnd.NextDouble() * 2 * Math.PI;
            var top = LakeArt.PineHeight;
            Trunk(mesh, ForestTrunkSides[lod], top, tiltDeg, tiltDir);
            var span = (top - LakeArt.PineTierBase[0]) / (tiers + 1.5);
            for (var t = 0; t < tiers; t++)
            {
                var b = LakeArt.PineTierBase[0] + t * span;
                var tip = Math.Min(top, b + 2.5 * span);
                var half = LakeArt.PineMaxHalfWidth * ForestRadiusScale * (1 - 0.6 * t / Math.Max(1, tiers - 1));
                Tier(mesh, rnd, t / (double)Math.Max(1, tiers - 1), b, tip, half, ForestAround[lod], ForestRings[lod], top, tiltDeg, tiltDir);
            }
        }

        public static int ForestVertexCapacity(int lod)
        {
            return 5 * (2 + ForestAround[lod] * ForestRings[lod]) + 2 * (ForestTrunkSides[lod] + 1);
        }

        public static int ForestIndexCapacity(int lod)
        {
            return 3 * (5 * 2 * ForestRings[lod] * ForestAround[lod] + 2 * ForestTrunkSides[lod]);
        }

        // Los factores del radio de los lóbulos de una grada: entre 0,85 y 1,08, el más grande en 1,08 (el ancho del legacy) y
        // ningún par de vecinos (también el último con el primero) a menos de 0,02.
        public static void LobeFactors(DartRandom rnd, Span<double> into)
        {
            var n = into.Length;
            var max = 0;
            for (var i = 0; i < n; i++)
            {
                into[i] = LakeArt.PineLobeMin + rnd.NextDouble() * (LakeArt.PineLobeMax - 0.03 - LakeArt.PineLobeMin);
                if (into[i] > into[max])
                {
                    max = i;
                }
            }

            into[max] = LakeArt.PineLobeMax;
            for (var i = 0; i < n; i++)
            {
                var prev = into[(i + n - 1) % n];
                if (i != max && Math.Abs(into[i] - prev) < 0.02)
                {
                    into[i] = prev + 0.02 <= LakeArt.PineLobeMax - 0.01 ? prev + 0.02 : prev - 0.02;
                }
            }

            // Una pasada más para el par último-primero.
            if (Math.Abs(into[0] - into[n - 1]) < 0.02 && max != 0)
            {
                into[0] = into[n - 1] + 0.02 <= LakeArt.PineLobeMax - 0.01 ? into[n - 1] + 0.02 : into[n - 1] - 0.02;
                if (Math.Abs(into[0] - into[1]) < 0.02)
                {
                    into[0] = Math.Max(LakeArt.PineLobeMin, Math.Min(into[n - 1], into[1]) - 0.025);
                }
            }
        }

        private static void Tier(MeshData mesh, DartRandom rnd, double tierLevel, double baseY, double tipY, double half, int around, int rings, double top,
                                 double tiltDeg, double tiltDir)
        {
            var lobes = LakeArt.PineLobesMin + rnd.NextInt(LakeArt.PineLobesMax - LakeArt.PineLobesMin + 1);
            Span<double> factors = stackalloc double[LakeArt.PineLobesMax];
            factors = factors.Slice(0, lobes);
            LobeFactors(rnd, factors);
            var phase = rnd.NextDouble() * 2 * Math.PI;
            var apex = Vertex(mesh, 0, tipY, 0, 0, 1, 0, LakeArt.PineOcclusionInner + 0.2, tipY, top, tierLevel, 1, 0.5, 1, tiltDeg, tiltDir);
            var firstRing = mesh.VertexCount;
            for (var k = 1; k <= rings; k++)
            {
                var t = k / (double)rings;
                for (var j = 0; j < around; j++)
                {
                    var theta = phase + 2 * Math.PI * j / around;
                    var lobePos = (2 * Math.PI * j / around) * lobes / (2 * Math.PI);
                    var lobe = (int)Math.Floor(lobePos) % lobes;
                    var u = lobePos - Math.Floor(lobePos);
                    var bump = Math.Sin(Math.PI * u);
                    var r = half * factors[lobe] * (LakeArt.PineValley + (1 - LakeArt.PineValley) * bump);
                    var rimY = baseY - LakeArt.PineLobeDroop * bump;
                    var x = Math.Cos(theta) * r * t;
                    var z = Math.Sin(theta) * r * t;
                    var y = tipY + (rimY - tipY) * t;
                    var slope = r / Math.Max(0.1, tipY - rimY);
                    var occ = LakeArt.PineOcclusionInner + (1 - LakeArt.PineOcclusionInner) * t * (0.5 + 0.5 * bump);
                    Vertex(mesh, x, y, z, Math.Cos(theta), slope, Math.Sin(theta), occ, y, top, tierLevel, 1, j / (double)around, y / top, tiltDeg, tiltDir);
                }
            }

            var center = Vertex(mesh, 0, baseY + 0.3, 0, 0, -1, 0, LakeArt.PineOcclusionInner, baseY, top, tierLevel, 1, 0.5, 0, tiltDeg, tiltDir);
            for (var j = 0; j < around; j++)
            {
                var a = firstRing + j;
                var b = firstRing + (j + 1) % around;
                mesh.AddTriangle(apex, b, a);
            }

            for (var k = 1; k < rings; k++)
            {
                var ring0 = firstRing + (k - 1) * around;
                var ring1 = firstRing + k * around;
                for (var j = 0; j < around; j++)
                {
                    var j1 = (j + 1) % around;
                    mesh.AddTriangle(ring0 + j, ring0 + j1, ring1 + j);
                    mesh.AddTriangle(ring0 + j1, ring1 + j1, ring1 + j);
                }
            }

            var rim = firstRing + (rings - 1) * around;
            for (var j = 0; j < around; j++)
            {
                mesh.AddTriangle(center, rim + j, rim + (j + 1) % around);
            }
        }

        private static void Trunk(MeshData mesh, int sides, double top, double tiltDeg, double tiltDir)
        {
            var first = mesh.VertexCount;
            for (var ring = 0; ring < 2; ring++)
            {
                var y = ring == 0 ? 0.0 : top;
                var r = ring == 0 ? LakeArt.PineTrunkRadius : LakeArt.PineTrunkTopRadius;
                for (var j = 0; j <= sides; j++)
                {
                    var theta = 2 * Math.PI * j / sides;
                    Vertex(mesh, Math.Cos(theta) * r, y, Math.Sin(theta) * r, Math.Cos(theta), 0.02, Math.Sin(theta), 1, y, top, 0, 0,
                        j / (double)sides, y / LakeArt.PineTrunkVisible, tiltDeg, tiltDir);
                }
            }

            for (var j = 0; j < sides; j++)
            {
                var a = first + j;
                var b = first + j + 1;
                var c = first + sides + 1 + j;
                var d = first + sides + 2 + j;
                mesh.AddTriangle(a, c, b);
                mesh.AddTriangle(b, c, d);
            }
        }

        // Un vértice con la inclinación de la forma aplicada (un giro chico alrededor de un eje horizontal por la base).
        private static int Vertex(MeshData mesh, double x, double y, double z, double nx, double ny, double nz, double occlusion, double height, double top,
                                  double tier, double foliage, double u, double v, double tiltDeg, double tiltDir)
        {
            var a = SceneMath.DegToRad(tiltDeg);
            var ax = Math.Cos(tiltDir);
            var az = Math.Sin(tiltDir);
            Rotate(ref x, ref y, ref z, ax, az, a);
            Rotate(ref nx, ref ny, ref nz, ax, az, a);
            var wind = Math.Max(0, height / top);
            return mesh.AddVertex(x, y, z, nx, ny, nz, occlusion, wind * wind, tier, foliage, u, v);
        }

        // Rodrigues alrededor del eje horizontal (ax; 0; az).
        private static void Rotate(ref double x, ref double y, ref double z, double ax, double az, double angle)
        {
            var c = Math.Cos(angle);
            var s = Math.Sin(angle);
            var dot = ax * x + az * z;
            var cx = -az * y;
            var cy = az * x - ax * z;
            var cz = ax * y;
            var nx = x * c + cx * s + ax * dot * (1 - c);
            var ny = y * c + cy * s;
            var nz = z * c + cz * s + az * dot * (1 - c);
            x = nx;
            y = ny;
            z = nz;
        }

        private static void Check(int lod, int lods, MeshData mesh)
        {
            if (lod < 0 || lod >= lods)
            {
                throw new ArgumentOutOfRangeException(nameof(lod));
            }

            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }
        }
    }
}
