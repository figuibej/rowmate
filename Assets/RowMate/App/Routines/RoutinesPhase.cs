namespace RowMate.App.Routines
{
    // Fase de la colección de rutinas (F-RUT-09; Q-13). Loading = el _loading del legacy (routines_provider.dart:10-25);
    // LoadFailed es nuevo: el legacy queda cargando para siempre si la base falla (DV-16).
    public enum RoutinesPhase
    {
        Loading,
        Ready,
        LoadFailed,
    }
}
