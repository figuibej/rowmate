using System;

namespace RowMate.Core.Design
{
    // Color opaco sRGB de 8 bits por canal (ADR 016).
    public readonly struct Rgb : IEquatable<Rgb>
    {
        public Rgb(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public byte R { get; }

        public byte G { get; }

        public byte B { get; }

        // Acepta "#RRGGBB" con cifras en mayúsculas o minúsculas; cualquier otra forma → FormatException.
        public static Rgb Parse(string hex)
        {
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            if (hex.Length != 7 || hex[0] != '#')
            {
                throw new FormatException("Rgb: se esperaba #RRGGBB y llegó \"" + hex + "\".");
            }

            return new Rgb(ParseByte(hex, 1), ParseByte(hex, 3), ParseByte(hex, 5));
        }

        public static bool operator ==(Rgb a, Rgb b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Rgb a, Rgb b)
        {
            return !a.Equals(b);
        }

        // "#RRGGBB" en mayúsculas.
        public string ToHex()
        {
            var chars = new char[7];
            chars[0] = '#';
            WriteByte(chars, 1, R);
            WriteByte(chars, 3, G);
            WriteByte(chars, 5, B);
            return new string(chars);
        }

        public bool Equals(Rgb other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object? obj)
        {
            return obj is Rgb other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (R << 16) | (G << 8) | B;
        }

        public override string ToString()
        {
            return ToHex();
        }

        private static byte ParseByte(string hex, int start)
        {
            return (byte)((Nibble(hex, start) << 4) | Nibble(hex, start + 1));
        }

        private static int Nibble(string hex, int index)
        {
            var c = hex[index];
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            throw new FormatException("Rgb: \"" + hex + "\" no es hexadecimal.");
        }

        private static void WriteByte(char[] chars, int start, byte value)
        {
            const string Digits = "0123456789ABCDEF";
            chars[start] = Digits[value >> 4];
            chars[start + 1] = Digits[value & 0xF];
        }
    }
}
