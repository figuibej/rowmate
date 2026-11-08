using System;

namespace RowMate.Core.History
{
    // El punto más cercano al cursor (AC-HIS-13-07; DT9-10): búsqueda binaria en x sobre los puntos ORIGINALES (no los
    // reducidos), que están ordenados por el tiempo del monitor. En un empate gana el de la izquierda. −1 sin puntos.
    public static class ChartHitTest
    {
        public static int Nearest(double[] xs, int count, double x)
        {
            if (xs == null)
            {
                throw new ArgumentNullException(nameof(xs));
            }

            if (count < 0 || count > xs.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return -1;
            }

            if (x <= xs[0])
            {
                return 0;
            }

            if (x >= xs[count - 1])
            {
                return LeftmostEqual(xs, count - 1);
            }

            // El primero con xs[i] ≥ x.
            var low = 0;
            var high = count - 1;
            while (low < high)
            {
                var mid = low + (high - low) / 2;
                if (xs[mid] < x)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            var right = low;
            var left = right - 1;
            var nearest = x - xs[left] <= xs[right] - x ? left : right;
            return LeftmostEqual(xs, nearest);
        }

        // Con x repetidos (el punto final repetido), el primero de ellos.
        private static int LeftmostEqual(double[] xs, int index)
        {
            while (index > 0 && xs[index - 1] == xs[index])
            {
                index--;
            }

            return index;
        }
    }
}
