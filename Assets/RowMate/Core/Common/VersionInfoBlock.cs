using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RowMate.Core.Common
{
    // El recurso VS_VERSIONINFO del RowMate.exe (It. 14, AC-APP-16-06; DT14-31, QA-G14-01): lo arma este código puro y lo escribe
    // el post-proceso de Windows (WP-B1, UpdateResourceW). VS_FIXEDFILEINFO con la versión x.y.z.0, StringFileInfo 040904B0 con
    // CompanyName, FileDescription, FileVersion, InternalName, LegalCopyright, OriginalFilename, ProductName y ProductVersion, y
    // VarFileInfo/Translation 0x0409 / 1200. Claves y valores en UTF-16 LE, cada bloque alineado a 4 bytes (formato de
    // https://learn.microsoft.com/windows/win32/menurc/vs-versioninfo). Los valores salen de PlayerSettings (no a mano).
    public static class VersionInfoBlock
    {
        public const ushort Language = 0x0409;
        public const ushort CodePage = 1200;
        public const string StringTableKey = "040904B0";

        private const uint FixedSignature = 0xFEEF04BD;
        private const uint FixedStrucVersion = 0x00010000;
        private const uint VosNtWindows32 = 0x00040004;
        private const uint VftApp = 1;

        // Las cadenas, en el orden en que se escriben.
        public static IReadOnlyList<KeyValuePair<string, string>> Strings(string productName, string companyName, string bundleVersion)
        {
            var version = Normalize(bundleVersion);
            return new[]
            {
                new KeyValuePair<string, string>("CompanyName", companyName),
                new KeyValuePair<string, string>("FileDescription", productName),
                new KeyValuePair<string, string>("FileVersion", version),
                new KeyValuePair<string, string>("InternalName", productName),
                new KeyValuePair<string, string>("LegalCopyright", "© " + companyName),
                new KeyValuePair<string, string>("OriginalFilename", productName + ".exe"),
                new KeyValuePair<string, string>("ProductName", productName),
                new KeyValuePair<string, string>("ProductVersion", version),
            };
        }

        public static byte[] Build(string productName, string companyName, string bundleVersion)
        {
            if (string.IsNullOrEmpty(productName) || string.IsNullOrEmpty(companyName))
            {
                throw new ArgumentException("VersionInfoBlock: falta el producto o la empresa.");
            }

            var parts = Parse(bundleVersion);
            var ms = (uint)((parts[0] << 16) | parts[1]);
            var ls = (uint)((parts[2] << 16) | parts[3]);
            var fixedInfo = new List<byte>(52);
            foreach (var value in new[] { FixedSignature, FixedStrucVersion, ms, ls, ms, ls, 0x3Fu, 0u, VosNtWindows32, VftApp, 0u, 0u, 0u })
            {
                AddUInt32(fixedInfo, value);
            }

            var strings = new List<Node>();
            foreach (var pair in Strings(productName, companyName, bundleVersion))
            {
                strings.Add(new Node(pair.Key, Utf16(pair.Value + "\0"), (ushort)(pair.Value.Length + 1), 1));
            }

            var table = new Node(StringTableKey, new byte[0], 0, 1, strings.ToArray());
            var stringFileInfo = new Node("StringFileInfo", new byte[0], 0, 1, table);
            var translation = new List<byte>(4);
            AddUInt16(translation, Language);
            AddUInt16(translation, CodePage);
            var var = new Node("Translation", translation.ToArray(), 4, 0);
            var varFileInfo = new Node("VarFileInfo", new byte[0], 0, 1, var);
            var root = new Node("VS_VERSION_INFO", fixedInfo.ToArray(), 52, 0, stringFileInfo, varFileInfo);
            var output = new List<byte>(1024);
            root.Write(output);
            return output.ToArray();
        }

        // Lee de vuelta un bloque (para las pruebas y la verificación): las cadenas de la tabla y la versión fija (x.y.z.w).
        public static IDictionary<string, string> ReadStrings(byte[] block, out string fixedFileVersion)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var rootValue = ValueOffset(block, 0, out var rootKey);
            if (rootKey != "VS_VERSION_INFO" || ReadUInt32(block, rootValue) != FixedSignature)
            {
                throw new FormatException("VersionInfoBlock: no es un VS_VERSIONINFO.");
            }

            var ms = ReadUInt32(block, rootValue + 8);
            var ls = ReadUInt32(block, rootValue + 12);
            fixedFileVersion = (ms >> 16) + "." + (ms & 0xFFFF) + "." + (ls >> 16) + "." + (ls & 0xFFFF);
            var rootEnd = ReadUInt16(block, 0);
            var child = Align(rootValue + ReadUInt16(block, 2));
            while (child < rootEnd)
            {
                var length = ReadUInt16(block, child);
                ValueOffset(block, child, out var key);
                if (key == "StringFileInfo")
                {
                    var tableAt = Align(ValueOffset(block, child, out _));
                    var tableEnd = tableAt + ReadUInt16(block, tableAt);
                    var entry = Align(ValueOffset(block, tableAt, out _));
                    while (entry < tableEnd)
                    {
                        var entryLength = ReadUInt16(block, entry);
                        var valueAt = ValueOffset(block, entry, out var name);
                        var chars = ReadUInt16(block, entry + 2);
                        var value = Encoding.Unicode.GetString(block, valueAt, Math.Max(0, chars - 1) * 2);
                        result[name] = value;
                        entry = Align(entry + entryLength);
                    }
                }

                child = Align(child + length);
            }

            return result;
        }

        // "1.0.0" → "1.0.0" (sin ceros de más); acepta de 1 a 4 números.
        private static string Normalize(string bundleVersion)
        {
            var parts = Parse(bundleVersion);
            var count = bundleVersion.Split('.').Length;
            var builder = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    builder.Append('.');
                }

                builder.Append(parts[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static int[] Parse(string bundleVersion)
        {
            if (string.IsNullOrEmpty(bundleVersion))
            {
                throw new ArgumentException("VersionInfoBlock: falta la versión.", nameof(bundleVersion));
            }

            var split = bundleVersion.Split('.');
            if (split.Length > 4)
            {
                throw new FormatException("VersionInfoBlock: versión con más de 4 números: " + bundleVersion);
            }

            var parts = new int[4];
            for (var i = 0; i < split.Length; i++)
            {
                if (!int.TryParse(split[i], NumberStyles.None, CultureInfo.InvariantCulture, out parts[i]) || parts[i] > 0xFFFF)
                {
                    throw new FormatException("VersionInfoBlock: versión inválida: " + bundleVersion);
                }
            }

            return parts;
        }

        private static int ValueOffset(byte[] block, int at, out string key)
        {
            var i = at + 6;
            var builder = new StringBuilder();
            while (true)
            {
                var c = (char)ReadUInt16(block, i);
                i += 2;
                if (c == '\0')
                {
                    break;
                }

                builder.Append(c);
            }

            key = builder.ToString();
            return Align(i);
        }

        private static int Align(int offset)
        {
            return (offset + 3) & ~3;
        }

        private static byte[] Utf16(string text)
        {
            return Encoding.Unicode.GetBytes(text);
        }

        private static void AddUInt16(List<byte> output, ushort value)
        {
            output.Add((byte)value);
            output.Add((byte)(value >> 8));
        }

        private static void AddUInt32(List<byte> output, uint value)
        {
            output.Add((byte)value);
            output.Add((byte)(value >> 8));
            output.Add((byte)(value >> 16));
            output.Add((byte)(value >> 24));
        }

        private static ushort ReadUInt16(byte[] block, int at)
        {
            return (ushort)(block[at] | (block[at + 1] << 8));
        }

        private static uint ReadUInt32(byte[] block, int at)
        {
            return (uint)(block[at] | (block[at + 1] << 8) | (block[at + 2] << 16) | (block[at + 3] << 24));
        }

        // Un bloque: wLength, wValueLength, wType, szKey (UTF-16 con su 0), relleno a 4, el valor, relleno a 4 y los hijos (cada
        // uno alineado a 4). wLength no cuenta el relleno que va después del bloque.
        private sealed class Node
        {
            private readonly string _key;
            private readonly byte[] _value;
            private readonly ushort _valueLength;
            private readonly ushort _type;
            private readonly Node[] _children;

            public Node(string key, byte[] value, ushort valueLength, ushort type, params Node[] children)
            {
                _key = key;
                _value = value;
                _valueLength = valueLength;
                _type = type;
                _children = children;
            }

            public void Write(List<byte> output)
            {
                var start = output.Count;
                AddUInt16(output, 0);
                AddUInt16(output, _valueLength);
                AddUInt16(output, _type);
                output.AddRange(Utf16(_key + "\0"));
                Pad(output);
                output.AddRange(_value);
                foreach (var child in _children)
                {
                    Pad(output);
                    child.Write(output);
                }

                var length = output.Count - start;
                output[start] = (byte)length;
                output[start + 1] = (byte)(length >> 8);
            }

            private static void Pad(List<byte> output)
            {
                while ((output.Count & 3) != 0)
                {
                    output.Add(0);
                }
            }
        }
    }
}
