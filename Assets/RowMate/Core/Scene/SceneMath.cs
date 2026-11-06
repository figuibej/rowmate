using System;

namespace RowMate.Core.Scene
{
    // Utilidades numéricas de la escena (01-po-spec §0.3): smooth(t) = t²·(3 − 2t) y smoothstep(a; b; x) =
    // smooth(clamp((x − a)/(b − a); 0; 1)), que también vale con a > b (las ventanas de la luna, 04-design-notes §3.2).
    public static class SceneMath
    {
        public static double Clamp01(double x)
        {
            if (x < 0)
            {
                return 0;
            }

            return x > 1 ? 1 : x;
        }

        public static double Clamp(double x, double lower, double upper)
        {
            if (x < lower)
            {
                return lower;
            }

            return x > upper ? upper : x;
        }

        public static double Smooth(double t)
        {
            return t * t * (3 - 2 * t);
        }

        public static double SmoothStep(double a, double b, double x)
        {
            return Smooth(Clamp01((x - a) / (b - a)));
        }

        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        public static double DegToRad(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double RadToDeg(double radians)
        {
            return radians * 180 / Math.PI;
        }

        // La dirección del mundo (x a la derecha, y arriba, z adelante) para un acimut y una elevación en grados.
        public static void Direction(double azimuthDeg, double elevationDeg, out double x, out double y, out double z)
        {
            var az = DegToRad(azimuthDeg);
            var el = DegToRad(elevationDeg);
            var c = Math.Cos(el);
            x = Math.Sin(az) * c;
            y = Math.Sin(el);
            z = Math.Cos(az) * c;
        }

        // El ángulo (radianes) entre dos direcciones unitarias.
        public static double AngleBetween(double ax, double ay, double az, double bx, double by, double bz)
        {
            // atan2(|a × b|, a·b): estable también para ángulos chicos.
            var cx = ay * bz - az * by;
            var cy = az * bx - ax * bz;
            var cz = ax * by - ay * bx;
            return Math.Atan2(Math.Sqrt(cx * cx + cy * cy + cz * cz), ax * bx + ay * by + az * bz);
        }
    }
}
