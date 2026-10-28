using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // El aviso (03-rutinas/04-design-notes §5.4; MotionSpecs.Toast*):
    // - Entra: y +16 → 0 y opacidad 0 → 1 en 220 (ease.enter).
    // - Sale a los 4 s: opacidad → 0 e y 0 → +8 en 160 (ease.exit).
    // - Reemplazo (DV-14): empujón de escala 1 → 1,02 (0-90, ease.enter) → 1 (90-240, ease.move), origen abajo al centro; con
    //   OTRO texto, además el viejo funde en 0-80 y el nuevo entra en 80-200 (swapText se llama a los 80 ms).
    // - Reducir movimiento: solo opacidad (220 / 160); sin empujón; el texto cambia en el acto.
    // La posición (bottom) la pone ToastLayer; acá solo translate, scale y opacity.
    public sealed class ToastTransition : TimedMotion
    {
        private enum Kind
        {
            In,
            Out,
            Bump
        }

        private VisualElement? _toast;
        private VisualElement? _text;
        private Action? _onDone;
        private Action? _swapText;
        private Kind _kind;
        private bool _reduced;
        private bool _swapped;

        public ToastTransition(MotionDriver driver)
            : base(driver)
        {
        }

        public bool IsLeaving => IsRunning && _kind == Kind.Out;

        protected override double TotalMs
        {
            get
            {
                switch (_kind)
                {
                    case Kind.In:
                        return MotionSpecs.ToastInMs;
                    case Kind.Out:
                        return MotionSpecs.ToastOutMs;
                    default:
                        return _reduced ? 0 : MotionSpecs.ToastBumpEndMs;
                }
            }
        }

        public void PlayIn(VisualElement toast, VisualElement text)
        {
            Start(Kind.In, toast, text, null, null);
        }

        public void PlayOut(VisualElement toast, VisualElement text, Action onDone)
        {
            Start(Kind.Out, toast, text, onDone, null);
        }

        // swapText null = el mismo texto (solo el empujón).
        public void PlayBump(VisualElement toast, VisualElement text, Action? swapText)
        {
            Start(Kind.Bump, toast, text, null, swapText);
        }

        protected override void OnUpdate(double elapsedMs)
        {
            if (_toast == null || _text == null)
            {
                return;
            }

            switch (_kind)
            {
                case Kind.In:
                {
                    var e = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.ToastInMs));
                    _toast.style.opacity = (float)e;
                    _toast.style.translate = _reduced ? new Translate(0, 0) : new Translate(0, (float)(Tokens.ShiftL.Value * (1 - e)));
                    break;
                }

                case Kind.Out:
                {
                    var e = Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.ToastOutMs));
                    _toast.style.opacity = (float)(1 - e);
                    _toast.style.translate = _reduced ? new Translate(0, 0) : new Translate(0, (float)(MotionSpecs.ToastOutShiftDp * e));
                    break;
                }

                default:
                {
                    if (_reduced)
                    {
                        Swap();
                        break;
                    }

                    var scale = elapsedMs <= MotionSpecs.ToastBumpPeakMs
                        ? 1 + (MotionSpecs.ToastBumpScale - 1) * Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.ToastBumpPeakMs))
                        : MotionSpecs.ToastBumpScale - (MotionSpecs.ToastBumpScale - 1)
                          * Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, MotionSpecs.ToastBumpPeakMs, MotionSpecs.ToastBumpEndMs));
                    _toast.style.scale = new Scale(new Vector2((float)scale, (float)scale));
                    if (_swapText != null)
                    {
                        if (elapsedMs < MotionSpecs.ToastTextOutMs)
                        {
                            _text.style.opacity = (float)(1 - Progress(elapsedMs, 0, MotionSpecs.ToastTextOutMs));
                        }
                        else
                        {
                            Swap();
                            _text.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter,
                                Progress(elapsedMs, MotionSpecs.ToastTextOutMs, MotionSpecs.ToastTextInEndMs));
                        }
                    }

                    break;
                }
            }
        }

        protected override void OnFinished()
        {
            Swap();
            if (_toast != null)
            {
                _toast.style.translate = StyleKeyword.Null;
                _toast.style.scale = StyleKeyword.Null;
                if (_kind != Kind.Out)
                {
                    _toast.style.opacity = StyleKeyword.Null;
                }
            }

            if (_text != null)
            {
                _text.style.opacity = StyleKeyword.Null;
            }

            var done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        private void Start(Kind kind, VisualElement toast, VisualElement text, Action? onDone, Action? swapText)
        {
            if (IsRunning)
            {
                // Un reemplazo durante la entrada termina la entrada en su lugar (sin saltos de opacidad).
                Finish();
            }

            _kind = kind;
            _toast = toast;
            _text = text;
            _onDone = onDone;
            _swapText = swapText;
            _swapped = false;
            _reduced = Driver.Settings.ReduceMotion;
            toast.style.transformOrigin = new TransformOrigin(new Length(50, LengthUnit.Percent), new Length(100, LengthUnit.Percent));
            if (!Driver.Armed)
            {
                OnUpdate(TotalMs);
                OnFinished();
                return;
            }

            Begin();
        }

        private void Swap()
        {
            if (_swapped || _swapText == null)
            {
                return;
            }

            _swapped = true;
            _swapText();
        }
    }
}
