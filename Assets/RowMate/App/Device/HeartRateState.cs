using System.Collections.Generic;
using RowMate.Core.Ble;

namespace RowMate.App.Device
{
    // Estado del pulsómetro para su tarjeta (HeartRateStore, F-HRM-12). Inmutable: un aviso = un estado nuevo. Los campos
    // que el legacy lee en vivo del servicio (reintentando, nombres) se fotografían en cada aviso, que es cuando la tarjeta
    // se redibuja. ChosenName: el nombre elegido en la lista, solo para ese intento de conexión (AC-HRM-12-02).
    // It. 11 (A11-01, DT11-15): el error va separado en tipo (ErrorKind), el nombre al empezar la acción (ErrorName) y el detalle
    // técnico sin traducir (ErrorDetail = Message); Link = el enlace del servicio (HrmLink.Idle en el simulado).
    // legacy: lib/features/device/heart_rate_provider.dart:10-54
    public sealed record HeartRateState(
        HrmStatus Status,
        int Bpm,
        IReadOnlyList<BleScanResult> ScanResults,
        HrmErrorKind ErrorKind,
        string? ErrorName,
        string? ErrorDetail,
        string? ChosenName,
        bool IsRetrying,
        string? ConnectedDeviceName,
        string? RememberedDeviceName,
        HrmLink Link)
    {
        // :42-44
        public bool IsConnected => Status == HrmStatus.Connected;

        public bool IsScanning => Status == HrmStatus.Scanning;

        public bool IsConnecting => Status == HrmStatus.Connecting;

        // :50
        public bool HasRemembered => RememberedDeviceName != null;

        // Hay un error en la tarjeta (el legacy: hr.error != null).
        public bool HasError => ErrorKind != HrmErrorKind.None;

        // Conectado y con el watchdog disparado (AC-HRM-05-03): "--" y "Sin señal del sensor".
        public bool IsStale => IsConnected && Link.IsStale;

        // Nombre a mostrar mientras conecta: el elegido en la lista o el recordado (:54).
        public string ConnectingName => ChosenName ?? RememberedDeviceName ?? "";
    }
}
