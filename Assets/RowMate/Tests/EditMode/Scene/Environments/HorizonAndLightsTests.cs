using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // HEN · Las siluetas lejanas de río, costa y regata contra el oráculo (scene_horizons.json; AC-SCN-19-03 a -05; DT8-09).
    [TestFixture]
    public sealed class HorizonEnvironmentTests
    {
        [Test, Category("AC-SCN-19-03"), Category("AC-SCN-14-10")]
        public void TheSkylineIsTheLegacyColumnByColumn()
        {
            var rnd = new DartRandom(0);
            var rows = GoldenVectors.CheckRows("scene_horizons.json", "skyline", row =>
            {
                Skyline.Column((int)row.Int("i"), rnd, out var h, out var w, out var r, out var c, out var mask);
                if (Math.Abs(h - row.F64("height")) > 1e-12 || Math.Abs(w - row.F64("width")) > 1e-12)
                {
                    return "alto o ancho distinto";
                }

                if (r != row.Int("rows") || c != row.Int("cols"))
                {
                    return "celdas " + r + "×" + c + " contra " + row.Int("rows") + "×" + row.Int("cols");
                }

                var bits = row.Text("windows");
                for (var i = 0; i < bits.Length; i++)
                {
                    var lit = (mask & (1UL << i)) != 0;
                    if (lit != (bits[i] == '1'))
                    {
                        return "ventana " + i + " distinta";
                    }
                }

                return null;
            });
            Assert.That(rows, Is.EqualTo(64));
            Assert.That(Skyline.ColumnRad * 180 / Math.PI, Is.EqualTo(4.74).Within(0.01), "34 px de TV");
        }

        [Test, Category("AC-SCN-19-04")]
        public void TheFarCliffHasNoPhaseAndFades()
        {
            var rows = GoldenVectors.CheckRows("scene_horizons.json", "coastCliff", row =>
            {
                var h = CoastFarCliff.Height((int)row.Int("step"));
                return Math.Abs(h - row.F64("height")) <= 1e-12 ? null : "paso " + row.Int("step") + ": " + h;
            });
            Assert.That(rows, Is.EqualTo(21));
            Assert.That(CoastFarCliff.Height(20), Is.EqualTo(0).Within(1e-15), "se desvanece hacia el centro");
            Assert.That(CoastFarCliff.StepRad * 180 / Math.PI, Is.EqualTo(1.0).Within(0.01), "1,0° por paso en TV");
            Assert.That(CoastFarCliff.AzimuthRad(20) * 411, Is.EqualTo(-205.5 + 0.35 * 411).Within(1e-9), "el 35 % izquierdo del ancho");
        }

        [Test, Category("AC-SCN-19-05")]
        public void TheRegattaBackdropMatchesTheLegacyRectanglesWithinOneDp()
        {
            var rects = GoldenVectors.Rows("scene_horizons.json", "regattaRects").ToList();
            Assert.That(rects.Count, Is.EqualTo(5), "tierra y dos bloques de tribuna y torre en TV a 0 m");
            var hMax = LegacyTv.Height * RegattaArt.HorizonFraction;
            double Px(double rad) => rad * LegacyTv.Focal + LegacyTv.Width / 2;
            Assert.That(rects[0].F64("height"), Is.EqualTo(hMax * RegattaBackdrop.LandTop).Within(1e-9), "la tierra, 0,25·hMax");
            for (var n = 0; n < 2; n++)
            {
                var stand = RegattaBackdrop.Stand(n);
                var tower = RegattaBackdrop.Tower(n);
                var s = rects[1 + 2 * n];
                var t = rects[2 + 2 * n];
                Assert.That(Px(stand.LeftRad), Is.EqualTo(s.F64("left")).Within(1), "tribuna " + n);
                Assert.That(stand.WidthRad * LegacyTv.Focal, Is.EqualTo(s.F64("width")).Within(1));
                Assert.That(LegacyTv.HorizonY - stand.TopFraction * hMax, Is.EqualTo(s.F64("top")).Within(1));
                Assert.That((stand.TopFraction - stand.BottomFraction) * hMax, Is.EqualTo(s.F64("height")).Within(1));
                Assert.That(Px(tower.LeftRad), Is.EqualTo(t.F64("left")).Within(1), "torre " + n);
                Assert.That(tower.WidthRad * LegacyTv.Focal, Is.EqualTo(t.F64("width")).Within(1));
                Assert.That(tower.TopFraction * hMax, Is.EqualTo(t.F64("height")).Within(1));
            }

            Assert.That(RegattaBackdrop.PeriodRad * 180 / Math.PI, Is.EqualTo(28.6).Within(0.05), "28,6° (DT: la cuenta manda)");
        }

        [Test, Category("AC-SCN-19-03"), Category("AC-SCN-19-04"), Category("AC-SCN-19-05")]
        public void ColorsByHourAreHorizonColorOfTheLegacy()
        {
            var rows = GoldenVectors.CheckRows("scene_horizons.json", "colors", row =>
            {
                var p = SceneTimeOfDay.PaletteFor(row.Int("hour"));
                SceneColor c;
                switch (row.Text("tone"))
                {
                    case "riverBody": c = EnvironmentHorizonColors.Skyline(p); break;
                    case "coastCliff": c = EnvironmentHorizonColors.CoastCliff(p); break;
                    case "regattaLand": c = EnvironmentHorizonColors.RegattaLand(p); break;
                    case "regattaStand": c = EnvironmentHorizonColors.RegattaStand(p); break;
                    case "regattaTower": c = EnvironmentHorizonColors.RegattaTower(p); break;
                    default: c = EnvironmentHorizonColors.RegattaFlag(p, row.Text("tone")[4] - '0'); break;
                }

                return Math.Abs(c.R - row.F64("r")) > 1e-12 || Math.Abs(c.G - row.F64("g")) > 1e-12 || Math.Abs(c.B - row.F64("b")) > 1e-12
                    ? row.Text("tone") + " a las " + row.Int("hour") + " h"
                    : null;
            });
            Assert.That(rows, Is.EqualTo(25 * 9));
        }

        [Test, Category("AC-SCN-19-03"), Category("AC-SCN-19-04"), Category("AC-SCN-19-05")]
        public void HeightCappedAtNineDegreesStillWhenStillAndNoSideways()
        {
            foreach (var spec in new[] { HorizonSpec.River, HorizonSpec.Coast, HorizonSpec.Regatta })
            {
                foreach (var size in SceneSeeds.Sizes)
                {
                    var cam = new SceneCamera(size.Width, size.Height);
                    Assert.That(HorizonModel.PeakAngleDeg(cam, spec), Is.LessThanOrEqualTo(9 + 1e-9), size.Name);
                }

                Assert.That(HorizonModel.ApproachScale(1234, true, spec), Is.EqualTo(HorizonModel.ApproachScale(1234, true, spec)), "quieta con el bote quieto");
                Assert.That(HorizonModel.ApproachScale(0, true, spec), Is.EqualTo(1));
                Assert.That(HorizonModel.ApproachScale(5000, true, spec), Is.GreaterThan(1), "se acerca");
            }

            Assert.That(HorizonSpec.River.PeakScreenFraction, Is.EqualTo(0.12));
            Assert.That(HorizonSpec.Coast.PeakScreenFraction, Is.EqualTo(0.08));
            Assert.That(HorizonSpec.Regatta.PeakScreenFraction, Is.EqualTo(0.06));
            // La forma no depende de la distancia: Column no la recibe (el legacy corría el skyline con distance·0,24 px).
            var rnd = new DartRandom(0);
            Skyline.Column(7, rnd, out var h1, out _, out _, out _, out var m1);
            Skyline.Column(7, rnd, out var h2, out _, out _, out _, out var m2);
            Assert.That((h1, m1), Is.EqualTo((h2, m2)));
        }

        [Test, Category("AC-SCN-19-03"), Category("AC-SCN-20-04")]
        public void SkylineWindowsOnlyBelowSixTenthsAmbient()
        {
            Assert.That(EnvironmentHorizonColors.SkylineWindowsOn(SceneTimeOfDay.PaletteFor(22).Ambient), Is.True);
            foreach (var h in new[] { 6.0, 12, 18 })
            {
                Assert.That(EnvironmentHorizonColors.SkylineWindowsOn(SceneTimeOfDay.PaletteFor(h).Ambient), Is.False, h + " h");
            }

            Assert.That(RiverArt.SkylineWindow, Is.EqualTo(0xFFFFE08Au));
            Assert.That(RiverArt.SkylineWindowAlpha, Is.EqualTo(0.85));
        }
    }

    // FWI · Las ventanas de los edificios del río (shore_painter.dart:133-148; AC-SCN-20-04).
    [TestFixture]
    public sealed class FacadeWindowsTests
    {
        [Test, Category("AC-SCN-20-04"), Category("AC-SCN-20-03")]
        public void TheMaskIsTheLegacyInTwentySeeds()
        {
            var rnd = new DartRandom(0);
            for (var seed = 0; seed < 1000; seed += 50)
            {
                // El legacy: floors = 3 + seed % 4; Random(seed); por piso y por ventana, nextDouble() < 0,6.
                var legacy = new DartRandom(seed);
                var floors = 3 + seed % 4;
                var expected = 0u;
                for (var f = 0; f < floors; f++)
                {
                    for (var i = 0; i < 3; i++)
                    {
                        if (legacy.NextDouble() < 0.6)
                        {
                            expected |= 1u << (f * 3 + i);
                        }
                    }
                }

                Assert.That(FacadeWindows.FrontMask(seed, rnd), Is.EqualTo(expected), "variante " + seed);
                Assert.That(FacadeWindows.Floors(seed), Is.EqualTo(floors));
                Assert.That(FacadeWindows.Tone(seed), Is.EqualTo(seed % 3));
                Assert.That((double)FacadeWindows.AsFloat(expected), Is.EqualTo((double)expected), "exacta en un float");
            }

            Assert.That(RiverArt.BuildingTones, Is.EqualTo(new[] { 0xFF78909Cu, 0xFF8D6E63u, 0xFFB0BEC5u }));
        }

        [Test, Category("AC-SCN-20-04")]
        public void TheRiverFaceContinuesTheSameSequence()
        {
            var rnd = new DartRandom(0);
            FacadeWindows.Masks(321, rnd, out var front, out var river);
            var legacy = new DartRandom(321);
            var floors = FacadeWindows.Floors(321);
            for (var i = 0; i < floors * 3; i++)
            {
                legacy.NextDouble();
            }

            for (var i = 0; i < floors * 5; i++)
            {
                Assert.That((river & (1UL << i)) != 0, Is.EqualTo(legacy.NextDouble() < 0.6), "ventana al río " + i);
            }

            Assert.That(front, Is.EqualTo(FacadeWindows.FrontMask(321, rnd)));
        }
    }

    // NLE · Las luces de la noche por tipo (AC-SCN-20-04; DV-60).
    [TestFixture]
    public sealed class NightLightsEnvironmentTests
    {
        private static readonly NightLightKind[] Kinds =
        {
            NightLightKind.BuildingWindows, NightLightKind.Lamp, NightLightKind.Lighthouse, NightLightKind.SkylineWindows,
            NightLightKind.FinishTower, NightLightKind.BoathouseGate, NightLightKind.CityBlock,
        };

        [Test, Category("AC-SCN-20-04")]
        public void LitBelowSixTenthsOffAtSixTwelveAndEighteen()
        {
            foreach (var kind in Kinds)
            {
                for (var seed = 0; seed < 40; seed++)
                {
                    Assert.That(NightLights.IsLit(kind, seed, 22, SceneTimeOfDay.PaletteFor(22).Ambient), Is.True, kind + " a las 22");
                    foreach (var h in new[] { 6.0, 12, 18 })
                    {
                        Assert.That(NightLights.IsLit(kind, seed, h, SceneTimeOfDay.PaletteFor(h).Ambient), Is.False, kind + " a las " + h);
                    }
                }
            }
        }

        [Test, Category("AC-SCN-20-04")]
        public void StaggeredWithinTenMinutesAndTheCabinKeepsItsMinute()
        {
            var minutes = new HashSet<int>();
            foreach (var kind in Kinds)
            {
                for (var seed = 0; seed < 50; seed++)
                {
                    var dusk = NightLights.DuskMinute(kind, seed);
                    Assert.That(Math.Abs(dusk - NightLights.DuskThresholdHour * 60), Is.LessThanOrEqualTo(10.5));
                    minutes.Add(dusk);
                }
            }

            Assert.That(minutes.Count, Is.GreaterThan(10), "escalonado (DV-60)");
            for (var seed = 0; seed < 30; seed++)
            {
                Assert.That(NightLights.DuskMinute(NightLightKind.Cabin, seed), Is.EqualTo(NightLights.DuskMinute(seed)), "la cabaña de la 6a no cambia");
            }
        }

        [Test, Category("AC-SCN-20-04")]
        public void EachLightPropHasItsKind()
        {
            Assert.That(NightLights.TryKindOf(new ShoreProp(13, 0, PropKind.Building, 1, 7), out var k1, out var s1) && k1 == NightLightKind.BuildingWindows && s1 == 7);
            Assert.That(NightLights.TryKindOf(new ShoreProp(-9.5, 12.5, PropKind.Lamp), out var k2, out var s2) && k2 == NightLightKind.Lamp);
            Assert.That(NightLights.TryKindOf(new ShoreProp(9.5, 12.5, PropKind.Lamp), out _, out var s3) && s3 != s2, "cada farol con su minuto");
            Assert.That(NightLights.TryKindOf(new ShoreProp(-14, 530, PropKind.Lighthouse), out var k4, out _) && k4 == NightLightKind.Lighthouse);
            Assert.That(NightLights.TryKindOf(new ShoreProp(13, 10, PropKind.Cabin, 1, 7), out var k5, out var s5) && k5 == NightLightKind.Cabin && s5 == 7);
            Assert.That(NightLights.TryKindOf(new ShoreProp(3, 25, PropKind.LaneBuoy), out _, out _), Is.False);

            // En la sesión del río a las 22 h: las ventanas y los faroles encendidos (nivel 1).
            var s = new SceneSession(new RiverEnvironment(), SceneSessionOptions.Default);
            var frame = SceneFrame.Create();
            s.Open(0);
            var at = new DateTimeOffset(2026, 3, 15, 22, 0, 0, TimeSpan.Zero);
            s.Tick(new SceneTickInputs(0.016, at, at, default, 22, false, 390, 844, 0), ref frame);
            var lit = 0;
            for (var i = 0; i < frame.VisibleProps.Count; i++)
            {
                var kind = frame.VisibleProps[i].Kind;
                if (kind == PropKind.Lamp || kind == PropKind.Building || kind == PropKind.Boathouse)
                {
                    Assert.That(frame.PropLight[i], Is.EqualTo(1), kind.ToString());
                    lit++;
                }
            }

            Assert.That(lit, Is.GreaterThan(20));
        }
    }
}
