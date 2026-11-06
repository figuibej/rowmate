using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // CEL · El astro que se ve y su luz (06a-escena-lago/02-tech-design §2.6; AC-SCN-10-04 a -06; D-ART-05, Q-63; DV-52, DV-54;
    // DT6a-02, DN6a-03, -04). Comparador: Within(1e-9) en grados (§2).
    [TestFixture]
    public sealed class CelestialPlacementTests
    {
        private const double Illumination = 0.94;

        private static CelestialState At(double hour)
        {
            return CelestialPlacement.Evaluate(hour, Illumination, CelestialSpec.Default);
        }

        [TestCase(6.0, -17.5, 1.5, false, Category = "AC-SCN-10-04")]
        [TestCase(9.0, -8.75, 8.9246, false, Category = "AC-SCN-10-04")]
        [TestCase(12.0, 0.0, 12.0, false, Category = "AC-SCN-10-04")]
        [TestCase(15.0, 8.75, 8.9246, false, Category = "AC-SCN-10-04")]
        [TestCase(18.0, 17.5, 1.5, false, Category = "AC-SCN-10-04")]
        [TestCase(22.0, -5.8333, 10.5933, true, Category = "AC-SCN-10-04")]
        [TestCase(0.0, 0.0, 12.0, true, Category = "AC-SCN-10-04")]
        public void TheVisibleDiskFollowsTheLegacyProgression(double hour, double azimuth, double elevation, bool moon)
        {
            var s = At(hour);
            Assert.That(s.DiskAzimuthDeg, Is.EqualTo(azimuth).Within(0.0001));
            Assert.That(s.DiskElevationDeg, Is.EqualTo(elevation).Within(0.0001));
            Assert.That(s.DiskIsMoon, Is.EqualTo(moon));
            Assert.That(s.DiskVisibility, Is.EqualTo(1).Within(1e-12));
            Assert.That(s.LightAzimuthDeg, Is.EqualTo(s.DiskAzimuthDeg), "la luz viene del mismo acimut");
        }

        [Test, Category("AC-SCN-10-04")]
        public void TheArcOfTheGate()
        {
            Assert.That(SkyArt.ArcDeg, Is.EqualTo(10.5));
            Assert.That(SkyArt.BaseDeg, Is.EqualTo(1.5));
            Assert.That(SkyArt.HiddenDeg, Is.EqualTo(-2.5));
            Assert.That(SkyArt.AzimuthSpanDeg, Is.EqualTo(70));
            Assert.That(SkyArt.BaseDeg + SkyArt.ArcDeg, Is.EqualTo(12.0), "12° al mediodía");
        }

        [TestCase(5.0, -20.42, -0.36, 0.54, Category = "AC-SCN-10-05")]
        [TestCase(19.0, 20.42, -0.36, 0.54, Category = "AC-SCN-10-05")]
        [TestCase(20.0, -11.67, 0.98, 0.38, Category = "AC-SCN-10-05")]
        [TestCase(4.0, 11.67, 0.98, 0.38, Category = "AC-SCN-10-05")]
        public void RisingAndSettingBehindTheLandscape(double hour, double azimuth, double elevation, double visibility)
        {
            var s = At(hour);
            Assert.That(s.DiskAzimuthDeg, Is.EqualTo(azimuth).Within(0.01));
            Assert.That(s.DiskElevationDeg, Is.EqualTo(elevation).Within(0.01));
            Assert.That(s.DiskVisibility, Is.EqualTo(visibility).Within(0.01));
        }

        [Test, Category("AC-SCN-10-05")]
        public void TheChangeOfStarIsNeverSeen()
        {
            // En el minuto del cambio (19:34 y 4:26) y en el instante exacto los dos astros están a −2,5° y la luz vale 0.
            foreach (var hour in new[]
                     {
                         19 + 34 / 60.0, SceneTimeOfDay.NightStartHour, SceneTimeOfDay.NightStartHour + 1e-9, 4 + 26 / 60.0,
                         SceneTimeOfDay.NightEndHour, SceneTimeOfDay.NightEndHour - 1e-9,
                     })
            {
                var s = At(hour);
                Assert.That(s.DiskElevationDeg, Is.EqualTo(SkyArt.HiddenDeg).Within(1e-3), "a las " + hour);
                Assert.That(s.LightIntensity, Is.EqualTo(0).Within(1e-4), "la luz en el cambio");
            }

            Assert.That(At(SceneTimeOfDay.NightStartHour).LightIntensity, Is.EqualTo(0).Within(1e-9), "19:34:19");
            Assert.That(At(SceneTimeOfDay.NightEndHour).LightIntensity, Is.EqualTo(0).Within(1e-9), "4:25:41");
            Assert.That(SkyArt.LightMinIntensity, Is.EqualTo(0));
        }

        [Test, Category("AC-SCN-10-05")]
        public void SweepMinuteByMinuteNoJumpsAndLightOnlyTurnsAtZero()
        {
            var maxSpeed = 0.0;
            var maxAt = 0.0;
            var prev = At(0);
            for (var i = 1; i <= 24 * 60 * 10; i++)
            {
                // Cada 6 s de escena: el tope es por minuto, se mide con el paso de minuto.
                var hour = i / 600.0;
                var s = At(hour % 24);
                if (s.DiskIsMoon == prev.DiskIsMoon)
                {
                    SceneMath.Direction(prev.DiskAzimuthDeg, prev.DiskElevationDeg, out var ax, out var ay, out var az);
                    SceneMath.Direction(s.DiskAzimuthDeg, s.DiskElevationDeg, out var bx, out var by, out var bz);
                    var perMinute = SceneMath.RadToDeg(SceneMath.AngleBetween(ax, ay, az, bx, by, bz)) * 10;
                    if (perMinute > maxSpeed)
                    {
                        maxSpeed = perMinute;
                        maxAt = hour;
                    }
                }
                else
                {
                    // El cambio de astro: los dos escondidos y sin luz.
                    Assert.That(prev.DiskElevationDeg, Is.EqualTo(SkyArt.HiddenDeg).Within(0.01));
                    Assert.That(s.DiskElevationDeg, Is.EqualTo(SkyArt.HiddenDeg).Within(0.01));
                    Assert.That(Math.Min(prev.LightIntensity, s.LightIntensity), Is.LessThan(1e-4));
                }

                prev = s;
            }

            TestContext.Out.WriteLine("el astro más rápido: " + maxSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                + "°/min a las " + maxAt.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            Assert.That(maxSpeed, Is.LessThanOrEqualTo(SkyArt.MaxDegPerMinute), "≤ 0,3°/min (DT6a-02)");
            Assert.That(maxSpeed, Is.EqualTo(0.257).Within(0.01), "lo más rápido medido");
            Assert.That(maxAt, Is.InRange(19.9, 20.3).Or.InRange(3.6, 4.0), "la luna que sale o se pone");
        }

        [Test, Category("AC-SCN-10-05")]
        public void TheSpecWindowsOfTheSunAndTheMoon()
        {
            Assert.That(At(18.5).DiskVisibility, Is.EqualTo(1).Within(1e-12), "el sol se pone desde las 18:30");
            Assert.That(At(19.55).DiskVisibility, Is.EqualTo(0).Within(1e-12));
            Assert.That(At(20.6).DiskVisibility, Is.EqualTo(1).Within(1e-12), "la luna sale hasta las ≈ 20:36");
            Assert.That(At(3.4).DiskVisibility, Is.EqualTo(1).Within(1e-12), "la luna se pone desde las ≈ 3:24");
            Assert.That(At(4.45).DiskVisibility, Is.EqualTo(0).Within(1e-12));
            Assert.That(At(5.5).DiskVisibility, Is.EqualTo(1).Within(1e-12), "el sol sale hasta las 5:30");
        }

        [TestCase(12.0, 55.0, 1.75, Category = "AC-SCN-10-06")]
        [TestCase(6.0, 6.0, 0.90, Category = "AC-SCN-10-06")]
        [TestCase(18.0, 6.0, 0.85, Category = "AC-SCN-10-06")]
        [TestCase(9.0, 40.648, 1.45, Category = "AC-SCN-10-06")]
        [TestCase(15.0, 40.648, 1.30, Category = "AC-SCN-10-06")]
        [TestCase(7.0, 18.682, 1.25, Category = "AC-SCN-10-06")]
        public void TheSunLightHasItsOwnElevation(double hour, double elevation, double intensity)
        {
            var s = At(hour);
            Assert.That(s.LightElevationDeg, Is.EqualTo(elevation).Within(0.001));
            Assert.That(s.LightIntensity, Is.EqualTo(intensity).Within(0.001));
        }

        [TestCase(22.0, 44.641, Category = "AC-SCN-10-06")]
        [TestCase(0.0, 50.0, Category = "AC-SCN-10-06")]
        [TestCase(20.0, 30.0, Category = "AC-SCN-10-06")]
        public void TheMoonLightHasItsOwnElevationAndGrowsWithThePhase(double hour, double elevation)
        {
            var s = At(hour);
            Assert.That(s.LightElevationDeg, Is.EqualTo(elevation).Within(0.001));
            var full = CelestialPlacement.Evaluate(hour, 1, CelestialSpec.Default);
            var newMoon = CelestialPlacement.Evaluate(hour, 0, CelestialSpec.Default);
            Assert.That(full.LightIntensity, Is.GreaterThan(newMoon.LightIntensity), "la luz de luna crece con la parte iluminada");
            Assert.That(full.LegacyStrength, Is.EqualTo(newMoon.LegacyStrength).And.EqualTo(0.45), "la fuerza del agua no");
            Assert.That(s.LightIntensity, Is.EqualTo((0.04 + 0.12 * Illumination) * s.DiskVisibility).Within(1e-12));
        }

        [Test, Category("AC-SCN-10-06")]
        public void ShadowsFallTowardTheCameraOnTheSideOppositeTheStar()
        {
            // La luz viene de adelante (del acimut del astro): la sombra va hacia −z y al costado opuesto.
            foreach (var hour in new[] { 7.0, 10.0, 14.0, 17.5, 22.0, 2.0 })
            {
                var s = At(hour);
                SceneMath.Direction(s.LightAzimuthDeg, s.LightElevationDeg, out var lx, out _, out var lz);
                Assert.That(lz, Is.GreaterThan(0), "la luz viene de adelante: contraluz");
                if (Math.Abs(s.LightAzimuthDeg) > 1e-9)
                {
                    Assert.That(Math.Sign(-lx), Is.EqualTo(-Math.Sign(s.LightAzimuthDeg)), "la sombra cae del lado opuesto al astro");
                }
            }
        }

        [TestCase(5.0, 0.2, Category = "AC-SCN-10-03")]
        [TestCase(9.0, 0.8485, Category = "AC-SCN-10-03")]
        [TestCase(12.0, 1.0, Category = "AC-SCN-10-03")]
        [TestCase(18.0, 0.2, Category = "AC-SCN-10-03")]
        [TestCase(23.0, 0.45, Category = "AC-SCN-10-03")]
        public void TheLegacyStrengthIsKept(double hour, double strength)
        {
            Assert.That(At(hour).LegacyStrength, Is.EqualTo(strength).Within(0.0005));
            Assert.That(At(hour).LegacyStrength, Is.EqualTo(SceneTimeOfDay.LightFor(hour, new SceneCamera(390, 844)).Strength).Within(1e-12));
        }

        [Test, Category("AC-SCN-10-04")]
        public void TheDiskAlwaysStaysInFrameAndOnTheLegacySide()
        {
            foreach (var size in SceneSeeds.Sizes)
            {
                var cam = new SceneCamera(size.Width, size.Height);
                for (var m = 0; m < 1440; m += 10)
                {
                    var hour = SceneSeeds.HourOfMinute(m);
                    var s = At(hour);
                    var x = DegradedWater.DiskScreenX(cam, s.DiskAzimuthDeg);
                    Assert.That(x, Is.InRange(0, cam.Width), size.Name + " a las " + hour);
                    var legacy = SceneTimeOfDay.LightFor(hour, cam);
                    Assert.That(Math.Sign(x - cam.CenterX), Is.EqualTo(Math.Sign(legacy.X - cam.CenterX)), "del mismo lado que en el legacy");
                }
            }
        }
    }
}
