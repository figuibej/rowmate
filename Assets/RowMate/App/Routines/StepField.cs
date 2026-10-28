namespace RowMate.App.Routines
{
    // Campos del diálogo de paso que pueden quedar en error (AC-RUT-13-11, -12), en el orden de arriba abajo del diálogo
    // (el foco va al primero con error). Duration = el par Min/Seg y Split = el par Split min/Split seg: cada par se marca
    // junto, con un solo texto debajo (#step-time-error, #step-split-error).
    public enum StepField
    {
        Duration,
        Meters,
        WattsMin,
        WattsMax,
        Spm,
        Split,
        ProgressionSpm,
        ProgressionWatts,
        ProgressionSplit,
    }
}
