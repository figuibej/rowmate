using System;
using System.Collections.Generic;
using System.Text;

namespace RowMate.Tests.Support
{
    // Paquetes como en la spec: un byte por grupo en hexadecimal ("2C 00 30").
    public static class HexBytes
    {
        // "2C 00 30" → { 0x2C, 0x00, 0x30 }. Acepta mayúsculas o minúsculas y espacios de más; "" → vacío.
        public static byte[] Parse(string spaced)
        {
            if (spaced == null)
            {
                throw new ArgumentNullException(nameof(spaced));
            }

            var result = new List<byte>();
            var parts = spaced.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part.Length != 2)
                {
                    throw new FormatException("HexBytes: \"" + part + "\" no es un byte de 2 cifras hex.");
                }

                result.Add((byte)((Nibble(part[0]) << 4) | Nibble(part[1])));
            }

            return result.ToArray();
        }

        // { 0x2C, 0x00, 0x30 } → "2C 00 30" (mayúsculas, como la spec).
        public static string Format(ReadOnlySpan<byte> bytes)
        {
            const string Digits = "0123456789ABCDEF";
            var sb = new StringBuilder(bytes.Length * 3);
            for (var i = 0; i < bytes.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(Digits[bytes[i] >> 4]).Append(Digits[bytes[i] & 0xF]);
            }

            return sb.ToString();
        }

        private static int Nibble(char c)
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

            throw new FormatException("HexBytes: '" + c + "' no es hexadecimal.");
        }
    }
}
