using System;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Scene;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // STR · Las 80 estrellas del legacy (sky_painter.dart:49-66; AC-SCN-11-03, -04; Q-62, DV-53, DN6a-05; LT-82). Las posiciones
    // contra el oráculo (las que dibuja SkyPainter: drawCircle recibe el centro y el radio en double), bit a bit.
    [TestFixture]
    public sealed class StarFieldTests
    {
        [Test, Category("AC-SCN-11-03")]
        public void TheEightyMatchTheOracleInSevenSizes()
        {
            var points = new StarPoint[StarField.Count];
            var rnd = new DartRandom(0);
            var rows = GoldenVectors.CheckRows("scene_sky.json", "stars", row =>
            {
                var cam = SceneSeeds.Camera(row.Text("size"));
                StarField.Layout(cam.Width, cam.HorizonY, points, rnd);
                var p = points[(int)row.Int("i")];
                return Same(p.X, row.F64("x")) && Same(p.Y, row.F64("y")) && Same(p.Radius, row.F64("r"))
                    ? null
                    : "(" + SceneGolden.F(p.X) + "; " + SceneGolden.F(p.Y) + "; " + SceneGolden.F(p.Radius) + ")";
            });
            Assert.That(rows, Is.EqualTo(80 * 7));
        }

        [Test, Category("AC-SCN-11-03")]
        public void TheFirstStarInV390()
        {
            var points = new StarPoint[StarField.Count];
            StarField.Layout(390, 337.6, points);
            Assert.That(points[0].X, Is.EqualTo(58.86).Within(0.005));
            Assert.That(points[0].Y, Is.EqualTo(173.37).Within(0.005));
            foreach (var p in points)
            {
                Assert.That(p.Radius, Is.InRange(0.6, 1.6));
                Assert.That(p.Y, Is.InRange(0.0, 337.6 * 0.85), "en el 85 % de arriba del cielo");
                Assert.That(p.X, Is.InRange(0.0, 390.0));
            }

            Assert.That(StarField.Seed, Is.EqualTo(42));
            Assert.That(StarField.Count, Is.EqualTo(80));
        }

        // QA-6a-PAR-09: con la hora al minuto armada como el legacy, 6:12 sí, 6:13 no, 17:59 no, 18:00 sí; 15:00 no.
        [TestCase(6, 12, true, Category = "AC-SCN-11-04")]
        [TestCase(6, 13, false, Category = "AC-SCN-11-04")]
        [TestCase(17, 59, false, Category = "AC-SCN-11-04")]
        [TestCase(18, 0, true, Category = "AC-SCN-11-04")]
        [TestCase(15, 0, false, Category = "AC-SCN-11-04")]
        [TestCase(12, 0, false, Category = "AC-SCN-11-04")]
        [TestCase(19, 0, true, Category = "AC-SCN-11-04")]
        [TestCase(22, 0, true, Category = "AC-SCN-11-04")]
        [TestCase(5, 0, true, Category = "AC-SCN-11-04")]
        public void TheyShowOnlyWithAmbientUpTo085(int hour, int minute, bool shown)
        {
            var h = SceneSeeds.HourOfMinute(hour * 60 + minute);
            var ambient = SceneTimeOfDay.PaletteFor(h).Ambient;
            Assert.That(StarField.IsShown(ambient), Is.EqualTo(shown), "ambiente " + SceneGolden.F(ambient));
            Assert.That(StarField.Visibility(ambient) > 0, Is.EqualTo(shown));
        }

        [Test, Category("AC-SCN-11-04")]
        public void TheExactAmbientsOfTheEdges()
        {
            // La spec escribe 0,8499999999999999 para las 6:12; el legacy (el oráculo, bit a bit) da 0,85 exacto, como a las 18:00: las
            // dos son visibles (≤ 0,85) y la regla no cambia.
            Assert.That(SceneTimeOfDay.PaletteFor(SceneSeeds.HourOfMinute(6 * 60 + 12)).Ambient, Is.EqualTo(0.85));
            Assert.That(SceneTimeOfDay.PaletteFor(18).Ambient, Is.EqualTo(0.85));
            Assert.That(StarField.LegacyDarkness(0.85), Is.EqualTo(0.23).Within(0.001));
            Assert.That(StarField.LegacyDarkness(0.35), Is.EqualTo(1));
            Assert.That(StarField.LegacyDarkness(1), Is.EqualTo(0));
            Assert.That(StarField.LegacyDarkness(0.925), Is.EqualTo(0.115).Within(0.001), "el legacy a las 15: 80 casi invisibles");
        }

        // legacy: test/scene/sky_shore_painter_test.dart:39 'las estrellas aparecen apenas oscurece (19 h), no solo de noche cerrada'
        [TestCase(TestName = "las estrellas aparecen apenas oscurece (19 h), no solo de noche cerrada", Category = "LT-82,AC-SCN-11-04")]
        public void StarsAppearAsSoonAsItDarkens()
        {
            // El legacy cuenta los círculos del cielo; el port cuenta las estrellas visibles: 80 contra 0 (tiempo 0, V390).
            Assert.That(Visible(19) - Visible(12), Is.GreaterThanOrEqualTo(80));
        }

        [Test, Category("AC-SCN-11-04")]
        public void TheAlphaIsTheLegacyFormulaInSteadyState()
        {
            foreach (var hour in new[] { 19.0, 22.0, 2.0 })
            {
                var a = SceneTimeOfDay.PaletteFor(hour).Ambient;
                for (var i = 0; i < 80; i++)
                {
                    var t = 3.2;
                    var twinkle = 0.5 + 0.5 * Math.Sin(t * 2 + i);
                    Assert.That(StarField.Alpha(i, t, a, 1), Is.EqualTo((0.4 + 0.5 * twinkle) * StarField.LegacyDarkness(a)).Within(1e-15));
                }
            }

            Assert.That(StarField.Alpha(3, 1, 0.35, 0), Is.EqualTo(0), "con el fundido en 0, nada");
        }

        [Test, Category("AC-SCN-11-04")]
        public void TheyTwinkleAlsoInPause()
        {
            // El titileo es del tiempo de escena, que sigue en pausa (AC-SCN-01-03).
            Assert.That(StarField.Alpha(5, 10, 0.35, 1), Is.Not.EqualTo(StarField.Alpha(5, 10.5, 0.35, 1)));
        }

        [Test, Category("AC-SCN-11-04")]
        public void TheFaderCrossesTheThresholdInNineTenths()
        {
            // DN6a-05, DV-53: a las 18:00 entran de 0 a la oscuridad de ese cuadro en 0,9 s (ease.move), y vuelta.
            var f = new StarFader();
            f.Snap(false);
            Assert.That(f.Level, Is.EqualTo(0));
            for (var i = 0; i < 27; i++)
            {
                f.Step(true, 1.0 / 60);
            }

            Assert.That(f.Level, Is.EqualTo(Easing.Evaluate(Tokens.EaseMove, 0.5)).Within(1e-9), "a los 0,45 s, la mitad de la curva");
            for (var i = 0; i < 27; i++)
            {
                f.Step(true, 1.0 / 60);
            }

            Assert.That(f.Level, Is.EqualTo(1).Within(1e-9));
            var alpha = StarField.Alpha(0, 0, 0.85, f.Level);
            Assert.That(alpha, Is.GreaterThan(0).And.LessThanOrEqualTo(0.9 * 0.231));
            for (var i = 0; i < 54; i++)
            {
                f.Step(false, 1.0 / 60);
            }

            Assert.That(f.Level, Is.EqualTo(0).Within(1e-9));
            Assert.That(StarFader.Seconds, Is.EqualTo(0.9));
        }

        [Test, Category("AC-SCN-11-04")]
        public void TheFaderSnapsAndReversesFromWhereItIs()
        {
            var f = new StarFader();
            f.Snap(true);
            Assert.That(f.Level, Is.EqualTo(1), "al abrir, al volver del segundo plano y con ForceHourForTest: el valor final");
            f.Step(false, 0.3);
            var mid = f.Level;
            f.Step(true, 0);
            Assert.That(f.Level, Is.EqualTo(mid), "un cambio a mitad de camino sale de donde está");
            f.Step(true, 0.3);
            Assert.That(f.Level, Is.EqualTo(1).Within(1e-9));
        }

        private static int Visible(double hour)
        {
            var a = SceneTimeOfDay.PaletteFor(hour).Ambient;
            var count = 0;
            for (var i = 0; i < StarField.Count; i++)
            {
                count += StarField.Alpha(i, 0, a, StarField.IsShown(a) ? 1 : 0) > 0 ? 1 : 0;
            }

            return count;
        }

        private static bool Same(double a, double b)
        {
            return BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
        }
    }
}
