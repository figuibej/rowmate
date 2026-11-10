using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // DF · Deflate, Adler-32 y CRC-32 propios del PNG del ícono (It. 14, DT14-02): vectores conocidos, determinismo y, en .NET 8,
    // ida y vuelta con ZLibStream (en Unity lo cubre Texture2D.LoadImage, AppIconImportTests de WP-B1).
    [TestFixture]
    public sealed class DeflateEncoderTests
    {
        [Test, Category("AC-APP-16-08")]
        public void KnownChecksums()
        {
            Assert.That(Crc32.Compute(Encoding.ASCII.GetBytes("123456789")), Is.EqualTo(0xCBF43926u));
            Assert.That(Adler32.Compute(Encoding.ASCII.GetBytes("Wikipedia")), Is.EqualTo(0x11E60398u));
            Assert.That(Adler32.Compute(new byte[0]), Is.EqualTo(1u));
            Assert.That(Crc32.Compute(Encoding.ASCII.GetBytes("IEND")), Is.EqualTo(0xAE426082u), "el CRC del IEND de todo PNG");
        }

        [Test, Category("AC-APP-16-08")]
        public void EmptyIsOneFixedBlockWithEndOfBlock()
        {
            Assert.That(DeflateEncoder.Deflate(new byte[0]), Is.EqualTo(new byte[] { 0x03, 0x00 }));
            var zlib = DeflateEncoder.ZLib(new byte[0]);
            Assert.That(zlib, Is.EqualTo(new byte[] { 0x78, 0x01, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01 }));
            Assert.That((zlib[0] * 256 + zlib[1]) % 31, Is.EqualTo(0), "FCHECK de zlib");
        }

        [Test, Category("AC-APP-16-08")]
        public void Deterministic()
        {
            var data = Sample(200000, 7);
            Assert.That(DeflateEncoder.ZLib(data), Is.EqualTo(DeflateEncoder.ZLib(data)));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [Category("AC-APP-16-08")]
        public void RoundTrip(int kind)
        {
            byte[] data;
            switch (kind)
            {
                case 1:
                    data = new byte[] { 42 };
                    break;
                case 2:
                    data = new byte[1 << 20]; // un mega de ceros: coincidencias de 258 a distancia 1
                    break;
                case 3:
                    data = Encoding.UTF8.GetBytes(string.Concat(System.Linq.Enumerable.Repeat("RowMate amanecer de regata · ", 3000)));
                    break;
                default:
                    data = Sample(300000, kind); // azar con semilla fija y tramos repetidos a distancias de toda la ventana
                    break;
            }

            var compressed = DeflateEncoder.ZLib(data);
            Assert.That(Inflate(compressed), Is.EqualTo(data));
            if (kind == 2)
            {
                Assert.That(compressed.Length, Is.LessThan(data.Length / 100), "comprime las corridas");
            }
        }

        // ZLibStream existe solo desde .NET 6; en Unity (netstandard2.1) se valida con Texture2D.LoadImage.
        public static byte[] Inflate(byte[] zlib)
        {
#if NET6_0_OR_GREATER
            using (var input = new MemoryStream(zlib))
            using (var stream = new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                stream.CopyTo(output);
                return output.ToArray();
            }
#else
            Assert.Ignore("ZLibStream solo en .NET 6+: en Unity lo cubre AppIconImportTests (Texture2D.LoadImage).");
            return zlib;
#endif
        }

        // Un LCG con semilla fija (System.Random solo vive en Core/Common/SystemRandom.cs, AC-BASE-07).
        private static int Next(ref uint state, int bound)
        {
            state = state * 1664525u + 1013904223u;
            return (int)((state >> 8) % (uint)bound);
        }

        private static byte[] Sample(int length, int seed)
        {
            var state = (uint)seed * 2654435761u + 1;
            var data = new byte[length];
            var i = 0;
            while (i < length)
            {
                if (i > 40000 && Next(ref state, 3) == 0)
                {
                    var distance = 1 + Next(ref state, 32767);
                    var count = Math.Min(length - i, 3 + Next(ref state, 300));
                    for (var j = 0; j < count; j++, i++)
                    {
                        data[i] = data[i - distance];
                    }
                }
                else
                {
                    data[i++] = (byte)Next(ref state, 256);
                }
            }

            return data;
        }
    }
}
