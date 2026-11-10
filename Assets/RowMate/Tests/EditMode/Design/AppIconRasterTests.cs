using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // IR · El rasterizador del ícono (It. 14, DT14-01; 00-arte §1.4 y §1.5): formas, fondos, suavizado y determinismo.
    [TestFixture]
    public sealed class AppIconRasterTests
    {
        [Test, Category("AC-APP-16-08")]
        public void PlateHasBgAppInsideAndTransparentCorners()
        {
            var image = AppIconRaster.Render(AppIconVariant.Plate, 256);
            image.Pixel(0, 0, out _, out _, out _, out var corner);
            Assert.That(corner, Is.EqualTo(0), "afuera de la placa");
            image.Pixel(30, 128, out var r, out var g, out var b, out var a);
            Assert.That(a, Is.EqualTo(255));
            Assert.That(new Rgb(r, g, b), Is.EqualTo(Tokens.BgApp.Rgb), "bg.app liso (DT14-03)");
            Assert.That(AppIconRaster.PlateMargin(16), Is.EqualTo(1));
            Assert.That(AppIconRaster.PlateRadius(16), Is.EqualTo(3));
            Assert.That(AppIconRaster.PlateMargin(256), Is.EqualTo(16));
            Assert.That(AppIconRaster.PlateRadius(256), Is.EqualTo(49));
        }

        [Test, Category("AC-APP-16-08")]
        public void CrossIsTheOarColorAtSixteen()
        {
            // A 16 px la silueta se distingue: el cruce (50; 47,76) cae en el píxel (7; 7) y es del color de los remos.
            var image = AppIconRaster.Render(AppIconVariant.Plate, 16);
            image.Pixel(7, 7, out var r, out var g, out var b, out var a);
            Assert.That(a, Is.EqualTo(255));
            Assert.That(new Rgb(r, g, b), Is.EqualTo(Tokens.AccentPrimary.Rgb));
            var oar = 0;
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    image.Pixel(x, y, out r, out g, out b, out _);
                    if (new Rgb(r, g, b) == Tokens.AccentPrimary.Rgb)
                    {
                        oar++;
                    }
                }
            }

            Assert.That(oar, Is.GreaterThanOrEqualTo(30), "una X legible, no unos píxeles sueltos");
        }

        [Test, Category("AC-APP-16-08")]
        public void IosIsOpaqueAndAntialiased()
        {
            var image = AppIconRaster.Render(AppIconVariant.Bleed, 128);
            Assert.That(image.IsOpaque, Is.True, "iOS: a sangre, sin alfa");
            var plate = AppIconRaster.Render(AppIconVariant.Plate, 128);
            var partial = 0;
            for (var i = 3; i < plate.Rgba.Length; i += 4)
            {
                if (plate.Rgba[i] > 0 && plate.Rgba[i] < 255)
                {
                    partial++;
                }
            }

            Assert.That(partial, Is.GreaterThan(20), "borde de la placa suavizado (8 × 8 muestras)");
        }

        [Test, Category("AC-APP-16-09")]
        public void AdaptiveLayers()
        {
            var background = AppIconRaster.Render(AppIconVariant.AdaptiveBackground, 108);
            Assert.That(background.IsOpaque, Is.True);
            for (var i = 0; i < background.Rgba.Length; i += 4)
            {
                Assert.That(new Rgb(background.Rgba[i], background.Rgba[i + 1], background.Rgba[i + 2]), Is.EqualTo(Tokens.BgApp.Rgb));
            }

            var foreground = AppIconRaster.Render(AppIconVariant.AdaptiveForeground, 108);
            foreground.Pixel(0, 0, out _, out _, out _, out var a);
            Assert.That(a, Is.EqualTo(0), "frente transparente alrededor");
            foreground.Pixel(53, 52, out var r, out var g, out var b, out a);
            Assert.That(a, Is.EqualTo(255));
            Assert.That(new Rgb(r, g, b), Is.EqualTo(Tokens.AccentPrimary.Rgb), "el cruce de los remos");
        }

        [Test, Category("AC-APP-16-08")]
        public void RoundMacAndBootMark()
        {
            var round = AppIconRaster.Render(AppIconVariant.Round, 96);
            round.Pixel(2, 48, out _, out _, out _, out var outside);
            Assert.That(outside, Is.EqualTo(0), "disco de 0,92");
            var mac = AppIconRaster.Render(AppIconVariant.MacPlate, 128);
            mac.Pixel(64, 6, out _, out _, out _, out var top);
            Assert.That(top, Is.EqualTo(0), "margen de 100/1024 arriba");
            mac.Pixel(64, 117, out _, out _, out _, out var shadow);
            Assert.That(shadow, Is.GreaterThan(0), "sombra debajo de la placa");
            Assert.That(shadow, Is.LessThan(80));
            var boot = AppIconRaster.Render(AppIconVariant.BootMark, 384);
            boot.Pixel(0, 0, out _, out _, out _, out var corner);
            Assert.That(corner, Is.EqualTo(0), "sin fondo");
            Assert.That(AppIconRaster.MarkPixels(AppIconVariant.BootMark, 384) * 52.8 / 100, Is.LessThan(192), "el radio máximo entra en la caja");
        }

        [Test, Category("AC-APP-16-08")]
        public void SameBytesTwice()
        {
            var a = AppIconRaster.Render(AppIconVariant.Plate, 64).ToPng();
            var b = AppIconRaster.Render(AppIconVariant.Plate, 64).ToPng();
            Assert.That(a, Is.EqualTo(b));
            Assert.Throws<ArgumentOutOfRangeException>(() => AppIconRaster.Render(AppIconVariant.Plate, 0));
        }
    }
}
