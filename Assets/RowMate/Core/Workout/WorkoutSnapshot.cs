using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Una foto del motor por aviso (= cada notifyListeners del legacy; 02-tech-design §2.10 de la It. 4a). Revision sube
    // siempre: dos lecturas iguales también avisan (LT-35 cuenta 2). Data es la lectura fusionada (en todas las fases);
    // TotalElapsedSeconds, el reloj del HUD (AC-WKT-02-03); Step, el estado del paso (solo con rutina y fuera de reposo);
    // IsSaving, terminada con el cierre en vuelo. En segundo plano no se publica ninguna (QA-G4aT-07): la primera después de un
    // tramo sin publicar lleva Instant = true y la vista la aplica sin animar (AC-WKT-02-08).
    public sealed record WorkoutSnapshot(long Revision, WorkoutPhase Phase, RowingData Data, int TotalElapsedSeconds, StepProgress? Step,
                                         bool IsRoutine, bool IsSaving, bool Instant)
    {
        public static readonly WorkoutSnapshot Initial = new WorkoutSnapshot(0, WorkoutPhase.Idle, RowingData.Empty, 0, null, false, false, false);
    }
}
