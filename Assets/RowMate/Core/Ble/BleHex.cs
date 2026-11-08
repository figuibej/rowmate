using System;

namespace RowMate.Core.Ble
{
    // Bytes en hexadecimal como el legacy: minúscula, 2 cifras, separados por un espacio (ble_service.dart:225, :293;
    // device_provider.dart:98-100). Vacío → "(sin datos)" (fijo en español, como el legacy: Q-04).
    public static class BleHex
    {
        public const string NoData = "(sin datos)";

        private const string Digits = "0123456789abcdef";

        // "2c 00 30"; vacío → "".
        public static string Format(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length == 0)
            {
                return "";
            }

            var chars = new char[bytes.Length * 3 - 1];
            for (var i = 0; i < bytes.Length; i++)
            {
                var at = i * 3;
                chars[at] = Digits[bytes[i] >> 4];
                chars[at + 1] = Digits[bytes[i] & 0xF];
                if (i < bytes.Length - 1)
                {
                    chars[at + 2] = ' ';
                }
            }

            return new string(chars);
        }

        // = lastRawHex (dp:98-100): "(sin datos)" sin paquete.
        public static string ForDiagnostics(byte[]? bytes)
        {
            return bytes == null || bytes.Length == 0 ? NoData : Format(bytes);
        }
    }
}
