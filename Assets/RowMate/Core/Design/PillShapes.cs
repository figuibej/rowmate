using System;

namespace RowMate.Core.Design
{
    // Un polígono (o polilínea) en un búfer propio: la vista lo reserva una vez y PillShapes escribe adentro, sin asignar por
    // cuadro (02-tech-design §3.13 de la It. 4b).
    public sealed class PolygonBuffer
    {
        public PolygonBuffer(int capacity)
        {
            if (capacity < 4)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            X = new double[capacity];
            Y = new double[capacity];
        }

        public double[] X { get; }

        public double[] Y { get; }

        public int Count { get; private set; }

        public int Capacity => X.Length;

        public void Clear()
        {
            Count = 0;
        }

        public void Add(double x, double y)
        {
            if (Count >= X.Length)
            {
                throw new InvalidOperationException("PolygonBuffer: sin lugar (" + X.Length + ").");
            }

            X[Count] = x;
            Y[Count] = y;
            Count++;
        }

        // Saca el último punto (QA-V4b-04: el que repite al primero).
        public void RemoveLast()
        {
            if (Count > 0)
            {
                Count--;
            }
        }
    }

    // Los bordes del halo en 9 partes (DN4b-17): x0 < x1 ≤ x2 < x3 y y0 < y1 ≤ y2 < y3, en las mismas unidades que la píldora.
    // El centro (x1-x2 × y1-y2) es la píldora menos su radio: no se dibuja (el halo va solo por fuera).
    public readonly struct NineSliceEdges
    {
        public NineSliceEdges(double x0, double x1, double x2, double x3, double y0, double y1, double y2, double y3)
        {
            X0 = x0;
            X1 = x1;
            X2 = x2;
            X3 = x3;
            Y0 = y0;
            Y1 = y1;
            Y2 = y2;
            Y3 = y3;
        }

        public double X0 { get; }

        public double X1 { get; }

        public double X2 { get; }

        public double X3 { get; }

        public double Y0 { get; }

        public double Y1 { get; }

        public double Y2 { get; }

        public double Y3 { get; }
    }

    // El arco del aro de la cuenta grande (DN4b-20), en grados medidos EN EL SENTIDO DEL RELOJ desde las 12.
    public readonly struct RingArcAngles
    {
        public RingArcAngles(double startDegrees, double endDegrees)
        {
            StartDegrees = startDegrees;
            EndDegrees = endDegrees;
        }

        // El borde que se mueve: 360°·(1 − f).
        public double StartDegrees { get; }

        // El borde fijo: las 12 (360°).
        public double EndDegrees { get; }

        public double SweepDegrees => EndDegrees - StartDegrees;

        public bool IsEmpty => SweepDegrees <= 0;
    }

    // Un tramo de la tira en píxeles enteros (DT4b-21): [Left; Right) con su color (sin premultiplicar) y su alfa.
    public struct StripRun
    {
        public int Left;
        public int Right;
        public double R;
        public double G;
        public double B;
        public double A;
    }

    // La geometría que dibuja la fila de píldoras y la cuenta grande (02-tech-design §3.4, §3.5 de la It. 4b; 04-design-notes
    // §3.2, §3.6; DN4b-17 a -20). Pura y sin asignaciones (escribe en los búferes que le pasan): la vista (PillRowView,
    // CenterCallouts, WP-B1) la lleva a Painter2D y a MeshGenerationContext. Las coordenadas son las de la vista.
    public static class PillShapes
    {
        // Segmentos por cuarto de círculo de los arcos de las cápsulas (a 3× en W1280 se ve liso).
        public const int ArcSegments = 8;

        // El BoxShadow del legacy (step_pill.dart:88-94): desenfoque 8 → sigma 8·0,57735 + 0,5 (Flutter
        // convertRadiusToSigma), expansión 2.
        public const double LegacyShadowBlur = 8;
        public const double LegacyShadowSpread = 2;

        public static double LegacyShadowSigma => LegacyShadowBlur * 0.57735 + 0.5;

        // Lo que deja el BoxShadow del legacy a una distancia d (dp base) del borde de la píldora, como fracción de α
        // (04-design-notes §3.2): Φ((expansión − d) / sigma). En el borde, Φ(2 / 5,12) = 0,652.
        public static double LegacyShadowFraction(double distance)
        {
            return NormalCdf((LegacyShadowSpread - distance) / LegacyShadowSigma);
        }

        // El halo de la app a una distancia d del borde, como fracción de α (DN4b-17): 0,65·(1 − d/E)², 0 desde E.
        public static double GlowProfile(double distance, double extent)
        {
            if (extent <= 0 || distance >= extent)
            {
                return 0;
            }

            var u = distance <= 0 ? 1 : 1 - distance / extent;
            return MotionSpecs.PillGlowEdgeFactor * u * u;
        }

        // Los bordes del 9-slice del halo alrededor de la píldora (o del pulgar): E a cada lado (10·s), arriba (8·s) y abajo
        // (4·s), con el radio de la píldora en las esquinas (DN4b-17). Cada borde se estira a su largo.
        public static NineSliceEdges GlowNineSlice(in DpRect pill, double radius, double extentSide, double extentTop, double extentBottom)
        {
            var r = Math.Min(radius, Math.Min(pill.Width, pill.Height) / 2);
            return new NineSliceEdges(pill.Left - extentSide, pill.Left + r, pill.Right - r, pill.Right + extentSide,
                pill.Top - extentTop, pill.Top + r, pill.Bottom - r, pill.Bottom + extentBottom);
        }

        // El contorno de una cápsula (rectángulo redondeado), en el sentido del reloj desde el centro de su extremo izquierdo.
        public static void CapsuleOutline(in DpRect rect, double radius, PolygonBuffer output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            output.Clear();
            var r = ClampRadius(rect, radius);
            var left = rect.Left;
            var top = rect.Top;
            var right = rect.Right;
            var bottom = rect.Bottom;
            // Arriba a la izquierda (de 180° a 270° con y hacia abajo), arriba a la derecha, abajo a la derecha, abajo a la izquierda.
            // Cada arco empieza en su punto (los bordes rectos quedan rectos) y nunca se repite un vértice: donde dos arcos se tocan
            // (las puntas de la cápsula de 8 con radio 4) va uno solo, y el contorno no cierra en su primer punto. Una arista de
            // largo 0 le dejaba a Painter2D una grieta en la línea del centro del relleno (QA-V4b-04, CAP-27).
            AddArc(output, left + r, top + r, r, Math.PI, 1.5 * Math.PI);
            AddArc(output, right - r, top + r, r, 1.5 * Math.PI, 2 * Math.PI);
            AddArc(output, right - r, bottom - r, r, 0, 0.5 * Math.PI);
            AddArc(output, left + r, bottom - r, r, 0.5 * Math.PI, Math.PI);
            DropClosingRepeats(output);
        }

        // El relleno de la en curso recortado por la cápsula (step_pill.dart:57-76, el ClipRRect del legacy): el rectángulo de
        // ancho fillWidth desde el borde izquierdo, intersecado con la cápsula. El borde derecho del relleno queda recto.
        // scratch: un búfer de trabajo del mismo tamaño. Sin relleno (fillWidth ≤ 0), output queda vacío.
        public static void FillPolygon(in DpRect pill, double radius, double fillWidth, PolygonBuffer output, PolygonBuffer scratch)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (scratch == null)
            {
                throw new ArgumentNullException(nameof(scratch));
            }

            output.Clear();
            if (fillWidth <= 0 || pill.Width <= 0)
            {
                return;
            }

            CapsuleOutline(pill, radius, scratch);
            var cut = pill.Left + Math.Min(fillWidth, pill.Width);
            ClipRight(scratch, cut, output);
            DropClosingRepeats(output);
        }

        // El brillo del relleno (DN4b-19): la línea de 1·s en el borde de arriba del relleno, recortada por la cápsula. Devuelve
        // el tramo [xFrom; xTo] de la línea a la altura y = top + grosor / 2 (vacío si el relleno no llega).
        public static bool HighlightSpan(in DpRect pill, double radius, double fillWidth, double thickness, out double xFrom, out double xTo)
        {
            xFrom = 0;
            xTo = 0;
            if (fillWidth <= 0 || pill.Width <= 0)
            {
                return false;
            }

            var r = ClampRadius(pill, radius);
            var y = thickness / 2;
            // A esa altura, la cápsula deja libre r − √(r² − (r − y)²) a cada lado.
            var dy = r - y;
            var inset = dy > 0 ? r - Math.Sqrt(Math.Max(0, r * r - dy * dy)) : 0;
            xFrom = pill.Left + inset;
            xTo = Math.Min(pill.Left + fillWidth, pill.Right - inset);
            return xTo > xFrom;
        }

        // El largo del contorno de una cápsula: 2·(w − 2r) + 2·(h − 2r) + 2πr. En una píldora de 54,8 × 8 con radio 4, 118,7.
        public static double PerimeterLength(in DpRect rect, double radius)
        {
            var r = ClampRadius(rect, radius);
            return 2 * (rect.Width - 2 * r) + 2 * (rect.Height - 2 * r) + 2 * Math.PI * r;
        }

        // El trazo del borde de la hueca hasta la fracción dada del contorno (0 → nada, 1 → entero), en el sentido del reloj
        // desde el centro de su extremo izquierdo (04-design-notes §4.5). La vista le pasa el rectángulo ya metido medio trazo.
        public static void PerimeterPolyline(in DpRect rect, double radius, double fraction, PolygonBuffer output, PolygonBuffer scratch)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (scratch == null)
            {
                throw new ArgumentNullException(nameof(scratch));
            }

            output.Clear();
            if (fraction <= 0)
            {
                return;
            }

            CapsuleOutline(rect, radius, scratch);
            var total = 0.0;
            for (var i = 0; i < scratch.Count; i++)
            {
                var j = (i + 1) % scratch.Count;
                total += Distance(scratch.X[i], scratch.Y[i], scratch.X[j], scratch.Y[j]);
            }

            var target = (fraction >= 1 ? 1 : fraction) * total;
            output.Add(scratch.X[0], scratch.Y[0]);
            var walked = 0.0;
            for (var i = 0; i < scratch.Count; i++)
            {
                var j = (i + 1) % scratch.Count;
                var length = Distance(scratch.X[i], scratch.Y[i], scratch.X[j], scratch.Y[j]);
                if (walked + length >= target)
                {
                    var u = length <= 0 ? 0 : (target - walked) / length;
                    output.Add(scratch.X[i] + (scratch.X[j] - scratch.X[i]) * u, scratch.Y[i] + (scratch.Y[j] - scratch.Y[i]) * u);
                    return;
                }

                walked += length;
                output.Add(scratch.X[j], scratch.Y[j]);
            }
        }

        // Los dos rieles de la hueca angosta (DN4b-18; AC-WKT-15-05): arriba y abajo, de `stroke` de grosor, sin los costados,
        // dibujados de izquierda a derecha hasta la fracción dada (los dos a la vez).
        public static void Rails(in DpRect pill, double stroke, double fraction, out DpRect top, out DpRect bottom)
        {
            var f = fraction <= 0 ? 0 : fraction >= 1 ? 1 : fraction;
            var width = pill.Width * f;
            var thickness = Math.Min(stroke, pill.Height / 2);
            top = new DpRect(pill.Left, pill.Top, width, thickness);
            bottom = new DpRect(pill.Left, pill.Bottom - thickness, width, thickness);
        }

        // El arco del aro de la cuenta grande (DN4b-20): la fracción que falta, f, va del ángulo 360°·(1 − f) hasta las 12,
        // medido en el sentido del reloj desde las 12. El borde que se mueve avanza en el sentido del reloj, como una aguja que se
        // come el arco; el otro queda en las 12. Lleno con f = 1, vacío con f = 0.
        public static RingArcAngles RingArc(double fraction)
        {
            var f = double.IsNaN(fraction) || fraction <= 0 ? 0 : fraction >= 1 ? 1 : fraction;
            return new RingArcAngles(360 * (1 - f), 360);
        }

        // Un punto del aro de radio r con centro (cx; cy) en el ángulo dado (en el sentido del reloj desde las 12, y hacia abajo).
        public static void RingPoint(double cx, double cy, double radius, double degrees, out double x, out double y)
        {
            var radians = degrees * Math.PI / 180;
            x = cx + radius * Math.Sin(radians);
            y = cy - radius * Math.Cos(radians);
        }

        // La tira por columnas de píxel (DT4b-21; QA-G4bT-11 (c)): los pasos contiguos con la misma pintura se juntan y, donde
        // una columna de píxel la cubren varios tramos, se dibuja con el color promedio de los que la cubren, pesado por lo que
        // cubre cada uno (lo que haría el suavizado). Así la tira cuesta a lo sumo un tramo por columna aunque tenga 1998 pasos.
        // left / right: los bordes de cada paso en píxeles, con decimales (originPx + LeftOf(i)·pxPerBase); r, g, b, a: el color
        // de cada paso, sin premultiplicar. runs: la salida (un tramo por columna como máximo). Devuelve cuántos tramos escribió.
        public static int StripRuns(int count, double[] left, double[] right, double[] r, double[] g, double[] b, double[] a, StripRun[] runs)
        {
            if (left == null || right == null || r == null || g == null || b == null || a == null || runs == null)
            {
                throw new ArgumentNullException(nameof(runs), "PillShapes.StripRuns: falta un arreglo.");
            }

            if (count <= 0)
            {
                return 0;
            }

            var first = (int)Math.Floor(left[0]);
            var last = (int)Math.Ceiling(right[count - 1]);
            var written = 0;
            var step = 0;
            for (var column = first; column < last; column++)
            {
                double pr = 0, pg = 0, pb = 0, pa = 0, covered = 0;
                while (step < count && right[step] <= column)
                {
                    step++;
                }

                for (var i = step; i < count && left[i] < column + 1; i++)
                {
                    var from = Math.Max(column, left[i]);
                    var to = Math.Min(column + 1, right[i]);
                    var cover = to - from;
                    if (cover <= 0)
                    {
                        continue;
                    }

                    var weight = cover * a[i];
                    pr += weight * r[i];
                    pg += weight * g[i];
                    pb += weight * b[i];
                    pa += weight;
                    covered += cover;
                }

                if (covered <= 0)
                {
                    continue;
                }

                var red = pa > 0 ? pr / pa : 0;
                var green = pa > 0 ? pg / pa : 0;
                var blue = pa > 0 ? pb / pa : 0;
                var alpha = pa;
                if (written > 0)
                {
                    var previous = runs[written - 1];
                    if (previous.Right == column && Same(previous.R, red) && Same(previous.G, green) && Same(previous.B, blue) && Same(previous.A, alpha))
                    {
                        runs[written - 1].Right = column + 1;
                        continue;
                    }
                }

                if (written >= runs.Length)
                {
                    throw new ArgumentException("PillShapes.StripRuns: el arreglo de tramos es más corto que las columnas.", nameof(runs));
                }

                runs[written] = new StripRun { Left = column, Right = column + 1, R = red, G = green, B = blue, A = alpha };
                written++;
            }

            return written;
        }

        // Φ(x) con erf de Abramowitz y Stegun 7.1.26 (error < 1,5e-7).
        public static double NormalCdf(double x)
        {
            return 0.5 * (1 + Erf(x / Math.Sqrt(2)));
        }

        private static double Erf(double x)
        {
            var sign = x < 0 ? -1 : 1;
            x = Math.Abs(x);
            var t = 1 / (1 + 0.3275911 * x);
            var y = 1 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
            return sign * y;
        }

        private static bool Same(double x, double y)
        {
            return Math.Abs(x - y) < 1e-9;
        }

        private static double ClampRadius(in DpRect rect, double radius)
        {
            var max = Math.Min(rect.Width, rect.Height) / 2;
            return radius < 0 ? 0 : radius > max ? max : radius;
        }

        private static double Distance(double x0, double y0, double x1, double y1)
        {
            var dx = x1 - x0;
            var dy = y1 - y0;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // Un arco con y hacia abajo: ángulo 0 = derecha, π/2 = abajo, π = izquierda, 3π/2 = arriba (el sentido del reloj en la
        // pantalla). includeStart: si agrega el primer punto.
        private static void AddArc(PolygonBuffer output, double cx, double cy, double r, double from, double to)
        {
            for (var i = 0; i <= ArcSegments; i++)
            {
                var angle = from + (to - from) * i / ArcSegments;
                AddDistinct(output, cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
            }
        }

        // Dos vértices a menos de esto son el mismo punto (dp base).
        private const double SamePointDp = 1e-6;

        private static void AddDistinct(PolygonBuffer output, double x, double y)
        {
            var n = output.Count;
            if (n > 0 && Math.Abs(output.X[n - 1] - x) < SamePointDp && Math.Abs(output.Y[n - 1] - y) < SamePointDp)
            {
                return;
            }

            output.Add(x, y);
        }

        // El contorno es cerrado: si termina en su primer punto, ese último sobra.
        private static void DropClosingRepeats(PolygonBuffer polygon)
        {
            while (polygon.Count > 1 && Math.Abs(polygon.X[polygon.Count - 1] - polygon.X[0]) < SamePointDp
                   && Math.Abs(polygon.Y[polygon.Count - 1] - polygon.Y[0]) < SamePointDp)
            {
                polygon.RemoveLast();
            }
        }

        // Sutherland-Hodgman con el semiplano x ≤ cut (la cápsula es convexa: el resultado también).
        private static void ClipRight(PolygonBuffer input, double cut, PolygonBuffer output)
        {
            var n = input.Count;
            for (var i = 0; i < n; i++)
            {
                var x0 = input.X[i];
                var y0 = input.Y[i];
                var j = (i + 1) % n;
                var x1 = input.X[j];
                var y1 = input.Y[j];
                var inside0 = x0 <= cut;
                var inside1 = x1 <= cut;
                if (inside0)
                {
                    AddDistinct(output, x0, y0);
                }

                if (inside0 != inside1)
                {
                    var u = (cut - x0) / (x1 - x0);
                    AddDistinct(output, cut, y0 + (y1 - y0) * u);
                }
            }
        }
    }
}
