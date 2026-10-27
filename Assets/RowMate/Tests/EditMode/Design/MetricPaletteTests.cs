using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Models;

namespace RowMate.Tests.EditMode.Design
{
    // MetricPalette = MetricColors del legacy (lib/shared/theme.dart:32-40), desde los tokens de contrato (F-APP-06).
    [TestFixture]
    public sealed class MetricPaletteTests
    {
        [TestCase(MetricKind.Split, "#FF9F1C", Category = "AC-APP-06-01")]
        [TestCase(MetricKind.Spm, "#00B4D8", Category = "AC-APP-06-01")]
        [TestCase(MetricKind.Watts, "#FFD60A", Category = "AC-APP-06-01")]
        [TestCase(MetricKind.Distance, "#4CC9F0", Category = "AC-APP-06-01")]
        [TestCase(MetricKind.HeartRate, "#EF476F", Category = "AC-APP-06-01")]
        [TestCase(MetricKind.Time, "#E0E0E0", Category = "AC-APP-06-01")]
        [TestCase(MetricKind.Calories, "#06D6A0", Category = "AC-APP-06-01")]
        public void ContractHexes(MetricKind kind, string hex)
        {
            var token = MetricPalette.For(kind);
            Assert.That(token.Rgb.ToHex(), Is.EqualTo(hex));
            Assert.That(token.IsOpaque, Is.True, "el valor va al 100 %");
            Assert.That(token.IsContract, Is.True, "[CONTRATO] del legacy: no se cambia nunca");
        }

        [Test, Category("AC-APP-06-01")]
        public void LabelAndIconColors()
        {
            // Ícono y etiqueta al 100 % del color de la métrica; el pulso, metric.hr.hud (#FF5A7E, el mismo matiz más claro; DV-03).
            Assert.That(MetricPalette.LabelFor(MetricKind.HeartRate), Is.SameAs(Tokens.MetricHrHud));
            Assert.That(MetricPalette.LabelFor(MetricKind.HeartRate).Rgb.ToHex(), Is.EqualTo("#FF5A7E"));
            foreach (var kind in new[] { MetricKind.Split, MetricKind.Spm, MetricKind.Watts, MetricKind.Distance })
            {
                Assert.That(MetricPalette.LabelFor(kind), Is.SameAs(MetricPalette.For(kind)), kind.ToString());
            }
        }

        [Test, Category("AC-APP-06-01")]
        public void AccentRestsAtSixtyPercent()
        {
            // El borde superior va en el color de la métrica con alfa 0,60 en reposo (legacy withOpacity(0.6), DV-02).
            Assert.That(AccentPulse.AlphaFor(MetricPalette.For(MetricKind.Split), 10000), Is.EqualTo(0.60).Within(1e-12));
            Assert.That(AccentPulse.AlphaFor(MetricPalette.For(MetricKind.Split), 0), Is.EqualTo(1.0).Within(1e-12), "al 100 % en el destello");
        }
    }
}
