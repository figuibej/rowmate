using System;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.History;

namespace RowMate.Tests.EditMode.History
{
    // CD · La reducción por columna (DT9-10): solo con más puntos que columnas; mínimo y máximo en el orden en que ocurren (picos y
    // caídas a 0 conservados); el primero y el último siempre.
    [TestFixture]
    public sealed class ChartDecimationTests
    {
        [Test, Category("AC-HIS-13-02")]
        public void IdentityWhenThereAreFewerPointsThanColumns()
        {
            var xs = new double[] { 0, 1, 2 };
            var ys = new float[] { 5, 6, 7 };
            var output = new int[3];
            Assert.That(ChartDecimation.MinMaxPerColumn(xs, ys, 3, 0, 2, 10, output), Is.EqualTo(3));
            Assert.That(output, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test, Category("AC-HIS-13-02")]
        public void KeepsPeaksAndDropsToZero()
        {
            var n = 1000;
            var xs = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
            var ys = Enumerable.Range(0, n).Select(i => i == 503 ? 0f : i == 701 ? 400f : 150f + (i % 3)).ToArray();
            var output = new int[ChartDecimation.Capacity(n, 100)];
            var count = ChartDecimation.MinMaxPerColumn(xs, ys, n, 0, n - 1, 100, output);
            var kept = output.Take(count).ToArray();
            Assert.That(count, Is.LessThanOrEqualTo(2 * 100 + 2));
            Assert.That(kept, Does.Contain(503), "la caída a 0");
            Assert.That(kept, Does.Contain(701), "el pico");
            Assert.That(kept.First(), Is.EqualTo(0));
            Assert.That(kept.Last(), Is.EqualTo(n - 1));
            Assert.That(kept, Is.Ordered.Ascending);
            Assert.That(kept.Distinct().Count(), Is.EqualTo(count), "sin repetidos");
        }

        [Test, Category("AC-HIS-13-02")]
        public void FlatColumnsGiveOnePoint()
        {
            var n = 400;
            var xs = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
            var ys = Enumerable.Repeat(140f, n).ToArray();
            var output = new int[ChartDecimation.Capacity(n, 100)];
            var count = ChartDecimation.MinMaxPerColumn(xs, ys, n, 0, n - 1, 100, output);
            Assert.That(count, Is.EqualTo(101), "uno por columna y el último");
        }
    }

    // SM · La cúbica de fl_chart (line_chart_painter.dart:565-640; curveSmoothness 0,2): subdivisión solo con 3 dp o más y hasta 8
    // partes; nunca baja del piso.
    [TestFixture]
    public sealed class ChartSmoothingTests
    {
        [Test, Category("AC-HIS-13-06")]
        public void DenseCurvesAreNotSubdivided()
        {
            var xs = new float[] { 0, 2, 4, 6 };
            var ys = new float[] { 0, 10, 0, 10 };
            var outX = new float[ChartSmoothing.Capacity(4, 6)];
            var outY = new float[outX.Length];
            Assert.That(ChartSmoothing.Tessellate(xs, ys, 4, 3, 0, outX, outY), Is.EqualTo(4));
            Assert.That(outX.Take(4), Is.EqualTo(xs));
        }

        [Test, Category("AC-HIS-13-06")]
        public void TheLegacyCubic()
        {
            // Tres puntos: (0,0), (12,12), (24,0). Tramo 1: c1 = (0,0) + 0, temp = ((24,0) − (0,0)) / 2 · 0,2 = (2,4; 0),
            // c2 = (12,12) − temp = (9,6; 12). Con 12 dp → 4 partes: t = 0,5 → B = 0,375·c1 + 0,375·c2 + 0,125·p1 = (5,1; 6).
            var xs = new float[] { 0, 12, 24 };
            var ys = new float[] { 0, 12, 0 };
            var outX = new float[ChartSmoothing.Capacity(3, 24)];
            var outY = new float[outX.Length];
            var count = ChartSmoothing.Tessellate(xs, ys, 3, 3, -100, outX, outY);
            Assert.That(count, Is.EqualTo(9), "1 + 4 + 4");
            Assert.That(outX[2], Is.EqualTo(5.1f).Within(1e-4));
            Assert.That(outY[2], Is.EqualTo(6f).Within(1e-4));
            Assert.That(outX[4], Is.EqualTo(12f));
            Assert.That(outY[4], Is.EqualTo(12f));
        }

        [Test, Category("AC-HIS-13-06")]
        public void AtMostEightPartsAndNeverBelowTheFloor()
        {
            var xs = new float[] { 0, 100, 200 };
            var ys = new float[] { 0, 0, 50 };
            var outX = new float[ChartSmoothing.Capacity(3, 200)];
            var outY = new float[outX.Length];
            var count = ChartSmoothing.Tessellate(xs, ys, 3, 3, 0, outX, outY);
            Assert.That(count, Is.EqualTo(1 + 8 + 8));
            Assert.That(outY.Take(count).Min(), Is.GreaterThanOrEqualTo(0f), "el sobrepaso de la cúbica no baja de la base");
            Assert.That(ChartSpecs.CurveSmoothness, Is.EqualTo(0.2));
        }
    }

    // HT · El punto más cercano en x, sobre los originales (AC-HIS-13-07).
    [TestFixture]
    public sealed class ChartHitTestTests
    {
        [Test, Category("AC-HIS-13-07")]
        public void Nearest()
        {
            var xs = new double[] { 5, 10, 15, 20, 20 };
            Assert.That(ChartHitTest.Nearest(xs, 5, -100), Is.EqualTo(0));
            Assert.That(ChartHitTest.Nearest(xs, 5, 11), Is.EqualTo(1));
            Assert.That(ChartHitTest.Nearest(xs, 5, 12.5), Is.EqualTo(1), "empate → el de la izquierda");
            Assert.That(ChartHitTest.Nearest(xs, 5, 13), Is.EqualTo(2));
            Assert.That(ChartHitTest.Nearest(xs, 5, 300), Is.EqualTo(3), "el punto final repetido: el primero");
            Assert.That(ChartHitTest.Nearest(xs, 0, 3), Is.EqualTo(-1));
            Assert.That(ChartHitTest.Nearest(xs, 3, 18), Is.EqualTo(2), "solo los primeros count");
        }
    }

    // TI · Las líneas de tiempo del gráfico (AC-HIS-12-02; ANIMATION_BIBLE §2.12; gate M-01).
    [TestFixture]
    public sealed class ChartTimelineTests
    {
        [Test, Category("AC-HIS-12-02")]
        public void Entry()
        {
            var start = ChartTimeline.Entry(0, 198, false);
            Assert.That(start.StaticAlpha, Is.EqualTo(0));
            Assert.That(start.Reveal, Is.EqualTo(0));
            Assert.That(start.AreaAlpha, Is.EqualTo(0));
            Assert.That(ChartTimeline.Entry(100, 198, false).Reveal, Is.EqualTo(0), "el revelado arranca a los 100");
            Assert.That(ChartTimeline.Entry(200, 198, false).StaticAlpha, Is.EqualTo(1));
            Assert.That(ChartTimeline.Entry(400, 198, false).Reveal, Is.InRange(0.5, 1.0));
            Assert.That(ChartTimeline.Entry(450, 198, false).AreaAlpha, Is.EqualTo(1));
            Assert.That(ChartTimeline.Entry(699, 198, false).Finished, Is.False);
            var end = ChartTimeline.Entry(700, 198, false);
            Assert.That(end.Finished, Is.True);
            Assert.That(end.Reveal, Is.EqualTo(1));
            Assert.That(end.YMax, Is.EqualTo(198));
        }

        [Test, Category("AC-HIS-12-02")]
        public void EntryReduceMotion()
        {
            var frame = ChartTimeline.Entry(0, 198, true);
            Assert.That(frame.Finished, Is.True, "estado final en t = 0");
            Assert.That(frame.StaticAlpha, Is.EqualTo(1));
            Assert.That(frame.Reveal, Is.EqualTo(1));
            Assert.That(frame.AreaAlpha, Is.EqualTo(1));
        }

        [Test, Category("AC-HIS-12-02")]
        public void Switch()
        {
            var start = ChartTimeline.Switch(0, 198, 59.4, false);
            Assert.That(start.OldSeriesAlpha, Is.EqualTo(1));
            Assert.That(start.NewSeriesAlpha, Is.EqualTo(0));
            Assert.That(start.YMax, Is.EqualTo(198));
            var middle = ChartTimeline.Switch(110, 198, 59.4, false);
            Assert.That(middle.NewSeriesAlpha, Is.EqualTo(0.5).Within(1e-9), "fundido cruzado lineal de 220");
            Assert.That(middle.OldSeriesAlpha + middle.NewSeriesAlpha, Is.EqualTo(1).Within(1e-9));
            Assert.That(ChartTimeline.Switch(120, 198, 59.4, false).OldLabelsAlpha, Is.EqualTo(0));
            Assert.That(ChartTimeline.Switch(100, 198, 59.4, false).NewLabelsAlpha, Is.EqualTo(0));
            Assert.That(ChartTimeline.Switch(220, 198, 59.4, false).NewSeriesAlpha, Is.EqualTo(1));
            Assert.That(ChartTimeline.Switch(220, 198, 59.4, false).NewLabelsAlpha, Is.EqualTo(1));
            Assert.That(ChartTimeline.Switch(220, 198, 59.4, false).YMax, Is.GreaterThan(59.4), "el reescalado dura 300");
            var end = ChartTimeline.Switch(300, 198, 59.4, false);
            Assert.That(end.Finished, Is.True);
            Assert.That(end.YMax, Is.EqualTo(59.4));
            Assert.That(end.NewTargetsAlpha, Is.EqualTo(1));
            Assert.That(end.OldTargetsAlpha, Is.EqualTo(0));
        }

        [Test, Category("AC-HIS-12-02")]
        public void SwitchReduceMotion()
        {
            var frame = ChartTimeline.Switch(0, 198, 59.4, true);
            Assert.That(frame.Finished, Is.True, "instantáneo (AC-HIS-12-02)");
            Assert.That(frame.YMax, Is.EqualTo(59.4));
            Assert.That(frame.OldSeriesAlpha, Is.EqualTo(0));
            Assert.That(frame.NewSeriesAlpha, Is.EqualTo(1));
            Assert.That(frame.OldLabelsAlpha, Is.EqualTo(0));
            Assert.That(frame.NewTargetsAlpha, Is.EqualTo(1));
        }

        [Test, Category("AC-HIS-13-07")]
        public void Tooltip()
        {
            Assert.That(ChartTimeline.TooltipIn(0, false).Scale, Is.EqualTo(0.96));
            Assert.That(ChartTimeline.TooltipIn(90, false).Opacity, Is.EqualTo(1));
            Assert.That(ChartTimeline.TooltipIn(90, false).Finished, Is.True);
            Assert.That(ChartTimeline.TooltipIn(0, true).Scale, Is.EqualTo(1), "con reducir movimiento, solo opacidad");
            Assert.That(ChartTimeline.TooltipOut(160, 1).Opacity, Is.EqualTo(0));
            Assert.That(ChartTimeline.TooltipOut(0, 0.5).Opacity, Is.EqualTo(0.5));
            Assert.That(ChartSpecs.TooltipInMs, Is.EqualTo(Tokens.DurMicro.Milliseconds));
            Assert.That(ChartSpecs.TooltipOutMs, Is.EqualTo(Tokens.DurFast.Milliseconds));
        }
    }
}
