using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Geometry
{
    // La silueta lejana de río, costa y regata (It. 8, WP-B1; AC-SCN-19-03 a -05) en la misma codificación que HorizonStripMesh
    // (posición = (acimut en rad; p; 0), p en fracciones de hMax; el shader Horizon la escala con tan Θ y k(d)), así el mismo
    // HorizonRenderer la dibuja. Siluetas de bloques: dos vértices por arista vertical (el escalón no se suaviza). La forma es fija
    // (DV-81) y cubre de −65° a +65° como la del lago: fuera del ancho de TV se repite el patrón del legacy (skyline y regata) o se
    // sigue el primer paso del acantilado (costa). Las ventanas del skyline de noche quedan para el shader de WP-D1.
    public static class EnvironmentHorizonStrip
    {
        public const int MaxPoints = 400;

        public static int VertexCapacity => 2 * MaxPoints + FlagVertexCapacity;

        public static int IndexCapacity => 6 * MaxPoints + FlagIndexCapacity;

        private static readonly double MinAz = SceneMath.DegToRad(LakeArt.StripMinAzimuthDeg);
        private static readonly double MaxAz = SceneMath.DegToRad(LakeArt.StripMaxAzimuthDeg);

        // Los puntos (acimut, p) de la silueta, de izquierda a derecha; devuelve cuántos.
        public static int Profile(EnvironmentId environment, double[] az, double[] p)
        {
            if (az == null || p == null)
            {
                throw new ArgumentNullException(az == null ? nameof(az) : nameof(p));
            }

            var n = 0;
            switch (environment)
            {
                case EnvironmentId.River:
                {
                    var rnd = new DartRandom(1);
                    var column = Skyline.ColumnRad;
                    var left = LegacyTv.PxToRad(-LegacyTv.Width / 2);
                    var first = (int)Math.Floor((MinAz - left) / column);
                    var last = (int)Math.Ceiling((MaxAz - left) / column);
                    for (var i = first; i <= last; i++)
                    {
                        Skyline.Column(i, rnd, out var height, out var width, out _, out _, out _);
                        var a0 = left + i * column;
                        var a1 = a0 + column * width;
                        Add(az, p, ref n, a0, 0);
                        Add(az, p, ref n, a0, height);
                        Add(az, p, ref n, a1, height);
                        Add(az, p, ref n, a1, 0);
                    }

                    break;
                }

                case EnvironmentId.Coast:
                {
                    Add(az, p, ref n, MinAz, CoastFarCliff.Height(0));
                    for (var step = 0; step <= CoastArt.FarCliffSteps; step++)
                    {
                        var a = CoastFarCliff.AzimuthRad(step);
                        Add(az, p, ref n, a, CoastFarCliff.Height(step));
                    }

                    Add(az, p, ref n, CoastFarCliff.AzimuthRad(CoastArt.FarCliffSteps) + 1e-4, 0);
                    Add(az, p, ref n, MaxAz, 0);
                    break;
                }

                case EnvironmentId.Regatta:
                {
                    var period = RegattaBackdrop.PeriodRad;
                    var left = LegacyTv.PxToRad(-LegacyTv.Width / 2);
                    var first = (int)Math.Floor((MinAz - left) / period) - 1;
                    var last = (int)Math.Ceiling((MaxAz - left) / period);
                    Add(az, p, ref n, MinAz, RegattaBackdrop.LandTop);
                    for (var b = first; b <= last; b++)
                    {
                        var stand = RegattaBackdrop.Stand(b);
                        var tower = RegattaBackdrop.Tower(b);
                        Rect(az, p, ref n, stand.LeftRad, stand.WidthRad, stand.TopFraction);
                        Rect(az, p, ref n, tower.LeftRad, tower.WidthRad, tower.TopFraction);
                    }

                    Add(az, p, ref n, MaxAz, RegattaBackdrop.LandTop);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, "EnvironmentHorizonStrip: el lago usa HorizonStripMesh.");
            }

            return n;
        }

        private static void Rect(double[] az, double[] p, ref int n, double left, double width, double top)
        {
            var land = RegattaBackdrop.LandTop;
            Add(az, p, ref n, left, land);
            Add(az, p, ref n, left, top);
            Add(az, p, ref n, left + width, top);
            Add(az, p, ref n, left + width, land);
        }

        // Solo dentro de ±65° y en orden (lo de afuera se recorta al borde).
        private static void Add(double[] az, double[] p, ref int n, double a, double value)
        {
            if (n >= az.Length)
            {
                return;
            }

            a = Math.Max(MinAz, Math.Min(MaxAz, a));
            if (n > 0 && a < az[n - 1])
            {
                a = az[n - 1];
            }

            az[n] = a;
            p[n] = Math.Max(0, value);
            n++;
        }

        // QA-V8-01 (AC-SCN-19-05, -20-06; regatta_environment.dart:56-68): los grupos de 4 banderas de la tira lejana de la regata, en
        // la misma codificación que la silueta pero con z = el código (1 a 4 la bandera i + 1, 5 el mástil; 0 la silueta) que el shader
        // Horizon pinta con su tono de la hora (rojo, ámbar, azul, verde con profundidad 0,5; el mástil del color de la torre). Mástil de
        // un px de TV de p 0,25 a 0,6; paño triangular de 6 px (0,6 → punta en 0,55 → 0,5) con la punta (r = 1) que flamea sin(3t + i)·2 px
        // en el vértice. Sin repetir el corrimiento con la distancia del legacy (forma fija, DV-81).
        public const int FlagsPerBlock = 4, FlagMastCode = 5;
        public const int FlagVertexCapacity = 7 * FlagsPerBlock * 10;
        public const int FlagIndexCapacity = 9 * FlagsPerBlock * 10;

        public static void AddRegattaFlags(MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            var period = RegattaBackdrop.PeriodRad;
            var left = LegacyTv.PxToRad(-LegacyTv.Width / 2);
            var first = (int)Math.Floor((MinAz - left) / period) - 1;
            var last = (int)Math.Ceiling((MaxAz - left) / period);
            var mastWidth = LegacyTv.PxToRad(1);
            var cloth = RegattaBackdrop.FlagClothRad;
            for (var b = first; b <= last; b++)
            {
                for (var i = 0; i < FlagsPerBlock; i++)
                {
                    var az = RegattaBackdrop.FlagAzimuthRad(b, i);
                    if (az - mastWidth < MinAz || az + cloth > MaxAz)
                    {
                        continue;
                    }

                    var m = mesh.VertexCount;
                    var bottom = RegattaBackdrop.FlagMastBottom;
                    var top = RegattaBackdrop.FlagMastTop;
                    mesh.AddVertex(az - mastWidth / 2, bottom, FlagMastCode, 0, 0, -1, 0, 0, 0, 1, 0, 0);
                    mesh.AddVertex(az - mastWidth / 2, top, FlagMastCode, 0, 0, -1, 0, 0, 0, 1, 0, 1);
                    mesh.AddVertex(az + mastWidth / 2, top, FlagMastCode, 0, 0, -1, 0, 0, 0, 1, 1, 1);
                    mesh.AddVertex(az + mastWidth / 2, bottom, FlagMastCode, 0, 0, -1, 0, 0, 0, 1, 1, 0);
                    mesh.AddTriangle(m, m + 1, m + 2);
                    mesh.AddTriangle(m, m + 2, m + 3);
                    var c = mesh.VertexCount;
                    var code = i + 1;
                    mesh.AddVertex(az, top, code, 0, 0, -1, 0, 0, 0, 1, 0, 1);
                    mesh.AddVertex(az + cloth, RegattaBackdrop.FlagTipP, code, 0, 0, -1, 1, 0, 0, 1, 1, 0.5);
                    mesh.AddVertex(az, RegattaBackdrop.FlagClothBottomP, code, 0, 0, -1, 0, 0, 0, 1, 0, 0);
                    mesh.AddTriangle(c, c + 1, c + 2);
                }
            }
        }

        public static void Build(EnvironmentId environment, MeshData mesh)
        {
            if (mesh == null)
            {
                throw new ArgumentNullException(nameof(mesh));
            }

            var az = new double[MaxPoints];
            var p = new double[MaxPoints];
            var n = Profile(environment, az, p);
            mesh.Clear();
            for (var i = 0; i < n; i++)
            {
                var u = (az[i] - MinAz) / (MaxAz - MinAz);
                mesh.AddVertex(az[i], 0, 0, 0, 0, -1, 0, 0, u, 0, u, 0);
                mesh.AddVertex(az[i], p[i], 0, 0, 0, -1, 0, 0, u, 1, u, 1);
            }

            for (var i = 0; i < n - 1; i++)
            {
                var a = 2 * i;
                mesh.AddTriangle(a, a + 1, a + 2);
                mesh.AddTriangle(a + 1, a + 3, a + 2);
            }

            if (environment == EnvironmentId.Regatta)
            {
                AddRegattaFlags(mesh);
            }
        }
    }
}
