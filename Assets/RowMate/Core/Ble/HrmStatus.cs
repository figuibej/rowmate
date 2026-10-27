namespace RowMate.Core.Ble
{
    // Estado del pulsómetro (F-HRM-12). Mismo orden que el legacy.
    // legacy: lib/core/bluetooth/heart_rate_service.dart:10
    public enum HrmStatus
    {
        Scanning,
        Connecting,
        Connected,
        Disconnected
    }
}
