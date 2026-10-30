using System;
using System.Collections.Generic;
using System.Text;
using RowMate.App.Routines;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Routines;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using RowMate.Presentation.Routines;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La barra de pasos de la It. 4b (F-WKT-14, -15, -19; AC-WKT-14-01 a -19, AC-WKT-09-08; legacy
    // immersive_workout_screen.dart:330-475; 02-tech-design §6.6 de la It. 4a; 04b-hud-exr/02-tech-design §2.15, §3.4 a §3.6;
    // 04-design-notes §3.1 a §3.4, §3.7; DN4b-16; DD4b-03):
    //   #hud-topbar .rm-hud-topbar .rm-hud-topbar--steps .rm-hud-glass   COLUMNA; alto 52 (74 con el sello, SealMotion); borde 1
    //   │                                                                @8 %; radio 14; overflow: hidden; sin relleno ni eventos
    //   ├── #hud-pill-row .rm-hud-pill-row   PillRowView: UN elemento, alto 20 (las píldoras en 10 / 8 / 10 / 4)
    //   ├── .rm-hud-step-row                  la fila de información: relleno 12 / 0 / 12 / 8, min-height 22
    //   │   ├── #hud-step-type                stepType* 13/700, tracking 0,04 em, step.<tipo>.hud
    //   │   ├── #hud-step-target (.rm-hud-pill)  la pastilla, en un slot que recorta (flex-shrink 1, sin text-overflow): el texto lo
    //   │   │                                  arma el código con los fragmentos ENTEROS del plan y el "…" en text.primary
    //   │   ├── .rm-flex-spacer
    //   │   ├── #hud-up-next                  slot que recorta (0 → N con ω 12): relleno 7 o 4 (el plan) × 3, radio 8
    //   │   │   ├── .rm-hud-up-next__bg       absoluto: fondo step.<siguiente> @20 % y borde 1 @50 %; ES LO QUE LATE
    //   │   │   ├── hudUpNext                 10 Inter 400 en text.secondary
    //   │   │   └── tipo siguiente            " " + stepType* 10/700 en text.primary
    //   │   ├── #hud-countdown                TabularNumber num.s 20, blanco, flex-shrink 0; su aire a la izquierda, 8 o 6 (el plan)
    //   │   └── #hud-wall-clock               el reloj de pared (WallClockView): margin-left 10 o 6 (el plan); flex-shrink 0
    //   └── #hud-seal .rm-hud-seal            la tercera fila (SealRow): 0 sin sello, 22 con él
    // La fila de información (DN4b-16; InfoRowFit): el modo (normal 6 · 8 · 10 con Próximo 7 × 3; compacto 1 con los dos aires en
    // 6; compacto 2, además, Próximo con 4 × 3) y cuántos fragmentos ENTEROS de la pastilla entran, sin Próximo y con él, se
    // deciden al empezar el paso y cuando cambian la ventana, el idioma o la letra (nunca dentro del paso), con los anchos medidos
    // acá (solo con panel). Cuando entra Próximo, lo único que cambia es la pastilla: pasa a sus fragmentos con Próximo o, con 0,
    // funde en 120 y sale del árbol (InfoRowMotion). Si el modo cambia de un paso al siguiente, los aires y el relleno pasan al
    // nuevo en 0-240 con el cambio de paso. Lo que nunca se corta: el tipo, Próximo, la cuenta y el reloj (pegados al borde
    // derecho). Un número o una palabra nunca se cortan: el "…" va pegado y en blanco (DD4b-03).
    // Por distancia: su largo (500m), sin "por terminar" ni Próximo (Q-51). Sin meta: ? (DV-25). En pausa la fila queda quieta y
    // los pulsos siguen. Con la rutina terminada: el último paso con 0:00 (o su largo), sin Próximo.
    public sealed class StepInfoBar : VisualElement, IDisposable
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
        public const float PillPadHDp = 6;
        public const string Ellipsis = "…";

        private readonly UiContext _ui;
        private readonly VisualElement _row;
        private readonly SpringMotion _upNextWidth;
        private readonly StepSwap _swap;
        private readonly InfoRowMotion _infoRow;
        private UpNextExit? _upNextExit;
        private StepType? _type;
        private string _typeText = "";
        private HudTargetLabel? _target;
        private bool _upNextShown;
        private double _upNextNatural;
        private Localizer? _texts;
        private InfoRowPlan _plan = new InfoRowPlan(InfoRowMode.Normal, int.MaxValue, int.MaxValue);
        private bool _hasPlan;
        private int _planStep = -1;
        private double _planAvailable = -1;
        private bool _planTexts;
        private int _fragmentsShown = -1;
        private bool _targetHidden;
        private float _upNextPad = UpNextPadHDp;
        private double _barHeight = HudGeometry.TopBarHeightSteps;
        private WorkoutHudState? _state;
        private bool _disposed;
        private bool _loggedOverflow;

        public StepInfoBar(UiContext ui, IReadOnlyObservable<WallClockState> clock)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            name = "hud-topbar";
            AddToClassList("rm-hud-topbar");
            AddToClassList("rm-hud-topbar--steps");
            AddToClassList("rm-hud-glass");
            pickingMode = PickingMode.Ignore;
            HudTopBars.ApplyBorder(this);
            style.flexDirection = FlexDirection.Column;
            style.alignItems = Align.Stretch;
            style.overflow = Overflow.Hidden;
            style.height = (float)HudGeometry.TopBarHeightSteps;
            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;

            _row = new VisualElement();
            _row.AddToClassList("rm-hud-step-row");
            _row.pickingMode = PickingMode.Ignore;
            _row.style.minHeight = (float)HudGeometry.TopBarRowHeight;
            _row.style.paddingLeft = (float)HudGeometry.StepRowPadH;
            _row.style.paddingRight = (float)HudGeometry.StepRowPadH;
            _row.style.paddingTop = 0;
            _row.style.paddingBottom = (float)HudGeometry.InfoRowPadBottom;
            _row.style.flexGrow = 0;
            _row.style.flexShrink = 0;

            TypeLabel = HudText.Label("hud-step-type", "rm-type-header-s", TypeSizeDp);
            TypeLabel.AddToClassList("rm-hud-step-type");
            TypeLabel.style.letterSpacing = 4;
            _row.Add(TypeLabel);

            Pill = HudText.Label("hud-step-target", "rm-type-button-s", PillSizeDp);
            Pill.AddToClassList("rm-hud-pill");
            Pill.enableRichText = true;
            Pill.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Pill.style.marginLeft = GapDp;
            // El slot que recorta (DN4b-16): sin text-overflow; el "…" lo arma el código con fragmentos enteros.
            Pill.style.textOverflow = TextOverflow.Clip;
            Pill.style.display = DisplayStyle.None;
            _row.Add(Pill);

            var spacer = new VisualElement();
            spacer.AddToClassList("rm-flex-spacer");
            spacer.pickingMode = PickingMode.Ignore;
            _row.Add(spacer);

            UpNext = new VisualElement { name = "hud-up-next" };
            UpNext.AddToClassList("rm-hud-up-next");
            UpNext.pickingMode = PickingMode.Ignore;
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
            ApplyUpNextPad(UpNextPadHDp);

            Countdown = new TabularNumber(ui.Motion) { name = "hud-countdown" };
            Countdown.AddToClassList("rm-type-num-s");
            Countdown.AddToClassList("rm-hud-countdown");
            Countdown.SetFontSize(CountdownSizeDp);
            Countdown.style.marginLeft = CountdownGapDp;
            Countdown.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            _row.Add(Countdown);

            Clock = new WallClockView(ui, clock);
            Clock.Number.style.marginLeft = (float)HudGeometry.WallClockGap;
            _row.Add(Clock.Number);

            Pulse = new StepPulse(ui.Motion, UpNextBg, Countdown, TokenColors.ToColor(Tokens.TextPrimary));
            PillRow = new PillRowView(ui, Pulse);
            Add(PillRow);
            Add(_row);
            Seal = new SealRow(ui, SetBarHeight);
            Add(Seal);

            _upNextWidth = new SpringMotion(ui.Motion, w => UpNext.style.width = (float)Math.Max(0, w));
            _swap = new StepSwap(ui.Motion, TypeLabel, Pill, ApplyStepTexts);
            _infoRow = new InfoRowMotion(ui.Motion, ApplyGaps, o => Pill.style.opacity = o >= 0.999 ? (StyleFloat)StyleKeyword.Null : (float)o, OnTargetGone);
            RegisterCallback<GeometryChangedEvent>(_ => OnGeometry());
            RegisterCallback<DetachFromPanelEvent>(_ => StopMotions());
        }

        public PillRowView PillRow { get; }

        public SealRow Seal { get; }

        public WallClockView Clock { get; }

        public Label TypeLabel { get; }

        public Label Pill { get; }

        public VisualElement UpNext { get; }

        public VisualElement UpNextBg { get; }

        public Label UpNextLabel { get; }

        public Label UpNextType { get; }

        public TabularNumber Countdown { get; }

        public StepPulse Pulse { get; }

        public InfoRowMotion InfoRow => _infoRow;

        public bool UpNextShown => _upNextShown;

        // El plan de la fila de información del paso (InfoRowFit).
        public InfoRowPlan Plan => _plan;

        // Los fragmentos de la pastilla que se ven (0 = sin pastilla; -1 antes del primer paso).
        public int FragmentsShown => _targetHidden ? 0 : _fragmentsShown;

        // El alto de la barra: 52, 74 con el sello (lo mueve SealMotion).
        public double BarHeight => _barHeight;

        // El texto de la pastilla tal como lo arma el legacy (con el 📈), para las pruebas.
        public string? TargetText => _target?.Text;

        // La pastilla como se ve: los fragmentos del plan unidos por " · " y el "…" si queda alguno afuera (sin marcas).
        public string PillText { get; private set; } = "";

        public void RenderTexts(Localizer texts)
        {
            _texts = texts;
            UpNextLabel.text = texts.Get(L10nKeys.HudUpNext);
            _planTexts = true;
        }

        // Un estado del HUD. stepChanged: el paso del motor cambió (la vista lo sabe por el índice). Sin paso (no pasa con rutina
        // fuera de reposo; la foto de reposo ya no llega: la salida conserva la última foto) se apagan los pulsos.
        public void Apply(WorkoutHudState state, bool stepChanged, bool instant)
        {
            if (_disposed)
            {
                return;
            }

            _state = state;
            var row = state.Step;
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
            var stepIndex = state.Pills?.Current ?? -1;
            var planChanged = UpdatePlan(stepIndex, stepChanged, instant);
            if (typeChanged || planChanged)
            {
                if (typeChanged && animate && stepChanged && TypeLabel.text.Length > 0)
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
            ApplyUpNext(row, animate, instant);
            var accent = TokenColors.ToColor(StepRowModel.HudColor(row.Type));
            Pulse.Update(row.UpNextType.HasValue, row.EndingSoon, accent, instant);
            PillRow.Apply(state.Pills, instant);
            Seal.Apply(state.Seal, instant);
        }

        // La última foto (O-01): nada más cambia; los pulsos y los rodillos terminan con su reloj.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopMotions();
            Pulse.Stop();
            PillRow.Dispose();
            Seal.Dispose();
        }

        private void StopMotions()
        {
            _infoRow.Stop();
            PillRow.Motion.Stop();
            Seal.Motion.Stop();
        }

        private void SetBarHeight(double height)
        {
            _barHeight = height;
            style.height = (float)height;
        }

        // ── El plan de la fila de información (§2.15) ──

        // La ventana, el idioma o la letra cambiaron: el plan se rehace en el acto (sin animar).
        private void OnGeometry()
        {
            if (_state == null || _disposed)
            {
                return;
            }

            if (UpdatePlan(_state.Pills?.Current ?? _planStep, false, true))
            {
                ApplyStepTexts();
                ApplyUpNextTarget(true);
            }
        }

        // Decide el plan del paso: al empezar el paso (con el cambio de modo animado) o si cambió el ancho o los textos (en el
        // acto). true = cambió lo que muestra la pastilla.
        private bool UpdatePlan(int stepIndex, bool stepChanged, bool instant)
        {
            if (panel == null || _type == null || _texts == null)
            {
                return false;
            }

            var available = layout.width - 2 * PillRowGeometry.BarBorder - 2 * HudGeometry.StepRowPadH;
            if (!(available > 0))
            {
                return false;
            }

            var newStep = stepIndex != _planStep || stepChanged;
            if (!newStep && !_planTexts && _hasPlan && Math.Abs(available - _planAvailable) < 0.5)
            {
                return false;
            }

            var typeWidth = Measure(TypeLabel, _typeText);
            var upNextWidth = WillHaveUpNext(out var nextType) ? UpNextWidthFor(nextType) : 0;
            var countdownWidth = CountdownWidth();
            var clockWidth = Clock.Number.NaturalBoxWidth;
            if (!(countdownWidth > 0) || !(clockWidth > 0))
            {
                return false;
            }

            var fragments = _target?.Fragments;
            var count = fragments?.Count ?? 0;
            var prefixes = new double[Math.Max(1, count)];
            for (var k = 0; k < count; k++)
            {
                prefixes[k] = Measure(Pill, Markup(fragments!, k + 1, count)) + 2 * PillPadHDp;
            }

            var plan = InfoRowFit.Plan(available, typeWidth, upNextWidth, countdownWidth, clockWidth, prefixes, count);
            var previous = _plan;
            var hadPlan = _hasPlan;
            _plan = plan;
            _hasPlan = true;
            _planStep = stepIndex;
            _planAvailable = available;
            _planTexts = false;
            if (!_loggedOverflow && InfoRowFit.Width(plan.Mode, typeWidth, upNextWidth, upNextWidth > 0, countdownWidth, clockWidth, 0) > available + 0.5)
            {
                _loggedOverflow = true;
                _ui.Logger.Log(Core.Common.LogLevel.Warning, "HUD", "la fila de información no entra ni en compacto 2 (" + plan + ")");
            }

            // Un paso nuevo: la pastilla vuelve (ShowTarget cancela el fundido) y los aires van al modo nuevo.
            _targetHidden = false;
            _infoRow.ShowTarget();
            Pill.style.opacity = StyleKeyword.Null;
            if (hadPlan && newStep && previous.Mode != plan.Mode)
            {
                _infoRow.ChangeMode(previous, plan, instant);
            }
            else
            {
                _infoRow.ChangeMode(plan, plan, true);
            }

            return true;
        }

        // El paso va a tener Próximo: hay uno siguiente y es por tiempo (AC-WKT-04-05).
        private bool WillHaveUpNext(out StepType next)
        {
            next = default;
            var pills = _state?.Pills;
            if (pills == null)
            {
                return false;
            }

            var index = pills.Current + 1;
            if (index >= pills.Types.Count || !(pills.Duration > 0))
            {
                return false;
            }

            next = pills.Types[index];
            return true;
        }

        // Próximo con su tipo y el relleno normal (7 + el borde de cada lado).
        private double UpNextWidthFor(StepType next)
        {
            var label = Measure(UpNextLabel, UpNextLabel.text);
            var type = Measure(UpNextType, " " + _texts!.Get(StepTypeInfo.NameKey(next)));
            return label + type + 2 * (InfoRowFit.UpNextPadH + 1);
        }

        // La cuenta como se ve en los últimos 15 s (M:SS); por distancia o sin meta, la que se ve.
        private double CountdownWidth()
        {
            var pills = _state?.Pills;
            if (pills != null && pills.Duration > 0)
            {
                return Countdown.MeasureDigits("0:00");
            }

            return Countdown.NaturalBoxWidth;
        }

        private void ApplyGaps(InfoRowVisual visual)
        {
            Countdown.style.marginLeft = (float)visual.CountdownGap;
            Clock.Number.style.marginLeft = (float)visual.ClockGap;
            ApplyUpNextPad((float)visual.UpNextPadH);
        }

        private void ApplyUpNextPad(float pad)
        {
            _upNextPad = pad;
            UpNext.style.paddingLeft = pad + 1;
            UpNext.style.paddingRight = pad + 1;
            MeasureUpNext();
        }

        private void OnTargetGone()
        {
            if (_targetHidden)
            {
                Pill.style.display = DisplayStyle.None;
            }
        }

        // ── La pastilla por fragmentos (DN4b-16, DD4b-03) ──

        // El tipo y la pastilla del paso actual (en el cuadro del cambio, o a mitad del fundido del cambio).
        private void ApplyStepTexts()
        {
            if (_type == null)
            {
                return;
            }

            TypeLabel.text = _typeText;
            TypeLabel.style.color = TokenColors.ToColor(StepRowModel.HudColor(_type.Value));
            ApplyTarget(_upNextShown ? _plan.FragmentsWithUpNext : _plan.FragmentsAlone, false);
        }

        // Los fragmentos que se ven (0 = sin pastilla).
        private void ApplyTarget(int fragments, bool fading)
        {
            if (_target == null || fragments <= 0 || _target.Fragments.Count == 0)
            {
                if (_target == null || _target.Fragments.Count == 0 || !fading)
                {
                    _fragmentsShown = 0;
                    Pill.style.display = DisplayStyle.None;
                    Pill.text = "";
                    PillText = "";
                }

                return;
            }

            var count = _target.Fragments.Count;
            var shown = Math.Min(fragments, count);
            _fragmentsShown = shown;
            Pill.style.display = DisplayStyle.Flex;
            Pill.style.backgroundColor = HudText.WithAlpha(TokenColors.ToColor(StepTypeInfo.Color(_type!.Value)), PillTintAlpha);
            Pill.text = Markup(_target.Fragments, shown, count);
            PillText = PlainText(_target.Fragments, shown, count);
        }

        // Al entrar Próximo, la pastilla pasa a sus fragmentos con Próximo o, con 0, funde y se va (InfoRowMotion).
        private void ApplyUpNextTarget(bool instant)
        {
            if (_target == null || !_hasPlan)
            {
                return;
            }

            if (!_upNextShown)
            {
                _targetHidden = false;
                _infoRow.ShowTarget();
                Pill.style.opacity = StyleKeyword.Null;
                ApplyTarget(_plan.FragmentsAlone, false);
                return;
            }

            if (_plan.FragmentsWithUpNext > 0)
            {
                ApplyTarget(_plan.FragmentsWithUpNext, false);
                return;
            }

            if (_plan.FragmentsAlone <= 0 || _targetHidden)
            {
                ApplyTarget(0, false);
                return;
            }

            _targetHidden = true;
            _infoRow.HideTarget(instant || !_ui.Motion.Armed);
            if (!_infoRow.IsHidingTarget)
            {
                Pill.style.display = DisplayStyle.None;
            }
        }

        // La pastilla con sus k primeros fragmentos (de count): " · " entre ellos, la tendencia con su glifo en metric.distance al
        // 117 % (el camino de StepSubtitleView) y, si queda alguno afuera, el "…" pegado en text.primary.
        private static string Markup(IReadOnlyList<HudTargetFragment> fragments, int k, int count)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < k; i++)
            {
                if (i > 0)
                {
                    builder.Append(StepLabels.PartSeparator);
                }

                var fragment = fragments[i];
                if (fragment.IsTrend && fragment.Text.StartsWith(TrendText.TrendEmoji, StringComparison.Ordinal))
                {
                    builder.Append("<color=").Append(Tokens.MetricDistance.Rgb.ToHex()).Append("><size=117%>").Append(StepSubtitleView.TrendGlyph)
                        .Append("</size></color>");
                    builder.Append(Escape(fragment.Text.Substring(TrendText.TrendEmoji.Length)));
                }
                else
                {
                    builder.Append(Escape(fragment.Text));
                }
            }

            if (k < count)
            {
                builder.Append("<color=").Append(Tokens.TextPrimary.Rgb.ToHex()).Append('>').Append(Ellipsis).Append("</color>");
            }

            return builder.ToString();
        }

        private static string PlainText(IReadOnlyList<HudTargetFragment> fragments, int k, int count)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < k; i++)
            {
                if (i > 0)
                {
                    builder.Append(StepLabels.PartSeparator);
                }

                builder.Append(fragments[i].Text);
            }

            if (k < count)
            {
                builder.Append(Ellipsis);
            }

            return builder.ToString();
        }

        // ── Próximo (4a) ──

        private void ApplyUpNext(HudStepRow row, bool animate, bool instant)
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
            ApplyUpNextTarget(instant || !animate);
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
            // "a lo sumo" el ancho que le deja el padre y las achicaba (QA-G4aT-14; WP-D de la 4a).
            if (Math.Abs(UpNextLabel.resolvedStyle.minWidth.value - label) > 0.01f)
            {
                UpNextLabel.style.minWidth = label;
            }

            if (Math.Abs(UpNextType.resolvedStyle.minWidth.value - type) > 0.01f)
            {
                UpNextType.style.minWidth = type;
            }

            var padding = 2 * (_upNextPad + 1);
            var natural = label + type + padding;
            if (!(natural > padding) || Math.Abs(natural - _upNextNatural) < 0.01)
            {
                return;
            }

            _upNextNatural = natural;
            if (_upNextShown && _upNextWidth.HasValue)
            {
                _upNextWidth.MoveTo(natural, MotionSpecs.UpNextWidthOmega);
            }
        }

        private static float Measure(Label label, string text)
        {
            return string.IsNullOrEmpty(text) ? 0 : label.MeasureTextSize(text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
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
