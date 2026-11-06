using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;

namespace RowMate.Tests.EditMode.Scene
{
    // HLT · La luz y el post de cada hora (06a-escena-lago/02-tech-design §2.7; AC-SCN-09-04, -05, AC-WKT-13-12; DT6a-10, -19;
    // 04-design-notes §10). Los valores de la tabla salen de HourLookTable (del Designer), nunca de un literal salvo los de la spec.
    [TestFixture]
    public sealed class HourLightingTests
    {
        private static HourLook Look(double hour)
        {
            var p = SceneTimeOfDay.PaletteFor(hour);
            var sky = CelestialPlacement.Evaluate(hour, 0.94, CelestialSpec.Default);
            return HourLighting.Evaluate(hour, p, sky, HourLookTable.Default);
        }

        [Test, Category("AC-SCN-09-05")]
        public void AtTheKeyframesTheTableValues()
        {
            var table = HourLookTable.Default;
            for (var i = 0; i < table.Count; i++)
            {
                var key = table[i];
                var hour = key.Hour % 24;
                var look = Look(hour);
                Assert.That(look.ExposureEv, Is.EqualTo(key.ExposureEv).Within(1e-12), "exposición a las " + hour);
                Assert.That(look.WhiteBalanceTemp, Is.EqualTo(key.WhiteBalanceTemp).Within(1e-12));
                Assert.That(look.WhiteBalanceTint, Is.EqualTo(key.WhiteBalanceTint).Within(1e-12));
                Assert.That(look.Saturation, Is.EqualTo(key.Saturation).Within(1e-12));
                Assert.That(look.Contrast, Is.EqualTo(key.Contrast).Within(1e-12));
                Assert.That(look.BloomIntensity, Is.EqualTo(key.BloomIntensity).Within(1e-12));
                Assert.That(look.BloomThreshold, Is.EqualTo(key.BloomThreshold).Within(1e-12));
                Assert.That(look.ShadowStrengthMedium, Is.EqualTo(key.ShadowMedium).Within(1e-12));
                Assert.That(look.ShadowStrengthHigh, Is.EqualTo(key.ShadowHigh).Within(1e-12));
            }
        }

        [Test, Category("AC-SCN-09-05")]
        public void BetweenKeyframesItIsLinear()
        {
            var a = HourLookTable.Default.Evaluate(12);
            var b = HourLookTable.Default.Evaluate(18);
            var mid = Look(15);
            Assert.That(mid.ExposureEv, Is.EqualTo((a.ExposureEv + b.ExposureEv) / 2).Within(1e-12));
            Assert.That(mid.WhiteBalanceTemp, Is.EqualTo((a.WhiteBalanceTemp + b.WhiteBalanceTemp) / 2).Within(1e-12));
            Assert.That(mid.ShadowStrengthMedium, Is.EqualTo((a.ShadowMedium + b.ShadowMedium) / 2).Within(1e-12));
            // La noche cerrada de 22 a 4, como la paleta.
            Assert.That(Look(2).ExposureEv, Is.EqualTo(Look(22).ExposureEv).Within(1e-12));
            Assert.That(Look(4.5).ExposureEv, Is.EqualTo((Look(4).ExposureEv + Look(5).ExposureEv) / 2).Within(1e-12));
        }

        [Test, Category("AC-SCN-09-05")]
        public void TheTableOfTheGate()
        {
            var t = HourLookTable.Default;
            Assert.That(t.Evaluate(18).WhiteBalanceTemp, Is.EqualTo(15), "la tarde con +15 / +4 (gate)");
            Assert.That(t.Evaluate(18).WhiteBalanceTint, Is.EqualTo(4));
            Assert.That(t.Evaluate(22).WhiteBalanceTemp, Is.EqualTo(-10), "la noche con −10 / −2 y −10 de saturación");
            Assert.That(t.Evaluate(22).WhiteBalanceTint, Is.EqualTo(-2));
            Assert.That(t.Evaluate(22).Saturation, Is.EqualTo(-10));
            Assert.That(t.Evaluate(12).ShadowMedium, Is.EqualTo(0.85));
            Assert.That(HourLookTable.VignetteIntensity, Is.EqualTo(0.18));
        }

        [Test, Category("AC-SCN-09-04")]
        public void OnePaletteRulesTheLight()
        {
            foreach (var hour in new[] { 6.0, 12.0, 18.0, 22.0 })
            {
                var p = SceneTimeOfDay.PaletteFor(hour);
                var look = Look(hour);
                Assert.That(look.FogColor, Is.EqualTo(p.Fog));
                var max = Math.Max(p.SunColor.R, Math.Max(p.SunColor.G, p.SunColor.B));
                Assert.That(look.KeyColor.R, Is.EqualTo(p.SunColor.R / max).Within(1e-15), "el color de la luz es el sunColor de la paleta");
                Assert.That(look.KeyIntensity, Is.EqualTo(CelestialPlacement.Evaluate(hour, 0.94, CelestialSpec.Default).LightIntensity));
                // El ambiente: el tinte del legacy (a; a; mín(1; a + 0,12)) sobre el matiz del cielo (con luminancia 1), llevado hacia el
                // blanco con HourLookTable.AmbientHue (WP-D2).
                var a = p.Ambient;
                var lum = 0.2126 * p.SkyTop.R + 0.7152 * p.SkyTop.G + 0.0722 * p.SkyTop.B;
                var h = HourLookTable.AmbientHue;
                Assert.That(look.AmbientSky.R, Is.EqualTo(a * HourLookTable.AmbientScale * (1 - h + h * p.SkyTop.R / lum)).Within(1e-12));
                Assert.That(look.AmbientSky.B,
                    Is.EqualTo(Math.Min(1, a + 0.12) * HourLookTable.AmbientScale * (1 - h + h * p.SkyTop.B / lum)).Within(1e-12));
            }
        }

        // WP-D2: el ambiente es frío a la sombra pero no cambia el color de lo que ilumina. A las 12 h, con el cielo más saturado del día,
        // ningún canal del ambiente se aparta más del 45 % (60 % el azul, que de noche suma el tinte del legacy) del gris (antes, con el matiz entero, el rojo quedaba en 0,16 y el
        // azul en 2,4: la cabaña de CAP-02 salía azul marino).
        [Test, Category("AC-SCN-09-04")]
        public void TheAmbientKeepsTheColorOfWhatItLights()
        {
            foreach (var hour in new[] { 6.0, 12.0, 18.0, 22.0 })
            {
                var look = Look(hour);
                foreach (var c in new[] { look.AmbientSky, look.AmbientEquator, look.AmbientGround })
                {
                    var mean = (c.R + c.G + c.B) / 3;
                    Assert.That(c.R / mean, Is.InRange(0.55, 1.45), "rojo a las " + hour);
                    Assert.That(c.B / mean, Is.InRange(0.55, 1.6), "azul a las " + hour);
                }
            }
        }

        [TestCase(6.0, 1.0, Category = "AC-SCN-09-05")]
        [TestCase(18.0, 1.0, Category = "AC-SCN-09-05")]
        [TestCase(7.0, 0.70, Category = "AC-SCN-09-05")]
        [TestCase(12.0, 0.0, Category = "AC-SCN-09-05")]
        [TestCase(10.0, 0.0, Category = "AC-SCN-09-05")]
        [TestCase(14.0, 0.0, Category = "AC-SCN-09-05")]
        [TestCase(5.0, 0.54, Category = "AC-SCN-09-05")]
        [TestCase(19.0, 0.54, Category = "AC-SCN-09-05")]
        [TestCase(22.0, 0.0, Category = "AC-SCN-09-05")]
        public void TheGoldenHour(double hour, double golden)
        {
            Assert.That(Look(hour).Golden, Is.EqualTo(golden).Within(0.01));
        }

        [TestCase(4.5, 0.0, Category = "AC-SCN-15-07")]
        [TestCase(5.0, 0.0, Category = "AC-SCN-15-07")]
        [TestCase(5.5, 0.275, Category = "AC-SCN-15-07")]
        [TestCase(6.0, 0.55, Category = "AC-SCN-15-07")]
        [TestCase(6.5, 0.55, Category = "AC-SCN-15-07")]
        [TestCase(7.0, 0.55, Category = "AC-SCN-15-07")]
        [TestCase(9.0, 0.0572, Category = "AC-SCN-15-07")]
        [TestCase(9.5, 0.0, Category = "AC-SCN-15-07")]
        [TestCase(12.0, 0.0, Category = "AC-SCN-15-07")]
        public void TheLowMorningMist(double hour, double mist)
        {
            Assert.That(Look(hour).LowMist, Is.EqualTo(mist).Within(0.001));
        }

        [Test, Category("AC-WKT-13-12")]
        public void ThePauseVeilComposesInOneVolume()
        {
            // Saturación × (1 − 0,3·w) y exposición + w·VeilEv (−0,50 de partida, DN6a-19; WP-D2 lo bajó a −0,17 con la sonda de CAP-20:
            // el canal máximo del cielo × 0,85 ± 0,03, que con la saturación × 0,7 ya cae ≈ 9 % en el azul del cielo).
            var table = HourLookTable.Default;
            Assert.That(table.VeilEv, Is.LessThan(0).And.GreaterThanOrEqualTo(-0.5));
            Assert.That(table.VeilSaturationLoss, Is.EqualTo(0.30));
            foreach (var hour in new[] { 12.0, 22.0 })
            {
                var look = Look(hour);
                var none = HourLighting.Compose(look, 0, table.VeilEv, table.VeilSaturationLoss);
                var full = HourLighting.Compose(look, 1, table.VeilEv, table.VeilSaturationLoss);
                Assert.That(none.ExposureEv, Is.EqualTo(look.ExposureEv));
                Assert.That(none.Saturation, Is.EqualTo(look.Saturation).Within(1e-9));
                Assert.That(full.ExposureEv, Is.EqualTo(look.ExposureEv + table.VeilEv).Within(1e-12));
                Assert.That(full.SaturationFactor, Is.EqualTo(none.SaturationFactor * 0.7).Within(1e-12),
                    "×0,7 también de noche (un segundo Volume desaturaría menos: DT6a-10)");
                var half = HourLighting.Compose(look, 0.5, table.VeilEv, table.VeilSaturationLoss);
                Assert.That(half.ExposureEv, Is.EqualTo(look.ExposureEv + 0.5 * table.VeilEv).Within(1e-12));
                Assert.That(full.VeilWeight, Is.EqualTo(1));
                Assert.That(HourLighting.Compose(look, 3, table.VeilEv).VeilWeight, Is.EqualTo(1), "el peso se recorta a [0; 1]");
            }
        }

        [Test, Category("AC-SCN-09-05")]
        public void NightShadowsGoToZeroInMediumWithoutTurningOffTheMap()
        {
            // DT6a-19: la fuerza baja a 0 (las palabras clave de las sombras quedan constantes en la sesión); en Alto, 0,25.
            var night = Look(23);
            Assert.That(night.ShadowStrength(false), Is.EqualTo(0));
            Assert.That(night.ShadowStrength(true), Is.EqualTo(0.25));
            Assert.That(Look(12).ShadowStrength(false), Is.EqualTo(0.85));
            for (var m = 0; m < 1440; m += 5)
            {
                var look = Look(m / 60.0);
                Assert.That(look.ShadowStrengthMedium, Is.InRange(0.0, 1.0));
                Assert.That(look.ShadowStrengthHigh, Is.GreaterThanOrEqualTo(look.ShadowStrengthMedium));
            }
        }

        [Test, Category("AC-SCN-09-05")]
        public void TheLightNeverJumpsMinuteToMinute()
        {
            var prev = Look(0);
            for (var m = 1; m < 1440; m++)
            {
                var look = Look(m / 60.0);
                Assert.That(Math.Abs(look.ExposureEv - prev.ExposureEv), Is.LessThan(0.02));
                Assert.That(Math.Abs(look.KeyIntensity - prev.KeyIntensity), Is.LessThan(0.05));
                Assert.That(Math.Abs(look.Golden - prev.Golden), Is.LessThan(0.05));
                prev = look;
            }
        }
    }
}
