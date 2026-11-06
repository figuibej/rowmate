using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // HZN · Las dos cordilleras (lake_environment.dart:43-85, environment.dart:127-129; AC-SCN-19-01, -02; Q-65, DV-55, DV-64,
    // DV-65; DN6a-01, -02). Los colores contra el oráculo bit a bit; el perfil con Within(1e-12) (trascendente).
    [TestFixture]
    public sealed class HorizonModelTests
    {
        private static readonly HorizonSpec Spec = HorizonSpec.Lake;

        [Test, Category("AC-SCN-19-01")]
        public void ColorsPerHourMatchTheOracle()
        {
            var rows = GoldenVectors.CheckRows("scene_sky.json", "horizon", row =>
            {
                var p = SceneTimeOfDay.PaletteFor(row.Int("hour"));
                var far = HorizonModel.FarColor(p);
                var near = HorizonModel.NearColor(p);
                var snow = HorizonModel.SnowColor(p);
                if (!Same(far, row, "far"))
                {
                    return "lejana " + far;
                }

                if (!Same(near, row, "near"))
                {
                    return "cercana " + near;
                }

                return Same(snow, row, "snow") ? null : "nieve " + snow;
            });
            Assert.That(rows, Is.EqualTo(25));
        }

        [Test, Category("AC-SCN-19-01")]
        public void TheTonesOfTheLegacy()
        {
            Assert.That(HorizonModel.FarTone, Is.EqualTo(SceneColor.FromHex("#5C6E8A")));
            Assert.That(HorizonModel.NearTone, Is.EqualTo(SceneColor.FromHex("#2F4A3A")));
            Assert.That(HorizonModel.SnowTone, Is.EqualTo(SceneColor.FromHex("#F2F6FA")));
            var p = SceneTimeOfDay.PaletteFor(12);
            Assert.That(HorizonModel.FarColor(p), Is.EqualTo(SceneColor.Lerp(HorizonModel.FarTone, p.Fog, 0.35 + 0.4 * 1.0)));
            Assert.That(HorizonModel.NearColor(p), Is.EqualTo(SceneColor.Lerp(HorizonModel.NearTone, p.Fog, 0.35 + 0.4 * 0.3)));
        }

        [Test, Category("AC-SCN-19-01")]
        public void TheProfileMatchesTheOracle()
        {
            var rows = GoldenVectors.CheckRows("scene_sky.json", "profile", row =>
            {
                var p = HorizonModel.LegacyProfile(row.F64("u"), row.F64("phase"));
                return Math.Abs(p - row.F64("p")) <= 1e-12 ? null : SceneGolden.F(p);
            });
            Assert.That(rows, Is.EqualTo(502));
        }

        [TestCase("V390", 9.0, Category = "AC-SCN-19-01")]
        [TestCase("TV", 9.0, Category = "AC-SCN-19-01")]
        [TestCase("P360", 9.0, Category = "AC-SCN-19-01")]
        [TestCase("H844", 6.8428, Category = "AC-SCN-19-01")]
        [TestCase("TH", 6.8428, Category = "AC-SCN-19-01")]
        [TestCase("L640", 6.8428, Category = "AC-SCN-19-01")]
        [TestCase("W1280", 6.8428, Category = "AC-SCN-19-01")]
        public void TheHeightIsTheLegacyOneWithATopOfNineDegrees(string size, double degrees)
        {
            var cam = SceneSeeds.Camera(size);
            Assert.That(HorizonModel.PeakAngleDeg(cam, Spec), Is.EqualTo(degrees).Within(0.0001));
            if (cam.Width > cam.Height)
            {
                Assert.That(HorizonModel.PeakTan(cam, Spec), Is.EqualTo(0.12).Within(1e-12), "en horizontal, el hMax del legacy exacto");
            }
        }

        [Test, Category("AC-SCN-19-01")]
        public void TheMassifAndTheCentralPeak()
        {
            // 04-design-notes §6.1: el macizo a −25,4° (p 0,892), el pico central a −2,3° (0,658), el de la derecha a +58° (0,942).
            Assert.That(PeakNear(-25.4, true), Is.EqualTo(0.892).Within(0.002));
            Assert.That(PeakNear(-2.3, true), Is.EqualTo(0.658).Within(0.002));
            Assert.That(PeakNear(58, true), Is.EqualTo(0.942).Within(0.002));
            // El macizo en grados: 6,11° en horizontal y 8,04° en vertical.
            Assert.That(HorizonModel.RidgeElevationDeg(SceneSeeds.Camera("TH"), Spec, -25.4, true, 0), Is.EqualTo(6.11).Within(0.02));
            Assert.That(HorizonModel.RidgeElevationDeg(SceneSeeds.Camera("TV"), Spec, -25.4, true, 0), Is.EqualTo(8.04).Within(0.02));
        }

        [Test, Category("AC-SCN-19-01")]
        public void TheShapeDependsNeitherOnTheWindowNorOnTheDistance()
        {
            for (var az = -65.0; az <= 65; az += 0.5)
            {
                var far = HorizonModel.Profile(az, true);
                var near = HorizonModel.Profile(az, false);
                Assert.That(far, Is.EqualTo(HorizonModel.LegacyProfile(az / 70 + 0.5, 0)).Within(1e-15));
                Assert.That(near, Is.EqualTo(HorizonModel.LegacyProfile(1.6 * (az / 70 + 0.5), 3.1)).Within(1e-15));
                // A cualquier distancia la elevación es la de d = 0 × k(d): el perfil solo escala.
                var cam = SceneSeeds.Camera("TV");
                var e0 = Math.Tan(SceneMath.DegToRad(HorizonModel.RidgeElevationDeg(cam, Spec, az, true, 0)));
                var e1 = Math.Tan(SceneMath.DegToRad(HorizonModel.RidgeElevationDeg(cam, Spec, az, true, 20000)));
                Assert.That(e1, Is.EqualTo(e0 * HorizonModel.ApproachScale(20000, true, Spec)).Within(1e-12));
            }
        }

        [TestCase(2000.0, 1.022, 1.067, Category = "AC-SCN-19-02")]
        [TestCase(5000.0, 1.055, 1.165, Category = "AC-SCN-19-02")]
        [TestCase(10000.0, 1.104, 1.322, Category = "AC-SCN-19-02")]
        [TestCase(21100.0, 1.195, 1.606, Category = "AC-SCN-19-02")]
        [TestCase(42200.0, 1.304, 1.887, Category = "AC-SCN-19-02")]
        [TestCase(100000.0, 1.390, 1.997, Category = "AC-SCN-19-02")]
        public void TheyApproachWithoutSliding(double distance, double far, double near)
        {
            // La tabla del Designer (00-deuda-diseno §5), ± 0,001.
            Assert.That(HorizonModel.ApproachScale(distance, true, Spec), Is.EqualTo(far).Within(0.001));
            Assert.That(HorizonModel.ApproachScale(distance, false, Spec), Is.EqualTo(near).Within(0.001));
        }

        [Test, Category("AC-SCN-19-02")]
        public void TheApproachIsMonotonousBoundedAndStillWhenTheBoatIs()
        {
            Assert.That(HorizonModel.ApproachScale(0, true, Spec), Is.EqualTo(1));
            Assert.That(HorizonModel.ApproachScale(0, false, Spec), Is.EqualTo(1));
            var prevFar = 1.0;
            var prevNear = 1.0;
            for (var d = 0.0; d <= 200000; d += 250)
            {
                var far = HorizonModel.ApproachScale(d, true, Spec);
                var near = HorizonModel.ApproachScale(d, false, Spec);
                Assert.That(far, Is.GreaterThanOrEqualTo(prevFar));
                Assert.That(near, Is.GreaterThanOrEqualTo(prevNear));
                Assert.That(far, Is.LessThanOrEqualTo(LakeArt.FarD0 / LakeArt.FarDMin), "nunca pasan su mínimo");
                Assert.That(near, Is.LessThanOrEqualTo(LakeArt.NearD0 / LakeArt.NearDMin));
                prevFar = far;
                prevNear = near;
            }

            Assert.That(HorizonModel.ApproachScale(1234, true, Spec), Is.EqualTo(HorizonModel.ApproachScale(1234, true, Spec)), "quietas con el bote quieto");
            // Al empezar se acercan al 4 % de la velocidad del bote: dk/dd = 0,04/D₀.
            var slope = (HorizonModel.ApproachScale(1, true, Spec) - 1) / 1;
            Assert.That(slope, Is.EqualTo(0.04 / LakeArt.FarD0).Within(1e-8));
        }

        [Test, Category("AC-SCN-19-01")]
        public void SnowAbove58PercentWithASoftEdge()
        {
            Assert.That(Spec.SnowThreshold, Is.EqualTo(0.58));
            Assert.That(Spec.SnowEdge, Is.EqualTo(0.03));
            Assert.That(Spec.SnowNoise, Is.EqualTo(0.04));
            Assert.That(Spec.SnowNoisePeriodDeg, Is.EqualTo(1.7));
            Assert.That(HorizonModel.SnowMask(0.70, 0, Spec), Is.EqualTo(1), "bien arriba: nieve");
            Assert.That(HorizonModel.SnowMask(0.45, 0, Spec), Is.EqualTo(0), "bien abajo: no");
            for (var az = -65.0; az <= 65; az += 0.05)
            {
                var m = HorizonModel.SnowMask(HorizonModel.Profile(az, true), az, Spec);
                Assert.That(m, Is.InRange(0.0, 1.0));
            }
        }

        [TestCase("TV", 0.45, Category = "AC-SCN-19-01")]
        [TestCase("TH", 0.33, Category = "AC-SCN-19-01")]
        public void SnowCoverageOfTheVisibleRange(string size, double coverage)
        {
            // DV-64: ≈ 45 % del ancho de la lejana en TV y ≈ 33 % en TH (± 3 %).
            var cam = SceneSeeds.Camera(size);
            var half = SceneMath.RadToDeg(Math.Atan(cam.CenterX / cam.Focal));
            var snowy = 0;
            var total = 0;
            for (var az = -half; az <= half; az += 0.01)
            {
                total++;
                snowy += HorizonModel.SnowMask(HorizonModel.Profile(az, true), az, Spec) >= 0.5 ? 1 : 0;
            }

            Assert.That((double)snowy / total, Is.EqualTo(coverage).Within(0.03));
        }

        [Test, Category("AC-SCN-19-01")]
        public void TheLegacySnowRuleIsAContractWithOnePeakAtZero()
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in GoldenVectors.Rows("scene_sky.json", "snowPeaks"))
            {
                expected[row.Text("size") + "@" + SceneGolden.F(row.F64("distance"))] = row.Text("peaks");
            }

            Span<int> peaks = stackalloc int[48];
            foreach (var s in SceneSeeds.Sizes)
            {
                foreach (var d in new[] { 0.0, 920.0, 1234.0 })
                {
                    var n = HorizonModel.LegacySnowPeaks(s.Width, s.Height, d, peaks);
                    var text = new List<string>();
                    for (var i = 0; i < n; i++)
                    {
                        text.Add(peaks[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    Assert.That(string.Join(" ", text), Is.EqualTo(expected[s.Name + "@" + SceneGolden.F(d)]), s.Name + " a " + d + " m");
                    if (d == 0)
                    {
                        Assert.That(n, Is.EqualTo(1), "a 0 m hay un solo pico nevado (DV-64)");
                    }
                }
            }
        }

        private static double PeakNear(double azimuth, bool far)
        {
            var best = 0.0;
            for (var az = azimuth - 3; az <= azimuth + 3; az += 0.01)
            {
                best = Math.Max(best, HorizonModel.Profile(az, far));
            }

            return best;
        }

        private static bool Same(in SceneColor c, GoldenRow row, string prefix)
        {
            return BitConverter.DoubleToInt64Bits(c.R) == BitConverter.DoubleToInt64Bits(row.F64(prefix + "R"))
                && BitConverter.DoubleToInt64Bits(c.G) == BitConverter.DoubleToInt64Bits(row.F64(prefix + "G"))
                && BitConverter.DoubleToInt64Bits(c.B) == BitConverter.DoubleToInt64Bits(row.F64(prefix + "B"));
        }
    }
}
