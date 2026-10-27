using System.Collections;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Device;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // Pestaña Dispositivo con el remo de prueba (F-BLE-05, -07, -11; 02-tech-design §3.3).
    public sealed class DeviceScreenTests : UiTestBase
    {
        private static readonly RowingData Image01 = new RowingData { StrokeRate = 23.5, PowerWatts = 149, DistanceMeters = 652, Pace500mSeconds = 133 };

        private IEnumerator Connected(DeviceProfile profile, RowingData? data = null)
        {
            Create(new UiHostOptions { Profile = profile });
            H.TestRower!.ConnectedName = "Remo de prueba";
            H.TestRower.Emit(BleStatus.Connected);
            if (data != null)
            {
                H.TestRower.EmitData(data);
            }

            yield return Settle();
        }

        private string? Tile(MetricKind kind)
        {
            return H.DeviceScreen!.Connected.Grid.Find(kind)?.Value.Text;
        }

        [UnityTest, Category("AC-BLE-11-01")]
        public IEnumerator ViewSwitchesWithStatus()
        {
            Create(new UiHostOptions());
            yield return Frames();
            var screen = H.DeviceScreen!;
            Assert.That(screen.View, Is.EqualTo(DeviceViewKind.Scan));
            foreach (var status in new[] { BleStatus.Connected, BleStatus.Scanning, BleStatus.Connected, BleStatus.Connecting, BleStatus.Disconnected })
            {
                H.TestRower!.Emit(status);
                yield return Settle();
                var expected = status == BleStatus.Connected ? DeviceViewKind.Connected : DeviceViewKind.Scan;
                Assert.That(screen.View, Is.EqualTo(expected), status.ToString());
                Assert.That(H.IsShown("device-connected-view"), Is.EqualTo(expected == DeviceViewKind.Connected), status + ": conectada");
                Assert.That(H.IsShown("device-scan-view"), Is.EqualTo(expected == DeviceViewKind.Scan), status + ": búsqueda");
            }
        }

        [UnityTest, Category("AC-BLE-07-01")]
        public IEnumerator DisconnectActionVisibility()
        {
            Create(new UiHostOptions());
            yield return Frames();
            Assert.That(H.IsShown("device-disconnect"), Is.False);
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            Assert.That(H.IsShown("device-disconnect"), Is.True);
            Assert.That(H.TextOf("device-disconnect"), Is.EqualTo("Desconectar"));
            Assert.That(H.Find("device-disconnect")!.Q<IconView>().Codepoint, Is.EqualTo(Icons.BluetoothDisabled));
            yield return Tap("device-disconnect");
            Assert.That(H.TestRower.DisconnectCalls, Is.EqualTo(1), "sin confirmación");
        }

        [UnityTest, Category("AC-BLE-11-02")]
        public IEnumerator HeaderIconNameAndBadge()
        {
            yield return Connected(DeviceProfile.TV);
            Assert.That(H.TextOf("device-name"), Is.EqualTo("Remo de prueba"));
            var header = H.Find("device-header")!;
            Assert.That(header.Q<IconView>(className: "rm-device-header__icon").Codepoint, Is.EqualTo(Icons.Rowing));
            Assert.That(header.Q<IconView>(className: "rm-device-header__icon").resolvedStyle.width, Is.EqualTo(32).Within(0.5f));
            var badge = H.Find("device-connected-badge")!;
            Assert.That(UiQuery.TextOf(badge.Q<Label>(className: "rm-badge__label")), Is.EqualTo("Conectado"));
            Assert.That(badge.Q<IconView>().resolvedStyle.width, Is.EqualTo(12).Within(0.5f));
            Assert.That(badge.resolvedStyle.borderTopLeftRadius, Is.EqualTo(12).Within(0.5f));
            H.TestRower!.ConnectedName = null;
            H.TestRower.Emit(BleStatus.Connected);
            yield return Settle();
            Assert.That(H.TextOf("device-name"), Is.EqualTo("Rower"), "sin nombre: Rower (device_screen.dart:98)");
        }

        [UnityTest, Category("AC-BLE-11-03")]
        public IEnumerator FourTilesInOrder()
        {
            yield return Connected(DeviceProfile.TV, Image01);
            var tiles = H.DeviceScreen!.Connected.Grid.Tiles;
            Assert.That(tiles.Count, Is.EqualTo(4));
            Assert.That(tiles[0].Kind, Is.EqualTo(MetricKind.Split));
            Assert.That(tiles[1].Kind, Is.EqualTo(MetricKind.Spm));
            Assert.That(tiles[2].Kind, Is.EqualTo(MetricKind.Watts));
            Assert.That(tiles[3].Kind, Is.EqualTo(MetricKind.Distance));
            Assert.That(tiles[0].LabelText, Is.EqualTo("SPLIT 500m"));
            Assert.That(tiles[2].LabelText, Is.EqualTo("VATIOS"));
            Assert.That(tiles[3].LabelText, Is.EqualTo("DISTANCIA"));
        }

        [UnityTest, Category("AC-BLE-11-04")]
        public IEnumerator PulseTileComesAndGoes()
        {
            yield return Connected(DeviceProfile.TV, Image01);
            Assert.That(H.Find("metric-tile-hr"), Is.Null);
            H.TestRower!.EmitData(Image01 with { HeartRate = 140 });
            yield return Settle();
            Assert.That(H.DeviceScreen!.Connected.Grid.Tiles.Count, Is.EqualTo(5));
            Assert.That(Tile(MetricKind.HeartRate), Is.EqualTo("140"));
            Assert.That(H.Find("metric-tile-hr")!.Q<IconView>().IsFilled, Is.True, "favorite relleno");
            H.TestRower.EmitData(Image01 with { HeartRate = 0 });
            yield return Settle();
            yield return Frames(3);
            Assert.That(H.Find("metric-tile-hr"), Is.Null, "con pulso 0 la tarjeta se va");
            Assert.That(H.DeviceScreen.Connected.Grid.Tiles.Count, Is.EqualTo(4));
        }

        [UnityTest, Category("AC-BLE-11-05")]
        public IEnumerator ColumnsAtProfiles()
        {
            var cases = new[] { (DeviceProfile.TV, 2, 1.3), (DeviceProfile.L640, 2, 1.3), (DeviceProfile.Dp(641, 400), 3, 1.6), (DeviceProfile.TH, 3, 1.6), (DeviceProfile.W1280, 3, 1.6) };
            foreach (var (profile, columns, ratio) in cases)
            {
                yield return Connected(profile, Image01);
                yield return Frames(2);
                var grid = H.DeviceScreen!.Connected.Grid;
                Assert.That(grid.Spec!.Columns, Is.EqualTo(columns), profile.Code);
                var tile = grid.Tiles[0];
                Assert.That(tile.layout.width / tile.layout.height, Is.EqualTo(ratio).Within(0.02), profile.Code + ": proporción");
                Assert.That(grid.layout.width, Is.EqualTo(profile.WidthDp - 40).Within(0.6), profile.Code + ": la barra de desplazamiento no ocupa ancho");
            }
        }

        [UnityTest, Category("AC-BLE-11-06")]
        public IEnumerator TileGeometryAndValueFits()
        {
            yield return Connected(DeviceProfile.TV, new RowingData { StrokeRate = 40.5, PowerWatts = 1999, DistanceMeters = 123456, Pace500mSeconds = 6001 });
            var tile = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Distance)!;
            Assert.That(tile.Value.Text, Is.EqualTo("123456m"));
            Assert.That(tile.ValueBox.FitScale, Is.LessThan(1f), "123456m se achica a 411 dp");
            Assert.That(tile.ValueBox.Fits, Is.True);
            Assert.That(tile.resolvedStyle.borderTopWidth, Is.EqualTo(3).Within(0.4f));
            Assert.That(tile.resolvedStyle.paddingLeft, Is.EqualTo(16).Within(0.01f));
            Assert.That(tile.resolvedStyle.borderTopLeftRadius, Is.EqualTo(16).Within(0.01f));
            Assert.That(tile.Value.FontSize, Is.EqualTo(52).Within(0.01f), "num.xl");
            AssertClean(H);
        }

        [UnityTest, Category("AC-BLE-11-06")]
        public IEnumerator AccentRestsAtSixtyAndFlashesOnlyWhenTheTextChanges()
        {
            yield return Connected(DeviceProfile.TV, Image01);
            yield return Settle();
            var spm = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Spm)!;
            var distance = H.DeviceScreen.Connected.Grid.Find(MetricKind.Distance)!;
            Assert.That(spm.Flash.CurrentK, Is.EqualTo(0.6).Within(1e-6), "en reposo: 60 %");
            Assert.That(spm.style.borderTopColor.value.a, Is.EqualTo(0.6f).Within(0.01f));
            H.TestRower!.EmitData(Image01 with { DistanceMeters = 660 });
            H.Handle.Tick();
            Assert.That(distance.Flash.CurrentK, Is.EqualTo(1).Within(1e-6), "el texto cambió: destello");
            Assert.That(spm.Flash.CurrentK, Is.EqualTo(0.6).Within(1e-6), "24.0 → 24.0: nada brilla");
            H.Advance(0.31);
            Assert.That(distance.Flash.CurrentK, Is.EqualTo(0.6).Within(1e-6), "vuelve al 60 % antes de 300 ms");
            yield return Frames();
        }

        [UnityTest, Category("AC-BLE-11-07")]
        public IEnumerator ConnectedViewScrollsAsOneBlockWithMargins()
        {
            yield return Connected(DeviceProfile.W1280, Image01);
            var content = H.Find<ScrollView>("device-connected-scroll")!.contentContainer;
            var header = H.Find("device-header")!;
            Assert.That(header.worldBound.yMin - content.worldBound.yMin, Is.EqualTo(24).Within(0.6f), "16 + 8 sobre la cabecera");
            Assert.That(H.Find("metric-grid")!.worldBound.xMin, Is.EqualTo(20).Within(0.6f), "20 a los costados");
            H.ScrollToEnd("device-connected-scroll");
            yield return Frames(3);
            var card = H.BoundsOf("hrm-card");
            var bar = H.BoundsOf("tab-bar");
            Assert.That(bar.yMin - card.yMax, Is.EqualTo(64).Within(1f), "64 dp bajo la tarjeta del pulsómetro hasta la barra");
        }

        [UnityTest, Category("AC-BLE-11-08")]
        public IEnumerator NoExtraMetrics()
        {
            yield return Connected(DeviceProfile.TV, Image01 with { TotalCalories = 99, ElapsedSeconds = 600, StrokeCount = 300 });
            foreach (var tile in H.DeviceScreen!.Connected.Grid.Tiles)
            {
                Assert.That(tile.Kind, Is.Not.EqualTo(MetricKind.Calories));
                Assert.That(tile.Kind, Is.Not.EqualTo(MetricKind.Time));
            }

            Assert.That(H.HasText("10:00"), Is.False);
        }

        [UnityTest, Category("AC-BLE-11-11")]
        public IEnumerator RollEndsOnTheExactValueAndRestarts()
        {
            yield return Connected(DeviceProfile.TV, Image01);
            yield return Settle();
            var watts = H.DeviceScreen!.Connected.Grid.Find(MetricKind.Watts)!.Value;
            H.TestRower!.EmitData(Image01 with { PowerWatts = 151 });
            H.Handle.Tick();
            Assert.That(watts.Text, Is.EqualTo("151"), "el texto nuevo desde el cuadro 0");
            Assert.That(watts.IsRolling, Is.True);
            H.Advance(0.09);
            H.TestRower.EmitData(Image01 with { PowerWatts = 160 });
            H.Handle.Tick();
            Assert.That(watts.Text, Is.EqualTo("160"), "otro valor: el rodillo salta al final y arranca el nuevo");
            H.Advance(0.2);
            Assert.That(watts.IsRolling, Is.False, "se asienta antes de 180 ms");
            Assert.That(watts.Text, Is.EqualTo("160"));
            yield return Frames();
        }

        [UnityTest, Category("AC-BLE-05-01")]
        public IEnumerator SearchButtonStatesWithTestRower()
        {
            Create(new UiHostOptions());
            yield return Frames();
            var search = H.Find<RmButton>("device-search")!;
            Assert.That(search.enabledSelf, Is.True);
            Assert.That(search.Label.text, Is.EqualTo("Buscar dispositivos"));
            Assert.That(search.resolvedStyle.minHeight.value, Is.EqualTo(48).Within(0.5f));
            Assert.That(search.worldBound.width, Is.EqualTo(DeviceProfile.TV.WidthDp - 32).Within(1f), "ancho completo");
            Assert.That(H.IsShown("device-search-hint"), Is.False);
            yield return Tap("device-search");
            Assert.That(H.TestRower!.StartScanCalls, Is.EqualTo(1));
            H.TestRower.Emit(BleStatus.Scanning);
            yield return Settle();
            Assert.That(search.enabledSelf, Is.False);
            Assert.That(search.Label.text, Is.EqualTo("Buscando..."));
            Assert.That(search.IsBusy, Is.True, "spinner en lugar del ícono");
            Assert.That(H.IsShown("device-search-hint"), Is.True);
        }

        [UnityTest, Category("AC-HRM-13-01")]
        public IEnumerator CardPlacementInBothViews()
        {
            Create(new UiHostOptions());
            yield return Frames();
            Assert.That(H.Find("hrm-card-slot-scan")!.Contains(H.Find("hrm-card")!), Is.True);
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            Assert.That(H.Find("hrm-card-slot-connected")!.Contains(H.Find("hrm-card")!), Is.True);
            Assert.That(H.Root.Query("hrm-card").ToList().Count, Is.EqualTo(1), "una sola instancia");
        }

        [UnityTest, Category("AC-BLE-01-05")]
        public IEnumerator GridTextsLikeImage01()
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                foreach (var culture in new[] { "es-AR", "de-DE" })
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                    yield return Connected(DeviceProfile.TV, Image01);
                    Assert.That(Tile(MetricKind.Spm), Is.EqualTo("23.5"), culture);
                    Assert.That(Tile(MetricKind.Watts), Is.EqualTo("149"), culture);
                    Assert.That(Tile(MetricKind.Distance), Is.EqualTo("652m"), culture);
                    Assert.That(Tile(MetricKind.Split), Is.EqualTo("02:13"), culture);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [UnityTest, Category("AC-APP-06-01")]
        public IEnumerator TileIconLabelAndAccentColors()
        {
            yield return Connected(DeviceProfile.TV, Image01 with { HeartRate = 140 });
            var grid = H.DeviceScreen!.Connected.Grid;
            var split = grid.Find(MetricKind.Split)!;
            AssertColor(split.Q(className: "rm-metric-tile__icon").resolvedStyle.color, Tokens.MetricSplit, "ícono al 100 %");
            AssertColor(split.Q(className: "rm-metric-tile__label").resolvedStyle.color, Tokens.MetricSplit, "etiqueta al 100 %");
            AssertColor(split.Value.resolvedStyle.color, Tokens.MetricSplit, "valor");
            var hr = grid.Find(MetricKind.HeartRate)!;
            AssertColor(hr.Q(className: "rm-metric-tile__label").resolvedStyle.color, Tokens.MetricHrHud, "PULSO en metric.hr.hud");
            AssertColor(hr.Value.resolvedStyle.color, Tokens.MetricHr, "valor de PULSO en metric.hr");
        }

        [UnityTest, Category("AC-APP-08-02")]
        public IEnumerator MetricTilesAndHrmCardShareTheCardContract()
        {
            yield return Connected(DeviceProfile.TV, Image01);
            yield return Settle();
            var tile = H.Find("metric-tile-spm")!;
            var card = H.Find("hrm-card")!;
            foreach (var element in new[] { tile, card })
            {
                Assert.That(element.ClassListContains("rm-card"), Is.True, element.name);
                AssertColor(element.resolvedStyle.backgroundColor, Tokens.SurfaceCard, element.name + ": fondo");
                Assert.That(element.resolvedStyle.borderTopLeftRadius, Is.EqualTo(16).Within(0.01f), element.name);
                Assert.That(element.resolvedStyle.borderTopWidth, Is.EqualTo(3).Within(0.4f), element.name);
                Assert.That(element.style.borderTopColor.value.a, Is.EqualTo(0.6f).Within(0.01f), element.name + ": acento al 60 %");
                Assert.That(element.Q(className: "rm-card__glow"), Is.Not.Null, element.name + ": brillo");
            }

            Assert.That(card.Q(className: "rm-card__glow").resolvedStyle.opacity, Is.EqualTo(0.084f).Within(0.002f), "brillo en reposo");
        }

        private static void AssertColor(Color actual, ColorToken token, string what)
        {
            var expected = TokenColors.ToColor(token);
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.004f), what + " (r)");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.004f), what + " (g)");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.004f), what + " (b)");
        }
    }
}
