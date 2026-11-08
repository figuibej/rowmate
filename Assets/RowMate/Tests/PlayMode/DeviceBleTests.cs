using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Presentation.Device;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // DBT · Dispositivo con el remo REAL (RowerService, Core) sobre el adaptador falso con los equipos ficticios K-10 (10a 02-tech-design
    // §6.1, §8.3; HostBackends.FakeBle). Ninguna prueba toca la radio: el adaptador es el falso y ROWMATE_BLE=off.
    public sealed class DeviceBleTests : UiTestBase
    {
        private FakeBleAdapter Adapter => H.FakeBle!.Adapter;

        private DeviceScreenView Screen => H.DeviceScreen!;

        private IEnumerator CreateBle(BleAdapterState initial = BleAdapterState.On, Action<FakeBleAdapter>? setup = null,
                                      DeviceProfile? profile = null)
        {
            Create(new UiHostOptions { Profile = profile ?? DeviceProfile.TV, Backend = HostBackends.FakeBle(setup, initial) });
            yield return Settle();
        }

        // Busca y anuncia (en el acto) los equipos; la búsqueda sigue hasta sus 10 s.
        private IEnumerator Search(params FakeBleDevice[] devices)
        {
            yield return Tap("device-search");
            foreach (var device in devices)
            {
                Adapter.Advertise(device);
            }

            yield return Settle();
        }

        private IEnumerator ConnectRow()
        {
            yield return Search(FakeBleDevices.Row, FakeBleDevices.Anon);
            yield return Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            yield return Advance(1);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected));
        }

        [UnityTest, Category("AC-BLE-04-01")]
        public IEnumerator OffShowsTheStateViewInsteadOfScanAndHrm()
        {
            yield return CreateBle(BleAdapterState.Off);
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.BluetoothState));
            Assert.That(H.IsShown("device-state-view"), Is.True);
            Assert.That(H.IsShown("device-scan-view"), Is.False, "ni la búsqueda ni la lista");
            Assert.That(H.IsShown("hrm-card"), Is.False, "ni la tarjeta del pulsómetro");
            Assert.That(H.TextOf("device-bt-title"), Is.EqualTo("Bluetooth apagado"));
            Assert.That(H.IsShown("device-bt-enable"), Is.True);
            Assert.That(Screen.State.Icon.ClassListContains("rm-device-state__icon--danger"), Is.True, "ícono en rojo");
            yield return Tap("device-bt-enable");
            Assert.That(Adapter.RequestEnableCalls, Is.EqualTo(1), "Activar Bluetooth → RequestEnableAsync");
        }

        [UnityTest, Category("AC-BLE-04-02")]
        public IEnumerator UnauthorizedShowsTheDeniedView()
        {
            yield return CreateBle(BleAdapterState.Unauthorized);
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.BluetoothState));
            Assert.That(H.TextOf("device-bt-title"), Is.EqualTo("Permiso de Bluetooth denegado"));
            Assert.That(H.IsShown("device-bt-enable"), Is.True);
        }

        [UnityTest, Category("AC-BLE-04-06")]
        public IEnumerator UnsupportedHasNoButtonAndATertiaryIcon()
        {
            yield return CreateBle(BleAdapterState.Unsupported);
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.BluetoothState));
            Assert.That(H.TextOf("device-bt-title"), Is.EqualTo("Este equipo no tiene Bluetooth LE"));
            Assert.That(H.IsShown("device-bt-enable"), Is.False);
            Assert.That(Screen.State.Icon.ClassListContains("rm-device-state__icon--danger"), Is.False);
        }

        [UnityTest, Category("AC-BLE-04-03")]
        public IEnumerator OffToOnShowsTheScanWithoutSearching()
        {
            yield return CreateBle(BleAdapterState.Off);
            yield return Advance(1);
            Adapter.SetState(BleAdapterState.On);
            yield return Settle();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(H.IsShown("device-search"), Is.True);
            Assert.That(Adapter.ScanStarts, Is.EqualTo(0), "no busca solo");
        }

        [UnityTest, Category("AC-BLE-04-01")]
        public IEnumerator TurningOffWhileConnectedGoesToTheStateView()
        {
            yield return CreateBle();
            yield return ConnectRow();
            Adapter.SetState(BleAdapterState.Off);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.BluetoothState));
        }

        [UnityTest, Category("AC-BLE-05-04")]
        public IEnumerator SearchingShowsTheSpinnerAndTheHintForTenSeconds()
        {
            yield return CreateBle();
            yield return Tap("device-search");
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Scanning));
            Assert.That(Screen.Scan.Search.IsBusy, Is.True);
            Assert.That(Screen.Scan.Search.enabledSelf, Is.False);
            Assert.That(H.IsShown("device-search-hint"), Is.True);
            Assert.That(H.IsShown("device-results"), Is.False, "sin lista");
            Assert.That(UiQuery.IsShown(Screen.Scan.EmptyTitle), Is.False, "buscando sin resultados no se ve el vacío");
            yield return Advance(10.1);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(Screen.Scan.Search.enabledSelf, Is.True);
        }

        [UnityTest, Category("AC-BLE-05-07")]
        public IEnumerator ItemCardsKeepTheOrderAndDropNonFtms()
        {
            yield return CreateBle();
            yield return Search(FakeBleDevices.Hp, FakeBleDevices.Row, FakeBleDevices.Anon, FakeBleDevices.Bike, FakeBleDevices.Row);
            var list = Screen.Scan.Results;
            Assert.That(list.Count, Is.EqualTo(3), "los auriculares no aparecen y AMS-670B una sola vez");
            Assert.That(list.CardAt(0)!.NameLabel.text, Is.EqualTo("AMS-670B"));
            Assert.That(list.CardAt(1)!.NameLabel.text, Is.EqualTo("Dispositivo desconocido"));
            Assert.That(list.CardAt(2)!.NameLabel.text, Is.EqualTo("Bici FTMS"));
            Assert.That(list.CardAt(0)!.Line2.text, Is.EqualTo(FakeBleDevices.RowId));
            Assert.That(H.HasText("Auriculares"), Is.False);
        }

        [UnityTest, Category("AC-BLE-05-08")]
        public IEnumerator AScanFailureShowsTheBannerAndReenablesSearch()
        {
            yield return CreateBle(setup: a => a.FailNextScan(new BleException("scan", "error 2", 2)));
            yield return Tap("device-search");
            yield return Settle();
            Assert.That(Screen.ErrorBanner.IsShown, Is.True);
            Assert.That(H.TextOf("device-error-text"), Does.StartWith("Error al escanear: "));
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(Screen.Scan.Search.enabledSelf, Is.True, "Buscar no queda deshabilitado");
            yield return Tap("device-search");
            Assert.That(Screen.ErrorBanner.IsShown, Is.False, "buscar otra vez lo borra");
        }

        [UnityTest, Category("AC-BLE-06-07")]
        public IEnumerator TheTappedCardShowsTheStepsAndHoldsReady()
        {
            yield return CreateBle(setup: a => a.RespondAfter(FakeBleDevices.RowId, TimeSpan.FromSeconds(1)));
            yield return Search(FakeBleDevices.Row, FakeBleDevices.Anon);
            // Sin Settle después del toque: el cruce de los pasos y el fundido de las otras tarjetas (WP-D) son finitos y gastarían reloj.
            yield return Frames();
            H.Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            yield return Frames();
            var row = Screen.Scan.Results.CardFor(FakeBleDevices.RowId)!;
            var anon = Screen.Scan.Results.CardFor(FakeBleDevices.AnonId)!;
            Assert.That(row.Line2.text, Is.EqualTo("Conectando…"));
            Assert.That(row.ShowsSpinner && anon.ShowsSpinner, Is.True, "todas con el spinner (AC-BLE-06-01)");
            Assert.That(anon.enabledSelf, Is.False, "conectando, ninguna responde");
            Assert.That(anon.Line2.text, Is.EqualTo(FakeBleDevices.AnonId));
            H.Advance(1.0);
            yield return Frames();
            Assert.That(row.Line2.text, Is.EqualTo("Tomando control del monitor…"));
            H.Advance(0.3);
            yield return Frames();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected), "la app ya está conectada");
            Assert.That(Screen.HoldingReady, Is.True, "la vista retiene el cambio");
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(row.Line2.text, Is.EqualTo("Listo"));
            H.Advance(DeviceScreenView.ReadyHoldMs / 1000.0);
            yield return Settle();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Connected));
            Assert.That(H.TextOf("device-name"), Is.EqualTo("AMS-670B"));
        }

        [UnityTest, Category("AC-BLE-06-06")]
        public IEnumerator AConnectTimeoutShowsTheBannerClearedOnRetry()
        {
            yield return CreateBle(setup: a => a.NeverRespond(FakeBleDevices.RowId));
            yield return Search(FakeBleDevices.Row);
            yield return Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            yield return Advance(15.1);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(H.TextOf("device-error-text"), Does.StartWith("Error al conectar: "));
            Adapter.Respond(FakeBleDevices.RowId);
            yield return Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            Assert.That(Screen.ErrorBanner.IsShown, Is.False);
        }

        [UnityTest, Category("AC-BLE-07-05")]
        public IEnumerator DisconnectReturnsToTheLastList()
        {
            yield return CreateBle();
            yield return ConnectRow();
            yield return Tap("device-disconnect");
            yield return Settle();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(Screen.Scan.Results.Count, Is.EqualTo(2), "la lista de la última búsqueda sigue");
            Assert.That(Screen.Scan.LinkBanner.Kind, Is.EqualTo(LinkBannerKind.None), "la manual no reconecta");
        }

        [UnityTest, Category("AC-BLE-11-12")]
        public IEnumerator TheHeaderShowsUnknownForANamelessRower()
        {
            yield return CreateBle();
            yield return Search(FakeBleDevices.Anon);
            yield return Tap(ScanResultCard.NamePrefix + FakeBleDevices.AnonId);
            yield return Advance(1);
            yield return Settle();
            Assert.That(H.TextOf("device-name"), Is.EqualTo("Dispositivo desconocido"));
        }

        [UnityTest, Category("AC-BLE-08-05")]
        public IEnumerator TheReconnectBannerEnablesSearchBetweenAttempts()
        {
            yield return CreateBle();
            yield return ConnectRow();
            Adapter.NeverRespond(FakeBleDevices.RowId);
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Settle();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(Screen.Scan.LinkBanner.Kind, Is.EqualTo(LinkBannerKind.Reconnecting));
            Assert.That(H.TextOf("device-link-text"), Is.EqualTo("Reconectando con AMS-670B…"));
            Assert.That(Screen.Scan.LinkBanner.Indicator.Current, Is.EqualTo(RowMate.Presentation.Controls.LinkIndicator.Mode.WaitingArc));
            Assert.That(Screen.Scan.Search.enabledSelf, Is.True, "entre intentos se puede buscar");
            H.Advance(3.05);
            yield return Frames();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connecting));
            Assert.That(Screen.Scan.Search.enabledSelf, Is.False, "durante el intento, no");
            Assert.That(Screen.Scan.LinkBanner.Indicator.Current, Is.EqualTo(RowMate.Presentation.Controls.LinkIndicator.Mode.Spinner));
            yield return Tap("device-link-action");
            Assert.That(Screen.Scan.LinkBanner.Kind, Is.EqualTo(LinkBannerKind.None), "Desconectar corta los reintentos");
        }

        [UnityTest, Category("AC-BLE-09-02")]
        public IEnumerator MonitorStoppedRetriesTheSameRower()
        {
            yield return CreateBle();
            yield return ConnectRow();
            Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return Advance(6);
            yield return Settle();
            Assert.That(Screen.Scan.LinkBanner.Kind, Is.EqualTo(LinkBannerKind.MonitorStopped));
            Assert.That(H.TextOf("device-link-text"), Is.EqualTo("El monitor dejó de enviar datos"));
            var calls = Adapter.ConnectCalls(FakeBleDevices.RowId);
            yield return Tap("device-link-action");
            yield return Advance(1);
            Assert.That(Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(calls + 1), "Reintentar conecta el mismo remo");
            Assert.That(Screen.Scan.LinkBanner.Kind, Is.EqualTo(LinkBannerKind.None));
        }

        [UnityTest, Category("AC-BLE-12-01")]
        public IEnumerator LongPressOnTheHeaderOpensThePanelAndReconnectResetsIt()
        {
            yield return CreateBle();
            yield return ConnectRow();
            Assert.That(H.IsShown("ble-debug-panel"), Is.False);
            H.PointerDown("device-header");
            H.Advance(0.3);
            H.PointerUp("device-header");
            yield return Frames();
            Assert.That(H.IsShown("ble-debug-panel"), Is.False, "menos de 0,5 s no abre");
            H.PointerDown("device-header");
            H.Advance(0.55);
            yield return Frames();
            H.PointerUp("device-header");
            yield return Frames();
            Assert.That(H.IsShown("ble-debug-panel"), Is.True);
            Assert.That(H.TextOf("ble-debug-badge"), Is.EqualTo("NO DATA"));
            Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return Frames();
            Assert.That(H.TextOf("ble-debug-badge"), Is.EqualTo("RECEIVING"));
            Assert.That(H.TextOf("ble-debug-hex"), Is.EqualTo("2c 00 30 2a 01 e2 01 00 85 00 96 00"));
            Assert.That(H.TextOf("ble-debug-count"), Is.EqualTo("1"));
            Adapter.DropLink(FakeBleDevices.RowId);
            yield return Advance(3.5);
            yield return Settle();
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Connected), "reconectó");
            Assert.That(H.IsShown("ble-debug-panel"), Is.False, "al reconectar hay que repetir el gesto");
        }

        [UnityTest, Category("AC-BLE-05-09")]
        public IEnumerator RowerAndHrmScansExcludeEachOtherWithTheRealRower()
        {
            yield return CreateBle(profile: DeviceProfile.Dp(800, 600));
            Assert.That(H.Find("device-search")!.enabledSelf, Is.True);
            H.TestHeartRate!.SetStatus(RowMate.Core.Ble.HrmStatus.Scanning);
            yield return Settle();
            Assert.That(H.Find("device-search")!.enabledSelf, Is.False, "LT-29: el pulsómetro busca, el remo no");
            H.TestHeartRate.SetStatus(RowMate.Core.Ble.HrmStatus.Disconnected);
            yield return Settle();
            yield return Tap("device-search");
            Assert.That(H.Find("hrm-search")!.enabledSelf, Is.False, "LT-31: el remo busca, el pulsómetro no");
            yield return Advance(10.1);
            yield return Settle();
            Assert.That(H.Find("hrm-search")!.enabledSelf, Is.True);
        }

        [UnityTest, Category("AC-BLE-05-09")]
        public IEnumerator TheListAndTheHrmCardFitInLandscape()
        {
            yield return CreateBle(profile: DeviceProfile.L640);
            yield return Search(FakeBleDevices.Row, FakeBleDevices.Anon, FakeBleDevices.Bike);
            yield return Advance(10.1);
            yield return Settle();
            AssertClean(H);
        }
    }
}
