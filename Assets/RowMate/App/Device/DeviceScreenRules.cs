using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;

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

        // Nombre del remo conectado sin nombre (:98). Se localiza en la It. 10a (Q-01); con el simulador nunca se ve.
        public const string FallbackRowerName = "Rower";

        // :33-38 (AC-BLE-11-01): conectada solo con el remo conectado; la de búsqueda en cualquier otro estado.
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

        private static MetricTileSpec Tile(MetricKind kind, string labelKey, Localizer texts, string value, double numeric,
                                           string icon, bool filled, string elementName)
        {
            return new MetricTileSpec(kind, labelKey, texts.Get(labelKey), value, numeric, MetricPalette.For(kind), icon, filled, elementName);
        }
    }
}
