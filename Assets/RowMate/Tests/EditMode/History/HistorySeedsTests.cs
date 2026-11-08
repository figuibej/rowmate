using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.App.History;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Infrastructure.Database;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.History
{
    // El ORÁCULO de F-9 (09-historial/01-po-spec §6; 02-tech-design DT9-15): la base sembrada por código con los repositorios
    // reales (SQLite en una carpeta temporal, zona UTC−03:00) reproduce todos los números de la spec, con las erratas E-01 y E-02
    // del gate M-03 (franjas work 65–185 y ceros en 425 y 430 s). Lo que ve la tarjeta sale de GetHistoryAsync; lo del detalle, de
    // SessionDetailStore sobre los mismos repositorios.
    [TestFixture]
    public sealed class HistorySeedsTests
    {
        private static readonly string[] Names = { "Escalera", "Libre", "Escalera", "Libre", "Libre", "Rutina borrada" };

        private static readonly string[] Dates =
        {
            "05/11/2026  18:40", "04/11/2026  07:15", "03/11/2026  19:02", "02/11/2026  12:00", "02/10/2026  13:51", "01/10/2026  21:05",
        };

        private static readonly string[][] Values =
        {
            new[] { "11:00", "2253m", "150", "24.1", "03:00", "139/150" },
            new[] { "33:44", "7923m", "160", "22.0", "02:10", "145/145" },
            new[] { "03:00", "651m", "150", "24.1", "02:30", "140/150" },
            new[] { "00:00", "0m", "0", "0.0", "--:--" },
            new[] { "1:02:05", "13796m", "140", "20.0", "02:15" },
            new[] { "04:00", "851m", "150", "24.1", "02:30", "136/150" },
        };

        private RecordingLogger _logger = null!;
        private TempDatabase _temp = null!;
        private LocalStore _store = null!;
        private HistorySeedResult _seed = null!;
        private IReadOnlyList<HistoryEntry> _history = null!;

        [OneTimeSetUp]
        public void SeedOnce()
        {
            _logger = new RecordingLogger();
            _temp = new TempDatabase();
            _store = _temp.Store(_logger, localZone: HistorySeeds.Zone);
            _seed = HistorySeeds.Apply(_store, HistorySeeds.F9);
            _history = _store.Sessions.GetHistoryAsync().GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void TearDownOnce()
        {
            _store.Dispose();
            _temp.Dispose();
        }

        [Test, Category("AC-HIS-07-02"), Category("AC-HIS-07-05"), Category("AC-HIS-05-01")]
        public void CardValues()
        {
            Assert.That(_history.Count, Is.EqualTo(6));
            for (var i = 0; i < 6; i++)
            {
                var card = HistoryCardModel.From(_history[i], LocalizationFixture.Es);
                Assert.That(card.Name, Is.EqualTo(Names[i]), "orden y nombre de la tarjeta " + (i + 1));
                Assert.That(card.Date, Is.EqualTo(Dates[i]), "fecha de la " + (i + 1));
                Assert.That(card.Columns.Select(c => c.Value), Is.EqualTo(Values[i]), "columnas de la " + (i + 1));
                Assert.That(card.Id, Is.EqualTo(_seed.Session(i + 1)));
            }

            Assert.That(_history[2].Session.FinishedAt, Is.Null, "la n.º 3 está cortada");
            Assert.That(_history[2].Session.TotalDistanceMeters, Is.EqualTo(0), "con los totales en 0: los números salen de los puntos");
            Assert.That(_history[3].Session.FinishedAt, Is.Null);
            Assert.That(_history[5].Session.RoutineId, Is.EqualTo(HistorySeeds.DeletedRoutineId));
        }

        [Test, Category("AC-HIS-11-01"), Category("AC-HIS-11-03"), Category("AC-HIS-11-04")]
        public void StepRows()
        {
            var detail = Load(1);
            var rows = detail.Steps;
            Assert.That(rows.Select(r => r.PointCount), Is.EqualTo(new[] { 12, 24, 12, 24, 12, 24, 12, 13 }));
            Assert.That(rows.Select(r => r.Badge), Is.EqualTo(new[] { "1", "2", "3", "4", "5", "6", "7", "8" }));
            AssertRow(rows[0], "1:00  ·  183m", "100W", "20.0 spm", "2:30/500m");
            AssertRow(rows[1], "2:00  ·  432m", "150W", "24.1 spm", "2:13/500m");
            AssertRow(rows[5], "2:00  ·  433m", "138W", "22.1 spm", "2:02/500m");
            Assert.That(rows[7].Line1, Is.EqualTo("1:05  ·  183m"), "el punto final repetido suma 5 s al último paso");
            Assert.That(Load(6).Steps.Count, Is.EqualTo(3));
        }

        [Test, Category("AC-HIS-13-04"), Category("AC-HIS-13-05")]
        public void Axes()
        {
            var chart = Load(1).Chart!;
            Assert.That(chart.View(ChartMetric.Watts).YLabels, Is.EqualTo(new[] { "0", "50", "100", "150", "198" }));
            Assert.That(chart.View(ChartMetric.Spm).YLabels, Is.EqualTo(new[] { "0", "5", "10", "15", "20", "25", "26" }));
            Assert.That(chart.View(ChartMetric.Split).YLabels, Is.EqualTo(new[] { "3:00", "2:50", "2:40", "2:30", "2:20", "2:10", "2:00" }));
            Assert.That(chart.View(ChartMetric.HeartRate).YLabels, Is.EqualTo(new[] { "0", "50", "100", "150", "165" }));
            Assert.That(chart.View(ChartMetric.Distance).MaxPlotted, Is.EqualTo(2253), "CAP-11: la curva sube hasta 2253");
            Assert.That(chart.View(ChartMetric.Watts).XLabels, Is.EqualTo(new[] { "0:05", "2:00", "4:00", "6:00", "8:00", "10:00", "11:00" }));
            Assert.That(Load(2).Chart!.View(ChartMetric.Watts).XLabels,
                Is.EqualTo(new[] { "3:44", "5:00", "10:00", "15:00", "20:00", "25:00", "30:00", "33:44" }));
            var fifth = Load(5).Chart!.View(ChartMetric.Watts).XLabels;
            Assert.That(fifth.First(), Is.EqualTo("0:05"));
            Assert.That(fifth.Last(), Is.EqualTo("62:05"));
        }

        [Test, Category("AC-HIS-13-01"), Category("AC-HIS-13-02"), Category("AC-HIS-13-03")]
        public void ChartSeries()
        {
            var chart = Load(1).Chart!;
            Assert.That(chart.Count, Is.EqualTo(133));
            Assert.That(chart.X[0], Is.EqualTo(5));
            Assert.That(chart.X[chart.Count - 1], Is.EqualTo(660));
            var zeros = new List<double>();
            var watts = chart.Raw(ChartMetric.Watts);
            for (var i = 0; i < chart.Count; i++)
            {
                if (watts[i] == 0)
                {
                    zeros.Add(chart.X[i]);
                }
            }

            Assert.That(zeros, Is.EqualTo(new[] { 425.0, 430.0 }), "E-02: las dos caídas a 0 de la fila 6");
            Assert.That(chart.SlowestPace, Is.EqualTo(180));
            var split = chart.Plotted(ChartMetric.Split);
            var at425 = Array.IndexOf(chart.X, 425.0);
            Assert.That(split[at425], Is.EqualTo(0f), "el ritmo 0 va al piso (Q-24)");
            Assert.That(split[0], Is.EqualTo(30f), "lento − 150");
            Assert.That(Load(2).Chart!.X[0], Is.EqualTo(224), "Libre del 04/11 arranca en 3:44 (Q-15)");
        }

        [Test, Category("AC-HIS-14-01")]
        public void Bands()
        {
            var bands = Load(1).Chart!.Bands;
            Assert.That(bands.Select(b => b.TypeName + " " + Fmt.Integer((long)b.Start) + "–" + Fmt.Integer((long)b.End)), Is.EqualTo(new[]
            {
                "warmup 5–65", "work 65–185", "rest 185–245", "work 245–365", "rest 365–425", "work 425–545", "rest 545–605", "cooldown 605–660",
            }), "E-01");
            Assert.That(Load(2).Chart!.Bands, Is.Empty, "una sesión libre no tiene franjas");
            Assert.That(Load(6).Chart!.Bands.Count, Is.EqualTo(3));
        }

        [Test, Category("AC-HIS-15-03"), Category("AC-RUT-20-01")]
        public void Targets()
        {
            var chart = Load(1).Chart!;
            Assert.That(chart.View(ChartMetric.Watts).Targets.Select(t => t.RawTarget),
                Is.EqualTo(new double[] { 140, 160, 150, 170, 160, 180 }));
            Assert.That(chart.View(ChartMetric.Watts).Targets.Select(t => t.Start), Is.EqualTo(new double[] { 65, 65, 245, 245, 425, 425 }));
            Assert.That(chart.View(ChartMetric.Spm).Targets.Select(t => t.RawTarget), Is.EqualTo(new double[] { 18, 20, 22 }));
            var split = chart.View(ChartMetric.Split).Targets;
            Assert.That(split.Select(t => t.RawTarget), Is.EqualTo(new double[] { 130, 128, 126 }));
            // gate m-05: el objetivo queda en la y de su ritmo (2:10, 2:08, 2:06), invertido como la curva.
            Assert.That(split.Select(t => t.Y), Is.EqualTo(new[]
            {
                SessionChartData.PlottedPace(130, 180), SessionChartData.PlottedPace(128, 180), SessionChartData.PlottedPace(126, 180),
            }));
            Assert.That(chart.View(ChartMetric.Distance).Targets, Is.Empty);
            Assert.That(chart.View(ChartMetric.HeartRate).Targets, Is.Empty);
            var deleted = Load(6);
            Assert.That(deleted.FlatSteps, Is.Empty, "la rutina de la n.º 6 ya no existe");
            Assert.That(deleted.Chart!.View(ChartMetric.Watts).Targets, Is.Empty);
            Assert.That(deleted.Title(LocalizationFixture.Es), Is.EqualTo("Rutina borrada"));
            Assert.That(Load(2).Title(LocalizationFixture.Es), Is.EqualTo("Entrenamiento libre"), "Q-09");
        }

        [Test, Category("AC-HIS-19-04")]
        public void CutSession()
        {
            var cut = Load(3);
            Assert.That(cut.Phase, Is.EqualTo(SessionDetailPhase.Ready));
            Assert.That(cut.Session.FinishedAt, Is.Null, "sin marca de cortada");
            Assert.That(cut.Steps.Select(r => r.Line1), Is.EqualTo(new[] { "1:00  ·  183m", "2:00  ·  432m" }));
            Assert.That(cut.Chart!.Bands.Count, Is.EqualTo(2));
            var x = cut.Chart.View(ChartMetric.Watts).XLabels;
            Assert.That(x.First(), Is.EqualTo("0:05"));
            Assert.That(x.Last(), Is.EqualTo("3:00"));
            Assert.That(cut.Columns.Select(c => c.Value), Is.EqualTo(Values[2]), "el Resumen = la tarjeta");
            Assert.That(Load(4).Phase, Is.EqualTo(SessionDetailPhase.NoTelemetry));
        }

        [Test, Category("AC-HIS-10-01")]
        public void SummaryEqualsCard()
        {
            for (var n = 1; n <= 6; n++)
            {
                if (n == 4)
                {
                    continue;
                }

                Assert.That(Load(n).Columns.Select(c => c.Value), Is.EqualTo(Values[n - 1]), "sesión " + n);
            }
        }

        // F-9V vacía y F-9P con 200 sesiones como la n.º 2, una por día (la más nueva el 04/11/2026).
        [Test, Category("AC-HIS-19-03")]
        public void OtherSets()
        {
            using (var temp = new TempDatabase())
            {
                using (var store = temp.Store(_logger, localZone: HistorySeeds.Zone))
                {
                    HistorySeeds.Apply(store, HistorySeeds.F9V);
                    Assert.That(store.Sessions.GetHistoryAsync().GetAwaiter().GetResult(), Is.Empty);
                    var seed = HistorySeeds.Apply(store, HistorySeeds.F9P);
                    var history = store.Sessions.GetHistoryAsync().GetAwaiter().GetResult();
                    Assert.That(history.Count, Is.EqualTo(HistorySeeds.PerformanceSessions));
                    Assert.That(HistoryFormats.CardDate(history[0].Session.StartedAt), Is.EqualTo("04/11/2026  07:15"));
                    Assert.That(history[0].Session.Id, Is.EqualTo(seed.Session(1)));
                    Assert.That(HistoryCardModel.From(history[199], LocalizationFixture.Es).Columns.Select(c => c.Value), Is.EqualTo(Values[1]));
                }
            }
        }

        // Los puntos de la spec (§6) sin base: 133, 362, 36, 0, 746 y 49.
        [Test, Category("AC-HIS-19-04")]
        public void PointCounts()
        {
            Assert.That(Enumerable.Range(1, 6).Select(n => HistorySeeds.Points(n).Count), Is.EqualTo(new[] { 133, 362, 36, 0, 746, 49 }));
            Assert.That(HistorySeeds.Points(2).First().DistanceMeters, Is.EqualTo(1000));
            Assert.That(HistorySeeds.Points(2).Last().DistanceMeters, Is.EqualTo(7923));
            Assert.That(HistorySeeds.Stress60MinAt1Hz().Count, Is.EqualTo(3600));
        }

        private static void AssertRow(StepBreakdownRow row, string line1, string watts, string spm, string split)
        {
            Assert.That(row.Line1, Is.EqualTo(line1));
            Assert.That(row.Watts, Is.EqualTo(watts));
            Assert.That(row.Spm, Is.EqualTo(spm));
            Assert.That(row.Split, Is.EqualTo(split));
        }

        private SessionDetailState Load(int number)
        {
            HistoryEntry? entry = null;
            foreach (var e in _history)
            {
                if (e.Session.Id == _seed.Session(number))
                {
                    entry = e;
                }
            }

            using (var detail = new SessionDetailStore(entry!, _store.Sessions, _store.Routines, _logger))
            {
                detail.LoadAsync().GetAwaiter().GetResult();
                return detail.State.Value;
            }
        }
    }
}
