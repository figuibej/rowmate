using System;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace RowMate.Presentation.History
{
    // El cursor del gráfico (AC-HIS-13-07; arte §5.4, §5.5; A9-03; 02-tech-design §5.5): una capa HERMANA de la malla grande, que
    // recibe el puntero y dibuja su propia malla chica (guía de 1 dp blanco @40 % a todo el alto del área y el punto de 8 dp en el
    // color de la métrica con su anillo de 2 dp bg.sunken) y el tooltip (#chart-tooltip: glass.fallback, borde 1 border.default,
    // r 8, relleno 6/10, ancho mínimo 64; "{valor} {unidad}" con el valor en metric.*.hud 16/700 y la unidad en text.secondary
    // 12). Mover el cursor NUNCA repinta la malla grande: solo esta capa y el tooltip (≤ 40 vértices y un texto dibujado por
    // carácter, sin cadenas nuevas por cuadro).
    // - Escritorio: el cursor sigue al puntero sobre el área y se va al salir.
    // - Toque: apoyar el dedo lo muestra en ese cuadro; si se mueve más de 6 dp y más en x que en y, el gráfico captura el
    //   puntero (la página no se desplaza); si el primer movimiento es vertical, lo suelta y se oculta (desplaza la ScrollView).
    //   Al levantar el dedo, se va con la salida de 160 (gate m-06).
    // - La guía y el punto siguen al punto elegido (el más cercano en x sobre los puntos ORIGINALES, ChartHitTest) con
    //   spring.snappy ω 22; con reducir movimiento, en el acto. El tooltip entra en 90 (escala 0,96 → 1) y sale en 160.
    public sealed class SessionChartCursor : VisualElement, IMotion
    {
        private static readonly ushort[] GuideIndices = { 0, 1, 2, 0, 2, 3 };

        private readonly Vertex[] _guide = new Vertex[4];
        private readonly SessionChartView _chart;
        private readonly MotionDriver _motion;
        private readonly ChartMeshBuffer _buffer = new ChartMeshBuffer(2 * ChartGeometry.DiskVertices + 8, 2 * ChartGeometry.DiskIndices + 30);
        private readonly char[] _text = new char[HistoryFormats.TooltipMaxChars];
        private readonly VisualElement _tooltip;
        private readonly DrawnFont _valueFont;
        private readonly DrawnFont _unitFont;
        private CriticalSpring _x;
        private CriticalSpring _y;
        private double _targetX;
        private double _targetY;
        private double _last;
        private double _fadeStart;
        private double _fadeFrom;
        private bool _visible;
        private bool _leaving;
        private bool _snap;
        private int _index = -1;
        private ChartMetric _textMetric;
        private int _length;
        private int _split;
        private float _tooltipWidth;
        private float _tooltipHeight;
        private int _pointerId = -1;
        private Vector2 _downAt;
        private bool _dragging;
        private bool _decided;

        public SessionChartCursor(SessionChartView chart, MotionDriver motion)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            name = "chart-cursor";
            AddToClassList("rm-chart__cursor");
            style.position = Position.Absolute;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
            pickingMode = PickingMode.Position;
            generateVisualContent += DrawCursor;

            _tooltip = new VisualElement { name = "chart-tooltip" };
            _tooltip.AddToClassList("rm-chart__tooltip");
            _tooltip.pickingMode = PickingMode.Ignore;
            _tooltip.style.position = Position.Absolute;
            _tooltip.style.left = 0;
            _tooltip.style.top = 0;
            _tooltip.style.opacity = 0;
            _tooltip.generateVisualContent += DrawTooltip;
            // Opacidad, escala y posición cambian por cuadro: sin volver a teselar (UIR).
            _tooltip.usageHints = UsageHints.DynamicColor | UsageHints.DynamicTransform;
            Add(_tooltip);
            _valueFont = new DrawnFont(this, "rm-type-num-stat", 16, null);
            _unitFont = new DrawnFont(this, "rm-type-body-xs", 12, null);

            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<PointerCancelEvent>(_ => Hide());
        }

        public bool IsFinite => true;

        public bool IsShown => _visible && !_leaving;

        // El punto elegido (índice en los puntos originales; −1 sin cursor).
        public int Index => _visible ? _index : -1;

        public double GuideX => _x.Position;

        public double DotY => _y.Position;

        public VisualElement Tooltip => _tooltip;

        public float TooltipOpacity => _tooltip.resolvedStyle.opacity;

        // Veces que se repintó esta capa (la malla grande cuenta las suyas: SessionChartView.GenerateCount).
        public int GenerateCount { get; private set; }

        // El texto del tooltip (asigna: solo para pruebas y sondas).
        public string TooltipText => new string(_text, 0, _length);

        // Muestra el cursor en el punto más cercano a ese tiempo, en el acto (las tomas y las pruebas: ShowCursorAt).
        public void ShowAtSeconds(double seconds)
        {
            var data = _chart.Data;
            if (data == null || data.Count == 0)
            {
                return;
            }

            Select(ChartHitTest.Nearest(data.X, data.Count, seconds), true);
            FinishMotion();
        }

        // La métrica o el ancho cambiaron: el cursor se queda en su x y se acomoda a la y nueva con su resorte.
        public void Retarget()
        {
            if (_visible && _index >= 0)
            {
                Select(_index, false);
            }
        }

        public void Hide()
        {
            ReleaseIfCaptured();
            if (!_visible || _leaving)
            {
                return;
            }

            _leaving = true;
            _fadeStart = _motion.Now;
            _fadeFrom = _tooltip.resolvedStyle.opacity;
            if (!_motion.Armed)
            {
                Clear();
                return;
            }

            _motion.Add(this);
        }

        // Lleva el resorte y los fundidos a su final (FinishEntry del arnés).
        public void FinishMotion()
        {
            if (!_visible)
            {
                return;
            }

            if (_leaving)
            {
                Clear();
                return;
            }

            _motion.Remove(this);
            _x.SnapTo(_targetX);
            _y.SnapTo(_targetY);
            _tooltip.style.opacity = 1;
            _tooltip.style.scale = new Scale(Vector3.one);
            Place();
        }

        public bool Tick(double nowSeconds)
        {
            if (!_visible)
            {
                return false;
            }

            var dt = Math.Max(0, nowSeconds - _last);
            _last = nowSeconds;
            var moving = false;
            if (_leaving)
            {
                var visual = ChartTimeline.TooltipOut((nowSeconds - _fadeStart) * 1000, _fadeFrom);
                _tooltip.style.opacity = (float)visual.Opacity;
                if (visual.Finished)
                {
                    Clear();
                    return false;
                }

                return true;
            }

            if (!_snap)
            {
                _x.Step(dt, _targetX, ChartSpecs.CursorOmega);
                _y.Step(dt, _targetY, ChartSpecs.CursorOmega);
                moving = !_x.IsSettled(_targetX, 0.05) || !_y.IsSettled(_targetY, 0.05);
                if (!moving)
                {
                    _x.SnapTo(_targetX);
                    _y.SnapTo(_targetY);
                }
            }

            var tip = ChartTimeline.TooltipIn((nowSeconds - _fadeStart) * 1000, _motion.Settings.ReduceMotion);
            _tooltip.style.opacity = (float)tip.Opacity;
            var s = (float)tip.Scale;
            _tooltip.style.scale = new Scale(new Vector3(s, s, 1));
            Place();
            return moving || !tip.Finished;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            var local = (Vector2)evt.localPosition;
            if (evt.pointerType == PointerType.mouse || evt.pointerType == PointerType.pen && evt.pressedButtons == 0)
            {
                if (_chart.InArea(local))
                {
                    SelectAt(local.x, false);
                }
                else
                {
                    Hide();
                }

                return;
            }

            if (evt.pointerId != _pointerId)
            {
                return;
            }

            if (!_decided)
            {
                var delta = local - _downAt;
                if (delta.magnitude <= ChartSpecs.GestureSlop)
                {
                    return;
                }

                _decided = true;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    // Más horizontal: el gráfico se queda con el puntero (la página no se desplaza).
                    _dragging = true;
                    this.CapturePointer(evt.pointerId);
                }
                else
                {
                    // Vertical: se suelta y se oculta; desplaza la ScrollView.
                    _pointerId = -1;
                    Hide();
                    return;
                }
            }

            if (_dragging)
            {
                evt.StopPropagation();
                SelectAt(local.x, false);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            var local = (Vector2)evt.localPosition;
            if (!_chart.InArea(local))
            {
                return;
            }

            // Un toque durante la entrada la lleva al final en ese cuadro (arte §5.1).
            _chart.FinishEntry();
            if (evt.pointerType == PointerType.mouse)
            {
                SelectAt(local.x, false);
                return;
            }

            _pointerId = evt.pointerId;
            _downAt = local;
            _decided = false;
            _dragging = false;
            SelectAt(local.x, true);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerType == PointerType.mouse)
            {
                return;
            }

            if (evt.pointerId == _pointerId)
            {
                _pointerId = -1;
                Hide();
            }
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (evt.pointerType == PointerType.mouse)
            {
                Hide();
            }
        }

        private void ReleaseIfCaptured()
        {
            if (_dragging && _pointerId >= 0 && this.HasPointerCapture(_pointerId))
            {
                this.ReleasePointer(_pointerId);
            }

            _dragging = false;
        }

        private void SelectAt(float localX, bool snap)
        {
            var data = _chart.Data;
            var geometry = _chart.Current;
            if (data == null || data.Count == 0 || geometry == null)
            {
                return;
            }

            var index = ChartHitTest.Nearest(data.X, data.Count, geometry.SecondsAt(localX));
            Select(index, snap);
        }

        private void Select(int index, bool snap)
        {
            var data = _chart.Data;
            var geometry = _chart.Current;
            if (data == null || geometry == null || index < 0 || index >= data.Count)
            {
                return;
            }

            var area = geometry.Frame.Area;
            var metric = geometry.Metric;
            _targetX = geometry.PixelX(data.X[index]);
            var plotted = data.Plotted(metric)[index];
            var y = geometry.PixelY(plotted, geometry.YMax);
            _targetY = y < area.Top ? area.Top : y > area.Bottom ? area.Bottom : y;
            var changedText = index != _index || !_visible || _textMetric != metric;
            _index = index;
            if (changedText)
            {
                _textMetric = metric;
                _length = HistoryFormats.WriteTooltip(metric, data.Raw(metric)[index], _text, 0);
                _split = LastSpace(_text, _length);
                MeasureTooltip();
                _tooltip.MarkDirtyRepaint();
            }

            var reduce = _motion.Settings.ReduceMotion || !_motion.Armed;
            var appearing = !_visible || _leaving;
            if (appearing)
            {
                _visible = true;
                _leaving = false;
                _fadeStart = _motion.Now;
                _x.SnapTo(_targetX);
                _y.SnapTo(_targetY);
            }

            _snap = reduce || snap;
            if (_snap)
            {
                _x.SnapTo(_targetX);
                _y.SnapTo(_targetY);
            }

            _last = _motion.Now;
            if (!_motion.Armed)
            {
                _tooltip.style.opacity = 1;
                Place();
                return;
            }

            _motion.Add(this);
            Place();
        }

        // La guía, el punto y el tooltip en su lugar (repinta solo esta capa).
        private void Place()
        {
            MarkDirtyRepaint();
            var geometry = _chart.Current;
            if (geometry == null)
            {
                return;
            }

            var frame = geometry.Frame;
            var w = Math.Max((float)ChartSpecs.TooltipMinWidth, _tooltipWidth);
            var h = _tooltipHeight;
            var x = (float)_x.Position - w / 2;
            var edge = (float)ChartSpecs.TooltipEdge;
            x = Mathf.Clamp(x, edge, Math.Max(edge, (float)frame.Width - edge - w));
            var dotRadius = (float)(ChartSpecs.CursorDotDiameter / 2);
            var y = (float)_y.Position - dotRadius - (float)ChartSpecs.TooltipOffset - h;
            if (y < edge)
            {
                // Arriba no entra: 10 dp debajo del punto.
                y = (float)_y.Position + dotRadius + (float)ChartSpecs.TooltipOffset;
            }

            y = Mathf.Clamp(y, edge, Math.Max(edge, (float)frame.Height - edge - h));
            if (Math.Abs(_tooltip.style.width.value.value - w) > 0.01f)
            {
                _tooltip.style.width = w;
                _tooltip.style.height = h;
            }

            _tooltip.style.translate = new Translate(x, y);
            _tooltip.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));
        }

        private void Clear()
        {
            _motion.Remove(this);
            _visible = false;
            _leaving = false;
            _index = -1;
            _tooltip.style.opacity = 0;
            MarkDirtyRepaint();
        }

        private void MeasureTooltip()
        {
            float width = 0;
            for (var i = 0; i < _length; i++)
            {
                var font = i < _split ? _valueFont : _unitFont;
                var size = i < _split ? 16f : 12f;
                var advance = font.Font != null ? font.Advance(_text[i], size) : size * 0.6f;
                width += float.IsNaN(advance) ? size * 0.6f : advance;
            }

            _tooltipWidth = Mathf.Ceil(width) + 2 * 10 + 2;
            _tooltipHeight = 16 * 1.25f + 2 * 6 + 2;
        }

        private static int LastSpace(char[] text, int length)
        {
            for (var i = length - 1; i >= 0; i--)
            {
                if (text[i] == ' ')
                {
                    return i;
                }
            }

            return length;
        }

        private void DrawCursor(MeshGenerationContext context)
        {
            GenerateCount++;
            var geometry = _chart.Current;
            if (!_visible || geometry == null || _index < 0)
            {
                return;
            }

            var area = geometry.Frame.Area;
            var pixel = geometry.Frame.PixelDp;
            var x = _x.Position;
            var y = _y.Position;
            var alpha = _leaving ? _tooltip.resolvedStyle.opacity : 1.0;
            // La guía: un rectángulo de 1 dp a todo el alto del área.
            Color32 guide = TokenColors.WithAlpha(Tokens.TextPrimary, ChartSpecs.CursorGuideAlpha * alpha);
            var gx = (float)x;
            _guide[0] = new Vertex { position = new Vector3(gx - 0.5f, (float)area.Top, Vertex.nearZ), tint = guide };
            _guide[1] = new Vertex { position = new Vector3(gx + 0.5f, (float)area.Top, Vertex.nearZ), tint = guide };
            _guide[2] = new Vertex { position = new Vector3(gx + 0.5f, (float)area.Bottom, Vertex.nearZ), tint = guide };
            _guide[3] = new Vertex { position = new Vector3(gx - 0.5f, (float)area.Bottom, Vertex.nearZ), tint = guide };
            var quad = context.Allocate(4, 6);
            quad.SetAllVertices(_guide);
            quad.SetAllIndices(GuideIndices);
            _buffer.Clear();
            var ring = Tokens.BgSunken.Rgb;
            var color = ChartMetrics.Palette(geometry.Metric).Rgb;
            var outer = ChartSpecs.CursorDotDiameter / 2 + ChartSpecs.CursorDotRing;
            ChartGeometry.Disk(_buffer, x, y, outer, pixel, ChartMeshBuffer.Pack(ring, alpha), ChartMeshBuffer.Pack(ring, 0));
            ChartGeometry.Disk(_buffer, x, y, ChartSpecs.CursorDotDiameter / 2, pixel, ChartMeshBuffer.Pack(color, alpha), ChartMeshBuffer.Pack(color, 0));
            SessionChartView.Copy(context, _buffer);
        }

        private void DrawTooltip(MeshGenerationContext context)
        {
            if (_length == 0)
            {
                return;
            }

            var geometry = _chart.Current;
            if (geometry == null)
            {
                return;
            }

            var ppp = scaledPixelsPerPoint > 0 ? scaledPixelsPerPoint : 1f;
            var valueColor = TokenColors.ToColor(ChartMetrics.TooltipPalette(geometry.Metric));
            var unitColor = TokenColors.ToColor(Tokens.TextSecondary);
            var w = _tooltip.contentRect.width;
            var textWidth = _tooltipWidth - 2 * 10 - 2;
            var x = Math.Max(10f, (w - textWidth) / 2f);
            var baseline = 6f + 16f;
            for (var i = 0; i < _length; i++)
            {
                var c = _text[i];
                var isValue = i < _split;
                var font = isValue ? _valueFont : _unitFont;
                var size = isValue ? 16f : 12f;
                if (c != ' ' && font.Font != null)
                {
                    font.Draw(context, DrawnFont.CharString(c), x, baseline, size, isValue ? valueColor : unitColor, 1f, ppp);
                }

                var advance = font.Font != null ? font.Advance(c, size) : size * 0.6f;
                x += float.IsNaN(advance) ? size * 0.6f : advance;
            }
        }
    }
}
