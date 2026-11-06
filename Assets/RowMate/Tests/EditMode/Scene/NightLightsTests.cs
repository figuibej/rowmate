using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;

namespace RowMate.Tests.EditMode.Scene
{
    // NLT · Las luces de la noche (shore_painter.dart:26, :121-122; AC-SCN-09-06, AC-SCN-20-02; DV-60).
    [TestFixture]
    public sealed class NightLightsTests
    {
        [Test, Category("AC-SCN-09-06"), Category("AC-SCN-20-02")]
        public void TheLegacyThreshold()
        {
            Assert.That(NightLights.IsNightLegacy(0.59), Is.True);
            Assert.That(NightLights.IsNightLegacy(0.6), Is.False);
            Assert.That(NightLights.IsNightLegacy(0.35), Is.True);
            // Ambiente 0,6 a las 19:25:43 y a las 4:42:51 (≈ 19:26 y ≈ 4:43).
            Assert.That(NightLights.DuskThresholdHour, Is.EqualTo(19 + 25 / 60.0 + 43 / 3600.0).Within(1.0 / 3600));
            Assert.That(NightLights.DawnThresholdHour, Is.EqualTo(4 + 42 / 60.0 + 51 / 3600.0).Within(1.0 / 3600));
            Assert.That(SceneTimeOfDay.PaletteFor(NightLights.DuskThresholdHour).Ambient, Is.EqualTo(0.6).Within(1e-12));
            Assert.That(SceneTimeOfDay.PaletteFor(NightLights.DawnThresholdHour).Ambient, Is.EqualTo(0.6).Within(1e-12));
        }

        [Test, Category("AC-SCN-09-06")]
        public void OutsideTheWindowItIsTheLegacy()
        {
            for (var m = 0; m < 1440; m++)
            {
                var hour = m / 60.0;
                if (Math.Abs(hour - NightLights.DuskThresholdHour) <= 10.5 / 60 || Math.Abs(hour - NightLights.DawnThresholdHour) <= 10.5 / 60)
                {
                    continue;
                }

                var ambient = SceneTimeOfDay.PaletteFor(hour).Ambient;
                for (var seed = 0; seed < 50; seed++)
                {
                    Assert.That(NightLights.IsLit(seed, hour, ambient), Is.EqualTo(NightLights.IsNightLegacy(ambient)), "a las " + hour);
                }
            }
        }

        [Test, Category("AC-SCN-09-06")]
        public void EachLightInItsOwnMinuteWithinTenOfTheThreshold()
        {
            var duskMinutes = new HashSet<int>();
            var dawnMinutes = new HashSet<int>();
            for (var seed = 7; seed < 7 + 12 * 200; seed += 12)
            {
                var dusk = NightLights.DuskMinute(seed);
                var dawn = NightLights.DawnMinute(seed);
                Assert.That(dusk, Is.InRange(19 * 60 + 16, 19 * 60 + 35), "≈ 19:16 a 19:36");
                Assert.That(dawn, Is.InRange(4 * 60 + 33, 4 * 60 + 52), "≈ 4:33 a 4:53");
                duskMinutes.Add(dusk);
                dawnMinutes.Add(dawn);
                // Encendida desde su minuto (al anochecer), apagada desde su minuto (al amanecer); fija para esa luz.
                Assert.That(NightLights.IsLit(seed, (dusk - 0.5) / 60.0, Ambient((dusk - 0.5) / 60.0)), Is.False);
                Assert.That(NightLights.IsLit(seed, dusk / 60.0, Ambient(dusk / 60.0)), Is.True);
                Assert.That(NightLights.IsLit(seed, (dawn - 0.5) / 60.0, Ambient((dawn - 0.5) / 60.0)), Is.True);
                Assert.That(NightLights.IsLit(seed, dawn / 60.0, Ambient(dawn / 60.0)), Is.False);
            }

            Assert.That(duskMinutes.Count, Is.GreaterThan(10), "las luces no se encienden todas juntas");
            Assert.That(dawnMinutes.Count, Is.GreaterThan(10));
        }

        [Test, Category("AC-SCN-09-06")]
        public void NoJumpAtTheEdgesOfTheWindow()
        {
            for (var seed = 0; seed < 300; seed++)
            {
                for (var s = -720; s <= 720; s += 1)
                {
                    var hour = NightLights.DuskThresholdHour + s / 3600.0;
                    var lit = NightLights.IsLit(seed, hour, SceneTimeOfDay.PaletteFor(hour).Ambient);
                    var later = NightLights.IsLit(seed, hour + 1 / 3600.0, SceneTimeOfDay.PaletteFor(hour + 1 / 3600.0).Ambient);
                    Assert.That(!lit || later, Is.True, "al anochecer, una vez encendida no se apaga");
                    var dawnHour = NightLights.DawnThresholdHour + s / 3600.0;
                    var on = NightLights.IsLit(seed, dawnHour, SceneTimeOfDay.PaletteFor(dawnHour).Ambient);
                    var onLater = NightLights.IsLit(seed, dawnHour + 1 / 3600.0, SceneTimeOfDay.PaletteFor(dawnHour + 1 / 3600.0).Ambient);
                    Assert.That(on || !onLater, Is.True, "al amanecer, una vez apagada no se enciende");
                }
            }
        }

        [Test, Category("AC-SCN-09-06")]
        public void TheFadeTakesFourTenths()
        {
            var f = new NightLightFader();
            Assert.That(f.Level, Is.EqualTo(0));
            for (var i = 0; i < 12; i++)
            {
                f.Step(true, 1.0 / 60);
            }

            Assert.That(f.Level, Is.EqualTo(0.5).Within(1e-9), "a los 0,2 s, la mitad");
            for (var i = 0; i < 12; i++)
            {
                f.Step(true, 1.0 / 60);
            }

            Assert.That(f.Level, Is.EqualTo(1).Within(1e-9));
            f.Step(false, 0.1);
            Assert.That(f.Level, Is.EqualTo(0.75).Within(1e-9), "y se apaga igual");
            f.Snap(false);
            Assert.That(f.Level, Is.EqualTo(0));
            Assert.That(NightLightFader.Seconds, Is.EqualTo(0.4));
        }

        private static double Ambient(double hour)
        {
            return SceneTimeOfDay.PaletteFor(hour).Ambient;
        }
    }
}
