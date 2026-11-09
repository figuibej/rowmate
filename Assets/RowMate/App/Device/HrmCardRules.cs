using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Localization;
using RowMate.Core.Text;

namespace RowMate.App.Device
{
    // Reglas de la tarjeta "Pulsómetro" con los textos ya resueltos (F-HRM-13; 02-tech-design §2.5 de la It. 2 y §3.2 de la It. 11).
    // legacy: lib/features/device/heart_rate_card.dart:13-238
    public static class HrmCardRules
    {
        // " · " + sufijo (hc:199, :203).
        public const string RememberedSeparator = " · ";

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

            var body = BodyFor(hr);
            var stale = hr.IsStale;
            var unknown = texts.Get(L10nKeys.HrmUnknownSensor);

            // A11-02 (AC-HRM-07-02, -03): el indicador del recordado.
            var indicator = HrmIndicator.None;
            double? arcStartAt = null;
            var arcDuration = TimeSpan.Zero;
            if (body == HrmCardBody.Remembered)
            {
                if (!hr.IsRetrying)
                {
                    indicator = HrmIndicator.LinkOff;
                }
                else if (hr.Link.Reconnect == ReconnectPhase.Waiting && hr.Link.NextAttemptAt.HasValue)
                {
                    indicator = HrmIndicator.WaitingArc;
                    arcStartAt = hr.Link.NextAttemptAt.Value - hr.Link.Wait.TotalSeconds;
                    arcDuration = hr.Link.Wait;
                }
                else
                {
                    indicator = HrmIndicator.Spinner;
                }
            }

            var error = Error(hr, texts);
            return new HrmCardViewModel(
                Title: texts.Get(L10nKeys.HrmTitle).ToUpperInvariant(),
                HeartFull: hr.IsConnected && !stale,                           // :39-40; AC-HRM-05-03 con la lectura vencida
                HeartBeating: hr.IsConnected && hr.Bpm > 0 && !stale,
                HeartBreathing: body == HrmCardBody.Remembered && hr.IsRetrying,   // D11-05
                Bpm: hr.Bpm,
                Body: body,
                ConnectedName: hr.ConnectedDeviceName == null ? "" : OrUnknown(hr.ConnectedDeviceName, unknown),   // :158, Q-41
                BpmText: BpmText(hr.Bpm),                                      // :171
                BpmUnit: texts.Get(L10nKeys.HrmBpm),
                StaleText: stale ? texts.Get(L10nKeys.HrmNoSignal) : null,
                ConnectingText: texts.Format(L10nKeys.HrmConnectingTo, MessageArg.Text("name", OrUnknown(hr.ConnectingName, unknown))),   // :84
                SearchingText: texts.Get(L10nKeys.HrmSearching),
                Rows: rows,
                Indicator: indicator,
                ArcStartAt: arcStartAt,
                ArcDuration: arcDuration,
                RememberedName: hr.RememberedDeviceName == null ? "" : OrUnknown(hr.RememberedDeviceName, unknown),
                RememberedSuffix: RememberedSeparator + texts.Get(hr.IsRetrying ? L10nKeys.HrmReconnecting : L10nKeys.HrmNotFound),   // :199, :203
                NoSensorText: texts.Get(L10nKeys.HrmNoSensor),
                WatchHint: texts.Get(L10nKeys.HrmWatchHint),
                SearchEnabled: !rowerBusy,                                     // :108, :218
                ConnectEnabled: !(hr.IsRetrying || rowerBusy),                 // :212
                SearchLabel: texts.Get(L10nKeys.HrmSearch),
                SearchOtherLabel: texts.Get(L10nKeys.HrmSearchOther),
                ConnectLabel: texts.Get(L10nKeys.HrmConnect),
                ForgetLabel: texts.Get(L10nKeys.HrmForget),
                DisconnectLabel: texts.Get(L10nKeys.HrmDisconnect),
                ErrorHuman: error.Human,
                ErrorDetail: error.Detail);
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

        // AC-HRM-13-14 (Q-42, DT11-15): la frase humana según el tipo y, debajo, el detalle técnico sin traducir (no en incompatible).
        // {name} de hrmConnectFailed = el nombre al empezar la acción, o el recordado, o hrmUnknownSensor. hrmConnectError queda en el
        // catálogo sin uso en la tarjeta.
        public static (string? Human, string? Detail) Error(HeartRateState hr, Localizer texts)
        {
            if (hr == null)
            {
                throw new ArgumentNullException(nameof(hr));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            switch (hr.ErrorKind)
            {
                case HrmErrorKind.Scan:
                    return (texts.Get(L10nKeys.HrmScanFailed), Detail(hr.ErrorDetail));
                case HrmErrorKind.Connect:
                    var name = !string.IsNullOrEmpty(hr.ErrorName) ? hr.ErrorName!
                        : !string.IsNullOrEmpty(hr.RememberedDeviceName) ? hr.RememberedDeviceName!
                        : texts.Get(L10nKeys.HrmUnknownSensor);
                    return (texts.Format(L10nKeys.HrmConnectFailed, MessageArg.Text("name", name)), Detail(hr.ErrorDetail));
                case HrmErrorKind.Incompatible:
                    return (texts.Get(L10nKeys.HrmIncompatible), null);
                default:
                    return (null, null);
            }
        }

        private static string? Detail(string? detail)
        {
            return string.IsNullOrEmpty(detail) ? null : detail;
        }

        // DT11-04 (Q-41): un sensor sin nombre ("") se muestra como hrmUnknownSensor, nunca con la dirección.
        private static string OrUnknown(string name, string unknown)
        {
            return name.Length > 0 ? name : unknown;
        }
    }
}
