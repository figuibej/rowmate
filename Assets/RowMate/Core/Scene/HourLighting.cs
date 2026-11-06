using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // La luz y el post de una hora, todo derivado de la paleta y del astro (AC-SCN-09-04: una sola paleta manda;
    // 06a-escena-lago/02-tech-design §2.7; 04-design-notes §10).
    public readonly struct HourLook
    {
        public HourLook(in SceneColor keyColor, double keyIntensity, in SceneColor ambientSky, in SceneColor ambientEquator,
                        in SceneColor ambientGround, double shadowMedium, double shadowHigh, in SceneColor fogColor, double fogStart,
                        double fogEnd, in HourLookKey post, double golden, double lowMist)
        {
            KeyColor = keyColor;
            KeyIntensity = keyIntensity;
            AmbientSky = ambientSky;
            AmbientEquator = ambientEquator;
            AmbientGround = ambientGround;
            ShadowStrengthMedium = shadowMedium;
            ShadowStrengthHigh = shadowHigh;
            FogColor = fogColor;
            FogStart = fogStart;
            FogEnd = fogEnd;
            ExposureEv = post.ExposureEv;
            WhiteBalanceTemp = post.WhiteBalanceTemp;
            WhiteBalanceTint = post.WhiteBalanceTint;
            Saturation = post.Saturation;
            Contrast = post.Contrast;
            BloomIntensity = post.BloomIntensity;
            BloomThreshold = post.BloomThreshold;
            Golden = golden;
            LowMist = lowMist;
        }

        // sunColor normalizado (su canal más alto en 1) y la intensidad del astro (CelestialPlacement).
        public SceneColor KeyColor { get; }
        public double KeyIntensity { get; }

        // El ambiente Trilight de URP para lo de terceros: T(a) ⊙ el matiz de skyTop, skyHorizon y waterNear (luminancia 1) × 0,65.
        public SceneColor AmbientSky { get; }
        public SceneColor AmbientEquator { get; }
        public SceneColor AmbientGround { get; }

        // La fuerza de las sombras en Medio y en Alto (de noche 0 en Medio sin apagar el mapa: DT6a-19).
        public double ShadowStrengthMedium { get; }
        public double ShadowStrengthHigh { get; }

        public SceneColor FogColor { get; }
        public double FogStart { get; }
        public double FogEnd { get; }
        public double ExposureEv { get; }
        public double WhiteBalanceTemp { get; }
        public double WhiteBalanceTint { get; }
        public double Saturation { get; }
        public double Contrast { get; }
        public double BloomIntensity { get; }
        public double BloomThreshold { get; }

        // g: 1 a las 6 y a las 18, 0 de 9 a 15 (00-deuda-diseno §4.2).
        public double Golden { get; }

        // La bruma baja m(h): 0 a las 5, 0,55 de 6 a 7, 0 desde las 9:30 (AC-SCN-15-07).
        public double LowMist { get; }

        public double ShadowStrength(bool highTier)
        {
            return highTier ? ShadowStrengthHigh : ShadowStrengthMedium;
        }
    }

    // Lo que va al único Volume de la escena: la luz de la hora con el velo de pausa compuesto (DT6a-10).
    public readonly struct PostLook
    {
        public PostLook(double exposureEv, double whiteBalanceTemp, double whiteBalanceTint, double saturation, double contrast,
                        double bloomIntensity, double bloomThreshold, double vignetteIntensity, double vignetteSmoothness, double veilWeight)
        {
            ExposureEv = exposureEv;
            WhiteBalanceTemp = whiteBalanceTemp;
            WhiteBalanceTint = whiteBalanceTint;
            Saturation = saturation;
            Contrast = contrast;
            BloomIntensity = bloomIntensity;
            BloomThreshold = bloomThreshold;
            VignetteIntensity = vignetteIntensity;
            VignetteSmoothness = vignetteSmoothness;
            VeilWeight = veilWeight;
        }

        public double ExposureEv { get; }
        public double WhiteBalanceTemp { get; }
        public double WhiteBalanceTint { get; }

        // En las unidades de URP (−100 a 100): el factor de saturación es 1 + Saturation/100.
        public double Saturation { get; }
        public double Contrast { get; }
        public double BloomIntensity { get; }
        public double BloomThreshold { get; }
        public double VignetteIntensity { get; }
        public double VignetteSmoothness { get; }
        public double VeilWeight { get; }

        public double SaturationFactor => 1 + Saturation / 100;
    }

    public static class HourLighting
    {
        public static HourLook Evaluate(double hour, in ScenePalette palette, in CelestialState sky, HourLookTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var key = table.Evaluate(hour);
            var a = palette.Ambient;
            var tint = new SceneColor(a, a, Math.Min(1.0, a + 0.12));
            return new HourLook(
                Normalized(palette.SunColor),
                sky.LightIntensity,
                AmbientOf(tint, palette.SkyTop),
                AmbientOf(tint, palette.SkyHorizon),
                AmbientOf(tint, palette.WaterNear),
                key.ShadowMedium,
                key.ShadowHigh,
                palette.Fog,
                HourLookTable.ThirdPartyFogStart,
                HourLookTable.ThirdPartyFogEnd,
                key,
                Golden(sky),
                LowMist(hour));
        }

        // El velo de pausa sobre la luz de la hora, en un solo Volume: saturación × (1 − 0,3·w) (multiplicativa, así de noche
        // desatura lo mismo que de día) y exposición + w·VeilEv (AC-WKT-13-12).
        public static PostLook Compose(in HourLook look, double veilWeight, double veilEv, double veilSaturationLoss = 0.30)
        {
            var w = SceneMath.Clamp01(veilWeight);
            var factor = (1 + look.Saturation / 100) * (1 - veilSaturationLoss * w);
            return new PostLook(look.ExposureEv + w * veilEv, look.WhiteBalanceTemp, look.WhiteBalanceTint, factor * 100 - 100, look.Contrast,
                look.BloomIntensity, look.BloomThreshold, HourLookTable.VignetteIntensity, HourLookTable.VignetteSmoothness, w);
        }

        // g = (1 − S(1,5°; 9°; elevación del disco del sol))·vis_sol; 0 de noche.
        public static double Golden(in CelestialState sky)
        {
            if (sky.DiskIsMoon)
            {
                return 0;
            }

            return (1 - SceneMath.SmoothStep(SkyArt.GoldenLowDeg, SkyArt.GoldenHighDeg, sky.DiskElevationDeg)) * sky.DiskVisibility;
        }

        public static double LowMist(double hour)
        {
            var h = hour % 24;
            if (h < 0)
            {
                h += 24;
            }

            if (h < HourLookTable.MistRiseStart || h >= HourLookTable.MistFallEnd)
            {
                return 0;
            }

            if (h < HourLookTable.MistRiseEnd)
            {
                return HourLookTable.MistPeak * SceneMath.SmoothStep(HourLookTable.MistRiseStart, HourLookTable.MistRiseEnd, h);
            }

            if (h <= HourLookTable.MistFallStart)
            {
                return HourLookTable.MistPeak;
            }

            return HourLookTable.MistPeak * (1 - SceneMath.SmoothStep(HourLookTable.MistFallStart, HourLookTable.MistFallEnd, h));
        }

        // El matiz de un color con luminancia (sRGB) 1, llevado hacia el blanco (HourLookTable.AmbientHue) y multiplicado por el tinte y
        // por 0,65.
        private static SceneColor AmbientOf(in SceneColor tint, in SceneColor color)
        {
            var luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
            var k = luminance > 1e-9 ? 1 / luminance : 0;
            var h = HourLookTable.AmbientHue;
            var s = HourLookTable.AmbientScale;
            return new SceneColor(
                tint.R * s * (1 - h + h * color.R * k),
                tint.G * s * (1 - h + h * color.G * k),
                tint.B * s * (1 - h + h * color.B * k));
        }

        private static SceneColor Normalized(in SceneColor c)
        {
            var max = Math.Max(c.R, Math.Max(c.G, c.B));
            return max > 0 ? new SceneColor(c.R / max, c.G / max, c.B / max) : c;
        }
    }
}
