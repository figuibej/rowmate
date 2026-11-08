using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Device
{
    // DRB: las reglas de Dispositivo de la It. 10a con los textos resueltos (02-tech-design §6.1): la vista según el adaptador, la
    // vista de estado, las tarjetas de la lista, la cabecera, la caja del enlace y el panel de diagnóstico.
    // legacy: lib/features/device/device_screen.dart:20-38, :98, :142-218, :346-471
    [TestFixture]
    public sealed class DeviceBleRulesTests
    {
        private static DeviceState State(BleStatus status = BleStatus.Disconnected, BleAdapterState adapter = BleAdapterState.On)
        {
            return new DeviceState(status, adapter, RowingData.Empty, new BleScanResult[0], null, null);
        }

        [Test, Category("AC-BLE-04-01")]
        public void OffShowsStateView()
        {
            Assert.That(DeviceScreenRules.ViewFor(BleStatus.Off, BleAdapterState.Off), Is.EqualTo(DeviceViewKind.BluetoothState));
            Assert.That(DeviceScreenRules.ViewFor(BleStatus.Disconnected, BleAdapterState.Off), Is.EqualTo(DeviceViewKind.BluetoothState));
            var es = DeviceScreenRules.BluetoothState(BleAdapterState.Off, LocalizationFixture.Es)!;
            Assert.That(es.Title, Is.EqualTo("Bluetooth apagado"));
            Assert.That(es.Hint, Is.EqualTo(LocalizationFixture.Es.Get("deviceBtOffHint")));
            Assert.That(es.ShowEnableButton, Is.True);
            Assert.That(es.EnableText, Is.EqualTo("Activar Bluetooth"));
            Assert.That(es.Icon, Is.EqualTo(Icons.BluetoothDisabled));
            Assert.That(es.IconDanger, Is.True);
            Assert.That(es.Breathes, Is.True);
            var en = DeviceScreenRules.BluetoothState(BleAdapterState.Off, LocalizationFixture.En)!;
            Assert.That(en.Title, Is.EqualTo("Bluetooth is off"));
            Assert.That(en.EnableText, Is.EqualTo("Enable Bluetooth"));
            Assert.That(DeviceScreenRules.ShowHeartRateCard(DeviceViewKind.BluetoothState), Is.False, "reemplaza la tarjeta del pulsómetro");
            Assert.That(DeviceScreenRules.ShowHeartRateCard(DeviceViewKind.Scan), Is.True);
        }

        [Test, Category("AC-BLE-04-02")]
        public void UnauthorizedShowsDenied()
        {
            Assert.That(DeviceScreenRules.ViewFor(BleStatus.Disconnected, BleAdapterState.Unauthorized), Is.EqualTo(DeviceViewKind.BluetoothState));
            var spec = DeviceScreenRules.BluetoothState(BleAdapterState.Unauthorized, LocalizationFixture.Es)!;
            Assert.That(spec.Title, Is.EqualTo("Permiso de Bluetooth denegado"));
            Assert.That(spec.Hint, Is.EqualTo(LocalizationFixture.Es.Get("deviceBtDeniedHint")));
            Assert.That(spec.ShowEnableButton, Is.True, "el mismo botón");
            Assert.That(spec.IconDanger, Is.True);
        }

        [Test, Category("AC-BLE-04-06")]
        public void UnsupportedHasNoButton()
        {
            Assert.That(DeviceScreenRules.ViewFor(BleStatus.Disconnected, BleAdapterState.Unsupported), Is.EqualTo(DeviceViewKind.BluetoothState));
            var spec = DeviceScreenRules.BluetoothState(BleAdapterState.Unsupported, LocalizationFixture.Es)!;
            Assert.That(spec.Title, Is.EqualTo("Este equipo no tiene Bluetooth LE"));
            Assert.That(spec.Hint, Is.EqualTo("RowMate necesita Bluetooth LE para conectar\nel remo y el pulsómetro"));
            Assert.That(spec.ShowEnableButton, Is.False);
            Assert.That(spec.IconDanger, Is.False, "text.tertiary (D10-07)");
            Assert.That(spec.Breathes, Is.False);
        }

        [Test, Category("AC-BLE-04-03")]
        public void OtherStatesShowScan()
        {
            foreach (var adapter in new[] { BleAdapterState.Unknown, BleAdapterState.TurningOn, BleAdapterState.TurningOff, BleAdapterState.On })
            {
                Assert.That(DeviceScreenRules.ViewFor(BleStatus.Disconnected, adapter), Is.EqualTo(DeviceViewKind.Scan), adapter.ToString());
                Assert.That(DeviceScreenRules.BluetoothState(adapter, LocalizationFixture.Es), Is.Null);
            }

            foreach (BleAdapterState adapter in Enum.GetValues(typeof(BleAdapterState)))
            {
                Assert.That(DeviceScreenRules.ViewFor(BleStatus.Connected, adapter), Is.EqualTo(DeviceViewKind.Connected), "conectado manda");
            }

            // De apagado a encendido la búsqueda NO arranca sola: es la vista la que cambia (el adaptador no llama a nada).
            using (var rig = new Ble.BleRig(initial: BleAdapterState.Off))
            {
                rig.Adapter.SetState(BleAdapterState.On);
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));
                Assert.That(DeviceScreenRules.ViewFor(rig.Status, rig.Rower.AdapterState.Value), Is.EqualTo(DeviceViewKind.Scan));
            }
        }

        [Test, Category("AC-BLE-11-12")]
        public void HeaderNameFallbacks()
        {
            // ds:98, :450-452 (DV-101): el nombre; sin nombre, deviceUnknown; sin remo asociado, deviceRowerFallback.
            Assert.That(DeviceScreenRules.DeviceName("AMS-670B", true, LocalizationFixture.Es), Is.EqualTo("AMS-670B"));
            Assert.That(DeviceScreenRules.DeviceName(null, true, LocalizationFixture.Es), Is.EqualTo("Dispositivo desconocido"));
            Assert.That(DeviceScreenRules.DeviceName("", true, LocalizationFixture.En), Is.EqualTo("Unknown device"));
            Assert.That(DeviceScreenRules.DeviceName(null, false, LocalizationFixture.Es), Is.EqualTo("Rower"));
            Assert.That(DeviceScreenRules.DeviceName("Remo simulado", false, LocalizationFixture.Es), Is.EqualTo("Remo simulado"), "el simulador");
            using (var rig = new Ble.BleRig())
            {
                var store = rig.Store();
                rig.Connected(FakeBleDevices.Anon);
                Assert.That(DeviceScreenRules.DeviceName(store.State.Value, LocalizationFixture.Es), Is.EqualTo("Dispositivo desconocido"));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-05-07"), Category("AC-BLE-06-07")]
        public void ScanItemsNamesAddressesAndSteps()
        {
            using (var rig = new Ble.BleRig())
            {
                var store = rig.Store();
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                rig.Adapter.Advertise(FakeBleDevices.Anon);
                var items = DeviceScreenRules.ScanItems(store.State.Value, LocalizationFixture.Es);
                Assert.That(items[0].Key, Is.EqualTo(FakeBleDevices.RowId));
                Assert.That(items[0].Name, Is.EqualTo("AMS-670B"));
                Assert.That(items[0].Line2, Is.EqualTo(FakeBleDevices.RowId), "la dirección");
                Assert.That(items[1].Name, Is.EqualTo("Dispositivo desconocido"));
                Assert.That(items[0].ShowSpinner, Is.False);
                Assert.That(items[0].Enabled, Is.True);

                store.ConnectAsync(FakeBleDevices.RowId);
                items = DeviceScreenRules.ScanItems(store.State.Value, LocalizationFixture.Es);
                Assert.That(items[0].IsActive, Is.True);
                Assert.That(items[0].Line2, Is.EqualTo("Tomando control del monitor…"), "el paso en lugar de la dirección");
                Assert.That(items[1].IsActive, Is.False);
                Assert.That(items[1].Line2, Is.EqualTo(FakeBleDevices.AnonId));
                rig.Wait(0.3);
                items = DeviceScreenRules.ScanItems(store.State.Value, LocalizationFixture.Es);
                Assert.That(items[0].Line2, Is.EqualTo("Listo"), "Listo mientras la vista retiene el cambio");
                Assert.That(DeviceScreenRules.StepText(ConnectPhase.Connecting, LocalizationFixture.En), Is.EqualTo("Connecting…"));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-08-05")]
        public void ReconnectBannerEnablesSearchBetweenAttempts()
        {
            using (var rig = new Ble.BleRig())
            {
                var store = rig.Store();
                rig.Connected();
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                var spec = DeviceScreenRules.LinkBanner(store.State.Value, LocalizationFixture.Es);
                Assert.That(spec.Kind, Is.EqualTo(LinkBannerKind.Reconnecting));
                Assert.That(spec.Text, Is.EqualTo("Reconectando con AMS-670B…"));
                Assert.That(spec.Name, Is.EqualTo("AMS-670B"));
                Assert.That(spec.Indicator, Is.EqualTo(LinkIndicatorKind.WaitingArc));
                Assert.That(spec.NextAttemptAt, Is.EqualTo(rig.Now + 3).Within(1e-9));
                Assert.That(spec.ActionText, Is.EqualTo("Desconectar"));
                Assert.That(DeviceScreenRules.Scan(store.State.Value.Status, false, 1).SearchEnabled, Is.True, "entre intentos, Buscar habilitado");
                Assert.That(DeviceScreenRules.ViewFor(store.State.Value.Status, store.State.Value.AdapterState), Is.EqualTo(DeviceViewKind.Scan));
                rig.Wait(3);
                spec = DeviceScreenRules.LinkBanner(store.State.Value, LocalizationFixture.Es);
                Assert.That(spec.Indicator, Is.EqualTo(LinkIndicatorKind.Spinner), "durante el intento");
                Assert.That(DeviceScreenRules.Scan(store.State.Value.Status, false, 1).SearchEnabled, Is.False);
                store.DisconnectAsync();
                Assert.That(DeviceScreenRules.LinkBanner(store.State.Value, LocalizationFixture.Es).Kind, Is.EqualTo(LinkBannerKind.None), "Desconectar corta");
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-08-06")]
        public void LongNamesAreCutWithEllipsisNotTheFixedText()
        {
            // DT10-14 (A10-08): {name} se corta a 24 con …; el texto fijo nunca.
            var name = "Remo con un nombre larguísimo de prueba";
            var cut = LinkNoticeText.Name(name, LocalizationFixture.Es);
            Assert.That(cut.Length, Is.EqualTo(24));
            Assert.That(cut, Does.EndWith("…"));
            Assert.That(LinkNoticeText.Reconnecting(name, LocalizationFixture.Es), Is.EqualTo("Reconectando con Remo con un nombre larg……"), "el nombre cortado y después el … del texto fijo");
            Assert.That(LinkNoticeText.Reconnected(null, LocalizationFixture.En), Is.EqualTo("Reconnected to Unknown device"));
            var toast = LinkNoticeText.Toast(new LinkNotice(LinkNoticeKind.Reconnecting, "AMS-670B"), LocalizationFixture.Es)!;
            Assert.That(toast.Duration, Is.Null, "fijo");
            Assert.That(LinkNoticeText.Toast(new LinkNotice(LinkNoticeKind.Reconnected, "AMS-670B"), LocalizationFixture.Es)!.Duration,
                Is.EqualTo(TimeSpan.FromSeconds(2)));
            var stopped = LinkNoticeText.Toast(new LinkNotice(LinkNoticeKind.MonitorStopped, "AMS-670B"), LocalizationFixture.Es)!;
            Assert.That(stopped.Text, Is.EqualTo("El monitor dejó de enviar datos"));
            Assert.That(stopped.Duration, Is.EqualTo(TimeSpan.FromSeconds(4)));
            Assert.That(LinkNoticeText.Toast(new LinkNotice(LinkNoticeKind.Cleared, null), LocalizationFixture.Es), Is.Null);
        }

        [Test, Category("AC-BLE-12-02")]
        public void DebugPanelSpec()
        {
            // ds:142-218: RECEIVING / NO DATA por el contador de la vida de la app, la hora HH:MM:SS o --:--:--, el hex y lo decodificado
            // crudo (Q-38): SPM con 1 decimal, vatios, "{n}m" y el split.
            var empty = DeviceScreenRules.BleDebug(State(BleStatus.Connected));
            Assert.That(empty.Receiving, Is.False);
            Assert.That(empty.Badge, Is.EqualTo("NO DATA"));
            Assert.That(empty.Notifications, Is.EqualTo("0"));
            Assert.That(empty.LastReceived, Is.EqualTo("--:--:--"));
            Assert.That(empty.RawHex, Is.EqualTo("(sin datos)"));
            Assert.That(empty.ParsedSpm, Is.EqualTo("0.0"));
            Assert.That(empty.ParsedDistance, Is.EqualTo("0m"));
            Assert.That(empty.ParsedSplit, Is.EqualTo("--:--"));

            var data = new RowingData { StrokeRate = 24.5, PowerWatts = -12, DistanceMeters = 482, Pace500mSeconds = 133 };
            var at = new DateTimeOffset(2026, 11, 8, 9, 5, 7, TimeSpan.FromHours(-3));
            var full = DeviceScreenRules.BleDebug(State(BleStatus.Connected) with { Data = data, Diagnostics = new BleDiagnostics(1234, "2c 00", at) });
            Assert.That(full.Receiving, Is.True);
            Assert.That(full.Badge, Is.EqualTo("RECEIVING"));
            Assert.That(full.Notifications, Is.EqualTo("1234"));
            Assert.That(full.LastReceived, Is.EqualTo("09:05:07"), "hora local");
            Assert.That(full.RawHex, Is.EqualTo("2c 00"));
            Assert.That(full.ParsedSpm, Is.EqualTo("24.5"));
            Assert.That(full.ParsedWatts, Is.EqualTo("-12"), "crudo (Q-38)");
            Assert.That(full.ParsedDistance, Is.EqualTo("482m"));
            Assert.That(full.ParsedSplit, Is.EqualTo("02:13"));
            Assert.That(BleDebugSpec.Title, Is.EqualTo("DEBUG BLE"));
            Assert.That(BleDebugSpec.ShowButton, Is.EqualTo("Show BLE debug"));
        }
    }
}
