namespace RowMate.Core.Design
{
    // El corazón del indicador del aviso del pulsómetro (It. 11; A11-04; 00-arte-pulsometro §5): el favorite de Material en su caja de
    // 24 × 24 (y hacia abajo), como tramos cúbicos, y su versión rota (heart_broken) con una grieta de la muesca a la punta. LinkIndicator
    // lo dibuja con Painter2D en su propio elemento (sin fuente ni elementos nuevos) y sin asignar: los arreglos son estáticos.
    // El contorno izquierdo va de la muesca (12; 5,09) a la punta (12; 21,35); el derecho es su espejo (x → 24 − x).
    public static class HeartGlyph
    {
        public const double ViewBox = 24;
        public const double NotchX = 12, NotchY = 5.09;
        public const double TipX = 12, TipY = 21.35;

        // Hueco de la versión rota: cada mitad se corre BrokenGap (en unidades de la caja) hacia su lado.
        public const double BrokenGap = 0.9;

        // Tramos cúbicos (c1x, c1y, c2x, c2y, x, y) desde la muesca; el último es la recta hasta la punta (controles en sus extremos).
        public static readonly double[] LeftContour =
        {
            10.91, 3.81, 9.24, 3, 7.5, 3,
            4.42, 3, 2, 5.42, 2, 8.5,
            2, 12.28, 5.4, 15.36, 10.55, 20.03,
            10.55, 20.03, 12, 21.35, 12, 21.35,
        };

        public static int Segments => LeftContour.Length / 6;

        // La grieta, de la muesca a la punta (pares x, y): baja en zigzag cerca del centro.
        public static readonly double[] Crack =
        {
            12, 5.09,
            10.6, 9.4,
            13.1, 12.3,
            11.1, 16.2,
            12, 21.35,
        };

        public static double MirrorX(double x)
        {
            return ViewBox - x;
        }

        // El inicio del tramo i (la muesca para el primero).
        public static void SegmentStart(int i, out double x, out double y)
        {
            if (i <= 0)
            {
                x = NotchX;
                y = NotchY;
                return;
            }

            x = LeftContour[i * 6 - 2];
            y = LeftContour[i * 6 - 1];
        }

        // La caja de los puntos extremos del contorno (los tramos del favorite son tangentes a la caja en sus extremos).
        public static void Bounds(out double minX, out double minY, out double maxX, out double maxY)
        {
            minX = NotchX;
            maxX = NotchX;
            minY = NotchY;
            maxY = NotchY;
            for (var i = 0; i < LeftContour.Length; i += 6)
            {
                var x = LeftContour[i + 4];
                var y = LeftContour[i + 5];
                minX = x < minX ? x : minX;
                maxX = MirrorX(x) > maxX ? MirrorX(x) : maxX;
                minY = y < minY ? y : minY;
                maxY = y > maxY ? y : maxY;
            }
        }
    }
}
