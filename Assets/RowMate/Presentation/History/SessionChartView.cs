using System;
using System.Diagnostics;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Presentation.Motion;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.History
{
    // El gráfico del detalle (F-HIS-13 a -15; A9-01; 02-tech-design §5.4 de la It. 9):
    //   #session-chart .rm-chart                 220 de alto en todos los tamaños, bg.sunken r 12
    //   ├── #chart-mesh                           la malla propia (generateVisualContent + Allocate, sin Painter2D; sin eventos)
    //   ├── ChartAxisLabels (#chart-axis)         #chart-y-<i>, #chart-x-<i>
    //   └── SessionChartCursor (#chart-cursor)    recibe el puntero; guía, punto y #chart-tooltip
    // Todo lo que calcula es Core (ChartGeometry, ChartTimeline): la vista prepara las 5 métricas una vez por datos y por ancho
    // (así cambiar de métrica no asigna nada) y en cada repintado escribe el cuadro en un buffer de capacidad fija y lo copia a
    // la malla. Se repinta SOLO mientras corre una línea de tiempo (la entrada o un cambio de métrica) o al cambiar el ancho:
    // desplazar la página o mover el cursor no la repintan (GenerateCount).
    // - Entrada (arte §5.1): una vez por apertura, cuando hay datos, ancho y la página terminó de entrar (Begin). Con reducir
    //   movimiento, el estado final en el cuadro 0 (gate M-01).
    // - Cambio de métrica (DT9-11): fundido cruzado de 220 con el yMax en 300 (ChartTimeline.Switch); reducir movimiento, en el acto.
    // Control para el arnés y las pruebas: FinishEntry(), ShowCursorAt(segundos), LastGenerateMs y los marcadores
    // RowMate.Chart.Prepare y RowMate.Chart.Generate del Profiler.
    public sealed class SessionChartView : VisualElement, IMotion
    {
        public static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("RowMate.Chart.Prepare");
        public static readonly ProfilerMarker GenerateMarker = new ProfilerMarker("RowMate.Chart.Generate");

        private const int MetricCount = 5;

        private readonly MotionDriver _motion;
        private readonly VisualElement _mesh;
        private readonly ChartGeometry[] _geometries = new ChartGeometry[MetricCount];
        private readonly ChartMeshBuffer _buffer = new ChartMeshBuffer(1024, 2048);

        // It. 15 (QA-V15-01): la malla grande se entrega con DrawMesh desde arreglos propios de tamaño fijo (ChartMeshUpload).
        private readonly ChartMeshUpload _upload = new ChartMeshUpload();
        private SessionChartData? _data;
        private ChartMetric _metric = ChartMetric.Watts;
        private ChartGeometry? _previous;
        private ChartVisual _visual = ChartVisual.Rest(ChartSpecs.EmptyYMax);
        private Phase _phase = Phase.Idle;
        private double _start;
        private double _fromYMax;
        private float _width = -1;
        private float _height = -1;
        private bool _entryRequested;
        private bool _entryDone;
        private int _stepSelected = -1;
        private double _stepSelectedT;
        private int _stepLeaving = -1;
        private double _stepLeavingT;

        public SessionChartView(MotionDriver motion)
        {
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            name = "session-chart";
            AddToClassList("rm-chart");
            pickingMode = PickingMode.Ignore;
            for (var i = 0; i < MetricCount; i++)
            {
                _geometries[i] = new ChartGeometry();
            }

            _mesh = new VisualElement { name = "chart-mesh" };
            _mesh.AddToClassList("rm-chart__mesh");
            _mesh.pickingMode = PickingMode.Ignore;
            _mesh.style.position = Position.Absolute;
            _mesh.style.left = 0;
            _mesh.style.top = 0;
            _mesh.style.right = 0;
            _mesh.style.bottom = 0;
            _mesh.generateVisualContent += Generate;
            _mesh.RegisterCallback<DetachFromPanelEvent>(_ => _upload.Dispose());
            Add(_mesh);
            Labels = new ChartAxisLabels();
            Add(Labels);
            Cursor = new SessionChartCursor(this, motion);
            Add(Cursor);
            StepHighlight = new StepHighlightMotion(motion, ApplyStepHighlight);
            RegisterCallback<GeometryChangedEvent>(OnGeometry);
        }

        private enum Phase
        {
            Idle,
            Entry,
            Switch,
        }

        public bool IsFinite => true;

        public SessionChartData? Data => _data;

        public ChartMetric Metric => _metric;

        // La geometría de la métrica a la vista (null antes de tener datos y ancho).
        public ChartGeometry? Current => _data != null && _geometries[(int)_metric].IsPrepared ? _geometries[(int)_metric] : null;

        public ChartAxisLabels Labels { get; }

        public SessionChartCursor Cursor { get; }

        public VisualElement MeshLayer => _mesh;

        public ChartVisual Visual => _visual;

        // It. 15 (#33, AC-D15-02): la marca del paso elegido en "Rendimiento por paso" (todas sus franjas del 10 al 22 %).
        public StepHighlightMotion StepHighlight { get; }

        // El cuadro que se dibuja: la línea de tiempo con la marca del paso.
        public ChartVisual DrawnVisual => _visual.WithSteps(_stepSelected, _stepSelectedT, _stepLeaving, _stepLeavingT);

        // Marca un paso (índice del paso en la rutina; −1 desmarca), con su animación de 160 ms.
        public void SelectStep(int stepIndex)
        {
            StepHighlight.Select(stepIndex);
        }

        public bool IsAnimating => _phase != Phase.Idle;

        public bool EntryDone => _entryDone;

        // Repintados de la malla grande y lo que tardó el último (ms de CPU, Stopwatch).
        public int GenerateCount { get; private set; }

        public double LastGenerateMs { get; private set; }

        public int LastVertexCount { get; private set; }

        // Lo que tardó la última preparación de las 5 métricas (ms).
        public double LastPrepareMs { get; private set; }

        public void SetData(SessionChartData data, ChartMetric metric)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _metric = metric;
            _previous = null;
            _phase = Phase.Idle;
            _motion.Remove(this);
            PrepareAll();
            Labels.Layout(_geometries[(int)_metric], false);
            _visual = ChartVisual.Rest(YMaxOf(_metric));
            if (!_entryDone)
            {
                // Hasta la entrada no se ve nada (el marco sí: es el fondo del elemento).
                _visual = ChartTimeline.Entry(0, YMaxOf(_metric), false);
            }

            ApplyLabels();
            _mesh.MarkDirtyRepaint();
            TryStartEntry();
        }

        // La página terminó de entrar y los datos están: arranca la entrada (una vez por apertura).
        public void Begin()
        {
            _entryRequested = true;
            TryStartEntry();
        }

        // Lleva la entrada (o un cambio de métrica) a su final en el acto (el toque durante la entrada; el arnés).
        public void FinishEntry()
        {
            _entryRequested = true;
            if (_phase == Phase.Idle && _entryDone)
            {
                Cursor.FinishMotion();
                return;
            }

            _entryDone = true;
            Finish();
            Cursor.FinishMotion();
        }

        public void ShowCursorAt(double seconds)
        {
            FinishEntry();
            Cursor.ShowAtSeconds(seconds);
        }

        public void SetMetric(ChartMetric metric)
        {
            if (metric == _metric || _data == null)
            {
                _metric = metric;
                return;
            }

            var from = _visual.YMax;
            _previous = Current;
            _metric = metric;
            var current = Current;
            if (current == null)
            {
                _previous = null;
                return;
            }

            if (!_entryDone || !_motion.Armed || _motion.Settings.ReduceMotion || _previous == null)
            {
                // Durante la entrada sigue la entrada con la métrica nueva; con reducir movimiento (o sin reloj), la métrica nueva
                // en su estado final en el acto (AC-HIS-12-02).
                _previous = null;
                if (_entryDone)
                {
                    _visual = ChartVisual.Rest(current.YMax);
                    _phase = Phase.Idle;
                    _motion.Remove(this);
                }

                Labels.Layout(current, false);
                ApplyLabels();
                _mesh.MarkDirtyRepaint();
                Cursor.Retarget();
                return;
            }

            Labels.Layout(current, true);
            _fromYMax = from;
            _phase = Phase.Switch;
            _start = _motion.Now;
            _visual = ChartTimeline.Switch(0, _fromYMax, current.YMax, false);
            _motion.Add(this);
            ApplyLabels();
            _mesh.MarkDirtyRepaint();
            Cursor.Retarget();
        }

        // El punto local (dp del gráfico) cae dentro del área de datos.
        public bool InArea(Vector2 local)
        {
            var current = Current;
            if (current == null)
            {
                return false;
            }

            var area = current.Frame.Area;
            return local.x >= area.Left && local.x <= area.Right && local.y >= area.Top - ChartSpecs.TypeStripSpace && local.y <= area.Bottom;
        }

        public bool Tick(double nowSeconds)
        {
            var current = Current;
            if (current == null || _phase == Phase.Idle)
            {
                return false;
            }

            var tMs = (nowSeconds - _start) * 1000;
            var reduce = _motion.Settings.ReduceMotion;
            _visual = _phase == Phase.Entry
                ? ChartTimeline.Entry(tMs, current.YMax, reduce)
                : ChartTimeline.Switch(tMs, _fromYMax, current.YMax, reduce);
            ApplyLabels();
            _mesh.MarkDirtyRepaint();
            if (_visual.Finished)
            {
                if (_phase == Phase.Entry)
                {
                    _entryDone = true;
                }

                _phase = Phase.Idle;
                _previous = null;
                Labels.SetAlphas(1, 1, 0);
                return false;
            }

            return true;
        }

        // Lo más grande que se pide a UI Toolkit de una vez (en vértices): una malla grande en un solo Allocate pasa por el camino
        // de "exceso" del asignador temporal de UIR, que asigna en cada cuadro (medido en HistoryPerformanceTests); en trozos de
        // este tamaño, no.
        public const int ChunkVertices = 1024;

        // Copia un buffer de Core a la malla (sin asignar: un vértice por vez), en trozos de triángulos seguidos cuyo rango de
        // vértices no pasa de ChunkVertices (los índices se corren al comienzo del trozo). Color sin compensación, como los
        // Allocate del HUD. It. 15 (QA-V15-01): solo para mallas chicas (el punto del cursor); la grande va por ChartMeshUpload.
        internal static void Copy(MeshGenerationContext context, ChartMeshBuffer buffer)
        {
            var ic = buffer.IndexCount;
            if (buffer.VertexCount == 0 || ic < 3)
            {
                return;
            }

            var xs = buffer.X;
            var ys = buffer.Y;
            var rgba = buffer.Rgba;
            var indices = buffer.Indices;
            var triangles = ic / 3;
            var t = 0;
            while (t < triangles)
            {
                var min = int.MaxValue;
                var max = -1;
                var end = t;
                while (end < triangles)
                {
                    var k = 3 * end;
                    var a = indices[k];
                    var b = indices[k + 1];
                    var c = indices[k + 2];
                    var lo = Math.Min(min, Math.Min(a, Math.Min(b, c)));
                    var hi = Math.Max(max, Math.Max(a, Math.Max(b, c)));
                    if (hi - lo + 1 > ChunkVertices && end > t)
                    {
                        break;
                    }

                    min = lo;
                    max = hi;
                    end++;
                }

                var mesh = context.Allocate(max - min + 1, 3 * (end - t));
                for (var i = min; i <= max; i++)
                {
                    var color = rgba[i];
                    mesh.SetNextVertex(new Vertex
                    {
                        position = new Vector3(xs[i], ys[i], Vertex.nearZ),
                        tint = new Color32((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color),
                    });
                }

                // UI Toolkit solo dibuja los triángulos en el sentido del reloj (en pantalla, y hacia abajo): los que vienen al revés (el
                // área de la curva) se dan vuelta acá.
                for (var k = 3 * t; k < 3 * end; k += 3)
                {
                    int a = indices[k], b = indices[k + 1], c = indices[k + 2];
                    var cross = (xs[b] - xs[a]) * (ys[c] - ys[a]) - (ys[b] - ys[a]) * (xs[c] - xs[a]);
                    if (cross < 0)
                    {
                        var swap = b;
                        b = c;
                        c = swap;
                    }

                    mesh.SetNextIndex((ushort)(a - min));
                    mesh.SetNextIndex((ushort)(b - min));
                    mesh.SetNextIndex((ushort)(c - min));
                }

                t = end;
            }
        }

        private void ApplyStepHighlight(int selected, double selectedT, int leaving, double leavingT)
        {
            _stepSelected = selected;
            _stepSelectedT = selectedT;
            _stepLeaving = leaving;
            _stepLeavingT = leavingT;
            _mesh.MarkDirtyRepaint();
        }

        private void Finish()
        {
            _motion.Remove(this);
            _phase = Phase.Idle;
            _previous = null;
            _visual = ChartVisual.Rest(YMaxOf(_metric));
            ApplyLabels();
            _mesh.MarkDirtyRepaint();
        }

        private void TryStartEntry()
        {
            if (_entryDone || !_entryRequested || _phase == Phase.Entry || Current == null)
            {
                return;
            }

            if (!_motion.Armed || _motion.Settings.ReduceMotion)
            {
                _entryDone = true;
                Finish();
                return;
            }

            _phase = Phase.Entry;
            _start = _motion.Now;
            _visual = ChartTimeline.Entry(0, YMaxOf(_metric), false);
            ApplyLabels();
            _motion.Add(this);
            _mesh.MarkDirtyRepaint();
        }

        private void ApplyLabels()
        {
            Labels.SetAlphas(_visual.StaticAlpha, _visual.StaticAlpha * _visual.NewLabelsAlpha, _visual.StaticAlpha * _visual.OldLabelsAlpha);
        }

        private double YMaxOf(ChartMetric metric)
        {
            var g = _geometries[(int)metric];
            return g.IsPrepared ? g.YMax : ChartSpecs.EmptyYMax;
        }

        private void OnGeometry(GeometryChangedEvent evt)
        {
            var w = evt.newRect.width;
            var h = evt.newRect.height;
            if (Math.Abs(w - _width) < 0.01f && Math.Abs(h - _height) < 0.01f)
            {
                return;
            }

            _width = w;
            _height = h;
            if (_data == null)
            {
                return;
            }

            PrepareAll();
            Labels.Layout(_geometries[(int)_metric], false);
            if (_phase == Phase.Idle)
            {
                _visual = _entryDone ? ChartVisual.Rest(YMaxOf(_metric)) : ChartTimeline.Entry(0, YMaxOf(_metric), false);
            }

            ApplyLabels();
            _mesh.MarkDirtyRepaint();
            Cursor.Retarget();
            TryStartEntry();
        }

        // Las 5 métricas, una vez por datos y por ancho (DT9-13: ≤ 2 ms con 746 puntos). La capacidad del buffer es la suma de las
        // dos mayores (el fundido cruzado dibuja dos curvas).
        private void PrepareAll()
        {
            if (_data == null || !(_width > 0) || !(_height > 0))
            {
                return;
            }

            var started = Stopwatch.GetTimestamp();
            using (PrepareMarker.Auto())
            {
                var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
                var frame = ChartFrame.For(_width, _height, ppp);
                int first = 0, second = 0, firstI = 0, secondI = 0;
                for (var i = 0; i < MetricCount; i++)
                {
                    var g = _geometries[i];
                    g.Prepare(_data, (ChartMetric)i, frame);
                    if (g.VertexBudget > first)
                    {
                        second = first;
                        secondI = firstI;
                        first = g.VertexBudget;
                        firstI = g.IndexBudget;
                    }
                    else if (g.VertexBudget > second)
                    {
                        second = g.VertexBudget;
                        secondI = g.IndexBudget;
                    }
                }

                _buffer.EnsureCapacity(Math.Min(ChartMeshBuffer.MaxVertices, first + second), firstI + secondI);
            }

            LastPrepareMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        }

        private void Generate(MeshGenerationContext context)
        {
            var current = Current;
            if (current == null)
            {
                return;
            }

            var started = Stopwatch.GetTimestamp();
            using (GenerateMarker.Auto())
            {
                GenerateCount++;
                var previous = _previous != null && _previous.IsPrepared ? _previous : null;
                var visual = _visual.WithSteps(_stepSelected, _stepSelectedT, _stepLeaving, _stepLeavingT);
                ChartGeometry.WriteFrame(_buffer, current, previous, in visual);
                LastVertexCount = _buffer.VertexCount;
                _upload.Draw(context, _buffer);
            }

            LastGenerateMs = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        }
    }
}
