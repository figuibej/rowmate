using System;
using System.Globalization;
using RowMate.Core.Common.Json;

namespace RowMate.Tests.Support
{
    public static class GoldenJsonExtensions
    {
        // Un double del oráculo, SIEMPRE por sus bits: {"v": …, "bits": "<16 hex>"} o directamente "<16 hex>".
        // El campo "v" es solo para leer el archivo; ningún parser de ningún runtime toca el valor.
        public static double AsF64(this JsonNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            string hex;
            if (node.Kind == JsonKind.Object)
            {
                hex = node["bits"].AsString();
            }
            else if (node.Kind == JsonKind.String)
            {
                hex = node.AsString();
            }
            else
            {
                throw new FormatException("Golden: se esperaba un double por bits y llegó " + node + ".");
            }

            if (hex.Length != 16 || !ulong.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var bits))
            {
                throw new FormatException("Golden: \"" + hex + "\" no son 16 cifras hex de un double.");
            }

            return BitConverter.Int64BitsToDouble(unchecked((long)bits));
        }

        // Bytes como "2C 00 30" o como [44, 0, 48].
        public static byte[] AsBytes(this JsonNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.Kind == JsonKind.String)
            {
                return HexBytes.Parse(node.AsString());
            }

            if (node.Kind == JsonKind.Array)
            {
                var items = node.Items;
                var bytes = new byte[items.Count];
                for (var i = 0; i < items.Count; i++)
                {
                    var value = items[i].AsLong();
                    if (value < 0 || value > 255)
                    {
                        throw new FormatException("Golden: " + value + " no es un byte.");
                    }

                    bytes[i] = (byte)value;
                }

                return bytes;
            }

            throw new FormatException("Golden: se esperaban bytes y llegó " + node + ".");
        }

        // Bits de un double como los escribe el oráculo (16 hex en minúsculas, big-endian).
        public static string ToF64Bits(double value)
        {
            return unchecked((ulong)BitConverter.DoubleToInt64Bits(value)).ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
