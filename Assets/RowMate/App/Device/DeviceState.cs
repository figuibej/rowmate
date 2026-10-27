using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Models;

namespace RowMate.App.Device
{
    // Estado del remo para la pantalla Dispositivo (DeviceStore). Inmutable: un aviso = un estado nuevo.
    // Error: la falla de la última búsqueda o conexión, guardada como el legacy (el banner es de la It. 10a, D-15).
    // ConnectedDeviceName: el nombre que informa el servicio en el momento del aviso (el legacy lo lee en vivo).
    // legacy: lib/features/device/device_provider.dart:13-17, :88-95
    public sealed record DeviceState(
        BleStatus Status,
        BleAdapterState AdapterState,
        RowingData Data,
        IReadOnlyList<BleScanResult> ScanResults,
        Exception? Error,
        string? ConnectedDeviceName)
    {
        public bool IsConnected => Status == BleStatus.Connected;

        public bool IsBluetoothOn => AdapterState == BleAdapterState.On;
    }
}
