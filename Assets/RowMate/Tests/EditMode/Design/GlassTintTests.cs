using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // GLT · El tinte del vidrio desenfocado (ART_BIBLE §3.2; P-04; AC-6B-23, -24; 06b-agua-avanzada/02-tech-design §4.4, DT6b-09): el alfa
    // lerp(0,72; 0,86; S(0,10; 0,70; L)), el compuesto monótono en L que con L = 1 es el respaldo @86 % sobre blanco, y el contraste del
    // peor caso igual al de hoy (recorre L de 0 a 1 con paso 1/255).
    [TestFixture]
    public sealed class GlassTintTests
    {
        [Test, Category("AC-6B-23")]
        public void Alpha()
        {
            Assert.That(GlassTint.Alpha(0.10), Is.EqualTo(0.72).Within(1e-12));
            Assert.That(GlassTint.Alpha(0.40), Is.EqualTo(0.79).Within(1e-12));
            Assert.That(GlassTint.Alpha(0.70), Is.EqualTo(0.86).Within(1e-12));
            Assert.That(GlassTint.Alpha(0), Is.EqualTo(0.72));
            Assert.That(GlassTint.Alpha(1), Is.EqualTo(0.86));
            Assert.That(GlassTint.Alpha(double.NaN), Is.EqualTo(0.72), "sin dato, el más transparente acotado");
            Assert.That(Tokens.GlassFallback.Alpha, Is.EqualTo(GlassTint.AlphaMax), "el respaldo plano es el alfa más alto");
        }

        [Test, Category("AC-6B-24")]
        public void RegionalCompositeNeverPassesTheWorstCase()
        {
            // QA-V6b-04: con el α de la zona, ningún par (píxel; zona) da un compuesto más claro que el del peor caso (L = 1, el respaldo sobre
            // blanco): el contraste del texto blanco sigue ≥ el de ContrastWorstCase.
            var worst = GlassTint.RegionalCompositeValue(1, 1);
            for (var p = 0.0; p <= 1.0001; p += 0.01)
            {
                for (var r = 0.0; r <= 1.0001; r += 0.01)
                {
                    Assert.That(GlassTint.RegionalCompositeValue(p, r), Is.LessThanOrEqualTo(worst + 1e-9), "píxel " + p + ", zona " + r);
                }
            }

            Assert.That(GlassTint.RegionalAlpha(1, 0), Is.EqualTo(GlassTint.AlphaMax).Within(1e-12), "un destello blanco sobre agua oscura: α pleno");
        }

        [Test, Category("AC-6B-24")]
        public void TheSternShowsThroughTheDialog()
        {
            // QA-V6b-04 (antes: +4-5 %): la popa desenfocada (L 0,6) sobre agua (L 0,45) en una zona de agua, con el α de la zona: ≥ +10 %; con
            // el α del píxel (el de antes) no llegaba.
            var stern = GlassTint.RegionalCompositeValue(0.6, 0.47);
            var water = GlassTint.RegionalCompositeValue(0.45, 0.47);
            Assert.That(stern / water - 1, Is.GreaterThanOrEqualTo(0.10));
            var perPixel = (GlassTint.Alpha(0.6) * 0.046 + (1 - GlassTint.Alpha(0.6)) * 0.6) / (GlassTint.Alpha(0.45) * 0.046 + (1 - GlassTint.Alpha(0.45)) * 0.45) - 1;
            Assert.That(perPixel, Is.LessThan(0.10), "el α del píxel aplasta los medios tonos");
        }

        [Test, Category("AC-6B-24")]
        public void CompositeIsMonotonic()
        {
            var last = -1.0;
            for (var i = 0; i <= 255; i++)
            {
                var l = GlassTint.CompositeL(i / 255.0);
                Assert.That(l, Is.GreaterThanOrEqualTo(last), "L = " + i + "/255");
                last = l;
            }

            // Con L = 1, exactamente el respaldo sobre blanco (Contrast.WorstGlassBackground).
            var worst = Contrast.WorstGlassBackground();
            var c = GlassTint.Composite(1);
            Assert.That(c.R, Is.EqualTo(worst.R).Within(1e-9));
            Assert.That(c.G, Is.EqualTo(worst.G).Within(1e-9));
            Assert.That(c.B, Is.EqualTo(worst.B).Within(1e-9));
        }

        [Test, Category("AC-6B-24")]
        public void ContrastWorstCase()
        {
            // Sobre una nube blanca: ≥ 13,5:1 al blanco, ≥ 7,0:1 a las métricas *.hud y ≥ 4,5:1 a la familia roja (D-ART-01), igual que
            // el respaldo de @86 %; con cualquier fondo, nunca peor.
            var white = (255.0, 255.0, 255.0);
            // El respaldo de hoy sobre blanco da 13,493:1 (ContrastTests: text.primary sobre el vidrio); el "≥ 13,5" de la spec es ese número
            // redondeado. Se exige lo mismo que hoy (nunca peor) y el 13,49 que da el respaldo.
            var today = GlassTint.ContrastOver(1, white);
            Assert.That(today, Is.EqualTo(13.493).Within(0.0006));
            var hud = new[]
            {
                Tokens.MetricSplitHud, Tokens.MetricSpmHud, Tokens.MetricWattsHud, Tokens.MetricDistanceHud, Tokens.MetricTimeHud, Tokens.MetricKcalHud,
            };
            var red = new[] { Tokens.MetricHrHud, Tokens.StateOffTargetHud };
            for (var i = 0; i <= 255; i++)
            {
                var l = i / 255.0;
                Assert.That(GlassTint.ContrastOver(l, white), Is.GreaterThanOrEqualTo(today - 1e-12).And.GreaterThanOrEqualTo(13.49), "blanco con L = " + i + "/255");
                foreach (var token in hud)
                {
                    Assert.That(GlassTint.ContrastOver(l, token), Is.GreaterThanOrEqualTo(7.0), token.Name + " con L = " + i + "/255");
                }

                foreach (var token in red)
                {
                    Assert.That(GlassTint.ContrastOver(l, token), Is.GreaterThanOrEqualTo(4.5), token.Name + " con L = " + i + "/255");
                }

                Assert.That(GlassTint.CompositeL(l), Is.LessThanOrEqualTo(GlassTint.CompositeL(1) + 1e-15), "el peor caso es el de hoy");
            }
        }
    }
}
