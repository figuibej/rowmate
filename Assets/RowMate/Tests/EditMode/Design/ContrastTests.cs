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
        private const string WhiteGlass = "white|glass.fallback";
        private const string SkyGlass = "backdrop.sky|glass.fallback";

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

        // QA-V2-12 (It. 3, 03-rutinas/00-deuda-diseno §2): margen de render. Cada token del HUD llega a su umbral también
        // contra el peor vidrio RENDERIZADO en 8 bits (#2A2F37, el que midió R-T1) más 1 por canal (#2B3038): la mezcla se
        // cuantiza hacia arriba (41,72 → 42…) y la GPU o la sonda pueden dar 1 más. Equivale a ≈ 1,7 % sobre el cálculo
        // sin redondear de HudAgainstWorstGlass; con #FF5A7E el pulso daba 4,497 en el píxel real.
        [Test, Category("AC-BASE-05")]
        public void HudKeepsItsThresholdOnTheRenderedWorstGlass()
        {
            var glass = Contrast.WorstGlassBackground();
            var rendered = (DartMath.Round(glass.R) + 1, DartMath.Round(glass.G) + 1, DartMath.Round(glass.B) + 1);
            Assert.That(new Rgb((byte)rendered.Item1, (byte)rendered.Item2, (byte)rendered.Item3).ToHex(), Is.EqualTo("#2B3038"));
            var checkedCount = 0;
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
                var ratio = Contrast.RatioOver(color, rendered);
                TestContext.Out.WriteLine(color.Name + " sobre #2B3038: " + Fmt.Fixed(ratio, 3) + " (umbral " + Fmt.DartToString(threshold) + ")");
                Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold), color.Name + " (" + color.Role + ") contra el peor vidrio renderizado + 1");
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

        // It. 3 (03-rutinas/04-design-notes §8.1, R1-R28): lista, editor, serie, campos, diálogos, menús y anillos. Mismo formato de
        // pila que la It. 2: "bg.app|brand.seed@12|accent.primary@20" es la cápsula del chip sobre el encabezado de la serie y
        // "|text.primary@4" el hover blanco. Datos del test, nunca hex.
        [TestCaseSource(nameof(ItThreePairCases))]
        public void ItThreeDesignerPairs(string front, string layers, double threshold)
        {
            var ratio = Contrast.RatioOver(TokenCatalog.Color(front), Stack(layers));
            TestContext.Out.WriteLine(front + " / " + layers + ": " + Fmt.Fixed(ratio, 3) + " (umbral " + Fmt.DartToString(threshold) + ")");
            Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold));
        }

        // Los valores que citan 04-design-notes de la It. 3 (§2.3, §4, §8.1) y QA, a 3 decimales: si cambia un token, cambia acá.
        [Test, Category("AC-BASE-05")]
        public void ItThreeControlValuesMatchTheDesignNotes()
        {
            Assert.That(Ratio("step.work.hud", "surface.card"), Is.EqualTo(4.685).Within(0.0006), "R1 título del trabajo");
            Assert.That(Ratio("step.cooldown.hud", "surface.card"), Is.EqualTo(4.730).Within(0.0006), "R1 título del enfriamiento (DT-19)");
            Assert.That(Ratio("step.work", "surface.card"), Is.EqualTo(3.815).Within(0.0006), "R3 tramo del trabajo (el legacy lo usaba como texto)");
            Assert.That(Ratio("step.cooldown.hud", "surface.card|step.cooldown@20"), Is.EqualTo(3.723).Within(0.0006), "R2 ícono del enfriamiento");
            Assert.That(Ratio("accent.onTonal", "bg.app|brand.seed@12|accent.primary@20"), Is.EqualTo(5.620).Within(0.0006), "R7 ×N");
            Assert.That(Ratio("accent.primary", "bg.app|brand.seed@12|accent.primary@20"), Is.EqualTo(4.370).Within(0.0006), "×N con el color del legacy (se corrige)");
            Assert.That(Ratio("border.input", "bg.sunken"), Is.EqualTo(3.807).Within(0.0006), "R18 borde del campo");
            Assert.That(Ratio("border.strong", "bg.sunken"), Is.EqualTo(2.132).Within(0.0006), "border.strong no llegaba (por eso border.input)");
            Assert.That(Ratio("state.dangerText", "surface.overlay|overlay.scrim@12"), Is.EqualTo(5.147).Within(0.0006), "R25 ítem de peligro con su hover oscuro");
            Assert.That(Ratio("state.dangerText", "surface.overlay|text.primary@4"), Is.EqualTo(4.174).Within(0.0006), "con la capa blanca no llegaba");
            Assert.That(Ratio("text.primary", "bg.sunken|text.selection"), Is.EqualTo(9.586).Within(0.0006), "R20 selección");
            Assert.That(Ratio("text.primary", "brand.seed"), Is.EqualTo(4.868).Within(0.0006), "R24 y R28 segmento elegido y su anillo interior");
        }

        // It. 4a (04a-motor-hud-funcional/04-design-notes §9.1, H1-H17 y W1-W4): los controles, la fila del paso, las tarjetas,
        // la banda y el anillo del HUD, y Workout en reposo. Mismo formato de pila que las It. 2 y 3; acá también el FRENTE puede
        // llevar "@NN" (las unidades al 80 %, los bordes y los íconos al 70 %, el halo del foco al 50 %), con el mismo parseo que
        // las capas. "white|glass.fallback" es el peor fondo del vidrio (una nube blanca detrás); "|text.primary@4" y "@8", las
        // capas blancas de hover y presionado; "|overlay.scrim@12" y "@24", la capa oscura de Terminar (DN4a-10). Datos del
        // test, nunca hex. Las excepciones de 04-design-notes §9.2 (el contorno de Pausa, lo decorativo, lo deshabilitado y lo
        // presionado) no se prueban como umbral.
        [TestCaseSource(nameof(ItFourAPairCases))]
        public void ItFourADesignerPairs(string front, string layers, double threshold)
        {
            var ratio = Contrast.RatioOver(Layer(front), Stack(layers));
            TestContext.Out.WriteLine(front + " / " + layers + ": " + Fmt.Fixed(ratio, 3) + " (umbral " + Fmt.DartToString(threshold) + ")");
            Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold));
        }

        // Los valores de 04-design-notes de la It. 4a §9.1 a 3 decimales, y los del margen de render (contra #2B3038, el peor
        // vidrio renderizado + 1: el criterio de la It. 3). Si cambia un token, cambia acá y en las notas.
        [Test, Category("AC-BASE-05")]
        public void ItFourAControlValuesMatchTheDesignNotes()
        {
            const string Glass = WhiteGlass;
            Assert.That(Ratio("state.dangerText", Glass), Is.EqualTo(5.077).Within(0.0006), "H1 Terminar");
            Assert.That(Ratio("state.dangerText", Glass + "|overlay.scrim@12"), Is.EqualTo(5.483).Within(0.0006), "H2 Terminar con hover (capa oscura)");
            Assert.That(Ratio("state.dangerText", Glass + "|overlay.scrim@24"), Is.EqualTo(5.892).Within(0.0006), "H2 Terminar presionado");
            Assert.That(Ratio("state.dangerText", Glass + "|text.primary@4"), Is.EqualTo(4.494).Within(0.0006), "con la capa blanca no llegaba (§9.2)");
            Assert.That(Ratio("text.primary", Glass), Is.EqualTo(13.493).Within(0.0006), "H3 Pausa, título libre, cuenta");
            Assert.That(Ratio("text.primary", Glass + "|text.primary@4"), Is.EqualTo(11.944).Within(0.0006), "H4 Pausa con hover");
            Assert.That(Ratio("text.onAccent", "state.success"), Is.EqualTo(9.985).Within(0.0006), "H5 Reanudar");
            Assert.That(Ratio("text.onAccent", "state.success|text.primary@4"), Is.EqualTo(10.172).Within(0.0006), "H5 Reanudar con hover");
            Assert.That(Ratio("text.onAccent", "state.success|text.primary@8"), Is.EqualTo(10.367).Within(0.0006), "H5 Reanudar presionado");
            var types = new[] { "work", "rest", "warmup", "cooldown" };
            var typeName = new[] { 4.577, 7.154, 9.358, 4.622 };
            var pillText = new[] { 10.962, 9.013, 8.134, 10.769 };
            var trend = new[] { 5.700, 4.687, 4.230, 5.600 };
            var upNextLabel = new[] { 6.301, 5.369, 4.977, 6.212 };
            for (var i = 0; i < types.Length; i++)
            {
                var pill = Glass + "|step." + types[i] + "@20";
                Assert.That(Ratio("step." + types[i] + ".hud", Glass), Is.EqualTo(typeName[i]).Within(0.0006), "H6 tipo " + types[i]);
                Assert.That(Ratio("text.primary", pill), Is.EqualTo(pillText[i]).Within(0.0006), "H7 pastilla " + types[i]);
                Assert.That(Ratio("metric.distance", pill), Is.EqualTo(trend[i]).Within(0.0006), "H8 tendencia " + types[i]);
                Assert.That(Ratio("text.secondary", pill), Is.EqualTo(upNextLabel[i]).Within(0.0006), "H9 Próximo: " + types[i]);
            }

            Assert.That(Ratio("step.work.hud", Glass + "|step.work@20"), Is.EqualTo(3.719).Within(0.0006), "la pastilla en su color no llegaba (DV-38)");
            Assert.That(Ratio("step.cooldown.hud", Glass + "|step.cooldown@20"), Is.EqualTo(3.688).Within(0.0006), "ídem enfriamiento (DV-38)");
            Assert.That(Ratio("text.secondary", Glass), Is.EqualTo(7.448).Within(0.0006), "H10 obj.");
            Assert.That(Ratio("metric.watts.hud@80", Glass), Is.EqualTo(6.678).Within(0.0006), "H11 W");
            Assert.That(Ratio("metric.distance.hud@80", Glass), Is.EqualTo(5.157).Within(0.0006), "H11 m");
            Assert.That(Ratio("metric.watts@55", Glass), Is.EqualTo(3.967).Within(0.0006), "W al 55 % del legacy (DV-31)");
            Assert.That(Ratio("metric.distance@55", Glass), Is.EqualTo(3.188).Within(0.0006), "m al 55 % del legacy (DV-31)");
            Assert.That(Ratio("metric.split", Glass), Is.EqualTo(6.572).Within(0.0006), "H12 rótulo Split");
            Assert.That(Ratio("metric.watts", Glass), Is.EqualTo(9.557).Within(0.0006), "H12 rótulo Watts");
            Assert.That(Ratio("metric.kcal", Glass), Is.EqualTo(7.154).Within(0.0006), "H12 rótulo kcal");
            Assert.That(Ratio("metric.hr.hud", Glass), Is.EqualTo(4.577).Within(0.0006), "H12 rótulo BPM");
            Assert.That(Ratio("state.offTarget.hud", Glass), Is.EqualTo(4.594).Within(0.0006), "H13 flecha");
            Assert.That(Ratio("metric.spm@70", Glass), Is.EqualTo(3.427).Within(0.0006), "H14 borde SPM");
            Assert.That(Ratio("metric.split@70", Glass), Is.EqualTo(3.979).Within(0.0006), "H14 borde Split");
            Assert.That(Ratio("metric.watts@70", Glass), Is.EqualTo(5.480).Within(0.0006), "H14 borde Watts");
            Assert.That(Ratio("metric.time@70", Glass), Is.EqualTo(5.856).Within(0.0006), "H15 cronómetro");
            Assert.That(Ratio("metric.distance@70", Glass), Is.EqualTo(4.239).Within(0.0006), "H15 regla");
            Assert.That(Ratio("accent.primary", Glass), Is.EqualTo(5.474).Within(0.0006), "H16 rowing");
            Assert.That(Ratio("text.primary", "white|overlay.scrim@50"), Is.EqualTo(3.977).Within(0.0006), "H17 anillo del HUD sobre su halo");
            Assert.That(Ratio("overlay.scrim@50", "white"), Is.EqualTo(3.977).Within(0.0006), "H17 halo sobre una nube");
            Assert.That(Ratio("focus.ring", "white"), Is.EqualTo(2.465).Within(0.0006), "el anillo cian de la app sobre una nube (§9.2)");
            Assert.That(Ratio("state.warning", "bg.app|state.warningBg"), Is.EqualTo(6.310).Within(0.0006), "W1 aviso sin remo");
            Assert.That(Ratio("text.tertiary", "bg.app"), Is.EqualTo(5.829).Within(0.0006), "W2 etiqueta, vacío, en curso");
            Assert.That(Ratio("text.tertiary", "surface.card"), Is.EqualTo(5.140).Within(0.0006), "W3 chevron");
            Assert.That(Ratio("text.tertiary", "surface.card|text.primary@4"), Is.EqualTo(4.785).Within(0.0006), "W3 chevron con hover");
            Assert.That(Ratio("text.onAccent", "accent.primary"), Is.EqualTo(7.641).Within(0.0006), "W4 Entrenamiento libre");

            // Margen de render: contra el peor vidrio renderizado + 1 (#2B3038).
            Assert.That(RenderedRatio("state.dangerText", ""), Is.EqualTo(4.993).Within(0.0006), "Terminar renderizado");
            Assert.That(RenderedRatio("text.secondary", ""), Is.EqualTo(7.352).Within(0.0006), "obj. renderizado");
            Assert.That(RenderedRatio("metric.watts.hud@80", ""), Is.EqualTo(6.584).Within(0.0006), "W renderizada");
            Assert.That(RenderedRatio("metric.distance.hud@80", ""), Is.EqualTo(5.086).Within(0.0006), "m renderizada");
            Assert.That(RenderedRatio("text.primary", "|step.work@20"), Is.EqualTo(10.805).Within(0.0006), "pastilla de trabajo renderizada");
            Assert.That(RenderedRatio("text.secondary", "|step.warmup@20"), Is.EqualTo(4.919).Within(0.0006), "Próximo: sobre calentamiento renderizado");
            Assert.That(RenderedRatio("metric.distance", "|step.warmup@20"), Is.EqualTo(4.168).Within(0.0006), "tendencia sobre calentamiento renderizada");
            Assert.That(RenderedRatio("text.primary", "|step.work@20"), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(RenderedRatio("text.secondary", "|step.warmup@20"), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(RenderedRatio("metric.distance", "|step.warmup@20"), Is.GreaterThanOrEqualTo(3.0));
        }

        // It. 4b (04b-hud-exr/04-design-notes §8.1, B1-B10; DN4b-29): el chip de fase y su punto, el sello, el aro de la cuenta
        // grande y el de PAUSA, el ícono del reloj de pared, las píldoras hechas y huecas sobre el fondo provisorio de la 4b y el
        // diálogo de completado (el check, los íconos, los rótulos y los valores). Mismo formato de pila que las It. 2 a 4a:
        // "white|glass.fallback" es el peor vidrio (una nube blanca detrás) y "backdrop.sky|glass.fallback" el vidrio sobre el
        // cielo provisorio, que es el fondo de la barra de pasos en los cinco perfiles. Las excepciones de §8.2 (la pendiente al
        // 18 %, la pista al 30 % y la del aro al 20 %, el borde del chip al 50 %, el halo, el destello, el brillo, el velo y el
        // punto en lo bajo de su latido) no se prueban como umbral. Datos del test, nunca hex.
        [TestCaseSource(nameof(ItFourBPairCases))]
        public void ItFourBDesignerPairs(string front, string layers, double threshold)
        {
            var ratio = Contrast.RatioOver(Layer(front), Stack(layers));
            TestContext.Out.WriteLine(front + " / " + layers + ": " + Fmt.Fixed(ratio, 3) + " (umbral " + Fmt.DartToString(threshold) + ")");
            Assert.That(ratio, Is.GreaterThanOrEqualTo(threshold));
        }

        // Los valores de 04-design-notes de la It. 4b §8.1 a 3 decimales y, donde las notas los dan, los del margen de render
        // (contra #2B3038, el peor vidrio renderizado + 1). Si cambia un token, cambia acá y en las notas.
        [Test, Category("AC-BASE-05")]
        public void ItFourBControlValuesMatchTheDesignNotes()
        {
            const string Glass = WhiteGlass;
            Assert.That(Ratio("phase.running", Glass + "|phase.running@15"), Is.EqualTo(5.324).Within(0.0006), "B1 EN MARCHA (y B2, el punto)");
            Assert.That(Ratio("phase.paused", Glass + "|phase.paused@15"), Is.EqualTo(6.432).Within(0.0006), "B1 PAUSADO");
            Assert.That(Ratio("phase.finished.hud", Glass + "|phase.finished@15"), Is.EqualTo(4.677).Within(0.0006), "B1 FINALIZADO (DN4b-15)");
            Assert.That(Ratio("step.work.hud", Glass + "|phase.finished@15"), Is.EqualTo(3.942).Within(0.0006), "FINALIZADO en #FF5D7F, el de la spec, no llegaba (DV-44)");
            Assert.That(Ratio("state.success", Glass), Is.EqualTo(7.154).Within(0.0006), "B3 Objetivo cumplido y su ícono");
            Assert.That(Ratio("phase.paused", Glass), Is.EqualTo(9.358).Within(0.0006), "B3 No alcanzado y su ícono; B5 el aro de PAUSA");
            var types = new[] { "work", "rest", "warmup", "cooldown" };
            var arc = new[] { 3.727, 7.154, 9.358, 3.407 };
            var done = new[] { 3.868, 7.204, 9.259, 3.645 };
            var hollow = new[] { 3.404, 5.305, 6.460, 3.064 };
            for (var i = 0; i < types.Length; i++)
            {
                var step = "step." + types[i];
                Assert.That(Ratio(step, Glass), Is.EqualTo(arc[i]).Within(0.0006), "B4 el arco de la cuenta grande " + types[i]);
                Assert.That(Ratio(step + "@85", SkyGlass), Is.EqualTo(done[i]).Within(0.0006), "B7 la píldora hecha " + types[i]);
                Assert.That(Ratio(step + "@85", SkyGlass + "|" + step + "@18"), Is.EqualTo(hollow[i]).Within(0.0006), "B8 el borde de la hueca " + types[i]);
            }

            Assert.That(Ratio("text.secondary", Glass), Is.EqualTo(7.448).Within(0.0006), "B6 el ícono del reloj de pared");
            Assert.That(Ratio("state.success", "surface.overlay"), Is.EqualTo(6.626).Within(0.0006), "B9 el disco del check");
            Assert.That(Ratio("surface.overlay", "state.success"), Is.EqualTo(6.626).Within(0.0006), "B9 el trazo del check");
            const string Summary = "surface.overlay|text.primary@4";
            Assert.That(Ratio("accent.primary", Summary), Is.EqualTo(4.501).Within(0.0006), "B10 los íconos de las filas");
            Assert.That(Ratio("text.secondary", Summary), Is.EqualTo(6.361).Within(0.0006), "B10 los rótulos y la m");
            Assert.That(Ratio("text.primary", Summary), Is.EqualTo(11.093).Within(0.0006), "B10 los valores");

            // Margen de render: contra el peor vidrio renderizado + 1 (#2B3038).
            Assert.That(RenderedRatio("phase.running", "|phase.running@15"), Is.EqualTo(5.244).Within(0.0006), "B1 EN MARCHA renderizado");
            Assert.That(RenderedRatio("phase.paused", "|phase.paused@15"), Is.EqualTo(6.332).Within(0.0006), "B1 PAUSADO renderizado");
            Assert.That(RenderedRatio("phase.finished.hud", "|phase.finished@15"), Is.EqualTo(4.606).Within(0.0006), "B1 FINALIZADO renderizado");
            Assert.That(RenderedRatio("step.work.hud", "|phase.finished@15"), Is.EqualTo(3.883).Within(0.0006), "FINALIZADO en #FF5D7F renderizado");
            Assert.That(RenderedRatio("state.success", ""), Is.EqualTo(7.035).Within(0.0006), "B3 Objetivo cumplido renderizado");
            Assert.That(RenderedRatio("phase.paused", ""), Is.EqualTo(9.203).Within(0.0006), "B3 No alcanzado renderizado");
            Assert.That(RenderedRatio("step.work", ""), Is.EqualTo(3.665).Within(0.0006), "B4 el arco del trabajo renderizado");
            Assert.That(RenderedRatio("step.cooldown", ""), Is.EqualTo(3.350).Within(0.0006), "B4 el arco del enfriamiento renderizado");
            Assert.That(RenderedRatio("phase.finished.hud", "|phase.finished@15"), Is.GreaterThanOrEqualTo(4.5));
            Assert.That(RenderedRatio("step.cooldown", ""), Is.GreaterThanOrEqualTo(3.0));
        }

        // Valores de control (02-tech-design §4.4 y 04-design-notes §4): mismos números que el Designer y QA, a 3 decimales.
        // Los del HUD, con los valores de la It. 3 (QA-V2-12; antes 7,004 · 7,016 · 7,040 · 4,508 · 4,519 · 4,512).
        [Test, Category("AC-BASE-05")]
        public void ControlValuesMatchTheDesignNotes()
        {
            var glass = Contrast.WorstGlassBackground();
            Assert.That(Contrast.RatioOver(Tokens.MetricSplitHud, glass), Is.EqualTo(7.127).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.MetricDistanceHud, glass), Is.EqualTo(7.163).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.MetricSpmHud, glass), Is.EqualTo(7.124).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.TextHudPrimary, glass), Is.EqualTo(13.493).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.MetricHrHud, glass), Is.EqualTo(4.577).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.StateOffTargetHud, glass), Is.EqualTo(4.594).Within(0.0006));
            Assert.That(Contrast.RatioOver(Tokens.StepCooldownHud, glass), Is.EqualTo(4.622).Within(0.0006));
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

        // R1-R27 de 04-design-notes de la It. 3 §8.1, más R28 (WP-D: el anillo interior del segmento elegido). Insignia = tarjeta
        // + tipo @20 %; cuerpo y pie de la serie = bg.app + brand.seed @6 %; encabezado = @12 %; cápsula = encabezado +
        // accent.primary @20 %; mosaico elegido = surface.overlay + tipo @20 %; selección = bg.sunken + text.selection.
        private static IEnumerable<TestCaseData> ItThreePairCases()
        {
            const string Hover = "|text.primary@4";
            const string Header = "bg.app|brand.seed@12";
            const string Body = "bg.app|brand.seed@6";
            const string Pill = Header + "|accent.primary@20";
            var types = new[] { "work", "rest", "warmup", "cooldown" };
            foreach (var type in types)
            {
                yield return ItTwoCase("R1", "step." + type + ".hud", "surface.card", 4.5);
                yield return ItTwoCase("R2", "step." + type + ".hud", "surface.card|step." + type + "@20", 3.0);
                yield return ItTwoCase("R3", "step." + type, "surface.card", 3.0);
            }

            yield return ItTwoCase("R4", "metric.distance", "surface.card", 3.0);
            yield return ItTwoCase("R4", "text.secondary", "surface.card" + Hover, 3.0);
            yield return ItTwoCase("R4", "state.dangerText", "surface.card" + Hover, 3.0);
            yield return ItTwoCase("R5", "brand.seed", "bg.app", 3.0);
            yield return ItTwoCase("R5", "brand.seed", Body, 3.0);
            yield return ItTwoCase("R6", "accent.primary", Header, 4.5);
            yield return ItTwoCase("R6", "accent.primary", Body + Hover, 4.5);
            yield return ItTwoCase("R7", "accent.onTonal", Pill, 4.5);
            yield return ItTwoCase("R7", "accent.onTonal", Pill + Hover, 4.5);
            yield return ItTwoCase("R8", "state.dangerText", Header + Hover, 3.0);
            yield return ItTwoCase("R9", "text.secondary", "surface.card" + Hover, 4.5);
            yield return ItTwoCase("R9", "text.tertiary", "surface.card" + Hover, 4.5);
            yield return ItTwoCase("R10", "accent.primary", "surface.card|accent.tonalBg", 3.0);
            yield return ItTwoCase("R11", "text.onAccent", "accent.primary", 4.5);
            yield return ItTwoCase("R11", "accent.primary", "surface.card", 3.0);
            yield return ItTwoCase("R12", "text.secondary", "bg.app", 4.5);
            yield return ItTwoCase("R12", "text.tertiary", "bg.app", 4.5);
            yield return ItTwoCase("R12", "state.dangerText", "bg.app", 3.0);
            yield return ItTwoCase("R13", "accent.onTonal", "bg.app|accent.tonalBg" + Hover, 4.5);
            yield return ItTwoCase("R14", "text.secondary", "surface.overlay", 4.5);
            yield return ItTwoCase("R14", "text.tertiary", "surface.overlay", 4.5);
            yield return ItTwoCase("R14", "accent.primary", "surface.overlay" + Hover, 4.5);
            yield return ItTwoCase("R15", "text.primary", "state.dangerFill" + Hover, 4.5);
            foreach (var front in new[] { "text.primary", "text.secondary", "accent.primary", "state.dangerText", "text.tertiary" })
            {
                yield return ItTwoCase("R16", front, "bg.sunken", 4.5);
            }

            yield return ItTwoCase("R17", "state.dangerText", "surface.overlay", 4.5);
            yield return ItTwoCase("R17", "state.dangerText", "bg.app", 4.5);
            foreach (var front in new[] { "border.input", "accent.primary", "state.danger" })
            {
                yield return ItTwoCase("R18", front, "bg.sunken", 3.0);
            }

            yield return ItTwoCase("R19", "border.input", "surface.overlay", 3.0);
            yield return ItTwoCase("R19", "border.input", "bg.app", 3.0);
            yield return ItTwoCase("R20", "text.primary", "bg.sunken|text.selection", 4.5);
            yield return ItTwoCase("R21", "text.secondary", "surface.overlay" + Hover, 4.5);
            foreach (var type in types)
            {
                yield return ItTwoCase("R21", "text.primary", "surface.overlay|step." + type + "@20", 4.5);
                yield return ItTwoCase("R22", "step." + type + ".hud", "surface.overlay", 3.0);
                yield return ItTwoCase("R22", "step." + type + ".hud", "surface.overlay|step." + type + "@20", 3.0);
                yield return ItTwoCase("R23", "step." + type, "surface.overlay", 3.0);
            }

            yield return ItTwoCase("R24", "text.primary", "brand.seed", 4.5);
            yield return ItTwoCase("R24", "text.secondary", "surface.overlay" + Hover, 4.5);
            yield return ItTwoCase("R25", "state.dangerText", "surface.overlay|overlay.scrim@12", 4.5);
            yield return ItTwoCase("R26", "text.primary", "surface.overlay" + Hover, 7.0);
            yield return ItTwoCase("R26", "text.primary", "surface.overlay", 7.0);
            foreach (var background in new[] { "surface.overlay", "surface.card", "bg.app", Header })
            {
                yield return ItTwoCase("R27", "focus.ring", background, 3.0);
            }

            yield return ItTwoCase("R28", "text.primary", "brand.seed", 3.0);
        }

        // H1-H17 y W1-W4 de 04-design-notes de la It. 4a §9.1, uno por caso (los valores, en ItFourAControlValuesMatchTheDesignNotes).
        private static IEnumerable<TestCaseData> ItFourAPairCases()
        {
            const string Glass = WhiteGlass;
            const string Hover = "|text.primary@4";
            const string Pressed = "|text.primary@8";
            yield return ItTwoCase("H1", "state.dangerText", Glass, 4.5);
            yield return ItTwoCase("H2", "state.dangerText", Glass + "|overlay.scrim@12", 4.5);
            yield return ItTwoCase("H2", "state.dangerText", Glass + "|overlay.scrim@24", 4.5);
            yield return ItTwoCase("H3", "text.primary", Glass, 7.0);
            yield return ItTwoCase("H4", "text.primary", Glass + Hover, 7.0);
            yield return ItTwoCase("H5", "text.onAccent", "state.success", 4.5);
            yield return ItTwoCase("H5", "text.onAccent", "state.success" + Hover, 4.5);
            yield return ItTwoCase("H5", "text.onAccent", "state.success" + Pressed, 4.5);
            foreach (var type in new[] { "work", "rest", "warmup", "cooldown" })
            {
                var pill = Glass + "|step." + type + "@20";
                yield return ItTwoCase("H6", "step." + type + ".hud", Glass, 4.5);
                yield return ItTwoCase("H7", "text.primary", pill, 4.5);
                yield return ItTwoCase("H8", "metric.distance", pill, 3.0);
                yield return ItTwoCase("H9", "text.secondary", pill, 4.5);
            }

            yield return ItTwoCase("H10", "text.secondary", Glass, 4.5);
            yield return ItTwoCase("H11", "metric.watts.hud@80", Glass, 4.5);
            yield return ItTwoCase("H11", "metric.distance.hud@80", Glass, 4.5);
            foreach (var label in new[] { "metric.spm", "metric.split", "metric.watts", "metric.kcal", "metric.hr.hud" })
            {
                yield return ItTwoCase("H12", label, Glass, 4.5);
            }

            yield return ItTwoCase("H13", "state.offTarget.hud", Glass, 3.0);
            foreach (var border in new[] { "metric.spm@70", "metric.split@70", "metric.watts@70" })
            {
                yield return ItTwoCase("H14", border, Glass, 3.0);
            }

            yield return ItTwoCase("H15", "metric.time@70", Glass, 3.0);
            yield return ItTwoCase("H15", "metric.distance@70", Glass, 3.0);
            yield return ItTwoCase("H16", "accent.primary", Glass, 3.0);
            yield return ItTwoCase("H17", "text.primary", "white|overlay.scrim@50", 3.0);
            yield return ItTwoCase("H17", "overlay.scrim@50", "white", 3.0);
            yield return ItTwoCase("W1", "state.warning", "bg.app|state.warningBg", 4.5);
            yield return ItTwoCase("W2", "text.tertiary", "bg.app", 4.5);
            yield return ItTwoCase("W3", "text.tertiary", "surface.card", 3.0);
            yield return ItTwoCase("W3", "text.tertiary", "surface.card" + Hover, 3.0);
            yield return ItTwoCase("W4", "text.onAccent", "accent.primary", 4.5);
        }

        // B1-B10 de 04-design-notes de la It. 4b §8.1, uno por caso (los valores, en ItFourBControlValuesMatchTheDesignNotes).
        private static IEnumerable<TestCaseData> ItFourBPairCases()
        {
            const string Glass = WhiteGlass;
            yield return ItTwoCase("B1", "phase.running", Glass + "|phase.running@15", 4.5);
            yield return ItTwoCase("B1", "phase.paused", Glass + "|phase.paused@15", 4.5);
            yield return ItTwoCase("B1", "phase.finished.hud", Glass + "|phase.finished@15", 4.5);
            yield return ItTwoCase("B2", "phase.running", Glass + "|phase.running@15", 3.0);
            yield return ItTwoCase("B3", "state.success", Glass, 4.5);
            yield return ItTwoCase("B3", "phase.paused", Glass, 4.5);
            foreach (var type in new[] { "work", "rest", "warmup", "cooldown" })
            {
                yield return ItTwoCase("B4", "step." + type, Glass, 3.0);
            }

            yield return ItTwoCase("B5", "phase.paused", Glass, 3.0);
            yield return ItTwoCase("B6", "text.secondary", Glass, 3.0);
            foreach (var type in new[] { "work", "rest", "warmup", "cooldown" })
            {
                var step = "step." + type;
                yield return ItTwoCase("B7", step + "@85", SkyGlass, 3.0);
                yield return ItTwoCase("B8", step + "@85", SkyGlass + "|" + step + "@18", 3.0);
            }

            yield return ItTwoCase("B9", "state.success", "surface.overlay", 3.0);
            yield return ItTwoCase("B9", "surface.overlay", "state.success", 3.0);
            yield return ItTwoCase("B10", "accent.primary", "surface.overlay|text.primary@4", 3.0);
            yield return ItTwoCase("B10", "text.secondary", "surface.overlay|text.primary@4", 4.5);
            yield return ItTwoCase("B10", "text.primary", "surface.overlay|text.primary@4", 4.5);
        }

        // El frente también puede llevar "@NN" (Layer): sin alfa propia es el token tal cual.
        private static double Ratio(string front, string layers)
        {
            return Contrast.RatioOver(Layer(front), Stack(layers));
        }

        // Contra el peor vidrio renderizado + 1 por canal (#2B3038, HudKeepsItsThresholdOnTheRenderedWorstGlass), con capas encima
        // ("|step.work@20"); "" = el vidrio solo.
        private static double RenderedRatio(string front, string layersAbove)
        {
            var glass = Contrast.WorstGlassBackground();
            (double R, double G, double B) under = (DartMath.Round(glass.R) + 1, DartMath.Round(glass.G) + 1, DartMath.Round(glass.B) + 1);
            foreach (var part in layersAbove.Split('|'))
            {
                if (part.Length > 0)
                {
                    under = Contrast.CompositeSrgb(Layer(part), under);
                }
            }

            return Contrast.RatioOver(Layer(front), under);
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
