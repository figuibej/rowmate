using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // RSC: conexión, descubrimiento, toma de control, keepalive y desconexión manual del remo real con el adaptador falso (F-BLE-06,
    // F-BLE-07; 10a 02-tech-design §2.2, §2.3, §2.5).
    // legacy: lib/core/bluetooth/ble_service.dart:135-160, :204-302, :304-319
    [TestFixture]
    public sealed class RowerServiceConnectTests
    {
        [Test, Category("AC-BLE-06-01")]
        public void ConnectingAndFifteenSecondCap()
        {
            // bs:136-144: conectando y un tope de 15 s; al vencer, desconectado y el error con el detalle.
            using (var rig = new BleRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                var connect = rig.Connect(FakeBleDevices.Row);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connecting));
                Assert.That(rig.Link.Phase, Is.EqualTo(ConnectPhase.Connecting));
                Assert.That(rig.Link.DeviceId, Is.EqualTo(FakeBleDevices.RowId));
                Assert.That(rig.Rower.ConnectedDeviceName, Is.EqualTo("AMS-670B"), "bs:57: el nombre del equipo desde que empieza");
                rig.At(14.999);
                Assert.That(connect.IsCompleted, Is.False);
                rig.At(15);
                Assert.That(connect.IsFaulted, Is.True);
                Assert.That(connect.Exception!.InnerException, Is.InstanceOf<BleException>());
                Assert.That(connect.Exception.InnerException!.Message, Is.EqualTo("connect: timed out after 15 s"));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(rig.Link.LastDisconnect, Is.EqualTo(DisconnectReason.ConnectFailed));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.None), "un connect fallido no reconecta");
            }
        }

        [Test, Category("AC-BLE-06-01")]
        public void TimeoutCancelsAdapterConnect()
        {
            // DT10-17 (QA-G10a-03): el plazo cancela el connect del adaptador (que cierra la sesión nativa por id).
            using (var rig = new BleRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Connect(FakeBleDevices.Row);
                rig.At(15);
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.RowId), Is.EqualTo(1));
            }
        }

        [Test, Category("AC-BLE-06-01")]
        public void AllItemsSpinAndBlockWhileConnecting()
        {
            // ds:397, :460-466: conectando, todas las tarjetas con el spinner, sin responder, y Buscar deshabilitado.
            using (var rig = new BleRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                var store = rig.Store();
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                rig.Adapter.Advertise(FakeBleDevices.Anon);
                store.ConnectAsync(FakeBleDevices.RowId);
                var items = DeviceScreenRules.ScanItems(store.State.Value, LocalizationFixture.Es);
                Assert.That(items.Count, Is.EqualTo(2));
                foreach (var item in items)
                {
                    Assert.That(item.ShowSpinner, Is.True);
                    Assert.That(item.Enabled, Is.False);
                }

                Assert.That(DeviceScreenRules.Scan(store.State.Value.Status, false, 2).SearchEnabled, Is.False);
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-06-02")]
        public void DiscoversAndLogs()
        {
            // bs:204-253, :291-302: los servicios y características con sus propiedades, la suscripción a todo lo que notifica, cada
            // paquete en hex y cada lectura decodificada.
            using (var rig = new BleRig())
            {
                rig.Connected();
                var lines = string.Join("\n", rig.Logger.Lines);
                Assert.That(lines, Does.Contain("[BLE] Info: ===== SERVICIOS DESCUBIERTOS ====="));
                Assert.That(lines, Does.Contain("[BLE] Info: Servicio: 00001826-0000-1000-8000-00805f9b34fb"));
                Assert.That(lines, Does.Contain("[BLE] Info:   Char: 00002ad1-0000-1000-8000-00805f9b34fb [NOTIFY]"));
                Assert.That(lines, Does.Contain("[BLE] Info:   Char: 00002ad9-0000-1000-8000-00805f9b34fb [WRITE,INDICATE]"));
                Assert.That(lines, Does.Contain("[BLE] Info:   Char: 00002a29-0000-1000-8000-00805f9b34fb [READ]"));
                Assert.That(lines, Does.Contain("[BLE] Info:   → Suscrito a 00002ad1-0000-1000-8000-00805f9b34fb"));
                var subscribed = 0;
                foreach (var call in rig.Adapter.SetNotifyCalls)
                {
                    Assert.That(call.Characteristic, Is.Not.EqualTo(FakeBleDevices.ManufacturerName), "solo lo que notifica o indica");
                    subscribed++;
                }

                Assert.That(subscribed, Is.EqualTo(3), "2AD1, 2ADA y 2AD9 (indica)");

                var data = new StateRecorder<RowingData>(rig.Rower.Data);
                var raw = new StateRecorder<byte[]>(rig.Rower.RawBytes);
                rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                Assert.That(raw.Count, Is.EqualTo(1));
                Assert.That(data.Count, Is.EqualTo(1));
                Assert.That(data.Last.StrokeRate, Is.EqualTo(24.0));
                Assert.That(data.Last.DistanceMeters, Is.EqualTo(482));
                Assert.That(data.Last.PowerWatts, Is.EqualTo(150));
                lines = string.Join("\n", rig.Logger.Lines);
                Assert.That(lines, Does.Contain("[BLE] Debug: Raw bytes (12): 2c 00 30 2a 01 e2 01 00 85 00 96 00"));
                Assert.That(lines, Does.Contain("[BLE] Debug: Parsed → SPM:24.0 W:150 dist:482m elapsed:0s"));
                rig.Adapter.Notify(FakeBleDevices.RowId, FakeBleDevices.FitnessMachineStatus, new byte[] { 0x04 });
                Assert.That(string.Join("\n", rig.Logger.Lines), Does.Contain("[BLE-CHAR:00002ADA-0000-1000-8000-00805F9B34FB] Debug: len=1 → 04"));
                Assert.That(data.Count, Is.EqualTo(1), "solo 0x2AD1 se decodifica");
                rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, new byte[0]);
                Assert.That(raw.Count, Is.EqualTo(1), "bs:292: el paquete vacío no cuenta");
            }
        }

        [Test, Category("AC-BLE-06-02")]
        public void BikeConnectsWithZeroMetricsNoWatchdog()
        {
            // bs:251-253 (Q-35): sin 0x2AD1, conectado igual, métricas en 0 y el watchdog nunca dispara.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                rig.Connected(FakeBleDevices.Bike);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("AVISO: No se encontró 0x2AD1"));
                rig.Adapter.Notify(FakeBleDevices.BikeId, FakeBleDevices.IndoorBikeData, new byte[] { 0x44, 0x02, 0x10, 0x00 });
                rig.Wait(60);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "el watchdog no dispara");
                Assert.That(store.State.Value.Data, Is.EqualTo(RowingData.Empty));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-06-03")]
        public void ControlZeroThen300msThenSeven()
        {
            // bs:259-276: 0x00 (Request Control) con respuesta, 300 ms y 0x07 (Start or Resume); recién ahí, conectado.
            using (var rig = new BleRig())
            {
                rig.Connect(FakeBleDevices.Row);
                Assert.That(rig.Adapter.Writes.Count, Is.EqualTo(1));
                Assert.That(rig.Adapter.Writes[0].Bytes, Is.EqualTo(new byte[] { 0x00 }));
                Assert.That(rig.Adapter.Writes[0].Characteristic, Is.EqualTo(FtmsUuids.ControlPoint));
                Assert.That(rig.Adapter.Writes[0].WithResponse, Is.True);
                Assert.That(rig.Link.Phase, Is.EqualTo(ConnectPhase.TakingControl));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connecting));
                rig.At(0.299);
                Assert.That(rig.Adapter.Writes.Count, Is.EqualTo(1));
                rig.At(0.3);
                Assert.That(rig.Adapter.Writes.Count, Is.EqualTo(2));
                Assert.That(rig.Adapter.Writes[1].Bytes, Is.EqualTo(new byte[] { 0x07 }));
                Assert.That(rig.Adapter.Writes[1].Time, Is.EqualTo(0.3).Within(1e-9));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.Link.Phase, Is.EqualTo(ConnectPhase.Ready));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Handshake FTMS completado"));
            }
        }

        [Test, Category("AC-BLE-06-03")]
        public void WriteFailureContinues()
        {
            // QA-G10a-07 (bs:265-276, un solo try): si falla 0x00, no espera ni escribe 0x07; si falla 0x07, sigue. En los dos casos
            // queda conectado y con keepalive.
            using (var rig = new BleRig())
            {
                rig.Adapter.WriteFailure = w => w.Bytes[0] == 0x00 ? new BleException("write", "GATT error 3", 3) : null;
                rig.Connect(FakeBleDevices.Row);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "sin la espera de 300 ms");
                Assert.That(rig.WritesOf(0x07), Is.EqualTo(0));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Control Point error (puede ser ignorado): write: GATT error 3"));
                rig.Wait(2);
                Assert.That(rig.WritesOf(0x07), Is.EqualTo(1), "el keepalive se arma igual");
            }

            using (var rig = new BleRig())
            {
                rig.Adapter.WriteFailure = w => w.Bytes[0] == 0x07 ? new BleException("write", "GATT error 3", 3) : null;
                rig.Connected();
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Control Point error (puede ser ignorado)"));
                rig.Wait(2);
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Keepalive error: write: GATT error 3"), "el error del keepalive solo se anota");
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
            }
        }

        [Test, Category("AC-BLE-06-03")]
        public void NoControlPointNoWrites()
        {
            // bs:261-264: K-NOCP no escribe nada ni arma keepalive.
            using (var rig = new BleRig())
            {
                rig.Connect(FakeBleDevices.NoCp);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.Link.Phase, Is.EqualTo(ConnectPhase.Ready));
                Assert.That(rig.Links.Values, Has.None.Matches<RowerLink>(l => l.Phase == ConnectPhase.TakingControl), "sin el paso de control");
                rig.Wait(60);
                Assert.That(rig.Adapter.Writes, Is.Empty);
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Control Point no encontrado, omitiendo handshake"));
            }
        }

        [Test, Category("AC-BLE-06-04")]
        public void KeepAliveThirtyInSixtySeconds()
        {
            // bs:278-288: 0x07 cada 2 s mientras dura la conexión: 30 en 60 s (± 1).
            using (var rig = new BleRig())
            {
                rig.Connected();
                var before = rig.WritesOf(0x07);
                rig.Wait(60);
                Assert.That(rig.WritesOf(0x07) - before, Is.InRange(29, 31));
                Assert.That(rig.WritesOf(0x07) - before, Is.EqualTo(30));
            }
        }

        [Test, Category("AC-BLE-06-04")]
        public void KeepAliveStopsOnAnyDisconnect()
        {
            // bs:315-319: se detiene al desconectar por cualquier motivo (manual, caída, watchdog, Bluetooth apagado).
            foreach (var cut in new[] { "manual", "caída", "watchdog", "apagado" })
            {
                using (var rig = new BleRig())
                {
                    rig.Connected();
                    switch (cut)
                    {
                        case "manual":
                            rig.Rower.DisconnectAsync();
                            break;
                        case "caída":
                            rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                            rig.Adapter.DropLink(FakeBleDevices.RowId);
                            break;
                        case "watchdog":
                            rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                            rig.Wait(6);
                            break;
                        default:
                            rig.Adapter.SetState(BleAdapterState.Off);
                            break;
                    }

                    Assert.That(rig.Status, Is.Not.EqualTo(BleStatus.Connected), cut);
                    var writes = rig.WritesOf(0x07);
                    rig.Wait(30);
                    Assert.That(rig.WritesOf(0x07), Is.EqualTo(writes), cut + ": sin keepalive");
                }
            }
        }

        [Test, Category("AC-BLE-06-05")]
        public void ConnectedAfterHandshakeZeroesData()
        {
            // bs:155, dp:38-42, :64-67: conectado recién después de todo; la grilla en cero (split --:--) hasta el primer paquete.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                rig.Connected();
                rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                Assert.That(store.State.Value.Data.DistanceMeters, Is.EqualTo(482));
                rig.Rower.DisconnectAsync();
                store.ConnectAsync(FakeBleDevices.RowId);
                Assert.That(store.State.Value.Status, Is.EqualTo(BleStatus.Connecting));
                rig.Wait(0.3);
                Assert.That(store.State.Value.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(store.State.Value.Data, Is.EqualTo(RowingData.Empty));
                Assert.That(store.State.Value.Data.Pace500mFormatted, Is.EqualTo("--:--"));
                Assert.That(DeviceScreenRules.ViewFor(store.State.Value.Status, store.State.Value.AdapterState), Is.EqualTo(DeviceViewKind.Connected));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-06-06")]
        public void TimeoutBannerClearedOnRetry()
        {
            // bs:156-159, dp:118-127: a los 15 s desconectado y el banner con el detalle; se borra al buscar o conectar otra vez.
            using (var rig = new BleRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                var store = rig.Store();
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                store.ConnectAsync(FakeBleDevices.RowId);
                rig.At(15);
                Assert.That(store.State.Value.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(DeviceScreenRules.ErrorText(store.State.Value, LocalizationFixture.Es), Is.EqualTo("Error al conectar: connect: timed out after 15 s"));
                Assert.That(DeviceScreenRules.ErrorText(store.State.Value, LocalizationFixture.En), Is.EqualTo("Connection error: connect: timed out after 15 s"));
                rig.Adapter.Respond(FakeBleDevices.RowId);
                store.ConnectAsync(FakeBleDevices.RowId);
                Assert.That(store.State.Value.Error, Is.Null, "conectar lo borra");
                rig.Wait(0.3);
                rig.Rower.DisconnectAsync();
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                store.ConnectAsync(FakeBleDevices.RowId);
                rig.Wait(15);
                Assert.That(store.State.Value.Error, Is.Not.Null);
                store.StartScanAsync();
                Assert.That(store.State.Value.Error, Is.Null, "buscar lo borra");
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-07-05")]
        public void ManualForgetsNoReconnect()
        {
            // bs:304-313: corta, detiene keepalive y notificaciones, olvida el remo (a los 10 s sigue desconectado) y vuelve a la
            // búsqueda con la última lista.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                rig.Connected();
                var peripheral = rig.Adapter.Peripheral(FakeBleDevices.RowId)!;
                store.DisconnectAsync();
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(peripheral.DisconnectCalls, Is.EqualTo(1));
                Assert.That(peripheral.Closed, Is.True);
                Assert.That(rig.Link.LastDisconnect, Is.EqualTo(DisconnectReason.Manual));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.None));
                rig.Wait(10);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(1), "no reconecta");
                Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(1), "la última lista");
                Assert.That(DeviceScreenRules.ViewFor(store.State.Value.Status, store.State.Value.AdapterState), Is.EqualTo(DeviceViewKind.Scan));
                Assert.That(DeviceScreenRules.ShowDisconnectAction(store.State.Value.Status), Is.False);
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-SEC-01")]
        public void RejectsUnknownId()
        {
            // DT10-04: solo un id que vino como resultado FTMS en esta sesión de la app o el último remo conectado.
            using (var rig = new BleRig())
            {
                var connect = rig.Rower.ConnectAsync("11:22:33:44:55:66");
                Assert.That(connect.IsFaulted, Is.True);
                Assert.That(connect.Exception!.InnerException, Is.InstanceOf<ArgumentException>());
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
            }
        }

        [Test, Category("AC-BLE-04-04")]
        public void RequestEnableDelegates()
        {
            // AC-BLE-04-04: "Activar Bluetooth" pasa al adaptador (Android: el pedido del sistema; Windows: los ajustes).
            using (var rig = new BleRig(initial: BleAdapterState.Off))
            {
                var store = rig.Store();
                var task = store.RequestEnableAsync();
                Assert.That(task.IsCompleted && !task.IsFaulted, Is.True, "nunca un error");
                Assert.That(rig.Adapter.RequestEnableCalls, Is.EqualTo(1));
                Assert.That(rig.Rower.AdapterState.Value, Is.EqualTo(BleAdapterState.Off), "la app no cambia la radio");
                store.Dispose();
            }
        }
    }
}
