using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateService: la conexión manual (hrs:188-326; 11-ble-pulsometro/02-tech-design §2.4) sobre el adaptador falso K-11.
    [TestFixture]
    public sealed class HeartRateServiceConnectTests
    {
        // AC-HRM-03-01: conexión directa sin MTU con tope de 15 s → 0x180D/0x2A37 → notificaciones → recordar → vigilancia → watchdog →
        // conectado y registro. El plazo de 15 s no queda pendiente (m-05): solo el temporizador del watchdog.
        [Test, Category("AC-HRM-03-01")]
        public void SequenceOrderNoMtuFifteenSecondCap()
        {
            using (var rig = new HrmRig())
            {
                rig.Connect();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                Assert.That(rig.Adapter.StepsOf(FakeBleDevices.HrmId), Is.EqualTo(new[] { "connect", "discover", "notify:2a37" }));
                Assert.That(rig.Adapter.ConnectRequests, Is.EqualTo(new[] { (FakeBleDevices.HrmId, false) }), "sin pedir MTU");
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.HrmId));
                Assert.That(rig.Logger.Lines, Has.Member("[HRM] Info: Conectado a Banda de prueba"));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(1), "solo el watchdog");
                Assert.That(rig.Link.ConnectedBy, Is.EqualTo(HrmConnectOrigin.Manual));
                Assert.That(rig.Adapter.Scanning, Is.False, "conectar detiene la búsqueda");
                Assert.That(rig.Hrm.ConnectedDeviceName, Is.EqualTo("Banda de prueba"));
            }

            using (var rig = new HrmRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                var connect = rig.Connect();
                rig.At(14.9);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connecting));
                rig.At(15);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.HrmId), Is.EqualTo(1), "el plazo cancela el connect del adaptador");
                Assert.That(connect.IsFaulted, Is.True);
                Assert.That(connect.Exception!.InnerException!.Message, Is.EqualTo("connect: timed out after 15 s"));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }
        }

        // QA-G11-02 (AC-HRM-03-01, CAP-04): buscar y tocar la fila: Scanning → Connecting sin Disconnected en el medio, y la tarjeta
        // dice "Conectando a Banda de prueba...".
        [Test, Category("AC-HRM-03-01")]
        public void ManualConnectNoDisconnectedFlashKeepsChosenName()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                rig.Wait(2);
                store.ConnectAsync(FakeBleDevices.HrmId, HrmCardRules.RowTitle(rig.Lists.Last[0], LocalizationFixture.Es));
                Assert.That(rig.DistinctStatuses(), Is.EqualTo(new[] { HrmStatus.Scanning, HrmStatus.Connecting }));
                var card = HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(card.Body, Is.EqualTo(HrmCardBody.Connecting));
                Assert.That(card.ConnectingText, Is.EqualTo("Conectando a Banda de prueba..."));
                rig.Wait(10);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connecting), "la búsqueda vieja no pisa el estado al vencer");
            }
        }

        // AC-HRM-03-02: un sensor sin 0x2A37 es incompatible: se desconecta, no se recuerda y la tarjeta lo dice.
        [Test, Category("AC-HRM-03-02")]
        public void NoMeasurementIsIncompatibleNotRemembered()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.NoHr);
                store.ConnectAsync(FakeBleDevices.NoHrId, "Sensor sin pulso");
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.NoHrId), Is.EqualTo(0), "enlace cerrado");
                Assert.That(rig.Adapter.StepsOf(FakeBleDevices.NoHrId), Is.EqualTo(new[] { "connect", "discover", "disconnect" }));
                Assert.That(rig.StoredId, Is.Null, "no se recuerda");
                Assert.That(store.State.Value.ErrorKind, Is.EqualTo(HrmErrorKind.Incompatible));
                Assert.That(HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es).ErrorText,
                    Is.EqualTo("No es un pulsómetro compatible (sin servicio Heart Rate)"));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0), "ni reintentos ni watchdog");
            }
        }

        // AC-HRM-03-02: un recordado anterior sigue recordado.
        [Test, Category("AC-HRM-03-02")]
        public void PreviousRememberedKept()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Scan(FakeBleDevices.NoHr);
                var connect = rig.Hrm.ConnectAsync(FakeBleDevices.NoHrId);
                Assert.That(connect.Exception!.InnerException, Is.InstanceOf<HrmIncompatibleException>());
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.HrmId));
                Assert.That(rig.StoredName, Is.EqualTo("Banda de prueba"));
            }
        }

        // AC-HRM-03-03: no responde (15 s), rechaza, falla el descubrimiento o las notificaciones: el enlace se cierra, desconectado,
        // error de conexión y sin reintentos; un recordado anterior sigue.
        [TestCase("timeout")]
        [TestCase("reject")]
        [TestCase("discover")]
        [TestCase("notify")]
        [Category("AC-HRM-03-03")]
        public void TimeoutRejectDiscoverNotifyFailuresCloseLink(string failure)
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Watch)))
            using (var store = rig.Store())
            {
                var id = FakeBleDevices.HrmId;
                switch (failure)
                {
                    case "timeout":
                        rig.Adapter.NeverRespond(id);
                        break;
                    case "reject":
                        rig.Adapter.Fail(id, new BleException("connect", "gatt error 133", 133));
                        break;
                    case "discover":
                        rig.Adapter.FailDiscover(id, new BleException("discover", "gatt error 129", 129));
                        break;
                    default:
                        rig.Adapter.FailSetNotify(id, new BleException("setNotify", "gatt error 3", 3));
                        break;
                }

                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                store.ConnectAsync(id, "Banda de prueba");
                rig.Wait(15);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected), failure);
                Assert.That(rig.Adapter.LiveSessions(id), Is.EqualTo(0), "nada queda colgado: " + failure);
                Assert.That(store.State.Value.ErrorKind, Is.EqualTo(HrmErrorKind.Connect));
                var card = HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(card.ErrorHuman, Is.EqualTo("No se pudo conectar con Banda de prueba"));
                Assert.That(card.ErrorDetail, Is.Not.Null.And.Not.Empty);
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.WatchId), "el recordado anterior sigue");
                rig.Wait(120);
                Assert.That(rig.Adapter.ConnectCalls(id), Is.EqualTo(1), "sin reintentos");
                Assert.That(rig.Hrm.IsRetrying, Is.False);
            }
        }

        // AC-HRM-03-03: después de una falla manual no se programa nada (ni con un sensor recordado).
        [Test, Category("AC-HRM-03-03")]
        public void NoRetriesAfterManualFailure()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Hrm.AutoConnectAsync();
                rig.Measure(HrPackets.Contact142);
                rig.Hrm.DisconnectAsync();
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Connect();
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(0));
            }
        }

        // AC-HRM-03-04: tocar una fila mientras ya hay una conexión en curso se ignora en silencio.
        [Test, Category("AC-HRM-03-04")]
        public void ConnectWhileConnectingIgnored()
        {
            using (var rig = new HrmRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Scan(FakeBleDevices.Hrm, FakeBleDevices.Watch);
                rig.Hrm.ConnectAsync(FakeBleDevices.HrmId);
                var second = rig.Hrm.ConnectAsync(FakeBleDevices.WatchId);
                Assert.That(second.IsCompleted && !second.IsFaulted, Is.True);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.WatchId), Is.EqualTo(0));
                Assert.That(rig.HrmLines("connect ignorado: ya hay una conexión en curso"), Is.EqualTo(1));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connecting));
            }
        }

        // AC-HRM-04-06: 72, 72, 142 y 0 (sin contacto); el inválido se descarta sin rearmar el watchdog. Sin promedios.
        [Test, Category("AC-HRM-04-06")]
        public void EveryNotificationPublished_72_72_142_0_InvalidDropped()
        {
            using (var rig = new HrmRig())
            {
                rig.Connect();
                rig.Measure(HrPackets.B72);
                rig.Measure(HrPackets.B72);
                rig.Measure(HrPackets.U16_142);
                rig.Measure(HrPackets.NoContact);
                rig.Measure(HrPackets.Invalid);
                Assert.That(rig.Bpms.Values, Is.EqualTo(new[] { 72, 72, 142, 0 }));
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(0));
                rig.Measure(HrPackets.Contact142);
                rig.At(9);
                rig.Measure(HrPackets.Invalid);
                rig.At(10);
                Assert.That(rig.Link.IsStale, Is.True, "el inválido no rearmó el plazo");
                // Otra característica (p. ej. 0x2A38) no cuenta.
                rig.Adapter.Notify(FakeBleDevices.HrmId, FakeBleDevices.BodySensorLocation, new byte[] { 0x00, 99 });
                Assert.That(rig.Bpms.Last, Is.EqualTo(0));
            }
        }

        // AC-HRM-SEC-01 (DT11-06): solo un id de una búsqueda del pulsómetro de esta ejecución o el recordado.
        [Test, Category("AC-HRM-SEC-01")]
        public void RejectsUnknownId()
        {
            using (var rig = new HrmRig())
            {
                Assert.Throws<ArgumentException>(() => rig.Hrm.ConnectAsync(FakeBleDevices.HrmId).GetAwaiter().GetResult(), "sin búsqueda");
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0));
                // Un remo que vino en la búsqueda del remo tampoco.
                rig.Rower.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                rig.Rower.StopScanAsync();
                Assert.Throws<ArgumentException>(() => rig.Hrm.ConnectAsync(FakeBleDevices.RowId).GetAwaiter().GetResult());
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0));
            }

            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Hrm.AutoConnectAsync();
                rig.At(60);
                rig.Adapter.Respond(FakeBleDevices.HrmId);
                rig.Hrm.ConnectAsync(FakeBleDevices.HrmId);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected), "el recordado sí, sin buscar");
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(7));
            }
        }
    }
}
