using System;
using RowMate.Core.Design;

namespace RowMate.Core.History
{
    // El suavizado de la curva (AC-HIS-13-06: isCurved con curveSmoothness 0,2; DT9-10). Es la cúbica de fl_chart 1.1.1
    // (generateNormalBarPath, lib/src/chart/line_chart/line_chart_painter.dart:565-640) sin preventCurveOverShooting (su
    // valor por defecto): en el tramo i, control1 = anterior + temp(i − 1), temp(i) = (siguiente − anterior) / 2 · 0,2 y
    // control2 = actual − temp(i). Un tramo se subdivide solo si mide 3 dp o más en x, en ⌈dx / 3⌉ partes hasta 8; más denso,
    // va recto (el suavizado no se ve). La cúbica es afín: se calcula en (x en dp, valor) y la escala Y se aplica después, así
    // un yMax animado no obliga a rehacerla. La y nunca baja de yFloor (la base del área). Sin asignar.
    public static class ChartSmoothing
    {
        // Lugar que alcanza siempre en outX/outY: cada punto, las partes de los tramos largos y un margen.
        public static int Capacity(int count, double spanDp, double minSpacing = ChartSpecs.SmoothMinSpacing)
        {
            if (count <= 0)
            {
                return 0;
            }

            var bySpan = minSpacing > 0 ? (int)Math.Ceiling(Math.Max(0, spanDp) / minSpacing) : 0;
            return 2 * count + bySpan + 1;
        }

        public static int Tessellate(float[] xs, float[] ys, int count, float minSpacing, float yFloor, float[] outX, float[] outY,
                                     float smoothness = (float)ChartSpecs.CurveSmoothness, int maxParts = ChartSpecs.SmoothMaxParts)
        {
            if (xs == null)
            {
                throw new ArgumentNullException(nameof(xs));
            }

            if (ys == null)
            {
                throw new ArgumentNullException(nameof(ys));
            }

            if (outX == null)
            {
                throw new ArgumentNullException(nameof(outX));
            }

            if (outY == null)
            {
                throw new ArgumentNullException(nameof(outY));
            }

            if (count < 0 || count > xs.Length || count > ys.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return 0;
            }

            var written = Put(outX, outY, 0, xs[0], Math.Max(ys[0], yFloor));
            float tempX = 0;
            float tempY = 0;
            for (var i = 1; i < count; i++)
            {
                var prevX = xs[i - 1];
                var prevY = ys[i - 1];
                var curX = xs[i];
                var curY = ys[i];
                var next = i + 1 < count ? i + 1 : i;
                var nextX = xs[next];
                var nextY = ys[next];

                var c1x = prevX + tempX;
                var c1y = prevY + tempY;
                tempX = (nextX - prevX) / 2 * smoothness;
                tempY = (nextY - prevY) / 2 * smoothness;
                var c2x = curX - tempX;
                var c2y = curY - tempY;

                var dx = curX - prevX;
                if (minSpacing > 0 && dx >= minSpacing)
                {
                    var parts = Math.Min(maxParts, (int)Math.Ceiling(dx / minSpacing));
                    for (var k = 1; k < parts; k++)
                    {
                        var t = (float)k / parts;
                        var u = 1 - t;
                        var b0 = u * u * u;
                        var b1 = 3 * u * u * t;
                        var b2 = 3 * u * t * t;
                        var b3 = t * t * t;
                        var x = b0 * prevX + b1 * c1x + b2 * c2x + b3 * curX;
                        var y = b0 * prevY + b1 * c1y + b2 * c2y + b3 * curY;
                        written = Put(outX, outY, written, x, Math.Max(y, yFloor));
                    }
                }

                written = Put(outX, outY, written, curX, Math.Max(curY, yFloor));
            }

            return written;
        }

        private static int Put(float[] outX, float[] outY, int at, float x, float y)
        {
            if (at >= outX.Length || at >= outY.Length)
            {
                throw new ArgumentException("ChartSmoothing: la curva no entra en el arreglo.", nameof(outX));
            }

            outX[at] = x;
            outY[at] = y;
            return at + 1;
        }
    }
}
