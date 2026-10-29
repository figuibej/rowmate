using System;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Fuera de objetivo en una tarjeta del HUD (AC-WKT-10-06, ANIMATION_BIBLE §2.8; 04-design-notes §5.3, DN4a-17;
    // MotionSpecs.HudColorMs y OffTarget*). El estado cambia en el acto (la tarjeta pasa a "fuera" desde la lectura misma):
    // esto es solo cómo se ve.
    // - Color del valor: 150 ms ease.move, mezcla sRGB desde el color que se ve (un cruce a mitad de otro no salta).
    // - Flecha: entra en 120 (ease.enter) desde 4 dp en el sentido contrario al que indica (▲ sube desde abajo, ▼ baja desde
    //   arriba) y sale 1 → 0 en 120 (ease.exit) siguiendo 4 dp en su sentido.
    // - Empujón del valor (escala 1 → 1,04 en 0-90 ease.enter → 1 en 90-240 ease.move), solo si quien lo usa lo pide (al pasar
    //   de cumple a fuera dentro del mismo paso, como mucho uno cada 4 s por tarjeta: OffTargetBumpCooldownMs).
    // - Reducir movimiento: sin empujón ni desplazamiento; color 150 y la flecha funde en 120. Instant (al volver del segundo
    //   plano) y sin armar: todo en su estado final en el acto (AC-WKT-02-08).
    // Sin asignaciones por cuadro.
    public sealed class OffTargetMotion : IMotion
    {
        private readonly MotionDriver _driver;
        private readonly VisualElement _value;
        private readonly VisualElement _arrow;
        private Color _colorFrom;
        private Color _colorTo;
        private double _colorStart = double.NaN;
        private bool _arrowShown;
        private double _arrowStart = double.NaN;
        private float _arrowFromOpacity;
        private float _arrowDirection;
        private double _bumpStart = double.NaN;
        private double _lastBump = double.NegativeInfinity;
        private bool _reduced;
        private bool _running;
        private bool _initialized;

        public OffTargetMotion(MotionDriver driver, VisualElement value, VisualElement arrow)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _value = value ?? throw new ArgumentNullException(nameof(value));
            _arrow = arrow ?? throw new ArgumentNullException(nameof(arrow));
            _value.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
        }

        public bool IsFinite => true;

        public bool IsRunning => _running;

        // El color que se ve ahora.
        public Color CurrentColor { get; private set; }

        public bool ArrowShown => _arrowShown;

        // Último empujón (para la regla de los 4 s).
        public double LastBumpAt => _lastBump;

        // El valor va hacia color; la flecha se ve o no (arrowDirection: −1 ▲, +1 ▼, en dp de y por cada unidad de desplazamiento);
        // bump: pedir el empujón (lo filtra la regla de los 4 s). instant: todo en el acto.
        public void Set(Color color, bool arrowShown, float arrowDirection, bool bump, bool instant)
        {
            var now = _driver.Now;
            _reduced = _driver.Settings.ReduceMotion;
            var animate = !instant && _initialized && _driver.Armed;
            _initialized = true;

            if (!animate)
            {
                _colorStart = double.NaN;
                _arrowStart = double.NaN;
                _bumpStart = double.NaN;
                CurrentColor = color;
                _colorTo = color;
                _arrowShown = arrowShown;
                _arrowDirection = arrowDirection;
                ApplyColor(color);
                ApplyArrow(arrowShown ? 1f : 0f, 0f);
                _value.style.scale = StyleKeyword.Null;
                Stop();
                return;
            }

            if (color != _colorTo)
            {
                _colorFrom = CurrentColor;
                _colorTo = color;
                _colorStart = now;
            }

            if (arrowShown != _arrowShown)
            {
                _arrowFromOpacity = _arrow.resolvedStyle.opacity;
                _arrowShown = arrowShown;
                _arrowDirection = arrowDirection;
                _arrowStart = now;
                if (arrowShown)
                {
                    _arrow.style.display = DisplayStyle.Flex;
                }
            }

            if (bump && !_reduced && (now - _lastBump) * 1000.0 >= MotionSpecs.OffTargetBumpCooldownMs)
            {
                _bumpStart = now;
                _lastBump = now;
            }

            if (!double.IsNaN(_colorStart) || !double.IsNaN(_arrowStart) || !double.IsNaN(_bumpStart))
            {
                Start();
                Tick(now);
            }
        }

        // Olvida la regla de los 4 s (paso nuevo: el empujón nunca sale al empezar un paso, y el próximo cruce del paso nuevo
        // cuenta desde ahí).
        public void ResetBumpWindow()
        {
            _lastBump = double.NegativeInfinity;
        }

        public void Finish()
        {
            if (!_running)
            {
                return;
            }

            Tick(double.PositiveInfinity);
        }

        public bool Tick(double nowSeconds)
        {
            var active = false;
            if (!double.IsNaN(_colorStart))
            {
                var t = Progress(nowSeconds, _colorStart, MotionSpecs.HudColorMs);
                CurrentColor = TokenColors.Lerp(_colorFrom, _colorTo, Easing.Evaluate(Tokens.EaseMove, t));
                ApplyColor(CurrentColor);
                if (t >= 1)
                {
                    _colorStart = double.NaN;
                    CurrentColor = _colorTo;
                    ApplyColor(_colorTo);
                }
                else
                {
                    active = true;
                }
            }

            if (!double.IsNaN(_arrowStart))
            {
                var t = Progress(nowSeconds, _arrowStart, MotionSpecs.OffTargetArrowMs);
                var shift = _reduced ? 0f : (float)MotionSpecs.OffTargetArrowShiftDp;
                if (_arrowShown)
                {
                    var e = (float)Easing.Evaluate(Tokens.EaseEnter, t);
                    // Entra desde 4 dp en el sentido contrario al que indica.
                    ApplyArrow(_arrowFromOpacity + (1f - _arrowFromOpacity) * e, -_arrowDirection * shift * (1f - e));
                }
                else
                {
                    var e = (float)Easing.Evaluate(Tokens.EaseExit, t);
                    // Sale siguiendo 4 dp en su sentido.
                    ApplyArrow(_arrowFromOpacity * (1f - e), _arrowDirection * shift * e);
                }

                if (t >= 1)
                {
                    _arrowStart = double.NaN;
                    ApplyArrow(_arrowShown ? 1f : 0f, 0f);
                }
                else
                {
                    active = true;
                }
            }

            if (!double.IsNaN(_bumpStart))
            {
                var elapsed = (nowSeconds - _bumpStart) * 1000.0;
                double s;
                if (elapsed <= MotionSpecs.OffTargetBumpPeakMs)
                {
                    s = 1 + (MotionSpecs.OffTargetBumpScale - 1) * Easing.Evaluate(Tokens.EaseEnter, Clamp01(elapsed / MotionSpecs.OffTargetBumpPeakMs));
                }
                else
                {
                    var u = Clamp01((elapsed - MotionSpecs.OffTargetBumpPeakMs) / (MotionSpecs.OffTargetBumpEndMs - MotionSpecs.OffTargetBumpPeakMs));
                    s = MotionSpecs.OffTargetBumpScale - (MotionSpecs.OffTargetBumpScale - 1) * Easing.Evaluate(Tokens.EaseMove, u);
                }

                if (elapsed >= MotionSpecs.OffTargetBumpEndMs)
                {
                    _bumpStart = double.NaN;
                    _value.style.scale = StyleKeyword.Null;
                }
                else
                {
                    _value.style.scale = new Scale(new Vector3((float)s, (float)s, 1f));
                    active = true;
                }
            }

            if (!active)
            {
                _running = false;
            }

            return active;
        }

        private void Start()
        {
            if (_running)
            {
                return;
            }

            _running = true;
            _driver.Add(this);
        }

        private void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _driver.Remove(this);
        }

        private void ApplyColor(Color color)
        {
            _value.style.color = color;
        }

        private void ApplyArrow(float opacity, float translateY)
        {
            _arrow.style.opacity = opacity;
            _arrow.style.display = opacity > 0.001f || _arrowShown ? DisplayStyle.Flex : DisplayStyle.None;
            _arrow.style.translate = Mathf.Abs(translateY) < 1e-4f ? new Translate(0, 0) : new Translate(0, translateY);
        }

        private static double Progress(double now, double start, double durationMs)
        {
            if (double.IsPositiveInfinity(now))
            {
                return 1;
            }

            return Clamp01((now - start) * 1000.0 / durationMs);
        }

        private static double Clamp01(double t)
        {
            return t < 0 ? 0 : t > 1 ? 1 : t;
        }
    }
}
