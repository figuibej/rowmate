using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Design
{
    // AC-BASE-05 (R-10, D-ART-01): contraste del HUD contra el peor fondo del vidrio sin desenfoque y del texto de interfaz,
    // con el método de 02-tech-design §4.4 (mezcla sRGB con alfa sin redondear). Imprime además el cálculo con el fondo
    // cuantizado a 8 bits y la mezcla lineal (R-T1), solo informativos.
    [TestFixture]
    public sealed class ContrastTests
    {
        private const string White = "white";
        private const string WorstGlass = "worstGlass";

        [Test, Category("AC-BASE-05")]
        public void WorstGlassIs2A2F37()
        {
            var glass = Contrast.WorstGlassBackground();
            Assert.That(glass.R, Is.EqualTo(41.72).Within(1e-9));
            Assert.That(glass.G, Is.EqualTo(46.88).Within(1e-9));
            Assert.That(glass.B, Is.EqualTo(54.62).Within(1e-9));
            var rounded = new Rgb((byte)DartMath.Round(glass.R), (byte)DartMath.Round(glass.G), (byte)DartMath.Round(glass.B));
            Assert.That(rounded.ToHex(), Is.EqualTo("#2A2F37"));
            Assert.That(Contrast.RelativeLuminance(glass.R, glass.G, glass.B), Is.EqualTo(0.02782).Within(5e-6));
        }

        [Test, Category("AC-BASE-05")]
        public void HudAgainstWorstGlass()
        {
            var glass = Contrast.WorstGlassBackground();
            var quantized = (DartMath.Round(glass.R), DartMath.Round(glass.G), DartMath.Round(glass.B));
            var linearGlass = Contrast.CompositeLinear(Tokens.GlassFallback, (255.0, 255.0, 255.0));
            var checkedCount = 0;
            TestContext.Out.WriteLine("token | rol | umbral | sRGB sin redondear | fondo #2A2F37 | mezcla lineal (R-T1)");
            foreach (var token in TokenCatalog.All)
            {
                if (!(token is ColorToken color))
                {
                    continue;
                }

                var threshold = HudThreshold(color.Role);
                if (threshold == 0)
                {
                    continue;
                }

                checkedCount++;
                var ratio = Contrast.RatioOver(color, glass);
                var ratioQuantized = Contrast.RatioOver(color, (quantized.Item1, quantized.Item2, quantized.Item3));
                var ratioLinear = Contrast.RatioOver(color, linearGlass);
                TestContext.Out.WriteLine(color.Name + " | " + color.Role + " | " + Fmt.DartToString(threshold) + " | " + Fmt.Fixed(ratio, 3)
                    + " | " + Fmt.Fixed(ratioQuantized, 3) + " | " + Fmt.Fixed(ratioLinear, 3));
                Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold), color.Name + " (" + color.Role + ") contra el peor vidrio");
            }

            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(14));
        }

        // Los tokens HUD de 02-tech-design §4.4 tienen AL MENOS esos roles: el Designer no puede bajarlos.
        [Test, Category("AC-BASE-05")]
        public void HudRolesAreNotLowered()
        {
            var hudValue = new[] { Tokens.MetricSplitHud, Tokens.MetricSpmHud, Tokens.MetricWattsHud, Tokens.MetricDistanceHud,
                Tokens.MetricTimeHud, Tokens.MetricKcalHud, Tokens.TextHudPrimary };
            var hudRed = new[] { Tokens.MetricHrHud, Tokens.StateOffTargetHud };
            var hudText = new[] { Tokens.StepWorkHud, Tokens.StepRestHud, Tokens.StepWarmupHud, Tokens.StepCooldownHud, Tokens.StepUnknownHud };
            foreach (var token in hudValue)
            {
                Assert.That(HudThreshold(token.Role), Is.GreaterThanOrEqualTo(7.0), token.Name);
            }

            foreach (var token in hudRed)
            {
                Assert.That(HudThreshold(token.Role), Is.GreaterThanOrEqualTo(4.5), token.Name);
            }

            foreach (var token in hudText)
            {
                Assert.That(HudThreshold(token.Role), Is.GreaterThanOrEqualTo(4.5), token.Name);
            }
        }

        [Test, Category("AC-BASE-05")]
        public void UiTextOnCard()
        {
            var card = Contrast.Channels(Tokens.SurfaceCard.Rgb);
            var checkedCount = 0;
            foreach (var token in TokenCatalog.All)
            {
                if (!(token is ColorToken color))
                {
                    continue;
                }

                double threshold;
                if (color.Role == ColorRole.UiText)
                {
                    threshold = 4.5;
                }
                else if (color.Role == ColorRole.UiTextPrimary)
                {
                    threshold = 7.0;
                }
                else
                {
                    continue;
                }

                checkedCount++;
                var ratio = Contrast.RatioOver(color, card);
                TestContext.Out.WriteLine(color.Name + " sobre surface.card: " + Fmt.Fixed(ratio, 3));
                Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold), color.Name + " sobre surface.card");
            }

            Assert.That(checkedCount, Is.GreaterThanOrEqualTo(3));
        }

        // QA-08: los 13 grupos de pares que pide el Designer (04-design-notes §4.3), uno por caso. Datos del test, nunca hex.
        [TestCaseSource(nameof(DesignerPairCases))]
        public void DesignerPairs(string front, string background, string? backgroundBase, double threshold)
        {
            var ratio = PairRatio(front, background, backgroundBase);
            TestContext.Out.WriteLine(front + " / " + background + (backgroundBase == null ? "" : " sobre " + backgroundBase) + ": "
                + Fmt.Fixed(ratio, 3) + " (umbral " + Fmt.DartToString(threshold) + ")");
            Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold));
        }

        // It. 2 (04-design-notes de 02-dispositivo-simulador §8.1, N1-N13): pares de Dispositivo, la barra y el panel del
        // simulador. El fondo es una pila de capas de abajo hacia arriba ("base|capa|capa"); "token@NN" fuerza el alfa de una
        // capa: brillo de la tarjeta a la altura de la etiqueta, *hover* blanco al 4 %, brillo de la barra al 5 %. "white" es
        // el peor caso del vidrio (T7 de la It. 1). Datos del test, nunca hex.
        [TestCaseSource(nameof(ItTwoPairCases))]
        public void ItTwoDesignerPairs(string front, string layers, double threshold)
        {
            var ratio = Contrast.RatioOver(TokenCatalog.Color(front), Stack(layers));
            TestContext.Out.WriteLine(front + " / " + layers + ": " + Fmt.Fixed(ratio, 3) + " (umbral " + Fmt.DartToString(threshold) + ")");
            Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold));
        }

        // Valores de control (02-tech-design §4.4 y 04-design-notes §4): mismos números que el Designer y QA, a 3 decimales.
        [Test, Category("AC-BASE-05")]
        public void ControlValuesMatchTheDesignNotes()
        {
            var glass = Contrast.WorstGlassBackground();
            Assert.That(Contrast.RatioOver(Tokens.MetricSplitHud, glass), Is.EqualTo(7.004).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.MetricDistanceHud, glass), Is.EqualTo(7.016).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.MetricSpmHud, glass), Is.EqualTo(7.040).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.TextHudPrimary, glass), Is.EqualTo(13.493).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.MetricHrHud, glass), Is.EqualTo(4.508).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.StateOffTargetHud, glass), Is.EqualTo(4.519).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.StepCooldownHud, glass), Is.EqualTo(4.512).Within(0.0006));
            Assert.That(PairRatio("step.unknown", "surface.card", null), Is.EqualTo(3.159).Within(0.0006));
            Assert.That(PairRatio("state.dangerText", "surface.raised", null), Is.EqualTo(4.639).Within(0.0006));
            Assert.That(PairRatio("text.tertiary", "surface.card", null), Is.EqualTo(5.140).Within(0.0006));
            Assert.That(PairRatio("text.secondary", "surface.card", null), Is.EqualTo(7.56).Within(0.006));
            Assert.That(PairRatio("metric.spm", WorstGlass, null), Is.EqualTo(5.474).Within(0.0006));
            Assert.That(PairRatio("state.dangerText", WorstGlass, null), Is.EqualTo(5.077).Within(0.0006));
        }

        [Test, Category("AC-BASE-05")]
        public void WcagFormulas()
        {
            Assert.That(Contrast.RelativeLuminance(255, 255, 255), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(Contrast.RelativeLuminance(0, 0, 0), Is.EqualTo(0.0));
            Assert.That(Contrast.Ratio(1.0, 0.0), Is.EqualTo(21.0).Within(1e-12));
            Assert.That(Contrast.Ratio(0.0, 1.0), Is.EqualTo(21.0).Within(1e-12));
            var half = Contrast.CompositeSrgb(ColorToken.HexAlpha("t.half", "#000000", 50), (255.0, 255.0, 255.0));
            Assert.That(half.R, Is.EqualTo(127.5).Within(1e-12));
        }

        // 04-design-notes §4.4: excepciones conocidas, que no se testean como umbral. Solo se imprimen.
        [Test, Category("AC-BASE-05")]
        public void KnownExceptionsAreOnlyReported()
        {
            var card = Contrast.Channels(Tokens.SurfaceCard.Rgb);
            TestContext.Out.WriteLine("text.disabled sobre surface.card: " + Fmt.Fixed(Contrast.RatioOver(Tokens.TextDisabled, card), 3) + " (exento: deshabilitado)");
            TestContext.Out.WriteLine("text.primary sobre brand.strava: " + Fmt.Fixed(PairRatio("text.primary", "brand.strava", null), 3) + " (guía de marca)");
            TestContext.Out.WriteLine("border.strong sobre surface.card: " + Fmt.Fixed(Contrast.RatioOver(Tokens.BorderStrong, card), 3) + " (el botón se identifica por su texto)");
            TestContext.Out.WriteLine("brand.seed sobre surface.card: " + Fmt.Fixed(PairRatio("brand.seed", "surface.card", null), 3) + " (relleno con texto blanco)");
            TestContext.Out.WriteLine("state.dangerFill sobre surface.card: " + Fmt.Fixed(PairRatio("state.dangerFill", "surface.card", null), 3) + " (relleno con texto blanco)");
            Assert.That(Tokens.TextDisabled.Role, Is.EqualTo(ColorRole.None));
        }

        private static IEnumerable<TestCaseData> DesignerPairCases()
        {
            // G1: colores gráficos contra card y bg.app, ≥ 3.
            var g1 = new[]
            {
                "metric.split", "metric.spm", "metric.watts", "metric.distance", "metric.hr", "metric.time", "metric.kcal",
                "step.work", "step.rest", "step.warmup", "step.cooldown", "step.unknown",
                "phase.running", "phase.paused", "phase.finished",
                "state.offTarget", "brand.strava", "state.success", "state.danger", "focus.ring", "accent.primary",
            };
            foreach (var front in g1)
            {
                yield return Case("G1", front, "surface.card", null, 3.0);
                yield return Case("G1", front, "bg.app", null, 3.0);
            }

            yield return Case("G2", "brand.seed", "bg.app", null, 3.0);
            yield return Case("G3", "dev.simulator", "bg.app", null, 3.0);
            foreach (var background in new[] { "accent.primary", "accent.primaryPressed", "state.success", "metric.watts" })
            {
                yield return Case("T1", "text.onAccent", background, null, 4.5);
            }

            yield return Case("T2", "text.primary", "state.dangerFill", null, 4.5);
            yield return Case("T2", "text.primary", "brand.seed", null, 4.5);
            yield return Case("T3", "dev.simulatorText", "dev.simulator", null, 4.5);
            yield return Case("T4", "dev.bleDebug", "dev.bleDebugBg", "bg.app", 4.5);
            yield return Case("T5", "state.connected", "state.connectedBg", "surface.card", 4.5);
            yield return Case("T6", "state.warning", "state.warningBg", "surface.card", 4.5);
            yield return Case("T7", "accent.primary", "surface.nav", White, 4.5);
            yield return Case("T7", "text.secondary", "surface.nav", White, 4.5);
            foreach (var front in new[] { "text.secondary", "text.tertiary", "accent.primary", "state.dangerText" })
            {
                foreach (var background in new[] { "bg.app", "bg.sunken", "surface.raised", "surface.overlay" })
                {
                    yield return Case("T8", front, background, null, 4.5);
                }
            }

            foreach (var front in new[] { "metric.split", "metric.spm", "metric.watts", "metric.distance", "metric.time", "metric.kcal" })
            {
                yield return Case("T9", front, WorstGlass, null, 4.5);
            }

            yield return Case("T10", "state.dangerText", WorstGlass, null, 4.5);
        }

        // N1 y N2: el brillo de la tarjeta (degradé de 24 dp) vale en la mayúscula de la etiqueta (≈ 19 dp bajo el borde)
        // 0,11 de su pico; con el pico de 18 % (peor que el 14 % elegido) da ≈ 2 %: se prueba con 3 %, más exigente.
        // Hover = blanco al 4 % (04-design-notes §4.7). N10 con el brillo de la barra entero (5 %), el peor caso.
        private static IEnumerable<TestCaseData> ItTwoPairCases()
        {
            const string Glow = "@3";
            const string Hover = "|text.primary@4";
            foreach (var metric in new[] { "metric.split", "metric.spm", "metric.watts", "metric.distance" })
            {
                yield return ItTwoCase("N1", metric, "surface.card|" + metric + Glow, 4.5);
            }

            yield return ItTwoCase("N2", "metric.hr.hud", "surface.card|metric.hr" + Glow, 4.5);
            yield return ItTwoCase("N3", "accent.onTonal", "surface.card|accent.tonalBg", 4.5);
            yield return ItTwoCase("N3", "accent.onTonal", "surface.card|accent.tonalBg" + Hover, 4.5);
            yield return ItTwoCase("N4", "accent.primary", "surface.card" + Hover, 4.5);
            yield return ItTwoCase("N4", "accent.primary", "surface.overlay" + Hover, 4.5);
            yield return ItTwoCase("N5", "state.dangerText", "surface.card" + Hover, 4.5);
            yield return ItTwoCase("N5", "state.dangerText", "bg.app" + Hover, 4.5);
            yield return ItTwoCase("N6", "state.connected", "bg.app|state.connectedBg", 4.5);
            yield return ItTwoCase("N7", "text.tertiary", "bg.app", 4.5);
            yield return ItTwoCase("N7", "text.secondary", "bg.app", 4.5);
            yield return ItTwoCase("N8", "accent.primary", "surface.overlay", 4.5);
            yield return ItTwoCase("N9", "text.secondary", "bg.sunken", 3.0);
            yield return ItTwoCase("N10", "accent.primary", White + "|surface.nav|nav.indicator", 3.0);
            yield return ItTwoCase("N10", "accent.primary", White + "|surface.nav|nav.indicator|text.primary@5", 3.0);
            yield return ItTwoCase("N11", "accent.primary", "bg.app|surface.nav", 4.5);
            yield return ItTwoCase("N11", "text.secondary", "bg.app|surface.nav", 4.5);
            yield return ItTwoCase("N11", "accent.primary", White + "|surface.nav", 4.5);
            yield return ItTwoCase("N11", "text.secondary", White + "|surface.nav", 4.5);
            foreach (var background in new[] { "bg.app", "surface.card", "surface.overlay" })
            {
                yield return ItTwoCase("N12", "focus.ring", background, 3.0);
            }

            yield return ItTwoCase("N13", "metric.hr.hud", "surface.card", 4.5);
        }

        private static TestCaseData ItTwoCase(string group, string front, string layers, double threshold)
        {
            return new TestCaseData(front, layers, threshold)
                .SetName(group + " " + front + " / " + layers.Replace("|", " + "))
                .SetCategory("AC-BASE-05");
        }

        // Compone la pila de capas en sRGB sin redondear (mismo método que PairRatio).
        private static (double R, double G, double B) Stack(string layers)
        {
            var parts = layers.Split('|');
            var bottom = parts[0];
            var under = bottom == White ? (255.0, 255.0, 255.0) : Contrast.Channels(Opaque(TokenCatalog.Color(bottom)).Rgb);
            for (var i = 1; i < parts.Length; i++)
            {
                under = Contrast.CompositeSrgb(Layer(parts[i]), under);
            }

            return under;
        }

        private static ColorToken Opaque(ColorToken token)
        {
            if (!token.IsOpaque)
            {
                throw new InvalidOperationException(token.Name + " tiene alfa: no puede ser la base de la pila");
            }

            return token;
        }

        private static ColorToken Layer(string spec)
        {
            var at = spec.IndexOf('@');
            if (at < 0)
            {
                return TokenCatalog.Color(spec);
            }

            var token = TokenCatalog.Color(spec.Substring(0, at));
            var alpha = int.Parse(spec.Substring(at + 1), System.Globalization.CultureInfo.InvariantCulture);
            return ColorToken.HexAlpha(token.Name + "@" + alpha, token.Rgb.ToHex(), alpha);
        }

        private static TestCaseData Case(string group, string front, string background, string? backgroundBase, double threshold)
        {
            var label = background == WorstGlass ? "peor vidrio" : background + (backgroundBase == null ? "" : " sobre " + backgroundBase);
            return new TestCaseData(front, background, backgroundBase, threshold)
                .SetName(group + " " + front + " / " + label)
                .SetCategory("AC-BASE-05");
        }

        // Fondo con alfa: se compone primero sobre su base; el frente con alfa, sobre el fondo ya compuesto.
        private static double PairRatio(string front, string background, string? backgroundBase)
        {
            (double R, double G, double B) under;
            if (background == WorstGlass)
            {
                under = Contrast.WorstGlassBackground();
            }
            else
            {
                var backgroundToken = TokenCatalog.Color(background);
                if (backgroundBase == null)
                {
                    if (!backgroundToken.IsOpaque)
                    {
                        throw new InvalidOperationException(background + " tiene alfa: el par necesita su base");
                    }

                    under = Contrast.Channels(backgroundToken.Rgb);
                }
                else
                {
                    var baseChannels = backgroundBase == White ? (255.0, 255.0, 255.0) : Contrast.Channels(TokenCatalog.Color(backgroundBase).Rgb);
                    under = Contrast.CompositeSrgb(backgroundToken, baseChannels);
                }
            }

            return Contrast.RatioOver(TokenCatalog.Color(front), under);
        }

        private static double HudThreshold(ColorRole role)
        {
            switch (role)
            {
                case ColorRole.HudValue:
                    return 7.0;
                case ColorRole.HudValueRed:
                case ColorRole.HudText:
                    return 4.5;
                default:
                    return 0;
            }
        }
    }
}
