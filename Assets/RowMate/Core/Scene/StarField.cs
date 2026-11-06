using System;
using RowMate.Core.Design;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Una estrella del legacy en dp de la pantalla del legacy (sky_painter.dart:62-64).
    public readonly struct StarPoint
    {
        public StarPoint(double x, double y, double radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public double X { get; }

        public double Y { get; }

        public double Radius { get; }
    }

    // Las 80 estrellas del legacy (sky_painter.dart:49-66; AC-SCN-11-03, -04; Q-62, DV-53). Blancas: de la paleta sale solo su
    // opacidad (AC-SCN-09-04).
    public static class StarField
    {
        public const int Count = 80;
        public const int Seed = 42;

        // Random(42) con las tres tiradas por estrella EN ORDEN: x = u₁·ancho, y = u₂·horizonte·0,85, radio = 0,6 + u₃ (:59-64).
        // Con el generador del llamador (Reseed: sin asignar).
        public static void Layout(double width, double horizonY, Span<StarPoint> into, DartRandom rnd)
        {
            if (rnd == null)
            {
                throw new ArgumentNullException(nameof(rnd));
            }

            if (into.Length < Count)
            {
                throw new ArgumentException("StarField.Layout: hacen falta " + Count + " lugares.", nameof(into));
            }

            rnd.Reseed(Seed);
            for (var i = 0; i < Count; i++)
            {
                var x = rnd.NextDouble() * width;
                var y = rnd.NextDouble() * horizonY * 0.85;
                var r = 0.6 + rnd.NextDouble();
                into[i] = new StarPoint(x, y, r);
            }
        }

        public static void Layout(double width, double horizonY, Span<StarPoint> into)
        {
            Layout(width, horizonY, into, new DartRandom(Seed));
        }

        // La oscuridad del legacy, clamp((1 − a)/0,65; 0; 1) (:51).
        public static double LegacyDarkness(double ambient)
        {
            return SceneMath.Clamp01((1 - ambient) / 0.65);
        }

        // Q-62 (DV-53): con ambiente > 0,85 no hay estrellas.
        public static bool IsShown(double ambient)
        {
            return ambient <= SkyArt.StarsMaxAmbient;
        }

        // El valor de régimen: la oscuridad del legacy si se ven, 0 si no.
        public static double Visibility(double ambient)
        {
            return IsShown(ambient) ? LegacyDarkness(ambient) : 0;
        }

        // (0,4 + 0,5·titileo)·oscuridad·fundido con titileo = 0,5 + 0,5·sin(2t + i) (:65-66). Con fade = 1 en régimen es la fórmula
        // de la spec; el fundido lo da StarFader.
        public static double Alpha(int i, double sceneTime, double ambient, double fade)
        {
            var twinkle = 0.5 + 0.5 * Math.Sin(sceneTime * 2 + i);
            return (0.4 + 0.5 * twinkle) * LegacyDarkness(ambient) * fade;
        }
    }

    // El fundido del conjunto de estrellas (DN6a-05, DV-53): 0,9 s con ease.move cuando IsShown cambia, en el tiempo real del
    // cuadro; sigue con reducir movimiento (es un fundido). Snap al abrir, al volver del segundo plano y con ForceHourForTest.
    public struct StarFader
    {
        public const double Seconds = MotionSpecs.StarFadeSeconds;

        private double _progress;

        // 0-1, ya con la curva.
        public double Level => Easing.Evaluate(Tokens.EaseMove, _progress);

        public void Step(bool shown, double dt)
        {
            var step = dt <= 0 ? 0 : dt / Seconds;
            _progress = shown ? Math.Min(1, _progress + step) : Math.Max(0, _progress - step);
        }

        public void Snap(bool shown)
        {
            _progress = shown ? 1 : 0;
        }
    }
}
