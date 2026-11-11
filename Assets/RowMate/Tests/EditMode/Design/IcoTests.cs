using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Design
{
    // ICO · El .ico de Windows que escribe tools/icon (It. 15, #74, AC-D15-20): 16, 24, 32, 48, 64 y 256 px, cada uno como el PNG de la
    // placa (los mismos bytes que Standalone/icon-N.png), reproducible y versionado en Art/Identity; Unity no lo usa.
    [TestFixture]
    public sealed class IcoTests
    {
        private static readonly string IcoPath = AppIconOutputs.IdentityFolder + "/" + AppIconIco.FileName;

        private static byte[] Generated()
        {
            foreach (var output in AppIconAssetsAreCurrentTests.Outputs)
            {
                if (output.RelativePath == IcoPath)
                {
                    return output.Bytes;
                }
            }

            Assert.Fail("AppIconOutputs no genera " + IcoPath);
            return new byte[0];
        }

        [Test, Category("AC-D15-20")]
        public void SixSizesReproducible()
        {
            var ico = Generated();
            Assert.That(AppIconIco.Sizes, Is.EqualTo(new[] { 16, 24, 32, 48, 64, 256 }));
            Assert.That(U16(ico, 0), Is.EqualTo(0), "reservado");
            Assert.That(U16(ico, 2), Is.EqualTo(1), "tipo ícono");
            Assert.That(U16(ico, 4), Is.EqualTo(6), "6 imágenes");
            var expectedOffset = 6 + 16 * 6;
            for (var i = 0; i < 6; i++)
            {
                var entry = 6 + 16 * i;
                var size = AppIconIco.Sizes[i];
                Assert.That(ico[entry], Is.EqualTo(size == 256 ? 0 : size), "ancho de " + size);
                Assert.That(ico[entry + 1], Is.EqualTo(size == 256 ? 0 : size), "alto de " + size);
                Assert.That(U16(ico, entry + 4), Is.EqualTo(1), "planos");
                Assert.That(U16(ico, entry + 6), Is.EqualTo(32), "bits");
                var length = (int)U32(ico, entry + 8);
                var offset = (int)U32(ico, entry + 12);
                Assert.That(offset, Is.EqualTo(expectedOffset), "las imágenes van seguidas");
                var png = new byte[length];
                Array.Copy(ico, offset, png, 0, length);
                Assert.That(png, Is.EqualTo(AppIconRaster.Render(AppIconVariant.Plate, size).ToPng(true)), "el PNG de la placa a " + size);
                Assert.That(png[1] == 'P' && png[2] == 'N' && png[3] == 'G', Is.True);
                expectedOffset += length;
            }

            Assert.That(ico.Length, Is.EqualTo(expectedOffset), "sin bytes de más");
            Assert.That(AppIconOutputs.All()[0].Bytes, Is.Not.Null);
            var again = new List<KeyValuePair<int, byte[]>>();
            foreach (var size in AppIconIco.Sizes)
            {
                again.Add(new KeyValuePair<int, byte[]>(size, AppIconRaster.Render(AppIconVariant.Plate, size).ToPng(true)));
            }

            Assert.That(AppIconIco.Encode(again), Is.EqualTo(ico), "dos armados, los mismos bytes");
        }

        [Test, Category("AC-D15-20")]
        public void VersionedIcoIsCurrent()
        {
            var path = RepoPaths.Combine(IcoPath);
            Assert.That(File.Exists(path), Is.True, IcoPath + " versionado (tools/icon)");
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(Generated()), "regenerá con tools/icon");
            var attributes = File.ReadAllText(RepoPaths.Combine(".gitattributes"));
            Assert.That(attributes, Does.Contain("Assets/RowMate/Art/Identity/*.ico -filter -diff binary"), "sin LFS: ci-core no baja LFS");
        }

        private static int U16(byte[] data, int at)
        {
            return data[at] | (data[at + 1] << 8);
        }

        private static uint U32(byte[] data, int at)
        {
            return (uint)(data[at] | (data[at + 1] << 8) | (data[at + 2] << 16) | (data[at + 3] << 24));
        }
    }
}
