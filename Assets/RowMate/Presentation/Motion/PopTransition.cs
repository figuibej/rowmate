using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Entrada de la llave y del panel del simulador (04-design-notes §5.7, DN-14): opacidad 0 → 1 en opacityMs y escala
    // scaleFrom → 1 en scaleMs, con ease.enter y origen abajo a la derecha (crece desde la llave). La salida es en el acto
    // (la hace quien la usa con display: none). Reducir movimiento: solo opacidad (reducedOpacityMs).
    public sealed class PopTransition : TimedMotion
    {
        private readonly VisualElement _target;
        private readonly double _scaleFrom;
        private readonly double _opacityMs;
        private readonly double _scaleMs;
        private readonly double _reducedOpacityMs;
        private bool _reduced;

        public PopTransition(MotionDriver driver, VisualElement target, double scaleFrom, double opacityMs, double scaleMs, double reducedOpacityMs)
            : base(driver)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _scaleFrom = scaleFrom;
            _opacityMs = opacityMs;
            _scaleMs = scaleMs;
            _reducedOpacityMs = reducedOpacityMs;
        }

        protected override double TotalMs => _reduced ? _reducedOpacityMs : Math.Max(_opacityMs, _scaleMs);

        public void Play()
        {
            if (!Driver.Armed)
            {
                Clear();
                return;
            }

            _reduced = Driver.Settings.ReduceMotion;
            _target.style.transformOrigin = new TransformOrigin(new Length(100, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
            Begin();
        }

        protected override void OnUpdate(double elapsedMs)
        {
            var opacityMs = _reduced ? _reducedOpacityMs : _opacityMs;
            _target.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, opacityMs));
            if (_reduced)
            {
                _target.style.scale = StyleKeyword.Null;
                return;
            }

            var s = _scaleFrom + (1 - _scaleFrom) * Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, _scaleMs));
            _target.style.scale = new Scale(new UnityEngine.Vector2((float)s, (float)s));
        }

        protected override void OnFinished()
        {
            Clear();
        }

        private void Clear()
        {
            _target.style.opacity = StyleKeyword.Null;
            _target.style.scale = StyleKeyword.Null;
            _target.style.transformOrigin = StyleKeyword.Null;
        }
    }
}
