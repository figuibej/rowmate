using System;
using System.Collections.Generic;
using RowMate.Core.Ble;

namespace RowMate.App.Device
{
    // Estado del pulsómetro para su tarjeta (HeartRateStore, F-HRM-12). Inmutable: un aviso = un estado nuevo. Los campos
    // que el legacy lee en vivo del servicio (reintentando, nombres) se fotografían en cada aviso, que es cuando la tarjeta
    // se redibuja. ChosenName: el nombre elegido en la lista, solo para ese intento de conexión (AC-HRM-12-02).
    // legacy: lib/features/device/heart_rate_provider.dart:10-54
    public sealed record HeartRateState(
        HrmStatus Status,
        int Bpm,
        IReadOnlyList<BleScanResult> ScanResults,
        Exception? Error,
        string? ChosenName,
        bool IsRetrying,
        string? ConnectedDeviceName,
        string? RememberedDeviceName)
    {
        // :42-44
        public bool IsConnected => Status == HrmStatus.Connected;

        public bool IsScanning => Status == HrmStatus.Scanning;

        public bool IsConnecting => Status == HrmStatus.Connecting;

        // :50
        public bool HasRemembered => RememberedDeviceName != null;

        // Nombre a mostrar mientras conecta: el elegido en la lista o el recordado (:54).
        public string ConnectingName => ChosenName ?? RememberedDeviceName ?? "";
    }
}
