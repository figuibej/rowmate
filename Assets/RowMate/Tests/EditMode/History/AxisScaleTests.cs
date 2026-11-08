using NUnit.Framework;
using RowMate.Core.History;

namespace RowMate.Tests.EditMode.History
{
    // AX · Las escalas de fl_chart 1.1.1 portadas al pie de la letra (AC-HIS-13-04, -05): roundInterval (utils.dart:134-187, debajo
    // de 1 con el toString de Dart), getEfficientInterval (:117-131) con 196 / 40 = 4 divisiones (DT9-09) e iterateThroughAxis
    // (axis_chart_helper.dart:23-58) con el módulo de Dart en getBestInitialIntervalValue (utils.dart:299-310).
    [TestFixture]
    public sealed class AxisScaleTests
    {
        [TestCase(0.0042, 0.005)]
        [TestCase(0.37, 0.5)]
        [TestCase(0.123456, 0.1)]
        [TestCase(0.0000001, 0.0000001)]
        [TestCase(1.0, 1.0)]
        [TestCase(1.59, 1.0)]
        [TestCase(1.6, 2.0)]
        [TestCase(2.6, 5.0)]
        [TestCase(7.6, 10.0)]
        [TestCase(9.99, 10.0)]
        [TestCase(49.5, 50.0)]
        [TestCase(619.6, 500.0)]
        [TestCase(6.6275, 5.0)]
        [TestCase(14.85, 10.0)]
        [TestCase(41.25, 50.0)]
        [Category("AC-HIS-13-04")]
        public void RoundInterval(double input, double expected)
        {
            Assert.That(FlChartInterval.Round(input), Is.EqualTo(expected).Within(1e-12));
        }

        [Test, Category("AC-HIS-13-04")]
        public void EfficientIntervalUsesFourDivisions()
        {
            Assert.That(FlChartInterval.Efficient(196, 198), Is.EqualTo(50), "198 / 4 = 49,5 → 50");
            Assert.That(FlChartInterval.Efficient(196, 0), Is.EqualTo(1));
            Assert.That(FlChartInterval.Efficient(80, 9), Is.EqualTo(4.5), "con 2 divisiones o menos, sin redondear");
        }

        [Test, Category("AC-HIS-13-04")]
        public void DartModuloInTheInitialValue()
        {
            Assert.That(AxisTicks.BestInitialValue(5, 660, 120), Is.EqualTo(120), "−5 % 120 = 115 en Dart (en C#, −5)");
            Assert.That(AxisTicks.BestInitialValue(224, 2024, 300), Is.EqualTo(300));
            Assert.That(AxisTicks.BestInitialValue(0, 198, 50), Is.EqualTo(0));
            Assert.That(AxisTicks.BestInitialValue(5, 5, 30), Is.EqualTo(5), "rango 0");
        }

        [Test, Category("AC-HIS-13-04")]
        public void IterateThroughAxis()
        {
            Assert.That(AxisTicks.Iterate(0, 198, 0, 50, true, true), Is.EqualTo(new double[] { 0, 50, 100, 150, 198 }));
            Assert.That(AxisTicks.Iterate(0, 198, 0, 50, false, false), Is.EqualTo(new double[] { 50, 100, 150 }), "la grilla: sin 0 ni máximo");
            Assert.That(AxisTicks.Iterate(-3, 3, 0, 2, true, true), Is.EqualTo(new double[] { -3, -2, 0, 2, 3 }), "pasa por el 0");
            Assert.That(AxisTicks.Iterate(0, 100, 0, 20, true, true), Is.EqualTo(new double[] { 0, 20, 40, 60, 80, 100 }), "el máximo no se repite");
            Assert.That(AxisTicks.Iterate(0, 100, 0, 20, false, false), Is.EqualTo(new double[] { 20, 40, 60, 80 }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => AxisTicks.Iterate(0, 1, 0, 0, true, true));
        }

        [Test, Category("AC-HIS-13-04")]
        public void YAxisVectorsOfTheSpec()
        {
            Assert.That(YAxis.For(180).Ticks.Length, Is.EqualTo(5));
            Assert.That(YAxis.For(180).Max, Is.EqualTo(198).Within(1e-9));
            Assert.That(YAxis.For(24.1).Ticks, Is.EqualTo(new[] { 0, 5, 10, 15, 20, 25, 24.1 * 1.1 }), "SPM 0 … 25 26");
            Assert.That(YAxis.For(54).Interval, Is.EqualTo(10), "split: 59,4 / 4 → 10");
            Assert.That(YAxis.For(150).Ticks, Is.EqualTo(new[] { 0, 50, 100, 150, 150 * 1.1 }));
            Assert.That(YAxis.For(0).Max, Is.EqualTo(100));
            Assert.That(YAxis.For(-5).Max, Is.EqualTo(100));
        }

        [Test, Category("AC-HIS-13-05")]
        public void XAxisVectorsOfTheSpec()
        {
            Assert.That(XAxis.For(5, 660).Ticks, Is.EqualTo(new double[] { 5, 120, 240, 360, 480, 600, 660 }));
            Assert.That(XAxis.For(224, 2024).Ticks, Is.EqualTo(new double[] { 224, 300, 600, 900, 1200, 1500, 1800, 2024 }));
            Assert.That(XAxis.For(5, 180).Ticks, Is.EqualTo(new double[] { 5, 30, 60, 90, 120, 150, 180 }));
            var fifth = XAxis.For(5, 3725);
            Assert.That(fifth.Interval, Is.EqualTo(300));
            Assert.That(fifth.Ticks[fifth.Ticks.Length - 1], Is.EqualTo(3725));
        }

        [TestCase(0, 30)]
        [TestCase(299, 30)]
        [TestCase(300, 60)]
        [TestCase(599, 60)]
        [TestCase(600, 120)]
        [TestCase(1799, 120)]
        [TestCase(1800, 300)]
        [Category("AC-HIS-13-05")]
        public void XIntervalByTheLastPoint(int last, double expected)
        {
            Assert.That(HistoryRules.XIntervalFor(last), Is.EqualTo(expected));
            Assert.That(HistoryRules.XIntervalFor(null), Is.EqualTo(60));
        }
    }
}
