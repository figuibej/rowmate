using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // TR · Reglas de la exportación (AC-STR-09-06, -07, -10; legacy: tcx_builder.dart:177-179, profile_provider.dart:159-188).
    [TestFixture]
    public sealed class TcxExportRulesTests
    {
        [TestCase(60, 50)]
        [TestCase(3, 3)]
        [TestCase(2, 2)]
        [TestCase(1, 1)]
        [Category("AC-STR-09-06")]
        public void LapCaloriesHalfAwayFromZero(int points, int calories)
        {
            // 100 kcal y 600 s: 100 × n / 120; 3 → 2,5 → 3 (mitad lejos de cero, nunca el del banquero).
            Assert.That(TcxExportRules.LapCalories(100, points, 600), Is.EqualTo(calories));
        }

        [Test, Category("AC-STR-09-06")]
        public void ZeroTimeGivesZero()
        {
            Assert.That(TcxExportRules.LapCalories(100, 3, 0), Is.EqualTo(0), "el legacy daba error (DV-119)");
            Assert.That(TcxExportRules.LapCalories(100, 3, -5), Is.EqualTo(0));
            Assert.That(TcxExportRules.LapCalories(0, 3, 600), Is.EqualTo(0));
            Assert.That(TcxExportRules.LapCalories(7, 1, 25), Is.EqualTo(1), "7 / 5 = 1,4 → 1");
            Assert.That(TcxExportRules.LapCalories(9, 1, 30), Is.EqualTo(2), "9 / 6 = 1,5 → 2");
        }

        [Test, Category("AC-STR-09-07")]
        public void CutSessionUsesPointMaxima()
        {
            var t3 = TcxSeeds.T3;
            Assert.That(TcxExportRules.NeedsRepair(t3.Session), Is.True, "sin fin");
            var basis = TcxExportRules.CalorieBasis(t3.Session, SessionStats.Compute(t3.Points));
            Assert.That(basis.TotalSeconds, Is.EqualTo(15));
            Assert.That(basis.TotalCalories, Is.EqualTo(3));
            Assert.That(TcxExportRules.LapCalories(basis.TotalCalories, 3, basis.TotalSeconds), Is.EqualTo(3), "T-3: una vuelta de 3 kcal");
            var finishedZero = TcxSeeds.T1.Session with { TotalTimeSeconds = 0 };
            Assert.That(TcxExportRules.NeedsRepair(finishedZero), Is.True, "terminada con tiempo 0");
            var t1 = TcxSeeds.T1;
            Assert.That(TcxExportRules.NeedsRepair(t1.Session), Is.False);
            var own = TcxExportRules.CalorieBasis(t1.Session, SessionStats.Compute(t1.Points));
            Assert.That(own.TotalSeconds, Is.EqualTo(600), "terminada: los totales de la sesión");
            Assert.That(own.TotalCalories, Is.EqualTo(100));
            Assert.That(t3.Session.TotalTimeSeconds, Is.EqualTo(0), "la sesión no se modifica");
        }

        [Test, Category("AC-STR-09-10")]
        public void IsExportable()
        {
            Assert.That(TcxExportRules.IsExportable(SessionStats.Compute(TcxSeeds.T5.Points), 1), Is.False, "T-5: tiempo y distancia 0");
            Assert.That(TcxExportRules.IsExportable(SessionStats.Empty, 0), Is.False, "sin puntos");
            Assert.That(TcxExportRules.IsExportable(SessionStats.Compute(TcxSeeds.T6.Points), 1), Is.True, "T-6: 10 m en 0 s");
            Assert.That(TcxExportRules.IsExportable(SessionStats.Compute(TcxSeeds.T3.Points), 3), Is.True);
            foreach (var number in new[] { 1, 2, 3, 5, 6 })
            {
                var points = HistorySeeds.Points(number);
                Assert.That(TcxExportRules.IsExportable(SessionStats.Compute(points), points.Count), Is.True, "F-9 n.º " + number);
            }

            Assert.That(TcxExportRules.IsExportable(SessionStats.Compute(HistorySeeds.Points(4)), 0), Is.False, "F-9 n.º 4");
        }
    }
}
