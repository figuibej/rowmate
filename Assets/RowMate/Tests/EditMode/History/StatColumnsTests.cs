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
            Assert.That(columns.Select(c => c.Value), Is.EqualTo(new[] { "11:00", "2253m", "150", "24.1", "03:00", "139/150" }));
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
            Assert.That(es.Columns.Select(c => c.Label), Is.EqualTo(new[] { "Tiempo", "Distancia", "Watts", "SPM", "Split", "Pulso" }));
            var en = HistoryCardModel.From(entry with { Session = entry.Session with { RoutineName = "Escalera" } }, LocalizationFixture.En);
            Assert.That(en.Name, Is.EqualTo("Escalera"));
            Assert.That(en.Date, Is.EqualTo("05/11/2026  18:40"), "igual en es y en (Q-06)");
            Assert.That(en.Columns.Select(c => c.Label), Is.EqualTo(new[] { "Time", "Distance", "Watts", "SPM", "Split", "HR" }));
            Assert.That(HistoryCardModel.From(entry, LocalizationFixture.En).Name, Is.EqualTo("Free"));
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
