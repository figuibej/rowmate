using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // El terreno de una orilla entre dos z de mundo (06a-escena-lago/02-tech-design §2.14; 04-design-notes §7.1, §7.6; DT6a-18),
    // con TerrainProfile como única verdad: dos trozos vecinos comparten exactamente el borde (sin costuras). Las columnas van
    // relativas a la orilla de cada fila (que serpentea): 0,5 m en el fondo, la playa y el talud, y de 2 a 128 m hacia afuera. El
    // trozo lejano (450-1 500 m, filas de 8 m) va SIN la franja sumergida y con la orilla levantada 0,4 m: sin Z invertida (GLES,
    // nivel Bajo) nunca hay dos superficies a menos de 0,4 m. Las posiciones son de cinta: z − zOrigin (la cámara cerca del
    // origen, ADR 015). Color de vértice: r = oclusión (1), g = 0, b = |x| desde la orilla / 300, a = peso de la copa lejana.
    public static class TerrainChunkMesh
    {
        // Las columnas cercanas, desde la orilla: de −2 (el fondo) a +7 (el fin del talud) cada 0,5 m, y después hacia afuera.
        // Antes que los arreglos que la usan (el orden de inicialización de los estáticos es el del texto).
        private static readonly double[] Outer = { 9, 11, 15, 23, 39, 71, 135, 263 };
        private static readonly double[] NearOffsets = BuildOffsets(-2.0, 0.5);
        private static readonly double[] FarOffsets = BuildOffsets(0.0, 1.0);

        public static int Columns(bool far) => (far ? FarOffsets : NearOffsets).Length;

        public static int VertexCapacity(int rows, bool far) => (rows + 1) * Columns(far);

        public static int IndexCapacity(int rows, bool far) => rows * (Columns(far) - 1) * 6;

        // side −1 = izquierda, +1 = derecha. rowStep: 1 m (0-150 m), 2 m (150-450 m), 8 m (lejano). withCanopy: la copa lejana en la
        // malla (a la izquierda, desde |x| 45, o desde 18 si el bosque cercano no se dibuja).
        public static void Build(EnvironmentId environment, int side, double zStart, double zEnd, double rowStep, bool far, bool withCanopy,
                                 double canopyFromX, double zOrigin, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var offsets = far ? FarOffsets : NearOffsets;
            var rows = (int)Math.Ceiling((zEnd - zStart) / rowStep - 1e-9);
            for (var r = 0; r <= rows; r++)
            {
                var z = r == rows ? zEnd : zStart + r * rowStep;
                var shore = TerrainProfile.ShoreX(side, z);
                for (var c = 0; c < offsets.Length; c++)
                {
                    var ax = shore + offsets[c];
                    var x = side * ax;
                    var y = TerrainProfile.Height(environment, x, z);
                    // It. 8: el realce lejano de la orilla y la copa del bosque son del lago (el mar de la costa no se levanta).
                    var lake = environment == EnvironmentId.Lake;
                    if (far && lake)
                    {
                        y = Math.Max(y, LakeArt.FarShoreLift);
                    }

                    var canopy = 0.0;
                    if (lake && withCanopy && side < 0 && ax >= canopyFromX)
                    {
                        var top = TerrainProfile.CanopyHeight(x, z);
                        if (!double.IsNaN(top) && top > y)
                        {
                            y = top;
                            canopy = 1;
                        }
                    }

                    mesh.AddVertex(x, y, z - zOrigin, 0, 1, 0, 1, 0, Math.Min(1, offsets[c] / 300), canopy, ax / 10, z / 10);
                }
            }

            var cols = offsets.Length;
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols - 1; c++)
                {
                    var a = r * cols + c;
                    var b = a + 1;
                    var d = a + cols;
                    var e = d + 1;
                    if (side < 0)
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

        // Un trozo de un segmento de 50 m (las filas de 1 m del tramo cercano).
        public static void BuildSegment(EnvironmentId environment, int side, long k, double rowStep, bool withCanopy, double zOrigin, MeshData mesh)
        {
            var z0 = k * ShoreEnvironment.SegmentLength;
            Build(environment, side, z0, z0 + ShoreEnvironment.SegmentLength, rowStep, false, withCanopy, LakeArt.ForestOuterX, zOrigin, mesh);
        }

        private static double[] BuildOffsets(double from, double step)
        {
            var count = (int)Math.Floor((LakeArt.BeachWidth + LakeArt.SlopeWidth - from) / step + 0.5) + 1;
            var result = new double[count + Outer.Length];
            for (var i = 0; i < count; i++)
            {
                result[i] = from + i * step;
            }

            for (var i = 0; i < Outer.Length; i++)
            {
                result[count + i] = Outer[i];
            }

            return result;
        }
    }

    // La tira de una cordillera (06a-escena-lago/02-tech-design §2.14; 04-design-notes §6.6): 260 columnas de −65° a +65°, con la
    // cámara en el centro (sin paralaje lateral). Cada columna tiene un vértice abajo (p = 0) y uno en la cresta (p del perfil del
    // legacy por acimut); el shader la escala por ventana (tan Θ) y por k(d). Posición: (acimut en radianes; p; 0). Color de
    // vértice: r = la máscara de nieve (solo la lejana), g = la pendiente dp/dacimut (por grado, recortada a ±1), b = semilla.
    public static class HorizonStripMesh
    {
        public static int VertexCapacity => 2 * (LakeArt.StripColumns + 1);

        public static int IndexCapacity => 6 * LakeArt.StripColumns;

        public static void Build(bool far, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var spec = HorizonSpec.Lake;
            var n = LakeArt.StripColumns;
            var span = LakeArt.StripMaxAzimuthDeg - LakeArt.StripMinAzimuthDeg;
            for (var i = 0; i <= n; i++)
            {
                var az = LakeArt.StripMinAzimuthDeg + span * i / n;
                var p = Math.Max(0, HorizonModel.Profile(az, far));
                var dp = (HorizonModel.Profile(az + 0.1, far) - HorizonModel.Profile(az - 0.1, far)) / 0.2;
                var snow = far ? HorizonModel.SnowMask(p, az, spec) : 0;
                var rad = SceneMath.DegToRad(az);
                var u = i / (double)n;
                var slope = Math.Max(-1, Math.Min(1, dp * 10));
                mesh.AddVertex(rad, 0, 0, 0, 0, -1, 0, slope, u, 0, u, 0);
                mesh.AddVertex(rad, p, 0, 0, 0, -1, snow, slope, u, 1, u, 1);
            }

            for (var i = 0; i < n; i++)
            {
                var a = 2 * i;
                mesh.AddTriangle(a, a + 1, a + 2);
                mesh.AddTriangle(a + 1, a + 3, a + 2);
            }
        }
    }

    // Las mallas chicas del relleno (00-deuda-diseno §6.4; 04-design-notes §7.4): rocas (icosaedros deformados, 5 variantes),
    // matas de juncos (10 a 14 hojas de 2 triángulos) y de pasto (6 hojas de 1 triángulo), a escala 1 (el alto va por instancia).
    public static class SmallMeshes
    {
        // LOD 0: 80 triángulos (el icosaedro subdividido); LOD 1: 20 (≤ 24).
        public static void Rock(int variant, int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var t = (1 + Math.Sqrt(5)) / 2;
            var v = new[]
            {
                -1, t, 0, 1, t, 0, -1, -t, 0, 1, -t, 0, 0, -1, t, 0, 1, t, 0, -1, -t, 0, 1, -t, t, 0, -1, t, 0, 1, -t, 0, -1, -t, 0, 1,
            };
            var f = new[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            var color = SceneColor.FromArgb(variant % 2 == 0 ? LakeArt.Rock : LakeArt.RockAlt);
            for (var i = 0; i < f.Length; i += 3)
            {
                if (lod == 0)
                {
                    // Cada cara en 4: los puntos medios llevados a la esfera.
                    double ax = v[f[i] * 3], ay = v[f[i] * 3 + 1], az = v[f[i] * 3 + 2];
                    double bx = v[f[i + 1] * 3], by = v[f[i + 1] * 3 + 1], bz = v[f[i + 1] * 3 + 2];
                    double cx = v[f[i + 2] * 3], cy = v[f[i + 2] * 3 + 1], cz = v[f[i + 2] * 3 + 2];
                    RockTri(mesh, variant, color, ax, ay, az, (ax + bx) / 2, (ay + by) / 2, (az + bz) / 2, (ax + cx) / 2, (ay + cy) / 2, (az + cz) / 2);
                    RockTri(mesh, variant, color, (ax + bx) / 2, (ay + by) / 2, (az + bz) / 2, bx, by, bz, (bx + cx) / 2, (by + cy) / 2, (bz + cz) / 2);
                    RockTri(mesh, variant, color, (ax + cx) / 2, (ay + cy) / 2, (az + cz) / 2, (bx + cx) / 2, (by + cy) / 2, (bz + cz) / 2, cx, cy, cz);
                    RockTri(mesh, variant, color, (ax + bx) / 2, (ay + by) / 2, (az + bz) / 2, (bx + cx) / 2, (by + cy) / 2, (bz + cz) / 2, (ax + cx) / 2,
                        (ay + cy) / 2, (az + cz) / 2);
                }
                else
                {
                    RockTri(mesh, variant, color, v[f[i] * 3], v[f[i] * 3 + 1], v[f[i] * 3 + 2], v[f[i + 1] * 3], v[f[i + 1] * 3 + 1], v[f[i + 1] * 3 + 2],
                        v[f[i + 2] * 3], v[f[i + 2] * 3 + 1], v[f[i + 2] * 3 + 2]);
                }
            }
        }

        // Una mata de juncos: LOD 0 con 10 a 14 hojas pero ≤ 24 triángulos (12 hojas de 2), LOD 1 con 4 hojas (8).
        public static void ReedTuft(int variant, int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var rnd = new DartRandom(0x52454544L + variant);
            var blades = lod == 0 ? Math.Min(12, LakeArt.ReedBladesMin + rnd.NextInt(LakeArt.ReedBladesMax - LakeArt.ReedBladesMin + 1)) : 4;
            for (var i = 0; i < blades; i++)
            {
                var a = rnd.NextDouble() * 2 * Math.PI;
                var lean = 0.05 + rnd.NextDouble() * 0.15;
                var h = 0.75 + rnd.NextDouble() * 0.25;
                var w = 0.012;
                var bx = Math.Cos(a) * 0.04;
                var bz = Math.Sin(a) * 0.04;
                var tx = bx + Math.Cos(a) * lean;
                var tz = bz + Math.Sin(a) * lean;
                var p0 = mesh.AddVertex(bx - w * Math.Sin(a), 0, bz + w * Math.Cos(a), Math.Cos(a), 0.3, Math.Sin(a), 0.7, 0, 0, 1, 0, 0);
                var p1 = mesh.AddVertex(bx + w * Math.Sin(a), 0, bz - w * Math.Cos(a), Math.Cos(a), 0.3, Math.Sin(a), 0.7, 0, 0, 1, 1, 0);
                var p2 = mesh.AddVertex(tx, h, tz, Math.Cos(a), 0.3, Math.Sin(a), 1, h * h, 0, 1, 0.5, 1);
                var p3 = mesh.AddVertex((bx + tx) / 2, h * 0.5, (bz + tz) / 2, Math.Cos(a), 0.3, Math.Sin(a), 0.85, 0.25, 0, 1, 0.5, 0.5);
                mesh.AddTriangle(p0, p3, p1);
                mesh.AddTriangle(p1, p3, p2);
            }
        }

        // Una mata de pasto: LOD 0 con 6 hojas de 1 triángulo, LOD 1 con 3.
        public static void GrassTuft(int variant, int lod, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            mesh.Clear();
            var rnd = new DartRandom(0x47524153L + variant);
            var blades = lod == 0 ? LakeArt.GrassBlades : LakeArt.GrassBlades / 2;
            for (var i = 0; i < blades; i++)
            {
                var a = (i + rnd.NextDouble() * 0.5) * 2 * Math.PI / blades;
                var lean = 0.1 + rnd.NextDouble() * 0.15;
                var w = 0.03;
                var p0 = mesh.AddVertex(-w * Math.Sin(a), 0, w * Math.Cos(a), Math.Cos(a), 0.5, Math.Sin(a), 0.7, 0, 0, 1, 0, 0);
                var p1 = mesh.AddVertex(w * Math.Sin(a), 0, -w * Math.Cos(a), Math.Cos(a), 0.5, Math.Sin(a), 0.7, 0, 0, 1, 1, 0);
                var p2 = mesh.AddVertex(Math.Cos(a) * lean, 1, Math.Sin(a) * lean, Math.Cos(a), 0.5, Math.Sin(a), 1, 1, 0, 1, 0.5, 1);
                mesh.AddTriangle(p0, p2, p1);
            }
        }

        private static void RockTri(MeshData mesh, int variant, in SceneColor color, double ax, double ay, double az, double bx, double by, double bz,
                                    double cx, double cy, double cz)
        {
            var a = RockVertex(mesh, variant, color, ax, ay, az);
            var b = RockVertex(mesh, variant, color, bx, by, bz);
            var c = RockVertex(mesh, variant, color, cx, cy, cz);
            mesh.AddTriangle(a, b, c);
        }

        // En la esfera de radio 0,5 deformada por ruido de la variante, con la base en y = 0 y el alto en 1 (aplastada).
        private static int RockVertex(MeshData mesh, int variant, in SceneColor color, double x, double y, double z)
        {
            var len = Math.Sqrt(x * x + y * y + z * z);
            x /= len;
            y /= len;
            z /= len;
            var n = SceneHash.Value2(0x524F434BL + variant, x * 2.3 + 7, z * 2.3 + y * 1.7 + 3);
            var r = 0.5 * (0.82 + 0.36 * n);
            return mesh.AddVertex(x * r * 1.25, y * r + 0.5, z * r, x, y, z, color.R, color.G, color.B, 0.5, 0.5 + 0.5 * x, y + 0.5);
        }
    }
}
