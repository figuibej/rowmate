using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Cambio entre la vista conectada y la de búsqueda de Dispositivo (DN-12; 04-design-notes §5.5; VID-03).
    // - 0-120 ms: la saliente se apaga (opacidad 1 → 0, ease.exit; al "apagar" la conectada, escala 1 → 0,985 desde arriba al
    //   centro). Mientras sale, un bloqueador TrickleDown en su raíz descarta los eventos (IsBlocking).
    // - 120 ms: la saliente pasa a display: none, se llama a onSwap (la tarjeta del pulsómetro cambia de slot, el
    //   desplazamiento de la conectada vuelve a 0) y la entrante pasa a flex.
    // - 120-360 ms: la entrante entra (búsqueda: opacidad 0 → 1 e y +8 → 0, ease.enter; conectada: la cabecera y las tarjetas
    //   las anima quien llama, desde onSwap).
    // Si el estado cambia en medio, la transición actual salta a su final. El estado inicial se aplica sin animación.
    // Reducir movimiento: solo opacidad, 0-120 y 120-240.
    public sealed class ViewSwap : TimedMotion
    {
        private VisualElement? _outgoing;
        private VisualElement? _incoming;
        private Action? _onSwap;
        private bool _scaleOutgoing;
        private bool _fadeIncoming;
        private bool _swapped;
        private bool _reduced;

        public ViewSwap(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => _reduced ? MotionSpecs.ViewOutMs + (MotionSpecs.ViewInEndMs - MotionSpecs.ViewOutMs) / 2 : MotionSpecs.ViewInEndMs;

        public VisualElement? Outgoing => IsRunning && !_swapped ? _outgoing : null;

        public bool IsBlocking(VisualElement view)
        {
            return IsRunning && !_swapped && ReferenceEquals(view, _outgoing);
        }

        // scaleOutgoing: la saliente se "apaga" con escala (la conectada). fadeIncoming: la entrante entra con opacidad e y
        // (la de búsqueda); si es false, la anima quien llama en onSwap (la conectada "se enciende" por partes).
        public void Play(VisualElement outgoing, VisualElement incoming, bool scaleOutgoing, bool fadeIncoming, Action onSwap)
        {
            Finish();
            _outgoing = outgoing;
            _incoming = incoming;
            _onSwap = onSwap;
            _scaleOutgoing = scaleOutgoing;
            _fadeIncoming = fadeIncoming;
            _swapped = false;
            if (!Driver.Armed)
            {
                Swap();
                Clear();
                return;
            }

            _reduced = Driver.Settings.ReduceMotion;
            outgoing.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(0));
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            if (_outgoing == null || _incoming == null)
            {
                return;
            }

            if (elapsedMs < MotionSpecs.ViewOutMs)
            {
                var e = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.ViewOutMs));
                _outgoing.style.opacity = (float)(1 - e);
                if (_scaleOutgoing && !_reduced)
                {
                    var s = (float)(1 - (1 - MotionSpecs.ViewOutScale) * e);
                    _outgoing.style.scale = new Scale(new UnityEngine.Vector2(s, s));
                }

                return;
            }

            if (!_swapped)
            {
                Swap();
            }

            if (_fadeIncoming)
            {
                var end = _reduced ? TotalMs : MotionSpecs.ViewInEndMs;
                var e = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, MotionSpecs.ViewOutMs, end));
                _incoming.style.opacity = (float)e;
                _incoming.style.translate = _reduced ? new Translate(0, 0) : new Translate(0, (float)(Tokens.ShiftS.Value * (1 - e)));
            }
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
            if (_outgoing != null)
            {
                _outgoing.style.display = DisplayStyle.None;
                _outgoing.style.opacity = StyleKeyword.Null;
                _outgoing.style.scale = StyleKeyword.Null;
            }

            if (_incoming != null)
            {
                _incoming.style.display = DisplayStyle.Flex;
                if (_fadeIncoming && IsRunning)
                {
                    _incoming.style.opacity = 0;
                }
            }

            _onSwap?.Invoke();
        }

        private void Clear()
        {
            if (_incoming != null)
            {
                _incoming.style.opacity = StyleKeyword.Null;
                _incoming.style.translate = StyleKeyword.Null;
            }

            if (_outgoing != null)
            {
                _outgoing.style.transformOrigin = StyleKeyword.Null;
            }

            _outgoing = null;
            _incoming = null;
            _onSwap = null;
        }
    }
}
