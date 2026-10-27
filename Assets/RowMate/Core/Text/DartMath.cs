using System;
using System.Globalization;

namespace RowMate.Core.Text
{
    // Aritmética con la semántica de Dart (ADR 012 §2-§3). Prohibido Math.Round en Core/App/Simulator.
    public static class DartMath
    {
        private const double TwoPow52 = 4503599627370496.0;
        private const double TwoPow53 = 9007199254740992.0;
        private const double TwoPow63 = 9223372036854775808.0;

        private static readonly double[] Pow10 =
        {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17,
            1e18, 1e19, 1e20
        };

        // = double.round(): mitad alejándose del cero, devuelve un entero (nunca -0).
        // NaN e infinito → ArgumentException (Dart: UnsupportedError). |x| ≥ 2^63 → fuera del rango de la app.
        public static long Round(double x)
        {
            if (double.IsNaN(x) || double.IsInfinity(x))
            {
                throw new ArgumentException("DartMath.Round: NaN o infinito no tienen redondeo entero.", nameof(x));
            }

            if (x >= TwoPow63 || x < -TwoPow63)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, "DartMath.Round: fuera del rango de long.");
            }

            if (x >= TwoPow52 || x <= -TwoPow52)
            {
                return (long)x; // ya es entero
            }

            // Resta exacta para |x| < 2^52: no cae en la trampa de floor(x + 0.5) con 0.49999999999999994.
            var t = Math.Truncate(x);
            var f = x - t;
            if (f >= 0.5)
            {
                t += 1;
            }
            else if (f <= -0.5)
            {
                t -= 1;
            }

            return (long)t;
        }

        // = double.parse(x.toStringAsFixed(n)) (legacy: lib/core/dev/rowing_simulator.dart:60), sin pasar por texto:
        // ±q / 10^n es una división IEEE de dos valores exactos (q < 2^53, n ≤ 20 ≤ 22), así que da el mismo double
        // que un parser correctamente redondeado.
        public static double RoundToFixed(double x, int fractionDigits)
        {
            if (fractionDigits < 0 || fractionDigits > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(fractionDigits), fractionDigits,
                    "fractionDigits tiene que estar en [0; 20].");
            }

            // NaN, infinitos y |x| ≥ 1e21: toStringAsFixed devuelve toString(), que vuelve al mismo double.
            if (double.IsNaN(x) || x >= 1e21 || x <= -1e21)
            {
                return x;
            }

            var negative = DoubleParts.Of(x).Negative;
            if (Fmt.TryQuotientFast(x, fractionDigits, out var q) && q < TwoPow53)
            {
                var magnitude = q / Pow10[fractionDigits];
                return negative ? -magnitude : magnitude;
            }

            var big = Fmt.QuotientBig(x, fractionDigits);
            if (big < new System.Numerics.BigInteger(TwoPow53))
            {
                var magnitude = (double)big / Pow10[fractionDigits];
                return negative ? -magnitude : magnitude;
            }

            return double.Parse(Fmt.Fixed(x, fractionDigits), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        // = a % b de Dart con int: el resultado nunca es negativo.
        public static long Mod(long a, long b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException("DartMath.Mod: división entera por cero.");
            }

            if (b == -1)
            {
                return 0; // evita el OverflowException de long.MinValue % -1
            }

            var r = a % b;
            if (r < 0)
            {
                r += b < 0 ? -b : b;
            }

            return r;
        }

        // = a % b de Dart con double (DartModulo de la VM): resto con el signo corregido y +0.0 en vez de -0.0.
        public static double Mod(double a, double b)
        {
            var r = a % b;
            if (r == 0.0)
            {
                return 0.0;
            }

            if (r < 0.0)
            {
                r += b < 0.0 ? -b : b;
            }

            return r;
        }

        // = a ~/ b de Dart con int: división truncada hacia cero.
        public static long TruncDiv(long a, long b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException("DartMath.TruncDiv: división entera por cero.");
            }

            if (b == -1)
            {
                return unchecked(-a); // Dart: el desborde de long.MinValue ~/ -1 da la vuelta
            }

            return a / b;
        }

        // = a ~/ b de Dart con double: (a / b).truncate(). NaN o infinito → ArgumentException (UnsupportedError).
        public static long TruncDiv(double a, double b)
        {
            var q = a / b;
            if (double.IsNaN(q) || double.IsInfinity(q))
            {
                throw new ArgumentException("DartMath.TruncDiv: el cociente es NaN o infinito.");
            }

            var t = Math.Truncate(q);
            if (t >= TwoPow63 || t < -TwoPow63)
            {
                throw new ArgumentOutOfRangeException(nameof(a), a, "DartMath.TruncDiv: fuera del rango de long.");
            }

            return (long)t;
        }
    }
}
