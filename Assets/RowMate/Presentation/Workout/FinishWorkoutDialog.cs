using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Confirmación de terminar (AC-WKT-20-03 a -07; legacy immersive_workout_screen.dart:125-147; 02-tech-design §6.8 de la
    // It. 4a; 04-design-notes §4.7): el diálogo OPACO de la app, el mismo del de inicio (QA-G4a-P05, DN4a-11; el legacy usa un
    // AlertDialog estándar): workoutFinishTitle (title 20/600, entra en una línea en TV), workoutFinishContent (body 15
    // text.secondary), #dialog-cancel (cancel, de texto) y #dialog-confirm (workoutFinish, primario relleno). NO lleva
    // .rm-dialog--hud (reservada, sin reglas, para el diálogo de completado de la 4b). Mientras está abierto la sesión sigue en
    // su fase. Si la rutina termina sola con el diálogo abierto, la página lo cancela, sin efecto (AC-WKT-20-07).
    public sealed class FinishWorkoutDialog : ModalDialog
    {
        public const string DialogName = "workout-finish-dialog";

        public FinishWorkoutDialog(UiContext ui)
            : base(ui, DialogName, DeleteAndRepeatMaxWidthDp)
        {
            var texts = ui.Texts;
            Title.text = texts.Get(L10nKeys.WorkoutFinishTitle);
            Message = RoutineCardView.UserText("rm-dialog__message", "rm-type-body");
            Message.name = "dialog-message";
            // Ajusta en varias líneas (la letra a 1,3×: AC-APP-05-05).
            Message.AddToClassList("rm-workout-dialog__message");
            Message.text = texts.Get(L10nKeys.WorkoutFinishContent);
            Content.Add(Message);
            CancelButton = AddAction("dialog-cancel", "text", texts.Get(L10nKeys.Cancel), Cancel);
            ConfirmButton = AddAction("dialog-confirm", "primary", texts.Get(L10nKeys.WorkoutFinish), Accept);
        }

        public Label Message { get; }

        public RmButton CancelButton { get; }

        public RmButton ConfirmButton { get; }
    }
}
