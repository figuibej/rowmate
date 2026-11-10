using System;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // IG · La geometría del ícono "amanecer de regata" (It. 14, 00-arte §1.3 y §1.4; DT14-03, DT14-04).
    [TestFixture]
    public sealed class AppIconGeometryTests
    {
        [Test, Category("AC-APP-16-08")]
        public void OarsCrossInFrontOfTheSun()
        {
            Assert.That(AppIconGeometry.OarLength, Is.EqualTo(105.262).Within(0.001));
            // El remo derecho en x = 50 y su espejo cortan en el mismo punto.
            var s = (AppIconGeometry.CrossX - AppIconGeometry.HandleX) * AppIconGeometry.OarLength / (AppIconGeometry.TipX - AppIconGeometry.HandleX);
            AppIconGeometry.OarPoint(true, s, out var x, out var y);
            AppIconGeometry.OarPoint(false, s, out var lx, out var ly);
            Assert.That(x, Is.EqualTo(50).Within(1e-9));
            Assert.That(lx, Is.EqualTo(50).Within(1e-9));
            Assert.That(y, Is.EqualTo(47.76).Within(0.01));
            Assert.That(ly, Is.EqualTo(y).Within(1e-12));
            // La pala termina en T (s = L) y empieza a la altura de la ola 1.
            AppIconGeometry.OarPoint(true, AppIconGeometry.OarLength, out var tx, out var ty);
            Assert.That(tx, Is.EqualTo(84).Within(1e-9));
            Assert.That(ty, Is.EqualTo(90).Within(1e-9));
            AppIconGeometry.OarPoint(true, 80, out _, out var bladeY);
            Assert.That(bladeY, Is.EqualTo(70.3).Within(0.1), "la pala entra al agua (W1 en y = 66 ± 2,2)");
        }

        [Test, Category("AC-APP-16-08")]
        public void LevelsBySize()
        {
            Assert.That(AppIconLevels.For(1024), Is.EqualTo(AppIconLevel.M));
            Assert.That(AppIconLevels.For(49), Is.EqualTo(AppIconLevel.M));
            Assert.That(AppIconLevels.For(48), Is.EqualTo(AppIconLevel.S));
            Assert.That(AppIconLevels.For(25), Is.EqualTo(AppIconLevel.S));
            Assert.That(AppIconLevels.For(24), Is.EqualTo(AppIconLevel.XS));
            Assert.That(AppIconLevels.For(16), Is.EqualTo(AppIconLevel.XS));
            Assert.That(AppIconLevels.MarkScale(AppIconLevel.M), Is.EqualTo(0.70));
            Assert.That(AppIconLevels.MarkScale(AppIconLevel.S), Is.EqualTo(0.80));
            Assert.That(AppIconLevels.MarkScale(AppIconLevel.XS), Is.EqualTo(0.88));
            Assert.Throws<ArgumentOutOfRangeException>(() => AppIconLevels.For(0));
        }

        [Test, Category("AC-APP-16-08")]
        public void PiecesPerLevelInDrawingOrder()
        {
            var m = AppIconGeometry.Mark(AppIconLevel.M).Select(p => p.Name).ToArray();
            Assert.That(m, Is.EqualTo(new[]
            {
                "G", "S", "W1", "W2",
                "OL.handle.ko", "OL.shaft.ko", "OL.blade.ko", "OL.handle", "OL.shaft", "OL.blade",
                "OR.handle.ko", "OR.shaft.ko", "OR.blade.ko", "OR.handle", "OR.shaft", "OR.blade",
            }));
            var s = AppIconGeometry.Mark(AppIconLevel.S).Select(p => p.Name).ToArray();
            Assert.That(s, Is.EqualTo(new[]
            {
                "S", "W1", "OL.shaft.ko", "OL.blade.ko", "OL.shaft", "OL.blade", "OR.shaft.ko", "OR.blade.ko", "OR.shaft", "OR.blade",
            }));
            // DT14-04: XS solo con los remos, sin recorte (la cruz en una pieza).
            var xs = AppIconGeometry.Mark(AppIconLevel.XS);
            Assert.That(xs.Select(p => p.Name).ToArray(), Is.EqualTo(new[] { "OL.shaft", "OL.blade", "OR.shaft", "OR.blade" }));
            Assert.That(xs.Any(p => p.KnockOut), Is.False);
            Assert.That(AppIconGeometry.Mark(AppIconLevel.M, glow: false).Any(p => p.Shape == IconShape.RadialGlow), Is.False);
        }

        [Test, Category("AC-APP-16-08")]
        public void ColorsFollowTheSpec()
        {
            // DT14-03: remos y ola 1 en accent.primary, ola 2 en brand.seed, el sol de step.warmup a metric.split.
            var m = AppIconGeometry.Mark(AppIconLevel.M);
            Assert.That(m.Single(p => p.Name == "OR.shaft").Color, Is.EqualTo(Tokens.AccentPrimary.Rgb));
            Assert.That(m.Single(p => p.Name == "W1").Color, Is.EqualTo(Tokens.AccentPrimary.Rgb));
            Assert.That(m.Single(p => p.Name == "W2").Color, Is.EqualTo(Tokens.BrandSeed.Rgb));
            var sun = m.Single(p => p.Name == "S");
            Assert.That(sun.Color, Is.EqualTo(Tokens.StepWarmup.Rgb));
            Assert.That(sun.Color2, Is.EqualTo(Tokens.MetricSplit.Rgb));
            Assert.That(m.Single(p => p.Name == "G").Alpha0, Is.EqualTo(0.20));
        }

        [Test, Category("AC-APP-16-08")]
        public void MaxRadiusIsTheHandle()
        {
            // 00-arte §1.3: 52,8 desde (50; 50) (la empuñadura en H); el resplandor se desvanece a 0 y no cuenta.
            var radius = AppIconGeometry.MaxRadius(AppIconGeometry.Mark(AppIconLevel.M));
            Assert.That(radius, Is.EqualTo(52.8).Within(0.01));
            Assert.That(radius, Is.LessThanOrEqualTo(52.81));
        }

        [Test, Category("AC-APP-16-09")]
        public void AdaptiveForegroundInsideSafeZone()
        {
            // La marca de 60 dp centrada en 108: el radio máximo (31,7 dp) entra en el círculo seguro de 66 dp (33 de radio).
            var radiusDp = AppIconGeometry.MaxRadius(AppIconGeometry.Mark(AppIconLevel.M, glow: false)) * 60 / 100;
            Assert.That(radiusDp, Is.EqualTo(31.7).Within(0.05));
            Assert.That(radiusDp, Is.LessThan(33));

            // Y en los píxeles: nada visible fuera de 33 dp (+1 px de suavizado) en el frente de 432 px.
            var image = AppIconRaster.Render(AppIconVariant.AdaptiveForeground, 432);
            var dp = 432 / 108.0;
            var outside = 0;
            for (var y = 0; y < 432; y++)
            {
                for (var x = 0; x < 432; x++)
                {
                    image.Pixel(x, y, out _, out _, out _, out var a);
                    var d = Math.Sqrt((x + 0.5 - 216) * (x + 0.5 - 216) + (y + 0.5 - 216) * (y + 0.5 - 216));
                    if (a > 0 && d > 33 * dp + 1)
                    {
                        outside++;
                    }
                }
            }

            Assert.That(outside, Is.EqualTo(0), "píxeles fuera de la zona segura");
        }

        [TestCase(16)]
        [TestCase(24)]
        [Category("AC-APP-16-08")]
        public void NothingThinnerThanTwoPixelsAtSixteen(int size)
        {
            // AC-APP-16-08 (DT14-04): a 16 px, ninguna caña ni pala mide menos de 2 px, y no hay huecos (XS no tiene recortes).
            var markPx = AppIconRaster.MarkPixels(AppIconVariant.Plate, size);
            foreach (var piece in AppIconGeometry.Mark(AppIconLevels.For(size)))
            {
                Assert.That(piece.KnockOut, Is.False, piece.Name);
                Assert.That(piece.Thickness * markPx / 100, Is.GreaterThanOrEqualTo(2.0), piece.Name);
            }

            Assert.That(AppIconGeometry.Mark(AppIconLevel.XS).Single(p => p.Name == "OR.shaft").Thickness * 0.88 * 16 / 100, Is.EqualTo(2.11).Within(0.01));
            Assert.That(AppIconGeometry.Mark(AppIconLevel.XS).Single(p => p.Name == "OR.blade").Thickness * 0.88 * 16 / 100, Is.EqualTo(3.10).Within(0.01));
        }
    }
}
