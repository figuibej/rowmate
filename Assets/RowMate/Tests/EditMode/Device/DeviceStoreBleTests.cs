using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Device
{
    // DSB: DeviceStore con el remo real sobre el adaptador falso (10a 02-tech-design §6.2): los datos para la app, los pasos y la
    // vibración de "Listo", el aviso de monitor apagado con "Reintentar", los avisos del enlace para el HUD y el diagnóstico.
    // legacy: lib/features/device/device_provider.dart:19-22, :34-102
    [TestFixture]
    public sealed class DeviceStoreBleTests
    {
        private static void Packet(BleRig rig, string id = FakeBleDevices.RowId)
        {
            rig.Adapter.Notify(id, FtmsUuids.RowerData, FakeBleDevices.RowerData);
        }

        [Test, Category("AC-BLE-06-05")]
        public void DataPublishedToApp()
        {
            // bs:291-302: cada paquete de Rower Data se publica para toda la app, igual que con el simulador.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                var appData = new StateRecorder<RowingData>(rig.Rower.Data);
                rig.Connected();
                Packet(rig);
                Assert.That(appData.Count, Is.EqualTo(1));
                Assert.That(store.State.Value.Data.StrokeRate, Is.EqualTo(24.0));
                Assert.That(store.State.Value.Data.Pace500mSeconds, Is.EqualTo(133));
                var tiles = DeviceScreenRules.Tiles(store.State.Value.Data, LocalizationFixture.Es);
                Assert.That(tiles[0].ValueText, Is.EqualTo("02:13"));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-06-07")]
        public void PhasesAndHapticOnce()
        {
            // A10-01, A10-06: Conectando → Tomando control → Listo, y una vibración al quedar Listo por una conexión elegida.
            using (var rig = new BleRig())
            {
                var haptics = new FakeHaptics();
                var store = rig.Store(haptics: haptics);
                var phases = new List<ConnectPhase>();
                store.State.Subscribe(s =>
                {
                    if (s.Link.Phase != ConnectPhase.None && (phases.Count == 0 || phases[phases.Count - 1] != s.Link.Phase))
                    {
                        phases.Add(s.Link.Phase);
                    }
                }, false);
                rig.Connected();
                Assert.That(phases, Is.EqualTo(new[] { ConnectPhase.Connecting, ConnectPhase.TakingControl, ConnectPhase.Ready }));
                Assert.That(haptics.Confirms, Is.EqualTo(1));
                Assert.That(store.State.Value.IsConnected, Is.True, "Connected no espera la vista (A10-05)");

                // It. 14 (#51, AC-DEBT-10): la reconexión vibra una vez, con "Reconectado con {name}" (antes no vibraba; lo cubre
                // DeviceStoreReconnectHapticTests).
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.Wait(3.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(haptics.Confirms, Is.EqualTo(2));
                store.Dispose();
            }

            using (var rig = new BleRig())
            {
                var haptics = new FakeHaptics();
                var store = rig.Store(haptics: haptics);
                rig.Connect(FakeBleDevices.NoCp);
                Assert.That(haptics.Confirms, Is.EqualTo(1), "sin Control Point: Conectando → Listo");
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-09-02")]
        public void MonitorStoppedRetrySameDevice()
        {
            // Q-31, DV-98, DT10-06: el watchdog deja el aviso con "Reintentar" (el mismo remo, aunque el servicio lo olvidó); se borra
            // al reintentar, buscar o conectar; sobre el HUD sale MonitorStopped (4 s).
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                var notices = new StateRecorder<LinkNotice>(store.LinkNotices);
                rig.Connected();
                Packet(rig);
                rig.Wait(6);
                var state = store.State.Value;
                Assert.That(state.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(state.LinkBanner, Is.EqualTo(LinkBannerKind.MonitorStopped));
                Assert.That(state.RetryDeviceId, Is.EqualTo(FakeBleDevices.RowId));
                Assert.That(notices.Values.Count, Is.EqualTo(1));
                Assert.That(notices.Last.Kind, Is.EqualTo(LinkNoticeKind.MonitorStopped));
                var spec = DeviceScreenRules.LinkBanner(state, LocalizationFixture.Es);
                Assert.That(spec.Text, Is.EqualTo("El monitor dejó de enviar datos"));
                Assert.That(spec.ActionText, Is.EqualTo("Reintentar"));
                Assert.That(spec.Indicator, Is.EqualTo(LinkIndicatorKind.LinkOff));

                store.RetryAsync();
                Assert.That(store.State.Value.LinkBanner, Is.EqualTo(LinkBannerKind.None), "reintentar lo borra");
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(2), "el mismo remo");
                rig.Wait(0.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));

                Packet(rig);
                rig.Wait(6);
                Assert.That(store.State.Value.LinkBanner, Is.EqualTo(LinkBannerKind.MonitorStopped), "otra vez");
                Assert.That(notices.Values.Count, Is.EqualTo(2));
                store.StartScanAsync();
                Assert.That(store.State.Value.LinkBanner, Is.EqualTo(LinkBannerKind.None), "buscar lo borra");
                Assert.That(store.State.Value.RetryDeviceId, Is.Null);
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-08-06"), Category("AC-BLE-08-05")]
        public void ReconnectNoticesForTheHud()
        {
            // Q-34: Reconectando fijo (una sola vez aunque haya varios intentos), Reconectado al volver; Cleared si se corta.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                var notices = new StateRecorder<LinkNotice>(store.LinkNotices);
                rig.Connected();
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                Assert.That(notices.Count, Is.EqualTo(1));
                Assert.That(notices.Last.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting));
                Assert.That(notices.Last.Name, Is.EqualTo("AMS-670B"));
                Assert.That(store.State.Value.LinkBanner, Is.EqualTo(LinkBannerKind.Reconnecting));
                rig.Wait(30);
                Assert.That(notices.Count, Is.EqualTo(1), "sin parpadeos entre intentos");
                rig.Adapter.Respond(FakeBleDevices.RowId);
                rig.Wait(15);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(notices.Count, Is.EqualTo(2));
                Assert.That(notices.Last.Kind, Is.EqualTo(LinkNoticeKind.Reconnected));
                Assert.That(store.State.Value.LinkBanner, Is.EqualTo(LinkBannerKind.None));

                rig.Adapter.DropLink(FakeBleDevices.RowId);
                Assert.That(notices.Last.Kind, Is.EqualTo(LinkNoticeKind.Reconnecting));
                store.DisconnectAsync();
                Assert.That(notices.Last.Kind, Is.EqualTo(LinkNoticeKind.Cleared), "Desconectar quita el fijo");
                Assert.That(store.State.Value.LinkBanner, Is.EqualTo(LinkBannerKind.None));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-12-02")]
        public void DiagnosticsLifetimeCounterAndHex()
        {
            // dp:56-61, :64-67, :98-102: el contador es de la vida de la app; la hora se borra al conectar; el hex queda.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                Assert.That(store.State.Value.Diagnostics, Is.EqualTo(BleDiagnostics.Empty));
                rig.Connected();
                rig.At(5);
                Packet(rig);
                Packet(rig);
                var d = store.State.Value.Diagnostics;
                Assert.That(d.Count, Is.EqualTo(2));
                Assert.That(d.Hex, Is.EqualTo("2c 00 30 2a 01 e2 01 00 85 00 96 00"));
                Assert.That(d.LastReceived, Is.EqualTo(rig.Scheduler.LocalNow), "la hora local del programador");
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.Wait(3.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                d = store.State.Value.Diagnostics;
                Assert.That(d.Count, Is.EqualTo(2), "no se reinicia al reconectar");
                Assert.That(d.Hex, Is.EqualTo("2c 00 30 2a 01 e2 01 00 85 00 96 00"), "el hex queda");
                Assert.That(d.LastReceived, Is.Null, "la hora de ESTA conexión");
                Assert.That(DeviceScreenRules.BleDebug(store.State.Value).Badge, Is.EqualTo("RECEIVING"), "el contador de la vida de la app");
                store.Dispose();
            }

            using (var rig = new BleRig())
            {
                // CAP-20 (QA-G10a-09): K-BIKE como primera conexión, NO DATA.
                var store = rig.Store();
                rig.Connected(FakeBleDevices.Bike);
                Assert.That(DeviceScreenRules.BleDebug(store.State.Value).Badge, Is.EqualTo("NO DATA"));
                store.Dispose();
            }
        }
    }
}
