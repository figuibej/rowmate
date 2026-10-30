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

        // SaveFailed → workoutSaveError (AC-WKT-01-11, AC-WKT-06-12); Ended(_, false) → workoutSaveError (con la rutina
        // terminada sola, después del Terminar del diálogo: AC-WKT-06-12); Ended(_, true) → nada. It. 4b (AC-WKT-03-05): sale el
        // aviso workoutCompleted, que era provisorio hasta el diálogo: ahora es el título del diálogo de completado. Al terminar a
        // mano no aparece nada de "completado" (Q-53). Con una falla pasajera que el reintento del cierre recuperó, el final es el
        // de un guardado bueno (DT4a-17).
        public static string? For(WorkoutNotice notice)
        {
            if (notice.Kind == WorkoutNoticeKind.SaveFailed)
            {
                return SaveError;
            }

            return notice.Saved ? null : SaveError;
        }
    }
}
