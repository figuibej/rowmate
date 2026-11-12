using System;
using RowMate.Core.Scene.Art;
using RowMate.Core.Text;

namespace RowMate.Core.Scene.Water
{
    // Lo que el shader del agua avanzada recibe ya reducido en double (Q-68; AC-6B-20; 00-arte-agua §3.3): el agua trabaja en cinta
    // (relZ = z − D, |relZ| ≤ 750 m) y nunca ve la distancia de escena. Fases de B0/B1 en [0; 2π); corrimientos de los mapas en [0; 1)
    // (uv = R·(x; relZ)/T + corrimiento) y el ruido de los parches con su período de 256 m. La malla ya no se ancla (DT6b-27: queda quieta
    // en cinta, WaterVertexField).
    public struct WaterPhaseSet
    {
        public double Phase1, Phase2;
        public double B2U, B2V, B3U, B3V, B4U, B4V;
        public double NoiseU, NoiseV;
    }

    public static class WavePhases
    {
        public const double TwoPi = 2 * Math.PI;
        public const double NoisePeriod = 256;   // el período de _RmNoiseTex (como Foam)

        public static void Compute(double distance, double sceneTime, in WaterProfile p, ref WaterPhaseSet into)
        {
            // CoastSwell.Phases generalizado: φ₁ = Mod(D/s + 0,6t; 2π), φ₂ = Mod(0,7·D/s + 0,9t; 2π).
            CoastSwell.Phases(distance, sceneTime, p.Scale, out into.Phase1, out into.Phase2);
            var b2 = p.B2;
            var b3 = p.B3;
            BandOffset(distance, sceneTime, b2.Tile, b2.Drift, b2.DirectionDeg, 0, out into.B2U, out into.B2V);
            BandOffset(distance, sceneTime, b3.Tile, b3.Drift, b3.DirectionDeg, 0, out into.B3U, out into.B3V);
            BandOffset(distance, sceneTime, b3.Tile * WaterLookArt.B4TileScale, b3.Drift, b3.DirectionDeg, WaterLookArt.B4RotationDeg,
                out into.B4U, out into.B4V);
            into.NoiseU = 0;
            into.NoiseV = DartMath.Mod((distance + p.CurrentMps * sceneTime) / NoisePeriod, 1.0);
        }

        // El corrimiento de una banda: la muestra en (x; z de mundo) es R(ρ)·((x; z) − v·t·θ̂)/T; en cinta, z = relZ + D, así que lo que
        // depende de D y de t va acá, en double, módulo 1 (al dar la vuelta salta un tile entero y el mapa no tiene costura).
        public static void BandOffset(double distance, double sceneTime, double tile, double drift, double directionDeg, double rotationDeg,
                                      out double u, out double v)
        {
            var th = SceneMath.DegToRad(directionDeg);
            var ax = -drift * sceneTime * Math.Sin(th);
            var az = distance - drift * sceneTime * Math.Cos(th);
            Rotate(ax, az, rotationDeg, out var rx, out var rz);
            u = DartMath.Mod(rx / tile, 1.0);
            v = DartMath.Mod(rz / tile, 1.0);
        }

        // La coordenada del mapa en un punto de mundo (la referencia de las pruebas: lo mismo que el shader con el corrimiento, salvo
        // el entero).
        public static void WorldUv(double x, double zWorld, double sceneTime, double tile, double drift, double directionDeg, double rotationDeg,
                                   out double u, out double v)
        {
            var th = SceneMath.DegToRad(directionDeg);
            Rotate(x - drift * sceneTime * Math.Sin(th), zWorld - drift * sceneTime * Math.Cos(th), rotationDeg, out var rx, out var rz);
            u = rx / tile;
            v = rz / tile;
        }

        // Lo que hace el shader en cinta: R·(x; relZ)/T + corrimiento.
        public static void TapeUv(double x, double relZ, double tile, double rotationDeg, double offsetU, double offsetV, out double u, out double v)
        {
            Rotate(x, relZ, rotationDeg, out var rx, out var rz);
            u = rx / tile + offsetU;
            v = rz / tile + offsetV;
        }

        // La ola de B0/B1 en cinta con las fases (lo que calcula el shader) y en mundo (la fórmula del legacy).
        public static double TapeHeight(double x, double relZ, double scale, in WaterPhaseSet ph)
        {
            return CoastSwell.HeightAt(x, relZ, scale, ph.Phase1, ph.Phase2);
        }

        public static double WorldHeight(double x, double zWorld, double sceneTime, double scale)
        {
            return CoastSwell.Height(x, zWorld, sceneTime, scale);
        }

        // La diferencia de fase envuelta en (−π; π] (para comparar avances a través de la vuelta).
        public static double WrappedDelta(double from, double to)
        {
            var d = DartMath.Mod(to - from + Math.PI, TwoPi) - Math.PI;
            return d;
        }

        private static void Rotate(double x, double z, double deg, out double rx, out double rz)
        {
            if (deg == 0)
            {
                rx = x;
                rz = z;
                return;
            }

            var a = SceneMath.DegToRad(deg);
            var c = Math.Cos(a);
            var s = Math.Sin(a);
            rx = x * c - z * s;
            rz = x * s + z * c;
        }
    }
}
