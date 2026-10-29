using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Lo que se va fundiéndose en su lugar sin ocupar lugar (el vacío del editor con el primer paso: funde → 0 en 120 con
    // ease.exit; 03-rutinas/04-design-notes §5.6; QA-V3-02): sale del flujo (position absolute en su misma caja) para que lo
    // nuevo tome su lugar desde el cuadro 0, y al terminar queda en display none, sin nada en línea. Sale desde la opacidad y el
    // desplazamiento que tiene: si estaba entrando, no pasa antes por 1 ni por y 0 (DD4a-05 a; 04a/00-deuda-diseno §2.3).
    // Reducir movimiento: igual (es un fundido, §5.10). Sin armar, fuera del panel o sin caja todavía: en el acto. No toca
    // pickingMode (el vacío ya es Ignore).
    public sealed class ExitMotion : TimedMotion
    {
        private VisualElement? _target;
        private double _duration;
        private double _fromOpacity = 1;

        public ExitMotion(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => _duration;

        // fromOpacity y shiftY: lo que se ve ahora (1 y 0 en reposo).
        public void Play(VisualElement target, double durationMs, double fromOpacity = 1, float shiftY = 0)
        {
            Finish();
            _target = target;
            _duration = durationMs;
            _fromOpacity = fromOpacity < 0 ? 0 : fromOpacity > 1 ? 1 : fromOpacity;
            var box = target.layout;
            if (!Driver.Armed || target.panel == null || target.resolvedStyle.display == DisplayStyle.None || !(box.height > 0) || !(box.width > 0))
            {
                OnFinished();
                return;
            }

            var style = target.resolvedStyle;
            target.style.position = Position.Absolute;
            target.style.left = box.x - style.marginLeft;
            target.style.top = box.y - style.marginTop;
            target.style.width = box.width;
            target.style.height = box.height;
            if (shiftY != 0)
            {
                target.style.translate = new Translate(0, shiftY);
            }

            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            if (_target == null)
            {
                return;
            }

            var e = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, TotalMs));
            _target.style.opacity = (float)(_fromOpacity * (1 - e));
        }

        protected override void OnFinished()
        {
            var target = _target;
            _target = null;
            if (target == null)
            {
                return;
            }

            target.style.display = DisplayStyle.None;
            target.style.position = StyleKeyword.Null;
            target.style.left = StyleKeyword.Null;
            target.style.top = StyleKeyword.Null;
            target.style.width = StyleKeyword.Null;
            target.style.height = StyleKeyword.Null;
            target.style.translate = StyleKeyword.Null;
            target.style.opacity = StyleKeyword.Null;
        }
    }
}
