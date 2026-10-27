using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Contenido de la pestaña al cambiar de destino (04-design-notes §5.4, DN-11, VID-02): la saliente va de opacidad 1 a 0 y
    // de x 0 a −12·dir en 0-120 ms (ease.exit) y pasa a display: none; la entrante, display: flex desde el cuadro 0, va de
    // opacidad 0 a 1 y de x +12·dir a 0 en 80-280 ms (ease.enter). dir = +1 si el destino está a la derecha. Si se
    // reinterrumpe, la actual salta a su final. Reducir movimiento: los mismos tiempos, solo opacidad.
    // La píldora, el ícono y los colores de la barra los mueve TabBarView.
    public sealed class TabTransition : TimedMotion
    {
        private VisualElement? _outgoing;
        private VisualElement? _incoming;
        private int _direction;
        private bool _reduced;

        public TabTransition(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => MotionSpecs.TabInEndMs;

        public void Play(VisualElement? outgoing, VisualElement incoming, int direction)
        {
            Finish();
            _outgoing = outgoing;
            _incoming = incoming;
            _direction = direction >= 0 ? 1 : -1;
            incoming.style.display = DisplayStyle.Flex;
            if (!Driver.Armed || outgoing == null || ReferenceEquals(outgoing, incoming))
            {
                OnFinished();
                return;
            }

            _reduced = Driver.Settings.ReduceMotion;
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            var shift = Tokens.ShiftM.Value;
            if (_outgoing != null)
            {
                var u = Progress(elapsedMs, 0, MotionSpecs.TabOutMs);
                var e = Easing.Evaluate(Tokens.EaseExit, u);
                _outgoing.style.opacity = (float)(1 - e);
                _outgoing.style.translate = _reduced ? new Translate(0, 0) : new Translate((float)(-_direction * shift * e), 0);
                if (u >= 1)
                {
                    _outgoing.style.display = DisplayStyle.None;
                }
            }

            if (_incoming != null)
            {
                var u = Progress(elapsedMs, MotionSpecs.TabInStartMs, MotionSpecs.TabInEndMs);
                var e = Easing.Evaluate(Tokens.EaseEnter, u);
                _incoming.style.opacity = (float)e;
                _incoming.style.translate = _reduced ? new Translate(0, 0) : new Translate((float)(_direction * shift * (1 - e)), 0);
            }
        }

        protected override void OnFinished()
        {
            if (_outgoing != null && !ReferenceEquals(_outgoing, _incoming))
            {
                _outgoing.style.display = DisplayStyle.None;
                _outgoing.style.opacity = StyleKeyword.Null;
                _outgoing.style.translate = StyleKeyword.Null;
            }

            if (_incoming != null)
            {
                _incoming.style.display = DisplayStyle.Flex;
                _incoming.style.opacity = StyleKeyword.Null;
                _incoming.style.translate = StyleKeyword.Null;
            }

            _outgoing = null;
            _incoming = null;
        }
    }
}
