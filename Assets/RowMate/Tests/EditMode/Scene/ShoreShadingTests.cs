using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SSH · La luz y la niebla de la orilla (shore_painter.dart:45-50; AC-SCN-20-01). Contra el oráculo, bit a bit: 6 colores de
    // referencia × 25 horas × 9 distancias.
    [TestFixture]
    public sealed class ShoreShadingTests
    {
        [Test, Category("AC-SCN-20-01")]
        public void TintAndFogMatchTheOracle()
        {
            var colors = new System.Collections.Generic.Dictionary<string, SceneColor>(StringComparer.Ordinal)
            {
                ["trunk"] = SceneColor.FromArgb(LakeArt.Trunk),
                ["pine0"] = SceneColor.FromArgb(LakeArt.PineGreens[0]),
                ["cabin"] = SceneColor.FromArgb(LakeArt.CabinWood),
                ["roof"] = SceneColor.FromArgb(LakeArt.CabinRoof),
                ["pier"] = SceneColor.FromArgb(LakeArt.PierDeck),
                ["window"] = SceneColor.FromArgb(LakeArt.WindowDay),
            };
            var rows = GoldenVectors.CheckRows("scene_sky.json", "shore", row =>
            {
                var p = SceneTimeOfDay.PaletteFor(row.Int("hour"));
                var c = ShoreShading.Shade(colors[row.Text("color")], row.F64("relZ"), p);
                return Bits(c.R) == Bits(row.F64("r")) && Bits(c.G) == Bits(row.F64("g")) && Bits(c.B) == Bits(row.F64("b")) ? null : c.ToString();
            });
            Assert.That(rows, Is.EqualTo(6 * 25 * 9));
        }

        [TestCase(0.0, 0.0, Category = "AC-SCN-20-01")]
        [TestCase(80.0, 0.0, Category = "AC-SCN-20-01")]
        [TestCase(240.0, 0.4, Category = "AC-SCN-20-01")]
        [TestCase(400.0, 0.8, Category = "AC-SCN-20-01")]
        [TestCase(1000.0, 0.8, Category = "AC-SCN-20-01")]
        public void TheFogOfTheLegacy(double relZ, double fog)
        {
            Assert.That(ShoreShading.Fog(relZ), Is.EqualTo(fog).Within(1e-12));
        }

        [TestCase(400.0, 0.8, Category = "AC-SCN-20-01")]
        [TestCase(800.0, 0.9, Category = "AC-SCN-20-01")]
        [TestCase(1200.0, 1.0, Category = "AC-SCN-20-01")]
        [TestCase(1500.0, 1.0, Category = "AC-SCN-20-01")]
        [TestCase(240.0, 0.4, Category = "AC-SCN-20-01")]
        public void TheExtendedFogIsTheLegacyUntil400(double relZ, double fog)
        {
            Assert.That(ShoreShading.ExtendedFog(relZ), Is.EqualTo(fog).Within(1e-12));
        }

        [Test, Category("AC-SCN-20-01")]
        public void TheTintIsBlueAtNight()
        {
            var c = SceneColor.FromHex("#808080");
            var night = ShoreShading.Tint(c, 0.35);
            Assert.That(night.R, Is.EqualTo(c.R * 0.35));
            Assert.That(night.G, Is.EqualTo(c.G * 0.35));
            Assert.That(night.B, Is.EqualTo(c.B * 0.47).Within(1e-15), "azul × mín(1; a + 0,12)");
            var day = ShoreShading.Tint(c, 1.0);
            Assert.That(day.B, Is.EqualTo(c.B), "de día el azul no pasa de 1");
        }

        private static long Bits(double d)
        {
            return BitConverter.DoubleToInt64Bits(d);
        }
    }

    // DWT · El agua degradada (water_painter.dart:79-103; AC-SCN-13-02, -03, AC-SCN-01-15; DV-59, DN6a-06). Contra el oráculo, bit a
    // bit, con la fuerza de la luz leída del archivo (no recalculada): las líneas que dibujó el WaterPainter del legacy sin shader.
    [TestFixture]
    public sealed class DegradedWaterTests
    {
        [Test, Category("AC-SCN-13-02")]
        public void TheThreeBandsMatchTheOracle()
        {
            var rows = GoldenVectors.CheckRows("scene_water.json", "bands", row =>
            {
                var p = SceneTimeOfDay.PaletteFor(row.Int("hour"));
                DegradedWater.Bands(p, out var top, out var mid, out var bottom);
                if (!Same(top, row, "top") || !Same(mid, row, "mid") || !Same(bottom, row, "bottom"))
                {
                    return top + " / " + mid + " / " + bottom;
                }

                return mid.Equals(p.WaterFar) && bottom.Equals(p.WaterNear) ? null : "el lago no tiñe el agua";
            });
            Assert.That(rows, Is.EqualTo(25));
        }

        [Test, Category("AC-SCN-13-03"), Category("AC-SCN-01-15")]
        public void TheTenLinesMatchTheOracle()
        {
            var rows = GoldenVectors.CheckRows("scene_water.json", "lines", row =>
            {
                var cam = SceneSeeds.Camera(row.Text("size"));
                var line = DegradedWater.Line((int)row.Int("i"), row.F64("distance"), cam.Width, cam.HorizonY, DegradedWater.WaterHeight(cam),
                    row.F64("centerX"), row.F64("strength"));
                if (Bits(line.T) != Bits(row.F64("t")))
                {
                    return "t " + SceneGolden.F(line.T);
                }

                if (Bits(line.Y) != Bits(row.F64("y")))
                {
                    return "y " + SceneGolden.F(line.Y);
                }

                if (Bits(line.CenterX - line.HalfWidth) != Bits(row.F64("x0")) || Bits(line.CenterX + line.HalfWidth) != Bits(row.F64("x1")))
                {
                    return "extremos";
                }

                return Bits(line.Alpha) == Bits(row.F64("alpha")) ? null : "alfa " + SceneGolden.F(line.Alpha);
            });
            Assert.That(rows, Is.EqualTo(2 * 4 * 6 * 10));
        }

        [Test, Category("AC-SCN-13-03")]
        public void AtZeroAtNoonTheFirstLine()
        {
            // Ejemplo de la spec: a 0 m al mediodía, la línea 0 en el horizonte, de 0,3 del ancho y opacidad 0,15.
            var cam = new SceneCamera(390, 844);
            var light = SceneTimeOfDay.LightFor(12, cam);
            var line = DegradedWater.Line(0, 0, cam.Width, cam.HorizonY, DegradedWater.WaterHeight(cam), light.X, light.Strength);
            Assert.That(line.Y, Is.EqualTo(cam.HorizonY));
            Assert.That(2 * line.HalfWidth / cam.Width, Is.EqualTo(0.3).Within(1e-12));
            Assert.That(line.Alpha, Is.EqualTo(0.15).Within(1e-12));
        }

        [Test, Category("AC-SCN-01-15"), Category("AC-SCN-13-03")]
        public void ThePhaseHasNoJumpsAt4096Or42195()
        {
            // Un ciclo cada 5 m; con el bote quieto, quietas; sin saltos cada 4 096 m.
            foreach (var d in new[] { 4096.0, 8192.0, 42195.0, 100000.0 })
            {
                var before = DegradedWater.Phase(d - 0.01);
                var after = DegradedWater.Phase(d + 0.01);
                var step = ((after - before) % 0.1 + 0.1) % 0.1;
                Assert.That(step, Is.EqualTo(0.0004).Within(1e-9), "a " + d + " m");
            }

            Assert.That(DegradedWater.Phase(5), Is.EqualTo(DegradedWater.Phase(0)).Within(1e-12), "un ciclo cada 5 m");
            Assert.That(DegradedWater.Phase(1234.5), Is.EqualTo(DegradedWater.Phase(1234.5)));
        }

        [Test, Category("AC-SCN-13-03")]
        public void OnScreenTheLinesFollowTheVisibleStar()
        {
            // DV-59: el alfa del legacy × la visibilidad del astro que se ve; 0 a las 19:34 y a las 4:26.
            foreach (var (h, m) in new[] { (19, 34), (4, 26) })
            {
                var hour = SceneSeeds.HourOfMinute(h * 60 + m);
                var sky = CelestialPlacement.Evaluate(hour, 0.94, CelestialSpec.Default);
                Assert.That(DegradedWater.ScreenAlpha(0.15, sky.DiskVisibility), Is.EqualTo(0).Within(1e-9), h + ":" + m);
            }

            foreach (var hour in new[] { 12.0, 22.0, 6.0, 18.0 })
            {
                var sky = CelestialPlacement.Evaluate(hour, 0.94, CelestialSpec.Default);
                Assert.That(DegradedWater.ScreenAlpha(0.15, sky.DiskVisibility), Is.EqualTo(0.15).Within(1e-12), "el alfa del legacy a las " + hour);
            }
        }

        [Test, Category("AC-SCN-13-03")]
        public void ScreenAlphaMinuteByMinuteNeverJumps()
        {
            var cam = new SceneCamera(914, 411);
            var prev = double.NaN;
            var prevX = double.NaN;
            for (var m = 0; m <= 1440; m++)
            {
                var hour = SceneSeeds.HourOfMinute(m % 1440);
                var sky = CelestialPlacement.Evaluate(hour, 0.94, CelestialSpec.Default);
                var legacy = SceneTimeOfDay.LightFor(hour, cam);
                var alpha = DegradedWater.ScreenAlpha(0.05 + 0.1 * legacy.Strength, sky.DiskVisibility);
                var x = DegradedWater.DiskScreenX(cam, sky.DiskAzimuthDeg);
                if (!double.IsNaN(prev))
                {
                    Assert.That(Math.Abs(alpha - prev), Is.LessThan(0.02), "a las " + hour);
                    if (Math.Abs(x - prevX) > 20)
                    {
                        Assert.That(Math.Max(alpha, prev), Is.LessThan(1e-3), "si el centro salta de lado, las líneas están apagadas");
                    }
                }

                prev = alpha;
                prevX = x;
            }
        }

        private static bool Same(in SceneColor c, GoldenRow row, string prefix)
        {
            return Bits(c.R) == Bits(row.F64(prefix + "R")) && Bits(c.G) == Bits(row.F64(prefix + "G")) && Bits(c.B) == Bits(row.F64(prefix + "B"));
        }

        private static long Bits(double d)
        {
            return BitConverter.DoubleToInt64Bits(d);
        }
    }
}
