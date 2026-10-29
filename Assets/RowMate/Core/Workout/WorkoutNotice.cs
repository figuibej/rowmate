namespace RowMate.Core.Workout
{
    public enum WorkoutNoticeKind
    {
        // La primera falla del alta o de un lote de puntos, una sola vez mientras se rema (AC-WKT-01-11, AC-WKT-06-12).
        SaveFailed,

        // La sesión volvió a reposo (AC-WKT-01-06, -07).
        Ended,
    }

    // Un aviso del motor (02-tech-design §2.9, §2.10 de la It. 4a). WorkoutStore lo traduce a un aviso de la pantalla
    // (workoutSaveError, workoutCompleted) y lo retiene si el usuario no está delante. El legacy no avisa nada (DV-29).
    public readonly struct WorkoutNotice
    {
        private WorkoutNotice(WorkoutNoticeKind kind, WorkoutEndKind end, bool saved)
        {
            Kind = kind;
            End = end;
            Saved = saved;
        }

        public WorkoutNoticeKind Kind { get; }

        // Con Ended: cómo terminó.
        public WorkoutEndKind End { get; }

        // Con Ended: la sesión quedó cerrada en la base con todos sus puntos (el alta y la transacción del cierre bien; un lote
        // que falló cuenta como guardado si entró en el reintento del cierre: DT4a-17).
        public bool Saved { get; }

        public static WorkoutNotice SaveFailed()
        {
            return new WorkoutNotice(WorkoutNoticeKind.SaveFailed, WorkoutEndKind.Manual, false);
        }

        public static WorkoutNotice Ended(WorkoutEndKind end, bool saved)
        {
            return new WorkoutNotice(WorkoutNoticeKind.Ended, end, saved);
        }

        public override string ToString()
        {
            return Kind == WorkoutNoticeKind.SaveFailed ? "SaveFailed" : "Ended(" + End + ", " + (Saved ? "true" : "false") + ")";
        }
    }
}
