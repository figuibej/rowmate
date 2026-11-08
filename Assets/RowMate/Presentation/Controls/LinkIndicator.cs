using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // El indicador del enlace con el remo (00-arte-bluetooth §2.1, §4.1, §4.2; D10-10): UN elemento con malla propia (Painter2D), así el
    // aviso del HUD no suma elementos (AC-BLE-08-06). Modos:
    // - Spinner: el intento en curso (arco de 270° que gira, como Spinner; una vuelta en 900 ms).
    // - WaitingArc: la espera hasta el próximo intento: una pista en border.strong y un arco que se llena desde arriba, en sentido
    //   horario, lineal hasta EndsAt (3 000 ms). El paso al spinner es en el acto: el arco lleno se vuelve la cabeza del spinner.
    // - Check: "Listo" / "Reconectado": el círculo se dibuja en 0-160 y la tilde se traza en 80-240 (ease.enter; CheckGlyph). Reducir
    //   movimiento: entero con un fundido de 160.
    // - LinkOff: el monitor dejó de enviar datos: dos eslabones separados y la diagonal que los corta (el link_off de Material).
    // Los colores salen de las clases rm-link-indicator--* (USS); el tamaño y el trazo, del constructor. Spinner y arco son continuos (no
    // cuentan para Settle) y siguen con reducir movimiento (indican espera); el check es finito hasta los 240 ms. Sin asignaciones por
    // cuadro (en el aviso del HUD, marcador RowMate.Toast.LinkNotice; BleAllocationTests).
    public sealed class LinkIndicator : VisualElement, IMotion
    {
        public const string UssClass = "rm-link-indicator";

        public static readonly ProfilerMarker ToastMarker = new ProfilerMarker("RowMate.Toast.LinkNotice");

        private static readonly Angle Top = new Angle(-90, AngleUnit.Degree);
        private static readonly Angle Full = new Angle(270, AngleUnit.Degree);

        private readonly MotionDriver? _motion;
        private readonly float _strokeDp;
        private Mode _mode = Mode.None;
        private double _endsAt;
        private double _durationSeconds = 3;
        private double _fraction;
        private double _checkStart = double.NaN;
        private double _checkElapsedMs = BleMotionSpecs.CheckTickEndMs;
        private bool _reducedCheck;

        public LinkIndicator(MotionDriver? motion, float sizeDp, float strokeDp = 2)
        {
            _motion = motion;
            _strokeDp = strokeDp;
            AddToClassList(UssClass);
            pickingMode = PickingMode.Ignore;
            style.width = sizeDp;
            style.height = sizeDp;
            style.flexShrink = 0;
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(_ => UpdateRegistration());
            RegisterCallback<DetachFromPanelEvent>(_ => _motion?.Remove(this));
        }

        public enum Mode
        {
            None,
            Spinner,
            WaitingArc,
            Check,
            LinkOff
        }

        // El check se está dibujando (finito); los demás modos con movimiento son continuos.
        public bool IsFinite => _mode == Mode.Check && !double.IsNaN(_checkStart);

        public Mode Current => _mode;

        // La parte llena del arco de espera (0 a 1; pruebas y arnés).
        public double Fraction => _fraction;

        // El aviso del HUD mide su cuadro con el marcador RowMate.Toast.LinkNotice (00-arte §8; A10-13).
        public bool MarkAsToast { get; set; }

        public void Show(Mode mode, double endsAt = 0, double durationSeconds = 3)
        {
            var changed = mode != _mode;
            _mode = mode;
            _endsAt = endsAt;
            _durationSeconds = durationSeconds > 0 ? durationSeconds : 3;
            EnableInClassList(UssClass + "--spinner", mode == Mode.Spinner);
            EnableInClassList(UssClass + "--waiting", mode == Mode.WaitingArc);
            EnableInClassList(UssClass + "--check", mode == Mode.Check);
            EnableInClassList(UssClass + "--link-off", mode == Mode.LinkOff);
            style.display = mode == Mode.None ? DisplayStyle.None : DisplayStyle.Flex;
            if (mode != Mode.Spinner)
            {
                style.rotate = StyleKeyword.Null;
            }

            if (mode == Mode.WaitingArc && _motion != null)
            {
                _fraction = FractionAt(_motion.Now);
            }

            if (changed && mode == Mode.Check)
            {
                StartCheck();
            }
            else if (mode != Mode.Check)
            {
                _checkStart = double.NaN;
                style.opacity = StyleKeyword.Null;
            }

            UpdateRegistration();
            if (changed || mode == Mode.WaitingArc)
            {
                MarkDirtyRepaint();
            }
        }

        public bool Tick(double nowSeconds)
        {
            if (!MarkAsToast)
            {
                return Step(nowSeconds);
            }

            using (ToastMarker.Auto())
            {
                return Step(nowSeconds);
            }
        }

        private bool Step(double nowSeconds)
        {
            if (panel == null)
            {
                return false;
            }

            switch (_mode)
            {
                case Mode.Spinner:
                {
                    var turns = nowSeconds * 1000.0 / MotionSpecs.SpinnerTurnMs;
                    style.rotate = new Rotate(new Angle((float)((turns - Math.Floor(turns)) * 360.0), AngleUnit.Degree));
                    return true;
                }

                case Mode.WaitingArc:
                {
                    var fraction = FractionAt(nowSeconds);
                    if (Math.Abs(fraction - _fraction) > 0.002)
                    {
                        _fraction = fraction;
                        MarkDirtyRepaint();
                    }

                    return true;
                }

                case Mode.Check:
                {
                    if (double.IsNaN(_checkStart))
                    {
                        return false;
                    }

                    var elapsed = (nowSeconds - _checkStart) * 1000.0;
                    var end = _reducedCheck ? BleMotionSpecs.ReducedCheckFadeMs : BleMotionSpecs.CheckTickEndMs;
                    if (elapsed >= end)
                    {
                        _checkStart = double.NaN;
                        _checkElapsedMs = BleMotionSpecs.CheckTickEndMs;
                        style.opacity = StyleKeyword.Null;
                        MarkDirtyRepaint();
                        return false;
                    }

                    if (_reducedCheck)
                    {
                        style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, elapsed / end);
                    }
                    else
                    {
                        _checkElapsedMs = elapsed;
                        MarkDirtyRepaint();
                    }

                    return true;
                }

                default:
                    return false;
            }
        }

        private void StartCheck()
        {
            if (_motion == null || !_motion.Armed)
            {
                _checkStart = double.NaN;
                _checkElapsedMs = BleMotionSpecs.CheckTickEndMs;
                return;
            }

            _reducedCheck = _motion.Settings.ReduceMotion;
            _checkStart = _motion.Now;
            _checkElapsedMs = _reducedCheck ? BleMotionSpecs.CheckTickEndMs : 0;
            if (_reducedCheck)
            {
                style.opacity = 0;
            }
        }

        private double FractionAt(double now)
        {
            var remaining = _endsAt - now;
            var f = 1 - remaining / _durationSeconds;
            return f < 0 ? 0 : f > 1 ? 1 : f;
        }

        private void UpdateRegistration()
        {
            if (_motion == null)
            {
                return;
            }

            if (panel != null && (_mode == Mode.Spinner || _mode == Mode.WaitingArc || _mode == Mode.Check && !double.IsNaN(_checkStart)))
            {
                _motion.Add(this);
            }
            else
            {
                _motion.Remove(this);
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var size = Mathf.Min(rect.width, rect.height);
            if (size <= 0 || _mode == Mode.None)
            {
                return;
            }

            var painter = context.painter2D;
            var color = resolvedStyle.color;
            var center = rect.center;
            var stroke = _strokeDp;
            var radius = size / 2 - stroke / 2;
            painter.lineWidth = stroke;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            switch (_mode)
            {
                case Mode.Spinner:
                    painter.strokeColor = color;
                    painter.BeginPath();
                    painter.Arc(center, radius, Top, new Angle(-90 + (float)MotionSpecs.SpinnerArcDegrees, AngleUnit.Degree), ArcDirection.Clockwise);
                    painter.Stroke();
                    break;
                case Mode.WaitingArc:
                    painter.strokeColor = TokenColors.ToColor(Tokens.BorderStrong);
                    painter.BeginPath();
                    painter.Arc(center, radius, Top, Full, ArcDirection.Clockwise);
                    painter.ClosePath();
                    painter.Stroke();
                    if (_fraction > 0.001)
                    {
                        painter.strokeColor = color;
                        painter.BeginPath();
                        painter.Arc(center, radius, Top, new Angle(-90 + (float)(360 * _fraction), AngleUnit.Degree), ArcDirection.Clockwise);
                        painter.Stroke();
                    }

                    break;
                case Mode.Check:
                    DrawCheck(painter, color, center, radius, stroke);
                    break;
                case Mode.LinkOff:
                    DrawLinkOff(painter, color, center, size, stroke);
                    break;
            }
        }

        // El círculo se dibuja desde arriba en sentido horario (0-160) con un relleno suave, y la tilde se traza (80-240).
        private void DrawCheck(Painter2D painter, Color color, Vector2 center, float radius, float stroke)
        {
            var t = _checkElapsedMs;
            var disc = Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(t, 0, BleMotionSpecs.CheckDiscEndMs));
            var tick = Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(t, BleMotionSpecs.CheckTickStartMs, BleMotionSpecs.CheckTickEndMs));
            if (disc > 0.001)
            {
                var fill = color;
                fill.a *= (float)(0.18 * disc);
                painter.fillColor = fill;
                painter.BeginPath();
                painter.Arc(center, radius, Top, Full, ArcDirection.Clockwise);
                painter.ClosePath();
                painter.Fill();
                painter.strokeColor = color;
                painter.BeginPath();
                painter.Arc(center, radius, Top, new Angle(-90 + (float)(360 * disc), AngleUnit.Degree), ArcDirection.Clockwise);
                if (disc >= 0.999)
                {
                    painter.ClosePath();
                }

                painter.Stroke();
            }

            if (tick <= 0.001)
            {
                return;
            }

            // La tilde dentro del círculo: CheckGlyph en un disco de 0,62 del radio.
            var n = CheckGlyph.Partial(tick, center.x, center.y, radius * 0.62, out var x0, out var y0, out var x1, out var y1, out var x2, out var y2);
            if (n < 2)
            {
                return;
            }

            painter.strokeColor = color;
            painter.lineWidth = stroke;
            painter.BeginPath();
            painter.MoveTo(new Vector2((float)x0, (float)y0));
            painter.LineTo(new Vector2((float)x1, (float)y1));
            if (n > 2)
            {
                painter.LineTo(new Vector2((float)x2, (float)y2));
            }

            painter.Stroke();
        }

        // link_off: dos medios eslabones redondeados con un hueco al centro y la diagonal de arriba a la izquierda a abajo a la derecha.
        private static void DrawLinkOff(Painter2D painter, Color color, Vector2 center, float size, float stroke)
        {
            painter.strokeColor = color;
            var h = size * 0.17f;
            var r = h;
            var left = center + new Vector2(-size * 0.22f, 0);
            var right = center + new Vector2(size * 0.22f, 0);
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x - size * 0.06f, center.y - h));
            painter.LineTo(new Vector2(left.x, left.y - h));
            painter.Arc(left, r, new Angle(-90, AngleUnit.Degree), new Angle(90, AngleUnit.Degree), ArcDirection.CounterClockwise);
            painter.LineTo(new Vector2(center.x - size * 0.06f, center.y + h));
            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x + size * 0.06f, center.y - h));
            painter.LineTo(new Vector2(right.x, right.y - h));
            painter.Arc(right, r, new Angle(-90, AngleUnit.Degree), new Angle(90, AngleUnit.Degree), ArcDirection.Clockwise);
            painter.LineTo(new Vector2(center.x + size * 0.06f, center.y + h));
            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(center + new Vector2(-0.34f, -0.34f) * size);
            painter.LineTo(center + new Vector2(0.34f, 0.34f) * size);
            painter.Stroke();
        }
    }
}
