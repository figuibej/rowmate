using System;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // Diálogo de inicio de una rutina (AC-WKT-12-07; legacy workout_screen.dart:145-167; 02-tech-design §6.2 de la It. 4a;
    // 04-design-notes §4.2): el diálogo de la app de la It. 3, opaco, ancho máximo 400 (331 en TV).
    // - Título = el NOMBRE de la rutina tal cual (sin texto enriquecido ni secuencias de escape, pre-wrap, como en la tarjeta);
    //   workoutStartTitle sigue sin usarse (paridad).
    // - Contenido = RoutineSummary.StartDialogContent (workoutStartContent: {summary}⏎⏎¿Iniciar el entrenamiento?), un solo
    //   texto del catálogo con pre-wrap.
    // - #dialog-cancel (cancel, de texto) y #dialog-confirm (workoutStart, primario relleno). Cancelar, tocar afuera, Escape y
    //   "Atrás" cierran sin iniciar (DialogLayer). Confirmar: quien lo abre llama WorkoutStore.StartRoutine sin mirar la conexión
    //   (AC-BLE-10-06). Dos toques en Iniciar dan una sesión: el diálogo se cierra con el primero y su velo se traga el segundo
    //   (DT-21).
    public sealed class StartWorkoutDialog : ModalDialog
    {
        public const string DialogName = "workout-start-dialog";

        public StartWorkoutDialog(UiContext ui, Routine routine)
            : base(ui, DialogName, DeleteAndRepeatMaxWidthDp)
        {
            Routine = routine ?? throw new ArgumentNullException(nameof(routine));
            var texts = ui.Texts;
            Title.enableRichText = false;
            Title.parseEscapeSequences = false;
            Title.AddToClassList("rm-user-text");
            Title.text = DisplayText.ForLabel(routine.Name);
            Message = RoutineCardView.UserText("rm-dialog__message", "rm-type-body");
            Message.name = "dialog-message";
            // Ajusta en varias líneas y conserva los saltos del texto (la letra a 1,3×: AC-APP-05-05).
            Message.AddToClassList("rm-workout-dialog__message");
            Message.text = RoutineSummary.StartDialogContent(routine, texts);
            Content.Add(Message);
            CancelButton = AddAction("dialog-cancel", "text", texts.Get(L10nKeys.Cancel), Cancel);
            ConfirmButton = AddAction("dialog-confirm", "primary", texts.Get(L10nKeys.WorkoutStart), Accept);
        }

        public Routine Routine { get; }

        public Label Message { get; }

        public RmButton CancelButton { get; }

        public RmButton ConfirmButton { get; }
    }
}
