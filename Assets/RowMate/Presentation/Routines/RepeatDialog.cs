using System;
using RowMate.App.Routines;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Shell;

namespace RowMate.Presentation.Routines
{
    // Diálogo de repeticiones (AC-RUT-14-04 a -06; legacy _changeRepeatCount, routine_editor_screen.dart:151-188;
    // 04-design-notes §4.10): título editorRepetitions; #repeat-amount (Cantidad, numérico con signo) precargado con N y con
    // el foco puesto (en la PC con todo seleccionado: escribir 4 deja 4; en el teléfono, el cursor al final); #dialog-cancel y
    // #dialog-confirm (OK). OK llama a RoutineEditorStore.ChangeRepeatCount: Applied o Unchanged cierran; TooMany y
    // TargetOutOfRange dejan el diálogo abierto con su texto en #repeat-error (Máximo 999; Con ×492, un objetivo queda fuera de
    // rango). Cambiar el campo borra el error. Cancelar, el velo, Escape y "Atrás" no cambian nada. Ancho máximo 400.
    public sealed class RepeatDialog : ModalDialog
    {
        private readonly RoutineEditorStore _store;

        public RepeatDialog(UiContext ui, RoutineEditorStore store, string groupId)
            : base(ui, "dialog-repeat", DeleteAndRepeatMaxWidthDp)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            GroupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
            var texts = ui.Texts;
            Title.text = texts.Get(L10nKeys.EditorRepetitions);
            Field = new RmTextField(ui, "repeat-amount", false, true, "repeat-error");
            Field.SetLabel(texts.Get(L10nKeys.EditorAmount));
            Field.Value = store.RepeatCountOf(groupId).ToString(System.Globalization.CultureInfo.InvariantCulture);
            Field.Changed += _ => Field.ClearError();
            Content.Add(Field);
            CancelButton = AddAction("dialog-cancel", "text", texts.Get(L10nKeys.Cancel), Cancel);
            ConfirmButton = AddAction("dialog-confirm", "primary", texts.Get(L10nKeys.Ok), Apply);
        }

        public string GroupId { get; }

        public RmTextField Field { get; }

        public RmButton CancelButton { get; }

        public RmButton ConfirmButton { get; }

        public RepeatChange? LastChange { get; private set; }

        protected override void OnOpened()
        {
            Field.FocusInput(Ui.IsDesktop);
        }

        private void Apply()
        {
            var change = _store.ChangeRepeatCount(GroupId, Field.Value);
            LastChange = change;
            if (change.ClosesTheDialog)
            {
                Accept();
                return;
            }

            Field.SetError(RoutineTexts.RepeatError(change, Ui.Texts));
            Field.FocusInput(Ui.IsDesktop);
        }
    }
}
