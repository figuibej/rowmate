using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Escalas de letra de la app (AC-APP-05-05, P-03: tope 1,3×): la escala del sistema se cuantiza a uno de estos cuatro
    // escalones (los de Android: pequeña, normal, grande, la más grande) y #app-root lleva la clase rm-text-scale-<n>, que
    // Typography.uss usa para agrandar cada .rm-type-* (TypographyUssWriter).
    public static class TextScaleSteps
    {
        public const double Min = 0.85;
        public const double Max = 1.3;

        private static readonly double[] Values = { 0.85, 1.0, 1.15, 1.3 };

        // 0,85 · 1 · 1,15 · 1,3, en orden.
        public static IReadOnlyList<double> Steps => Values;

        // El escalón más cercano a la escala del sistema, entre 0,85 y 1,3 (más chica → 0,85; más grande → 1,3, P-03).
        // Empate entre dos escalones → el más grande (la accesibilidad pide agrandar). Un valor inválido (NaN, ≤ 0) → 1.
        // Android 14 suma 1,5 / 1,8 / 2,0 y Windows llega a 2,25: los dos caen en 1,3.
        public static double Quantize(double systemScale)
        {
            if (double.IsNaN(systemScale) || systemScale <= 0)
            {
                return 1.0;
            }

            var best = Values[0];
            var bestDistance = Math.Abs(systemScale - best);
            for (var i = 1; i < Values.Length; i++)
            {
                var distance = Math.Abs(systemScale - Values[i]);
                if (distance <= bestDistance + 1e-9)
                {
                    best = Values[i];
                    bestDistance = distance;
                }
            }

            return best;
        }

        // "rm-text-scale-085" | "rm-text-scale-100" | "rm-text-scale-115" | "rm-text-scale-130".
        public static string ClassName(double step)
        {
            var hundredths = DartMath.Round(step * 100);
            for (var i = 0; i < Values.Length; i++)
            {
                if (DartMath.Round(Values[i] * 100) == hundredths)
                {
                    var digits = Fmt.Integer(hundredths);
                    return "rm-text-scale-" + (digits.Length < 3 ? new string('0', 3 - digits.Length) + digits : digits);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(step), step, "TextScaleSteps: " + Fmt.DartToString(step)
                + " no es un escalón (usar Quantize).");
        }
    }
}
