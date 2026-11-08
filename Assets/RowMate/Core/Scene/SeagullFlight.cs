using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Las 5 gaviotas de la costa (coast_environment.dart:67-87; AC-SCN-19-04; DV-89; 02-tech-design §2.7): el recorrido del legacy
    // en TV pasado a ángulos. gx = ((t·(8 + 3i) + 170i) mod (w + 60)) − 30 px → acimut (gx − w/2)/411 rad; gy = horizonte·(0,35 +
    // 0,1i) + 4·sin(2t + i) px → elevación atan((horizonte − gy)/411); aleteo sin(6t + i); tamaño (5 + i)/5 de la envergadura.
    // Al salir por la derecha vuelven por la izquierda con un fundido de 1 s (medio segundo de cada lado), sin saltar.
    public static class SeagullFlight
    {
        public const int Count = CoastArt.SeagullCount;
        private const double SpanPx = LegacyTv.Width + 60;

        public static double SpeedPxPerSecond(int i) => 8 + 3 * i;

        public static void Sample(int i, double sceneTime, out double azimuthRad, out double elevationRad, out double flap, out double alpha)
        {
            if (i < 0 || i >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(i), i, "SeagullFlight: 5 gaviotas.");
            }

            var speed = SpeedPxPerSecond(i);
            var u = Text.DartMath.Mod(sceneTime * speed + i * 170, SpanPx);
            var gx = u - 30;
            azimuthRad = LegacyTv.PxToRad(gx - LegacyTv.Width / 2);
            var gy = LegacyTv.HorizonY * (0.35 + 0.1 * i) + Math.Sin(sceneTime * 2 + i) * 4;
            elevationRad = Math.Atan((LegacyTv.HorizonY - gy) / LegacyTv.Focal);
            flap = Math.Sin(sceneTime * 6 + i);
            var half = CoastArt.SeagullFadeSeconds / 2;
            var since = u / speed;
            var until = (SpanPx - u) / speed;
            alpha = SceneMath.Clamp01(since / half) * SceneMath.Clamp01(until / half);
        }

        // (5 + i)/5 de la envergadura.
        public static double SizeScale(int i) => (5 + i) / 5.0;

        // La opacidad del legacy: 0,5 + 0,4·ambiente.
        public static double Opacity(double ambient) => 0.5 + 0.4 * ambient;
    }
}
