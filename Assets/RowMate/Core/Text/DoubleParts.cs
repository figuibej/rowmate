using System;

namespace RowMate.Core.Text
{
    // Descomposición exacta de un double IEEE-754: |x| = Mantissa · 2^Exponent, sin error (ADR 012 §1.3).
    // Negative es el bit de signo (así -0.0 es negativo, como en Dart).
    internal readonly struct DoubleParts
    {
        private DoubleParts(bool negative, ulong mantissa, int exponent, int biasedExponent, ulong fraction)
        {
            Negative = negative;
            Mantissa = mantissa;
            Exponent = exponent;
            BiasedExponent = biasedExponent;
            Fraction = fraction;
        }

        public bool Negative { get; }

        // m: con el bit implícito si el número es normal.
        public ulong Mantissa { get; }

        // e: exponente binario de m.
        public int Exponent { get; }

        // be: los 11 bits del exponente tal cual (0 = subnormal o cero, 2047 = infinito o NaN).
        public int BiasedExponent { get; }

        // f: los 52 bits de la fracción tal cual.
        public ulong Fraction { get; }

        public static DoubleParts Of(double x)
        {
            var bits = BitConverter.DoubleToInt64Bits(x);
            var negative = bits < 0;
            var be = (int)((bits >> 52) & 0x7FF);
            var f = (ulong)bits & 0xF_FFFF_FFFF_FFFFUL;
            if (be == 0)
            {
                return new DoubleParts(negative, f, -1074, be, f);
            }

            return new DoubleParts(negative, f | (1UL << 52), be - 1075, be, f);
        }
    }
}
