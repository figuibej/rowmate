using System;
using System.Globalization;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Environments
{
    // WIN · Las 28 entradas del agua (LT-124; AC-SCN-12-01, -17-01) y los tres tramos del agua degradada con el tinte del
    // escenario, contra el oráculo (scene_{river,coast,regatta}.json: uniforms, bands).
    [TestFixture]
    public sealed class WaterInputsTests
    {
        // legacy: test/scene/water_painter_test.dart:27 'WaterPainter.uniforms devuelve 28 floats en el orden del shader'
        [TestCase(TestName = "WaterPainter.uniforms devuelve 28 floats en el orden del shader", Category = "LT-124,AC-SCN-12-01,AC-SCN-17-01")]
        public void UniformsAreTheLegacyTwentyEight()
        {
            var cam = new SceneCamera(844, 390);
            var u = new double[WaterShaderInputs.Count];
            WaterShaderInputs.Uniforms(cam, 0, 0, 2, SceneTimeOfDay.PaletteFor(12), SceneTimeOfDay.LightFor(12, cam), new CoastEnvironment(), u);
            Assert.That(u, Has.Length.EqualTo(28));
            Assert.That(u[0], Is.EqualTo(844));
            Assert.That(u[1], Is.EqualTo(390));
            Assert.That(u[2], Is.EqualTo(cam.HorizonY));
            Assert.That(u[7], Is.EqualTo(0.8), "waveAmplitude de la costa");
            Assert.That(u[9], Is.EqualTo(cam.Width / 2), "uSunX al mediodía");
            Assert.That(u[26], Is.EqualTo(0.5).Within(1e-9), "wake = speed / 4");
            Assert.That(u[27], Is.EqualTo(SceneCamera.BoatZ - 4.2), "popa");
        }

        [TestCase(EnvironmentId.River, "scene_river.json", Category = "AC-SCN-12-01")]
        [TestCase(EnvironmentId.Coast, "scene_coast.json", Category = "AC-SCN-12-01,AC-SCN-17-01")]
        [TestCase(EnvironmentId.Regatta, "scene_regatta.json", Category = "AC-SCN-12-01")]
        public void UniformsMatchTheOracle(EnvironmentId id, string file)
        {
            var env = EnvironmentCatalog.Create(id);
            var u = new double[WaterShaderInputs.Count];
            var rows = GoldenVectors.CheckRows(file, "uniforms", row =>
            {
                var cam = SceneSeeds.Camera(row.Text("size"));
                var hour = row.F64("hour");
                WaterShaderInputs.Uniforms(cam, 0, 0, 2, SceneTimeOfDay.PaletteFor(hour), SceneTimeOfDay.LightFor(hour, cam), env, u);
                var expected = row.Text("values").Split(' ');
                if (expected.Length != 28)
                {
                    return "el oráculo no trae 28 valores";
                }

                for (var i = 0; i < 28; i++)
                {
                    var e = BitConverter.Int64BitsToDouble(long.Parse(expected[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    if (Math.Abs(e - u[i]) > 1e-9)
                    {
                        return "[" + i + "] esperado " + e.ToString("R", CultureInfo.InvariantCulture) + ", obtenido " + u[i].ToString("R", CultureInfo.InvariantCulture);
                    }
                }

                return null;
            });
            Assert.That(rows, Is.EqualTo(8));
        }

        [TestCase(EnvironmentId.River, "scene_river.json", Category = "AC-SCN-17-01")]
        [TestCase(EnvironmentId.Coast, "scene_coast.json", Category = "AC-SCN-17-01")]
        [TestCase(EnvironmentId.Regatta, "scene_regatta.json", Category = "AC-SCN-17-01")]
        public void TintedBandsByHourMatchTheOracle(EnvironmentId id, string file)
        {
            var env = EnvironmentCatalog.Create(id);
            var rows = GoldenVectors.CheckRows(file, "bands", row =>
            {
                DegradedWater.Bands(SceneTimeOfDay.PaletteFor(row.Int("hour")), env, out var top, out var mid, out var bottom);
                return Diff("top", top, row, "top") ?? Diff("mid", mid, row, "mid") ?? Diff("bottom", bottom, row, "bottom");
            });
            Assert.That(rows, Is.EqualTo(25));
        }

        [Test, Category("AC-SCN-17-01")]
        public void TheSessionPaintsTheTintedBandsInTheLegacyOrder()
        {
            // La franja de arriba es lerp(far teñido; fog; 0,5), no el tinte de lerp(far; fog; 0,5).
            var p = SceneTimeOfDay.PaletteFor(12);
            var coast = new CoastEnvironment();
            DegradedWater.Bands(p, coast, out var top, out _, out _);
            var wrong = coast.TintWater(SceneColor.Lerp(p.WaterFar, p.Fog, 0.5));
            Assert.That(top, Is.Not.EqualTo(wrong));
            DegradedWater.Bands(p, new LakeEnvironment(), out var lakeTop, out var lakeMid, out var lakeBottom);
            DegradedWater.Bands(p, out var t6, out var m6, out var b6);
            Assert.That((lakeTop, lakeMid, lakeBottom), Is.EqualTo((t6, m6, b6)), "el lago queda igual que en la 6a");
        }

        private static string? Diff(string name, in SceneColor c, GoldenRow row, string prefix)
        {
            var r = row.F64(prefix + "R");
            var g = row.F64(prefix + "G");
            var b = row.F64(prefix + "B");
            return Math.Abs(c.R - r) > 1e-12 || Math.Abs(c.G - g) > 1e-12 || Math.Abs(c.B - b) > 1e-12
                ? name + " a las " + row.Int("hour") + " h: " + c + " contra (" + r + "; " + g + "; " + b + ")"
                : null;
        }
    }

    // CSW · La ola de la costa (AC-SCN-12-01, -06-04; DT8-05, DT8-06).
    [TestFixture]
    public sealed class CoastSwellTests
    {
        [Test, Category("AC-SCN-12-01")]
        public void TheFormulaIsWaterFragWithoutNoise()
        {
            // water.frag:45-53 sin el ruido (n = 0), en z de mundo.
            for (var i = 0; i < 50; i++)
            {
                var x = -6 + i * 0.37;
                var zw = 3 + i * 11.3;
                var t = i * 0.41;
                const double S = 4.0;
                var expected = Math.Sin(zw / S + t * 0.6) * 0.6 + Math.Sin((zw * 0.7 + x * 1.3) / S + t * 0.9) * 0.4;
                Assert.That(CoastSwell.Height(x, zw, t, S), Is.EqualTo(expected));
            }

            Assert.That(CoastSwell.ColorFactor(1, 0.8), Is.EqualTo(1.2).Within(1e-12), "± 20 %");
            Assert.That(CoastSwell.ColorFactor(-1, 0.8), Is.EqualTo(0.8).Within(1e-12));
        }

        [Test, Category("AC-SCN-12-01")]
        public void PhasesInTapeEqualWorldZUpTo42Km()
        {
            foreach (var d in new[] { 0.0, 920, 4096, 12345.6789, 42195 })
            {
                foreach (var t in new[] { 0.0, 3.2, 1234.5 })
                {
                    CoastSwell.Phases(d, t, 4, out var p1, out var p2);
                    Assert.That(p1, Is.InRange(0, 2 * Math.PI));
                    Assert.That(p2, Is.InRange(0, 2 * Math.PI));
                    foreach (var relZ in new[] { 0.6, 9.5, 37.25, 150.0 })
                    {
                        Assert.That(CoastSwell.HeightAt(0.7, relZ, 4, p1, p2), Is.EqualTo(CoastSwell.Height(0.7, relZ + d, t, 4)).Within(1e-9),
                            "d " + d + ", t " + t + ", relZ " + relZ);
                    }
                }
            }
        }

        [Test, Category("AC-SCN-06-04")]
        public void TheHullRidesTheSwellWithinTwelveCentimeters()
        {
            var maxHeave = 0.0;
            var maxPitch = 0.0;
            for (var i = 0; i < 2000; i++)
            {
                CoastSwell.SampleHull(i * 1.37, i * 0.05, 4, out var heave, out var pitch);
                maxHeave = Math.Max(maxHeave, Math.Abs(heave));
                maxPitch = Math.Max(maxPitch, Math.Abs(pitch));
                Assert.That(Math.Abs(heave), Is.LessThanOrEqualTo(0.15 + 1e-12), "0,15·h con |h| ≤ 1");
            }

            Assert.That(maxHeave, Is.GreaterThan(0.08), "la arfada se nota (± 0,12 m)");
            Assert.That(maxPitch, Is.GreaterThan(0.005).And.LessThan(Math.Atan(0.3 / 8.2) + 1e-9));
            CoastSwell.SampleHull(100, 2, 4, out var h1, out var p1);
            var bow = 0.15 * CoastSwell.Height(0, 113.5, 2, 4);
            var mid = 0.15 * CoastSwell.Height(0, 109.5, 2, 4);
            var stern = 0.15 * CoastSwell.Height(0, 105.3, 2, 4);
            Assert.That(h1, Is.EqualTo((bow + mid + stern) / 3).Within(1e-9));
            Assert.That(p1, Is.EqualTo(Math.Atan((bow - stern) / 8.2)).Within(1e-9));
        }

        [Test, Category("AC-SCN-06-04"), Category("AC-SCN-12-01")]
        public void TheSessionAddsTheFilteredSwellOnlyOnTheCoastAndNeverTouchesTheDistance()
        {
            foreach (var id in new[] { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var s = new SceneSession(EnvironmentCatalog.Create(id), SceneSessionOptions.Default);
                var frame = SceneFrame.Create();
                s.Open(0);
                var inputs = Inputs(0);
                s.Tick(inputs, ref frame);
                s.Advance(inputs, 5, ref frame);
                Assert.That(frame.Swell, Is.EqualTo(id == EnvironmentId.Coast), id + ": la ola solo en la costa");
                if (id == EnvironmentId.Coast)
                {
                    Assert.That(Math.Abs(frame.Rower.SwellHeaveM) + Math.Abs(frame.Rower.SwellPitchRad), Is.GreaterThan(0), "el casco sigue la ola, también quieto");
                    Assert.That(frame.SwellAmplitude, Is.EqualTo(0.8));
                }
                else
                {
                    Assert.That(frame.Rower.SwellHeaveM, Is.EqualTo(0));
                    Assert.That(frame.Rower.SwellPitchRad, Is.EqualTo(0));
                }

                Assert.That(s.State.Distance, Is.EqualTo(0), id + ": la ola no cambia la distancia (AC-SCN-08-11)");
            }
        }

        [Test, Category("AC-SCN-06-04")]
        public void TheSwellFilterHasATauOfThreeTenths()
        {
            var f = default(SwellFilter);
            // Tres pasos de 0,1 s (el paso acotado de SceneState): 0,3 s = un τ.
            f.Step(0.1, 1, 1);
            f.Step(0.1, 1, 1);
            f.Step(0.1, 1, 1);
            Assert.That(f.HeaveM, Is.EqualTo(1 - Math.Exp(-1)).Within(1e-12));
            Assert.That(f.PitchRad, Is.EqualTo(1 - Math.Exp(-1)).Within(1e-12));
            Assert.That(EnvironmentProfile.SwellThreshold, Is.EqualTo(0.5));
            Assert.That(EnvironmentCatalog.HasSwell(new CoastEnvironment()) && !EnvironmentCatalog.HasSwell(new RiverEnvironment())
                && !EnvironmentCatalog.HasSwell(new RegattaEnvironment()) && !EnvironmentCatalog.HasSwell(new LakeEnvironment()), Is.True);
        }

        internal static SceneTickInputs Inputs(double now)
        {
            var local = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
            return new SceneTickInputs(now, local, local, default, 12, false, 390, 844, 0);
        }
    }

    // BRC · Rolido y cámara por escenario (AC-SCN-06-04).
    [TestFixture]
    public sealed class BoatRollAndCameraTests
    {
        [Test, Category("AC-SCN-06-04")]
        public void RollKPerEnvironment()
        {
            Assert.That(EnvironmentCatalog.Profile(EnvironmentId.Lake).RollK, Is.EqualTo(0.5));
            Assert.That(EnvironmentCatalog.Profile(EnvironmentId.River).RollK, Is.EqualTo(0.8));
            Assert.That(EnvironmentCatalog.Profile(EnvironmentId.Coast).RollK, Is.EqualTo(2.0));
            Assert.That(EnvironmentCatalog.Profile(EnvironmentId.Regatta).RollK, Is.EqualTo(0.4));
            Assert.That(BoatMotion.LakeRollK, Is.EqualTo(EnvironmentCatalog.LakeRollK));
            foreach (var id in new[] { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Coast, EnvironmentId.Regatta })
            {
                var s = new SceneSession(EnvironmentCatalog.Create(id), SceneSessionOptions.Default);
                var frame = SceneFrame.Create();
                s.Open(0);
                s.FreezeSceneTimeForTest(2.5);
                s.Tick(CoastSwellTests.Inputs(0.016), ref frame);
                Assert.That(frame.Rower.Boat.RollRad, Is.EqualTo(BoatMotion.Roll(2.5, s.Profile.RollK)).Within(1e-15), id.ToString());
            }
        }

        [Test, Category("AC-SCN-06-04")]
        public void TheCoastBreathesOnePointEightTimesWithACapOfFivePointFour()
        {
            var lake = BreathingSpec.Lake;
            var coast = lake.Scaled(EnvironmentCatalog.Profile(EnvironmentId.Coast).BreathingGain);
            Assert.That(coast.RollDeg, Is.EqualTo(lake.RollDeg * 1.8).Within(1e-15));
            Assert.That(coast.PitchDeg, Is.EqualTo(lake.PitchDeg * 1.8).Within(1e-15));
            Assert.That(coast.LateralA, Is.EqualTo(lake.LateralA * 1.8).Within(1e-15));
            Assert.That(coast.Vertical, Is.EqualTo(lake.Vertical * 1.8).Within(1e-15));
            Assert.That(coast.RollFreq, Is.EqualTo(lake.RollFreq));
            var same = lake.Scaled(1);
            Assert.That(same.RollDeg, Is.EqualTo(lake.RollDeg), "× 1 es idéntica (el lago no cambia)");
            Assert.That(EnvironmentCatalog.Profile(EnvironmentId.Coast).BreathingCapDp, Is.EqualTo(5.4));
            Assert.That(EnvironmentCatalog.Profile(EnvironmentId.Coast).SoftPitchCapDp, Is.EqualTo(5.35).Within(1e-12));
            foreach (var id in new[] { EnvironmentId.Lake, EnvironmentId.River, EnvironmentId.Regatta })
            {
                Assert.That(EnvironmentCatalog.Profile(id).BreathingCapDp, Is.EqualTo(3.0));
                Assert.That(EnvironmentCatalog.Profile(id).BreathingGain, Is.EqualTo(1.0));
                Assert.That(EnvironmentCatalog.Profile(id).SoftPitchCapDp, Is.EqualTo(Core.Design.MotionSpecs.CameraHorizonCapDp).Within(1e-12));
            }
        }

        [Test, Category("AC-SCN-06-04")]
        public void TheHorizonStaysWithinTheEnvironmentCapAndReduceMotionStillsTheCamera()
        {
            foreach (var id in new[] { EnvironmentId.Coast, EnvironmentId.River })
            {
                var cap = EnvironmentCatalog.Profile(id).BreathingCapDp;
                foreach (var size in SceneSeeds.Sizes)
                {
                    var s = new SceneSession(EnvironmentCatalog.Create(id), SceneSessionOptions.Default);
                    var frame = SceneFrame.Create();
                    s.Open(0);
                    s.Camera.ShowInPlace(0);
                    var local = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
                    var worst = 0.0;
                    for (var i = 1; i < 900; i++)
                    {
                        var inputs = new SceneTickInputs(i / 60.0, local, local, default, 12, false, size.Width, size.Height, 0);
                        s.Tick(inputs, ref frame);
                        if (frame.CameraMode == CameraMode.Contract)
                        {
                            worst = Math.Max(worst, frame.HorizonDeviationDp);
                        }
                    }

                    Assert.That(worst, Is.LessThanOrEqualTo(cap + 1e-9), id + " " + size.Name);
                }
            }

            var still = new SceneSession(new CoastEnvironment(), SceneSessionOptions.Default);
            var f = SceneFrame.Create();
            still.Open(0);
            still.Camera.ShowInPlace(0);
            var at = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
            for (var i = 1; i < 300; i++)
            {
                still.Tick(new SceneTickInputs(i / 60.0, at, at, default, 12, true, 390, 844, 0), ref f);
            }

            Assert.That(f.BreathingMultiplier, Is.EqualTo(0), "reducir movimiento: la cámara × 0");
            Assert.That(Math.Abs(f.Rower.SwellHeaveM), Is.GreaterThan(0), "el bote sigue la ola");
        }
    }
}
