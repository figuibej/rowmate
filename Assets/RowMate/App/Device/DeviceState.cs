using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Models;

namespace RowMate.App.Device
{
    // Estado del remo para la pantalla Dispositivo (DeviceStore). Inmutable: un aviso = un estado nuevo.
    // Error: la falla de la última búsqueda o conexión, guardada como el legacy; ErrorKind dice cuál (el banner de la It. 10a).
    // ConnectedDeviceName: el nombre que informa el servicio en el momento del aviso (el legacy lo lee en vivo).
    // It. 10a (02-tech-design §6.2): Link (fase, reconexión y motivo, A10-01), Diagnostics (F-BLE-12), LinkBanner y RetryDeviceId
    // (el remo del aviso de monitor apagado, DT10-06), como propiedades con valor por defecto: el constructor no cambia.
    // legacy: lib/features/device/device_provider.dart:13-22, :88-102
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

        public DeviceErrorKind ErrorKind { get; init; }

        public RowerLink Link { get; init; } = RowerLink.Idle;

        public BleDiagnostics Diagnostics { get; init; } = BleDiagnostics.Empty;

        public LinkBannerKind LinkBanner { get; init; }

        public string? RetryDeviceId { get; init; }
    }
}
