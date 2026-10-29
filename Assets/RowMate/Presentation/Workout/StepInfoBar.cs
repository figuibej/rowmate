using System;
using RowMate.App.Routines;
using RowMate.App.Workout;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Routines;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La barra de pasos de la 4a: solo la fila de información (AC-WKT-14-01 a -06; legacy immersive_workout_screen.dart:330-475;
    // 02-tech-design §6.6 de la It. 4a; 04-design-notes §3.3, §4.5, §5.4, §5.5; DV-34, DV-35, DV-38):
    //   #hud-topbar .rm-hud-topbar .rm-hud-topbar--steps .rm-hud-glass   alto 40, relleno 12 a los lados, borde 1 @8 %, sin eventos
    //   └── .rm-hud-step-row        min-height 22, align-items: center
    //       ├── #hud-step-type      stepType* 13/700, tracking 0,04 em, en step.<tipo>.hud
    //       ├── #hud-step-target    pastilla (.rm-hud-pill): fondo step.<tipo> @20 %, radio 6, relleno 6 × 2, 11/600 en
    //       │                       text.primary; UN Label con trending_up (1,17 em, metric.distance) por texto enriquecido
    //       │                       generado por código (el camino de StepSubtitleView); se recorta con "…" y nunca empuja la cuenta
    //       ├── .rm-flex-spacer
    //       ├── #hud-up-next        slot que recorta (ancho 0 → N con ω 12): relleno 7 × 3, radio 8, alto 20,4
    //       │   ├── .rm-hud-up-next__bg   absoluto: fondo step.<siguiente> @20 % y borde 1 @50 %; ES LO QUE LATE
    //       │   ├── hudUpNext       10 Inter 400 en text.secondary
    //       │   └── tipo siguiente  " " + stepType* 10/700 en text.primary
    //       └── #hud-countdown      TabularNumber num.s 20, blanco, a 8 de Próximo; por terminar: 1,1 y el color del paso (StepPulse)
    // Por distancia: su largo (500m), sin "por terminar" ni Próximo (Q-51). Sin meta: ? (DV-25). En pausa la fila queda
    // quieta y los pulsos siguen. Con la rutina terminada: el último paso con 0:00 (o su largo), sin Próximo, de vuelta a 1 y a
    // blanco. El cambio de paso funde el tipo y la pastilla viejos (0-120 ease.exit) y entran los nuevos con una subida de 8
    // (120-260 ease.enter): los mismos dos elementos, uno después del otro (presupuesto de elementos, §6.13).
    public sealed class StepInfoBar : VisualElement
    {
        public const float TypeSizeDp = 13;
        public const float PillSizeDp = 11;
        public const float UpNextSizeDp = 10;
        public const float CountdownSizeDp = 20;
        public const float PillTintAlpha = 0.20f;
        public const float UpNextBorderAlpha = 0.50f;
        public const float UpNextPadHDp = 7;
        public const float UpNextPadVDp = 3;
        public const float GapDp = 6;
        public const float CountdownGapDp = 8;

        private readonly UiContext _ui;
        private readonly VisualElement _row;
        private readonly SpringMotion _upNextWidth;
        private readonly StepSwap _swap;
        private UpNextExit? _upNextExit;
        private StepType? _type;
        private string _typeText = "";
        private HudTargetLabel? _target;
        private bool _upNextShown;
        private double _upNextNatural;
        private Localizer? _texts;

        public StepInfoBar(UiContext ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "hud-topbar";
            AddToClassList("rm-hud-topbar");
            AddToClassList("rm-hud-topbar--steps");
            AddToClassList("rm-hud-glass");
            pickingMode = PickingMode.Ignore;
            HudTopBars.ApplyFrame(this, HudGeometry.StepRowPadH);

            _row = new VisualElement();
            _row.AddToClassList("rm-hud-step-row");
            _row.pickingMode = PickingMode.Ignore;
            _row.style.minHeight = (float)HudGeometry.TopBarRowHeight;
            Add(_row);

            TypeLabel = HudText.Label("hud-step-type", "rm-type-header-s", TypeSizeDp);
            TypeLabel.AddToClassList("rm-hud-step-type");
            TypeLabel.style.letterSpacing = 4;
            _row.Add(TypeLabel);

            Pill = HudText.Label("hud-step-target", "rm-type-button-s", PillSizeDp);
            Pill.AddToClassList("rm-hud-pill");
            Pill.enableRichText = true;
            Pill.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Pill.style.marginLeft = GapDp;
            Pill.style.display = DisplayStyle.None;
            _row.Add(Pill);

            var spacer = new VisualElement();
            spacer.AddToClassList("rm-flex-spacer");
            spacer.pickingMode = PickingMode.Ignore;
            _row.Add(spacer);

            UpNext = new VisualElement { name = "hud-up-next" };
            UpNext.AddToClassList("rm-hud-up-next");
            UpNext.pickingMode = PickingMode.Ignore;
            UpNext.style.paddingLeft = UpNextPadHDp + 1;
            UpNext.style.paddingRight = UpNextPadHDp + 1;
            UpNext.style.paddingTop = UpNextPadVDp + 1;
            UpNext.style.paddingBottom = UpNextPadVDp + 1;
            UpNext.style.display = DisplayStyle.None;
            UpNextBg = new VisualElement();
            UpNextBg.AddToClassList("rm-hud-up-next__bg");
            UpNextBg.pickingMode = PickingMode.Ignore;
            UpNext.Add(UpNextBg);
            UpNextLabel = HudText.Label("hud-up-next-label", "rm-type-body-s", UpNextSizeDp);
            UpNextLabel.AddToClassList("rm-hud-up-next__label");
            UpNextLabel.style.color = TokenColors.ToColor(Tokens.TextSecondary);
            UpNext.Add(UpNextLabel);
            UpNextType = HudText.Label("hud-up-next-type", "rm-type-header-s", UpNextSizeDp);
            UpNextType.AddToClassList("rm-hud-up-next__type");
            UpNextType.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            UpNext.Add(UpNextType);
            _row.Add(UpNext);
            UpNextLabel.RegisterCallback<GeometryChangedEvent>(_ => MeasureUpNext());
            UpNextType.RegisterCallback<GeometryChangedEvent>(_ => MeasureUpNext());

            Countdown = new TabularNumber(ui.Motion) { name = "hud-countdown" };
            Countdown.AddToClassList("rm-type-num-s");
            Countdown.AddToClassList("rm-hud-countdown");
            Countdown.SetFontSize(CountdownSizeDp);
            Countdown.style.marginLeft = CountdownGapDp;
            Countdown.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            _row.Add(Countdown);

            Pulse = new StepPulse(ui.Motion, UpNextBg, Countdown, TokenColors.ToColor(Tokens.TextPrimary));
            _upNextWidth = new SpringMotion(ui.Motion, w => UpNext.style.width = (float)Math.Max(0, w));
            _swap = new StepSwap(ui.Motion, TypeLabel, Pill, ApplyStepTexts);
        }

        public Label TypeLabel { get; }

        public Label Pill { get; }

        public VisualElement UpNext { get; }

        public VisualElement UpNextBg { get; }

        public Label UpNextLabel { get; }

        public Label UpNextType { get; }

        public TabularNumber Countdown { get; }

        public StepPulse Pulse { get; }

        public bool UpNextShown => _upNextShown;

        // El texto de la pastilla tal como lo arma el legacy (con el 📈), para las pruebas.
        public string? TargetText => _target?.Text;

        public void RenderTexts(Localizer texts)
        {
            _texts = texts;
            UpNextLabel.text = texts.Get(L10nKeys.HudUpNext);
        }

        // La fila de un estado del HUD (null = sin paso: no pasa con rutina fuera de reposo). Sin paso (la foto de reposo, con el
        // inmersivo que sale) se apagan los pulsos que hubieran quedado de un Terminar a mano con "a continuación" o "por
        // terminar" (QA-V4a-02); la fila queda con lo último que mostró.
        public void Apply(HudStepRow? row, bool stepChanged, bool instant)
        {
            if (row == null)
            {
                Pulse.Release(instant);
                return;
            }

            var animate = !instant && _ui.Motion.Armed;
            var typeChanged = _type != row.Type || _typeText != row.TypeText || !SameTarget(_target, row.Target);
            _type = row.Type;
            _typeText = row.TypeText;
            _target = row.Target;
            if (typeChanged)
            {
                if (animate && stepChanged && TypeLabel.text.Length > 0)
                {
                    _swap.Play();
                }
                else
                {
                    _swap.Finish();
                    ApplyStepTexts();
                }
            }

            Countdown.SetValue(row.Countdown, row.CountdownValue, !instant);
            ApplyUpNext(row, animate);
            var accent = TokenColors.ToColor(StepRowModel.HudColor(row.Type));
            Pulse.Update(row.UpNextType.HasValue, row.EndingSoon, accent, instant);
        }

        // El tipo y la pastilla del paso actual (en el cuadro del cambio, o a mitad del fundido del cambio).
        private void ApplyStepTexts()
        {
            if (_type == null)
            {
                return;
            }

            TypeLabel.text = _typeText;
            TypeLabel.style.color = TokenColors.ToColor(StepRowModel.HudColor(_type.Value));
            if (_target == null)
            {
                Pill.style.display = DisplayStyle.None;
                Pill.text = "";
                return;
            }

            Pill.style.display = DisplayStyle.Flex;
            Pill.style.backgroundColor = HudText.WithAlpha(TokenColors.ToColor(StepTypeInfo.Color(_type.Value)), PillTintAlpha);
            Pill.text = PillMarkup(_target);
        }

        private void ApplyUpNext(HudStepRow row, bool animate)
        {
            var shown = row.UpNextType.HasValue;
            if (shown)
            {
                var next = row.UpNextType!.Value;
                var typeText = " " + (row.UpNextTypeText ?? "");
                if (UpNextType.text != typeText)
                {
                    UpNextType.text = typeText;
                    MeasureUpNext();
                }

                var baseColor = TokenColors.ToColor(StepTypeInfo.Color(next));
                UpNextBg.style.backgroundColor = HudText.WithAlpha(baseColor, PillTintAlpha);
                var border = HudText.WithAlpha(baseColor, UpNextBorderAlpha);
                UpNextBg.style.borderLeftColor = border;
                UpNextBg.style.borderRightColor = border;
                UpNextBg.style.borderTopColor = border;
                UpNextBg.style.borderBottomColor = border;
            }

            if (shown == _upNextShown)
            {
                return;
            }

            _upNextShown = shown;
            _upNextExit?.Finish();
            if (shown)
            {
                UpNext.style.display = DisplayStyle.Flex;
                if (animate && !_ui.Motion.Settings.ReduceMotion)
                {
                    // Entra con su ancho de 0 a N (ω 12) y funde 0 → 1 en 200 (ease.enter); el latido del fondo es de StepPulse.
                    _upNextWidth.Snap(0);
                    UpNext.style.opacity = 0;
                    _upNextExit = new UpNextExit(_ui.Motion, UpNext, null, true);
                    _upNextExit.Play();
                    MeasureUpNext();
                    if (_upNextNatural > 0)
                    {
                        _upNextWidth.MoveTo(_upNextNatural, MotionSpecs.UpNextWidthOmega);
                    }
                }
                else
                {
                    _upNextWidth.Stop();
                    UpNext.style.width = StyleKeyword.Null;
                    UpNext.style.opacity = StyleKeyword.Null;
                }

                return;
            }

            if (animate)
            {
                // Sale: opacidad → 0 en 0-120 (ease.exit) y ancho → 0 en 80-280 (ease.move); el espacio lo toma.
                _upNextWidth.Stop();
                _upNextExit = new UpNextExit(_ui.Motion, UpNext, () =>
                {
                    if (!_upNextShown)
                    {
                        UpNext.style.display = DisplayStyle.None;
                        UpNext.style.width = StyleKeyword.Null;
                        UpNext.style.opacity = StyleKeyword.Null;
                    }
                }, false);
                _upNextExit.Play();
                return;
            }

            _upNextWidth.Stop();
            UpNext.style.display = DisplayStyle.None;
            UpNext.style.width = StyleKeyword.Null;
            UpNext.style.opacity = StyleKeyword.Null;
        }

        // El ancho natural de Próximo (relleno + los dos textos enteros). Se mide el TEXTO y no la caja: con la pastilla
        // creciendo desde 0, cada etiqueta mide lo que le deja su padre (0 al aparecer) y la pastilla quedaba en su relleno.
        private void MeasureUpNext()
        {
            if (UpNextLabel.panel == null || UpNextType.panel == null)
            {
                return;
            }

            var label = UpNextLabel.MeasureTextSize(UpNextLabel.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
            var type = UpNextType.MeasureTextSize(UpNextType.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
            // Las dos etiquetas, a su ancho natural: mientras la pastilla crece desde 0 (o se cierra), UI Toolkit mide cada texto
            // "a lo sumo" el ancho que le deja el padre y las achicaba, así que el tipo se dibujaba encima de "Próximo:" ("PT",
            // "PróxT" en VID-01). Con el mínimo quedan enteras y la pastilla las recorta (QA-G4aT-14; WP-D).
            if (Math.Abs(UpNextLabel.resolvedStyle.minWidth.value - label) > 0.01f)
            {
                UpNextLabel.style.minWidth = label;
            }

            if (Math.Abs(UpNextType.resolvedStyle.minWidth.value - type) > 0.01f)
            {
                UpNextType.style.minWidth = type;
            }

            var natural = label + type + 2 * (UpNextPadHDp + 1);
            if (!(natural > 2 * (UpNextPadHDp + 1)) || Math.Abs(natural - _upNextNatural) < 0.01)
            {
                return;
            }

            _upNextNatural = natural;
            if (_upNextShown && _upNextWidth.HasValue)
            {
                _upNextWidth.MoveTo(natural, MotionSpecs.UpNextWidthOmega);
            }
        }

        // trending_up (U+E8E5) en metric.distance al 117 % y el texto en el color de la etiqueta (blanco, DV-38).
        private static string PillMarkup(HudTargetLabel target)
        {
            if (!target.HasTrend)
            {
                return Escape(target.Text);
            }

            return Escape(target.Before) + "<color=" + Tokens.MetricDistance.Rgb.ToHex() + "><size=117%>" + StepSubtitleView.TrendGlyph
                   + "</size></color>" + Escape(target.After);
        }

        // Las etiquetas de objetivo las arma el motor con números y el catálogo: un '<' no debería llegar, pero si llega no forma
        // marcas (noparse).
        private static string Escape(string text)
        {
            return text.IndexOf('<') >= 0 ? "<noparse>" + text + "</noparse>" : text;
        }

        private static bool SameTarget(HudTargetLabel? a, HudTargetLabel? b)
        {
            if (a == null || b == null)
            {
                return a == null && b == null;
            }

            return a.Text == b.Text;
        }

        // El cambio de paso del tipo y la pastilla: los viejos funden 0-120 (ease.exit) y los nuevos entran 120-260 (ease.enter)
        // desde 8 dp más abajo; con reducir movimiento, solo los fundidos.
        private sealed class StepSwap : TimedMotion
        {
            private readonly VisualElement _type;
            private readonly VisualElement _pill;
            private readonly Action _swap;
            private bool _swapped;
            private bool _reduced;

            public StepSwap(MotionDriver driver, VisualElement type, VisualElement pill, Action swap)
                : base(driver)
            {
                _type = type;
                _pill = pill;
                _swap = swap;
            }

            protected override double TotalMs => MotionSpecs.StepSwapInEndMs;

            public void Play()
            {
                Finish();
                _swapped = false;
                _reduced = Driver.Settings.ReduceMotion;
                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (elapsedMs < MotionSpecs.StepSwapOutMs)
                {
                    var o = 1 - Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.StepSwapOutMs));
                    Set((float)o, 0);
                    return;
                }

                if (!_swapped)
                {
                    _swapped = true;
                    _swap();
                }

                var e = Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, MotionSpecs.StepSwapOutMs, MotionSpecs.StepSwapInEndMs));
                var shift = _reduced ? 0 : Tokens.ShiftS.Value * (1 - e);
                Set((float)e, (float)shift);
            }

            protected override void OnFinished()
            {
                if (!_swapped)
                {
                    _swapped = true;
                    _swap();
                }

                _type.style.opacity = StyleKeyword.Null;
                _type.style.translate = StyleKeyword.Null;
                _pill.style.opacity = StyleKeyword.Null;
                _pill.style.translate = StyleKeyword.Null;
            }

            private void Set(float opacity, float shift)
            {
                _type.style.opacity = opacity;
                _pill.style.opacity = opacity;
                var translate = Mathf.Abs(shift) < 1e-4f ? new Translate(0, 0) : new Translate(0, shift);
                _type.style.translate = translate;
                _pill.style.translate = translate;
            }
        }

        // La entrada (fundido 0 → 1 en 200, ease.enter) y la salida (fundido → 0 en 0-120 ease.exit, ancho → 0 en 80-280
        // ease.move) de la pastilla Próximo.
        private sealed class UpNextExit : TimedMotion
        {
            private readonly VisualElement _slot;
            private readonly Action? _done;
            private readonly bool _entering;
            private float _fromWidth;

            public UpNextExit(MotionDriver driver, VisualElement slot, Action? done, bool entering)
                : base(driver)
            {
                _slot = slot;
                _done = done;
                _entering = entering;
            }

            protected override double TotalMs => _entering ? MotionSpecs.UpNextFadeInMs : MotionSpecs.CollapseEndMs;

            public void Play()
            {
                _fromWidth = _slot.layout.width;
                if (!Driver.Armed)
                {
                    OnUpdate(TotalMs);
                    OnFinished();
                    return;
                }

                Begin();
            }

            protected override void OnUpdate(double elapsedMs)
            {
                if (_entering)
                {
                    _slot.style.opacity = (float)Easing.Evaluate(Tokens.EaseEnter, Progress(elapsedMs, 0, MotionSpecs.UpNextFadeInMs));
                    return;
                }

                _slot.style.opacity = (float)(1 - Easing.Evaluate(Tokens.EaseExit, Progress(elapsedMs, 0, MotionSpecs.CollapseFadeMs)));
                var shrink = Easing.Evaluate(Tokens.EaseMove, Progress(elapsedMs, MotionSpecs.CollapseStartMs, MotionSpecs.CollapseEndMs));
                _slot.style.width = (float)(_fromWidth * (1 - shrink));
            }

            protected override void OnFinished()
            {
                if (_entering)
                {
                    _slot.style.opacity = StyleKeyword.Null;
                }

                _done?.Invoke();
            }
        }
    }
}
