using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // "Lo nuevo crece" (03-rutinas/04-design-notes §5.5, §5.6; MotionSpecs.Grow*): una tarjeta, una fila o un bloque de serie
    // nuevos van dentro de un slot (.rm-slot) cuyo alto pasa de 0 al natural con un resorte crítico ω 12 (spring.smooth);
    // como los márgenes del contenido están DENTRO del slot, el alto y los márgenes crecen juntos y al final no salta nada.
    // El contenido funde 0 → 1 en 60-260 (ease.enter); un bloque de serie funde sus piezas en escalera (Stagger). Mientras
    // crece, overflow: hidden solo en el slot y el contenido en position: absolute arriba (left/right 0): así Yoga le da su alto
    // natural (un hijo en flujo de un slot de alto 0 se mide "a lo sumo 0" y quedaba en 0) y el slot lo recorta. Reducir
    // movimiento: fundido de 160 sin cambio de alto.
    // Hasta que el contenido tiene su alto (el layout del cuadro siguiente) la línea de tiempo no avanza: el crecimiento y el
    // fundido empiezan juntos cuando hay a qué crecer (ILayoutBoundMotion; sin esto, un Settle de prueba gastaba reloj con el
    // slot en 0).
    public sealed class GrowMotion : IMotion, ILayoutBoundMotion
    {
        private readonly MotionDriver _driver;
        private readonly VisualElement _slot;
        private readonly VisualElement _content;
        private readonly List<(VisualElement Element, double Start, double End)> _fades = new List<(VisualElement, double, double)>();
        private CriticalSpring _spring;
        private double _start;
        private double _last;
        private double _target;
        private bool _measured;
        private bool _running;
        private bool _reduced;
        private Action? _onDone;

        public GrowMotion(MotionDriver driver, VisualElement slot, VisualElement content)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _content.RegisterCallback<GeometryChangedEvent>(OnContentGeometry);
        }

        public bool IsFinite => true;

        public bool IsRunning => _running;

        // Esperando el primer alto del contenido (solo al crecer; con reducir movimiento no hay alto que medir).
        public bool IsWaitingForLayout => _running && !_reduced && !_measured && _content.panel != null;

        // Piezas que funden en escalera en lugar del contenido entero (la serie nueva: encabezado 0-200, fila 1 40-240…).
        public void Stagger(VisualElement element, double startMs, double endMs)
        {
            _fades.Add((element, startMs, endMs));
        }

        public void Play(Action? onDone = null)
        {
            _onDone = onDone;
            if (!_driver.Armed)
            {
                Clear();
                Done();
                return;
            }

            _reduced = _driver.Settings.ReduceMotion;
            _start = _driver.Now;
            _last = _start;
            _running = true;
            if (_fades.Count == 0)
            {
                _fades.Add((_content, MotionSpecs.GrowFadeStartMs, MotionSpecs.GrowFadeEndMs));
            }

            for (var i = 0; i < _fades.Count; i++)
            {
                _fades[i].Element.style.opacity = 0;
            }

            if (!_reduced)
            {
                _spring.SnapTo(0);
                _measured = false;
                _slot.style.height = 0;
                _slot.style.overflow = Overflow.Hidden;
                _content.style.flexShrink = 0;
                _content.style.position = Position.Absolute;
                _content.style.left = 0;
                _content.style.right = 0;
                _content.style.top = 0;
                Measure();
            }

            _driver.Add(this);
        }

        public void Finish()
        {
            if (!_running)
            {
                return;
            }

            _driver.Remove(this);
            Clear();
            Done();
        }

        public bool Tick(double nowSeconds)
        {
            if (!_running)
            {
                return false;
            }

            if (_content.panel == null)
            {
                // Se sacó del árbol (la lista se rearmó): termina sin esperar un alto que no va a llegar.
                _running = false;
                Clear();
                Done();
                return false;
            }

            if (!_reduced && !_measured)
            {
                Measure();
                if (!_measured)
                {
                    // Sin alto todavía: la línea de tiempo espera (el slot sigue en 0 y el contenido transparente).
                    _start = nowSeconds;
                    _last = nowSeconds;
                    return true;
                }
            }

            var elapsed = (nowSeconds - _start) * 1000.0;
            var dt = nowSeconds - _last;
            _last = nowSeconds;
            var fadesDone = true;
            for (var i = 0; i < _fades.Count; i++)
            {
                var fade = _fades[i];
                double u;
                if (_reduced)
                {
                    u = Easing.Evaluate(Tokens.EaseLinear, Progress(elapsed, 0, Tokens.DurFast.Milliseconds));
                }
                else
                {
                    u = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsed, fade.Start, fade.End));
                }

                fade.Element.style.opacity = (float)u;
                fadesDone &= u >= 1;
            }

            var heightDone = true;
            if (!_reduced)
            {
                if (_measured)
                {
                    _spring.Step(dt, _target, MotionSpecs.GrowOmega);
                    heightDone = _spring.IsSettled(_target, 0.25);
                    _slot.style.height = (float)(heightDone ? _target : Math.Max(0, _spring.Position));
                }
                else
                {
                    heightDone = false;
                    Measure();
                }
            }

            if (heightDone && fadesDone)
            {
                _running = false;
                Clear();
                Done();
                return false;
            }

            return true;
        }

        private void OnContentGeometry(GeometryChangedEvent evt)
        {
            if (_running && !_reduced)
            {
                Measure();
            }
        }

        private void Measure()
        {
            var height = _content.layout.height;
            if (!(height > 0))
            {
                return;
            }

            var style = _content.resolvedStyle;
            _target = height + style.marginTop + style.marginBottom;
            _measured = true;
        }

        private void Clear()
        {
            _running = false;
            _slot.style.height = StyleKeyword.Null;
            _slot.style.overflow = StyleKeyword.Null;
            _content.style.flexShrink = StyleKeyword.Null;
            _content.style.position = StyleKeyword.Null;
            _content.style.left = StyleKeyword.Null;
            _content.style.right = StyleKeyword.Null;
            _content.style.top = StyleKeyword.Null;
            for (var i = 0; i < _fades.Count; i++)
            {
                _fades[i].Element.style.opacity = StyleKeyword.Null;
            }

            _content.style.opacity = StyleKeyword.Null;
        }

        private void Done()
        {
            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        private static double Progress(double elapsedMs, double startMs, double endMs)
        {
            if (endMs <= startMs)
            {
                return elapsedMs >= endMs ? 1 : 0;
            }

            var u = (elapsedMs - startMs) / (endMs - startMs);
            return u <= 0 ? 0 : u >= 1 ? 1 : u;
        }
    }

    // "Lo cambiado se ilumina" (04-design-notes §5.5, §5.6): la capa .rm-highlight (blanca, sin eventos) va de 0,08 a 0 en
    // 600 ms (ease.move). Igual con reducir movimiento (es opacidad).
    public sealed class HighlightMotion : TimedMotion
    {
        private readonly VisualElement _layer;

        public HighlightMotion(MotionDriver driver, VisualElement layer)
            : base(driver)
        {
            _layer = layer ?? throw new ArgumentNullException(nameof(layer));
        }

        protected override double TotalMs => MotionSpecs.HighlightMs;

        public void Play()
        {
            if (!Driver.Armed)
            {
                _layer.style.opacity = 0;
                return;
            }

            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            var u = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, TotalMs));
            _layer.style.opacity = (float)(MotionSpecs.HighlightPeak * (1 - u));
        }

        protected override void OnFinished()
        {
            _layer.style.opacity = 0;
        }
    }

    // Entrada con fundido y desplazamiento después de una espera (la primera carga de la lista con la pestaña a la vista:
    // tarjeta i en 35·i → +240, opacidad 0 → 1 e y +12 → 0; el vacío y el error en 240 con y +8 → 0; 04-design-notes §5.5).
    // Reducir movimiento: todo funde en 160 junto, sin desplazamiento ni escalonado. Una espera negativa arranca como si hubiera
    // empezado hace ese tiempo (el vacío que entra en el instante exacto en que terminó un colapso: TimedMotion.OverrunMs).
    public sealed class EnterMotion : TimedMotion
    {
        private VisualElement? _target;
        private double _delay;
        private double _duration;
        private double _shift;
        private bool _reduced;

        public EnterMotion(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => _delay + _duration;

        public void Play(VisualElement target, double delayMs, double durationMs, double shiftDp)
        {
            Finish();
            _target = target;
            _reduced = Driver.Settings.ReduceMotion;
            _delay = _reduced ? 0 : delayMs;
            _duration = _reduced ? Tokens.DurFast.Milliseconds : durationMs;
            _shift = _reduced ? 0 : shiftDp;
            if (!Driver.Armed)
            {
                OnFinished();
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

            var e = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, _delay, _delay + _duration));
            _target.style.opacity = (float)e;
            _target.style.translate = _shift == 0 ? new Translate(0, 0) : new Translate(0, (float)(_shift * (1 - e)));
        }

        protected override void OnFinished()
        {
            if (_target != null)
            {
                _target.style.opacity = StyleKeyword.Null;
                _target.style.translate = StyleKeyword.Null;
            }

            _target = null;
        }
    }
}
