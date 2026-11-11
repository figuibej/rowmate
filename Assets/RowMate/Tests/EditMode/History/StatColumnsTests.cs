using System.Linq;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.History
{
    // SC · La fila de números de la tarjeta y del resumen (AC-HIS-07-03, -04; AC-HIS-10-01; hs:109-123, sd:160-176): 5 columnas,
    // o 6 con pulso, en orden, con sus claves y los colores base.
    [TestFixture]
    public sealed class StatColumnsTests
    {
        private static readonly SessionStats WithPulse = new SessionStats
        {
            TotalTimeSeconds = 660, TotalDistance = 2253, P99Watts = 150, P99Spm = 24.1, P99SplitSeconds = 180, AvgHeartRate = 139,
            MaxHeartRate = 150,
        };

        [Test, Category("AC-HIS-07-04")]
        public void SixColumnsWithPulseInOrder()
        {
            var columns = StatColumns.Build(WithPulse);
            Assert.That(columns.Select(c => c.LabelKey), Is.EqualTo(new[]
            {
                L10nKeys.HistoryStatTime, L10nKeys.HistoryStatDistance, L10nKeys.HistoryStatPower, L10nKeys.HistoryStatSpm,
                L10nKeys.HistoryStatSplit, L10nKeys.HistoryStatHeartRate,
            }));
            // It. 15 (AC-D15-23, DV-141): el split en m:ss ("3:00"; antes "03:00").
            Assert.That(columns.Select(c => c.Value), Is.EqualTo(new[] { "11:00", "2253m", "150", "24.1", "3:00", "139/150" }));
            Assert.That(columns.Select(c => c.Color.Rgb.ToHex()), Is.EqualTo(new[] { "#FFFFFF", "#4CC9F0", "#FFD60A", "#00B4D8", "#FF9F1C", "#EF476F" }),
                "el tiempo en blanco y el resto en los colores base (§10.2)");
            Assert.That(columns[0].Color, Is.SameAs(Tokens.TextPrimary));
            Assert.That(columns.Select(c => c.Metric), Is.EqualTo(new[]
            {
                MetricKind.Time, MetricKind.Distance, MetricKind.Watts, MetricKind.Spm, MetricKind.Split, MetricKind.HeartRate,
            }));
        }

        [Test, Category("AC-HIS-07-04")]
        public void FiveColumnsWithoutPulseAndNoCalories()
        {
            var columns = StatColumns.Build(WithPulse with { AvgHeartRate = 0, MaxHeartRate = 0, TotalCalories = 99 });
            Assert.That(columns.Count, Is.EqualTo(5));
            Assert.That(columns.Any(c => c.Metric == MetricKind.Calories), Is.False, "sin calorías (Q-28)");
            Assert.That(StatColumns.Build(SessionStats.Empty).Select(c => c.Value), Is.EqualTo(new[] { "00:00", "0m", "0", "0.0", "--:--" }));
        }

        [Test, Category("AC-HIS-07-03"), Category("AC-HIS-07-04")]
        public void CardTextsInBothLanguages()
        {
            var entry = new HistoryEntry(new WorkoutSession(new System.DateTimeOffset(2026, 11, 5, 18, 40, 0, System.TimeSpan.FromHours(-3))) { Id = 7 },
                WithPulse);
            var es = HistoryCardModel.From(entry, LocalizationFixture.Es);
            Assert.That(es.Name, Is.EqualTo("Libre"));
            Assert.That(es.Date, Is.EqualTo("05/11/2026  18:40"));
            Assert.That(es.Id, Is.EqualTo(7));
            // It. 15 (AC-D15-22, DV-140): la potencia se llama Vatios en es (antes Watts).
            Assert.That(es.Columns.Select(c => c.Label), Is.EqualTo(new[] { "Tiempo", "Distancia", "Vatios", "SPM", "Split", "Pulso" }));
            var en = HistoryCardModel.From(entry with { Session = entry.Session with { RoutineName = "Escalera" } }, LocalizationFixture.En);
            Assert.That(en.Name, Is.EqualTo("Escalera"));
            Assert.That(en.Date, Is.EqualTo("05/11/2026  18:40"), "igual en es y en (Q-06)");
            Assert.That(en.Columns.Select(c => c.Label), Is.EqualTo(new[] { "Time", "Distance", "Watts", "SPM", "Split", "HR" }));
            Assert.That(HistoryCardModel.From(entry, LocalizationFixture.En).Name, Is.EqualTo("Free"));
        }

        [TestCase(133, "2:13")]
        [TestCase(605, "10:05")]
        [TestCase(45, "0:45")]
        [TestCase(6000, "100:00")]
        [TestCase(0, "--:--")]
        [TestCase(-5, "--:--")]
        [Category("AC-D15-23")]
        public void SplitIsMinutesColonSeconds(int seconds, string expected)
        {
            // It. 15 (#75, DV-141): la tarjeta y el resumen en m:ss, como las filas por paso; Dispositivo y el HUD siguen en mm:ss.
            Assert.That(HistoryFormats.SummarySplit(seconds), Is.EqualTo(expected));
            var split = StatColumns.Build(WithPulse with { P99SplitSeconds = seconds }).Single(c => c.Metric == MetricKind.Split);
            Assert.That(split.Value, Is.EqualTo(expected));
            Assert.That(new SessionStats { P99SplitSeconds = 133 }.SplitFormatted, Is.EqualTo("02:13"), "SplitFormatted (Dispositivo, HUD) no cambia");
        }

        [Test, Category("AC-D15-09")]
        public void CardCanExportOnlyWithIdAndData()
        {
            // It. 15 (#67, DT15-04): el botón de la tarjeta, solo en las exportables (F-9 n.º 4, sin puntos, no lo tiene).
            var at = new System.DateTimeOffset(2026, 11, 5, 18, 40, 0, System.TimeSpan.FromHours(-3));
            var exportable = new HistoryEntry(new WorkoutSession(at) { Id = 7 }, WithPulse);
            Assert.That(HistoryCardModel.From(exportable, LocalizationFixture.Es).CanExport, Is.True);
            Assert.That(HistoryCardModel.From(exportable with { Stats = SessionStats.Empty }, LocalizationFixture.Es).CanExport, Is.False, "sin datos");
            Assert.That(HistoryCardModel.From(exportable with { Session = exportable.Session with { Id = null } }, LocalizationFixture.Es).CanExport,
                Is.False, "sin id");
            Assert.That(HistoryCardModel.CanExportEntry(exportable with { Stats = WithPulse with { TotalTimeSeconds = 0, TotalDistance = 0 } }),
                Is.False, "tiempo y distancia en 0 (TcxExportRules.HasData)");
        }

        [Test, Category("AC-HIS-10-01")]
        public void SummaryRowIsTheCardRow()
        {
            var card = HistoryCardColumn.Row(WithPulse, LocalizationFixture.Es);
            var entry = new HistoryEntry(new WorkoutSession(System.DateTimeOffset.UnixEpoch) { Id = 1 }, WithPulse);
            Assert.That(card, Is.EqualTo(HistoryCardModel.From(entry, LocalizationFixture.Es).Columns));
        }
    }
}
