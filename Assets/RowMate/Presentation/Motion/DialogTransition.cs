using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Entrada y salida de un diálogo con su velo (03-rutinas/04-design-notes §5.2; MotionSpecs.Dialog*):
    // - Entrada: velo 0 → 0,6 en 0-200 (ease.move); diálogo opacidad 0 → 1 y escala 0,96 → 1 (origen al centro) en 0-220
    //   (ease.enter).
    // - Salida: diálogo opacidad → 0 y escala → 0,98 en 0-150 (ease.exit); velo → 0 en 0-150 (ease.exit). El resultado ya se
    //   aplicó en el cuadro 0 (lo hace quien cierra).
    // - Reducir movimiento: velo igual; diálogo solo opacidad (220 / 150).
    // También la usan el menú y la ayuda (escala 0,96 → 1 en 120, salida 90; con el origen en la esquina del ancla).
    public sealed class DialogTransition : TimedMotion
    {
        private VisualElement? _scrim;
        private VisualElement? _box;
        private Action? _onDone;
        private bool _entering;
        private bool _reduced;
        private double _boxInMs;
        private double _boxOutMs;
        private double _scrimInMs;
        private double _scaleFrom;
        private double _scaleTo;
        private double _scrimOpacity;
        private double _fromOpacity;
        private double _fromScale;
        private double _fromScrim;

        public DialogTransition(MotionDriver driver)
            : base(driver)
        {
        }

        protected override double TotalMs => _entering ? Math.Max(_boxInMs, _scrim != null ? _scrimInMs : 0) : _boxOutMs;

        public bool IsEntering => IsRunning && _entering;

        // Diálogo (velo y caja).
        public void PlayIn(VisualElement? scrim, VisualElement box, Action? onDone = null)
        {
            Configure(scrim, box, MotionSpecs.DialogInMs, MotionSpecs.DialogOutMs, MotionSpecs.DialogScrimInMs, MotionSpecs.DialogInScaleFrom,
                MotionSpecs.DialogOutScaleTo, MotionSpecs.DialogScrimOpacity);
            StartIn(onDone);
        }

        public void PlayOut(VisualElement? scrim, VisualElement box, Action onDone)
        {
            Configure(scrim, box, MotionSpecs.DialogInMs, MotionSpecs.DialogOutMs, MotionSpecs.DialogScrimInMs, MotionSpecs.DialogInScaleFrom,
                MotionSpecs.DialogOutScaleTo, MotionSpecs.DialogScrimOpacity);
            StartOut(onDone);
        }

        // Menú o ayuda (sin velo): entra en inMs con escala scaleFrom (1 = solo opacidad) y sale en outMs.
        public void PlayPopupIn(VisualElement box, double inMs, double scaleFrom)
        {
            Configure(null, box, inMs, inMs, 0, scaleFrom, 1, 0);
            StartIn(null);
        }

        public void PlayPopupOut(VisualElement box, double outMs, Action onDone)
        {
            Configure(null, box, outMs, outMs, 0, 1, 1, 0);
            StartOut(onDone);
        }

        protected override void OnUpdate(double elapsedMs)
        {
            Apply(elapsedMs);
        }

        protected override void OnFinished()
        {
            if (_box != null && _entering)
            {
                _box.style.opacity = StyleKeyword.Null;
                _box.style.scale = StyleKeyword.Null;
            }

            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        private void Configure(VisualElement? scrim, VisualElement box, double boxInMs, double boxOutMs, double scrimInMs, double scaleFrom,
            double scaleTo, double scrimOpacity)
        {
            // Desde los valores actuales: cerrar a mitad de la entrada no salta.
            var current = ReferenceEquals(_box, box) && IsRunning;
            _fromOpacity = current ? box.resolvedStyle.opacity : (_entering ? 1 : 0);
            _fromScale = current ? box.resolvedStyle.scale.value.x : 1;
            _fromScrim = current && scrim != null ? scrim.resolvedStyle.opacity : scrimOpacity;
            if (IsRunning)
            {
                _onDone = null;
                Finish();
            }

            _scrim = scrim;
            _box = box;
            _boxInMs = boxInMs;
            _boxOutMs = boxOutMs;
            _scrimInMs = scrimInMs;
            _scaleFrom = scaleFrom;
            _scaleTo = scaleTo;
            _scrimOpacity = scrimOpacity;
            _reduced = Driver.Settings.ReduceMotion;
        }

        private void StartIn(Action? onDone)
        {
            _entering = true;
            _onDone = onDone;
            if (!Driver.Armed)
            {
                Apply(double.MaxValue);
                OnFinished();
                return;
            }

            Begin();
        }

        private void StartOut(Action onDone)
        {
            _entering = false;
            _onDone = onDone;
            if (!Driver.Armed)
            {
                Apply(double.MaxValue);
                OnFinished();
                return;
            }

            Begin();
        }

        private void Apply(double elapsedMs)
        {
            if (_box == null)
            {
                return;
            }

            if (_entering)
            {
                var e = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, _boxInMs));
                _box.style.opacity = (float)e;
                var s = _reduced ? 1 : _scaleFrom + (1 - _scaleFrom) * e;
                _box.style.scale = new Scale(new Vector2((float)s, (float)s));
                if (_scrim != null)
                {
                    var m = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, _scrimInMs));
                    _scrim.style.opacity = (float)(_scrimOpacity * m);
                }

                return;
            }

            var x = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, _boxOutMs));
            _box.style.opacity = (float)(_fromOpacity * (1 - x));
            var scale = _reduced ? 1 : _fromScale + (_scaleTo - _fromScale) * x;
            _box.style.scale = new Scale(new Vector2((float)scale, (float)scale));
            if (_scrim != null)
            {
                _scrim.style.opacity = (float)(_fromScrim * (1 - x));
            }
        }
    }
}
