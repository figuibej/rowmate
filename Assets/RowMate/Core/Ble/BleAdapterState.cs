namespace RowMate.Core.Ble
{
    // Estado del adaptador Bluetooth del equipo (= BluetoothAdapterState de flutter_blue_plus; ARCHITECTURE §10.2).
    // Dispositivo cambia a la vista de "Bluetooth apagado / sin permiso" solo con Off y Unauthorized (It. 10a,
    // device_screen.dart:346-347); con cualquier otro valor muestra la vista de búsqueda.
    public enum BleAdapterState
    {
        Unknown,
        Unsupported,
        Unauthorized,
        Off,
        TurningOn,
        On,
        TurningOff
    }
}
