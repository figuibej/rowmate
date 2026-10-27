using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Device
{
    // DeviceScreenRules = las reglas de lib/features/device/device_screen.dart, con los textos ya resueltos.
    [TestFixture]
    public sealed class DeviceScreenRulesTests
    {
        [TestCase(BleStatus.Connected, DeviceViewKind.Connected, Category = "AC-BLE-11-01")]
        [TestCase(BleStatus.Disconnected, DeviceViewKind.Scan, Category = "AC-BLE-11-01")]
        [TestCase(BleStatus.Scanning, DeviceViewKind.Scan, Category = "AC-BLE-11-01")]
        [TestCase(BleStatus.Connecting, DeviceViewKind.Scan, Category = "AC-BLE-11-01")]
        [TestCase(BleStatus.Off, DeviceViewKind.Scan, Category = "AC-BLE-11-01")]
        public void ViewForEveryStatus(BleStatus status, DeviceViewKind expected)
        {
            Assert.That(DeviceScreenRules.ViewFor(status), Is.EqualTo(expected));
        }

        [Test, Category("AC-BLE-07-01")]
        public void DisconnectOnlyWhenConnected()
        {
            foreach (BleStatus status in Enum.GetValues(typeof(BleStatus)))
            {
                Assert.That(DeviceScreenRules.ShowDisconnectAction(status), Is.EqualTo(status == BleStatus.Connected), status.ToString());
            }
        }

        [TestCase(360, 2, 1.3, Category = "AC-BLE-11-05")]
        [TestCase(411, 2, 1.3, Category = "AC-BLE-11-05")]
        [TestCase(640, 2, 1.3, Category = "AC-BLE-11-05")]
        [TestCase(641, 3, 1.6, Category = "AC-BLE-11-05")]
        [TestCase(914, 3, 1.6, Category = "AC-BLE-11-05")]
        [TestCase(1280, 3, 1.6, Category = "AC-BLE-11-05")]
        public void ColumnsAndAspectByWidth(double windowWidth, int columns, double aspect)
        {
            // La regla mira el ancho de la GRILLA (ventana − 40): más de 600 → 3 columnas y 1,6; si no, 2 y 1,3.
            var grid = DeviceScreenRules.Grid(DeviceScreenRules.GridWidthForWindow(windowWidth));
            Assert.That(grid.Columns, Is.EqualTo(columns));
            Assert.That(grid.AspectRatio, Is.EqualTo(aspect));
            Assert.That(grid.Spacing, Is.EqualTo(12.0), "12 dp en los dos sentidos");
        }

        [Test, Category("AC-BLE-11-05")]
        public void GridWidthBoundaryIsSixHundred()
        {
            Assert.That(DeviceScreenRules.Grid(600).Columns, Is.EqualTo(2));
            Assert.That(DeviceScreenRules.Grid(600.01).Columns, Is.EqualTo(3));
            Assert.That(DeviceScreenRules.GridWidthForWindow(640), Is.EqualTo(600.0));
        }

        [TestCase(411.4285714285714, 179.71, 138.24, Category = "AC-BLE-11-05")] // TV: 1080 px a 2,625
        [TestCase(640, 294.0, 226.15, Category = "AC-BLE-11-05")]
        [TestCase(641, 192.33, 120.21, Category = "AC-BLE-11-05")]
        [TestCase(1280, 405.33, 253.33, Category = "AC-BLE-11-05")]
        public void CellGeometry(double windowWidth, double cellWidth, double cellHeight)
        {
            // 04-design-notes §3 (celda = (grilla − 12·(columnas − 1)) / columnas; alto = ancho / proporción).
            var gridWidth = DeviceScreenRules.GridWidthForWindow(windowWidth);
            var grid = DeviceScreenRules.Grid(gridWidth);
            Assert.That(grid.CellWidth(gridWidth), Is.EqualTo(cellWidth).Within(0.01));
            Assert.That(grid.CellHeight(gridWidth), Is.EqualTo(cellHeight).Within(0.01));
        }

        [Test, Category("AC-BLE-11-05")]
        public void GridHeightForFourAndFiveTiles()
        {
            var grid = DeviceScreenRules.Grid(1240);
            Assert.That(grid.Rows(4), Is.EqualTo(2));
            Assert.That(grid.Rows(3), Is.EqualTo(1));
            Assert.That(grid.Height(1240, 4), Is.EqualTo(2 * (1216.0 / 3 / 1.6) + 12).Within(1e-9),
                "2 filas: con la cabecera, la tarjeta del pulsómetro empieza en y ≈ 667 a 1280 × 720 (CAP-04)");
            Assert.That(DeviceScreenRules.Grid(371).Rows(5), Is.EqualTo(3));
            Assert.That(grid.Height(1240, 0), Is.EqualTo(0.0));
        }

        [Test, Category("AC-BLE-11-03")]
        public void TilesOrderLabelsIconsAndColors()
        {
            var tiles = DeviceScreenRules.Tiles(RowingData.Empty, LocalizationFixture.Es);
            Assert.That(tiles.Count, Is.EqualTo(4));
            var expected = new[]
            {
                (MetricKind.Split, "metricSplit", "SPLIT 500m", Icons.Timer, Tokens.MetricSplit, "metric-tile-split"),
                (MetricKind.Spm, "metricSpm", "SPM", Icons.Speed, Tokens.MetricSpm, "metric-tile-spm"),
                (MetricKind.Watts, "metricWatts", "VATIOS", Icons.Bolt, Tokens.MetricWatts, "metric-tile-watts"),
                (MetricKind.Distance, "metricDistance", "DISTANCIA", Icons.Straighten, Tokens.MetricDistance, "metric-tile-distance"),
            };
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(tiles[i].Kind, Is.EqualTo(expected[i].Item1));
                Assert.That(tiles[i].LabelKey, Is.EqualTo(expected[i].Item2));
                Assert.That(tiles[i].Label, Is.EqualTo(expected[i].Item3));
                Assert.That(tiles[i].IconCodepoint, Is.EqualTo(expected[i].Item4), "split con timer (como el legacy), SPM con speed");
                Assert.That(tiles[i].IconFilled, Is.False);
                Assert.That(tiles[i].Color, Is.SameAs(expected[i].Item5));
                Assert.That(tiles[i].ElementName, Is.EqualTo(expected[i].Item6));
            }

            var english = DeviceScreenRules.Tiles(RowingData.Empty, LocalizationFixture.En);
            Assert.That(new[] { english[0].Label, english[1].Label, english[2].Label, english[3].Label },
                Is.EqualTo(new[] { "SPLIT 500m", "SPM", "WATTS", "DISTANCE" }));
        }

        [Test, Category("AC-BLE-11-03"), Category("AC-BLE-01-05")]
        public void TileTextsLikeImage01()
        {
            // Los valores de docs/image-01.png del legacy: 23,5 spm, 149 W y 652 m; y ceros al conectar.
            var data = new RowingData { StrokeRate = 23.5, PowerWatts = 149, DistanceMeters = 652, Pace500mSeconds = 133 };
            var tiles = DeviceScreenRules.Tiles(data, LocalizationFixture.Es);
            Assert.That(new[] { tiles[0].ValueText, tiles[1].ValueText, tiles[2].ValueText, tiles[3].ValueText },
                Is.EqualTo(new[] { "02:13", "23.5", "149", "652m" }));
            Assert.That(tiles[0].NumericValue, Is.EqualTo(133.0), "el sentido del rodillo: segundos del split");
            Assert.That(tiles[1].NumericValue, Is.EqualTo(23.5));
            Assert.That(tiles[3].NumericValue, Is.EqualTo(652.0));

            var zeros = DeviceScreenRules.Tiles(RowingData.Empty, LocalizationFixture.Es);
            Assert.That(new[] { zeros[0].ValueText, zeros[1].ValueText, zeros[2].ValueText, zeros[3].ValueText },
                Is.EqualTo(new[] { "--:--", "0.0", "0", "0m" }));
        }

        [Test, Category("AC-BLE-11-04")]
        public void HeartRateTileOnlyWithFtmsPulse()
        {
            var withPulse = DeviceScreenRules.Tiles(new RowingData { HeartRate = 140 }, LocalizationFixture.Es);
            Assert.That(withPulse.Count, Is.EqualTo(5));
            var hr = withPulse[4];
            Assert.That(hr.Kind, Is.EqualTo(MetricKind.HeartRate));
            Assert.That(hr.Label, Is.EqualTo("PULSO"));
            Assert.That(hr.ValueText, Is.EqualTo("140"));
            Assert.That(hr.IconCodepoint, Is.EqualTo(Icons.Favorite));
            Assert.That(hr.IconFilled, Is.True);
            Assert.That(hr.Color, Is.SameAs(Tokens.MetricHr));
            Assert.That(hr.ElementName, Is.EqualTo("metric-tile-hr"));
            Assert.That(DeviceScreenRules.Tiles(new RowingData { HeartRate = 140 }, LocalizationFixture.En)[4].Label, Is.EqualTo("HR"));

            Assert.That(DeviceScreenRules.Tiles(new RowingData { HeartRate = 0 }, LocalizationFixture.Es).Count, Is.EqualTo(4),
                "pulso 0 → vuelven a ser 4");
            Assert.That(DeviceScreenRules.Tiles(new RowingData { HeartRate = -1 }, LocalizationFixture.Es).Count, Is.EqualTo(4));
        }

        [Test, Category("AC-BLE-11-08")]
        public void NoCaloriesTimeOrStrokes()
        {
            var data = new RowingData { TotalCalories = 99, ElapsedSeconds = 3600, StrokeCount = 900, HeartRate = 150 };
            foreach (var tile in DeviceScreenRules.Tiles(data, LocalizationFixture.Es))
            {
                Assert.That(tile.Kind, Is.Not.EqualTo(MetricKind.Calories));
                Assert.That(tile.Kind, Is.Not.EqualTo(MetricKind.Time));
                Assert.That(tile.ValueText, Is.Not.EqualTo("900"), "sin paladas");
            }
        }

        [Test, Category("AC-BLE-05-01")]
        public void ScanSpecSearchButton()
        {
            var idle = DeviceScreenRules.Scan(BleStatus.Disconnected, false, 0);
            Assert.That(idle.SearchEnabled, Is.True);
            Assert.That(idle.Searching, Is.False);
            Assert.That(idle.ShowSearchHint, Is.False);

            var searching = DeviceScreenRules.Scan(BleStatus.Scanning, false, 0);
            Assert.That(searching.SearchEnabled, Is.False, "deshabilitado con el remo buscando");
            Assert.That(searching.Searching, Is.True, "spinner y Buscando...");
            Assert.That(searching.ShowSearchHint, Is.True, "y la ayuda de la búsqueda");

            Assert.That(DeviceScreenRules.Scan(BleStatus.Connecting, false, 0).SearchEnabled, Is.False, "deshabilitado conectando");
            Assert.That(DeviceScreenRules.Scan(BleStatus.Off, false, 0).SearchEnabled, Is.True);
        }

        [Test, Category("AC-BLE-05-02")]
        public void EmptyStateFromTheFirstFrame()
        {
            // Sin conectar, sin búsqueda en curso y sin resultados: el estado vacío, sin haber buscado nunca.
            Assert.That(DeviceScreenRules.Scan(BleStatus.Disconnected, false, 0).ShowEmptyState, Is.True);
            Assert.That(DeviceScreenRules.Scan(BleStatus.Disconnected, true, 0).ShowEmptyState, Is.True, "aunque el pulsómetro busque");
            Assert.That(DeviceScreenRules.Scan(BleStatus.Scanning, false, 0).ShowEmptyState, Is.False);
            Assert.That(DeviceScreenRules.Scan(BleStatus.Disconnected, false, 2).ShowEmptyState, Is.False, "con resultados, la lista (It. 10a)");
        }

        [Test, Category("AC-HRM-13-08")]
        public void ScanSpecExcludesHrmScan()
        {
            Assert.That(DeviceScreenRules.Scan(BleStatus.Disconnected, true, 0).SearchEnabled, Is.False,
                "mientras el pulsómetro escanea no se puede escanear el remo");
            Assert.That(DeviceScreenRules.Scan(BleStatus.Disconnected, false, 0).SearchEnabled, Is.True, "termina la búsqueda: habilitado");
        }

        [Test]
        public void DeviceNameFallsBackToRower()
        {
            Assert.That(DeviceScreenRules.DeviceName("Remo simulado"), Is.EqualTo("Remo simulado"));
            Assert.That(DeviceScreenRules.DeviceName(null), Is.EqualTo("Rower"), "device_screen.dart:98 (se localiza en la 10a)");
        }

        [Test]
        public void ContentMarginsOfTheLegacy()
        {
            // device_screen.dart:89, :119, :121.
            Assert.That(DeviceScreenRules.ContentPaddingH, Is.EqualTo(20.0));
            Assert.That(DeviceScreenRules.ContentPaddingV, Is.EqualTo(16.0));
            Assert.That(DeviceScreenRules.HeaderGridGap, Is.EqualTo(24.0));
            Assert.That(DeviceScreenRules.GridCardGap, Is.EqualTo(12.0));
        }
    }
}
