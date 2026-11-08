namespace RowMate.App.Device
{
    // Qué vista muestra Dispositivo (AC-BLE-11-01; It. 10a AC-BLE-04-01, -02, -06): la conectada con el remo conectado; la de
    // estado del Bluetooth con el adaptador apagado, sin permiso o sin Bluetooth LE; la de búsqueda en cualquier otro caso.
    public enum DeviceViewKind
    {
        Connected,
        Scan,
        BluetoothState
    }
}
