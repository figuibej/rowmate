using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateService: la memoria del último sensor (hrs:34-35, :91-98, :405-421; DT11-04, DT11-05) en las preferencias de la app.
    [TestFixture]
    public sealed class HeartRateServiceMemoryTests
    {
        // AC-HRM-06-01: hrm.deviceId y hrm.deviceName del sensor conectado.
        [Test, Category("AC-HRM-06-01")]
        public void SavesIdAndName()
        {
            using (var rig = new HrmRig())
            {
                rig.Connect();
                Assert.That(rig.StoredId, Is.EqualTo("AA:BB:CC:00:18:0D"));
                Assert.That(rig.StoredName, Is.EqualTo("Banda de prueba"));
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo("Banda de prueba"));
            }
        }

        // AC-HRM-06-01 (Q-41, DV-106): conectar después un sensor sin nombre cambia el id y BORRA el nombre: Sensor desconocido.
        [Test, Category("AC-HRM-06-01")]
        public void AnonAfterHrmRemovesName()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo("Banda de prueba"));
                rig.Scan(FakeBleDevices.HrAnon);
                rig.Hrm.ConnectAsync(FakeBleDevices.HrAnonId);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.HrAnonId));
                Assert.That(rig.StoredName, Is.Null, "se borra");
                Assert.That(rig.Hrm.ConnectedDeviceName, Is.EqualTo(""), "nunca la dirección (DT11-04)");
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo(""));
                var card = HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(card.ConnectedName, Is.EqualTo("Sensor desconocido"));
            }
        }

        // DT11-05: el mismo id sin nombre (reconectar sin buscar) conserva el nombre guardado.
        [Test, Category("AC-HRM-06-01")]
        public void SameIdWithoutNameKeepsName()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.HrAnon, "Reloj guardado")))
            {
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connected));
                Assert.That(rig.StoredName, Is.EqualTo("Reloj guardado"));
                Assert.That(rig.Hrm.ConnectedDeviceName, Is.EqualTo("Reloj guardado"), "el del sistema falta: el recordado");
            }
        }

        // AC-HRM-06-02: la memoria se lee al abrir (y en cada Conectar).
        [Test, Category("AC-HRM-06-02")]
        public void LoadedOnAutoConnect()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                Assert.That(rig.Hrm.RememberedDeviceName, Is.Null, "todavía sin leer");
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo("Banda de prueba"));
                rig.Hrm.ForgetAsync();
                rig.Preferences.SetString(HeartRateService.PrefDeviceId, FakeBleDevices.WatchId);
                rig.Preferences.SetString(HeartRateService.PrefDeviceName, "Reloj de prueba");
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo("Reloj de prueba"), "se relee en cada Conectar");
            }
        }

        // AC-HRM-06-02: el nombre solo vale con id (un nombre huérfano se ignora).
        [Test, Category("AC-HRM-06-02")]
        public void OrphanNameIgnored()
        {
            var seed = new[] { new KeyValuePair<string, object>(HeartRateService.PrefDeviceName, "Huérfano") };
            using (var rig = new HrmRig(seed: seed))
            {
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Hrm.RememberedDeviceName, Is.Null);
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0));
            }
        }

        // AC-HRM-06-02 (Q-41): recordado = el guardado o ""; conectado = el del sistema, si no el recordado, si no "".
        [Test, Category("AC-HRM-06-02")]
        public void NameFallbacks()
        {
            using (var rig = new HrmRig(seed: new[] { new KeyValuePair<string, object>(HeartRateService.PrefDeviceId, FakeBleDevices.HrAnonId) }))
            {
                Assert.That(rig.Hrm.ConnectedDeviceName, Is.Null, "no conectado");
                rig.Adapter.NeverRespond(FakeBleDevices.HrAnonId);
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo(""), "recordado sin nombre");
                Assert.That(rig.Hrm.ConnectedDeviceName, Is.Null, "conectando todavía no es conectado");
                rig.Hrm.ForgetAsync();
                rig.Scan(FakeBleDevices.Watch);
                rig.Hrm.ConnectAsync(FakeBleDevices.WatchId);
                Assert.That(rig.Hrm.ConnectedDeviceName, Is.EqualTo("Reloj de prueba"), "el del sistema");
            }
        }

        // AC-HRM-06-03: nunca se recuerda un incompatible, uno que falló antes de las notificaciones ni un intento abortado.
        [TestCase("incompatible")]
        [TestCase("connect")]
        [TestCase("notify")]
        [TestCase("aborted")]
        [Category("AC-HRM-06-03")]
        public void NeverRememberedIncompatibleFailedOrAborted(string how)
        {
            using (var rig = new HrmRig())
            {
                switch (how)
                {
                    case "incompatible":
                        rig.Connect(FakeBleDevices.NoHr);
                        break;
                    case "connect":
                        rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                        rig.Connect();
                        break;
                    case "notify":
                        rig.Adapter.FailSetNotify(FakeBleDevices.HrmId, new BleException("setNotify", "gatt error 3", 3));
                        rig.Connect();
                        break;
                    default:
                        var release = rig.Adapter.HoldNextDiscover(FakeBleDevices.HrmId);
                        rig.Connect();
                        rig.Hrm.ForgetAsync();
                        release();
                        break;
                }

                Assert.That(rig.StoredId, Is.Null, how);
                Assert.That(rig.StoredName, Is.Null, how);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected), how);
            }
        }

        // AC-HRM-06-03: hay un solo recordado: Buscar otro no olvida hasta que otro conecta.
        [Test, Category("AC-HRM-06-03")]
        public void SearchOtherDoesNotForget()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Hrm.AutoConnectAsync();
                rig.Scan(FakeBleDevices.Watch);
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.HrmId));
                Assert.That(rig.Hrm.RememberedDeviceName, Is.EqualTo("Banda de prueba"));
                rig.Hrm.ConnectAsync(FakeBleDevices.WatchId);
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.WatchId), "uno solo: el nuevo reemplaza");
                Assert.That(rig.StoredName, Is.EqualTo("Reloj de prueba"));
            }
        }
    }
}
