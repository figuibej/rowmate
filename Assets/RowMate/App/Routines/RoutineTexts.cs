using System;
using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.App.Routines
{
    // Textos de Rutinas y del editor que arma el código (Q-01: todo sale del catálogo, en el idioma de la app).
    public static class RoutineTexts
    {
        // Pasos (N) / Steps (N): la cantidad de pasos de la PLANTILLA (AC-RUT-11-03).
        public static string StepsCount(int count, Localizer l10n)
        {
            return Check(l10n).Format(L10nKeys.EditorStepsCount, MessageArg.Number("count", count));
        }

        // ×N del chip de la serie, igual en es y en (AC-RUT-14-03): ×1 sin repeticiones guardadas, ×0 con una serie ×0.
        public static string RepeatChip(int count, Localizer l10n)
        {
            return Check(l10n).Format(L10nKeys.EditorRepeatCount, MessageArg.Number("count", count));
        }

        // ¿Eliminar "nombre"? con el nombre tal cual se escribió (AC-RUT-10-07).
        public static string DeleteContent(Routine routine, Localizer l10n)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            return Check(l10n).Format(L10nKeys.RoutineDeleteContent, MessageArg.Text("name", routine.Name));
        }

        // Texto debajo de un campo del diálogo de paso: editorErrorRange o editorErrorRangeRep (AC-RUT-13-11, -12).
        public static string FieldErrorText(FieldError error, Localizer l10n)
        {
            Check(l10n);
            if (error.Repetition.HasValue)
            {
                return l10n.Format(L10nKeys.EditorErrorRangeRep, MessageArg.Number("rep", error.Repetition.Value),
                    MessageArg.Text("max", error.MaxText));
            }

            return l10n.Format(L10nKeys.EditorErrorRange, MessageArg.Text("max", error.MaxText));
        }

        // Texto del diálogo de repeticiones que queda abierto (Máximo 999, Con ×492, un objetivo queda fuera de rango), o
        // null si el diálogo se cierra (AC-RUT-14-05, -06).
        public static string? RepeatError(RepeatChange change, Localizer l10n)
        {
            Check(l10n);
            switch (change.Kind)
            {
                case RepeatChangeKind.TooMany:
                    return l10n.Format(L10nKeys.EditorRepetitionsMax, MessageArg.Number("max", change.Count));
                case RepeatChangeKind.TargetOutOfRange:
                    return l10n.Format(L10nKeys.EditorRepetitionsOutOfRange, MessageArg.Number("count", change.Count));
                default:
                    return null;
            }
        }

        // Aviso de Guardar (AC-RUT-11-05, AC-RUT-09-05), o null si no hay aviso (Saved, Ignored).
        public static string? SaveToast(SaveOutcome outcome, Localizer l10n)
        {
            Check(l10n);
            switch (outcome)
            {
                case SaveOutcome.NameEmpty:
                    return l10n.Get(L10nKeys.EditorNameEmpty);
                case SaveOutcome.NoSteps:
                    return l10n.Get(L10nKeys.EditorNoSteps);
                case SaveOutcome.StepOutOfRange:
                    return l10n.Get(L10nKeys.EditorStepOutOfRange);
                case SaveOutcome.Failed:
                    return l10n.Get(L10nKeys.EditorSaveError);
                default:
                    return null;
            }
        }

        // Aviso del borrado que falla (AC-RUT-09-06).
        public static string DeleteFailedToast(Localizer l10n)
        {
            return Check(l10n).Get(L10nKeys.RoutineDeleteError);
        }

        private static Localizer Check(Localizer l10n)
        {
            return l10n ?? throw new ArgumentNullException(nameof(l10n));
        }
    }
}
