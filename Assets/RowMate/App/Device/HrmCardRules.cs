using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Localization;
using RowMate.Core.Text;

namespace RowMate.App.Device
{
    // Reglas de la tarjeta "Pulsómetro" con los textos ya resueltos (F-HRM-13; 02-tech-design §2.5).
    // legacy: lib/features/device/heart_rate_card.dart:13-238
    public static class HrmCardRules
    {
        // ToUpperInvariant = toUpperCase de Dart (no depende del idioma del equipo).
        public static HrmCardViewModel Build(HeartRateState hr, BleStatus rowerStatus, Localizer texts)
        {
            if (hr == null)
            {
                throw new ArgumentNullException(nameof(hr));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            // :22-24: el remo buscando o conectando frena Buscar, Buscar otro y Conectar.
            var rowerBusy = rowerStatus == BleStatus.Scanning || rowerStatus == BleStatus.Connecting;

            var rows = new List<HrmDeviceRow>(hr.ScanResults.Count);
            for (var i = 0; i < hr.ScanResults.Count; i++)
            {
                var result = hr.ScanResults[i];
                rows.Add(new HrmDeviceRow(result.DeviceId, RowTitle(result, texts), result.DeviceId, "hrm-device-" + result.DeviceId));
            }

            // :199, :203: "${hr.rememberedDeviceName} · $suffix".
            var suffix = texts.Get(hr.IsRetrying ? L10nKeys.HrmReconnecting : L10nKeys.HrmNotFound);
            var rememberedLine = (hr.RememberedDeviceName ?? "") + " · " + suffix;

            return new HrmCardViewModel(
                Title: texts.Get(L10nKeys.HrmTitle).ToUpperInvariant(),
                HeartFull: hr.IsConnected,                                   // :39-40
                HeartBeating: hr.IsConnected && hr.Bpm > 0,
                Bpm: hr.Bpm,
                Body: BodyFor(hr),
                ConnectedName: hr.ConnectedDeviceName ?? "",                 // :158
                BpmText: BpmText(hr.Bpm),                                    // :171
                BpmUnit: texts.Get(L10nKeys.HrmBpm),
                ConnectingText: texts.Format(L10nKeys.HrmConnectingTo, MessageArg.Text("name", hr.ConnectingName)),   // :84
                SearchingText: texts.Get(L10nKeys.HrmSearching),
                Rows: rows,
                RememberedLine: rememberedLine,
                NoSensorText: texts.Get(L10nKeys.HrmNoSensor),
                WatchHint: texts.Get(L10nKeys.HrmWatchHint),
                SearchEnabled: !rowerBusy,                                   // :108, :218
                ConnectEnabled: !(hr.IsRetrying || rowerBusy),               // :212
                SearchLabel: texts.Get(L10nKeys.HrmSearch),
                SearchOtherLabel: texts.Get(L10nKeys.HrmSearchOther),
                ConnectLabel: texts.Get(L10nKeys.HrmConnect),
                ForgetLabel: texts.Get(L10nKeys.HrmForget),
                DisconnectLabel: texts.Get(L10nKeys.HrmDisconnect),
                ErrorText: ErrorText(hr.Error, texts));
        }

        // :68-94, prioridad de AC-HRM-13-03.
        public static HrmCardBody BodyFor(HeartRateState hr)
        {
            if (hr == null)
            {
                throw new ArgumentNullException(nameof(hr));
            }

            switch (hr.Status)
            {
                case HrmStatus.Connected:
                    return HrmCardBody.Connected;
                case HrmStatus.Connecting:
                    // Reintento automático: "recordado · reconectando..." con Olvidar / Buscar otro (:73-78).
                    return hr.IsRetrying && hr.HasRemembered ? HrmCardBody.Remembered : HrmCardBody.Connecting;
                case HrmStatus.Scanning:
                    return HrmCardBody.Scanning;
                default:
                    return hr.HasRemembered ? HrmCardBody.Remembered : HrmCardBody.NoSensor;
            }
        }

        // :232-237: nombre del sistema; si está vacío, el anunciado; si tampoco, hrmUnknownSensor (AC-HRM-13-05).
        public static string RowTitle(BleScanResult result, Localizer texts)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (!string.IsNullOrEmpty(result.PlatformName))
            {
                return result.PlatformName!;
            }

            return !string.IsNullOrEmpty(result.AdvName) ? result.AdvName! : texts.Get(L10nKeys.HrmUnknownSensor);
        }

        // :171: el pulso como entero sin separadores, o "--" si es 0 (sin lectura, AC-HRM-04-03).
        public static string BpmText(int bpm)
        {
            return bpm > 0 ? Fmt.Integer(bpm) : "--";
        }

        // :52-62 (AC-HRM-13-07): incompatible → hrmIncompatible; si no, hrmConnectError con el mensaje de la excepción.
        // El legacy interpola el toString() de Dart ('${hr.error}', con el prefijo del tipo); el port usa Message (D-25).
        public static string? ErrorText(Exception? error, Localizer texts)
        {
            if (error == null)
            {
                return null;
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            return HeartRateStore.IsIncompatible(error)
                ? texts.Get(L10nKeys.HrmIncompatible)
                : texts.Format(L10nKeys.HrmConnectError, MessageArg.Text("error", error.Message ?? ""));
        }
    }
}
