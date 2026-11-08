namespace RowMate.App.History
{
    // Fase del detalle de una sesión (F-HIS-09; AC-HIS-09-03, -04). NoTelemetry: la sesión no tiene puntos (solo
    // sessionNoTelemetry, sd:125-128). LoadFailed es nuevo (Q-13, DV-95): el legacy queda en el indicador si la base falla.
    public enum SessionDetailPhase
    {
        Loading,
        Ready,
        NoTelemetry,
        LoadFailed,
    }
}
