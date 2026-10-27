using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Alto del cuerpo de la tarjeta del pulsómetro al cambiar de estado (spec §10.4, 04-design-notes §5.6, DN-13): el cuerpo
    // anima su alto del valor anterior al nuevo en dur.slow (360) con ease.move, con overflow: hidden SOLO mientras dura, y
    // el contenido nuevo entra con opacidad 0 → 1 en dur.fast (160) con ease.enter. Al terminar, alto automático.
    // Mientras anima, el contenido va en posición absoluta (arriba, a todo el ancho) para medir su alto natural sin depender
    // del cuerpo. Reducir movimiento: sin animación (no se usa).
    public sealed class HeightTween : TimedMotion
    {
        private readonly VisualElement _body;
        private readonly VisualElement _content;
        private float _from;
        private float _to;
        private bool _measured;

        public HeightTween(MotionDriver driver, VisualElement body, VisualElement content)
            : base(driver)
        {
            _body = body;
            _content = content;
            _content.RegisterCallback<GeometryChangedEvent>(OnContentGeometry);
        }

        protected override double TotalMs => Tokens.DurSlow.Milliseconds;

        // Llamar ANTES de cambiar el contenido, con el alto que tenía el cuerpo.
        public void Start(float fromHeight)
        {
            if (IsRunning)
            {
                // Arranca desde donde está (el alto en línea actual).
                var current = _body.style.height.value.value;
                fromHeight = current > 0 ? current : fromHeight;
            }

            _from = fromHeight;
            _to = fromHeight;
            _measured = false;
            _body.style.height = fromHeight;
            _body.style.overflow = Overflow.Hidden;
            _content.style.position = Position.Absolute;
            _content.style.left = 0;
            _content.style.right = 0;
            _content.style.top = 0;
            _content.style.opacity = 0;
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            var u = Progress(elapsedMs, 0, TotalMs);
            var target = _measured ? _to : _from;
            _body.style.height = (float)(_from + (target - _from) * Easing.Evaluate(Tokens.EaseMove, u));
            var fade = Progress(elapsedMs, 0, Tokens.DurFast.Milliseconds);
            _content.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, fade);
        }

        protected override void OnFinished()
        {
            _body.style.height = StyleKeyword.Null;
            _body.style.overflow = StyleKeyword.Null;
            _content.style.position = StyleKeyword.Null;
            _content.style.left = StyleKeyword.Null;
            _content.style.right = StyleKeyword.Null;
            _content.style.top = StyleKeyword.Null;
            _content.style.opacity = StyleKeyword.Null;
        }

        private void OnContentGeometry(GeometryChangedEvent evt)
        {
            if (!IsRunning)
            {
                return;
            }

            _to = evt.newRect.height;
            _measured = true;
        }
    }
}
