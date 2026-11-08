using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Coreografías del Bluetooth real (It. 10a; 00-arte-bluetooth §1-§5; BleMotionSpecs). Todas con el reloj de MotionDriver (capturas
    // deterministas), sin asignar por cuadro, y con su variante de reducir movimiento (A10-12). Las piezas:
    // - Fade: opacidad de un elemento de "from" a "to" en un tramo (las otras tarjetas a 0,38, el cruce de los pasos, el check reducido).
    // - Grow: lo nuevo crece con la regla del empuje (alto 0 → natural en 0-160 con ease.move; opacidad 0 → 1 e y −8 → 0 en 60-260 con
    //   ease.enter), con un retardo para el escalonado de 35 ms. Lo usan las tarjetas nuevas y las cajas del enlace y del error.
    // - Wake: "se despierta" (apagado o sin permiso → encendido): la ilustración en 0-360, la vista de estado sale en 360-480 y la
    //   búsqueda entra en 480-720.
    public static class BleMotion
    {
        // ¿Se ve? (ningún ancestro con display: none; la pestaña oculta y las vistas superpuestas de Dispositivo usan display). Sin
        // asignar: recorre la jerarquía.
        public static bool IsDisplayed(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return element.panel != null;
        }

        // Opacidad de from a to en [startMs; endMs] con la curva dada. clearAtEnd: al terminar, la opacidad en línea vuelve a null (la
        // pone el USS, p. ej. :disabled a 0,38).
        public sealed class Fade : TimedMotion
        {
            private VisualElement? _target;
            private float _from;
            private float _to;
            private double _start;
            private double _end;
            private EasingToken _curve = Tokens.EaseMove;
            private bool _clear;

            public Fade(MotionDriver driver)
                : base(driver)
            {
            }

            protected override double TotalMs => _end;

            public void Play(VisualElement target, float from, float to, double startMs, double endMs, EasingToken curve, bool clearAtEnd)
            {
                Finish();
                _target = target;
                _from = from;
                _to = to;
                _start = startMs;
                _end = endMs;
                _curve = curve;
                _clear = clearAtEnd;
                if (!Driver.Armed)
                {
                    target.style.opacity = clearAtEnd ? StyleKeyword.Null : new StyleFloat(to);
                    _target = null;
                    return;
                }

                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_target == null)
                {
                    return;
                }

                var e = Easing.Evaluate(_curve, BleMotionSpecs.Segment(elapsedMs, _start, _end));
                _target.style.opacity = (float)(_from + (_to - _from) * e);
            }

            protected override void OnFinished()
            {
                if (_target != null && _clear)
                {
                    _target.style.opacity = StyleKeyword.Null;
                }

                _target = null;
            }
        }

        // Lo nuevo crece (A10-04; §1.3 y §4.1): el alto va de 0 a heightDp (+ el margen de abajo marginDp) en 0-160 (ease.move), y la
        // opacidad de 0 a 1 y la y de −dropDp a 0 en 60-260 (ease.enter), después de delayMs. Al final todo vuelve a null (el alto
        // natural). Reducir movimiento: fundido de 160 sin cambio de alto.
        public sealed class Grow : TimedMotion
        {
            private readonly VisualElement _target;
            private double _delay;
            private double _height;
            private double _margin;
            private double _drop;
            private bool _reduced;

            public Grow(MotionDriver driver, VisualElement target)
                : base(driver)
            {
                _target = target ?? throw new ArgumentNullException(nameof(target));
            }

            protected override double TotalMs => _delay + (_reduced ? Tokens.DurFast.Milliseconds : BleMotionSpecs.CardFadeEndMs);

            public void Play(double heightDp, double marginDp, double dropDp, double delayMs)
            {
                Finish();
                if (!Driver.Armed)
                {
                    return;
                }

                _height = heightDp;
                _margin = marginDp;
                _drop = dropDp;
                _delay = delayMs;
                _reduced = Driver.Settings.ReduceMotion;
                if (!_reduced)
                {
                    _target.style.overflow = Overflow.Hidden;
                    _target.style.minHeight = 0;
                }

                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                var t = elapsedMs - _delay;
                if (_reduced)
                {
                    _target.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(t, 0, Tokens.DurFast.Milliseconds));
                    return;
                }

                var h = Easing.Evaluate(Tokens.EaseMove, BleMotionSpecs.Segment(t, 0, BleMotionSpecs.CardGrowEndMs));
                var e = Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(t, BleMotionSpecs.CardFadeStartMs, BleMotionSpecs.CardFadeEndMs));
                _target.style.height = (float)(_height * h);
                _target.style.marginBottom = (float)(_margin * h);
                _target.style.opacity = (float)e;
                _target.style.translate = new Translate(0, (float)(-_drop * (1 - e)));
            }

            protected override void OnFinished()
            {
                _target.style.height = StyleKeyword.Null;
                _target.style.minHeight = StyleKeyword.Null;
                _target.style.marginBottom = StyleKeyword.Null;
                _target.style.overflow = StyleKeyword.Null;
                _target.style.opacity = StyleKeyword.Null;
                _target.style.translate = StyleKeyword.Null;
            }
        }

        // "Se despierta" (§3.2; A10-07): de la vista de estado a la búsqueda. onSwap se llama al cambiar (480 ms; reducido, 280): la
        // tarjeta del pulsómetro pasa a su slot. Si llega otro cambio en el medio, Finish salta al final (regla de la It. 2).
        public sealed class Wake : TimedMotion
        {
            private VisualElement? _state;
            private VisualElement? _scan;
            private Device.BluetoothIllustration? _art;
            private Action? _onSwap;
            private bool _swapped;
            private bool _reduced;

            public Wake(MotionDriver driver)
                : base(driver)
            {
            }

            protected override double TotalMs => _reduced ? BleMotionSpecs.ReducedWakeInEndMs : BleMotionSpecs.WakeInEndMs;

            private double OutStart => _reduced ? BleMotionSpecs.ReducedWakeGlyphMs : BleMotionSpecs.WakeOutStartMs;

            private double OutEnd => _reduced ? BleMotionSpecs.ReducedWakeOutEndMs : BleMotionSpecs.WakeOutEndMs;

            public bool IsBlocking(VisualElement view)
            {
                return IsRunning && !_swapped && ReferenceEquals(view, _state);
            }

            public void Play(VisualElement state, VisualElement scan, Device.BluetoothIllustration art, Action onSwap)
            {
                Finish();
                _state = state;
                _scan = scan;
                _art = art;
                _onSwap = onSwap;
                _swapped = false;
                if (!Driver.Armed)
                {
                    Swap();
                    Clear();
                    return;
                }

                _reduced = Driver.Settings.ReduceMotion;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_state == null || _scan == null || _art == null)
                {
                    return;
                }

                if (elapsedMs < OutEnd)
                {
                    _art.ApplyWake(elapsedMs, _reduced);
                    var e = Easing.Evaluate(Tokens.EaseExit, BleMotionSpecs.Segment(elapsedMs, OutStart, OutEnd));
                    _state.style.opacity = (float)(1 - e);
                    return;
                }

                if (!_swapped)
                {
                    Swap();
                }

                var i = Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(elapsedMs, OutEnd, TotalMs));
                _scan.style.opacity = (float)i;
                _scan.style.translate = _reduced ? new Translate(0, 0) : new Translate(0, (float)(Tokens.ShiftS.Value * (1 - i)));
            }

            protected override void OnFinished()
            {
                if (!_swapped)
                {
                    Swap();
                }

                Clear();
            }

            private void Swap()
            {
                _swapped = true;
                if (_state != null)
                {
                    _state.style.display = DisplayStyle.None;
                    _state.style.opacity = StyleKeyword.Null;
                }

                _art?.ResetWake();
                if (_scan != null)
                {
                    _scan.style.display = DisplayStyle.Flex;
                    if (IsRunning)
                    {
                        _scan.style.opacity = 0;
                    }
                }

                _onSwap?.Invoke();
            }

            private void Clear()
            {
                if (_scan != null)
                {
                    _scan.style.opacity = StyleKeyword.Null;
                    _scan.style.translate = StyleKeyword.Null;
                }

                _state = null;
                _scan = null;
                _art = null;
                _onSwap = null;
            }
        }

        // La caja de estado entra con el disco 0,94 → 1 (Búsqueda → apagado, §3.2), en el tramo de entrada de ViewSwap (120-360).
        public sealed class DiscIn : TimedMotion
        {
            private VisualElement? _disc;

            public DiscIn(MotionDriver driver)
                : base(driver)
            {
            }

            protected override double TotalMs => MotionSpecs.ViewInEndMs - MotionSpecs.ViewOutMs;

            public void Play(VisualElement disc)
            {
                Finish();
                if (!Driver.Armed || Driver.Settings.ReduceMotion)
                {
                    return;
                }

                _disc = disc;
                disc.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(50, LengthUnit.Percent));
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_disc == null)
                {
                    return;
                }

                var e = Easing.Evaluate(Tokens.EaseEnter, BleMotionSpecs.Segment(elapsedMs, 0, TotalMs));
                var s = (float)(BleMotionSpecs.StateInDiscScaleFrom + (1 - BleMotionSpecs.StateInDiscScaleFrom) * e);
                _disc.style.scale = new Scale(new Vector2(s, s));
            }

            protected override void OnFinished()
            {
                if (_disc != null)
                {
                    _disc.style.scale = StyleKeyword.Null;
                }

                _disc = null;
            }
        }
    }
}
