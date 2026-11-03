using System;

namespace RowMate.Core.Design
{
    // Un texto con rótulo y cifras partido en tramos (QA-V5-02 de la verificación de la It. 5; H5-D2-01; ANIMATION_BIBLE §2.4: solo
    // ruedan las celdas que cambian). "Rep 2/3 · 1.112 m" → "Rep " · "2" · "/" · "3" · " · " · "1.112" · " m". Un tramo de cifras
    // son dígitos con '.', ':' o ',' ENTRE dos dígitos (el punto de miles, el reloj); el resto es texto. Dos textos con la misma
    // forma (la misma cantidad de tramos, las cifras en el mismo lugar y el texto igual) ruedan tramo por tramo: el texto no rueda y
    // cada tramo de cifras rueda alineado a su derecha, como un número de TabularNumber. Puro, sin asignar: los tramos van a
    // arreglos del que llama.
    public static class DigitRuns
    {
        // La línea Rep más larga ("Rep 999/999 · 999.999 m") tiene 7.
        public const int MaxRuns = 8;

        // Parte text en tramos (en orden): starts[i], lengths[i] y numeric[i]. Devuelve la cantidad, o −1 si no entran en
        // MaxRuns (o en los arreglos). Un texto vacío tiene 0 tramos.
        public static int Split(string text, int[] starts, int[] lengths, bool[] numeric)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (starts == null || lengths == null || numeric == null)
            {
                throw new ArgumentNullException(starts == null ? nameof(starts) : lengths == null ? nameof(lengths) : nameof(numeric));
            }

            var capacity = Math.Min(MaxRuns, Math.Min(starts.Length, Math.Min(lengths.Length, numeric.Length)));
            var count = 0;
            var i = 0;
            while (i < text.Length)
            {
                var isNumber = IsDigit(text[i]);
                var start = i;
                i++;
                while (i < text.Length && InRun(text, i, isNumber))
                {
                    i++;
                }

                if (count >= capacity)
                {
                    return -1;
                }

                starts[count] = start;
                lengths[count] = i - start;
                numeric[count] = isNumber;
                count++;
            }

            return count;
        }

        // Los dos textos tienen la misma forma: la misma cantidad de tramos, las cifras en los mismos lugares y los tramos de texto
        // iguales carácter a carácter.
        public static bool SameShape(string a, int countA, int[] startsA, int[] lengthsA, bool[] numericA,
                                     string b, int countB, int[] startsB, int[] lengthsB, bool[] numericB)
        {
            if (a == null || b == null || countA < 0 || countA != countB)
            {
                return false;
            }

            for (var r = 0; r < countA; r++)
            {
                if (numericA[r] != numericB[r])
                {
                    return false;
                }

                if (numericA[r])
                {
                    continue;
                }

                if (lengthsA[r] != lengthsB[r] || string.CompareOrdinal(a, startsA[r], b, startsB[r], lengthsA[r]) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        // Dentro de un tramo de cifras alineado a la derecha: el índice (en el texto anterior) de la celda que ocupaba el lugar de la
        // celda j (0 = la de la izquierda) del tramo nuevo, o −1 si la celda es nueva.
        public static int PreviousCell(int previousStart, int previousLength, int nextLength, int cell)
        {
            var offset = cell - (nextLength - previousLength);
            return offset >= 0 && offset < previousLength ? previousStart + offset : -1;
        }

        private static bool InRun(string text, int i, bool numeric)
        {
            var c = text[i];
            if (!numeric)
            {
                // El texto sigue hasta el próximo dígito.
                return !IsDigit(c);
            }

            if (IsDigit(c))
            {
                return true;
            }

            // Un separador entre dos dígitos es parte del número ("1.112", "0:50.0").
            return (c == '.' || c == ':' || c == ',') && i + 1 < text.Length && IsDigit(text[i + 1]);
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}
