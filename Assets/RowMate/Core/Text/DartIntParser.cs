using System.Numerics;

namespace RowMate.Core.Text
{
    // = int.tryParse(texto) de Dart, sin base (AC-RUT-13-05; <flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm_shared/lib/
    // integers_patch.dart:84-125, _parseRadix :153-269, _parseBlock :271-294). La gramática es la de Dart:
    // - espacios de Dart a los costados (DartText.IsWhitespace), nunca en el medio ("1␣5" no es un número);
    // - un signo opcional '+' o '-' (ASCII; "−2" con U+2212 no es un número), uno solo ("--2" y "+-2" no lo son);
    // - "0x" o "0X" después del signo → hexadecimal (P-04: "0x10" = 16, "-0x10" = −16); "0x" solo no es un número;
    // - ceros a la izquierda ("0150" = 150) y solo dígitos ASCII ("５" de ancho completo no es un número).
    // Diferencias a propósito (DV-17, DT-11): Dart lee enteros de 64 bits y un decimal que no entra da null; un hexadecimal
    // positivo de hasta 64 bits da la vuelta (0xFFFFFFFFFFFFFFFF = −1). Acá el resultado es el valor matemático EXACTO, de
    // cualquier largo, así un número enorme es "fuera de rango" (Q-92) y no "vacío". Nunca desborda ni lanza.
    public static class DartIntParser
    {
        // Cifras que entran en un ulong sin desbordar en cada bloque (10^19 > 2^64 − 1 > 10^18; 16^16 = 2^64).
        private const int DecimalBlock = 18;
        private const int HexBlock = 15;
        private static readonly BigInteger DecimalBlockFactor = BigInteger.Pow(10, DecimalBlock);
        private static readonly BigInteger HexBlockFactor = BigInteger.Pow(16, HexBlock);

        // null = vacío, solo espacios o no numérico (lo que el legacy trata como "vacío" con ?? 0 / ?? 1).
        public static BigInteger? TryParse(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var end = text!.Length;
            while (end > 0 && DartText.IsWhitespace(text[end - 1]))
            {
                end--;
            }

            if (end == 0)
            {
                return null;
            }

            var start = 0;
            while (DartText.IsWhitespace(text[start]))
            {
                start++;
            }

            var negative = false;
            var first = text[start];
            if (first == '+' || first == '-')
            {
                negative = first == '-';
                start++;
                if (start == end)
                {
                    return null;
                }
            }

            var hex = false;
            if (text[start] == '0')
            {
                // integers_patch.dart:109-118: el primer '0' se consume; si sigue una x es hexadecimal.
                start++;
                if (start == end)
                {
                    return BigInteger.Zero;
                }

                if ((text[start] | 0x20) == 'x')
                {
                    start++;
                    if (start == end)
                    {
                        return null;
                    }

                    hex = true;
                }
            }

            var magnitude = hex ? ParseDigits(text, start, end, 16, HexBlock, HexBlockFactor)
                                : ParseDigits(text, start, end, 10, DecimalBlock, DecimalBlockFactor);
            if (!magnitude.HasValue)
            {
                return null;
            }

            return negative ? -magnitude.Value : magnitude.Value;
        }

        // Dígitos de text[start..end) en la base dada, de a bloques que entran en un ulong (lineal en el largo del texto).
        private static BigInteger? ParseDigits(string text, int start, int end, int radix, int blockSize, BigInteger blockFactor)
        {
            var result = BigInteger.Zero;
            var block = 0UL;
            var inBlock = 0;
            for (var i = start; i < end; i++)
            {
                var digit = Digit(text[i], radix);
                if (digit < 0)
                {
                    return null;
                }

                block = block * (ulong)radix + (ulong)digit;
                inBlock++;
                if (inBlock == blockSize)
                {
                    result = result * blockFactor + block;
                    block = 0;
                    inBlock = 0;
                }
            }

            if (inBlock > 0)
            {
                result = result * BigInteger.Pow(radix, inBlock) + block;
            }

            return result;
        }

        // = _parseBlock: '0'-'9' y, en hexadecimal, 'a'-'f' / 'A'-'F' (solo ASCII). −1 si no es un dígito de la base.
        private static int Digit(char c, int radix)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (radix == 16)
            {
                var lower = c | 0x20;
                if (lower >= 'a' && lower <= 'f')
                {
                    return lower - 'a' + 10;
                }
            }

            return -1;
        }
    }
}
