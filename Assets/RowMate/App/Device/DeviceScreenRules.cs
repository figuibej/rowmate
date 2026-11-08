using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.App.Device
{
    // Reglas de la pantalla Dispositivo con los textos ya resueltos (02-tech-design §2.5): qué vista, el botón Desconectar,
    // el nombre del remo, la grilla, sus tarjetas y la vista de búsqueda. Las vistas de WP-B solo dibujan lo que esto dice.
    // legacy: lib/features/device/device_screen.dart
    public static class DeviceScreenRules
    {
        // Márgenes de la vista conectada (:89, :119, :121): 20 a los costados, 16 arriba y abajo, 24 entre la cabecera y la
        // grilla, 12 entre la grilla y la tarjeta del pulsómetro.
        public const double ContentPaddingH = 20;
        public const double ContentPaddingV = 16;
        public const double HeaderGridGap = 24;
        public const double GridCardGap = 12;

        // Ancho de grilla a partir del cual hay 3 columnas (> 600, :264) y separación entre tarjetas (:270-271).
        public const double ThreeColumnsAboveGridWidth = 600;
        public const double GridSpacing = 12;

        // Nombre del remo conectado sin nombre (:98). It. 10a: lo reemplazan deviceRowerFallback y deviceUnknown (DeviceName con
        // Localizer); queda hasta que WP-B1 cambie la vista conectada.
        public const string FallbackRowerName = "Rower";

        // :33-38 (AC-BLE-11-01): conectada solo con el remo conectado; la de búsqueda en cualquier otro estado. It. 10a: la vista
        // usa ViewFor(status, adapter); esta queda para el simulador y las pruebas de la It. 2.
        public static DeviceViewKind ViewFor(BleStatus status)
        {
            return status == BleStatus.Connected ? DeviceViewKind.Connected : DeviceViewKind.Scan;
        }

        // :20-31 (AC-BLE-07-01): el botón Desconectar de la barra superior, solo con el remo conectado.
        public static bool ShowDisconnectAction(BleStatus status)
        {
            return status == BleStatus.Connected;
        }

        // :98: p.connectedDeviceName ?? 'Rower'.
        public static string DeviceName(string? connectedName)
        {
            return connectedName ?? FallbackRowerName;
        }

        // Ancho de la grilla para un ancho de ventana: la ventana menos los 20 dp de cada costado (:87-89).
        public static double GridWidthForWindow(double windowWidthDp)
        {
            return windowWidthDp - 2 * ContentPaddingH;
        }

        // :261-276 (AC-BLE-11-05): con la grilla de más de 600 dp, 3 columnas y proporción 1,6; si no, 2 y 1,3; separación 12.
        // Recibe el ancho de la GRILLA, no el de la ventana: 640 dp de ventana → 600 → 2 columnas; 641 → 601 → 3.
        public static DeviceGridSpec Grid(double gridWidthDp)
        {
            return gridWidthDp > ThreeColumnsAboveGridWidth
                ? new DeviceGridSpec(3, 1.6, GridSpacing)
                : new DeviceGridSpec(2, 1.3, GridSpacing);
        }

        // :252-259 (AC-BLE-11-03, -04, -08): SPLIT 500m, SPM, VATIOS, DISTANCIA y, solo con pulso FTMS > 0, PULSO. El pulso
        // del pulsómetro nunca entra a la grilla. Calorías, tiempo y paladas no aparecen. Textos de AC-BLE-01-05 (RowingFormats).
        public static IReadOnlyList<MetricTileSpec> Tiles(RowingData data, Localizer texts)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            var tiles = new List<MetricTileSpec>(5)
            {
                Tile(MetricKind.Split, L10nKeys.MetricSplit, texts, RowingFormats.Split(data.Pace500mSeconds), data.Pace500mSeconds,
                    Icons.Timer, false, "metric-tile-split"),
                Tile(MetricKind.Spm, L10nKeys.MetricSpm, texts, RowingFormats.Spm(data.StrokeRate), data.StrokeRate,
                    Icons.Speed, false, "metric-tile-spm"),
                Tile(MetricKind.Watts, L10nKeys.MetricWatts, texts, RowingFormats.Watts(data.PowerWatts), data.PowerWatts,
                    Icons.Bolt, false, "metric-tile-watts"),
                Tile(MetricKind.Distance, L10nKeys.MetricDistance, texts, RowingFormats.Meters(data.DistanceMeters), data.DistanceMeters,
                    Icons.Straighten, false, "metric-tile-distance"),
            };

            if (data.HeartRate > 0)
            {
                tiles.Add(Tile(MetricKind.HeartRate, L10nKeys.MetricHeartRate, texts, RowingFormats.HeartRate(data.HeartRate), data.HeartRate,
                    Icons.Favorite, true, "metric-tile-hr"));
            }

            return tiles;
        }

        // :344-348, :397-442 (AC-BLE-05-01, -02, AC-HRM-13-08). Con el remo buscando: spinner, "Buscando..." y la ayuda. El
        // estado vacío se ve desde el primer momento sin haber buscado nunca. La lista de remos encontrados es de la It. 10a.
        public static ScanViewSpec Scan(BleStatus rower, bool hrmScanning, int resultCount)
        {
            var searching = rower == BleStatus.Scanning;
            var searchEnabled = !(searching || rower == BleStatus.Connecting || hrmScanning);
            return new ScanViewSpec(searchEnabled, searching, searching, resultCount == 0 && !searching);
        }

        // ── It. 10a (02-tech-design §6.1) ──

        // :33-38, :346-347 (AC-BLE-04-01, -02, -03, -06, AC-BLE-11-01): conectada con el remo conectado; la de estado del Bluetooth
        // con el adaptador apagado, sin permiso o sin Bluetooth LE; la de búsqueda en cualquier otro estado (desconocido,
        // encendiendo, apagando, encendido). Reemplaza a ViewFor(status) de la It. 2 (que no mira el adaptador).
        public static DeviceViewKind ViewFor(BleStatus status, BleAdapterState adapter)
        {
            if (status == BleStatus.Connected)
            {
                return DeviceViewKind.Connected;
            }

            return adapter == BleAdapterState.Off || adapter == BleAdapterState.Unauthorized || adapter == BleAdapterState.Unsupported
                ? DeviceViewKind.BluetoothState
                : DeviceViewKind.Scan;
        }

        // AC-BLE-11-12 (:98, :450-452; DV-101): el nombre del remo; sin nombre, deviceUnknown (como en la lista); sin remo asociado
        // (el simulador desconectado, la vista conectada sin enlace), deviceRowerFallback.
        public static string DeviceName(string? connectedName, bool hasDevice, Localizer texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (!string.IsNullOrEmpty(connectedName))
            {
                return connectedName!;
            }

            return texts.Get(hasDevice ? L10nKeys.DeviceUnknown : L10nKeys.DeviceRowerFallback);
        }

        // La cabecera de la vista conectada con el estado del store.
        public static string DeviceName(DeviceState state, Localizer texts)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return DeviceName(state.ConnectedDeviceName, state.Link.DeviceId != null, texts);
        }

        // :346-394 (AC-BLE-04-01, -02, -06; D10-06, D10-07): la vista de estado, o null si el adaptador no la pide.
        public static BluetoothStateSpec? BluetoothState(BleAdapterState adapter, Localizer texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            switch (adapter)
            {
                case BleAdapterState.Off:
                    return new BluetoothStateSpec(L10nKeys.DeviceBtOff, texts.Get(L10nKeys.DeviceBtOff), texts.Get(L10nKeys.DeviceBtOffHint), true,
                        texts.Get(L10nKeys.DeviceBtEnable), Icons.BluetoothDisabled, true, true);
                case BleAdapterState.Unauthorized:
                    return new BluetoothStateSpec(L10nKeys.DeviceBtDenied, texts.Get(L10nKeys.DeviceBtDenied), texts.Get(L10nKeys.DeviceBtDeniedHint), true,
                        texts.Get(L10nKeys.DeviceBtEnable), Icons.BluetoothDisabled, true, true);
                case BleAdapterState.Unsupported:
                    return new BluetoothStateSpec(L10nKeys.DeviceBleUnsupported, texts.Get(L10nKeys.DeviceBleUnsupported),
                        texts.Get(L10nKeys.DeviceBleUnsupportedHint), false, texts.Get(L10nKeys.DeviceBtEnable), Icons.BluetoothDisabled, false, false);
                default:
                    return null;
            }
        }

        // La tarjeta del pulsómetro no se ve en la vista de estado (AC-BLE-04-01, -06; spec §7).
        public static bool ShowHeartRateCard(DeviceViewKind view)
        {
            return view != DeviceViewKind.BluetoothState;
        }

        // :444-471 (AC-BLE-05-07, AC-BLE-06-01, -07; A10-04, A10-05): las tarjetas de la lista, en el orden del servicio (primer
        // descubrimiento). La tocada (el remo del enlace, sin reconexión) muestra el paso en lugar de la dirección, también en el
        // instante de "Listo" (la vista lo retiene 240 ms antes del "encender").
        public static IReadOnlyList<ScanItemSpec> ScanItems(DeviceState state, Localizer texts)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            var connecting = state.Status == BleStatus.Connecting;
            var link = state.Link;
            var items = new List<ScanItemSpec>(state.ScanResults.Count);
            for (var i = 0; i < state.ScanResults.Count; i++)
            {
                var result = state.ScanResults[i];
                var name = string.IsNullOrEmpty(result.PlatformName) ? texts.Get(L10nKeys.DeviceUnknown) : result.PlatformName!;
                var active = link.Reconnect == ReconnectPhase.None && link.DeviceId == result.DeviceId && link.Phase != ConnectPhase.None
                             && (connecting || state.Status == BleStatus.Connected);
                var line2 = active ? StepText(link.Phase, texts) : result.DeviceId;
                items.Add(new ScanItemSpec(result.DeviceId, name, line2, active, connecting, !connecting));
            }

            return items;
        }

        // AC-BLE-06-07: el texto del paso.
        public static string StepText(ConnectPhase phase, Localizer texts)
        {
            switch (phase)
            {
                case ConnectPhase.TakingControl:
                    return texts.Get(L10nKeys.DeviceStepControl);
                case ConnectPhase.Ready:
                    return texts.Get(L10nKeys.DeviceStepReady);
                default:
                    return texts.Get(L10nKeys.DeviceStepConnecting);
            }
        }

        // :355-365 (AC-BLE-05-08, AC-BLE-06-06; dp:111, :125): el texto del banner rojo, o null sin error. El detalle es el del
        // sistema, sin traducir.
        public static string? ErrorText(DeviceState state, Localizer texts)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Error == null)
            {
                return null;
            }

            var key = state.ErrorKind == DeviceErrorKind.Scan ? L10nKeys.DeviceScanError : L10nKeys.DeviceConnectError;
            return texts.Format(key, MessageArg.Text("error", state.Error.Message));
        }

        // AC-BLE-08-05, AC-BLE-09-02 (00-arte §4.1): la caja del enlace arriba de "Buscar dispositivos".
        public static LinkBannerSpec LinkBanner(DeviceState state, Localizer texts)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            switch (state.LinkBanner)
            {
                case LinkBannerKind.Reconnecting:
                {
                    var link = state.Link;
                    var waiting = link.Reconnect == ReconnectPhase.Waiting;
                    return new LinkBannerSpec(LinkBannerKind.Reconnecting, LinkNoticeText.Reconnecting(link.DeviceName, texts),
                        LinkNoticeText.Name(link.DeviceName, texts), waiting ? LinkIndicatorKind.WaitingArc : LinkIndicatorKind.Spinner,
                        waiting ? link.NextAttemptAt : null, L10nKeys.DeviceDisconnect, texts.Get(L10nKeys.DeviceDisconnect));
                }

                case LinkBannerKind.MonitorStopped:
                    return new LinkBannerSpec(LinkBannerKind.MonitorStopped, texts.Get(L10nKeys.DeviceMonitorStopped), null, LinkIndicatorKind.LinkOff,
                        null, L10nKeys.Retry, texts.Get(L10nKeys.Retry));
                default:
                    return LinkBannerSpec.None;
            }
        }

        // ds:142-218 (AC-BLE-12-02): el panel de diagnóstico, fijo en inglés.
        public static BleDebugSpec BleDebug(DeviceState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var diagnostics = state.Diagnostics;
            var receiving = diagnostics.Count > 0;
            var t = diagnostics.LastReceived;
            var time = t.HasValue
                ? TimeFormats.Pad2(t.Value.Hour) + ":" + TimeFormats.Pad2(t.Value.Minute) + ":" + TimeFormats.Pad2(t.Value.Second)
                : BleDebugSpec.NoTime;
            var data = state.Data;
            return new BleDebugSpec(receiving, receiving ? BleDebugSpec.ReceivingBadge : BleDebugSpec.NoDataBadge, Fmt.Integer(diagnostics.Count), time,
                diagnostics.Hex, Fmt.Fixed(data.StrokeRate, 1), Fmt.Integer(data.PowerWatts), Fmt.Integer(data.DistanceMeters) + "m",
                data.Pace500mFormatted);
        }

        private static MetricTileSpec Tile(MetricKind kind, string labelKey, Localizer texts, string value, double numeric,
                                           string icon, bool filled, string elementName)
        {
            return new MetricTileSpec(kind, labelKey, texts.Get(labelKey), value, numeric, MetricPalette.For(kind), icon, filled, elementName);
        }
    }
}
