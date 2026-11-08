namespace RowMate.Core.Ble
{
    // El sistema negó el permiso de Bluetooth (Android). No es un error de búsqueda: Dispositivo muestra la vista "sin permiso"
    // (DV-100, AC-APP-13-02), nunca el banner "Error al escanear".
    public sealed class BlePermissionDeniedException : BleException
    {
        // Código propio del plugin de Android para "sin permiso" (SecurityException), el mismo en ScanFailed y RequestEnable.
        public const int PermissionCode = -2;

        public BlePermissionDeniedException(string operation, string detail = "permission denied")
            : base(operation, detail, PermissionCode)
        {
        }
    }
}
