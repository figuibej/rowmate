using System;

namespace RowMate.Core.Design
{
    // PNG determinista para el ícono (It. 14, DT14-01): IHDR, un IDAT y IEND; sin tIME, gAMA, sRGB ni tEXt (los bytes no
    // dependen de la hora ni del equipo). 8 bits por canal, RGBA (tipo 6) o RGB sin alfa (tipo 2, el de iOS). Filtro Paeth
    // (4) fijo en todas las filas. El IDAT lo comprime DeflateEncoder (propio, no DeflateStream).
    public static class PngEncoder
    {
        public static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        // rgba: width × height × 4 bytes, fila por fila desde arriba, alfa recto. withAlpha = false descarta el alfa (tiene que
        // ser 255 en todos los píxeles: si no, ArgumentException).
        public static byte[] Encode(int width, int height, byte[] rgba, bool withAlpha)
        {
            if (rgba == null)
            {
                throw new ArgumentNullException(nameof(rgba));
            }

            if (width <= 0 || height <= 0 || rgba.Length != width * height * 4)
            {
                throw new ArgumentException("PngEncoder: el tamaño no coincide con los píxeles.", nameof(rgba));
            }

            var channels = withAlpha ? 4 : 3;
            var stride = width * channels;
            var raw = new byte[height * (stride + 1)];
            var row = new byte[stride];
            var above = new byte[stride];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var src = (y * width + x) * 4;
                    var dst = x * channels;
                    row[dst] = rgba[src];
                    row[dst + 1] = rgba[src + 1];
                    row[dst + 2] = rgba[src + 2];
                    if (withAlpha)
                    {
                        row[dst + 3] = rgba[src + 3];
                    }
                    else if (rgba[src + 3] != 255)
                    {
                        throw new ArgumentException("PngEncoder: sin alfa, todos los píxeles tienen que ser opacos.", nameof(rgba));
                    }
                }

                var start = y * (stride + 1);
                raw[start] = 4; // Paeth
                for (var i = 0; i < stride; i++)
                {
                    var a = i >= channels ? row[i - channels] : 0;
                    var b = above[i];
                    var c = i >= channels ? above[i - channels] : 0;
                    raw[start + 1 + i] = (byte)(row[i] - Paeth(a, b, c));
                }

                var swap = above;
                above = row;
                row = swap;
            }

            var ihdr = new byte[13];
            WriteUInt32(ihdr, 0, (uint)width);
            WriteUInt32(ihdr, 4, (uint)height);
            ihdr[8] = 8;                          // bits por canal
            ihdr[9] = (byte)(withAlpha ? 6 : 2);  // RGBA o RGB
            ihdr[10] = 0;                         // deflate
            ihdr[11] = 0;                         // filtros adaptativos
            ihdr[12] = 0;                         // sin entrelazado
            var idat = DeflateEncoder.ZLib(raw);

            var output = new byte[Signature.Length + 12 * 3 + ihdr.Length + idat.Length];
            Array.Copy(Signature, output, Signature.Length);
            var offset = Signature.Length;
            offset = WriteChunk(output, offset, "IHDR", ihdr);
            offset = WriteChunk(output, offset, "IDAT", idat);
            WriteChunk(output, offset, "IEND", new byte[0]);
            return output;
        }

        // PNG §9.4: el predictor más cercano a a + b − c (empates: a, b, c).
        public static int Paeth(int a, int b, int c)
        {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc)
            {
                return a;
            }

            return pb <= pc ? b : c;
        }

        private static int WriteChunk(byte[] output, int offset, string type, byte[] data)
        {
            WriteUInt32(output, offset, (uint)data.Length);
            for (var i = 0; i < 4; i++)
            {
                output[offset + 4 + i] = (byte)type[i];
            }

            Array.Copy(data, 0, output, offset + 8, data.Length);
            var crc = Crc32.Compute(output, offset + 4, 4 + data.Length);
            WriteUInt32(output, offset + 8 + data.Length, crc);
            return offset + 12 + data.Length;
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
