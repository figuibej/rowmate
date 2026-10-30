using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // PRG: la fila de píldoras y la geometría que dibuja (02-tech-design §2.2, §3.4 de la It. 4b; 04-design-notes §3.2; Q-59,
    // DV-40, DN4b-17 a -20). legacy: immersive_workout_screen.dart:358-377; step_pill.dart:57-60, :88-103.
    [TestFixture]
    public sealed class PillRowGeometryTests
    {
        private const double Tv = 344.0;
        private const double Th = 821.3;
        private const double L640 = 647.3;
        private const double W1280 = 754.0;

        [Test, Category("AC-WKT-14-07")]
        public void PaddingAndHeight()
        {
            // :359-360: relleno 10 / 8 / 10 / 4; la píldora de 8 con radio 4; la fila de 20.
            Assert.That(PillRowGeometry.PadLeft, Is.EqualTo(10));
            Assert.That(PillRowGeometry.PadTop, Is.EqualTo(8));
            Assert.That(PillRowGeometry.PadRight, Is.EqualTo(10));
            Assert.That(PillRowGeometry.PadBottom, Is.EqualTo(4));
            Assert.That(PillRowGeometry.RowHeight, Is.EqualTo(20));
            var layout = PillRowGeometry.Layout(Tv, 6);
            var pill = layout.PillOf(0);
            Assert.That(pill.Top, Is.EqualTo(8));
            Assert.That(pill.Height, Is.EqualTo(8));
            Assert.That(layout.LeftOf(0), Is.EqualTo(10));
            Assert.That(layout.PillOf(5).Right, Is.EqualTo(10 + Tv).Within(1e-9), "la última llega al relleno derecho");
        }

        [TestCase(411, 914, 344.0)]
        [TestCase(360, 800, 344.0)]
        [TestCase(914, 411, 821.3)]
        [TestCase(640, 360, 647.3)]
        [TestCase(1280, 720, 754.0)]
        public void UsefulWidthOfTheDesignNotes(double width, double height, double useful)
        {
            // 04b/00-deuda-diseno §4.4: la barra a 12·s de cada lado, menos el borde y el relleno de 10 y 10.
            Assert.That(PillRowGeometry.UsefulWidthBase(width, HudGeometry.Scale(width, height)), Is.EqualTo(useful).Within(0.05));
        }

        [TestCase(Tv, 6, 54.8, 3)]
        [TestCase(Tv, 14, 21.8, 3)]
        [TestCase(Tv, 5, 66.4, 3)]
        [TestCase(Th, 6, 134.4, 3)]
        [TestCase(Th, 14, 55.9, 3)]
        [TestCase(L640, 6, 105.4, 3)]
        [TestCase(L640, 14, 43.5, 3)]
        [TestCase(W1280, 6, 123.2, 3)]
        [TestCase(W1280, 14, 51.1, 3)]
        [TestCase(Th, 200, 3.11, 1)]
        [TestCase(L640, 200, 2.24, 1)]
        [TestCase(W1280, 200, 2.77, 1)]
        [TestCase(Tv, 40, 7.62, 1)]
        [TestCase(Tv, 100, 2.45, 1)]
        public void WidthsOfTheDesignNotes(double useful, int count, double width, double gap)
        {
            var layout = PillRowGeometry.Layout(useful, count);
            Assert.That(layout.PillWidth, Is.EqualTo(width).Within(0.06), "la tabla redondea a un decimal");
            Assert.That(layout.Gap, Is.EqualTo(gap));
            Assert.That(layout.Strip, Is.False);
        }

        [Test, Category("AC-WKT-14-08")]
        public void EqualWidthsWithinOnePixel()
        {
            // Q-59: un lugar por paso, todos del mismo ancho ± 1 px, en los cuatro perfiles con 6 y 14 pasos (y la tira).
            var profiles = new[] { (411.0, 914.0, 2.625), (914.0, 411.0, 2.625), (640.0, 360.0, 1.0), (1280.0, 720.0, 1.0) };
            foreach (var p in profiles)
            {
                var s = HudGeometry.Scale(p.Item1, p.Item2);
                var useful = PillRowGeometry.UsefulWidthBase(p.Item1, s);
                foreach (var count in new[] { 6, 14, 50, 200 })
                {
                    var layout = PillRowGeometry.Layout(useful, count);
                    var left = new int[count];
                    var right = new int[count];
                    PillRowGeometry.PixelEdges(layout, 33.4, s * p.Item3, left, right);
                    int min = int.MaxValue, max = int.MinValue;
                    for (var i = 0; i < count; i++)
                    {
                        var w = right[i] - left[i];
                        min = Math.Min(min, w);
                        max = Math.Max(max, w);
                        if (i > 0)
                        {
                            Assert.That(left[i], Is.GreaterThanOrEqualTo(right[i - 1]), "en orden, sin encimarse");
                        }
                    }

                    Assert.That(max - min, Is.LessThanOrEqualTo(1), p + " con " + count + " pasos");
                }
            }
        }

        [Test, Category("AC-WKT-14-16")]
        public void GapComesDownAndThresholds()
        {
            // Los umbrales de §2.2 en TV: separación 3 hasta 38 pasos, 1 hasta 115, la tira desde 116; las angostas desde 50.
            Assert.That(PillRowGeometry.Layout(Tv, 38).Gap, Is.EqualTo(3));
            Assert.That(PillRowGeometry.Layout(Tv, 39).Gap, Is.EqualTo(1));
            Assert.That(PillRowGeometry.Layout(Tv, 115).Gap, Is.EqualTo(1));
            Assert.That(PillRowGeometry.Layout(Tv, 115).Strip, Is.False);
            Assert.That(PillRowGeometry.Layout(Tv, 116).Strip, Is.True);
            Assert.That(PillRowGeometry.Layout(Tv, 116).Gap, Is.EqualTo(0));
            Assert.That(PillRowGeometry.Layout(Tv, 49).Narrow, Is.False);
            Assert.That(PillRowGeometry.Layout(Tv, 50).Narrow, Is.True);
            Assert.That(PillRowGeometry.Layout(Th, 91).Gap, Is.EqualTo(3));
            Assert.That(PillRowGeometry.Layout(Th, 92).Gap, Is.EqualTo(1));
            Assert.That(PillRowGeometry.Layout(Th, 274).Strip, Is.False);
            Assert.That(PillRowGeometry.Layout(Th, 275).Strip, Is.True);
            Assert.That(PillRowGeometry.Layout(L640, 72).Gap, Is.EqualTo(3));
            Assert.That(PillRowGeometry.Layout(L640, 217).Strip, Is.True);
            Assert.That(PillRowGeometry.Layout(W1280, 84).Gap, Is.EqualTo(3));
            Assert.That(PillRowGeometry.Layout(W1280, 252).Strip, Is.True);
        }

        [TestCase(Tv, 50)]
        [TestCase(Th, 118)]
        [TestCase(L640, 93)]
        [TestCase(W1280, 108)]
        public void ThumbFromTheNarrowThreshold(double useful, int first)
        {
            // DN4b-18: el pulgar en toda píldora de menos de 6 base, en la separación 1 y en la tira.
            Assert.That(PillRowGeometry.Layout(useful, first - 1).Narrow, Is.False);
            Assert.That(PillRowGeometry.Layout(useful, first).Narrow, Is.True);
        }

        [Test, Category("AC-WKT-14-16")]
        public void ThumbFromFiftyStepsInTv()
        {
            // ≥ 4 × 10, centrado en su lugar (1 arriba y 1 abajo de la franja) y acotado a la fila.
            var layout = PillRowGeometry.Layout(Tv, 100);
            var thumb = layout.ThumbOf(40);
            Assert.That(thumb.Width, Is.EqualTo(4));
            Assert.That(thumb.Height, Is.EqualTo(10));
            Assert.That(thumb.Top, Is.EqualTo(7));
            Assert.That(thumb.Bottom, Is.EqualTo(17));
            Assert.That(thumb.CenterX, Is.EqualTo(layout.LeftOf(40) + layout.PillWidth / 2).Within(1e-9));
            Assert.That(layout.ThumbRadiusOf(40), Is.EqualTo(2), "min(4; ancho / 2)");
            var first = layout.ThumbOf(0);
            Assert.That(first.Left, Is.EqualTo(PillRowGeometry.PadLeft), "acotado a la fila");
            var last = layout.ThumbOf(99);
            Assert.That(last.Right, Is.EqualTo(PillRowGeometry.PadLeft + Tv).Within(1e-9));
            var wide = PillRowGeometry.Layout(Tv, 6);
            Assert.That(wide.ThumbOf(2), Is.EqualTo(wide.PillOf(2)), "sin angostas, la píldora misma");
            Assert.That(PillRowGeometry.Layout(Tv, 50).ThumbOf(10).Width, Is.EqualTo(PillRowGeometry.Layout(Tv, 50).PillWidth),
                "max(4; ancho) con 5,9 de ancho");
        }

        [Test, Category("AC-WKT-14-16")]
        public void TwoHundredStepsInTvAreAStrip()
        {
            // Serie larga: tira de 1,72 base; el pulgar de 4·s × 10·s = 11,1 px en TV contra 4,8 px de un tramo.
            var layout = PillRowGeometry.Layout(Tv, 200);
            Assert.That(layout.Strip, Is.True);
            Assert.That(layout.PillWidth, Is.EqualTo(1.72).Within(0.005));
            var s = HudGeometry.Scale(411, 914);
            Assert.That(layout.ThumbOf(100).Width * s * 2.625, Is.EqualTo(11.1).Within(0.05));
            Assert.That(layout.PillWidth * s * 2.625, Is.EqualTo(4.76).Within(0.05));
            Assert.That(layout.PillOf(199).Right, Is.EqualTo(10 + Tv).Within(1e-9), "nunca sale de la barra");
        }

        [Test, Category("AC-WKT-14-16")]
        public void MaxEditorSteps()
        {
            // El tope del editor (×999, 1998 pasos): tramos de 0,172 base (0,48 px en TV); la franja sigue mostrando todo.
            var layout = PillRowGeometry.Layout(Tv, 1998);
            Assert.That(layout.Strip, Is.True);
            Assert.That(layout.PillWidth, Is.EqualTo(0.172).Within(0.0005));
            var s = HudGeometry.Scale(411, 914);
            Assert.That(layout.PillWidth * s * 2.625, Is.EqualTo(0.48).Within(0.01));
        }

        [Test, Category("AC-WKT-15-05")]
        public void NarrowHollowHasRails()
        {
            // DN4b-18: la hueca angosta lleva dos rieles de 1,5 al 0,85 (arriba y abajo, sin los costados), dibujados de izquierda
            // a derecha a la vez durante el cambio de paso.
            var pill = PillRowGeometry.Layout(Tv, 100).PillOf(7);
            PillShapes.Rails(pill, 1.5, 1, out var top, out var bottom);
            Assert.That(top.Height, Is.EqualTo(1.5));
            Assert.That(bottom.Height, Is.EqualTo(1.5));
            Assert.That(top.Top, Is.EqualTo(pill.Top));
            Assert.That(bottom.Bottom, Is.EqualTo(pill.Bottom).Within(1e-9));
            Assert.That(top.Width, Is.EqualTo(pill.Width).Within(1e-9));
            PillShapes.Rails(pill, 1.5, 0.5, out var halfTop, out var halfBottom);
            Assert.That(halfTop.Width, Is.EqualTo(pill.Width / 2).Within(1e-9));
            Assert.That(halfBottom.Width, Is.EqualTo(halfTop.Width));
            Assert.That(halfTop.Left, Is.EqualTo(pill.Left), "de izquierda a derecha");
        }

        [Test, Category("AC-WKT-14-16")]
        public void StripRunsAtMostOneColumnPerPixelWith1998()
        {
            // DT4b-21: con 1998 pasos en TV (≈ 951 px), a lo sumo un tramo por columna de píxel, y el color de cada columna es el
            // promedio de los tramos que la cubren, pesado por lo que cubre cada uno.
            const int count = 1998;
            var s = HudGeometry.Scale(411, 914);
            var pxPerBase = s * 2.625;
            var layout = PillRowGeometry.Layout(Tv, count);
            var left = new double[count];
            var right = new double[count];
            var r = new double[count];
            var g = new double[count];
            var b = new double[count];
            var a = new double[count];
            for (var i = 0; i < count; i++)
            {
                left[i] = 20 + layout.LeftOf(i) * pxPerBase;
                right[i] = left[i] + layout.PillWidth * pxPerBase;
                var work = i % 2 == 0;
                r[i] = work ? 239 : 6;
                g[i] = work ? 71 : 214;
                b[i] = work ? 111 : 160;
                a[i] = i < 1000 ? 0.85 : i == 1000 ? 0.30 : 0.18;
            }

            var runs = new StripRun[2000];
            var n = PillShapes.StripRuns(count, left, right, r, g, b, a, runs);
            var columns = (int)Math.Ceiling(right[count - 1]) - (int)Math.Floor(left[0]);
            Assert.That(n, Is.LessThanOrEqualTo(columns), "a lo sumo uno por columna");
            Assert.That(columns, Is.InRange(945, 960));
            for (var i = 1; i < n; i++)
            {
                Assert.That(runs[i].Left, Is.EqualTo(runs[i - 1].Right), "contiguos, en orden");
            }

            // Una columna llena de tramos de 0,48 px: el promedio de trabajo y descanso, pesado por lo que cubre cada uno.
            var mid = runs[n / 4];
            Assert.That(mid.A, Is.EqualTo(0.85).Within(0.01), "lo hecho, al 85 %");
            Assert.That(mid.R, Is.InRange(6.0, 239.0));
            var tail = runs[n - 2];
            Assert.That(tail.A, Is.EqualTo(0.18).Within(0.01), "lo que falta, al 18 %");
        }

        [Test]
        public void StripRunsJoinEqualPaints()
        {
            // Los pasos contiguos con la misma pintura se juntan en un tramo.
            var left = new[] { 0.0, 10.0, 20.0 };
            var right = new[] { 10.0, 20.0, 30.0 };
            var r = new[] { 1.0, 1.0, 9.0 };
            var g = new[] { 2.0, 2.0, 9.0 };
            var b = new[] { 3.0, 3.0, 9.0 };
            var a = new[] { 0.5, 0.5, 1.0 };
            var runs = new StripRun[30];
            Assert.That(PillShapes.StripRuns(3, left, right, r, g, b, a, runs), Is.EqualTo(2));
            Assert.That(runs[0].Left, Is.EqualTo(0));
            Assert.That(runs[0].Right, Is.EqualTo(20));
            Assert.That(runs[1].Right, Is.EqualTo(30));
        }

        [Test, Category("AC-WKT-14-11")]
        public void HaloStartsAtTheEdgeAndFadesWithinTheRow()
        {
            // DN4b-17: el halo empieza en el borde de la píldora (o del pulgar), solo por fuera, a 0,65·α y se apaga como
            // (1 − d/E)² hasta 10 a los costados, 8 arriba y 4 abajo: llega a 0 en el borde de la fila.
            var expected = new[] { (0.0, 0.650), (1.0, 0.527), (2.0, 0.416), (4.0, 0.234), (6.0, 0.104), (8.0, 0.026), (10.0, 0.0) };
            foreach (var e in expected)
            {
                Assert.That(PillShapes.GlowProfile(e.Item1, 10), Is.EqualTo(e.Item2).Within(0.0006), "d = " + e.Item1);
            }

            var pill = PillRowGeometry.Layout(Tv, 6).PillOf(3);
            var nine = PillShapes.GlowNineSlice(pill, 4, MotionSpecs.PillGlowExtentSideDp, MotionSpecs.PillGlowExtentTopDp, MotionSpecs.PillGlowExtentBottomDp);
            Assert.That(nine.Y0, Is.EqualTo(0), "arriba llega al borde de la fila (8 de relleno)");
            Assert.That(nine.Y3, Is.EqualTo(PillRowGeometry.RowHeight), "abajo, también (4 de relleno): no pasa debajo de la fila de información");
            Assert.That(nine.X0, Is.EqualTo(pill.Left - 10));
            Assert.That(nine.X3, Is.EqualTo(pill.Right + 10));
            Assert.That(nine.X1, Is.EqualTo(pill.Left + 4));
            Assert.That(nine.Y1, Is.EqualTo(nine.Y2), "la píldora de 8 con radio 4: el centro no tiene alto");
        }

        [Test]
        public void CapsuleOutlineFillAndHighlight()
        {
            // El relleno de la en curso recortado por la cápsula (el ClipRRect del legacy, step_pill.dart:57-76): el borde derecho
            // recto; con menos del radio, solo el pedazo del extremo izquierdo.
            var pill = new DpRect(10, 8, 54.8, 8);
            var output = new PolygonBuffer(64);
            var scratch = new PolygonBuffer(64);
            PillShapes.FillPolygon(pill, 4, 27.4, output, scratch);
            double maxX = double.MinValue, minX = double.MaxValue;
            var onCut = 0;
            for (var i = 0; i < output.Count; i++)
            {
                maxX = Math.Max(maxX, output.X[i]);
                minX = Math.Min(minX, output.X[i]);
                onCut += Math.Abs(output.X[i] - 37.4) < 1e-9 ? 1 : 0;
                Assert.That(output.Y[i], Is.InRange(8 - 1e-9, 16 + 1e-9));
            }

            Assert.That(maxX, Is.EqualTo(37.4).Within(1e-9), "la mitad");
            Assert.That(minX, Is.EqualTo(10).Within(1e-9));
            Assert.That(onCut, Is.EqualTo(2), "el borde derecho, recto");
            PillShapes.FillPolygon(pill, 4, 0, output, scratch);
            Assert.That(output.Count, Is.EqualTo(0), "sin relleno");
            Assert.That(PillShapes.HighlightSpan(pill, 4, 27.4, 1, out var from, out var to), Is.True);
            Assert.That(to, Is.EqualTo(37.4).Within(1e-9));
            Assert.That(from, Is.GreaterThan(10), "recortado por la cápsula");
        }

        // QA-V4b-04 (H-D2-03): los contornos que llenan Painter2D no repiten vértices. La cápsula de 8 con radio 4 tiene las dos
        // puntas en un solo punto; el contorno cerraba en su primer punto (una arista de largo 0) y Painter2D dejaba una grieta en
        // la línea del centro del relleno (un píxel de la pista en CAP-27). Además, cada arco empieza en su punto: los bordes de
        // arriba y de abajo quedan rectos (antes iban al segundo punto del arco siguiente, inclinados 0,08).
        [TestCase(66.4, 8.0, 4.0, TestName = "OutlinesHaveNoRepeatedVertices_CAP27_66_4x8")]
        [TestCase(54.8, 8.0, 4.0, TestName = "OutlinesHaveNoRepeatedVertices_54_8x8")]
        [TestCase(4.0, 10.0, 2.0, TestName = "OutlinesHaveNoRepeatedVertices_Thumb4x10")]
        [TestCase(8.0, 8.0, 4.0, TestName = "OutlinesHaveNoRepeatedVertices_Circle8")]
        [Category("AC-WKT-15-03")]
        public void OutlinesHaveNoRepeatedVertices(double width, double height, double radius)
        {
            var pill = new DpRect(10, 8, width, height);
            var output = new PolygonBuffer(64);
            var scratch = new PolygonBuffer(64);
            PillShapes.CapsuleOutline(pill, radius, output);
            AssertDistinct(output, "cápsula " + width + " × " + height);
            var r = Math.Min(radius, Math.Min(width, height) / 2);
            if (width > 2 * r + 1e-9)
            {
                Assert.That(HasPoint(output, pill.Left + r, pill.Top), Is.True, "el borde de arriba empieza en el radio");
                Assert.That(HasPoint(output, pill.Right - r, pill.Top), Is.True, "y termina en el radio: recto");
                Assert.That(HasPoint(output, pill.Right - r, pill.Bottom), Is.True, "el de abajo, también");
                Assert.That(HasPoint(output, pill.Left + r, pill.Bottom), Is.True);
            }

            foreach (var fraction in new[] { 0.02, 0.3, 0.5, 0.85, 0.999, 1.0 })
            {
                PillShapes.FillPolygon(pill, radius, fraction * width, output, scratch);
                AssertDistinct(output, "relleno al " + fraction);
                Assert.That(output.Count, Is.GreaterThanOrEqualTo(3), "relleno al " + fraction);
            }

            // Un corte justo en un vértice del arco no agrega un punto repetido.
            PillShapes.CapsuleOutline(pill, radius, scratch);
            var vertexX = scratch.X[2];
            PillShapes.FillPolygon(pill, radius, vertexX - pill.Left, output, scratch);
            AssertDistinct(output, "corte en un vértice");
        }

        private static void AssertDistinct(PolygonBuffer polygon, string label)
        {
            for (var i = 0; i < polygon.Count; i++)
            {
                var j = (i + 1) % polygon.Count;
                var d = Math.Sqrt(Math.Pow(polygon.X[j] - polygon.X[i], 2) + Math.Pow(polygon.Y[j] - polygon.Y[i], 2));
                Assert.That(d, Is.GreaterThan(1e-6), label + ": los vértices " + i + " y " + j + " son el mismo punto");
            }
        }

        private static bool HasPoint(PolygonBuffer polygon, double x, double y)
        {
            for (var i = 0; i < polygon.Count; i++)
            {
                if (Math.Abs(polygon.X[i] - x) < 1e-9 && Math.Abs(polygon.Y[i] - y) < 1e-9)
                {
                    return true;
                }
            }

            return false;
        }

        [Test, Category("AC-WKT-14-12")]
        public void PerimeterOfTheHollow()
        {
            // 04-design-notes §4.5: en una píldora de 54,8 base, 118,7 de contorno; el trazo arranca en el centro del extremo
            // izquierdo y va en el sentido del reloj (primero sube).
            var pill = new DpRect(10, 8, 54.8, 8);
            Assert.That(PillShapes.PerimeterLength(pill, 4), Is.EqualTo(118.7).Within(0.05));
            var output = new PolygonBuffer(80);
            var scratch = new PolygonBuffer(80);
            PillShapes.PerimeterPolyline(pill, 4, 0.1, output, scratch);
            Assert.That(output.X[0], Is.EqualTo(10).Within(1e-9));
            Assert.That(output.Y[0], Is.EqualTo(12).Within(1e-9), "el centro del extremo izquierdo");
            Assert.That(output.Y[1], Is.LessThan(12), "en el sentido del reloj: sube");
            var length = 0.0;
            for (var i = 1; i < output.Count; i++)
            {
                length += Math.Sqrt(Math.Pow(output.X[i] - output.X[i - 1], 2) + Math.Pow(output.Y[i] - output.Y[i - 1], 2));
            }

            Assert.That(length, Is.EqualTo(0.1 * 118.7).Within(0.15));
            PillShapes.PerimeterPolyline(pill, 4, 0, output, scratch);
            Assert.That(output.Count, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-14-18")]
        public void RingArcAdvancesClockwise()
        {
            // DN4b-20: el arco de 360°·(1 − f) a las 12; con f = 0,80 el borde que se mueve está en los 72° (CAP-02).
            var arc = PillShapes.RingArc(0.80);
            Assert.That(arc.StartDegrees, Is.EqualTo(72).Within(1e-9));
            Assert.That(arc.EndDegrees, Is.EqualTo(360));
            Assert.That(PillShapes.RingArc(1).StartDegrees, Is.EqualTo(0), "lleno en el tick del 3");
            Assert.That(PillShapes.RingArc(0).IsEmpty, Is.True, "vacío en el cambio");
            Assert.That(PillShapes.RingArc(2.0 / 3).StartDegrees, Is.LessThan(PillShapes.RingArc(1.0 / 3).StartDegrees),
                "el borde que se mueve avanza en el sentido del reloj");
            PillShapes.RingPoint(0, 0, 34.5, 90, out var x, out var y);
            Assert.That(x, Is.EqualTo(34.5).Within(1e-9), "a los 90°, las 3");
            Assert.That(y, Is.EqualTo(0).Within(1e-9));
            Assert.That(HudGeometry.BigCountRingRadius, Is.EqualTo(34.5));
            Assert.That(MotionSpecs.BigCountRingClockwise, Is.True);
        }
    }
}
