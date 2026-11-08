using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Core.Models;

namespace RowMate.Tests.EditMode.History
{
    // SB · El Rendimiento por paso (AC-HIS-11-01, -03, -04; sd:183-224): agrupa por índice en orden ascendente, suma los tramos
    // no contiguos, deja afuera los puntos sin índice, promedia CON los ceros, distancia último − primero (negativa si un punto
    // trae 0) y la duración es puntos × el intervalo.
    [TestFixture]
    public sealed class StepBreakdownTests
    {
        private static DataPoint P(int t, int? step, int watts = 100, double spm = 20, int split = 150, int distance = 0)
        {
            return new DataPoint(1, t)
            {
                StepIndex = step, StepTypeName = step.HasValue ? StepTypeNames.Work : null, PowerWatts = watts, StrokeRate = spm, Pace500mSeconds = split,
                DistanceMeters = distance,
            };
        }

        [Test, Category("AC-HIS-11-01")]
        public void GroupsByIndexAscendingAndAddsNonContiguousStretches()
        {
            var points = new List<DataPoint>
            {
                P(5, 2, distance: 10), P(10, null), P(15, 0, distance: 20), P(20, 2, distance: 30), P(25, 1, distance: 40),
            };
            var rows = StepBreakdown.Compute(points, 5);
            Assert.That(rows.Select(r => r.StepIndex), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(rows.Select(r => r.PointCount), Is.EqualTo(new[] { 1, 1, 2 }));
            Assert.That(rows[2].DistanceMeters, Is.EqualTo(20), "30 − 10: el último menos el primero del índice");
            Assert.That(StepBreakdown.Compute(new[] { P(5, null), P(10, null) }, 5), Is.Empty, "sin índices no hay filas");
        }

        [Test, Category("AC-HIS-11-03")]
        public void AveragesWithZerosAndRounding()
        {
            var points = new List<DataPoint>();
            for (var i = 0; i < 24; i++)
            {
                var zero = i < 2;
                points.Add(P(5 * (i + 1), 5, zero ? 0 : 150, zero ? 0 : 24.1, zero ? 0 : 133, 1454 + i));
            }

            var row = StepBreakdown.Compute(points, 5).Single();
            Assert.That(row.AvgWatts, Is.EqualTo(138), "137,5 → 138 (la mitad sube)");
            Assert.That(row.Spm, Is.EqualTo("22.1 spm"));
            Assert.That(row.AvgSplitSeconds, Is.EqualTo(122));
            Assert.That(row.Split, Is.EqualTo("2:02/500m"));
            Assert.That(row.Watts, Is.EqualTo("138W"));
        }

        [Test, Category("AC-HIS-11-03")]
        public void DistanceCanBeNegative()
        {
            var rows = StepBreakdown.Compute(new[] { P(5, 0, distance: 500), P(10, 0, distance: 0) }, 5);
            Assert.That(rows[0].DistanceMeters, Is.EqualTo(-500));
            Assert.That(rows[0].Line1, Is.EqualTo("0:10  ·  -500m"));
        }

        [Test, Category("AC-HIS-11-04")]
        public void DurationIsPointsTimesTheInterval()
        {
            var points = Enumerable.Range(1, 13).Select(i => P(600 + 5 * i, 7)).ToList();
            Assert.That(StepBreakdown.Compute(points, 5)[0].DurationSeconds, Is.EqualTo(65));
            Assert.That(StepBreakdown.Compute(points, 5)[0].Line1, Does.StartWith("1:05"));
            Assert.That(StepBreakdown.Compute(points, 1)[0].DurationSeconds, Is.EqualTo(13), "otro intervalo (It. 13)");
            Assert.That(HistoryRules.AppSampleIntervalSeconds, Is.EqualTo(5));
            Assert.That(StepBreakdown.Compute(points, 5)[0].Badge, Is.EqualTo("8"), "índice + 1");
        }
    }
}
