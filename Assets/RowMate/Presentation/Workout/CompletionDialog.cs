using System;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Text;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion.Hud;
using RowMate.Presentation.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El diálogo de rutina completada (F-WKT-21; AC-WKT-21-01 a -12; legacy immersive_workout_screen.dart:955-1019, el
    // _CompletedDialog; 04b-hud-exr/02-tech-design §2.6, §3.9; 04-design-notes §3.8, §4.12; DN4b-21; DV-42, DV-43, DV-47):
    //   #completion-dialog .rm-dialog .rm-dialog--completion   ModalDialog de 400, NO descartable, opaco (surface.overlay, P-08)
    //   ├── (dialog-title de la base, en display: none)
    //   ├── .rm-dialog__pinned > .rm-completion__header        fijo arriba: [check 32] 12 [título 20/600, ajusta]
    //   ├── ScrollView .rm-dialog__content                     lo que no entra se desplaza (AC-WKT-21-12)
    //   │   ├── .rm-completion__message                        workoutCompletedMessage, body 15, text.secondary
    //   │   ├── 16
    //   │   └── .rm-completion__summary                        blanco @4 %, radio 8, relleno 12; filas de 22 separadas 8
    //   │       ├── #completion-time       timer 20 accent.primary · 12 · workoutCompletedTime · el valor
    //   │       ├── #completion-distance   straighten · workoutCompletedDistance · "{n}" con la m de sufijo
    //   │       └── #completion-calories   local_fire_department · workoutCompletedCalories · kcal
    //   │           (rótulos body-m 14 text.secondary; cada valor un TabularNumber num.s 20/700 blanco que sigue la letra del
    //   │            sistema; la m, sufijo Inter 500 de 14 en text.secondary a 3, en la línea de base)
    //   └── .rm-dialog__actions > #completion-finish             RmButton primario relleno, workoutFinish (un solo botón, sin
    //                                                             Strava hasta la It. 12)
    // - No descartable (AC-WKT-21-02): el velo, Escape y "Atrás" no lo cierran (ModalDialog.Dismissible = false); el dueño sí
    //   (la página lo cierra con Cancel al volver a reposo).
    // - Los valores son los de HudCompletion: fijos (DV-43), aunque lleguen lecturas o se rote. Entra con peso: el check se
    //   estampa y se dibuja, y los totales cuentan de 0 a su valor exacto (CompletionMotion), sin rodillo, con el formato del
    //   legacy en cada cuadro. Con reducir movimiento o abierto sin transición (AC-WKT-21-09), todo final desde el primer cuadro.
    // - Terminar (AC-WKT-21-06): AcknowledgeCompletionAsync; desde el primer toque el botón queda tomado y, si la vuelta a reposo
    //   tarda más de 300 ms, muestra el spinner. Cuando el motor vuelve a reposo, la página lo cierra y sale (§3.3).
    // - El conteo se detiene con el diálogo (QA-G4bT-08 (a)): OnClosing y DetachFromPanelEvent llaman a CompletionMotion.Stop.
    public sealed class CompletionDialog : ModalDialog
    {
        public const string DialogName = "completion-dialog";
        public const float RowIconDp = 20;
        public const float RowIconGapDp = 12;
        public const float RowGapDp = 8;
        public const float SummaryAlpha = 0.04f;
        public const float UnitSizeRatio = 14f / 20f;

        private readonly HudCompletion _completion;
        private readonly Func<System.Threading.Tasks.Task> _finish;
        private readonly CompletionMotion _motion;

        // El búfer de los totales que cuentan (QA-V4b-MOT-01): cada cuadro escribe ahí, sin cadena nueva.
        private readonly char[] _digits = new char[TimeFormats.ElapsedMaxChars];
        private IDisposable? _spinnerTimer;
        private bool _finishing;

        public CompletionDialog(UiContext ui, HudCompletion completion, Func<System.Threading.Tasks.Task> finish)
            : base(ui, DialogName, DeleteAndRepeatMaxWidthDp)
        {
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
            _finish = finish ?? throw new ArgumentNullException(nameof(finish));
            Box.AddToClassList("rm-dialog--completion");
            var texts = ui.Texts;
            Title.style.display = DisplayStyle.None;

            Pinned.style.display = DisplayStyle.Flex;
            Header = new VisualElement();
            Header.AddToClassList("rm-completion__header");
            Header.pickingMode = PickingMode.Ignore;
            Header.style.flexDirection = FlexDirection.Row;
            Header.style.alignItems = Align.Center;
            Header.style.marginBottom = 12;
            Check = new CheckMarkView();
            Check.style.marginRight = 12;
            Header.Add(Check);
            HeaderTitle = new Label(texts.Get(L10nKeys.WorkoutCompleted)) { name = "completion-title" };
            HeaderTitle.AddToClassList("rm-completion__title");
            HeaderTitle.AddToClassList("rm-type-title");
            HeaderTitle.pickingMode = PickingMode.Ignore;
            HeaderTitle.style.whiteSpace = WhiteSpace.Normal;
            HeaderTitle.style.flexShrink = 1;
            HeaderTitle.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Header.Add(HeaderTitle);
            Pinned.Add(Header);

            Message = new Label(texts.Get(L10nKeys.WorkoutCompletedMessage)) { name = "completion-message" };
            Message.AddToClassList("rm-completion__message");
            Message.AddToClassList("rm-type-body");
            Message.pickingMode = PickingMode.Ignore;
            Message.style.whiteSpace = WhiteSpace.Normal;
            Message.style.color = TokenColors.ToColor(Tokens.TextSecondary);
            Message.style.marginBottom = 16;
            Content.Add(Message);

            Summary = new VisualElement();
            Summary.AddToClassList("rm-completion__summary");
            Summary.pickingMode = PickingMode.Ignore;
            Summary.style.backgroundColor = HudText.WithAlpha(Color.white, SummaryAlpha);
            Summary.style.borderTopLeftRadius = 8;
            Summary.style.borderTopRightRadius = 8;
            Summary.style.borderBottomLeftRadius = 8;
            Summary.style.borderBottomRightRadius = 8;
            Summary.style.paddingLeft = 12;
            Summary.style.paddingRight = 12;
            Summary.style.paddingTop = 12;
            Summary.style.paddingBottom = 12;
            Content.Add(Summary);

            TimeValue = AddRow("completion-time", Icons.Timer, texts.Get(L10nKeys.WorkoutCompletedTime), false);
            DistanceValue = AddRow("completion-distance", Icons.Straighten, texts.Get(L10nKeys.WorkoutCompletedDistance), true);
            CaloriesValue = AddRow("completion-calories", Icons.LocalFireDepartment, texts.Get(L10nKeys.WorkoutCompletedCalories), true);
            DistanceValue.RegisterCallback<GeometryChangedEvent>(_ => ApplyUnit());
            ApplyUnit();

            FinishButton = AddAction("completion-finish", "primary", texts.Get(L10nKeys.WorkoutFinish), OnFinish);

            _motion = new CompletionMotion(ui.Motion, ApplyVisual);
            Box.RegisterCallback<DetachFromPanelEvent>(_ => _motion.Stop());
            ShowValues(_completion.TimeValue, _completion.DistanceValue, _completion.CaloriesValue);
        }

        protected override bool Dismissible => false;

        public HudCompletion Completion => _completion;

        public VisualElement Header { get; }

        public CheckMarkView Check { get; }

        public Label HeaderTitle { get; }

        public Label Message { get; }

        public VisualElement Summary { get; }

        public TabularNumber TimeValue { get; }

        public TabularNumber DistanceValue { get; }

        public TabularNumber CaloriesValue { get; }

        public RmButton FinishButton { get; }

        public CompletionMotion Motion => _motion;

        // Terminar ya se tocó (el botón queda tomado).
        public bool IsFinishing => _finishing;

        protected override void OnOpened()
        {
            _motion.Play(OpenedInstantly);
        }

        protected override void OnClosing(bool confirmed)
        {
            _motion.Stop();
            _spinnerTimer?.Dispose();
            _spinnerTimer = null;
        }

        private TabularNumber AddRow(string name, string icon, string label, bool spaceBelow)
        {
            var row = new VisualElement { name = name };
            row.AddToClassList("rm-completion__row");
            row.pickingMode = PickingMode.Ignore;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 22;
            if (Summary.childCount > 0)
            {
                row.style.marginTop = RowGapDp;
            }

            var iconView = new IconView(icon, false, RowIconDp);
            iconView.AddToClassList("rm-completion__row-icon");
            iconView.style.color = TokenColors.ToColor(Tokens.AccentPrimary);
            iconView.style.marginRight = RowIconGapDp;
            row.Add(iconView);
            var text = new Label(label);
            text.AddToClassList("rm-completion__row-label");
            text.AddToClassList("rm-type-body-m");
            text.pickingMode = PickingMode.Ignore;
            text.style.color = TokenColors.ToColor(Tokens.TextSecondary);
            text.style.flexGrow = 1;
            text.style.flexShrink = 1;
            row.Add(text);
            var value = new TabularNumber(Ui.Motion) { name = name + "-value" };
            value.AddToClassList("rm-completion__row-value");
            value.AddToClassList("rm-type-num-s");
            value.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            value.style.marginLeft = 8;
            row.Add(value);
            Summary.Add(row);
            return value;
        }

        // La m de sufijo de la distancia (DN4b-21): Inter 500 de 14 en text.secondary, a 3, en la línea de base; crece con la
        // letra del sistema como la cifra (14/20 de su tamaño).
        private void ApplyUnit()
        {
            var size = DistanceValue.FontSize > 0 ? DistanceValue.FontSize : 20f;
            DistanceValue.SetSuffix(Ui.Texts.Get(L10nKeys.HudUnitMeters), "rm-type-caption", size * UnitSizeRatio,
                TokenColors.ToColor(Tokens.TextSecondary), (float)HudGeometry.CompletionUnitGapDp * size / 20f);
        }

        private void ApplyVisual(CompletionVisual visual)
        {
            Check.Set(visual.CheckScale, visual.CheckStroke);
            ShowValues(_motion.ValueOf(_completion.TimeValue, 0), _motion.ValueOf(_completion.DistanceValue, 1), _motion.ValueOf(_completion.CaloriesValue, 2));
        }

        // Los totales con el formato del legacy en cada cuadro, sin rodillo (cuentan: ANIMATION_BIBLE §2.5). Sin asignar (QA-V4b-MOT-01):
        // los caracteres de SessionClock y de Fmt.Integer escritos en el búfer propio; TabularNumber solo rehace las celdas si
        // cambiaron.
        private void ShowValues(long time, long distance, long calories)
        {
            TimeValue.SetValue(_digits, WorkoutFormats.WriteSessionClock(time, _digits, 0), time);
            DistanceValue.SetValue(_digits, Fmt.WriteInteger(distance, _digits, 0), distance);
            CaloriesValue.SetValue(_digits, Fmt.WriteInteger(calories, _digits, 0), calories);
        }

        private void OnFinish()
        {
            if (_finishing)
            {
                return;
            }

            _finishing = true;
            _spinnerTimer = Ui.App.Scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.FinishSpinnerDelayMs), () =>
            {
                _spinnerTimer = null;
                if (IsOpen)
                {
                    FinishButton.SetBusy(true, Ui.Motion);
                }
            });
            _finish().Forget(Ui.Logger, WorkoutEngine.Tag);
        }
    }
}
