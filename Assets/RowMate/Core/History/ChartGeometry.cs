using System;
using System.Collections.Generic;
using RowMate.Core.Design;

namespace RowMate.Core.History
{
    // El marco del gráfico en dp, con y desde ARRIBA (A9-01; arte §3): el área de datos (x de 12 + 36 al ancho − 12 − 4; y de
    // 12 + 6 al alto − 12 − 18), la tira de tipo (y 12-15) y el tamaño de 1 px físico en dp (el borde suavizado de los trazos).
    public readonly struct ChartFrame
    {
        private ChartFrame(double width, double height, DpRect area, double stripTop, double pixelDp)
        {
            Width = width;
            Height = height;
            Area = area;
            StripTop = stripTop;
            PixelDp = pixelDp;
        }

        public double Width { get; }

        public double Height { get; }

        public DpRect Area { get; }

        public double StripTop { get; }

        public double PixelDp { get; }

        public bool IsEmpty => !(Area.Width > 0) || !(Area.Height > 0);

        public static ChartFrame For(double widthDp, double heightDp, double pixelsPerDp)
        {
            if (!(pixelsPerDp > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(pixelsPerDp), pixelsPerDp, "ChartFrame: la densidad es positiva.");
            }

            var left = ChartSpecs.Padding + ChartSpecs.YAxisColumn;
            var top = ChartSpecs.Padding + ChartSpecs.TypeStripSpace;
            var right = Math.Max(left, widthDp - ChartSpecs.Padding - ChartSpecs.RightMargin);
            var bottom = Math.Max(top, heightDp - ChartSpecs.Padding - ChartSpecs.XLabelBand);
            return new ChartFrame(widthDp, heightDp, DpRect.FromEdges(left, top, right, bottom), ChartSpecs.Padding, 1.0 / pixelsPerDp);
        }
    }

    // Los vértices y los índices de la malla del gráfico (A9-01): arreglos de capacidad fija que se reusan en cada cuadro (Clear
    // no libera nada). Color por vértice en RGBA de 8 bits sin premultiplicar (0xRRGGBBAA); la vista lo pasa a Color32 con la
    // compensación del panel (MeshColor). Crece solo con EnsureCapacity, que se llama al preparar (nunca por cuadro).
    public sealed class ChartMeshBuffer
    {
        public ChartMeshBuffer(int vertexCapacity, int indexCapacity)
        {
            X = new float[Math.Max(0, vertexCapacity)];
            Y = new float[X.Length];
            Rgba = new uint[X.Length];
            Indices = new ushort[Math.Max(0, indexCapacity)];
        }

        // UI Toolkit indexa con ushort.
        public const int MaxVertices = 65535;

        public float[] X { get; private set; }

        public float[] Y { get; private set; }

        public uint[] Rgba { get; private set; }

        public ushort[] Indices { get; private set; }

        public int VertexCount { get; private set; }

        public int IndexCount { get; private set; }

        public int VertexCapacity => X.Length;

        public int IndexCapacity => Indices.Length;

        public void Clear()
        {
            VertexCount = 0;
            IndexCount = 0;
        }

        // Agranda los arreglos si hace falta (al preparar; descarta lo escrito).
        public void EnsureCapacity(int vertices, int indices)
        {
            if (vertices > MaxVertices)
            {
                throw new ArgumentOutOfRangeException(nameof(vertices), vertices, "ChartMeshBuffer: más de 65 535 vértices.");
            }

            if (vertices > X.Length)
            {
                X = new float[vertices];
                Y = new float[vertices];
                Rgba = new uint[vertices];
            }

            if (indices > Indices.Length)
            {
                Indices = new ushort[indices];
            }

            Clear();
        }

        public static uint Pack(Rgb rgb, double alpha)
        {
            var a = alpha <= 0 ? 0 : alpha >= 1 ? 255 : (int)(alpha * 255 + 0.5);
            return ((uint)rgb.R << 24) | ((uint)rgb.G << 16) | ((uint)rgb.B << 8) | (uint)a;
        }

        public static byte AlphaOf(uint rgba)
        {
            return (byte)(rgba & 0xFF);
        }

        internal int Vertex(double x, double y, uint rgba)
        {
            if (VertexCount >= X.Length)
            {
                throw new InvalidOperationException("ChartMeshBuffer: no entran más vértices (capacidad " + X.Length + ").");
            }

            X[VertexCount] = (float)x;
            Y[VertexCount] = (float)y;
            Rgba[VertexCount] = rgba;
            return VertexCount++;
        }

        internal void Triangle(int a, int b, int c)
        {
            if (IndexCount + 3 > Indices.Length)
            {
                throw new InvalidOperationException("ChartMeshBuffer: no entran más índices (capacidad " + Indices.Length + ").");
            }

            Indices[IndexCount++] = (ushort)a;
            Indices[IndexCount++] = (ushort)b;
            Indices[IndexCount++] = (ushort)c;
        }

        // a-b-c-d en orden alrededor del cuadrilátero.
        internal void Quad(int a, int b, int c, int d)
        {
            Triangle(a, b, c);
            Triangle(a, c, d);
        }
    }

    // La geometría del gráfico de UNA métrica en un marco (A9-01, A9-02; AC-HIS-13-06, -14-02, -14-03, -15-02; 02-tech-design
    // §2.9). Prepare (una vez por métrica y ancho) reduce la curva por columna, la pasa a dp en x, la suaviza y ubica las
    // franjas, los separadores y los objetivos; los Write* (en cada cuadro de una animación) solo escriben vértices en el buffer,
    // con el yMax animado (reescala las y sin rehacer las x) y el revelado por prefijo con el último tramo interpolado hasta el
    // corte. Sin asignar nada en los Write*. El orden de dibujo es el del arte §3 (WriteFrame).
    public sealed class ChartGeometry
    {
        private int[] _indices = new int[0];
        private float[] _px = new float[0];
        private float[] _pv = new float[0];
        private float[] _sx = new float[0];
        private float[] _sv = new float[0];
        private int _reduced;
        private int _series;
        private BandPx[] _bands = new BandPx[0];
        private int _bandCount;
        private TargetPx[] _targets = new TargetPx[0];
        private int _targetCount;
        private double[] _grid = new double[0];
        private double _xMin;
        private double _xMax;
        private Rgb _seriesRgb;

        public bool IsPrepared { get; private set; }

        public ChartMetric Metric { get; private set; }

        public ChartFrame Frame { get; private set; }

        public ChartMetricView? View { get; private set; }

        public Rgb SeriesColor => _seriesRgb;

        // El yMax de la métrica, quieta.
        public double YMax => View?.Y.Max ?? ChartSpecs.EmptyYMax;

        // Puntos de la curva después de reducir y de suavizar.
        public int ReducedCount => _reduced;

        public int SeriesCount => _series;

        public int BandCount => _bandCount;

        // Separadores: todas las franjas salvo la primera (AC-HIS-14-03).
        public int SeparatorCount => Math.Max(0, _bandCount - 1);

        public int TargetCount => _targetCount;

        public bool IsSinglePoint => _series == 1;

        // Lo que puede ocupar un cuadro de esta geometría sola (una curva) en el buffer.
        public int VertexBudget { get; private set; }

        public int IndexBudget { get; private set; }

        public void Prepare(SessionChartData data, ChartMetric metric, ChartFrame frame)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Metric = metric;
            Frame = frame;
            var view = data.View(metric);
            View = view;
            _seriesRgb = ChartMetrics.Palette(metric).Rgb;
            _xMin = view.X.Min;
            _xMax = view.X.Max;
            var area = frame.Area;

            // 1. Reducción por columna (solo si hay más puntos que columnas).
            var n = data.Count;
            var columns = Math.Max(1, Math.Min(ChartSpecs.MaxColumns, (int)Math.Floor(area.Width / ChartSpecs.ColumnDp)));
            _indices = Grow(_indices, ChartDecimation.Capacity(n, columns));
            var plotted = data.Plotted(metric);
            _reduced = ChartDecimation.MinMaxPerColumn(data.X, plotted, n, _xMin, _xMax, columns, _indices);
            _px = Grow(_px, _reduced);
            _pv = Grow(_pv, _reduced);
            for (var i = 0; i < _reduced; i++)
            {
                _px[i] = (float)PixelX(data.X[_indices[i]]);
                _pv[i] = plotted[_indices[i]];
            }

            // 2. Suavizado en (dp, valor), con la base como piso.
            var capacity = ChartSmoothing.Capacity(_reduced, area.Width);
            _sx = Grow(_sx, capacity);
            _sv = Grow(_sv, capacity);
            _series = ChartSmoothing.Tessellate(_px, _pv, _reduced, (float)ChartSpecs.SmoothMinSpacing, 0f, _sx, _sv);

            // 3. Franjas y separadores (no dependen de la métrica).
            var bands = data.Bands;
            if (_bands.Length < bands.Count)
            {
                _bands = new BandPx[bands.Count];
            }

            _bandCount = bands.Count;
            for (var i = 0; i < bands.Count; i++)
            {
                var color = StepBandPalette.For(bands[i].TypeName).Rgb;
                _bands[i] = new BandPx(Clamp(PixelX(bands[i].Start), area.Left, area.Right), Clamp(PixelX(bands[i].End), area.Left, area.Right), color);
            }

            // 4. Objetivos.
            var targets = view.Targets;
            if (_targets.Length < targets.Count)
            {
                _targets = new TargetPx[targets.Count];
            }

            _targetCount = targets.Count;
            for (var i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                _targets[i] = new TargetPx(Clamp(PixelX(t.Start), area.Left, area.Right), Clamp(PixelX(t.End), area.Left, area.Right), t.Y,
                    StepBandPalette.For(t.TypeName).Rgb);
            }

            _grid = view.Y.Grid;
            IsPrepared = true;
            ComputeBudget();
        }

        // El tiempo del monitor → x en dp. Con un solo punto (o todos en el mismo segundo), el centro del área (gate m-02).
        public double PixelX(double seconds)
        {
            var area = Frame.Area;
            if (!(_xMax > _xMin))
            {
                return area.CenterX;
            }

            return area.Left + (seconds - _xMin) / (_xMax - _xMin) * area.Width;
        }

        // x en dp → tiempo del monitor (para el cursor).
        public double SecondsAt(double px)
        {
            var area = Frame.Area;
            if (!(_xMax > _xMin) || !(area.Width > 0))
            {
                return _xMin;
            }

            return _xMin + (px - area.Left) / area.Width * (_xMax - _xMin);
        }

        // Un valor dibujado → y en dp (0 en la base, yMax arriba).
        public double PixelY(double value, double yMax)
        {
            var area = Frame.Area;
            var max = yMax > 0 ? yMax : ChartSpecs.EmptyYMax;
            return area.Bottom - value / max * area.Height;
        }

        // ── El cuadro entero, en el orden del arte §3 ──
        // previous: la métrica que se va en un cambio (o null). Las franjas, la tira y los separadores salen de current.
        public static void WriteFrame(ChartMeshBuffer buffer, ChartGeometry current, ChartGeometry? previous, in ChartVisual visual)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            buffer.Clear();
            var yMax = visual.YMax;
            var reveal = visual.Reveal;
            current.WriteBands(buffer, visual.StaticAlpha);
            current.WriteBaseline(buffer, visual.StaticAlpha);
            if (previous != null && visual.OldLabelsAlpha > 0)
            {
                previous.WriteGrid(buffer, yMax, visual.StaticAlpha * visual.OldLabelsAlpha);
            }

            current.WriteGrid(buffer, yMax, visual.StaticAlpha * visual.NewLabelsAlpha);
            if (previous != null && visual.OldTargetsAlpha > 0)
            {
                previous.WriteTargetAreas(buffer, yMax, reveal, visual.OldTargetsAlpha);
            }

            current.WriteTargetAreas(buffer, yMax, reveal, visual.NewTargetsAlpha);
            if (previous != null && visual.OldSeriesAlpha > 0)
            {
                previous.WriteSeriesArea(buffer, yMax, reveal, visual.OldSeriesAlpha * visual.AreaAlpha);
            }

            current.WriteSeriesArea(buffer, yMax, reveal, visual.NewSeriesAlpha * visual.AreaAlpha);
            current.WriteSeparators(buffer, visual.StaticAlpha);
            if (previous != null && visual.OldTargetsAlpha > 0)
            {
                previous.WriteTargetLines(buffer, yMax, reveal, visual.OldTargetsAlpha);
            }

            current.WriteTargetLines(buffer, yMax, reveal, visual.NewTargetsAlpha);
            if (previous != null && visual.OldSeriesAlpha > 0)
            {
                previous.WriteSeriesLine(buffer, yMax, reveal, visual.OldSeriesAlpha);
            }

            current.WriteSeriesLine(buffer, yMax, reveal, visual.NewSeriesAlpha);
            current.WriteStrip(buffer, visual.StaticAlpha);
        }

        // ── Lo que el diseño nombra (§2.9), armado con las capas ──
        // Franjas, base, separadores y tira (no dependen de la métrica).
        public void WriteStatic(ChartMeshBuffer buffer, double alpha)
        {
            WriteBands(buffer, alpha);
            WriteBaseline(buffer, alpha);
            WriteSeparators(buffer, alpha);
            WriteStrip(buffer, alpha);
        }

        // La curva: el área en degradé y la línea (o el disco de un solo punto).
        public void WriteSeries(ChartMeshBuffer buffer, double yMax, double reveal, double lineAlpha, double areaAlpha)
        {
            WriteSeriesArea(buffer, yMax, reveal, areaAlpha);
            WriteSeriesLine(buffer, yMax, reveal, lineAlpha);
        }

        // Los objetivos: sus áreas y sus líneas punteadas.
        public void WriteTargets(ChartMeshBuffer buffer, double yMax, double reveal, double alpha)
        {
            WriteTargetAreas(buffer, yMax, reveal, alpha);
            WriteTargetLines(buffer, yMax, reveal, alpha);
        }

        // ── Capas ──
        // Franjas: el color del tipo @10 % a todo el alto del área (AC-HIS-14-02).
        public void WriteBands(ChartMeshBuffer buffer, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var area = Frame.Area;
            for (var i = 0; i < _bandCount; i++)
            {
                var band = _bands[i];
                if (band.X1 > band.X0)
                {
                    Rect(buffer, band.X0, area.Top, band.X1, area.Bottom, ChartMeshBuffer.Pack(band.Color, ChartSpecs.BandAlpha * alpha));
                }
            }
        }

        // La línea base: 1 dp border.default (@12 %) en el piso del área.
        public void WriteBaseline(ChartMeshBuffer buffer, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var area = Frame.Area;
            var color = Tokens.BorderDefault;
            Feathered(buffer, area.Left, area.Bottom - ChartSpecs.GridWidth / 2, area.Right, area.Bottom + ChartSpecs.GridWidth / 2,
                ChartMeshBuffer.Pack(color.Rgb, color.Alpha * alpha));
        }

        // La grilla: horizontal, 1 dp blanco @8 %, en las marcas interiores de la métrica (no en 0 ni en el máximo).
        public void WriteGrid(ChartMeshBuffer buffer, double yMax, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var area = Frame.Area;
            var color = ChartMeshBuffer.Pack(Tokens.TextPrimary.Rgb, ChartSpecs.GridAlpha * alpha);
            for (var i = 0; i < _grid.Length; i++)
            {
                var y = PixelY(_grid[i], yMax);
                if (y < area.Top - 0.5 || y > area.Bottom)
                {
                    continue;
                }

                Feathered(buffer, area.Left, y - ChartSpecs.GridWidth / 2, area.Right, y + ChartSpecs.GridWidth / 2, color);
            }
        }

        // Las áreas de los objetivos: el color del tipo @7 % de la línea a la base; con mínimo y máximo se superponen (legacy).
        public void WriteTargetAreas(ChartMeshBuffer buffer, double yMax, double reveal, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var area = Frame.Area;
            var cut = RevealX(reveal);
            for (var i = 0; i < _targetCount; i++)
            {
                var t = _targets[i];
                var x1 = Math.Min(t.X1, cut);
                var y = Math.Max(area.Top, PixelY(t.Y, yMax));
                if (x1 <= t.X0 || y >= area.Bottom)
                {
                    continue;
                }

                Rect(buffer, t.X0, y, x1, area.Bottom, ChartMeshBuffer.Pack(t.Color, ChartSpecs.TargetAreaAlpha * alpha));
            }
        }

        // El área de la serie: degradé del color de la métrica, 24 % arriba del área → 0 en la base (DV-91); el alfa de cada
        // vértice de la curva es 0,24 · (base − y) / alto.
        public void WriteSeriesArea(ChartMeshBuffer buffer, double yMax, double reveal, double alpha)
        {
            if (alpha <= 0 || _series < 2)
            {
                return;
            }

            var area = Frame.Area;
            var visible = VisibleCount(reveal, out var inside, out var tailX, out var tailV);
            if (visible < 2)
            {
                return;
            }

            var baseColor = ChartMeshBuffer.Pack(_seriesRgb, 0);
            var previousTop = -1;
            var previousBase = -1;
            for (var j = 0; j < visible; j++)
            {
                Point(j, inside, tailX, tailV, out var x, out var v);
                var y = Math.Max(area.Top, PixelY(v, yMax));
                var k = area.Height > 0 ? (area.Bottom - y) / area.Height : 0;
                var top = buffer.Vertex(x, y, ChartMeshBuffer.Pack(_seriesRgb, ChartSpecs.SeriesAreaTopAlpha * k * alpha));
                var bottom = buffer.Vertex(x, area.Bottom, baseColor);
                if (previousTop >= 0)
                {
                    buffer.Quad(previousTop, previousBase, bottom, top);
                }

                previousTop = top;
                previousBase = bottom;
            }
        }

        // Los separadores: al inicio de cada franja salvo la primera, vertical de 1 dp blanco @24 % punteada [4, 4] desde arriba
        // del área (la fase arranca en el borde de arriba y no cambia).
        public void WriteSeparators(ChartMeshBuffer buffer, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var area = Frame.Area;
            var color = ChartMeshBuffer.Pack(Tokens.TextPrimary.Rgb, ChartSpecs.SeparatorAlpha * alpha);
            var half = ChartSpecs.SeparatorWidth / 2;
            var period = ChartSpecs.SeparatorDash + ChartSpecs.SeparatorGap;
            for (var i = 1; i < _bandCount; i++)
            {
                var x = _bands[i].X0;
                for (var y0 = area.Top; y0 < area.Bottom; y0 += period)
                {
                    var y1 = Math.Min(area.Bottom, y0 + ChartSpecs.SeparatorDash);
                    Feathered(buffer, x - half, y0, x + half, y1, color);
                }
            }
        }

        // Las líneas objetivo: horizontales de 2 dp, blanco @78 %, punteadas [8, 5] con la fase anclada al inicio de su franja;
        // el último trazo se corta en el fin de la franja (o en el corte del revelado).
        public void WriteTargetLines(ChartMeshBuffer buffer, double yMax, double reveal, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var area = Frame.Area;
            var cut = RevealX(reveal);
            var color = ChartMeshBuffer.Pack(Tokens.TextPrimary.Rgb, ChartSpecs.TargetAlpha * alpha);
            var half = ChartSpecs.TargetWidth / 2;
            var period = ChartSpecs.TargetDash + ChartSpecs.TargetGap;
            for (var i = 0; i < _targetCount; i++)
            {
                var t = _targets[i];
                var end = Math.Min(t.X1, cut);
                var y = Clamp(PixelY(t.Y, yMax), area.Top, area.Bottom);
                for (var x0 = t.X0; x0 < end; x0 += period)
                {
                    var x1 = Math.Min(end, x0 + ChartSpecs.TargetDash);
                    Feathered(buffer, x0, y - half, x1, y + half, color);
                }
            }
        }

        // La línea de la serie: 2 dp en el color base de la métrica con 1 px suavizado de cada lado (4 vértices por punto) y la
        // normal promediada con inglete topado en 2. Un solo punto: un disco de 6 dp sin área.
        public void WriteSeriesLine(ChartMeshBuffer buffer, double yMax, double reveal, double alpha)
        {
            if (alpha <= 0 || _series == 0)
            {
                return;
            }

            if (_series == 1)
            {
                WriteSinglePoint(buffer, yMax, alpha);
                return;
            }

            var area = Frame.Area;
            var visible = VisibleCount(reveal, out var inside, out var tailX, out var tailV);
            if (visible < 2)
            {
                return;
            }

            var core = ChartMeshBuffer.Pack(_seriesRgb, alpha);
            var edge = ChartMeshBuffer.Pack(_seriesRgb, 0);
            var half = ChartSpecs.LineWidth / 2;
            var feather = Frame.PixelDp;
            var previous = -1;
            for (var j = 0; j < visible; j++)
            {
                Point(j, inside, tailX, tailV, out var x, out var v);
                var y = Math.Max(area.Top, PixelY(v, yMax));
                Normal(j, visible, inside, tailX, tailV, yMax, out var nx, out var ny);
                var a = buffer.Vertex(x + nx * (half + feather), y + ny * (half + feather), edge);
                buffer.Vertex(x + nx * half, y + ny * half, core);
                buffer.Vertex(x - nx * half, y - ny * half, core);
                buffer.Vertex(x - nx * (half + feather), y - ny * (half + feather), edge);
                if (previous >= 0)
                {
                    buffer.Quad(previous, previous + 1, a + 1, a);
                    buffer.Quad(previous + 1, previous + 2, a + 2, a + 1);
                    buffer.Quad(previous + 2, previous + 3, a + 3, a + 2);
                }

                previous = a;
            }
        }

        // La tira de tipo (arte §4): 3 dp encima del área en el color base del tipo, con 2 dp de hueco entre franjas.
        public void WriteStrip(ChartMeshBuffer buffer, double alpha)
        {
            if (alpha <= 0)
            {
                return;
            }

            var top = Frame.StripTop;
            var gap = ChartSpecs.TypeStripGap / 2;
            for (var i = 0; i < _bandCount; i++)
            {
                var band = _bands[i];
                var x0 = band.X0 + gap;
                var x1 = band.X1 - gap;
                if (x1 > x0)
                {
                    Rect(buffer, x0, top, x1, top + ChartSpecs.TypeStripHeight, ChartMeshBuffer.Pack(band.Color, alpha));
                }
            }
        }

        // Un solo punto: un disco de 6 dp en el color de la métrica (arte §4), con su borde de 1 px.
        public void WriteSinglePoint(ChartMeshBuffer buffer, double yMax, double alpha)
        {
            if (alpha <= 0 || _series == 0)
            {
                return;
            }

            var area = Frame.Area;
            var cx = (double)_sx[0];
            var cy = Clamp(PixelY(_sv[0], yMax), area.Top, area.Bottom);
            Disk(buffer, cx, cy, ChartSpecs.SinglePointDiameter / 2, Frame.PixelDp, ChartMeshBuffer.Pack(_seriesRgb, alpha),
                ChartMeshBuffer.Pack(_seriesRgb, 0));
        }

        // Un disco con su borde suavizado (también para el cursor): centro + 16 + 16 vértices.
        public static void Disk(ChartMeshBuffer buffer, double cx, double cy, double radius, double feather, uint fill, uint edge)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            const int Segments = 16;
            var center = buffer.Vertex(cx, cy, fill);
            var first = buffer.VertexCount;
            for (var i = 0; i < Segments; i++)
            {
                var angle = 2 * Math.PI * i / Segments;
                var c = Math.Cos(angle);
                var s = Math.Sin(angle);
                buffer.Vertex(cx + c * radius, cy + s * radius, fill);
                buffer.Vertex(cx + c * (radius + feather), cy + s * (radius + feather), edge);
            }

            for (var i = 0; i < Segments; i++)
            {
                var inner = first + 2 * i;
                var nextInner = first + 2 * ((i + 1) % Segments);
                buffer.Triangle(center, inner, nextInner);
                buffer.Quad(inner, inner + 1, nextInner + 1, nextInner);
            }
        }

        // Lo que ocupa un disco.
        public const int DiskVertices = 33;
        public const int DiskIndices = 16 * 9;

        // ── Ayudas ──
        private double RevealX(double reveal)
        {
            var area = Frame.Area;
            if (reveal >= 1)
            {
                return double.PositiveInfinity;
            }

            return area.Left + Math.Max(0, reveal) * area.Width;
        }

        // Cuántos puntos se dibujan con este revelado: los que tienen x ≤ corte y, si el corte cae entre dos, uno interpolado.
        private int VisibleCount(double reveal, out int inside, out double tailX, out double tailV)
        {
            inside = _series;
            tailX = 0;
            tailV = 0;
            var cut = RevealX(reveal);
            if (double.IsPositiveInfinity(cut))
            {
                return _series;
            }

            // El primero con x > corte (los x están ordenados).
            var low = 0;
            var high = _series;
            while (low < high)
            {
                var mid = low + (high - low) / 2;
                if (_sx[mid] <= cut)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            inside = low;
            if (inside == 0 || inside >= _series)
            {
                return inside;
            }

            var x0 = _sx[inside - 1];
            var x1 = _sx[inside];
            var u = x1 > x0 ? (cut - x0) / (x1 - x0) : 0;
            if (u <= 0)
            {
                return inside;
            }

            tailX = cut;
            tailV = _sv[inside - 1] + (_sv[inside] - _sv[inside - 1]) * u;
            return inside + 1;
        }

        // El punto j de lo visible: los de la curva hasta inside y, después, el interpolado del corte.
        private void Point(int j, int inside, double tailX, double tailV, out double x, out double v)
        {
            if (j < inside)
            {
                x = _sx[j];
                v = _sv[j];
                return;
            }

            x = tailX;
            v = tailV;
        }

        // La normal del punto j: el promedio de las de sus dos tramos, con el inglete topado (en dp, con y hacia abajo).
        private void Normal(int j, int visible, int inside, double tailX, double tailV, double yMax, out double nx, out double ny)
        {
            Point(j, inside, tailX, tailV, out var x, out var v);
            var y = PixelY(v, yMax);
            double ax = 0, ay = 0, bx = 0, by = 0;
            var hasA = false;
            var hasB = false;
            if (j > 0)
            {
                Point(j - 1, inside, tailX, tailV, out var px, out var pv);
                hasA = Unit(x - px, y - PixelY(pv, yMax), out ax, out ay);
            }

            if (j < visible - 1)
            {
                Point(j + 1, inside, tailX, tailV, out var qx, out var qv);
                hasB = Unit(qx - x, PixelY(qv, yMax) - y, out bx, out by);
            }

            if (!hasA && !hasB)
            {
                nx = 0;
                ny = -1;
                return;
            }

            if (!hasA)
            {
                ax = bx;
                ay = by;
            }

            if (!hasB)
            {
                bx = ax;
                by = ay;
            }

            // Normales (−dy, dx) de cada tramo y su suma.
            var n1x = -ay;
            var n1y = ax;
            var mx = n1x + -by;
            var my = n1y + bx;
            var length = Math.Sqrt(mx * mx + my * my);
            if (length < 1e-9)
            {
                nx = n1x;
                ny = n1y;
                return;
            }

            mx /= length;
            my /= length;
            var cos = mx * n1x + my * n1y;
            var scale = cos > 1.0 / ChartSpecs.MiterLimit ? 1.0 / cos : ChartSpecs.MiterLimit;
            nx = mx * scale;
            ny = my * scale;
        }

        private static bool Unit(double dx, double dy, out double ux, out double uy)
        {
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-9)
            {
                ux = 0;
                uy = 0;
                return false;
            }

            ux = dx / length;
            uy = dy / length;
            return true;
        }

        private static void Rect(ChartMeshBuffer buffer, double x0, double y0, double x1, double y1, uint color)
        {
            var a = buffer.Vertex(x0, y0, color);
            var b = buffer.Vertex(x1, y0, color);
            var c = buffer.Vertex(x1, y1, color);
            var d = buffer.Vertex(x0, y1, color);
            buffer.Quad(a, b, c, d);
        }

        // Un rectángulo con su borde de 1 px que va a alfa 0 (el suavizado de los trazos: el panel no tiene MSAA).
        private void Feathered(ChartMeshBuffer buffer, double x0, double y0, double x1, double y1, uint color)
        {
            var f = Frame.PixelDp;
            var edge = color & 0xFFFFFF00u;
            var i0 = buffer.Vertex(x0, y0, color);
            var i1 = buffer.Vertex(x1, y0, color);
            var i2 = buffer.Vertex(x1, y1, color);
            var i3 = buffer.Vertex(x0, y1, color);
            var o0 = buffer.Vertex(x0 - f, y0 - f, edge);
            var o1 = buffer.Vertex(x1 + f, y0 - f, edge);
            var o2 = buffer.Vertex(x1 + f, y1 + f, edge);
            var o3 = buffer.Vertex(x0 - f, y1 + f, edge);
            buffer.Quad(i0, i1, i2, i3);
            buffer.Quad(o0, o1, i1, i0);
            buffer.Quad(o1, o2, i2, i1);
            buffer.Quad(o2, o3, i3, i2);
            buffer.Quad(o3, o0, i0, i3);
        }

        // El tope de un cuadro de esta geometría (una curva): se calcula al preparar.
        private void ComputeBudget()
        {
            var area = Frame.Area;
            var points = _series + 1;
            var vertices = 4 * points + 2 * points;
            var indices = 18 * Math.Max(0, points - 1) + 6 * Math.Max(0, points - 1);
            vertices += DiskVertices;
            indices += DiskIndices;

            // Franjas y tira: 4 / 6 cada una.
            vertices += 8 * _bandCount;
            indices += 12 * _bandCount;

            // Separadores: un rectángulo suavizado (8 / 30) por trazo.
            var dashes = (int)Math.Ceiling(area.Height / (ChartSpecs.SeparatorDash + ChartSpecs.SeparatorGap)) + 1;
            vertices += 8 * dashes * SeparatorCount;
            indices += 30 * dashes * SeparatorCount;

            // Grilla y base.
            vertices += 8 * (_grid.Length + 1);
            indices += 30 * (_grid.Length + 1);

            // Objetivos: el área (4 / 6) y sus trazos.
            for (var i = 0; i < _targetCount; i++)
            {
                var length = Math.Max(0, _targets[i].X1 - _targets[i].X0);
                var count = (int)Math.Ceiling(length / (ChartSpecs.TargetDash + ChartSpecs.TargetGap)) + 1;
                vertices += 4 + 8 * count;
                indices += 6 + 30 * count;
            }

            VertexBudget = vertices;
            IndexBudget = indices;
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : value > max ? max : value;
        }

        private static T[] Grow<T>(T[] array, int size)
        {
            return array.Length >= size ? array : new T[size];
        }

        private readonly struct BandPx
        {
            public BandPx(double x0, double x1, Rgb color)
            {
                X0 = x0;
                X1 = x1;
                Color = color;
            }

            public double X0 { get; }

            public double X1 { get; }

            public Rgb Color { get; }
        }

        private readonly struct TargetPx
        {
            public TargetPx(double x0, double x1, double y, Rgb color)
            {
                X0 = x0;
                X1 = x1;
                Y = y;
                Color = color;
            }

            public double X0 { get; }

            public double X1 { get; }

            public double Y { get; }

            public Rgb Color { get; }
        }
    }
}
