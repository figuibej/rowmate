namespace RowMate.Core.Models
{
    // Una sesión del historial con sus estadísticas calculadas con SUS PUNTOS, no con los totales de la fila (F-HIS-04,
    // F-HIS-07; AC-HIS-07-05, AC-HIS-19-03; legacy: HistoryProvider.statsFor, history_provider.dart:20-34). Igualdad por valor
    // (DT9-02): una recarga que trae lo mismo no cambia nada en la lista.
    public sealed record HistoryEntry(WorkoutSession Session, SessionStats Stats);
}
