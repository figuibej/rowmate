using System;
using System.Collections.Generic;
using RowMate.App.Routines;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Diálogo de paso (F-RUT-13; legacy _StepDialog, routine_editor_screen.dart:474-739; 03-rutinas/02-tech-design §4.5;
    // 04-design-notes §4.11), de arriba abajo:
    //   #step-preview (StepRowView sin acciones): fija bajo el título (.rm-dialog__pinned) si el alto máximo del diálogo es
    //     ≥ 560 (TV, W1280); si no (L640, TH), primer elemento del contenido (DN3-10)
    //   rótulo editorStepType + #step-type-{warmup,work,rest,cooldown} en mosaicos (DV-09): 2 × 2 con menos de 400 dp de
    //     contenido, 4 en fila desde 400 (DN3-08)
    //   #step-mode-time / #step-mode-distance; #step-min + #step-sec (error #step-time-error) o #step-meters
    //   divisor + editorOptionalTargets; #step-wmin + #step-wmax; #step-spm; #step-split-min + #step-split-sec (#step-split-error)
    //   solo en una serie: divisor + editorProgressions; #step-prog-spm, #step-prog-watts, #step-prog-split (con ayuda, DV-19)
    //   #dialog-cancel, #dialog-confirm
    // Cada campo escribe en StepDialogForm; la vista previa se rearma con Preview() en cada cambio (AC-RUT-13-16); Confirmar
    // llama a Confirm(): con errores queda abierto, marca cada campo (los pares con las dos cajas y un texto), borra el error
    // de un campo en cuanto cambia y lleva el foco al PRIMER campo con error, desplazando hasta mostrarlo con su texto
    // (AC-RUT-13-11, DN3-10). En un campo, Enter no confirma (paridad). Ancho máximo 560.
    public sealed class StepDialog : ModalDialog
    {
        public const double PinnedPreviewFromDp = 560;
        public const double FourTilesFromDp = 400;

        private readonly StepDialogForm _form;
        private readonly Dictionary<StepField, RmTextField[]> _fields = new Dictionary<StepField, RmTextField[]>();
        private readonly Dictionary<StepField, ErrorLine> _pairErrors = new Dictionary<StepField, ErrorLine>();
        private readonly VisualElement _timeRow;
        private readonly EnterMotion _modeIn;
        private readonly ErrorLine _timeError;
        private readonly ErrorLine _splitError;
        // null = todavía sin ubicar (la vista previa nace en .rm-dialog__pinned; OnLayout la deja donde corresponde).
        private bool? _pinned;

        public StepDialog(UiContext ui, StepDialogForm form)
            : base(ui, "dialog-step", StepMaxWidthDp)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            var texts = ui.Texts;
            Title.text = texts.Get(form.IsNew ? L10nKeys.EditorNewStep : L10nKeys.EditorEditStep);
            _modeIn = new EnterMotion(ui.Motion);

            Preview = new StepRowView(ui, true, null, null) { name = "step-preview" };
            Pinned.Add(Preview);

            TypeLabel = Caption("step-type-label", texts.Get(L10nKeys.EditorStepType));
            Content.Add(TypeLabel);
            Types = new SegmentedControl(true, ui.Motion) { name = "step-type" };
            Types.AddToClassList("rm-step-dialog__types");
            var order = StepTypeInfo.SelectorOrder;
            for (var i = 0; i < order.Count; i++)
            {
                var type = order[i];
                Types.AddOption("step-type-" + StepTypeNames.ToStorage(type), StepRowModel.IconFor(type), texts.Get(StepTypeInfo.NameKey(type)),
                    StepTypeInfo.Color(type), StepRowModel.HudColor(type));
            }

            Types.Select(IndexOf(order, form.Type), false);
            Types.Changed += index =>
            {
                _form.Type = StepTypeInfo.SelectorOrder[index];
                RefreshPreview();
            };
            Content.Add(Types);

            Mode = new SegmentedControl(false, ui.Motion) { name = "step-mode" };
            Mode.AddToClassList("rm-step-dialog__mode");
            Mode.AddOption("step-mode-time", null, texts.Get(L10nKeys.EditorByTime));
            Mode.AddOption("step-mode-distance", null, texts.Get(L10nKeys.EditorByDistance));
            Mode.Select(form.ByDistance ? 1 : 0, false);
            Mode.Changed += index =>
            {
                _form.ByDistance = index == 1;
                ApplyMode(true);
                RefreshPreview();
            };
            Content.Add(Mode);

            _timeRow = Pair(out var minutes, "step-min", L10nKeys.EditorMinutes, out var seconds, "step-sec", L10nKeys.EditorSeconds);
            _timeRow.AddToClassList("rm-step-dialog__first-row");
            Content.Add(_timeRow);
            _timeError = new ErrorLine(ui.Motion, PairError("step-time-error"));
            Content.Add(_timeError.Slot);
            Bind(StepField.Duration, minutes, () => _form.Minutes, v => _form.Minutes = v);
            Bind(StepField.Duration, seconds, () => _form.Seconds, v => _form.Seconds = v);
            _pairErrors[StepField.Duration] = _timeError;

            Meters = Field("step-meters", L10nKeys.EditorMeters);
            Meters.AddToClassList("rm-step-dialog__first-row");
            Content.Add(Meters);
            Bind(StepField.Meters, Meters, () => _form.Meters, v => _form.Meters = v);

            Content.Add(Divider());
            Content.Add(Caption("step-optional-label", texts.Get(L10nKeys.EditorOptionalTargets)));
            var watts = Pair(out var wattsMin, "step-wmin", L10nKeys.EditorWattsMin, out var wattsMax, "step-wmax", L10nKeys.EditorWattsMax);
            Content.Add(watts);
            Bind(StepField.WattsMin, wattsMin, () => _form.WattsMin, v => _form.WattsMin = v);
            Bind(StepField.WattsMax, wattsMax, () => _form.WattsMax, v => _form.WattsMax = v);
            var spm = Field("step-spm", L10nKeys.EditorTargetSpm);
            spm.AddToClassList("rm-step-dialog__row");
            Content.Add(spm);
            Bind(StepField.Spm, spm, () => _form.Spm, v => _form.Spm = v);
            var split = Pair(out var splitMin, "step-split-min", L10nKeys.EditorSplitMin, out var splitSec, "step-split-sec", L10nKeys.EditorSplitSec);
            split.AddToClassList("rm-step-dialog__row");
            Content.Add(split);
            _splitError = new ErrorLine(ui.Motion, PairError("step-split-error"));
            Content.Add(_splitError.Slot);
            Bind(StepField.Split, splitMin, () => _form.SplitMin, v => _form.SplitMin = v);
            Bind(StepField.Split, splitSec, () => _form.SplitSec, v => _form.SplitSec = v);
            _pairErrors[StepField.Split] = _splitError;

            if (form.ShowsProgressions)
            {
                Content.Add(Divider());
                Content.Add(Caption("step-progressions-label", texts.Get(L10nKeys.EditorProgressions)));
                var progSpm = Field("step-prog-spm", L10nKeys.EditorProgressionSpm, L10nKeys.EditorProgressionSpmHint);
                var progWatts = Field("step-prog-watts", L10nKeys.EditorProgressionWatts, L10nKeys.EditorProgressionWattsHint);
                var progSplit = Field("step-prog-split", L10nKeys.EditorProgressionSplit, L10nKeys.EditorProgressionSplitHint);
                progWatts.AddToClassList("rm-step-dialog__row");
                progSplit.AddToClassList("rm-step-dialog__row");
                Content.Add(progSpm);
                Content.Add(progWatts);
                Content.Add(progSplit);
                Bind(StepField.ProgressionSpm, progSpm, () => _form.ProgressionSpm, v => _form.ProgressionSpm = v);
                Bind(StepField.ProgressionWatts, progWatts, () => _form.ProgressionWatts, v => _form.ProgressionWatts = v);
                Bind(StepField.ProgressionSplit, progSplit, () => _form.ProgressionSplit, v => _form.ProgressionSplit = v);
            }

            CancelButton = AddAction("dialog-cancel", "text", texts.Get(L10nKeys.Cancel), Cancel);
            ConfirmButton = AddAction("dialog-confirm", "primary", texts.Get(L10nKeys.Confirm), OnConfirm);
            ApplyMode(false);
            RefreshPreview();
        }

        public StepDialogForm Form => _form;

        public StepRowView Preview { get; }

        public Label TypeLabel { get; }

        public SegmentedControl Types { get; }

        public SegmentedControl Mode { get; }

        public RmTextField Meters { get; }

        // Las líneas de error de los pares (#step-time-error, #step-split-error), con su ErrorLine (QA-V3-02).
        public ErrorLine TimeError => _timeError;

        public ErrorLine SplitError => _splitError;

        public RmButton CancelButton { get; }

        public RmButton ConfirmButton { get; }

        // El paso confirmado (null si se canceló o si todavía está abierto).
        public IntervalStep? Result { get; private set; }

        public IReadOnlyList<FieldError> LastErrors { get; private set; } = new FieldError[0];

        public bool PreviewIsPinned => _pinned == true;

        protected override void OnLayout(double widthDp, double maxHeightDp)
        {
            var compact = Box.ClassListContains("rm-dialog--compact");
            var contentWidth = widthDp - 2 * (compact ? 16 : 24);
            Types.SetColumns(contentWidth >= FourTilesFromDp ? 4 : 2);
            var pinned = maxHeightDp >= PinnedPreviewFromDp;
            if (_pinned == pinned)
            {
                return;
            }

            _pinned = pinned;
            if (pinned)
            {
                Pinned.Add(Preview);
                Pinned.style.display = DisplayStyle.Flex;
            }
            else
            {
                Content.Insert(0, Preview);
                Pinned.style.display = DisplayStyle.None;
            }
        }

        private void OnConfirm()
        {
            var result = _form.Confirm();
            if (result.IsValid)
            {
                Result = result.Step;
                Accept();
                return;
            }

            LastErrors = result.Errors;
            RmTextField? first = null;
            Label? firstText = null;
            for (var i = 0; i < result.Errors.Count; i++)
            {
                var error = result.Errors[i];
                var text = RoutineTexts.FieldErrorText(error, Ui.Texts);
                if (!_fields.TryGetValue(error.Field, out var fields))
                {
                    continue;
                }

                if (_pairErrors.TryGetValue(error.Field, out var pairLine))
                {
                    for (var f = 0; f < fields.Length; f++)
                    {
                        fields[f].SetBoxError(true);
                    }

                    pairLine.Set(text);
                    first ??= fields[0];
                    firstText ??= pairLine.Label;
                }
                else
                {
                    fields[0].SetError(text);
                    first ??= fields[0];
                    firstText ??= fields[0].ErrorLabel;
                }
            }

            if (first != null)
            {
                first.FocusInput(Ui.IsDesktop);
                var target = first;
                var below = firstText;
                // Con el texto de error ya en el layout: el primer error a la vista con 16 dp de aire (ω 22).
                EventCallback<GeometryChangedEvent>? callback = null;
                callback = _ =>
                {
                    Content.UnregisterCallback(callback!);
                    Scroller.ScrollIntoView(target, MotionSpecs.ScrollToFocusOmega, below);
                };
                Content.RegisterCallback(callback);
                Scroller.ScrollIntoView(target, MotionSpecs.ScrollToFocusOmega, below);
            }
        }

        private void ApplyMode(bool animate)
        {
            var distance = _form.ByDistance;
            _timeRow.style.display = distance ? DisplayStyle.None : DisplayStyle.Flex;
            // La línea del par Min/Seg se va y vuelve con su fila, en el acto (el campo que se va desaparece en el acto, §5.7).
            _timeError.SetVisible(!distance);
            Meters.style.display = distance ? DisplayStyle.Flex : DisplayStyle.None;
            if (animate)
            {
                _modeIn.Play(distance ? (VisualElement)Meters : _timeRow, 0, MotionSpecs.ModeSwapInMs, 0);
            }
        }

        private void RefreshPreview()
        {
            Preview.Render(_form.Preview(), Ui.Texts);
        }

        private void Bind(StepField field, RmTextField input, Func<string> read, Action<string> write)
        {
            input.Value = read();
            input.Changed += text =>
            {
                write(text);
                ClearError(field);
                RefreshPreview();
            };
            if (_fields.TryGetValue(field, out var existing))
            {
                var list = new RmTextField[existing.Length + 1];
                existing.CopyTo(list, 0);
                list[existing.Length] = input;
                _fields[field] = list;
            }
            else
            {
                _fields[field] = new[] { input };
            }
        }

        private void ClearError(StepField field)
        {
            if (_fields.TryGetValue(field, out var fields))
            {
                for (var i = 0; i < fields.Length; i++)
                {
                    fields[i].ClearError();
                }
            }

            if (_pairErrors.TryGetValue(field, out var line))
            {
                line.Set(null);
            }
        }

        private RmTextField Field(string name, string labelKey, string? hintKey = null)
        {
            var field = new RmTextField(Ui, name, false, true);
            field.SetLabel(Ui.Texts.Get(labelKey));
            if (hintKey != null)
            {
                field.Placeholder = Ui.Texts.Get(hintKey);
            }

            return field;
        }

        private VisualElement Pair(out RmTextField first, string firstName, string firstLabel, out RmTextField second, string secondName, string secondLabel)
        {
            var row = new VisualElement();
            row.AddToClassList("rm-field-pair");
            row.pickingMode = PickingMode.Ignore;
            first = Field(firstName, firstLabel);
            second = Field(secondName, secondLabel);
            first.AddToClassList("rm-field-pair__item");
            second.AddToClassList("rm-field-pair__item");
            second.style.marginLeft = 8;
            row.Add(first);
            row.Add(second);
            return row;
        }

        private static Label PairError(string name)
        {
            var label = new Label { name = name };
            label.AddToClassList("rm-field__error");
            label.AddToClassList("rm-field-pair__error");
            label.AddToClassList("rm-type-body-xs");
            label.pickingMode = PickingMode.Ignore;
            label.style.display = DisplayStyle.None;
            return label;
        }

        private static Label Caption(string name, string text)
        {
            var label = new Label(text) { name = name };
            label.AddToClassList("rm-step-dialog__caption");
            label.AddToClassList("rm-type-body-xs");
            label.AddToClassList("rm-text--tertiary");
            label.pickingMode = PickingMode.Ignore;
            return label;
        }

        private static VisualElement Divider()
        {
            var divider = new VisualElement();
            divider.AddToClassList("rm-divider");
            divider.AddToClassList("rm-step-dialog__divider");
            divider.pickingMode = PickingMode.Ignore;
            return divider;
        }

        private static int IndexOf(IReadOnlyList<StepType> order, StepType type)
        {
            for (var i = 0; i < order.Count; i++)
            {
                if (order[i] == type)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
