using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateService: Desconectar, Olvidar, la prioridad de las acciones del usuario y el cierre (hrs:66-69, :277-301, :337-383,
    // :435-447; DT11-07; 11-ble-pulsometro/02-tech-design §2.6).
    [TestFixture]
    public sealed class HeartRateServiceActionsTests
    {
        // AC-HRM-10-01: Desconectar corta cadencias e intento, cuenta como pedida, pulso a 0, cierra el enlace en el acto (un connect
        // pendiente se cancela sin esperar 15 s) y borra la memoria.
        [Test, Category("AC-HRM-10-01")]
        public void DisconnectForgetsCancelsPendingNoDropLoop()
        {
            using (var rig = new HrmRig())
            {
                rig.Connected142();
                rig.Timeline.Clear();
                rig.Hrm.DisconnectAsync();
                Assert.That(rig.Timeline[0], Is.EqualTo("bpm:0"));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(0));
                Assert.That(rig.Adapter.StepsOf(FakeBleDevices.HrmId), Has.Member("disconnect"));
                Assert.That(rig.StoredId, Is.Null);
                Assert.That(rig.StoredName, Is.Null);
                Assert.That(rig.Hrm.RememberedDeviceName, Is.Null);
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }

            using (var rig = new HrmRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Connect();
                rig.Wait(1);
                rig.Hrm.DisconnectAsync();
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.HrmId), Is.EqualTo(1), "en el acto, no a los 15 s");
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(0));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }
        }

        // AC-HRM-10-01: al reabrir la app, no se reconecta.
        [Test, Category("AC-HRM-10-01")]
        public void NoReconnectAfterReopen()
        {
            var preferences = new RowMate.Core.Common.MemoryPreferences();
            using (var rig = new HrmRig(preferences: preferences))
            {
                rig.Connect();
                rig.Hrm.DisconnectAsync();
            }

            using (var reopened = new HrmRig(preferences: preferences))
            {
                reopened.Hrm.AutoConnectAsync();
                Assert.That(reopened.Adapter.TotalConnectCalls, Is.EqualTo(0));
                Assert.That(reopened.Hrm.RememberedDeviceName, Is.Null);
            }
        }

        // AC-HRM-10-02: Olvidar durante "· reconectando..." corta la cadencia, aborta el intento en vuelo y borra la memoria.
        [Test, Category("AC-HRM-10-02")]
        public void ForgetDuringReconnectAbortsAndClears()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Hrm.AutoConnectAsync();
                rig.Wait(3);
                store.ForgetAsync();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.HrmId), Is.EqualTo(1));
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(0));
                Assert.That(rig.StoredId, Is.Null);
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                Assert.That(HrmCardRules.BodyFor(store.State.Value), Is.EqualTo(HrmCardBody.NoSensor));
                Assert.That(store.State.Value.HasError, Is.False);
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }
        }

        // AC-HRM-11-01: un intento reemplazado (búsqueda, otro sensor, desconectar, olvidar o cierre) no cambia el estado visible cuando
        // por fin responde, no recuerda nada, no publica lecturas y cierra su enlace en silencio.
        [TestCase("scan")]
        [TestCase("other")]
        [TestCase("disconnect")]
        [TestCase("forget")]
        [TestCase("dispose")]
        [Category("AC-HRM-11-01")]
        public void SupersededAttemptSilent(string action)
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                var release = rig.Adapter.HoldNextDiscover(FakeBleDevices.HrmId);
                rig.Scan(FakeBleDevices.Hrm);
                store.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connecting));
                var old = rig.Adapter.Peripheral(FakeBleDevices.HrmId)!;
                switch (action)
                {
                    case "scan":
                        store.StartScanAsync();
                        break;
                    case "other":
                        store.StartScanAsync();
                        rig.Adapter.Advertise(FakeBleDevices.Watch);
                        store.ConnectAsync(FakeBleDevices.WatchId, "Reloj de prueba");
                        break;
                    case "disconnect":
                        store.DisconnectAsync();
                        break;
                    case "forget":
                        store.ForgetAsync();
                        break;
                    default:
                        rig.Hrm.Dispose();
                        break;
                }

                Assert.That(old.Closed, Is.True, "cierra su enlace: " + action);
                var statuses = rig.Statuses.Count;
                var bpms = rig.Bpms.Count;
                release();
                rig.Measure(HrPackets.Contact142);
                Assert.That(rig.Statuses.Count, Is.EqualTo(statuses), "no cambia el estado: " + action);
                Assert.That(rig.Bpms.Count, Is.EqualTo(bpms), "no publica lecturas: " + action);
                Assert.That(store.State.Value.HasError, Is.False, "sin error: " + action);
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(0), action);
                Assert.That(rig.StoredId, action == "other" ? Is.EqualTo(FakeBleDevices.WatchId) : Is.Null, "no recuerda nada: " + action);
                Assert.That(rig.HrmLines("Intento de conexión reemplazado: AA:BB:…:0D"), Is.EqualTo(1), action);
            }
        }

        // DT11-07 (AC-HRM-11-01): si la acción nueva apunta al MISMO sensor, el intento viejo no le corta el nuevo.
        [Test, Category("AC-HRM-11-01")]
        public void SameSensorNewAttemptNotCut()
        {
            using (var rig = new HrmRig())
            {
                var release = rig.Adapter.HoldNextDiscover(FakeBleDevices.HrmId);
                rig.Connect();
                rig.Scan(FakeBleDevices.Hrm);
                rig.Hrm.ConnectAsync(FakeBleDevices.HrmId);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                var current = rig.Adapter.Peripheral(FakeBleDevices.HrmId)!;
                release(); // el viejo responde ahora: reemplazado, con el mismo id como dueño
                Assert.That(current.Closed, Is.False, "no corta el intento nuevo");
                Assert.That(current.DisconnectCalls, Is.EqualTo(0));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(1));
                rig.Measure(HrPackets.Contact142);
                Assert.That(rig.Hrm.CurrentBpm, Is.EqualTo(142));
            }

            using (var rig = new HrmRig())
            {
                // Con el connect todavía pendiente: cancelar el viejo (por pedido) no cancela el nuevo al mismo id.
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Connect();
                rig.Scan(FakeBleDevices.Hrm);
                rig.Adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
                rig.Hrm.ConnectAsync(FakeBleDevices.HrmId);
                rig.Wait(1);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.HrmId), Is.EqualTo(1), "solo el viejo");
            }
        }

        // AC-HRM-11-01: tras Olvidar, las preferencias siguen vacías aunque el intento viejo responda.
        [Test, Category("AC-HRM-11-01")]
        public void ForgottenStaysForgotten()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                var release = rig.Adapter.HoldNextDiscover(FakeBleDevices.HrmId);
                rig.Hrm.AutoConnectAsync();
                rig.Hrm.ForgetAsync();
                release();
                rig.Wait(60);
                Assert.That(rig.StoredId, Is.Null);
                Assert.That(rig.StoredName, Is.Null);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1));
            }
        }

        // AC-HRM-11-02: si mientras se cerraba un intento fallido arrancó otra acción, ese fallo no toca el estado ni muestra error.
        [Test, Category("AC-HRM-11-02")]
        public void FailureWhileNewActionStartedIsSilent()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Adapter.FailSetNotify(FakeBleDevices.HrmId, new BleException("setNotify", "gatt error 3", 3));
                var release = rig.Adapter.HoldNextDisconnect(FakeBleDevices.HrmId);
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                var connect = store.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connecting), "cerrando el enlace del intento fallido");
                store.StartScanAsync();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning));
                release();
                Assert.That(connect.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning), "el fallo viejo no toca el estado");
                Assert.That(store.State.Value.HasError, Is.False, "ni muestra error");
            }
        }

        // AC-HRM-11-02: mientras hay una búsqueda del usuario, la reconexión automática no hace nada.
        [Test, Category("AC-HRM-11-02")]
        public void AutoConnectIgnoredDuringUserScan()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Scan();
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
            }
        }

        // AC-HRM-11-03: al cerrar se detienen búsqueda, intento, cadencias y watchdog, se sueltan los enlaces y no queda ningún
        // temporizador vivo.
        [TestCase("connected")]
        [TestCase("scanning")]
        [TestCase("connecting")]
        [TestCase("reconnecting")]
        [Category("AC-HRM-11-03")]
        public void DisposeReleasesEverything(string state)
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                switch (state)
                {
                    case "connected":
                        rig.Hrm.AutoConnectAsync();
                        rig.Measure(HrPackets.Contact142);
                        break;
                    case "scanning":
                        rig.Scan(FakeBleDevices.Hrm);
                        break;
                    case "connecting":
                        rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                        rig.Hrm.AutoConnectAsync();
                        break;
                    default:
                        rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                        rig.Hrm.AutoConnectAsync();
                        rig.Wait(4);
                        break;
                }

                rig.Hrm.Dispose();
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0), state);
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(0), state);
                Assert.That(rig.Adapter.Scanning, Is.False, state);
                Assert.That(rig.Hrm.IsRetrying, Is.False, state);
                var statuses = rig.Statuses.Count;
                var bpms = rig.Bpms.Count;
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                rig.Adapter.SetState(BleAdapterState.Off);
                rig.Wait(60);
                Assert.That(rig.Statuses.Count, Is.EqualTo(statuses), state);
                Assert.That(rig.Bpms.Count, Is.EqualTo(bpms), state);
                Assert.That(rig.Hrm.ConnectAsync(FakeBleDevices.HrmId).IsCompleted, Is.True, "dispuesto: no hace nada");
            }
        }
    }
}
