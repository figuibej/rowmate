namespace RowMate.App.Routines
{
    // Resultado de Guardar (AC-RUT-11-05 a -07, AC-RUT-09-03, -05). La vista: Saved → cierra el editor una vez;
    // NameEmpty, NoSteps, StepOutOfRange y Failed → aviso (editorNameEmpty, editorNoSteps, editorStepOutOfRange,
    // editorSaveError; RoutineTexts.SaveToast); Ignored → nada (un toque o un Enter mientras guarda o mientras sale).
    public enum SaveOutcome
    {
        Saved,
        Ignored,
        NameEmpty,
        NoSteps,
        StepOutOfRange,
        Failed,
    }
}
