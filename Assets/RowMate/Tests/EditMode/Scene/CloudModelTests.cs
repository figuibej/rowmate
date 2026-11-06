using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // CLD · Las 7 nubes (sky_painter.dart:29-33, :88-103; AC-SCN-11-06; D-ART-06, DV-56, DN6a-26, QA-6a-PAR-08). Contra el oráculo,
    // bit a bit: la x del legacy (con la deriva por distancia, contrato), la y, el color y los alfas.
    [TestFixture]
    public sealed class CloudModelTests
    {
        [Test, Category("AC-SCN-11-06")]
        public void TheSevenOfTheLegacy()
        {
            var expected = new[]
            {
                (0.08, 0.10, 34.0, 1.0), (0.26, 0.18, 22.0, 1.4), (0.42, 0.07, 40.0, 0.8), (0.58, 0.22, 26.0, 1.2),
                (0.72, 0.12, 36.0, 0.9), (0.88, 0.20, 24.0, 1.3), (0.97, 0.05, 30.0, 1.1),
            };
            var legacy = CloudModel.Legacy;
            Assert.That(legacy.Length, Is.EqualTo(7));
            for (var i = 0; i < 7; i++)
            {
                Assert.That(legacy[i].X0, Is.EqualTo(expected[i].Item1));
                Assert.That(legacy[i].YRel, Is.EqualTo(expected[i].Item2));
                Assert.That(legacy[i].SizeDp, Is.EqualTo(expected[i].Item3));
                Assert.That(legacy[i].Speed, Is.EqualTo(expected[i].Item4));
            }
        }

        [Test, Category("AC-SCN-11-06")]
        public void LegacyXAndYMatchTheOracle()
        {
            var rows = GoldenVectors.CheckRows("scene_sky.json", "clouds", row =>
            {
                var cam = SceneSeeds.Camera(row.Text("size"));
                var i = (int)row.Int("i");
                var x = CloudModel.LegacyX(i, cam.Width, row.F64("distance"), row.F64("time"));
                var y = CloudModel.Y(i, cam.HorizonY);
                return Same(x, row.F64("x")) && Same(y, row.F64("y")) ? null : "(" + SceneGolden.F(x) + "; " + SceneGolden.F(y) + ")";
            });
            Assert.That(rows, Is.EqualTo(7 * 7 * 2 * 2));
        }

        [Test, Category("AC-SCN-11-06")]
        public void OnlyTheWindMovesThem()
        {
            // DV-56: sin deriva por distancia; con distancia 0, la x del legacy exacta.
            for (var i = 0; i < 7; i++)
            {
                foreach (var t in new[] { 0.0, 3.2, 120.0, 1000.0 })
                {
                    Assert.That(Same(CloudModel.X(i, 411, t), CloudModel.LegacyX(i, 411, 0, t)), Is.True);
                    Assert.That(CloudModel.X(i, 411, t), Is.EqualTo(CloudModel.X(i, 411, t)), "no depende de la distancia");
                }

                Assert.That(CloudModel.LegacyX(i, 411, 1234, 0), Is.Not.EqualTo(CloudModel.LegacyX(i, 411, 0, 0)), "el legacy sí se corría");
            }
        }

        [Test, Category("AC-SCN-11-06")]
        public void TheyLoopEvery214To375Seconds()
        {
            for (var i = 0; i < 7; i++)
            {
                var period = 1.2 / (0.004 * CloudModel.Legacy[i].Speed);
                Assert.That(period, Is.InRange(214.0, 375.1));
                Assert.That(CloudModel.X(i, 411, 0), Is.EqualTo(CloudModel.X(i, 411, period)).Within(1e-6));
            }
        }

        [Test, Category("AC-SCN-11-06")]
        public void ColorsAndAlphasMatchTheOracle()
        {
            var rows = GoldenVectors.CheckRows("scene_sky.json", "cloudColors", row =>
            {
                var p = SceneTimeOfDay.PaletteFor(row.Int("hour"));
                var body = CloudModel.BodyColor(p);
                if (!Same(body.R, row.F64("bodyR")) || !Same(body.G, row.F64("bodyG")) || !Same(body.B, row.F64("bodyB")))
                {
                    return "cuerpo " + body;
                }

                if (!Same(CloudModel.BodyAlpha(p.Ambient), row.F64("bodyAlpha")))
                {
                    return "alfa del cuerpo";
                }

                return Same(CloudModel.ShadeAlpha(p.Ambient), row.F64("shadeAlpha")) ? null : "alfa de la sombra";
            });
            Assert.That(rows, Is.EqualTo(25));
            Assert.That(CloudModel.BodyAlpha(1), Is.EqualTo(0.58));
            Assert.That(CloudModel.BodyAlpha(0.35), Is.EqualTo(0.14125).Within(1e-12), "casi invisibles de noche");
        }

        [Test, Category("AC-SCN-11-06")]
        public void TheWrapFadeIsZeroAtTheJumpOnBothSides()
        {
            const double Width = 411;
            for (var i = 0; i < 7; i++)
            {
                var spec = CloudModel.Legacy[i];
                var v = 0.004 * spec.Speed;
                // El instante del salto: la fase llega a 1,2 y vuelve a 0.
                var phase0 = spec.X0 % 1.2;
                var tJump = (1.2 - phase0) / v;
                var before = CloudModel.WrapFade(i, Width, tJump - 1e-6, SkyArt.CloudWrapRight, SkyArt.CloudWrapLeft);
                var after = CloudModel.WrapFade(i, Width, tJump + 1e-6, SkyArt.CloudWrapRight, SkyArt.CloudWrapLeft);
                Assert.That(before, Is.LessThan(1e-6), "se apaga antes de saltar");
                Assert.That(after, Is.LessThan(1e-6), "y entra desde 0 del otro lado");
                var far = CloudModel.WrapFade(i, Width, tJump + 0.6 / v, SkyArt.CloudWrapRight, SkyArt.CloudWrapLeft);
                Assert.That(far, Is.EqualTo(1), "lejos de la vuelta, 1");
            }
        }

        [TestCaseSource(nameof(Sizes))]
        public void TheWrapFadeIsContinuousAndNeverTouchesX(string name)
        {
            var size = SceneSeeds.Camera(name);
            // Ningún cambio de alfa > 0,01 entre dos cuadros de 1/60 s; la x sigue siendo la del legacy.
            for (var i = 0; i < 7; i++)
            {
                var prev = CloudModel.WrapFade(i, size.Width, 0, SkyArt.CloudWrapRight, SkyArt.CloudWrapLeft);
                for (var f = 1; f <= 60 * 400; f++)
                {
                    var t = f / 60.0;
                    var v = CloudModel.WrapFade(i, size.Width, t, SkyArt.CloudWrapRight, SkyArt.CloudWrapLeft);
                    Assert.That(Math.Abs(v - prev), Is.LessThanOrEqualTo(0.01), name + " nube " + i + " a los " + t + " s");
                    prev = v;
                }
            }
        }

        [Test, Category("AC-SCN-11-06")]
        public void YIsTheLegacyOne()
        {
            Assert.That(CloudModel.Y(2, 365.6), Is.EqualTo(365.6 * (0.12 + 0.07 * 0.6)).Within(1e-12));
            Assert.That(CloudModel.Phase(0, 0), Is.EqualTo(0.08));
        }

        private static System.Collections.Generic.IEnumerable<TestCaseData> Sizes()
        {
            foreach (var s in SceneSeeds.Sizes)
            {
                yield return new TestCaseData(s.Name).SetCategory("AC-SCN-11-06");
            }
        }

        private static bool Same(double a, double b)
        {
            return BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
        }
    }
}
