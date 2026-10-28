using System;
using RowMate.App.Routines;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Routines
{
    // Diálogo de eliminar (AC-RUT-10-07; legacy routines_screen.dart:97-115; 04-design-notes §4.10): título
    // routineDeleteTitle, contenido routineDeleteContent con el nombre TAL CUAL (sin marcas ni escapes, DN3-22), #dialog-cancel
    // (texto) y #dialog-confirm (Eliminar, relleno state.dangerFill con texto blanco). Solo Eliminar borra; Cancelar, el velo,
    // Escape y "Atrás" no cambian nada. Ancho máximo 400 (DN3-10).
    public sealed class DeleteRoutineDialog : ModalDialog
    {
        public DeleteRoutineDialog(UiContext ui, Routine routine)
            : base(ui, "dialog-delete-routine", DeleteAndRepeatMaxWidthDp)
        {
            Routine = routine ?? throw new ArgumentNullException(nameof(routine));
            var texts = ui.Texts;
            Title.text = texts.Get(L10nKeys.RoutineDeleteTitle);
            Message = RoutineCardView.UserText("rm-dialog__message", "rm-type-body");
            Message.name = "dialog-message";
            Message.text = DisplayText.ForLabel(RoutineTexts.DeleteContent(routine, texts));
            Content.Add(Message);
            CancelButton = AddAction("dialog-cancel", "text", texts.Get(L10nKeys.Cancel), Cancel);
            ConfirmButton = AddAction("dialog-confirm", "danger-fill", texts.Get(L10nKeys.Delete), Accept);
        }

        public Routine Routine { get; }

        public Label Message { get; }

        public Controls.RmButton CancelButton { get; }

        public Controls.RmButton ConfirmButton { get; }
    }
}
