namespace RowMate.Core.Ble
{
    // El permiso de Bluetooth de Android como lo ve Core (DT10-15, QA-G10a-02). NotAsked nunca da Unauthorized: en la primera
    // instalación Dispositivo muestra la búsqueda, y el diálogo aparece al tocar "Buscar dispositivos" (AC-APP-13-02).
    public enum BlePermissionState
    {
        Granted,
        NotAsked,
        Denied,
        DeniedForever
    }
}
