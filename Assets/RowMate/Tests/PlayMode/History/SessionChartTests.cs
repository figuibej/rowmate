using System.Collections;
using NUnit.Framework;
using RowMate.Core.History;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode.History
{
    // SCH · El gráfico del detalle (F-HIS-13 a -15; AC-HIS-12-02, AC-HIS-13-06, -07; 02-tech-design §5.4, §5.5).
    public sealed class SessionChartTests : HistoryUiTestBase
    {
        [UnityTest, Category("AC-HIS-13-06")]
        public IEnumerator MeshAndLabels()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            var chart = Detail.Chart;
            Assert.That(chart.Current, Is.Not.Null);
            Assert.That(chart.LastVertexCount, Is.GreaterThan(100), "la malla está");
            Assert.That(chart.Labels.YTexts(), Is.EqualTo(new[] { "0", "50", "100", "150", "198" }));
            Assert.That(chart.Labels.XTexts(), Is.EqualTo(new[] { "0:05", "2:00", "4:00", "6:00", "8:00", "10:00", "11:00" }));
            Assert.That(chart.Current!.BandCount, Is.EqualTo(8), "8 franjas");
            Assert.That(chart.Current.SeparatorCount, Is.EqualTo(7), "7 separadores");
            Assert.That(chart.Current.TargetCount, Is.EqualTo(6), "mínimo y máximo de los 3 trabajos");
            Assert.That(chart.Current.SeriesColor.ToHex(), Is.EqualTo(RowMate.Core.Design.Tokens.MetricWatts.Rgb.ToHex()));
            var label = chart.Labels.XAt(0);
            Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(11).Within(0.01f), "ejes de 11");
            Assert.That(chart.Labels.XAt(chart.Labels.XCount - 1).worldBound.xMax, Is.LessThanOrEqualTo(chart.worldBound.xMax + 0.5f), "adentro del marco");
            yield return Tap("session-metric-spm");
            chart.FinishEntry();
            yield return Frames();
            Assert.That(chart.Labels.YTexts(), Is.EqualTo(new[] { "0", "5", "10", "15", "20", "25", "26" }));
            yield return Tap("session-metric-split");
            chart.FinishEntry();
            yield return Frames();
            Assert.That(chart.Labels.YTexts(), Is.EqualTo(new[] { "3:00", "2:50", "2:40", "2:30", "2:20", "2:10", "2:00" }));
            yield return Tap("session-metric-hr");
            chart.FinishEntry();
            yield return Frames();
            Assert.That(chart.Labels.YTexts(), Is.EqualTo(new[] { "0", "50", "100", "150", "165" }));
            Assert.That(chart.Current!.TargetCount, Is.EqualTo(0), "Pulso sin objetivos");
        }

        [UnityTest, Category("AC-HIS-13-07")]
        public IEnumerator Tooltip()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return OpenCard(0);
            var chart = Detail.Chart;
            chart.ShowCursorAt(300);
            yield return Frames();
            Assert.That(chart.Cursor.TooltipText, Is.EqualTo("150.0 W"));
            Assert.That(chart.Cursor.IsShown, Is.True);
            Detail.Store.SelectMetric(ChartMetric.Split);
            chart.FinishEntry();
            chart.ShowCursorAt(300);
            yield return Frames();
            Assert.That(chart.Cursor.TooltipText, Is.EqualTo("2:13 s"));
            Assert.That(chart.Cursor.GuideX, Is.EqualTo(chart.Current!.PixelX(300)).Within(0.5), "guía en x(300)");
            chart.ShowCursorAt(425);
            yield return Frames();
            Assert.That(chart.Cursor.TooltipText, Is.EqualTo("--:-- s"), "sin ritmo");
            var tip = chart.Cursor.Tooltip.worldBound;
            Assert.That(tip.xMin, Is.GreaterThanOrEqualTo(chart.worldBound.xMin + 3.5f));
            Assert.That(tip.xMax, Is.LessThanOrEqualTo(chart.worldBound.xMax - 3.5f));
            chart.Cursor.Hide();
            yield return Settle();
            Assert.That(chart.Cursor.IsShown, Is.False, "al salir se va");
            Assert.That(chart.Cursor.TooltipOpacity, Is.EqualTo(0).Within(0.001f));
        }

        [UnityTest, Category("AC-HIS-13-07")]
        public IEnumerator CursorDoesNotRepaintTheMesh()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return OpenCard(0);
            yield return Frames(3);
            var chart = Detail.Chart;
            var meshDraws = chart.GenerateCount;
            var cursorDraws = chart.Cursor.GenerateCount;
            for (var s = 10; s <= 600; s += 40)
            {
                chart.Cursor.ShowAtSeconds(s);
                yield return null;
            }

            Assert.That(chart.Cursor.GenerateCount, Is.GreaterThan(cursorDraws), "la capa del cursor se repinta");
            Assert.That(chart.GenerateCount, Is.EqualTo(meshDraws), "la malla grande no");
            Detail.Scroll.scrollOffset = new Vector2(0, 200);
            yield return Frames(3);
            Assert.That(chart.GenerateCount, Is.EqualTo(meshDraws), "desplazar la página tampoco");
        }

        [UnityTest, Category("AC-HIS-12-02")]
        public IEnumerator SwitchTiming()
        {
            CreateHistory(HistorySeeds.F9);
            yield return GoToHistory();
            yield return OpenCard(0);
            var chart = Detail.Chart;
            Detail.Store.SelectMetric(ChartMetric.Spm);
            Assert.That(chart.IsAnimating, Is.True, "fundido cruzado");
            H.Advance(0.11);
            Assert.That(chart.Visual.NewSeriesAlpha, Is.EqualTo(0.5).Within(0.05), "a la mitad de los 220");
            H.Advance(0.25);
            Assert.That(chart.IsAnimating, Is.False, "terminó a los 300");
            Assert.That(chart.Visual.YMax, Is.EqualTo(chart.Current!.YMax).Within(1e-6));
            Assert.That(Detail.Store.Metric.Value, Is.EqualTo(ChartMetric.Spm), "la animación no cambia los datos");
            yield return null;
        }

        [UnityTest, Category("AC-HIS-12-02")]
        public IEnumerator SwitchReduceMotion()
        {
            CreateHistory(HistorySeeds.F9, null, o => o.ReduceMotion = true);
            yield return GoToHistory();
            yield return OpenCard(0);
            var chart = Detail.Chart;
            Assert.That(chart.EntryDone, Is.True, "la entrada en su final desde el cuadro 0");
            Detail.Store.SelectMetric(ChartMetric.Split);
            Assert.That(chart.IsAnimating, Is.False, "instantáneo");
            Assert.That(chart.Visual.NewSeriesAlpha, Is.EqualTo(1));
            Assert.That(chart.Visual.OldSeriesAlpha, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest, Category("AC-HIS-13-07")]
        public IEnumerator MouseHoverShowsAndLeaves()
        {
            CreateHistory(HistorySeeds.F9, DeviceProfile.W1280);
            yield return GoToHistory();
            yield return OpenCard(0);
            H.ScrollTo("session-chart");
            yield return Frames(3);
            var chart = Detail.Chart;
            var area = chart.Current!.Frame.Area;
            var origin = chart.worldBound.position;
            var from = origin + new Vector2((float)area.Left + 20, (float)area.CenterY);
            H.DragAt(from, from + new Vector2(200, 0));
            yield return Frames();
            Assert.That(chart.Cursor.IsShown, Is.True, "el puntero sobre el área muestra el cursor");
            Assert.That(chart.Cursor.Index, Is.GreaterThan(0));
            H.PointerLeave("chart-cursor");
            yield return Settle();
            Assert.That(chart.Cursor.IsShown, Is.False, "al salir, se va");
        }
    }
}
