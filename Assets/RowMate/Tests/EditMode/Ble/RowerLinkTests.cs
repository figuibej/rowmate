using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // RRC y RWD: la reconexión (F-BLE-08; Q-29, DV-96, DV-97) y el watchdog de 5 s (F-BLE-09; Q-31) del remo real con el adaptador
    // falso (10a 02-tech-design §2.4). RowerLink (A10-01): fase, reconexión con NextAttemptAt y motivo.
    // legacy: lib/core/bluetooth/ble_service.dart:148-199, :304-319; lib/features/device/device_provider.dart:8, :64-86
    [TestFixture]
    public sealed class RowerLinkTests
    {
        private static void Notify(BleRig rig)
        {
            rig.Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
        }

        // ── Reconexión ──

        [Test, Category("AC-BLE-08-02")]
        public void FirstAttemptAtThreeSecondsTenSecondCap()
        {
            // bs:148-153, :163-192: la caída → desconectado y limpio; a los 3 s, conectando con un tope de 10 s, descubrimiento, toma de
            // control y keepalive. Si sale bien, conectado.
            using (var rig = new BleRig())
            {
                rig.Connected();
                rig.At(10);
                var peripheral = rig.Adapter.Peripheral(FakeBleDevices.RowId)!;
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(peripheral.Closed, Is.True);
                Assert.That(rig.Rower.ConnectedDeviceName, Is.Null);
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Waiting));
                Assert.That(rig.Link.NextAttemptAt, Is.EqualTo(13.0).Within(1e-9));
                Assert.That(rig.Link.LastDisconnect, Is.EqualTo(DisconnectReason.Lost));
                Assert.That(rig.Link.DeviceName, Is.EqualTo("AMS-670B"));
                rig.At(12.999);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(1));
                rig.At(13);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(2));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connecting));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Attempting));
                Assert.That(rig.Link.NextAttemptAt, Is.Null);
                rig.At(13.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.None));
                Assert.That(rig.Link.Phase, Is.EqualTo(ConnectPhase.Ready));
                Assert.That(rig.WritesOf(0x00), Is.EqualTo(2), "repite la toma de control");
                var writes = rig.WritesOf(0x07);
                rig.Wait(10);
                Assert.That(rig.WritesOf(0x07) - writes, Is.EqualTo(5), "y el keepalive");
            }

            using (var rig = new BleRig())
            {
                rig.Connected();
                rig.At(10);
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.At(22.999);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connecting));
                rig.At(23);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected), "tope de 10 s (bs:178)");
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.RowId), Is.EqualTo(1));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Waiting));
                Assert.That(rig.Link.NextAttemptAt, Is.EqualTo(26.0).Within(1e-9));
            }
        }

        [Test, Category("AC-BLE-08-03")]
        public void RetriesForeverEveryThreeSecondsAfterFailure()
        {
            // Q-29 (cambio): un intento nuevo 3 s después de cada fallo, sin límite (el legacy hacía uno solo, bs:193-197, :315-319).
            using (var rig = new BleRig())
            {
                rig.Connected();
                rig.At(10);
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.At(70);
                // 13, 26, 39, 52 y 65 (cada intento dura 10 s y el próximo va a +3 s).
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId) - 1, Is.EqualTo(5));
                var alternating = new List<BleStatus>();
                foreach (var status in rig.Statuses.Values)
                {
                    if (alternating.Count == 0 || alternating[alternating.Count - 1] != status)
                    {
                        alternating.Add(status);
                    }
                }

                var tail = alternating.GetRange(alternating.Count - 4, 4);
                Assert.That(tail, Is.EqualTo(new[] { BleStatus.Disconnected, BleStatus.Connecting, BleStatus.Disconnected, BleStatus.Connecting }));
                rig.Adapter.Respond(FakeBleDevices.RowId);
                rig.At(75);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected), "el intento en curso sigue sin respuesta hasta su tope");
                rig.At(78.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "al responder, queda conectado");
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.None));
            }

            using (var rig = new BleRig())
            {
                rig.Connected();
                rig.At(10);
                rig.Adapter.Fail(FakeBleDevices.RowId, new BleException("connect", "GATT error 133", 133));
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.At(70);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId) - 1, Is.EqualTo(20), "fallo en el acto: cada 3 s");
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Reconexión fallida: connect: GATT error 133"));
            }
        }

        [Test, Category("AC-BLE-08-04")]
        public void CancelledByDisconnectScanConnect()
        {
            // DV-96: Desconectar, Buscar (en el legacy no) y elegir un remo cancelan la reconexión.
            foreach (var action in new[] { "desconectar", "buscar", "conectar" })
            {
                using (var rig = new BleRig())
                {
                    rig.Scan(FakeBleDevices.Row, FakeBleDevices.Anon);
                    rig.Rower.ConnectAsync(FakeBleDevices.RowId);
                    rig.At(11);
                    rig.Adapter.DropLink(FakeBleDevices.RowId);
                    rig.At(12);
                    switch (action)
                    {
                        case "desconectar":
                            rig.Rower.DisconnectAsync();
                            break;
                        case "buscar":
                            rig.Rower.StartScanAsync();
                            break;
                        default:
                            rig.Rower.ConnectAsync(FakeBleDevices.AnonId);
                            break;
                    }

                    Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.None), action);
                    rig.At(30);
                    Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(1), action + ": no hay intento");
                }
            }

            using (var rig = new BleRig())
            {
                // Durante un intento: Desconectar lo corta.
                rig.Connected();
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.Wait(3);
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Attempting));
                rig.Rower.DisconnectAsync();
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.RowId), Is.EqualTo(1), "el connect en curso se cancela");
                rig.Wait(30);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(2));
            }
        }

        [Test, Category("AC-BLE-08-04")]
        public void WatchdogAndManualNeverReconnect()
        {
            using (var rig = new BleRig())
            {
                rig.Connected();
                Notify(rig);
                rig.Wait(6);
                Assert.That(rig.Link.LastDisconnect, Is.EqualTo(DisconnectReason.Watchdog));
                rig.Wait(30);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(1), "watchdog");
            }

            using (var rig = new BleRig())
            {
                rig.Connected();
                rig.Rower.DisconnectAsync();
                rig.Wait(30);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(1), "manual");
            }
        }

        [Test, Category("AC-BLE-08-04")]
        public void AdapterOffThenOnReconnects()
        {
            // DV-97, DT10-02: con el Bluetooth apagado los intentos se saltean y se reagendan; al encenderlo, el próximo reconecta.
            using (var rig = new BleRig())
            {
                rig.Connected();
                rig.At(10);
                rig.Adapter.SetState(BleAdapterState.Off);
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Waiting));
                rig.At(20);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(1), "sin On no intenta");
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Off));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Waiting), "sigue esperando, sin parpadear");
                rig.Adapter.SetState(BleAdapterState.On);
                rig.At(22);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.RowId), Is.EqualTo(2), "el próximo intento (a los 22) reconecta");
                rig.At(22.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
            }
        }

        // ── Watchdog ──

        [Test, Category("AC-BLE-09-01")]
        public void FiresAtFiveWholeSeconds()
        {
            // dp:69-80: cada 1 s desde que conecta; ≥ 5 s enteros desde el último paquete → desconexión manual (sin reconectar).
            using (var rig = new BleRig())
            {
                rig.Connected(); // t = 0,3: el chequeo corre a los 1,3, 2,3, …
                rig.At(2);
                Notify(rig);
                rig.At(7.299);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "a los 6,3 pasaron 4,3 s");
                rig.At(7.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected), "a los 7,3 pasaron 5,3 s: entre 5 y 6");
                Assert.That(rig.Link.LastDisconnect, Is.EqualTo(DisconnectReason.Watchdog));
                Assert.That(rig.Link.DeviceId, Is.EqualTo(FakeBleDevices.RowId));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Watchdog: sin datos por 5s → desconectando"));
            }

            using (var rig = new BleRig())
            {
                // Con un paquete por segundo, nunca.
                rig.Connected();
                using (rig.Adapter.NotifyEvery(FakeBleDevices.RowId, TimeSpan.FromSeconds(1), k => FakeBleDevices.RowerData))
                {
                    rig.Wait(120);
                }

                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                rig.Wait(5);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
            }
        }

        [Test, Category("AC-BLE-09-01")]
        public void NeverWithoutPacket()
        {
            // dp:72-73: sin ningún paquete de esta conexión, nunca dispara. QA-G10a-10: los de la toma de control no cuentan.
            using (var rig = new BleRig())
            {
                rig.Connect(FakeBleDevices.Row);
                rig.Wait(0.1);
                Notify(rig); // durante la toma de control
                rig.Wait(0.2);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                rig.Wait(60);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
            }
        }

        [Test, Category("AC-BLE-09-01")]
        public void StopsOutsideConnected()
        {
            // dp:40-45: el chequeo se detiene en cualquier estado que no sea conectado; al reconectar, vuelve a empezar sin paquete.
            using (var rig = new BleRig())
            {
                rig.Connected();
                Notify(rig);
                rig.At(2);
                rig.Adapter.DropLink(FakeBleDevices.RowId);
                rig.At(5.3);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "reconectó a los 5,3");
                rig.Wait(30);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "sin paquete en la conexión nueva, no dispara");
                Assert.That(rig.Link.LastDisconnect, Is.EqualTo(DisconnectReason.None));
            }
        }
    }
}
