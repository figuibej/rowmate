using System.Collections.Generic;

namespace RowMate.Core.Ble
{
    // Resultado de búsqueda BLE (lo que el filtro del legacy lee de flutter_blue_plus ScanResult):
    // identificador del equipo, nombre de plataforma y anunciado (pueden faltar), RSSI y servicios anunciados.
    // legacy: lib/core/bluetooth/scan_filter.dart:12-20 (advertisementData.serviceUuids).
    public sealed record BleScanResult(
        string DeviceId,
        string? PlatformName,
        string? AdvName,
        int Rssi,
        IReadOnlyList<BleUuid> ServiceUuids);
}
