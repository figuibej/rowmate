namespace RowMate.Core.Models
{
    // Tipo de paso de una rutina (F-RUT-01). Orden = el del enum del legacy y el del selector del diálogo de paso
    // (legacy: lib/core/models/interval_step.dart:1; lib/features/routines/routine_editor_screen.dart).
    public enum StepType
    {
        Warmup,
        Work,
        Rest,
        Cooldown,
    }
}
