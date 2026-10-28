using NUnit.Framework;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Routines
{
    // SST: subtítulos del editor (AC-RUT-12-06, AC-RUT-04-06; routine_editor_screen.dart:345-349) y su partición para dibujar
    // el 📈 como el ícono trending_up (DV-10). Text es el texto del legacy al carácter, en es y en.
    [TestFixture]
    public sealed class StepSubtitleTests
    {
        private static readonly string Trend = StepSubtitle.TrendEmoji;

        // AC-RUT-12-06: los 11 pasos de F-BORDE, en orden.
        [Test, Category("AC-RUT-12-06")]
        public void BorderCasesTable()
        {
            var expected = new[]
            {
                "0s",
                "?",
                "1m 30s  ·  >150W",
                "60m",
                "62m 5s",
                "2000m  ·  0 spm",
                "30s",
                "1m  ·  00:59/500m",
                "2m  ·  120-180W · 22 spm · 02:05/500m",
                "5m  ·  20 spm · " + Trend + " SPM -1/rep",
                "1m  ·  " + Trend + " Split +3s/rep",
            };
            var titles = new[] { "Trabajo", "Descanso", "Trabajo", "Trabajo", "Enfriamiento", "Trabajo", "Calentamiento", "Trabajo", "Trabajo", "Trabajo", "Descanso" };
            var steps = RoutineSeeds.CasosBorde().Steps;
            Assert.That(steps.Count, Is.EqualTo(11));
            foreach (var l10n in LocalizationFixture.Both)
            {
                for (var i = 0; i < steps.Count; i++)
                {
                    Assert.That(StepSubtitle.For(steps[i], l10n).Text, Is.EqualTo(expected[i]), l10n.LanguageCode + " fila " + (i + 1));
                }
            }

            for (var i = 0; i < steps.Count; i++)
            {
                Assert.That(LocalizationFixture.Es.Get(StepTypeInfo.NameKey(steps[i].Type)), Is.EqualTo(titles[i]), "título fila " + (i + 1));
            }
        }

        // DV-10: lo de antes del 📈, el ícono y lo de después (con el espacio que sigue al emoji).
        [Test, Category("AC-RUT-12-06")]
        public void SplitsAroundTheTrendIcon()
        {
            var l10n = LocalizationFixture.Es;
            var steps = RoutineSeeds.CasosBorde().Steps;
            var row10 = StepSubtitle.For(steps[9], l10n);
            Assert.That(row10.HasTrend, Is.True);
            Assert.That(row10.Before, Is.EqualTo("5m  ·  20 spm · "));
            Assert.That(row10.After, Is.EqualTo(" SPM -1/rep"));
            Assert.That(row10.Before + Trend + row10.After, Is.EqualTo(row10.Text));
            var row11 = StepSubtitle.For(steps[10], l10n);
            Assert.That(row11.Before, Is.EqualTo("1m  ·  "));
            Assert.That(row11.After, Is.EqualTo(" Split +3s/rep"));
            var plain = StepSubtitle.For(steps[2], l10n);
            Assert.That(plain.HasTrend, Is.False);
            Assert.That(plain.Before, Is.EqualTo(plain.Text));
            Assert.That(plain.After, Is.EqualTo(""));
            var escalera = StepSubtitle.For(RoutineSeeds.EscaleraSpm().Steps[1], l10n);
            Assert.That(escalera.Text, Is.EqualTo("5m  ·  18 spm · " + Trend + " SPM +2/rep"), "demo, paso 5");
            Assert.That(Trend, Is.EqualTo(char.ConvertFromUtf32(0x1F4C8)));
        }

        // AC-RUT-04-06: dos espacios a cada lado del punto medio, solo entre la duración y los objetivos.
        [Test, Category("AC-RUT-04-06")]
        public void EditorSeparatorHasTwoSpaces()
        {
            Assert.That(StepLabels.SubtitleSeparator, Is.EqualTo("  " + (char)0xB7 + "  "));
            var l10n = LocalizationFixture.En;
            var text = StepSubtitle.For(RoutineSeeds.Step(StepType.Work, dur: 90, wMin: 150, spm: 20), l10n).Text;
            Assert.That(text, Is.EqualTo("1m 30s  ·  >150W · 20 spm"));
            Assert.That(text.IndexOf("  ·  ", System.StringComparison.Ordinal), Is.EqualTo(6));
            Assert.That(text.LastIndexOf("  ·  ", System.StringComparison.Ordinal), Is.EqualTo(6), "entre objetivos va un solo espacio");
        }
    }
}
