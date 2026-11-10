using System;

namespace RowMate.Core.Design
{
    // Deflate propio (RFC 1951) para los PNG del ícono (It. 14, DT14-02): un solo bloque de Huffman fijo (BTYPE 01) con LZ77
    // voraz (ventana de 32 KiB, hash de 3 bytes, cadena de 64 como máximo, coincidencias de 3 a 258). No se usa DeflateStream:
    // su zlib cambia entre .NET 8, Mono y la versión del runtime. Este es determinista por construcción (solo enteros), así
    // que los bytes son los mismos en la PC, en el CI y en Unity. ZLib envuelve el flujo con la cabecera 78 01 y el Adler-32.
    public static class DeflateEncoder
    {
        private const int WindowSize = 32768;
        private const int MinMatch = 3;
        private const int MaxMatch = 258;
        private const int MaxChain = 64;
        private const int HashBits = 15;
        private const int HashSize = 1 << HashBits;

        // Base y bits extra de los códigos de largo 257..285 (RFC 1951 §3.2.5).
        private static readonly int[] LengthBase =
        {
            3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258,
        };

        private static readonly int[] LengthExtra =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0,
        };

        // Base y bits extra de los códigos de distancia 0..29.
        private static readonly int[] DistanceBase =
        {
            1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145,
            8193, 12289, 16385, 24577,
        };

        private static readonly int[] DistanceExtra =
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13,
        };

        // Flujo zlib (RFC 1950): 78 01, el deflate y el Adler-32 de los datos sin comprimir (big-endian).
        public static byte[] ZLib(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var writer = new BitWriter(data.Length / 4 + 64);
            writer.WriteByte(0x78);
            writer.WriteByte(0x01);
            WriteDeflate(writer, data);
            writer.AlignToByte();
            var adler = Adler32.Compute(data);
            writer.WriteByte((byte)(adler >> 24));
            writer.WriteByte((byte)(adler >> 16));
            writer.WriteByte((byte)(adler >> 8));
            writer.WriteByte((byte)adler);
            return writer.ToArray();
        }

        // Deflate crudo (sin cabecera ni Adler).
        public static byte[] Deflate(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var writer = new BitWriter(data.Length / 4 + 64);
            WriteDeflate(writer, data);
            writer.AlignToByte();
            return writer.ToArray();
        }

        private static void WriteDeflate(BitWriter writer, byte[] data)
        {
            writer.WriteBits(1, 1); // BFINAL
            writer.WriteBits(1, 2); // BTYPE = 01 (Huffman fijo)

            var head = new int[HashSize];
            for (var i = 0; i < head.Length; i++)
            {
                head[i] = -1;
            }

            var prev = new int[WindowSize];
            var n = data.Length;
            var pos = 0;
            while (pos < n)
            {
                var bestLength = 0;
                var bestDistance = 0;
                if (pos + MinMatch <= n)
                {
                    var candidate = head[Hash(data, pos)];
                    var chain = 0;
                    var limit = Math.Min(MaxMatch, n - pos);
                    while (candidate >= 0 && pos - candidate <= WindowSize && chain < MaxChain)
                    {
                        if (data[candidate + bestLength] == data[pos + bestLength])
                        {
                            var length = 0;
                            while (length < limit && data[candidate + length] == data[pos + length])
                            {
                                length++;
                            }

                            if (length > bestLength)
                            {
                                bestLength = length;
                                bestDistance = pos - candidate;
                                if (length == limit)
                                {
                                    break;
                                }
                            }
                        }

                        candidate = prev[candidate & (WindowSize - 1)];
                        chain++;
                    }
                }

                if (bestLength >= MinMatch)
                {
                    WriteLength(writer, bestLength);
                    WriteDistance(writer, bestDistance);
                    var end = pos + bestLength;
                    while (pos < end)
                    {
                        Insert(data, pos, head, prev);
                        pos++;
                    }
                }
                else
                {
                    WriteLiteral(writer, data[pos]);
                    Insert(data, pos, head, prev);
                    pos++;
                }
            }

            WriteLiteral(writer, 256); // fin de bloque
        }

        private static int Hash(byte[] data, int pos)
        {
            var h = (data[pos] << 10) ^ (data[pos + 1] << 5) ^ data[pos + 2];
            return h & (HashSize - 1);
        }

        private static void Insert(byte[] data, int pos, int[] head, int[] prev)
        {
            if (pos + MinMatch > data.Length)
            {
                return;
            }

            var hash = Hash(data, pos);
            prev[pos & (WindowSize - 1)] = head[hash];
            head[hash] = pos;
        }

        // Códigos fijos (RFC 1951 §3.2.6): 0-143 → 8 bits desde 0x30; 144-255 → 9 bits desde 0x190; 256-279 → 7 bits desde 0;
        // 280-287 → 8 bits desde 0xC0. Los códigos de Huffman van del bit más significativo al menos significativo.
        private static void WriteLiteral(BitWriter writer, int symbol)
        {
            if (symbol <= 143)
            {
                writer.WriteHuffman(0x30 + symbol, 8);
            }
            else if (symbol <= 255)
            {
                writer.WriteHuffman(0x190 + symbol - 144, 9);
            }
            else if (symbol <= 279)
            {
                writer.WriteHuffman(symbol - 256, 7);
            }
            else
            {
                writer.WriteHuffman(0xC0 + symbol - 280, 8);
            }
        }

        private static void WriteLength(BitWriter writer, int length)
        {
            var code = LengthBase.Length - 1;
            while (LengthBase[code] > length)
            {
                code--;
            }

            WriteLiteral(writer, 257 + code);
            if (LengthExtra[code] > 0)
            {
                writer.WriteBits(length - LengthBase[code], LengthExtra[code]);
            }
        }

        private static void WriteDistance(BitWriter writer, int distance)
        {
            var code = DistanceBase.Length - 1;
            while (DistanceBase[code] > distance)
            {
                code--;
            }

            writer.WriteHuffman(code, 5);
            if (DistanceExtra[code] > 0)
            {
                writer.WriteBits(distance - DistanceBase[code], DistanceExtra[code]);
            }
        }

        // Escritor de bits LSB primero (el orden de deflate) sobre un búfer que crece.
        private sealed class BitWriter
        {
            private byte[] _buffer;
            private int _length;
            private uint _bits;
            private int _count;

            public BitWriter(int capacity)
            {
                _buffer = new byte[Math.Max(capacity, 64)];
            }

            public void WriteBits(int value, int count)
            {
                _bits |= (uint)value << _count;
                _count += count;
                while (_count >= 8)
                {
                    Put((byte)_bits);
                    _bits >>= 8;
                    _count -= 8;
                }
            }

            // Un código de Huffman: sus bits invertidos (el primero que se lee es el más significativo).
            public void WriteHuffman(int code, int count)
            {
                var reversed = 0;
                for (var i = 0; i < count; i++)
                {
                    reversed = (reversed << 1) | ((code >> i) & 1);
                }

                WriteBits(reversed, count);
            }

            public void WriteByte(byte value)
            {
                WriteBits(value, 8);
            }

            public void AlignToByte()
            {
                if (_count > 0)
                {
                    Put((byte)_bits);
                    _bits = 0;
                    _count = 0;
                }
            }

            public byte[] ToArray()
            {
                var result = new byte[_length];
                Array.Copy(_buffer, result, _length);
                return result;
            }

            private void Put(byte value)
            {
                if (_length == _buffer.Length)
                {
                    var bigger = new byte[_buffer.Length * 2];
                    Array.Copy(_buffer, bigger, _length);
                    _buffer = bigger;
                }

                _buffer[_length++] = value;
            }
        }
    }
}
