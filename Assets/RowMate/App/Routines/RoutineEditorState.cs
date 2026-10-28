using System.Collections.Generic;
using RowMate.Core.Routines;

namespace RowMate.App.Routines
{
    // Lo que dibuja el editor (F-RUT-11, -12, -14): los pasos de la plantilla con su clave, los bloques de la lista
    // (EditorLayout), la cuenta de Pasos (N) = pasos de la PLANTILLA (AC-RUT-11-03: una serie de 2 pasos ×3 suma 2) y si hay
    // un guardado en vuelo (Q-19). Cada cambio trae listas nuevas.
    public sealed record RoutineEditorState(IReadOnlyList<EditorStep> Steps, IReadOnlyList<EditorItem> Layout, int StepCount, bool IsSaving);
}
