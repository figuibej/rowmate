namespace RowMate.App.History
{
    // Fase de la lista del historial (F-HIS-06; Q-13). Loading = el _loading del legacy (history_provider.dart:10-34), solo
    // hasta la primera lista; LoadFailed es nuevo: el legacy queda cargando para siempre si la base falla (DV-95).
    public enum HistoryPhase
    {
        Loading,
        Ready,
        LoadFailed,
    }
}
