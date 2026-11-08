using System.Linq;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.History
{
    // CS · Los datos del gráfico (AC-HIS-13-01 a -03; sd:291-311): x del monitor, los valores con los ceros, el split invertido con
    // el ritmo 0 al piso (Q-24), sin ritmo sin etiquetas Y y un solo punto sin fallar (gate m-02).
    [TestFixture]
    public sealed class ChartSeriesTests
    {
        private static DataPoint P(int t, int watts, int pace, int hr = 0)
        {
            return new DataPoint(1, t) { PowerWatts = watts, Pace500mSeconds = pace, StrokeRate = 20, DistanceMeters = t * 3, HeartRate = hr };
        }

        [Test, Category("AC-HIS-13-01"), Category("AC-HIS-13-02")]
        public void XIsTheMonitorTimeAndZerosAreKept()
        {
            var data = SessionChartData.Build(new[] { P(224, 150, 130), P(229, 0, 0), P(234, 160, 140) }, new IntervalStep[0]);
            Assert.That(data.X, Is.EqualTo(new double[] { 224, 229, 234 }), "no arranca en 0 (Q-15)");
            Assert.That(data.Raw(ChartMetric.Watts), Is.EqualTo(new double[] { 150, 0, 160 }));
            Assert.That(data.Plotted(ChartMetric.Watts), Is.EqualTo(new float[] { 150, 0, 160 }));
            Assert.That(data.Raw(ChartMetric.Distance), Is.EqualTo(new double[] { 672, 687, 702 }));
            Assert.That(data.View(ChartMetric.Watts).X.Min, Is.EqualTo(224));
            Assert.That(data.Bands, Is.Empty);
        }

        [Test, Category("AC-HIS-13-03")]
        public void SplitIsInvertedWithStopsOnTheFloor()
        {
            var data = SessionChartData.Build(new[] { P(5, 150, 150), P(10, 0, 0), P(15, 150, 133), P(20, 150, 180) }, new IntervalStep[0]);
            Assert.That(data.SlowestPace, Is.EqualTo(180), "el máximo de los ritmos > 0");
            Assert.That(data.Plotted(ChartMetric.Split), Is.EqualTo(new float[] { 30, 0, 47, 0 }), "lo rápido arriba; la parada al piso");
            Assert.That(data.Raw(ChartMetric.Split), Is.EqualTo(new double[] { 150, 0, 133, 180 }), "el tooltip usa el ritmo original");
            Assert.That(data.View(ChartMetric.Split).YLabels.First(), Is.EqualTo("3:00"));
        }

        [Test, Category("AC-HIS-13-03")]
        public void WithoutPaceTheCurveIsFlatAndHasNoYLabels()
        {
            var data = SessionChartData.Build(new[] { P(5, 150, 0), P(10, 150, 0) }, new IntervalStep[0]);
            Assert.That(data.HasPace, Is.False);
            Assert.That(data.Plotted(ChartMetric.Split), Is.EqualTo(new float[] { 0, 0 }));
            Assert.That(data.View(ChartMetric.Split).YLabels, Is.Empty);
            Assert.That(data.View(ChartMetric.Split).Y.Max, Is.EqualTo(100), "máximo ≤ 0 → 100");
            Assert.That(data.View(ChartMetric.HeartRate).YLabels.Last(), Is.EqualTo("100"), "pulso plano en 0: eje de 100 con etiquetas");
        }

        [Test, Category("AC-HIS-13-01")]
        public void OnePointDoesNotFail()
        {
            var data = SessionChartData.Build(new[] { P(5, 150, 133, 150) }, new IntervalStep[0]);
            var view = data.View(ChartMetric.Watts);
            Assert.That(view.X.IsDegenerate, Is.True);
            Assert.That(view.XLabels, Is.EqualTo(new[] { "0:05" }));
            var geometry = new ChartGeometry();
            geometry.Prepare(data, ChartMetric.Watts, ChartFrame.For(379, 220, 2.625));
            Assert.That(geometry.PixelX(5), Is.EqualTo(geometry.Frame.Area.CenterX), "x centrada (gate m-02)");
            Assert.That(geometry.IsSinglePoint, Is.True);
            var buffer = new ChartMeshBuffer(geometry.VertexBudget, geometry.IndexBudget);
            ChartGeometry.WriteFrame(buffer, geometry, null, ChartVisual.Rest(geometry.YMax));
            Assert.That(buffer.VertexCount, Is.GreaterThan(0));
        }

        [Test, Category("AC-HIS-13-02")]
        public void ViewsAreCachedPerMetric()
        {
            var data = SessionChartData.Build(HistorySeeds.Points(1), RoutineExpansion.Flatten(HistorySeeds.Escalera().Steps));
            Assert.That(data.View(ChartMetric.Watts), Is.SameAs(data.View(ChartMetric.Watts)));
            Assert.That(data.View(ChartMetric.Watts).MaxPlotted, Is.EqualTo(180), "los objetivos cuentan para el máximo (160/180 > 150)");
        }
    }
}
