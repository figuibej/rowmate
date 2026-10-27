namespace RowMate.Core.Ble
{
    // Estado de la conexión con el remo (F-BLE-07, F-BLE-11). Mismo orden que el legacy.
    // legacy: lib/core/bluetooth/ble_service.dart:8
    public enum BleStatus
    {
        Off,
        Scanning,
        Connecting,
        Connected,
        Disconnected
    }
}
