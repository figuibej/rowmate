using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // AC-BASE-04: los colores de contrato del legacy, definidos una sola vez con su hex exacto (02-tech-design §4.4).
    [TestFixture]
    public sealed class TokensContractTests
    {
        // Los 21 hex de contrato, por nombre de token (legacy: theme.dart:5-68, immersive_workout_screen.dart:531-533
        // y :581-582, history_screen.dart:99).
        [TestCase("bg.app", "#0D1B2A", Category = "AC-BASE-04")]
        [TestCase("surface.card", "#1A2E45", Category = "AC-BASE-04")]
        [TestCase("brand.seed", "#0077B6", Category = "AC-BASE-04")]
        [TestCase("text.primary", "#FFFFFF", Category = "AC-BASE-04")]
        [TestCase("metric.split", "#FF9F1C", Category = "AC-BASE-04")]
        [TestCase("metric.spm", "#00B4D8", Category = "AC-BASE-04")]
        [TestCase("metric.watts", "#FFD60A", Category = "AC-BASE-04")]
        [TestCase("metric.distance", "#4CC9F0", Category = "AC-BASE-04")]
        [TestCase("metric.hr", "#EF476F", Category = "AC-BASE-04")]
        [TestCase("metric.time", "#E0E0E0", Category = "AC-BASE-04")]
        [TestCase("metric.kcal", "#06D6A0", Category = "AC-BASE-04")]
        [TestCase("step.work", "#EF476F", Category = "AC-BASE-04")]
        [TestCase("step.rest", "#06D6A0", Category = "AC-BASE-04")]
        [TestCase("step.warmup", "#FFD166", Category = "AC-BASE-04")]
        [TestCase("step.cooldown", "#118AB2", Category = "AC-BASE-04")]
        [TestCase("step.unknown", "#607D8B", Category = "AC-BASE-04")]
        [TestCase("phase.running", "#06D6A0", Category = "AC-BASE-04")]
        [TestCase("phase.paused", "#FFD166", Category = "AC-BASE-04")]
        [TestCase("phase.finished", "#EF476F", Category = "AC-BASE-04")]
        [TestCase("state.offTarget", "#FF4444", Category = "AC-BASE-04")]
        [TestCase("brand.strava", "#FC4C02", Category = "AC-BASE-04")]
        public void LegacyContractHex(string name, string hex)
        {
            var token = TokenCatalog.Color(name);
            Assert.That(token.Rgb.ToHex(), Is.EqualTo(hex));
            Assert.That(token.IsContract, Is.True, name + " tiene que ser [CONTRATO]");
            Assert.That(token.IsOpaque, Is.True, name + " es opaco en el legacy");
        }

        [Test, Category("AC-BASE-04")]
        public void ExactlyTheTwentyOneLegacyColorsAreContracts()
        {
            var contracts = new List<string>();
            foreach (var token in TokenCatalog.All)
            {
                if (token is ColorToken color && color.IsContract)
                {
                    contracts.Add(color.Name);
                }
            }

            Assert.That(contracts, Is.EquivalentTo(new[]
            {
                "bg.app", "surface.card", "brand.seed", "text.primary",
                "metric.split", "metric.spm", "metric.watts", "metric.distance", "metric.hr", "metric.time", "metric.kcal",
                "step.work", "step.rest", "step.warmup", "step.cooldown", "step.unknown",
                "phase.running", "phase.paused", "phase.finished", "state.offTarget", "brand.strava",
            }));
        }

        // Tokens obligatorios de 02-tech-design §4.4: nombre de campo fijo (los usa el código), valor, alfa y rol.
        // Se referencian por campo para que el compilador verifique los nombres que usan StepTypeInfo y la It. 2.
        [Test, Category("AC-BASE-04")]
        public void MandatoryTokensHaveTheirFieldValueAndRole()
        {
            Check(Tokens.BgApp, "bg.app", "#0D1B2A", 100, ColorRole.Surface);
            Check(Tokens.SurfaceCard, "surface.card", "#1A2E45", 100, ColorRole.Surface);
            Check(Tokens.BrandSeed, "brand.seed", "#0077B6", 100, ColorRole.Graphic);
            Check(Tokens.TextPrimary, "text.primary", "#FFFFFF", 100, ColorRole.UiTextPrimary);
            Check(Tokens.TextSecondary, "text.secondary", "#FFFFFF", 70, ColorRole.UiText);
            Check(Tokens.TextTertiary, "text.tertiary", "#FFFFFF", 54, ColorRole.UiText);
            Check(Tokens.MetricSplit, "metric.split", "#FF9F1C", 100, ColorRole.Graphic);
            Check(Tokens.MetricSpm, "metric.spm", "#00B4D8", 100, ColorRole.Graphic);
            Check(Tokens.MetricWatts, "metric.watts", "#FFD60A", 100, ColorRole.Graphic);
            Check(Tokens.MetricDistance, "metric.distance", "#4CC9F0", 100, ColorRole.Graphic);
            Check(Tokens.MetricHr, "metric.hr", "#EF476F", 100, ColorRole.Graphic);
            Check(Tokens.MetricTime, "metric.time", "#E0E0E0", 100, ColorRole.Graphic);
            Check(Tokens.MetricKcal, "metric.kcal", "#06D6A0", 100, ColorRole.Graphic);
            Check(Tokens.StepWork, "step.work", "#EF476F", 100, ColorRole.Graphic);
            Check(Tokens.StepRest, "step.rest", "#06D6A0", 100, ColorRole.Graphic);
            Check(Tokens.StepWarmup, "step.warmup", "#FFD166", 100, ColorRole.Graphic);
            Check(Tokens.StepCooldown, "step.cooldown", "#118AB2", 100, ColorRole.Graphic);
            Check(Tokens.StepUnknown, "step.unknown", "#607D8B", 100, ColorRole.Graphic);
            Check(Tokens.PhaseRunning, "phase.running", "#06D6A0", 100, ColorRole.Graphic);
            Check(Tokens.PhasePaused, "phase.paused", "#FFD166", 100, ColorRole.Graphic);
            Check(Tokens.PhaseFinished, "phase.finished", "#EF476F", 100, ColorRole.Graphic);
            Check(Tokens.StateOffTarget, "state.offTarget", "#FF4444", 100, ColorRole.Graphic);
            Check(Tokens.BrandStrava, "brand.strava", "#FC4C02", 100, ColorRole.Graphic);
            Check(Tokens.MetricSplitHud, "metric.split.hud", "#FEA845", 100, ColorRole.HudValue);
            Check(Tokens.MetricSpmHud, "metric.spm.hud", "#38CBEF", 100, ColorRole.HudValue);
            Check(Tokens.MetricWattsHud, "metric.watts.hud", "#FFD60A", 100, ColorRole.HudValue);
            Check(Tokens.MetricDistanceHud, "metric.distance.hud", "#4CC9F0", 100, ColorRole.HudValue);
            Check(Tokens.MetricTimeHud, "metric.time.hud", "#E0E0E0", 100, ColorRole.HudValue);
            Check(Tokens.MetricKcalHud, "metric.kcal.hud", "#06D6A0", 100, ColorRole.HudValue);
            Check(Tokens.TextHudPrimary, "text.hudPrimary", "#FFFFFF", 100, ColorRole.HudValue);
            Check(Tokens.MetricHrHud, "metric.hr.hud", "#FF5A7E", 100, ColorRole.HudValueRed);
            Check(Tokens.StateOffTargetHud, "state.offTarget.hud", "#FF5F59", 100, ColorRole.HudValueRed);
            Check(Tokens.StepWorkHud, "step.work.hud", "#FF5A7E", 100, ColorRole.HudText);
            Check(Tokens.StepRestHud, "step.rest.hud", "#06D6A0", 100, ColorRole.HudText);
            Check(Tokens.StepWarmupHud, "step.warmup.hud", "#FFD166", 100, ColorRole.HudText);
            Check(Tokens.StepCooldownHud, "step.cooldown.hud", "#35A0C9", 100, ColorRole.HudText);
            Check(Tokens.StepUnknownHud, "step.unknown.hud", "#7C9AA8", 100, ColorRole.HudText);
            Check(Tokens.GlassFallback, "glass.fallback", "#070D16", 86, ColorRole.Surface);
        }

        private static void Check(ColorToken token, string name, string hex, int alphaPercent, ColorRole role)
        {
            Assert.That(token.Name, Is.EqualTo(name));
            Assert.That(token.Rgb.ToHex(), Is.EqualTo(hex), name);
            Assert.That(token.AlphaPercent, Is.EqualTo(alphaPercent), name);
            Assert.That(token.Role, Is.EqualTo(role), name);
            Assert.That(TokenCatalog.Get(name), Is.SameAs(token), name + ": el catálogo devuelve la misma instancia");
        }
    }
}
