using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Motion
{
    // Coreografías de la tarjeta del pulsómetro real (It. 11; 00-arte-pulsometro §1-§3, §7; 02-tech-design §1.3, §4.1, §4.2; los números en
    // BleMotionSpecs). Todas con el reloj de MotionDriver (capturas deterministas), sin asignar por cuadro y con su variante de reducir
    // movimiento: lo finito termina en el mismo estado fijo, así una toma quieta sale igual con y sin reducir movimiento. Las piezas:
    // - Breath: el corazón del encabezado respira 0,40 ↔ 0,70 (3 200 ms) mientras reconecta (D11-05), solo con la tarjeta a la vista.
    // - Line: la línea "Sin señal del sensor" entra con la regla del empuje (alto 0-160, opacidad 60-260) y sale (0-120 / 80-240).
    // - Row: una fila de sensor nueva entra (alto y su margen 0-160; opacidad e y −8 → 0 en 60-260), con el retardo del escalonado.
    // - Follow: en un único desplazamiento (L640), si la tarjeta crece y su borde inferior sale de la vista, el ScrollView la sigue lo
    //   justo en 240 (ease.move), sin esconder su borde superior; con reducir movimiento, en el acto.
    public static class HrmMotion
    {
        // El corazón que respira. Continuo (no cuenta para Settle). La opacidad de reposo (0,40) y el fundido de los cambios los pone
        // HeartbeatAnimator; esto solo escribe la opacidad desde los 160 ms (cuando el fundido ya terminó) y la devuelve al dejar.
        public sealed class Breath : IMotion
        {
            private readonly MotionDriver _driver;
            private readonly VisualElement _heart;
            private readonly Fade _settle;
            private bool _on;
            private double _start;

            public Breath(MotionDriver driver, VisualElement heart)
            {
                _driver = driver ?? throw new ArgumentNullException(nameof(driver));
                _heart = heart ?? throw new ArgumentNullException(nameof(heart));
                _settle = new Fade(driver);
                _heart.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    if (_on)
                    {
                        _driver.Add(this);
                    }
                });
            }

            public bool IsFinite => false;

            public bool IsBreathing => _on;

            // breathing: HrmCardViewModel.HeartBreathing. restOpacity: la del corazón sin respirar (HeartbeatAnimator.TargetOpacity);
            // heartFading: HeartbeatAnimator ya está fundiendo hacia ella (al conectar), así que no se toca.
            public void Set(bool breathing, float restOpacity, bool heartFading)
            {
                if (breathing == _on)
                {
                    return;
                }

                _on = breathing;
                if (breathing)
                {
                    _settle.Finish();
                    _start = _driver.Now;
                    _driver.Add(this);
                    return;
                }

                _driver.Remove(this);
                if (heartFading)
                {
                    return;
                }

                var current = _heart.resolvedStyle.opacity;
                if (Math.Abs(current - restOpacity) > 0.001f)
                {
                    _settle.Play(_heart, current, restOpacity, Tokens.DurFast.Milliseconds);
                }
            }

            public bool Tick(double nowSeconds)
            {
                if (!_on || _heart.panel == null)
                {
                    return false;
                }

                // Reducir movimiento: 0,40 fijo (lo deja HeartbeatAnimator). Fuera de la vista no se anima.
                if (_driver.Settings.ReduceMotion || !BleMotion.IsDisplayed(_heart))
                {
                    return true;
                }

                var elapsed = (nowSeconds - _start) * 1000.0 - BleMotionSpecs.HrmBreathDelayMs;
                if (elapsed >= 0)
                {
                    _heart.style.opacity = (float)BleMotionSpecs.HrmBreathOpacity(elapsed);
                }

                return true;
            }
        }

        // Opacidad de "from" a "to" en ms (ease.move). Lo usa Breath para volver al reposo sin salto.
        public sealed class Fade : TimedMotion
        {
            private VisualElement? _target;
            private float _from;
            private float _to;
            private double _ms = 160;

            public Fade(MotionDriver driver)
                : base(driver)
            {
            }

            protected override double TotalMs => _ms;

            public void Play(VisualElement target, float from, float to, double ms)
            {
                Finish();
                _target = target;
                _from = from;
                _to = to;
                _ms = ms;
                if (!Driver.Armed || Driver.Settings.ReduceMotion)
                {
                    target.style.opacity = to;
                    _target = null;
                    return;
                }

                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_target != null)
                {
                    _target.style.opacity = (float)(_from + (_to - _from) * Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, _ms)));
                }
            }

            protected override void OnFinished()
            {
                _target = null;
            }
        }

        // La línea "Sin señal del sensor" (§1.2 estado 6, §2.2). Attach la ata al label del cuerpo vigente (en el acto); Set la muestra
        // o la saca. El alto se mide con el texto (una línea) y el margen de arriba es el del USS (4).
        public sealed class Line : TimedMotion
        {
            private Label? _target;
            private bool _shown;
            private bool _entering;
            private bool _reduced;
            private float _height;
            private float _gap;

            public Line(MotionDriver driver)
                : base(driver)
            {
            }

            public bool Shown => _shown;

            protected override double TotalMs => _entering
                ? (_reduced ? Tokens.DurFast.Milliseconds : BleMotionSpecs.HrmFadeEndMs)
                : (_reduced ? BleMotionSpecs.HrmOutFadeEndMs : BleMotionSpecs.HrmOutHeightEndMs);

            public void Attach(Label? target, bool shown)
            {
                Finish();
                _target = target;
                _shown = shown;
                if (target != null)
                {
                    Clear(target);
                    target.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            public void Set(bool shown)
            {
                var target = _target;
                if (target == null || shown == _shown)
                {
                    return;
                }

                Finish();
                _shown = shown;
                if (!Driver.Armed || !BleMotion.IsDisplayed(target.hierarchy.parent ?? target))
                {
                    Clear(target);
                    target.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                    return;
                }

                _entering = shown;
                _reduced = Driver.Settings.ReduceMotion;
                _gap = target.resolvedStyle.marginTop > 0 ? target.resolvedStyle.marginTop : (float)BleMotionSpecs.HrmStaleGapDp;
                if (shown)
                {
                    target.style.display = DisplayStyle.Flex;
                    var measured = target.MeasureTextSize(target.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined).y;
                    var pad = target.resolvedStyle.paddingTop + target.resolvedStyle.paddingBottom;
                    _height = measured > 0 ? measured + pad : (float)BleMotionSpecs.HrmStaleLineDp;
                }
                else
                {
                    _height = target.layout.height > 0 ? target.layout.height : (float)BleMotionSpecs.HrmStaleLineDp;
                }

                if (!_reduced)
                {
                    target.style.overflow = Overflow.Hidden;
                    target.style.minHeight = 0;
                }

                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                var target = _target;
                if (target == null)
                {
                    return;
                }

                if (_entering)
                {
                    if (_reduced)
                    {
                        target.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, Tokens.DurFast.Milliseconds));
                        return;
                    }

                    var h = (float)Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, BleMotionSpecs.HrmGrowEndMs));
                    target.style.height = _height * h;
                    target.style.marginTop = _gap * h;
                    target.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, BleMotionSpecs.HrmFadeStartMs, BleMotionSpecs.HrmFadeEndMs));
                    return;
                }

                target.style.opacity = (float)(1 - Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, BleMotionSpecs.HrmOutFadeEndMs)));
                if (!_reduced)
                {
                    var k = 1 - (float)Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, BleMotionSpecs.HrmOutHeightStartMs, BleMotionSpecs.HrmOutHeightEndMs));
                    target.style.height = _height * k;
                    target.style.marginTop = _gap * k;
                }
            }

            protected override void OnFinished()
            {
                var target = _target;
                if (target == null)
                {
                    return;
                }

                Clear(target);
                target.style.display = _shown ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private static void Clear(VisualElement target)
            {
                target.style.height = StyleKeyword.Null;
                target.style.minHeight = StyleKeyword.Null;
                target.style.marginTop = StyleKeyword.Null;
                target.style.overflow = StyleKeyword.Null;
                target.style.opacity = StyleKeyword.Null;
            }
        }

        // Una fila de sensor nueva (§1.3): alto 0 → heightDp y margen de arriba 0 → marginDp en 0-160 (ease.move); opacidad 0 → 1 e y −8 → 0
        // en 60-260 (ease.enter), después de delayMs. Al final todo vuelve a null. Reducir movimiento: fundido de 160 sin crecer.
        public sealed class Row : TimedMotion
        {
            private readonly VisualElement _target;
            private double _delay;
            private float _height;
            private float _margin;
            private bool _reduced;

            public Row(MotionDriver driver, VisualElement target)
                : base(driver)
            {
                _target = target ?? throw new ArgumentNullException(nameof(target));
            }

            protected override double TotalMs => _delay + (_reduced ? Tokens.DurFast.Milliseconds : BleMotionSpecs.HrmFadeEndMs);

            public void Play(double heightDp, double marginDp, double delayMs)
            {
                Finish();
                if (!Driver.Armed)
                {
                    return;
                }

                _height = (float)heightDp;
                _margin = (float)marginDp;
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
                    _target.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(t, 0, Tokens.DurFast.Milliseconds));
                    return;
                }

                var h = (float)Easing.Evaluate(Tokens.EaseMove, Progress(t, 0, BleMotionSpecs.HrmGrowEndMs));
                var e = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(t, BleMotionSpecs.HrmFadeStartMs, BleMotionSpecs.HrmFadeEndMs));
                _target.style.height = _height * h;
                _target.style.marginTop = _margin * h;
                _target.style.opacity = e;
                _target.style.translate = new Translate(0, (float)(-BleMotionSpecs.HrmRowDropDp * (1 - e)));
            }

            protected override void OnFinished()
            {
                _target.style.height = StyleKeyword.Null;
                _target.style.minHeight = StyleKeyword.Null;
                _target.style.marginTop = StyleKeyword.Null;
                _target.style.overflow = StyleKeyword.Null;
                _target.style.opacity = StyleKeyword.Null;
                _target.style.translate = StyleKeyword.Null;
            }
        }

        // Seguir a la tarjeta (§3; 02-tech-design §4.2). Track(card) se llama cuando la tarjeta cambió de alto: si creció y su borde inferior
        // queda fuera de la vista, desplaza lo justo (nunca hacia arriba ni más allá de su borde superior). Si ya estaba siguiendo, arranca
        // desde donde está. No actúa mientras el usuario toca o desplaza.
        public sealed class Follow : TimedMotion
        {
            private readonly ScrollView _scroll;
            private float _from;
            private float _to;

            public Follow(MotionDriver driver, ScrollView scroll)
                : base(driver)
            {
                _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            }

            protected override double TotalMs => BleMotionSpecs.HrmFollowMs;

            public void Track(VisualElement card)
            {
                if (Driver.UserInputActive || card.panel == null)
                {
                    return;
                }

                var viewport = _scroll.contentViewport.layout.height;
                if (!(viewport > 0))
                {
                    return;
                }

                // La tarjeta en coordenadas del contenido (sin asignar: suma los layout hasta el contenedor).
                var top = 0f;
                for (var e = card; e != null && e != _scroll.contentContainer; e = e.hierarchy.parent)
                {
                    top += e.layout.y;
                }

                var bottom = top + card.layout.height + card.resolvedStyle.marginBottom;
                var current = IsRunning ? _to : _scroll.scrollOffset.y;
                var target = Mathf.Min(top, bottom - viewport);
                if (!(target > current + 0.5f))
                {
                    return;
                }

                var max = Mathf.Max(0, _scroll.contentContainer.layout.height - viewport);
                target = Mathf.Min(target, max);
                if (!Driver.Armed || Driver.Settings.ReduceMotion)
                {
                    Finish();
                    _scroll.scrollOffset = new Vector2(_scroll.scrollOffset.x, target);
                    return;
                }

                _from = _scroll.scrollOffset.y;
                _to = target;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                var e = (float)Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, 0, BleMotionSpecs.HrmFollowMs));
                _scroll.scrollOffset = new Vector2(_scroll.scrollOffset.x, _from + (_to - _from) * e);
            }
        }
    }
}
