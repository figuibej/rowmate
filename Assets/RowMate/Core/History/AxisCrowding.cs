using System;
using RowMate.Core.Design;

namespace RowMate.Core.History
{
    // Etiquetas de eje sin encimarse (It. 14, #32, AC-DEBT-07, DV-137; DT14-28): una marca INTERMEDIA cuya etiqueta quedaría a
    // menos de 4 dp de la etiqueta de un borde (la primera o la última) no se dibuja, en X y en Y (p. ej. nunca 3:44 pegado a
    // 5:00, ni 25 con 26). Las de los bordes siempre quedan. Cambio frente a fl_chart (AC-HIS-13-05), aceptado por el PO.
    // Lo usa ChartAxisLabels (WP-D). Sin asignaciones: escribe en el arreglo que recibe.
    public static class AxisCrowding
    {
        public const double MinGapDp = 4;

        // Etiquetas centradas en su marca: centers (dp, en orden creciente) y su extensión sobre el eje (ancho en X, alto en Y).
        // visible[i] = false para las intermedias que chocan con un borde. Devuelve cuántas quedan.
        public static int DropCrowded(double[] centersDp, double[] labelExtentsDp, bool[] visible, int count, double minGapDp = MinGapDp)
        {
            if (centersDp == null || labelExtentsDp == null || visible == null)
            {
                throw new ArgumentNullException(centersDp == null ? nameof(centersDp) : labelExtentsDp == null ? nameof(labelExtentsDp) : nameof(visible));
            }

            if (count < 0 || count > centersDp.Length || count > labelExtentsDp.Length || count > visible.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "AxisCrowding: los arreglos son más cortos.");
            }

            var kept = 0;
            for (var i = 0; i < count; i++)
            {
                var keep = true;
                if (i > 0 && i < count - 1)
                {
                    keep = Gap(centersDp, labelExtentsDp, i, 0) >= minGapDp && Gap(centersDp, labelExtentsDp, i, count - 1) >= minGapDp;
                }

                visible[i] = keep;
                if (keep)
                {
                    kept++;
                }
            }

            return kept;
        }

        // It. 15 (AC-D15-25, DT15-22): el ancho de una etiqueta de eje en RowMate Numeric SC 600 (los avances de ChartSpecs, medidos
        // en el hmtx de la fuente), sin medir el Label. Un carácter que no es cifra, ":", ".", "-" ni espacio cuenta como una cifra.
        public static double LabelWidthDp(string text, double fontSizeDp = ChartSpecs.AxisFontSizeDp)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            double em = 0;
            for (var i = 0; i < text.Length; i++)
            {
                em += AdvanceEm(text[i]);
            }

            return em * fontSizeDp;
        }

        // El avance de un carácter de los ejes, en em.
        public static double AdvanceEm(char c)
        {
            switch (c)
            {
                case ':':
                    return ChartSpecs.AxisColonAdvanceEm;
                case '.':
                    return ChartSpecs.AxisPeriodAdvanceEm;
                case '-':
                    return ChartSpecs.AxisMinusAdvanceEm;
                case ' ':
                    return ChartSpecs.AxisSpaceAdvanceEm;
                default:
                    return ChartSpecs.AxisDigitAdvanceEm;
            }
        }

        // El aire entre dos etiquetas centradas (negativo si se pisan).
        public static double Gap(double[] centersDp, double[] labelExtentsDp, int a, int b)
        {
            return Math.Abs(centersDp[a] - centersDp[b]) - (labelExtentsDp[a] + labelExtentsDp[b]) / 2;
        }
    }

    // El rango del eje Y del split (It. 14, #32, AC-DEBT-07, DV-137): con un split casi constante (rango < 10 s, p. ej. el
    // simulador), se abre a ±5 s del valor central, sin bajar de 0. Así la curva no queda en el piso y el eje no va de 2:13 a 0:33.
    public static class SplitRange
    {
        public const double MinSpanSeconds = 10;

        public static void Widen(double min, double max, out double widenedMin, out double widenedMax)
        {
            if (double.IsNaN(min) || double.IsNaN(max) || max < min)
            {
                throw new ArgumentException("SplitRange: rango inválido (" + min + " a " + max + ").");
            }

            if (max - min >= MinSpanSeconds)
            {
                widenedMin = min;
                widenedMax = max;
                return;
            }

            var center = (min + max) / 2;
            widenedMin = center - MinSpanSeconds / 2;
            if (widenedMin < 0)
            {
                widenedMin = 0;
            }

            widenedMax = widenedMin + MinSpanSeconds;
        }
    }
}
