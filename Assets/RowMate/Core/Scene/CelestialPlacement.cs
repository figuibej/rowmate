using System;
using RowMate.Core.Scene.Art;
using RowMate.Core.Text;

namespace RowMate.Core.Scene
{
    // Dónde va el astro que se ve y su luz (06a-escena-lago/02-tech-design §2.6; D-ART-05, Q-63; DV-52, DV-54). Los valores por
    // defecto viven en SkyArt (del Designer desde WP-D1).
    public readonly struct CelestialSpec
    {
        public CelestialSpec(double arcDeg, double baseDeg, double hiddenDeg, double azimuthSpanDeg, double lightMinIntensity,
                             double maxDegPerMinute, double sunRiseStart, double sunRiseEnd, double sunSetStart, double sunSetEnd,
                             double moonRiseEnd, double moonSetEnd)
        {
            ArcDeg = arcDeg;
            BaseDeg = baseDeg;
            HiddenDeg = hiddenDeg;
            AzimuthSpanDeg = azimuthSpanDeg;
            LightMinIntensity = lightMinIntensity;
            MaxDegPerMinute = maxDegPerMinute;
            SunRiseStart = sunRiseStart;
            SunRiseEnd = sunRiseEnd;
            SunSetStart = sunSetStart;
            SunSetEnd = sunSetEnd;
            MoonRiseEnd = moonRiseEnd;
            MoonSetEnd = moonSetEnd;
        }

        public static CelestialSpec Default => new CelestialSpec(SkyArt.ArcDeg, SkyArt.BaseDeg, SkyArt.HiddenDeg, SkyArt.AzimuthSpanDeg,
            SkyArt.LightMinIntensity, SkyArt.MaxDegPerMinute, SkyArt.SunRiseStart, SkyArt.SunRiseEnd, SkyArt.SunSetStart, SkyArt.SunSetEnd,
            SkyArt.MoonRiseEnd, SkyArt.MoonSetEnd);

        public double ArcDeg { get; }
        public double BaseDeg { get; }
        public double HiddenDeg { get; }
        public double AzimuthSpanDeg { get; }
        public double LightMinIntensity { get; }
        public double MaxDegPerMinute { get; }
        public double SunRiseStart { get; }
        public double SunRiseEnd { get; }
        public double SunSetStart { get; }
        public double SunSetEnd { get; }
        public double MoonRiseEnd { get; }
        public double MoonSetEnd { get; }
    }

    // El astro de una hora: el disco (acimut, elevación, visibilidad; 0 detrás del paisaje) y la luz principal (mismo acimut,
    // elevación propia, intensidad), y la fuerza del legacy (la de las líneas del agua, AC-SCN-10-03).
    public readonly struct CelestialState
    {
        public CelestialState(bool isNight, double diskAzimuthDeg, double diskElevationDeg, double diskVisibility, bool diskIsMoon,
                              double lightAzimuthDeg, double lightElevationDeg, double lightIntensity, double legacyStrength, double elevation)
        {
            IsNight = isNight;
            DiskAzimuthDeg = diskAzimuthDeg;
            DiskElevationDeg = diskElevationDeg;
            DiskVisibility = diskVisibility;
            DiskIsMoon = diskIsMoon;
            LightAzimuthDeg = lightAzimuthDeg;
            LightElevationDeg = lightElevationDeg;
            LightIntensity = lightIntensity;
            LegacyStrength = legacyStrength;
            Elevation = elevation;
        }

        public bool IsNight { get; }
        public double DiskAzimuthDeg { get; }
        public double DiskElevationDeg { get; }
        public double DiskVisibility { get; }
        public bool DiskIsMoon { get; }
        public double LightAzimuthDeg { get; }
        public double LightElevationDeg { get; }
        public double LightIntensity { get; }
        public double LegacyStrength { get; }

        // e = clamp(sin((h − 6)/12·π); 0; 1) de la hora (o de la espejada de noche).
        public double Elevation { get; }
    }

    public static class CelestialPlacement
    {
        public static CelestialState Evaluate(double hour, double moonIllumination, in CelestialSpec spec)
        {
            var h = DartMath.Mod(hour, 24);
            var night = SceneTimeOfDay.IsNight(h);
            var hm = night ? DartMath.Mod(h + 12, 24) : h;
            var e = SceneTimeOfDay.Clamp(Math.Sin((hm - 6) / 12 * Math.PI), 0.0, 1.0);
            var fx = SceneTimeOfDay.Clamp(0.25 + 0.5 * (hm - 6) / 12, 0.15, 0.85);
            var azimuth = (fx - 0.5) * spec.AzimuthSpanDeg;
            double vis;
            double lightElevation;
            double intensity;
            if (!night)
            {
                vis = SceneMath.SmoothStep(spec.SunRiseStart, spec.SunRiseEnd, h) * (1 - SceneMath.SmoothStep(spec.SunSetStart, spec.SunSetEnd, h));
                lightElevation = SkyArt.SunLightBaseDeg + SkyArt.SunLightArcDeg * e;
                intensity = SunIntensity(h) * vis;
            }
            else
            {
                vis = h >= 12
                    ? SceneMath.SmoothStep(SceneTimeOfDay.NightStartHour, spec.MoonRiseEnd, h)
                    : SceneMath.SmoothStep(SceneTimeOfDay.NightEndHour, spec.MoonSetEnd, h);
                lightElevation = SkyArt.MoonLightBaseDeg + SkyArt.MoonLightArcDeg * e;
                intensity = (SkyArt.MoonLightBase + SkyArt.MoonLightPerIllumination * moonIllumination) * vis;
            }

            intensity = Math.Max(intensity, spec.LightMinIntensity);
            var diskElevation = spec.HiddenDeg + (spec.BaseDeg + spec.ArcDeg * e - spec.HiddenDeg) * vis;
            var legacyStrength = night ? 0.45 : SceneTimeOfDay.Clamp(e * 1.2, 0.2, 1.0);
            return new CelestialState(night, azimuth, diskElevation, vis, night, azimuth, lightElevation, intensity, legacyStrength, e);
        }

        // K(h) del sol (04-design-notes §3.2): 0,55 hasta las 5, 1,25 a las 7, 1,75 a las 12 y 0,85 desde las 18, lineal entre ellos.
        public static double SunIntensity(double hour)
        {
            var hours = SkyArt.KeyHours;
            var values = SkyArt.KeyIntensities;
            if (hour <= hours[0])
            {
                return values[0];
            }

            for (var i = 0; i < hours.Length - 1; i++)
            {
                if (hour <= hours[i + 1])
                {
                    var t = (hour - hours[i]) / (hours[i + 1] - hours[i]);
                    return values[i] + (values[i + 1] - values[i]) * t;
                }
            }

            return values[values.Length - 1];
        }

        // Los radios del disco y del halo (radianes) y el factor del halo (DV-57, DV-63): la luna 0,8 × el sol; el halo 4 × su
        // disco; el halo de la luna × (0,4 + 0,6·iluminación). La visibilidad la multiplica quien dibuja.
        public static double DiskRadiusRad(bool moon)
        {
            return SceneMath.DegToRad(SkyArt.SunDiskRadiusDeg * (moon ? SkyArt.MoonDiskScale : 1));
        }

        public static double HaloRadiusRad(bool moon)
        {
            return DiskRadiusRad(moon) * SkyArt.HaloScale;
        }

        public static double HaloFactor(bool moon, double moonIllumination)
        {
            return moon ? SkyArt.MoonHaloBase + SkyArt.MoonHaloPerIllumination * moonIllumination : 1;
        }

        // La intensidad HDR del disco: el sol 3 + 5·e, la luna 1,6 (DN6a-21); × vis la pone quien dibuja.
        public static double DiskHdr(in CelestialState state)
        {
            return state.DiskIsMoon ? SkyArt.MoonHdr : SkyArt.SunHdrBase + SkyArt.SunHdrPerElevation * state.Elevation;
        }
    }
}
