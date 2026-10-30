using System;

namespace RowMate.Core.Design
{
    // El trazo del check del diálogo de completado como polilínea parcial (02-tech-design §3.9 de la It. 4b; 04-design-notes
    // §3.8; DN4b-21): la línea central del check_circle de Material, en radios del disco (R = 16) con y hacia abajo: P0
    // (−0,629; −0,071), P1 (−0,200; +0,359), P2 (+0,630; −0,471). Grosor 0,20·R, unión en inglete, extremos rectos; largo
    // 1,780·R en dos tramos de 0,607·R y 1,173·R. Se dibuja de P0 a P2 en proporción al largo. Pura y sin asignaciones.
    public static class CheckGlyph
    {
        public const double P0X = -0.629, P0Y = -0.071;
        public const double P1X = -0.200, P1Y = 0.359;
        public const double P2X = 0.630, P2Y = -0.471;

        // El grosor del trazo, en radios del disco.
        public const double StrokeRadii = 0.20;

        public static double FirstLength => Length(P0X, P0Y, P1X, P1Y);

        public static double SecondLength => Length(P1X, P1Y, P2X, P2Y);

        public static double TotalLength => FirstLength + SecondLength;

        // El trazo hasta la fracción dada (0 → nada, 1 → entero) en un disco de centro (cx; cy) y radio r, en coordenadas de la
        // vista. Devuelve cuántos puntos valen (0, 2 o 3) y los escribe en x0..x2 / y0..y2.
        public static int Partial(double fraction, double cx, double cy, double radius, out double x0, out double y0, out double x1,
                                  out double y1, out double x2, out double y2)
        {
            var f = double.IsNaN(fraction) || fraction <= 0 ? 0 : fraction >= 1 ? 1 : fraction;
            x0 = cx + P0X * radius;
            y0 = cy + P0Y * radius;
            x1 = x0;
            y1 = y0;
            x2 = x0;
            y2 = y0;
            if (f <= 0)
            {
                return 0;
            }

            var walked = f * TotalLength;
            if (walked <= FirstLength)
            {
                var u = walked / FirstLength;
                x1 = cx + (P0X + (P1X - P0X) * u) * radius;
                y1 = cy + (P0Y + (P1Y - P0Y) * u) * radius;
                return 2;
            }

            x1 = cx + P1X * radius;
            y1 = cy + P1Y * radius;
            var v = (walked - FirstLength) / SecondLength;
            x2 = cx + (P1X + (P2X - P1X) * v) * radius;
            y2 = cy + (P1Y + (P2Y - P1Y) * v) * radius;
            return 3;
        }

        private static double Length(double ax, double ay, double bx, double by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
