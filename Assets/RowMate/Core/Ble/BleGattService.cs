using System.Collections.Generic;

namespace RowMate.Core.Ble
{
    // Un servicio descubierto con sus características (= BluetoothService de flutter_blue_plus, ble_service.dart:205-216).
    public sealed record BleGattService(BleUuid Uuid, IReadOnlyList<BleCharacteristic> Characteristics);
}
