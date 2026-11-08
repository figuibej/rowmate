using System.Linq;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.History
{
    // TL · Las líneas objetivo (AC-HIS-15-01, -03, AC-RUT-20-01; sd:352-397): una por franja con su índice entre los pasos
    // expandidos, por métrica; el split invertido como la curva y al piso (Q-24, DV-93); sin rutina, ninguna.
    [TestFixture]
    public sealed class TargetLinesTests
    {
        private static readonly StepBand[] Bands =
        {
            new StepBand(5, 65, 0, "warmup"), new StepBand(65, 185, 1, "work"), new StepBand(185, 245, 2, "rest"), new StepBand(245, 365, 3, "work"),
            new StepBand(800, 900, 12, "work"),
        };

        private static readonly System.Collections.Generic.IReadOnlyList<IntervalStep> Flat = RoutineExpansion.Flatten(HistorySeeds.Escalera().Steps);

        [Test, Category("AC-HIS-15-01"), Category("AC-HIS-15-03")]
        public void PerMetricWithProgressions()
        {
            var watts = TargetLines.Build(Bands, Flat, ChartMetric.Watts, 180);
            Assert.That(watts.Select(t => t.RawTarget), Is.EqualTo(new double[] { 140, 160, 150, 170 }), "mín y máx, con +10 por repetición");
            Assert.That(watts.Select(t => t.TypeName).Distinct(), Is.EqualTo(new[] { "work" }));
            Assert.That(watts[0].Start, Is.EqualTo(65));
            Assert.That(watts[0].End, Is.EqualTo(185));
            Assert.That(TargetLines.Build(Bands, Flat, ChartMetric.Spm, 180).Select(t => t.Y), Is.EqualTo(new double[] { 18, 20 }));
            Assert.That(TargetLines.Build(Bands, Flat, ChartMetric.Distance, 180), Is.Empty);
            Assert.That(TargetLines.Build(Bands, Flat, ChartMetric.HeartRate, 180), Is.Empty);
        }

        [Test, Category("AC-HIS-15-01")]
        public void IndexOutOfRangeHasNoLine()
        {
            var lines = TargetLines.Build(Bands, Flat, ChartMetric.Watts, 180);
            Assert.That(lines.Any(t => t.Start == 800), Is.False, "el índice 12 no está entre los 8 pasos");
            var onlyMax = new[] { new IntervalStep(0, 0, StepType.Work) { TargetWattsMax = 200 } };
            Assert.That(TargetLines.Build(new[] { new StepBand(0, 10, 0, "work") }, onlyMax, ChartMetric.Watts, 0).Single().RawTarget,
                Is.EqualTo(200), "solo las que están definidas");
        }

        [Test, Category("AC-HIS-15-03")]
        public void SplitIsInvertedAndFloored()
        {
            var split = TargetLines.Build(Bands, Flat, ChartMetric.Split, 180);
            Assert.That(split.Select(t => t.RawTarget), Is.EqualTo(new double[] { 130, 128 }));
            Assert.That(split.Select(t => t.Y), Is.EqualTo(new double[] { 50, 52 }), "lento − objetivo");
            var slower = TargetLines.Build(Bands, Flat, ChartMetric.Split, 120);
            Assert.That(slower.Select(t => t.Y), Is.EqualTo(new double[] { 0, 0 }), "un objetivo más lento que el más lento queda en el piso");
        }

        [Test, Category("AC-RUT-20-01")]
        public void DeletedRoutine()
        {
            Assert.That(TargetLines.Build(Bands, new IntervalStep[0], ChartMetric.Watts, 180), Is.Empty);
        }
    }
}
