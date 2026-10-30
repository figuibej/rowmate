using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Una foto del motor por aviso (= cada notifyListeners del legacy; 02-tech-design §2.10 de la It. 4a). Revision sube
    // siempre: dos lecturas iguales también avisan (LT-35 cuenta 2). Data es la lectura fusionada (en todas las fases);
    // TotalElapsedSeconds, el reloj del HUD (AC-WKT-02-03); Step, el estado del paso (solo con rutina y fuera de reposo);
    // IsSaving, terminada con el cierre en vuelo. En segundo plano no se publica ninguna (QA-G4aT-07): la primera después de un
    // tramo sin publicar lleva Instant = true y la vista la aplica sin animar (AC-WKT-02-08).
    // It. 4b (02-tech-design §2.6): las propiedades nuevas son init, con valor por defecto (los que arman fotos en las pruebas
    // no cambian).
    public sealed record WorkoutSnapshot(long Revision, WorkoutPhase Phase, RowingData Data, int TotalElapsedSeconds, StepProgress? Step,
                                         bool IsRoutine, bool IsSaving, bool Instant)
    {
        public static readonly WorkoutSnapshot Initial = new WorkoutSnapshot(0, WorkoutPhase.Idle, RowingData.Empty, 0, null, false, false, false);

        // La generación de la sesión: sube en cada inicio y la conserva el reposo (el diálogo de completado se abre una vez por
        // sesión, AC-WKT-13-11). 0 antes de la primera.
        public long Session { get; init; }

        // Con Phase == Finished: cómo terminó (a mano o sola). null fuera de Finished.
        public WorkoutEndKind? EndedBy { get; init; }

        // Con EndedBy == Completed: los totales del punto final, fijos (AC-WKT-21-04; DV-43).
        public CompletionSummary? Completion { get; init; }

        // El estado del cierre: Pending en vuelo; Saved o Failed cuando volvió (o venció su tope); None fuera de Finished.
        public CloseStatus Close { get; init; }

        // El último paso que cerró SOLO con objetivos y no era el último (el sello, AC-WKT-09-08); null sin él.
        public StepOutcome? LastOutcome { get; init; }

        // El instante monótono (s, el reloj despierto del programador) del segundo que lo cerró; NaN sin él.
        public double LastOutcomeAt { get; init; } = double.NaN;
    }
}
