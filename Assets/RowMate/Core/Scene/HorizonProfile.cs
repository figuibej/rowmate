using System;
using RowMate.Core.Scene.Art;
using RowMate.Core.Scene.Geometry;

namespace RowMate.Core.Scene
{
    // El perfil del horizonte para el reflejo de Medio (PA6b-05; 00-arte-agua §4.2): una textura de 256 × 1 (R8) con el tope de la silueta
    // lejana por acimut, de −65° a +65° (el centro de cada columna), en fracciones del alto máximo del escenario (tan Θ) × el acercamiento
    // k(d), codificado como byte = redondeo(255·mín(v/2; 1)) (v llega a ≈ 1,4 en el lago). El lago toma la cordillera lejana y la cercana
    // (a 0,7 del alto); río, costa y regata, su silueta de bloques (EnvironmentHorizonStrip). 0 = sin silueta (ahí se mide la costura del
    // horizonte, 00-arte-agua §2.8). Se rehace por filas (la unidad TextureRow) cuando cambia el escenario o cada 50 m, sin asignar: la
    // forma del escenario se arma una vez al construir.
    public sealed class HorizonProfile
    {
        public const int Width = 256;
        public const double Encoding = 2.0;
        public const double RebuildEveryMeters = 50;

        private readonly EnvironmentId _environment;
        private readonly HorizonSpec _spec;
        private readonly double[] _shape = new double[Width];

        public HorizonProfile(EnvironmentId environment)
        {
            _environment = environment;
            _spec = EnvironmentCatalog.Profile(environment).Horizon;
            if (environment == EnvironmentId.Lake)
            {
                return;
            }

            // La silueta de bloques: el máximo de p en el ancho de cada columna (escalones verticales, sin suavizar).
            var az = new double[EnvironmentHorizonStrip.MaxPoints];
            var p = new double[EnvironmentHorizonStrip.MaxPoints];
            var n = EnvironmentHorizonStrip.Profile(environment, az, p);
            for (var c = 0; c < Width; c++)
            {
                var a0 = SceneMath.DegToRad(AzimuthDeg(c) - HalfColumnDeg);
                var a1 = SceneMath.DegToRad(AzimuthDeg(c) + HalfColumnDeg);
                var best = 0.0;
                for (var i = 0; i + 1 < n; i++)
                {
                    // El tramo [az[i]; az[i+1]] con su p (lineal en el tramo) que cae en la columna.
                    var lo = az[i];
                    var hi = az[i + 1];
                    if (hi < a0 || lo > a1)
                    {
                        continue;
                    }

                    best = Math.Max(best, Math.Max(At(lo, hi, p[i], p[i + 1], Math.Max(lo, a0)), At(lo, hi, p[i], p[i + 1], Math.Min(hi, a1))));
                }

                _shape[c] = best;
            }
        }

        public EnvironmentId Environment => _environment;

        private static double HalfColumnDeg => (LakeArt.StripMaxAzimuthDeg - LakeArt.StripMinAzimuthDeg) / Width / 2;

        // El acimut (grados) del centro de la columna c.
        public static double AzimuthDeg(int column)
        {
            return LakeArt.StripMinAzimuthDeg + (LakeArt.StripMaxAzimuthDeg - LakeArt.StripMinAzimuthDeg) * (column + 0.5) / Width;
        }

        // El tope (fracciones de tan Θ, con k(d)) de la columna c a esa distancia.
        public double Value(int column, double distance)
        {
            if ((uint)column >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column));
            }

            if (_environment == EnvironmentId.Lake)
            {
                var azimuth = AzimuthDeg(column);
                var far = Math.Max(0, HorizonModel.Profile(azimuth, true)) * HorizonModel.ApproachScale(distance, true, _spec);
                var near = Math.Max(0, HorizonModel.Profile(azimuth, false)) * LakeArt.NearHeightScale * HorizonModel.ApproachScale(distance, false, _spec);
                return Math.Max(far, near);
            }

            return _shape[column] * HorizonModel.ApproachScale(distance, true, _spec);
        }

        // Una fila de la textura (la unidad TextureRow): todo el ancho, sin asignar.
        public void Fill(double distance, byte[] r8)
        {
            if (r8 == null || r8.Length < Width)
            {
                throw new ArgumentException("HorizonProfile: hacen falta " + Width + " bytes.", nameof(r8));
            }

            for (var c = 0; c < Width; c++)
            {
                r8[c] = Encode(Value(c, distance));
            }
        }

        // La firma del diseño: arma la forma y llena (asigna la forma: para la precarga o las pruebas; en un cuadro, la instancia).
        public static void Fill(EnvironmentId id, double distance, byte[] r8x256)
        {
            new HorizonProfile(id).Fill(distance, r8x256);
        }

        public static byte Encode(double value)
        {
            var v = SceneMath.Clamp01(value / Encoding) * 255;
            return (byte)Math.Floor(v + 0.5);
        }

        public static double Decode(byte value)
        {
            return value / 255.0 * Encoding;
        }

        private static double At(double lo, double hi, double plo, double phi, double a)
        {
            if (hi - lo < 1e-12)
            {
                return Math.Max(plo, phi);
            }

            return plo + (phi - plo) * SceneMath.Clamp01((a - lo) / (hi - lo));
        }
    }
}
