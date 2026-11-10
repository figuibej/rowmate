using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // PN · El PNG determinista del ícono (It. 14, DT14-01): solo IHDR, IDAT e IEND con CRC válidos, RGBA o RGB, filtro Paeth en
    // todas las filas, y los píxeles de vuelta iguales.
    [TestFixture]
    public sealed class PngEncoderTests
    {
        [Test, Category("AC-APP-16-08")]
        public void OnlyTheThreeChunksWithValidCrc()
        {
            var png = AppIconRaster.Render(AppIconVariant.Plate, 32).ToPng();
            var chunks = Chunks(png, out var ihdr, out _);
            Assert.That(chunks, Is.EqualTo(new[] { "IHDR", "IDAT", "IEND" }), "sin tIME, gAMA, sRGB ni tEXt");
            Assert.That(ReadUInt32(ihdr, 0), Is.EqualTo(32u));
            Assert.That(ReadUInt32(ihdr, 4), Is.EqualTo(32u));
            Assert.That(ihdr[8], Is.EqualTo(8), "8 bits");
            Assert.That(ihdr[9], Is.EqualTo(6), "RGBA");
            Assert.That(ihdr[12], Is.EqualTo(0), "sin entrelazado");
        }

        [Test, Category("AC-APP-16-08")]
        public void RgbWithoutAlphaOnlyIfOpaque()
        {
            var opaque = AppIconRaster.Render(AppIconVariant.Bleed, 16);
            Chunks(opaque.ToPng(false), out var ihdr, out _);
            Assert.That(ihdr[9], Is.EqualTo(2), "RGB: iOS sin canal alfa");
            var plate = AppIconRaster.Render(AppIconVariant.Plate, 16);
            Assert.Throws<ArgumentException>(() => plate.ToPng(false));
        }

        [Test, Category("AC-APP-16-08")]
        public void PaethPredictor()
        {
            Assert.That(PngEncoder.Paeth(10, 20, 10), Is.EqualTo(20));
            Assert.That(PngEncoder.Paeth(20, 10, 10), Is.EqualTo(20));
            Assert.That(PngEncoder.Paeth(10, 10, 10), Is.EqualTo(10));
            Assert.That(PngEncoder.Paeth(0, 0, 255), Is.EqualTo(0));
        }

        [TestCase(true)]
        [TestCase(false)]
        [Category("AC-APP-16-08")]
        public void PixelsComeBack(bool withAlpha)
        {
            var image = AppIconRaster.Render(withAlpha ? AppIconVariant.Plate : AppIconVariant.Bleed, 48);
            var png = image.ToPng(withAlpha);
            Chunks(png, out _, out var idat);
            var raw = DeflateEncoderTests.Inflate(idat);
            var channels = withAlpha ? 4 : 3;
            var stride = 48 * channels;
            Assert.That(raw.Length, Is.EqualTo(48 * (stride + 1)));
            var previous = new byte[stride];
            for (var y = 0; y < 48; y++)
            {
                Assert.That(raw[y * (stride + 1)], Is.EqualTo(4), "Paeth en la fila " + y);
                var row = new byte[stride];
                for (var i = 0; i < stride; i++)
                {
                    var a = i >= channels ? row[i - channels] : 0;
                    var c = i >= channels ? previous[i - channels] : 0;
                    row[i] = (byte)(raw[y * (stride + 1) + 1 + i] + PngEncoder.Paeth(a, previous[i], c));
                }

                for (var x = 0; x < 48; x++)
                {
                    for (var k = 0; k < channels; k++)
                    {
                        Assert.That(row[x * channels + k], Is.EqualTo(image.Rgba[(y * 48 + x) * 4 + k]), "(" + x + ", " + y + ")");
                    }
                }

                previous = row;
            }
        }

        public static List<string> Chunks(byte[] png, out byte[] ihdr, out byte[] idat)
        {
            for (var i = 0; i < PngEncoder.Signature.Length; i++)
            {
                Assert.That(png[i], Is.EqualTo(PngEncoder.Signature[i]), "firma");
            }

            var names = new List<string>();
            ihdr = new byte[0];
            idat = new byte[0];
            var at = 8;
            while (at < png.Length)
            {
                var length = (int)ReadUInt32(png, at);
                var name = Encoding.ASCII.GetString(png, at + 4, 4);
                var data = new byte[length];
                Array.Copy(png, at + 8, data, 0, length);
                Assert.That(ReadUInt32(png, at + 8 + length), Is.EqualTo(Crc32.Compute(png, at + 4, 4 + length)), "CRC de " + name);
                names.Add(name);
                if (name == "IHDR")
                {
                    ihdr = data;
                }
                else if (name == "IDAT")
                {
                    idat = data;
                }

                at += 12 + length;
            }

            return names;
        }

        private static uint ReadUInt32(byte[] data, int at)
        {
            return (uint)((data[at] << 24) | (data[at + 1] << 16) | (data[at + 2] << 8) | data[at + 3]);
        }
    }
}
