using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // BSR · La sombra del bote (AC-SCN-08-06; DT7-08; DA7-10; boat_painter.dart:118-130): el modo por nivel y por fuerza de la luz,
    // el lado del legacy (astro centrado → izquierda), el lado por hora y el fundido continuo.
    [TestFixture]
    public sealed class BoatShadowRuleTests
    {
        [Test, Category("AC-SCN-08-06")]
        public void TheModeByTierAndLight()
        {
            Assert.That(BoatShadowRule.Mode(true, 0.5), Is.EqualTo(BoatShadowMode.Map));
            Assert.That(BoatShadowRule.Mode(true, 0.05), Is.EqualTo(BoatShadowMode.Map));
            Assert.That(BoatShadowRule.Mode(true, 0.049), Is.EqualTo(BoatShadowMode.Oval), "de noche en Medio (fuerza 0)");
            Assert.That(BoatShadowRule.Mode(false, 1), Is.EqualTo(BoatShadowMode.Oval), "Bajo: siempre el óvalo");
        }

        [Test, Category("AC-SCN-08-06")]
        public void TheLegacySide()
        {
            Assert.That(BoatShadowRule.LegacySide(100, 200), Is.EqualTo(0.35), "astro a la izquierda → sombra a la derecha");
            Assert.That(BoatShadowRule.LegacySide(300, 200), Is.EqualTo(-0.35));
            Assert.That(BoatShadowRule.LegacySide(200, 200), Is.EqualTo(-0.35), "centrado → izquierda");
            Assert.That(BoatShadowRule.OvalAlpha(0.8), Is.EqualTo(0.08 * 0.8).Within(1e-15));
            Assert.That(BoatShadowRule.OvalSternZ, Is.EqualTo(5.8).Within(1e-12));
            Assert.That(BoatShadowRule.OvalBowZ, Is.EqualTo(12.5).Within(1e-12));
            Assert.That(BoatShadowRule.OvalWidth, Is.EqualTo(0.5));
        }

        [Test, Category("AC-SCN-08-06")]
        public void TheSideByHourInTh()
        {
            var cam = new SceneCamera(914, 411);
            var nine = BoatShadowRule.LegacySide(SceneTimeOfDay.LightFor(9, cam).X, cam.CenterX);
            var five = BoatShadowRule.LegacySide(SceneTimeOfDay.LightFor(17, cam).X, cam.CenterX);
            Assert.That(nine, Is.EqualTo(-five), "a las 9 y a las 17 la sombra cae de lados opuestos");
            foreach (var hour in new[] { 12.0, 22.0 })
            {
                var side = BoatShadowRule.LegacySide(SceneTimeOfDay.LightFor(hour, cam).X, cam.CenterX);
                Assert.That(Math.Abs(side), Is.EqualTo(0.35));
            }
        }

        [Test, Category("AC-SCN-08-06")]
        public void TheSideFadesContinuouslyOrInstantly()
        {
            var fader = default(ShadowSideFader);
            fader.Step(0.35, 0, false);
            Assert.That(fader.Value, Is.EqualTo(0.35), "al abrir, en el acto");
            var previous = fader.Value;
            for (var i = 0; i < 100; i++)
            {
                fader.Step(-0.35, 1.0 / 60, false);
                Assert.That(fader.Value, Is.LessThanOrEqualTo(previous));
                Assert.That(previous - fader.Value, Is.LessThan(0.03), "sin saltos");
                previous = fader.Value;
            }

            Assert.That(fader.Value, Is.EqualTo(-0.35).Within(1e-9), "en 1,5 s");
            fader.Step(0.35, 1.0 / 60, true);
            Assert.That(fader.Value, Is.EqualTo(0.35), "con la hora forzada, en el acto");
        }

        [Test, Category("AC-SCN-08-06")]
        public void TheSessionPicksTheModeByTierAndHour()
        {
            var s = new StrokeTestSession(914, 411);
            s.Step(StrokeTestSession.Rowing(24));
            Assert.That(s.Rower.ShadowMode, Is.EqualTo(BoatShadowMode.Map), "Alto a las 12");
            s.Session.SetRenderTier(RowerRenderTier.Low);
            s.Step(StrokeTestSession.Rowing(24));
            Assert.That(s.Rower.ShadowMode, Is.EqualTo(BoatShadowMode.Oval), "Bajo");
            Assert.That(s.Rower.ShadowAlpha, Is.EqualTo(0.08 * s.Frame.Palette.Ambient).Within(1e-12));
            Assert.That(s.Rower.ShadowSide, Is.EqualTo(BoatShadowRule.LegacySide(s.Frame.LegacyLight.X, 457)));
        }
    }
}
