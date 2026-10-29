namespace RowMate.Core.Workout
{
    // Las cuatro fases de la sesión (F-WKT-01; AC-WKT-01-01): reposo, en marcha, en pausa y terminada. La app abre en reposo.
    // legacy: lib/features/workout/workout_provider.dart:13
    public enum WorkoutPhase
    {
        Idle,
        Active,
        Paused,
        Finished,
    }
}
