using System;

namespace RowMate.Core.History
{
    // Reducción de la curva por columna (DT9-10; arte §7.2): solo si hay más puntos que columnas. Por cada columna, el índice
    // del mínimo y el del máximo, en el orden en que ocurren (así no se pierden los picos ni las caídas a 0); si los dos dan el
    // mismo valor, uno solo (un tramo de largo 0 no tiene normal). Las columnas sin puntos no aportan nada; el primer punto y el
    // último siempre están (la curva va de punta a punta del eje X). Sin asignar: escribe en outIndices, en orden creciente, y
    // devuelve cuántos son (a lo sumo Capacity(count, columns)).
    public static class ChartDecimation
    {
        public static int MinMaxPerColumn(double[] xs, float[] ys, int count, double x0, double x1, int columns, int[] outIndices)
        {
            if (xs == null)
            {
                throw new ArgumentNullException(nameof(xs));
            }

            if (ys == null)
            {
                throw new ArgumentNullException(nameof(ys));
            }

            if (outIndices == null)
            {
                throw new ArgumentNullException(nameof(outIndices));
            }

            if (count < 0 || count > xs.Length || count > ys.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (columns < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(columns), columns, "ChartDecimation: al menos una columna.");
            }

            if (count <= columns || !(x1 > x0))
            {
                if (outIndices.Length < count)
                {
                    throw new ArgumentException("ChartDecimation: los índices no entran.", nameof(outIndices));
                }

                for (var i = 0; i < count; i++)
                {
                    outIndices[i] = i;
                }

                return count;
            }

            var written = 0;
            var scale = columns / (x1 - x0);
            var i0 = 0;
            while (i0 < count)
            {
                var column = Column(xs[i0], x0, scale, columns);
                var min = i0;
                var max = i0;
                var i = i0 + 1;
                while (i < count && Column(xs[i], x0, scale, columns) == column)
                {
                    if (ys[i] < ys[min])
                    {
                        min = i;
                    }

                    if (ys[i] > ys[max])
                    {
                        max = i;
                    }

                    i++;
                }

                if (ys[min] == ys[max])
                {
                    written = PutAfter(outIndices, written, i0);
                }
                else if (min < max)
                {
                    written = PutAfter(outIndices, written, min);
                    written = PutAfter(outIndices, written, max);
                }
                else
                {
                    written = PutAfter(outIndices, written, max);
                    written = PutAfter(outIndices, written, min);
                }

                i0 = i;
            }

            // La curva siempre llega al último punto (el fin del eje X).
            return PutAfter(outIndices, written, count - 1);
        }

        // Capacidad de outIndices que alcanza siempre: 2 por columna, el primero y el último.
        public static int Capacity(int count, int columns)
        {
            return Math.Min(count, 2 * columns + 2);
        }

        // Agrega el índice si va después del último escrito (el primero de la curva siempre es el 0).
        private static int PutAfter(int[] indices, int count, int value)
        {
            if (count == 0 && value != 0)
            {
                count = Put(indices, count, 0);
            }

            if (count > 0 && indices[count - 1] >= value)
            {
                return count;
            }

            return Put(indices, count, value);
        }

        // La columna de x en [0; columns − 1] (la última incluye el borde derecho).
        public static int Column(double x, double x0, double scale, int columns)
        {
            var c = (int)Math.Floor((x - x0) * scale);
            if (c < 0)
            {
                return 0;
            }

            return c >= columns ? columns - 1 : c;
        }

        private static int Put(int[] indices, int count, int value)
        {
            if (count >= indices.Length)
            {
                throw new ArgumentException("ChartDecimation: los índices no entran.", nameof(indices));
            }

            indices[count] = value;
            return count + 1;
        }
    }
}
