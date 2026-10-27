using System;
using System.Text;

namespace RowMate.Core.Ble
{
    // UUID de Bluetooth LE con la semántica de Guid de flutter_blue_plus_platform_interface 9.0.3
    // (lib/src/guid.dart:24-90): se sacan los '-', se decodifican pares hex (mayúsculas o minúsculas);
    // 2 bytes = 16 bits, 4 = 32 bits, 16 = 128 bits; otra longitud o un par no hex → FormatException;
    // "" → Empty (List.filled(16, 0)). La igualdad compara la forma de 128 bits (str128), sin distinguir
    // mayúsculas (AC-BLE-03-04). Igualdad y hash sin reservar memoria.
    public readonly struct BleUuid : IEquatable<BleUuid>
    {
        public const string BaseSuffix = "-0000-1000-8000-00805f9b34fb";

        // Base Bluetooth 0000xxxx-0000-1000-8000-00805f9b34fb: mitad alta sin el valor y mitad baja.
        private const ulong BaseHigh = 0x0000_0000_0000_1000UL;
        private const ulong BaseLow = 0x8000_0080_5F9B_34FBUL;

        public static readonly BleUuid Empty = new BleUuid(0, 0);

        public BleUuid(ulong high, ulong low)
        {
            High = high;
            Low = low;
        }

        public ulong High { get; }

        public ulong Low { get; }

        // 0000xxxx-0000-1000-8000-00805f9b34fb
        public static BleUuid FromShort(ushort value)
        {
            return new BleUuid(((ulong)value << 32) | BaseHigh, BaseLow);
        }

        // xxxxxxxx-0000-1000-8000-00805f9b34fb
        public static BleUuid From32(uint value)
        {
            return new BleUuid(((ulong)value << 32) | BaseHigh, BaseLow);
        }

        public static BleUuid Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!TryParseCore(text, out var value, out var error))
            {
                throw new FormatException("BleUuid: " + error + ": \"" + text + "\".");
            }

            return value;
        }

        public static bool TryParse(string? text, out BleUuid value)
        {
            if (text == null)
            {
                value = Empty;
                return false;
            }

            return TryParseCore(text, out value, out _);
        }

        public static bool operator ==(BleUuid a, BleUuid b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(BleUuid a, BleUuid b)
        {
            return !a.Equals(b);
        }

        public bool Equals(BleUuid other)
        {
            return High == other.High && Low == other.Low;
        }

        public override bool Equals(object? obj)
        {
            return obj is BleUuid other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (High ^ (Low * 31)).GetHashCode();
        }

        // Forma de 128 bits en minúsculas (= Guid.str128).
        public override string ToString()
        {
            var sb = new StringBuilder(36);
            AppendHex(sb, High >> 32, 8);
            sb.Append('-');
            AppendHex(sb, (High >> 16) & 0xFFFF, 4);
            sb.Append('-');
            AppendHex(sb, High & 0xFFFF, 4);
            sb.Append('-');
            AppendHex(sb, Low >> 48, 4);
            sb.Append('-');
            AppendHex(sb, Low & 0xFFFF_FFFF_FFFFUL, 12);
            return sb.ToString();
        }

        // Forma más corta (= Guid.str): 16 bits si empieza con 0000 y tiene la base, 32 si tiene la base, si no 128.
        public string ToShortString()
        {
            var hasBase = (High & 0xFFFF_FFFFUL) == BaseHigh && Low == BaseLow;
            if (hasBase && (High >> 48) == 0)
            {
                var sb16 = new StringBuilder(4);
                AppendHex(sb16, High >> 32, 4);
                return sb16.ToString();
            }

            if (hasBase)
            {
                var sb32 = new StringBuilder(8);
                AppendHex(sb32, High >> 32, 8);
                return sb32.ToString();
            }

            return ToString();
        }

        private static bool TryParseCore(string text, out BleUuid value, out string error)
        {
            value = Empty;
            if (text.Length == 0)
            {
                error = "";
                return true;
            }

            // Hasta 32 cifras hex (16 bytes); los '-' se ignoran en cualquier posición (replaceAll('-', '')).
            ulong high = 0;
            ulong low = 0;
            var digits = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == '-')
                {
                    continue;
                }

                var nibble = HexValue(c);
                if (nibble < 0)
                {
                    error = "no es hexadecimal";
                    return false;
                }

                if (digits >= 32)
                {
                    error = "tiene que ser de 16, 32 o 128 bits";
                    return false;
                }

                high = (high << 4) | (low >> 60);
                low = (low << 4) | (uint)nibble;
                digits++;
            }

            if (digits % 2 != 0)
            {
                error = "cantidad impar de cifras hexadecimales";
                return false;
            }

            switch (digits)
            {
                case 4:
                    value = FromShort((ushort)low);
                    break;
                case 8:
                    value = From32((uint)low);
                    break;
                case 32:
                    value = new BleUuid(high, low);
                    break;
                default:
                    error = "tiene que ser de 16, 32 o 128 bits";
                    return false;
            }

            error = "";
            return true;
        }

        private static int HexValue(char c)
        {
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

            return -1;
        }

        private static void AppendHex(StringBuilder sb, ulong value, int width)
        {
            for (var shift = (width - 1) * 4; shift >= 0; shift -= 4)
            {
                var nibble = (int)((value >> shift) & 0xF);
                sb.Append((char)(nibble < 10 ? '0' + nibble : 'a' + nibble - 10));
            }
        }
    }
}
