using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace RowMate.Core.Text
{
    // Formato de números idéntico al de Dart (ADR 012, QA-P-02). Ningún texto visible de Core/App pasa por
    // ToString("F…"), Math.Round ni la cultura del equipo (PureCodeRulesTests): todo sale de acá.
    public static class Fmt
    {
        // Caracteres máximos del camino rápido: q < 2^64 (20 cifras), '.' y '-'.
        private const int MaxFastChars = 24;

        private static readonly ulong[] Pow10 =
        {
            1UL, 10UL, 100UL, 1000UL, 10000UL, 100000UL, 1000000UL, 10000000UL, 100000000UL, 1000000000UL,
            10000000000UL, 100000000000UL, 1000000000000UL, 10000000000000UL, 100000000000000UL,
            1000000000000000UL, 10000000000000000UL, 100000000000000000UL, 1000000000000000000UL,
            10000000000000000000UL
        };

        // = double.toStringAsFixed(fractionDigits) de la VM de Dart (02-tech-design §3.1):
        // redondea el valor binario EXACTO y el empate exacto se aleja del cero; '-' si el bit de signo está
        // prendido aunque el resultado sea cero (-0.04 → "-0.0").
        public static string Fixed(double x, int fractionDigits)
        {
            CheckDigits(fractionDigits);
            if (double.IsNaN(x))
            {
                return "NaN";
            }

            // Dart: x >= 1e21 || -x >= 1e21 → x.toString() (incluye ±Infinity).
            if (x >= 1e21 || x <= -1e21)
            {
                return DartToString(x);
            }

            var parts = DoubleParts.Of(x);
            if (TryQuotientFast(parts, fractionDigits, out var q))
            {
                Span<char> buffer = stackalloc char[MaxFastChars];
                var written = WriteFixed(parts.Negative, q, fractionDigits, buffer);
                return buffer.Slice(0, written).ToString();
            }

            return FormatFixedBig(parts.Negative, QuotientBig(parts, fractionDigits), fractionDigits);
        }

        // Igual que Fixed pero escribe en destination. Por el camino rápido (todo valor de la app: n ≤ 3 y
        // magnitudes de métricas) no reserva memoria. Devuelve false si el texto no entra.
        public static bool TryFormatFixed(double x, int fractionDigits, Span<char> destination, out int charsWritten)
        {
            CheckDigits(fractionDigits);
            if (double.IsNaN(x))
            {
                return TryCopy("NaN", destination, out charsWritten);
            }

            if (x >= 1e21 || x <= -1e21)
            {
                return TryCopy(DartToString(x), destination, out charsWritten);
            }

            var parts = DoubleParts.Of(x);
            if (TryQuotientFast(parts, fractionDigits, out var q))
            {
                var written = WriteFixed(parts.Negative, q, fractionDigits, destination);
                if (written < 0)
                {
                    charsWritten = 0;
                    return false;
                }

                charsWritten = written;
                return true;
            }

            return TryCopy(FormatFixedBig(parts.Negative, QuotientBig(parts, fractionDigits), fractionDigits), destination, out charsWritten);
        }

        // = '$x' / x.toString() de un double en la VM de Dart (Double_toString: double-conversion ToShortest con
        // decimal_in_shortest_low = -6 y high = 21, exponente con signo '+', ".0" en los enteros).
        // Dígitos más cortos EXACTOS, sin parsear (el parser de Mono no es confiable, R-T5), con el resultado de
        // BignumDtoa en modo SHORTEST (QA-04): para cada cantidad de cifras p, piso y techo; los que entran en
        // el intervalo de redondeo; el más cercano; en empate exacto, el de último dígito par.
        public static string DartToString(double x)
        {
            if (double.IsNaN(x))
            {
                return "NaN";
            }

            if (double.IsPositiveInfinity(x))
            {
                return "Infinity";
            }

            if (double.IsNegativeInfinity(x))
            {
                return "-Infinity";
            }

            var parts = DoubleParts.Of(x);
            if (parts.Mantissa == 0)
            {
                return parts.Negative ? "-0.0" : "0.0";
            }

            ShortestDigits(parts, Math.Abs(x), out var digits, out var k);
            return FormatShortest(parts.Negative, digits, k);
        }

        // = int.toString(): cultura invariante, sin separador de miles, '-' ASCII (en sv-SE .NET usaría U+2212).
        public static string Integer(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        // q = ⌊m · 10^n · 2^e + ½⌋ con aritmética de ulong (02-tech-design §3.1, pasos 4-5). false si no entra.
        internal static bool TryQuotientFast(double x, int fractionDigits, out ulong q)
        {
            return TryQuotientFast(DoubleParts.Of(x), fractionDigits, out q);
        }

        // El mismo q con BigInteger (cualquier double finito y cualquier n de [0; 20]).
        internal static BigInteger QuotientBig(double x, int fractionDigits)
        {
            return QuotientBig(DoubleParts.Of(x), fractionDigits);
        }

        private static bool TryQuotientFast(DoubleParts parts, int n, out ulong q)
        {
            q = 0;
            if (n >= Pow10.Length)
            {
                return false;
            }

            var m = parts.Mantissa;
            if (m == 0)
            {
                return true;
            }

            var pow = Pow10[n];
            if (m > ulong.MaxValue / pow)
            {
                return false;
            }

            var scaled = m * pow;
            var e = parts.Exponent;
            if (e >= 0)
            {
                if (e > 63 - BitLength(scaled))
                {
                    return false;
                }

                q = scaled << e;
                return true;
            }

            var s = -e;
            if (s > 64)
            {
                return true; // scaled < 2^64 < 2^(s-1): la mitad no alcanza, q = 0
            }

            if (s == 64)
            {
                q = scaled >= (1UL << 63) ? 1UL : 0UL;
                return true;
            }

            q = scaled >> s;
            var remainder = scaled & ((1UL << s) - 1);
            if (remainder >= (1UL << (s - 1)))
            {
                q++; // 2r ≥ 2^s: el empate exacto sube (se aleja del cero porque se trabaja con |x|)
            }

            return true;
        }

        private static BigInteger QuotientBig(DoubleParts parts, int n)
        {
            if (parts.Mantissa == 0)
            {
                return BigInteger.Zero;
            }

            var scaled = new BigInteger(parts.Mantissa) * BigInteger.Pow(10, n);
            var e = parts.Exponent;
            if (e >= 0)
            {
                return scaled << e;
            }

            var s = -e;
            var q = scaled >> s;
            var remainder = scaled - (q << s);
            if (remainder * 2 >= BigInteger.One << s)
            {
                q += 1;
            }

            return q;
        }

        // Escribe [-]dígitos con el punto antes de los últimos n; completa con ceros hasta n + 1 cifras.
        // Devuelve la cantidad de caracteres, o -1 si no entra.
        private static int WriteFixed(bool negative, ulong q, int n, Span<char> destination)
        {
            var digitCount = 1;
            for (var v = q / 10; v != 0; v /= 10)
            {
                digitCount++;
            }

            var total = Math.Max(digitCount, n + 1);
            var length = (negative ? 1 : 0) + total + (n > 0 ? 1 : 0);
            if (length > destination.Length)
            {
                return -1;
            }

            var pos = length;
            var rest = q;
            for (var i = 0; i < total; i++)
            {
                if (n > 0 && i == n)
                {
                    destination[--pos] = '.';
                }

                destination[--pos] = (char)('0' + (int)(rest % 10));
                rest /= 10;
            }

            if (negative)
            {
                destination[--pos] = '-';
            }

            return length;
        }

        private static string FormatFixedBig(bool negative, BigInteger q, int n)
        {
            var digits = q.ToString(CultureInfo.InvariantCulture);
            if (digits.Length < n + 1)
            {
                digits = new string('0', n + 1 - digits.Length) + digits;
            }

            var sb = new StringBuilder(digits.Length + 2);
            if (negative)
            {
                sb.Append('-');
            }

            if (n > 0)
            {
                sb.Append(digits, 0, digits.Length - n);
                sb.Append('.');
                sb.Append(digits, digits.Length - n, n);
            }
            else
            {
                sb.Append(digits);
            }

            return sb.ToString();
        }

        // Dígitos más cortos (sin ceros finales) y k, la posición del punto: 10^(k-1) ≤ |x| < 10^k.
        private static void ShortestDigits(DoubleParts parts, double magnitude, out string digits, out int k)
        {
            var m = parts.Mantissa;
            var e = parts.Exponent;

            // Potencia de 2 normal (no la menor): el vecino de abajo está a la mitad de distancia (intervalo
            // asimétrico), como Double::LowerBoundaryIsCloser de double-conversion.
            var lowerCloser = parts.Fraction == 0 && parts.BiasedExponent > 1;

            // Los extremos del intervalo entran si m es par (ROUND_HALF_EVEN del parser IEEE).
            var inclusive = (m & 1UL) == 0;

            // Todo escalado por 2^(e-2) para trabajar con enteros: X = 4m, L = 4m - 1 ó 4m - 2, H = 4m + 2.
            var value = new BigInteger(m) * 4;
            var low = value - (lowerCloser ? 1 : 2);
            var high = value + 2;
            var denominator = BigInteger.One;
            var shift = e - 2;
            if (shift >= 0)
            {
                value <<= shift;
                low <<= shift;
                high <<= shift;
            }
            else
            {
                denominator <<= -shift;
            }

            // k estimado con Log10 y corregido con comparaciones exactas.
            k = (int)Math.Floor(Math.Log10(magnitude)) + 1;
            while (ComparePow10(value, denominator, k - 1) < 0)
            {
                k--;
            }

            while (ComparePow10(value, denominator, k) >= 0)
            {
                k++;
            }

            for (var p = 1; p <= 17; p++)
            {
                var t = p - k;
                var x = value;
                var lo = low;
                var hi = high;
                var d = denominator;
                if (t >= 0)
                {
                    var scale = BigInteger.Pow(10, t);
                    x *= scale;
                    lo *= scale;
                    hi *= scale;
                }
                else
                {
                    d *= BigInteger.Pow(10, -t);
                }

                var c = BigInteger.Divide(x, d);
                var floor = c * d;
                var ceiling = floor + d;
                var floorOk = inclusive ? floor >= lo : floor > lo;
                var ceilingOk = inclusive ? ceiling <= hi : ceiling < hi;
                if (!floorOk && !ceilingOk)
                {
                    continue;
                }

                BigInteger chosen;
                if (floorOk && ceilingOk)
                {
                    var below = x - floor;
                    var above = ceiling - x;
                    if (below < above)
                    {
                        chosen = c;
                    }
                    else if (above < below)
                    {
                        chosen = c + 1;
                    }
                    else
                    {
                        chosen = c.IsEven ? c : c + 1; // empate exacto: al par (BignumDtoa)
                    }
                }
                else
                {
                    chosen = floorOk ? c : c + 1;
                }

                var text = chosen.ToString(CultureInfo.InvariantCulture);
                if (text.Length > p)
                {
                    k++; // acarreo: 9.99… sube a 10^p
                }

                text = text.TrimEnd('0');
                digits = text.Length == 0 ? "0" : text;
                return;
            }

            throw new InvalidOperationException("Fmt.DartToString: 17 cifras siempre alcanzan.");
        }

        // Compara value / denominator con 10^j.
        private static int ComparePow10(BigInteger value, BigInteger denominator, int j)
        {
            if (j >= 0)
            {
                return value.CompareTo(denominator * BigInteger.Pow(10, j));
            }

            return (value * BigInteger.Pow(10, -j)).CompareTo(denominator);
        }

        // Notación fija si -6 < k ≤ 21; si no, d[.ddd]e±(k-1).
        private static string FormatShortest(bool negative, string digits, int k)
        {
            var sb = new StringBuilder(digits.Length + 8);
            if (negative)
            {
                sb.Append('-');
            }

            var n = digits.Length;
            if (k > -6 && k <= 21)
            {
                if (k <= 0)
                {
                    sb.Append("0.");
                    sb.Append('0', -k);
                    sb.Append(digits);
                }
                else if (k >= n)
                {
                    sb.Append(digits);
                    sb.Append('0', k - n);
                    sb.Append(".0");
                }
                else
                {
                    sb.Append(digits, 0, k);
                    sb.Append('.');
                    sb.Append(digits, k, n - k);
                }
            }
            else
            {
                sb.Append(digits[0]);
                if (n > 1)
                {
                    sb.Append('.');
                    sb.Append(digits, 1, n - 1);
                }

                var exponent = k - 1;
                sb.Append('e');
                sb.Append(exponent >= 0 ? '+' : '-');
                sb.Append(Math.Abs(exponent).ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        private static int BitLength(ulong value)
        {
            var length = 0;
            while (value != 0)
            {
                length++;
                value >>= 1;
            }

            return length;
        }

        private static bool TryCopy(string text, Span<char> destination, out int charsWritten)
        {
            if (text.Length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            text.AsSpan().CopyTo(destination);
            charsWritten = text.Length;
            return true;
        }

        private static void CheckDigits(int fractionDigits)
        {
            if (fractionDigits < 0 || fractionDigits > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(fractionDigits), fractionDigits,
                    "fractionDigits tiene que estar en [0; 20] (como el RangeError de Dart).");
            }
        }
    }
}
