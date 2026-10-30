using System;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // La fila de píldoras calculada para un ancho útil y una cantidad de pasos (02-tech-design §2.2 de la It. 4b): todas del
    // mismo ancho (Q-59), con la separación que baja de 3 a 1 y, en el extremo, la tira continua (DV-40). Todo en dp BASE,
    // desde el borde izquierdo y de arriba del área de la fila (el elemento de 20 de alto); la vista lo multiplica por s y lo
    // lleva a píxeles con la densidad. Struct sin asignaciones.
    public readonly struct PillRowLayout
    {
        public PillRowLayout(int count, double usefulWidth, double gap, double pillWidth, bool strip)
        {
            Count = count;
            UsefulWidth = usefulWidth;
            Gap = gap;
            PillWidth = pillWidth;
            Strip = strip;
        }

        public int Count { get; }

        // El ancho útil de la fila (sin los rellenos de 10 y 10).
        public double UsefulWidth { get; }

        // 3; 1 si con 3 no llegan a 6 de ancho; 0 en la tira.
        public double Gap { get; }

        // El lugar de cada una, igual para todas (Q-59): (ancho útil − Gap·(n − 1)) / n.
        public double PillWidth { get; }

        // La tira continua (DV-40): ni con separación 1 llegan a 2 de ancho.
        public bool Strip { get; }

        // PillWidth < 6 (en cualquier régimen): la en curso es un pulgar y la hueca, dos rieles (DN4b-18; AC-WKT-14-16).
        public bool Narrow => Count > 0 && PillWidth < PillRowGeometry.NarrowWidth;

        // El borde izquierdo del lugar del paso, desde el borde izquierdo del área de la fila.
        public double LeftOf(int index)
        {
            return PillRowGeometry.PadLeft + index * (PillWidth + Gap);
        }

        // La píldora del paso: su lugar en la franja de 8 (y de 8 a 16).
        public DpRect PillOf(int index)
        {
            return new DpRect(LeftOf(index), PillRowGeometry.PadTop, PillWidth, PillRowGeometry.Height);
        }

        // El pulgar de la en curso (DN4b-18; AC-WKT-14-16): max(4; PillWidth) de ancho × 10 de alto (1 arriba y 1 abajo de la
        // franja de 8), centrado en su lugar y acotado a la fila. Sin Narrow, la píldora misma.
        public DpRect ThumbOf(int index)
        {
            if (!Narrow)
            {
                return PillOf(index);
            }

            var width = Math.Max(PillRowGeometry.ThumbMinWidth, PillWidth);
            var center = LeftOf(index) + PillWidth / 2;
            var left = center - width / 2;
            var min = PillRowGeometry.PadLeft;
            var max = PillRowGeometry.PadLeft + UsefulWidth - width;
            left = left < min ? min : left > max ? max : left;
            var top = PillRowGeometry.PadTop - (PillRowGeometry.ThumbHeight - PillRowGeometry.Height) / 2;
            return new DpRect(left, top, width, PillRowGeometry.ThumbHeight);
        }

        // El radio del pulgar: min(4; ancho / 2).
        public double ThumbRadiusOf(int index)
        {
            return Math.Min(PillRowGeometry.Radius, ThumbOf(index).Width / 2);
        }
    }

    // La fila de píldoras (immersive_workout_screen.dart:358-377; step_pill.dart:57-60, :103; 04b/00-deuda-diseno §4.4;
    // 04-design-notes §3.2; DN4b-18). El legacy reparte con Expanded separados por 3 px: con más de ≈ 120 pasos en un teléfono
    // cada píldora mide 0 y la fila queda vacía (DV-40). Pura: la prueban PillRowGeometryTests.
    public static class PillRowGeometry
    {
        // :359-360: el relleno de la fila.
        public const double PadLeft = 10, PadTop = 8, PadRight = 10, PadBottom = 4;

        // step_pill.dart:57-60 (alto 8, radio 4) y :364 (separación 3).
        public const double Height = 8, Radius = 4, Gap = 3, NarrowGap = 1;

        // 04b/00-deuda-diseno §4.4: la separación 3 mientras las píldoras miden 6 o más; la 1 mientras miden 2 o más.
        public const double MinWidthWithGap = 6, MinWidthNarrow = 2;

        // DN4b-18 (sale StripCurrentMin 2): debajo de 6, el pulgar de al menos 4 × 10 y los rieles.
        public const double NarrowWidth = 6, ThumbMinWidth = 4, ThumbHeight = 10;

        // El borde de la barra (1, en base) y su margen a la ventana (12·s): el ancho útil sale de la ventana.
        public const double BarBorder = 1;

        // 20 = 8 + 8 + 4.
        public static double RowHeight => PadTop + Height + PadBottom;

        // El ancho útil de la fila en base para una ventana (dp) y su escala: la barra a 12·s de cada lado, menos el borde y el
        // relleno de 10 y 10 (TV y P360 344,0; TH 821,3; L640 647,3; W1280 754,0).
        public static double UsefulWidthBase(double windowWidthDp, double scale)
        {
            return windowWidthDp / scale - 2 * HudGeometry.TopBarSide - 2 * BarBorder - PadLeft - PadRight;
        }

        public static PillRowLayout Layout(double usefulWidthBase, int count)
        {
            if (count <= 0 || usefulWidthBase <= 0)
            {
                return new PillRowLayout(0, Math.Max(0, usefulWidthBase), 0, 0, false);
            }

            var withGap = (usefulWidthBase - Gap * (count - 1)) / count;
            if (withGap >= MinWidthWithGap)
            {
                return new PillRowLayout(count, usefulWidthBase, Gap, withGap, false);
            }

            var narrow = (usefulWidthBase - NarrowGap * (count - 1)) / count;
            if (narrow >= MinWidthNarrow)
            {
                return new PillRowLayout(count, usefulWidthBase, NarrowGap, narrow, false);
            }

            return new PillRowLayout(count, usefulWidthBase, 0, usefulWidthBase / count, true);
        }

        // Bordes en píxeles enteros de cada lugar (izquierdo y derecho), así ningún lugar se ve más ancho que otro en más de 1
        // px (AC-WKT-14-08). originPx: el píxel del borde izquierdo del área de la fila; pxPerBase = s × densidad. Escribe en los
        // arreglos que le pasan (sin asignar).
        public static void PixelEdges(in PillRowLayout layout, double originPx, double pxPerBase, int[] left, int[] right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (left.Length < layout.Count || right.Length < layout.Count)
            {
                throw new ArgumentException("PillRowGeometry: los arreglos de bordes son más cortos que la fila.");
            }

            for (var i = 0; i < layout.Count; i++)
            {
                var x0 = layout.LeftOf(i);
                left[i] = (int)DartMath.Round(originPx + x0 * pxPerBase);
                right[i] = (int)DartMath.Round(originPx + (x0 + layout.PillWidth) * pxPerBase);
            }
        }
    }
}
