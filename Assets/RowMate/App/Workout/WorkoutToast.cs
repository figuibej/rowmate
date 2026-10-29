using RowMate.Core.Localization;
using RowMate.Core.Workout;

namespace RowMate.App.Workout
{
    // Los avisos de Workout y del inmersivo (AC-APP-03-11): la clave del catálogo de cada uno y la traducción de los avisos
    // del motor (02-tech-design §2.11 de la It. 4a). El legacy no avisa nada (DV-29).
    public static class WorkoutToast
    {
        public const string CannotStart = L10nKeys.WorkoutRoutineCannotStart;
        public const string SaveError = L10nKeys.WorkoutSaveError;
        public const string Completed = L10nKeys.WorkoutCompleted;

        // SaveFailed → workoutSaveError (AC-WKT-01-11, AC-WKT-06-12); Ended(Completed, true) → workoutCompleted
        // (AC-WKT-03-05, provisorio hasta el diálogo de la 4b); Ended(_, false) → workoutSaveError; Ended(Manual, true) →
        // nada (Q-53: al terminar a mano no aparece nada de "completado"). Con una falla pasajera que el reintento del cierre
        // recuperó, el final es el de un guardado bueno (DT4a-17).
        public static string? For(WorkoutNotice notice)
        {
            if (notice.Kind == WorkoutNoticeKind.SaveFailed)
            {
                return SaveError;
            }

            if (!notice.Saved)
            {
                return SaveError;
            }

            return notice.End == WorkoutEndKind.Completed ? Completed : null;
        }
    }
}
