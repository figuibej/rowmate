using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // STD · La hora del día del legacy (scene/time_of_day.dart; F-SCN-09, F-SCN-10; LT-69 a LT-74; AC-SCN-09-02, -03, AC-SCN-10-01
    // a -03). Comparadores (02-tech-design §2, QA-6a-PAR-05): la paleta y el ambiente bit a bit; sunElevation con Within(1e-12);
    // el astro del legacy con Within(1e-9) en dp y la fuerza con Within(1e-12); IsNight exacto con el margen afirmado.
    [TestFixture]
    public sealed class SceneTimeOfDayTests
    {
        private static readonly SceneCamera V390 = new SceneCamera(390, 844);

        // legacy: test/scene/time_of_day_test.dart:9 '12:00 es día con el sol arriba'
        [TestCase(TestName = "12:00 es día con el sol arriba", Category = "LT-69,AC-SCN-10-01,AC-SCN-10-02,AC-SCN-10-03")]
        public void NoonIsDayWithTheSunUp()
        {
            Assert.That(SceneTimeOfDay.IsNight(12), Is.False);
            var light = SceneTimeOfDay.LightFor(12, V390);
            Assert.That(light.IsMoon, Is.False);
            Assert.That(light.Y, Is.LessThan(V390.HorizonY * 0.2));
            Assert.That(light.Strength, Is.EqualTo(1.0));
        }

        // legacy: test/scene/time_of_day_test.dart:17 '23:00 es noche con luna'
        [TestCase(TestName = "23:00 es noche con luna", Category = "LT-70,AC-SCN-10-01,AC-SCN-10-02,AC-SCN-09-02")]
        public void ElevenPmIsNightWithMoon()
        {
            Assert.That(SceneTimeOfDay.IsNight(23), Is.True);
            var light = SceneTimeOfDay.LightFor(23, V390);
            Assert.That(light.IsMoon, Is.True);
            Assert.That(light.Y, Is.LessThan(V390.HorizonY));
            Assert.That(SceneTimeOfDay.PaletteFor(23).Ambient, Is.LessThan(0.5));
        }

        // legacy: test/scene/time_of_day_test.dart:25 '18:30 interpola entre atardecer y anochecer'
        [TestCase(TestName = "18:30 interpola entre atardecer y anochecer", Category = "LT-71,AC-SCN-09-02,AC-SCN-09-03")]
        public void HalfPastSixInterpolatesSunsetAndDusk()
        {
            var p = SceneTimeOfDay.PaletteFor(18.5);
            Assert.That(p.Ambient, Is.EqualTo(0.85 + (0.5 - 0.85) * 0.25).Within(1e-9));
            Assert.That(p.SkyHorizon, Is.Not.EqualTo(SceneTimeOfDay.PaletteFor(18).SkyHorizon));
            Assert.That(p.SkyHorizon, Is.Not.EqualTo(SceneTimeOfDay.PaletteFor(20).SkyHorizon));
        }

        // legacy: test/scene/time_of_day_test.dart:32 'de 22 h a 4 h la paleta es noche cerrada, sin falso amanecer'
        [TestCase(TestName = "de 22 h a 4 h la paleta es noche cerrada, sin falso amanecer", Category = "LT-72,AC-SCN-09-02")]
        public void TenPmToFourAmIsDeepNight()
        {
            Assert.That(SceneTimeOfDay.PaletteFor(0).Ambient, Is.EqualTo(0.35));
            Assert.That(SceneTimeOfDay.PaletteFor(3).Ambient, Is.EqualTo(0.35));
        }

        // legacy: test/scene/time_of_day_test.dart:37 'a las 19 h sigue el atardecer y a las 20 h ya sale la luna'
        [TestCase(TestName = "a las 19 h sigue el atardecer y a las 20 h ya sale la luna", Category = "LT-73,AC-SCN-10-01")]
        public void SevenPmIsStillDayAndEightPmHasTheMoon()
        {
            Assert.That(SceneTimeOfDay.IsNight(19), Is.False);
            Assert.That(SceneTimeOfDay.IsNight(20), Is.True);
            Assert.That(SceneTimeOfDay.IsNight(5), Is.False);
            Assert.That(SceneTimeOfDay.LightFor(5, V390).IsMoon, Is.False);
        }

        // legacy: test/scene/time_of_day_test.dart:44 'la paleta es continua a medianoche y al amanecer'
        [TestCase(TestName = "la paleta es continua a medianoche y al amanecer", Category = "LT-74,AC-SCN-09-02")]
        public void PaletteIsContinuousAtMidnightAndDawn()
        {
            Assert.That(SceneTimeOfDay.PaletteFor(23.99).Ambient, Is.EqualTo(SceneTimeOfDay.PaletteFor(0).Ambient).Within(0.01));
            Assert.That(SceneTimeOfDay.PaletteFor(4.99).Ambient, Is.EqualTo(SceneTimeOfDay.PaletteFor(5).Ambient).Within(0.01));
        }

        [TestCase(0.0, 0.35, Category = "AC-SCN-09-03")]
        [TestCase(2.0, 0.35, Category = "AC-SCN-09-03")]
        [TestCase(4.0, 0.35, Category = "AC-SCN-09-03")]
        [TestCase(4.5, 0.525, Category = "AC-SCN-09-03")]
        [TestCase(6.0, 0.825, Category = "AC-SCN-09-03")]
        [TestCase(9.0, 0.97, Category = "AC-SCN-09-03")]
        [TestCase(15.0, 0.925, Category = "AC-SCN-09-03")]
        [TestCase(18.5, 0.7625, Category = "AC-SCN-09-03")]
        [TestCase(19.0, 0.675, Category = "AC-SCN-09-03")]
        [TestCase(21.0, 0.425, Category = "AC-SCN-09-03")]
        public void ReferenceAmbients(double hour, double ambient)
        {
            Assert.That(SceneTimeOfDay.PaletteFor(hour).Ambient, Is.EqualTo(ambient).Within(1e-12));
        }

        [TestCase(5.0, "#2A1B5E", "#FF9A5C", "#1A2B4A", "#3A2F55", "#FFB070", "#FF7A3D", "#C98A7A", 0.70, Category = "AC-SCN-09-02")]
        [TestCase(7.0, "#3A7BD5", "#FFD9A0", "#1E5A9E", "#5C8FC9", "#FFE4A0", "#FFC46B", "#DCE8F5", 0.95, Category = "AC-SCN-09-02")]
        [TestCase(12.0, "#0B4FA8", "#8FD3FF", "#0E4F8F", "#2F8FD6", "#FFF6D5", "#FFE9A6", "#CFE9FF", 1.00, Category = "AC-SCN-09-02")]
        [TestCase(18.0, "#2E3C8C", "#FF8C5A", "#163A66", "#7C5C7A", "#FFB36B", "#FF6B3D", "#E0A28A", 0.85, Category = "AC-SCN-09-02")]
        [TestCase(20.0, "#0D1A4A", "#4A3A7A", "#0B1E3A", "#2A2A55", "#DDE6FF", "#8FA3FF", "#30335A", 0.50, Category = "AC-SCN-09-02")]
        [TestCase(22.0, "#050A1E", "#121C3A", "#06122A", "#0E1E3C", "#E6EEFF", "#A6B8FF", "#0F1730", 0.35, Category = "AC-SCN-09-02")]
        [TestCase(1.0, "#050A1E", "#121C3A", "#06122A", "#0E1E3C", "#E6EEFF", "#A6B8FF", "#0F1730", 0.35, Category = "AC-SCN-09-02")]
        public void TheKeyframesAreTheLegacyTable(double hour, string skyTop, string skyHorizon, string waterNear, string waterFar,
                                                  string sunColor, string sunGlow, string fog, double ambient)
        {
            // La tabla de AC-SCN-09-02 (texto) contra los Color(0xFF…) del código: exactos.
            var p = SceneTimeOfDay.PaletteFor(hour);
            Assert.That(p.SkyTop, Is.EqualTo(SceneColor.FromHex(skyTop)));
            Assert.That(p.SkyHorizon, Is.EqualTo(SceneColor.FromHex(skyHorizon)));
            Assert.That(p.WaterNear, Is.EqualTo(SceneColor.FromHex(waterNear)));
            Assert.That(p.WaterFar, Is.EqualTo(SceneColor.FromHex(waterFar)));
            Assert.That(p.SunColor, Is.EqualTo(SceneColor.FromHex(sunColor)));
            Assert.That(p.SunGlow, Is.EqualTo(SceneColor.FromHex(sunGlow)));
            Assert.That(p.Fog, Is.EqualTo(SceneColor.FromHex(fog)));
            Assert.That(p.Ambient, Is.EqualTo(ambient));
        }

        [Test, Category("AC-SCN-09-02")]
        public void PaletteMatchesTheOracleEveryMinuteBitForBit()
        {
            var rows = GoldenVectors.CheckRows("scene_palette.json", "minutes", row =>
            {
                var m = (int)row.Int("minute");
                var hour = SceneSeeds.HourOfMinute(m);
                if (BitConverter.DoubleToInt64Bits(hour) != BitConverter.DoubleToInt64Bits(row.F64("hour")))
                {
                    return "la hora del minuto no es la del legacy";
                }

                var p = SceneTimeOfDay.PaletteFor(hour);
                var c = new[] { p.SkyTop, p.SkyHorizon, p.WaterNear, p.WaterFar, p.SunColor, p.SunGlow, p.Fog };
                var names = new[] { "skyTop", "skyHorizon", "waterNear", "waterFar", "sunColor", "sunGlow", "fog" };
                for (var i = 0; i < c.Length; i++)
                {
                    if (!Same(c[i].R, row.F64(names[i] + "R")) || !Same(c[i].G, row.F64(names[i] + "G")) || !Same(c[i].B, row.F64(names[i] + "B")))
                    {
                        return names[i] + " distinto: " + c[i];
                    }
                }

                return Same(p.Ambient, row.F64("ambient")) ? null : "ambient " + SceneGolden.F(p.Ambient);
            });
            Assert.That(rows, Is.EqualTo(1440));
        }

        [Test, Category("AC-SCN-10-01")]
        public void SunElevationAndNightMatchTheOracleEveryMinute()
        {
            var closest = double.MaxValue;
            GoldenVectors.CheckRows("scene_palette.json", "minutes", row =>
            {
                var hour = row.F64("hour");
                var e = SceneTimeOfDay.SunElevation(hour);
                closest = Math.Min(closest, Math.Abs(e + 0.4));
                if (Math.Abs(e - row.F64("sunElevation")) > 1e-12)
                {
                    return "sunElevation " + SceneGolden.F(e);
                }

                return SceneTimeOfDay.IsNight(hour) == (row.Int("isNight") == 1) ? null : "isNight distinto";
            });
            // IsNight es exacto porque ninguna muestra cae cerca del umbral: un último bit distinto no lo puede dar vuelta.
            Assert.That(closest, Is.GreaterThan(1e-6));
        }

        [Test, Category("AC-SCN-10-02"), Category("AC-SCN-10-03")]
        public void TheLegacyLightMatchesTheOracleEveryMinuteInSevenSizes()
        {
            var rows = GoldenVectors.CheckRows("scene_light.json", "minutes", row =>
            {
                var hour = row.F64("hour");
                foreach (var s in SceneSeeds.Sizes)
                {
                    var l = SceneTimeOfDay.LightFor(hour, new SceneCamera(s.Width, s.Height));
                    if (Math.Abs(l.X - row.F64("x" + s.Name)) > 1e-9 || Math.Abs(l.Y - row.F64("y" + s.Name)) > 1e-9)
                    {
                        return s.Name + ": (" + SceneGolden.F(l.X) + "; " + SceneGolden.F(l.Y) + ")";
                    }

                    if (Math.Abs(l.Strength - row.F64("strength")) > 1e-12 || l.IsMoon != (row.Int("isMoon") == 1))
                    {
                        return "fuerza o luna distintas";
                    }
                }

                return null;
            });
            Assert.That(rows, Is.EqualTo(1440));
        }

        [TestCase(5.0, 0.208, 0.0, false, Category = "AC-SCN-10-02")]
        [TestCase(9.0, 0.375, 0.707, false, Category = "AC-SCN-10-02")]
        [TestCase(12.0, 0.500, 1.0, false, Category = "AC-SCN-10-02")]
        [TestCase(18.0, 0.750, 0.0, false, Category = "AC-SCN-10-02")]
        [TestCase(19.0, 0.792, 0.0, false, Category = "AC-SCN-10-02")]
        [TestCase(20.0, 0.333, 0.5, true, Category = "AC-SCN-10-02")]
        [TestCase(0.0, 0.500, 1.0, true, Category = "AC-SCN-10-02")]
        [TestCase(4.0, 0.667, 0.5, true, Category = "AC-SCN-10-02")]
        public void TheLegacyLightTable(double hour, double fraction, double elevation, bool moon)
        {
            var l = SceneTimeOfDay.LightFor(hour, V390);
            Assert.That(l.XFraction, Is.EqualTo(fraction).Within(0.0005));
            Assert.That(l.Elevation, Is.EqualTo(elevation).Within(0.0005));
            Assert.That(l.IsMoon, Is.EqualTo(moon));
            Assert.That(l.X, Is.EqualTo(V390.Width * l.XFraction).Within(1e-9));
        }

        [TestCase(5.0, 0.2, Category = "AC-SCN-10-03")]
        [TestCase(9.0, 0.8485, Category = "AC-SCN-10-03")]
        [TestCase(12.0, 1.0, Category = "AC-SCN-10-03")]
        [TestCase(18.0, 0.2, Category = "AC-SCN-10-03")]
        [TestCase(22.0, 0.45, Category = "AC-SCN-10-03")]
        [TestCase(2.0, 0.45, Category = "AC-SCN-10-03")]
        public void TheLegacyStrength(double hour, double strength)
        {
            Assert.That(SceneTimeOfDay.LightFor(hour, V390).Strength, Is.EqualTo(strength).Within(0.0005));
        }

        [Test, Category("AC-SCN-10-01")]
        public void NightThresholds()
        {
            Assert.That(SceneTimeOfDay.NightStartHour, Is.EqualTo(19 + 34 / 60.0 + 19 / 3600.0).Within(1.0 / 3600));
            Assert.That(SceneTimeOfDay.NightEndHour, Is.EqualTo(4 + 25 / 60.0 + 41 / 3600.0).Within(1.0 / 3600));
            Assert.That(SceneTimeOfDay.IsNight(SceneTimeOfDay.NightStartHour + 1e-7), Is.True);
            Assert.That(SceneTimeOfDay.IsNight(SceneTimeOfDay.NightStartHour - 1e-7), Is.False);
            Assert.That(SceneTimeOfDay.IsNight(SceneTimeOfDay.NightEndHour - 1e-7), Is.True);
            Assert.That(SceneTimeOfDay.IsNight(SceneTimeOfDay.NightEndHour + 1e-7), Is.False);
            Assert.That(SceneTimeOfDay.IsNight(19 + 34 / 60.0), Is.False, "a las 19:34, todavía de día (QA-6a-PAR-07)");
            Assert.That(SceneTimeOfDay.IsNight(19 + 35 / 60.0), Is.True);
        }

        [Test, Category("AC-SCN-09-02")]
        public void ColorLerpClampsLikeFlutter()
        {
            // QA-6a-PAR-06: clampDouble(a·(1 − t) + b·t; 0; 1) por canal, también con t fuera de [0; 1].
            var a = SceneColor.FromHex("#202020");
            var b = SceneColor.FromHex("#F0F0F0");
            var low = SceneColor.Lerp(a, b, -0.5);
            var high = SceneColor.Lerp(a, b, 1.5);
            Assert.That(low.R, Is.EqualTo(0.0), "a·1,5 + b·(−0,5) < 0 → 0");
            Assert.That(high.R, Is.EqualTo(1.0), "> 1 → 1");
            var c = SceneColor.FromHex("#80FF00");
            var d = SceneColor.FromHex("#FF00FF");
            Assert.That(SceneColor.Lerp(c, d, 1.5).G, Is.EqualTo(0.0));
            Assert.That(SceneColor.Lerp(c, d, -0.5).G, Is.EqualTo(1.0));
            Assert.That(SceneColor.Lerp(c, d, 0.25).R, Is.EqualTo(0x80 / 255.0 * 0.75 + 1.0 * 0.25));
            Assert.That(SceneColor.FromHex("#FF8C5A").R, Is.EqualTo(1.0));
            Assert.That(SceneColor.FromHex("#FF8C5A").G, Is.EqualTo(0x8C / 255.0), "byte/255,0 sin redondear");
            Assert.That(SceneColor.FromArgb(0xFF0B4FA8), Is.EqualTo(SceneColor.FromHex("#0B4FA8")));
            Assert.That(SceneColor.FromHex("#0B4FA8").ToHex(), Is.EqualTo("#0B4FA8"));
            Assert.Throws<FormatException>(() => SceneColor.FromHex("0B4FA8"));
        }

        [Test, Category("AC-SCN-09-02")]
        public void TheClampDoesNotChangeTheLegacyPalette()
        {
            // Barrido de QA (1 440 minutos × 7 colores): con la paleta del legacy el recorte no cambia ningún valor.
            for (var m = 0; m < 1440; m++)
            {
                var p = SceneTimeOfDay.PaletteFor(SceneSeeds.HourOfMinute(m));
                foreach (var c in new[] { p.SkyTop, p.SkyHorizon, p.WaterNear, p.WaterFar, p.SunColor, p.SunGlow, p.Fog })
                {
                    Assert.That(c.R >= 0 && c.R <= 1 && c.G >= 0 && c.G <= 1 && c.B >= 0 && c.B <= 1, Is.True);
                }
            }
        }

        private static bool Same(double a, double b)
        {
            return BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
        }
    }
}
