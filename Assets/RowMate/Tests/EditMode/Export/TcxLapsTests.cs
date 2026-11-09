using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // TL · Las vueltas (AC-STR-09-04; legacy: _groupPointsByInterval, tcx_builder.dart:83-116; DV-118).
    [TestFixture]
    public sealed class TcxLapsTests
    {
        private static DataPoint P(int seconds, int? step)
        {
            return new DataPoint(1, seconds) { StepIndex = step };
        }

        private static int[] Counts(IReadOnlyList<DataPoint> points)
        {
            return TcxLaps.Group(points).Select(l => l.Count).ToArray();
        }

        [Test, Category("AC-STR-09-04")]
        public void NoIndexOneLap()
        {
            var laps = TcxLaps.Group(new[] { P(5, null), P(10, null), P(15, null) });
            Assert.That(laps.Count, Is.EqualTo(1));
            Assert.That(laps[0].Start, Is.EqualTo(0));
            Assert.That(laps[0].Count, Is.EqualTo(3));
            Assert.That(TcxLaps.Group(new DataPoint[0]).Count, Is.EqualTo(0));
        }

        [Test, Category("AC-STR-09-04")]
        public void ReturningIndexOpensAnother()
        {
            Assert.That(Counts(TcxSeeds.T1.Points), Is.EqualTo(new[] { 3, 2, 1 }), "T-1: 0, 0, 0, 1, 1, 0 → 3 vueltas");
            Assert.That(Counts(new[] { P(5, 0), P(10, 1), P(15, 0), P(20, 1) }), Is.EqualTo(new[] { 1, 1, 1, 1 }));
        }

        [Test, Category("AC-STR-09-04")]
        public void NullRunIsItsOwnLap()
        {
            Assert.That(Counts(new[] { P(5, 0), P(10, null), P(15, null), P(20, 1) }), Is.EqualTo(new[] { 1, 2, 1 }));
            Assert.That(Counts(new[] { P(5, null), P(10, 2), P(15, 2) }), Is.EqualTo(new[] { 1, 2 }), "la racha inicial sin índice");
        }

        [Test, Category("AC-STR-09-04")]
        public void F9Laps_1is8_2is1_3is2()
        {
            Assert.That(TcxLaps.Group(HistorySeeds.Points(1)).Count, Is.EqualTo(8), "n.º 1: los 8 pasos de Escalera");
            Assert.That(TcxLaps.Group(HistorySeeds.Points(2)).Count, Is.EqualTo(1), "n.º 2: libre");
            Assert.That(TcxLaps.Group(HistorySeeds.Points(3)).Count, Is.EqualTo(2), "n.º 3: cortada en el segundo paso");
            var laps = TcxLaps.Group(HistorySeeds.Points(1));
            Assert.That(laps.Sum(l => l.Count), Is.EqualTo(HistorySeeds.Points(1).Count), "todos los puntos, una vez");
            for (var i = 1; i < laps.Count; i++)
            {
                Assert.That(laps[i].Start, Is.EqualTo(laps[i - 1].Last + 1), "consecutivas");
            }
        }
    }
}
