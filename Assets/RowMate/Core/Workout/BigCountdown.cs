namespace RowMate.Core.Workout
{
    // La cuenta grande de los últimos 3 s (D-ANI-05; AC-WKT-14-18; 02-tech-design §2.4 de la It. 4b). Mejora EXR: sale del
    // restante del motor (StepProgress.RemainingInStep, workout_provider.dart:101-104), así cambia en la misma foto que la
    // cuenta de la barra y el reloj de la banda. La regla no tiene memoria: al reanudar vuelve si corresponde.
    public static class BigCountdown
    {
        public const int FromSeconds = 3;

        // En marcha, en un paso por tiempo (también el último) con restante 3, 2 o 1: ese número. null en pausa (ahí va PAUSA,
        // AC-WKT-13-12), por distancia o sin meta (RemainingInStep null), con 0 s o duración negativa (restante 0), en libre
        // (sin paso), en reposo y terminada.
        public static int? ValueOf(StepProgress? step, WorkoutPhase phase)
        {
            if (phase != WorkoutPhase.Active || !step.HasValue)
            {
                return null;
            }

            var remaining = step.Value.RemainingInStep;
            if (!remaining.HasValue || remaining.Value < 1 || remaining.Value > FromSeconds)
            {
                return null;
            }

            return remaining.Value;
        }
    }
}
