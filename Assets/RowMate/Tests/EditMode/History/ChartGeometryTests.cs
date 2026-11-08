using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.History
{
    // CG · La geometría del gráfico (A9-01, A9-02; AC-HIS-13-06, -14-03, -15-02; 02-tech-design §2.9, §6): el marco del arte §3,
    // los colores y alfas, los separadores salvo el primero, el punteado anclado, el revelado por prefijo y la capacidad con la
    // sesión de 60 min a 1 Hz.
    [TestFixture]
    public sealed class ChartGeometryTests
    {
        private const double Tv = 2.625;

        private static readonly IReadOnlyList<IntervalStep> Flat = RoutineExpansion.Flatten(HistorySeeds.Escalera().Steps);

        private static ChartGeometry Prepared(IReadOnlyList<DataPoint> points, ChartMetric metric, double width = 379, IReadOnlyList<IntervalStep>? flat = null)
        {
            var geometry = new ChartGeometry();
            geometry.Prepare(SessionChartData.Build(points, flat ?? Flat), metric, ChartFrame.For(width, ChartSpecs.Height, Tv));
            return geometry;
        }

        private static ChartMeshBuffer Buffer(ChartGeometry geometry, ChartGeometry? other = null)
        {
            return new ChartMeshBuffer(geometry.VertexBudget + (other?.VertexBudget ?? 0), geometry.IndexBudget + (other?.IndexBudget ?? 0));
        }

        [Test, Category("AC-HIS-13-06")]
        public void FrameOfTheArt()
        {
            var frame = ChartFrame.For(379, 220, Tv);
            Assert.That(frame.Area.Left, Is.EqualTo(48));
            Assert.That(frame.Area.Right, Is.EqualTo(363));
            Assert.That(frame.Area.Width, Is.EqualTo(315), "arte §3: 315 × 172 en TV");
            Assert.That(frame.Area.Top, Is.EqualTo(18));
            Assert.That(frame.Area.Bottom, Is.EqualTo(190));
            Assert.That(frame.StripTop, Is.EqualTo(12));
            Assert.That(frame.PixelDp, Is.EqualTo(1 / Tv).Within(1e-12));
        }

        [TestCase(ChartMetric.Watts, "#FFD60A")]
        [TestCase(ChartMetric.Spm, "#00B4D8")]
        [TestCase(ChartMetric.Split, "#FF9F1C")]
        [TestCase(ChartMetric.Distance, "#4CC9F0")]
        [TestCase(ChartMetric.HeartRate, "#EF476F")]
        [Category("AC-HIS-13-06")]
        public void Style(ChartMetric metric, string hex)
        {
            var geometry = Prepared(HistorySeeds.Points(2), metric);
            Assert.That(geometry.SeriesColor.ToHex(), Is.EqualTo(hex));
            var line = Buffer(geometry);
            geometry.WriteSeriesLine(line, geometry.YMax, 1, 1);
            // 4 vértices por punto: borde (alfa 0), núcleo, núcleo, borde.
            Assert.That(line.VertexCount, Is.EqualTo(4 * geometry.SeriesCount));
            Assert.That(ChartMeshBuffer.AlphaOf(line.Rgba[0]), Is.EqualTo(0));
            Assert.That(ChartMeshBuffer.AlphaOf(line.Rgba[1]), Is.EqualTo(255));
            Assert.That(line.Rgba[1] >> 8, Is.EqualTo(ChartMeshBuffer.Pack(Rgb.Parse(hex), 1) >> 8));
            // Área en degradé: arriba del área 24 %, en la base 0.
            var area = Buffer(geometry);
            geometry.WriteSeriesArea(area, geometry.YMax, 1, 1);
            Assert.That(area.VertexCount, Is.EqualTo(2 * geometry.SeriesCount));
            for (var i = 0; i < area.VertexCount; i += 2)
            {
                var k = (geometry.Frame.Area.Bottom - area.Y[i]) / geometry.Frame.Area.Height;
                Assert.That(ChartMeshBuffer.AlphaOf(area.Rgba[i]), Is.EqualTo((int)(0.24 * k * 255 + 0.5)).Within(1));
                Assert.That(ChartMeshBuffer.AlphaOf(area.Rgba[i + 1]), Is.EqualTo(0));
            }
        }

        [Test, Category("AC-HIS-13-06")]
        public void GridOnlyOnInteriorTicksAndABaseline()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            var buffer = Buffer(geometry);
            geometry.WriteGrid(buffer, geometry.YMax, 1);
            Assert.That(buffer.VertexCount, Is.EqualTo(8 * 3), "50, 100 y 150 (sin el 0 ni el 198)");
            Assert.That(ChartMeshBuffer.AlphaOf(buffer.Rgba[0]), Is.EqualTo((int)(0.08 * 255 + 0.5)));
            buffer.Clear();
            geometry.WriteBaseline(buffer, 1);
            Assert.That(buffer.VertexCount, Is.EqualTo(8));
            Assert.That(ChartMeshBuffer.AlphaOf(buffer.Rgba[0]), Is.EqualTo((int)(0.12 * 255 + 0.5)), "border.default");
        }

        [Test, Category("AC-HIS-14-03")]
        public void Separators()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            Assert.That(geometry.BandCount, Is.EqualTo(8));
            Assert.That(geometry.SeparatorCount, Is.EqualTo(7), "salvo la primera");
            var buffer = Buffer(geometry);
            geometry.WriteSeparators(buffer, 1);
            var dashes = (int)Math.Ceiling(geometry.Frame.Area.Height / 8.0);
            Assert.That(buffer.VertexCount, Is.EqualTo(7 * dashes * 8));
            Assert.That(buffer.Y[0], Is.EqualTo(geometry.Frame.Area.Top).Within(1e-4), "la fase arranca arriba del área");
            Assert.That(buffer.Y[2] - buffer.Y[0], Is.EqualTo(4).Within(1e-4), "trazo de 4");
            Assert.That(buffer.X[0], Is.EqualTo(geometry.PixelX(65) - 0.5).Within(1e-3), "la primera, en el inicio de la 2.ª franja");
            Assert.That(ChartMeshBuffer.AlphaOf(buffer.Rgba[0]), Is.EqualTo((int)(0.24 * 255 + 0.5)));
            var free = Prepared(HistorySeeds.Points(2), ChartMetric.Watts);
            Assert.That(free.SeparatorCount, Is.EqualTo(0), "una sesión libre no tiene separadores");
        }

        [Test, Category("AC-HIS-15-02")]
        public void TargetDashes()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            Assert.That(geometry.TargetCount, Is.EqualTo(6));
            var full = Buffer(geometry);
            geometry.WriteTargetLines(full, geometry.YMax, 1, 1);
            var x0 = geometry.PixelX(65);
            var x1 = geometry.PixelX(185);
            Assert.That(full.X[0], Is.EqualTo(x0).Within(1e-3), "la fase se ancla al inicio de la franja");
            Assert.That(full.X[1] - full.X[0], Is.EqualTo(8).Within(1e-3), "trazo de 8");
            var perLine = (int)Math.Ceiling((x1 - x0) / 13.0);
            Assert.That(full.X[8 * (perLine - 1) + 1], Is.LessThanOrEqualTo((float)x1 + 1e-3f), "el último trazo se corta en el fin de la franja");
            Assert.That(full.Y[0], Is.EqualTo(geometry.PixelY(140, geometry.YMax) - 1).Within(1e-3), "2 dp centrada en 140");
            Assert.That(ChartMeshBuffer.AlphaOf(full.Rgba[0]), Is.EqualTo((int)(0.78 * 255 + 0.5)));
            // Revelado: los trazos que se ven no se mueven (la fase no "camina").
            var half = Buffer(geometry);
            geometry.WriteTargetLines(half, geometry.YMax, 0.5, 1);
            Assert.That(half.VertexCount, Is.LessThan(full.VertexCount));
            Assert.That(half.X[0], Is.EqualTo(full.X[0]));
            Assert.That(half.X[8], Is.EqualTo(full.X[8]));
            // Áreas: el color del tipo @7 % hasta la base.
            var areas = Buffer(geometry);
            geometry.WriteTargetAreas(areas, geometry.YMax, 1, 1);
            Assert.That(areas.VertexCount, Is.EqualTo(4 * 6), "mínimo y máximo se superponen");
            Assert.That(ChartMeshBuffer.AlphaOf(areas.Rgba[0]), Is.EqualTo((int)(0.07 * 255 + 0.5)));
            Assert.That(areas.Rgba[0] >> 8, Is.EqualTo(ChartMeshBuffer.Pack(Tokens.StepWork.Rgb, 1) >> 8));
        }

        [Test, Category("AC-HIS-14-02")]
        public void BandsAndStrip()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            var buffer = Buffer(geometry);
            geometry.WriteBands(buffer, 1);
            Assert.That(buffer.VertexCount, Is.EqualTo(4 * 8));
            Assert.That(buffer.Rgba[0] >> 8, Is.EqualTo(ChartMeshBuffer.Pack(Tokens.StepWarmup.Rgb, 1) >> 8));
            Assert.That(ChartMeshBuffer.AlphaOf(buffer.Rgba[0]), Is.EqualTo((int)(0.10 * 255 + 0.5)));
            Assert.That(buffer.Y[0], Is.EqualTo(geometry.Frame.Area.Top));
            Assert.That(buffer.Y[2], Is.EqualTo(geometry.Frame.Area.Bottom), "a todo el alto");
            buffer.Clear();
            geometry.WriteStrip(buffer, 1);
            Assert.That(buffer.Y[0], Is.EqualTo(12));
            Assert.That(buffer.Y[2], Is.EqualTo(15), "3 dp encima del área");
            Assert.That(buffer.X[0], Is.EqualTo(geometry.PixelX(5) + 1).Within(1e-3), "2 dp de hueco entre franjas");
        }

        [Test, Category("AC-HIS-13-06")]
        public void RevealDrawsAPrefixUpToTheCut()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            var full = Buffer(geometry);
            geometry.WriteSeriesLine(full, geometry.YMax, 1, 1);
            var half = Buffer(geometry);
            geometry.WriteSeriesLine(half, geometry.YMax, 0.5, 1);
            var cut = geometry.Frame.Area.Left + 0.5 * geometry.Frame.Area.Width;
            Assert.That(half.VertexCount, Is.LessThan(full.VertexCount));
            for (var i = 0; i < half.VertexCount - 4; i++)
            {
                Assert.That(half.X[i], Is.EqualTo(full.X[i]).Within(2e-3), "el prefijo no se rehace");
            }

            var last = half.VertexCount - 4;
            Assert.That((half.X[last + 1] + half.X[last + 2]) / 2, Is.EqualTo(cut).Within(0.05), "el último tramo, interpolado hasta el corte");
            var none = Buffer(geometry);
            geometry.WriteSeriesLine(none, geometry.YMax, 0, 1);
            Assert.That(none.VertexCount, Is.EqualTo(0));
        }

        [Test, Category("AC-HIS-13-06")]
        public void AnimatedYMaxRescalesWithoutTouchingX()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            var a = Buffer(geometry);
            geometry.WriteSeriesLine(a, 198, 1, 1);
            var b = Buffer(geometry);
            geometry.WriteSeriesLine(b, 396, 1, 1);
            Assert.That(b.VertexCount, Is.EqualTo(a.VertexCount));
            var area = geometry.Frame.Area;
            Assert.That(area.Bottom - (b.Y[1] + b.Y[2]) / 2, Is.EqualTo((area.Bottom - (a.Y[1] + a.Y[2]) / 2) / 2).Within(0.05));
        }

        [Test, Category("AC-HIS-13-02")]
        public void TheStressSessionFitsTheBudget()
        {
            // 02-tech-design §6: ≤ 4 500 vértices en TV y ≤ 9 500 en W1280 con un cuadro quieto (una curva). En el fundido cruzado
            // de 220 ms hay dos curvas: la capacidad del buffer es la suma (desvío anotado en WP-A).
            var points = HistorySeeds.Stress60MinAt1Hz();
            foreach (var width in new[] { 379.0, 1248.0 })
            {
                var watts = Prepared(points, ChartMetric.Watts, width, new IntervalStep[0]);
                var split = Prepared(points, ChartMetric.Split, width, new IntervalStep[0]);
                var limit = width < 600 ? 4500 : 9500;
                Assert.That(watts.VertexBudget, Is.LessThanOrEqualTo(limit), "tope de un cuadro quieto en " + width);
                var rest = Buffer(watts);
                ChartGeometry.WriteFrame(rest, watts, null, ChartVisual.Rest(watts.YMax));
                Assert.That(rest.VertexCount, Is.LessThanOrEqualTo(watts.VertexBudget));
                Assert.That(rest.IndexCount, Is.LessThanOrEqualTo(watts.IndexBudget));
                var both = Buffer(split, watts);
                ChartGeometry.WriteFrame(both, split, watts, ChartTimeline.Switch(110, watts.YMax, split.YMax, false));
                Assert.That(both.VertexCount, Is.LessThanOrEqualTo(both.VertexCapacity));
                Assert.That(both.VertexCount, Is.LessThan(ChartMeshBuffer.MaxVertices));
                Assert.That(watts.ReducedCount, Is.LessThanOrEqualTo(2 * ChartSpecs.MaxColumns + 2), "la reducción por columna");
            }
        }

        [Test, Category("AC-HIS-13-06")]
        public void WritingAFrameDoesNotGrowTheBuffer()
        {
            var geometry = Prepared(HistorySeeds.Points(1), ChartMetric.Watts);
            var buffer = Buffer(geometry);
            var vertices = buffer.X;
            var indices = buffer.Indices;
            for (var t = 0; t <= 700; t += 16)
            {
                ChartGeometry.WriteFrame(buffer, geometry, null, ChartTimeline.Entry(t, geometry.YMax, false));
            }

            Assert.That(buffer.X, Is.SameAs(vertices), "los arreglos se reusan");
            Assert.That(buffer.Indices, Is.SameAs(indices));
            Assert.Throws<InvalidOperationException>(() => ChartGeometry.WriteFrame(new ChartMeshBuffer(10, 10), geometry, null,
                ChartVisual.Rest(geometry.YMax)), "la capacidad es fija: pasarse es un error, no crece");
        }
    }
}
