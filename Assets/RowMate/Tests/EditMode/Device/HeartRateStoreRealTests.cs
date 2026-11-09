using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Device
{
    // HeartRateStore con el pulsómetro REAL sobre el adaptador falso K-11 (It. 11, 02-tech-design §3.1): el tipo del error, el enlace y
    // los avisos (HRS de §6.3). Los casos de la It. 2 con el pulsómetro de prueba siguen en HeartRateStoreTests.
    [TestFixture]
    public sealed class HeartRateStoreRealTests
    {
        private static HrmCardViewModel Card(HeartRateStore store)
        {
            return HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
        }

        // AC-HRM-03-01 (hp:23-28): al quedar conectado se borran el error anterior y el nombre elegido.
        [Test, Category("AC-HRM-03-01")]
        public void ConnectedClearsErrorAndChosenName()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                store.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
                Assert.That(store.State.Value.HasError, Is.True);
                rig.Adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(1));
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                store.ConnectAsync(FakeBleDevices.HrmId, "Banda de prueba");
                Assert.That(store.State.Value.ChosenName, Is.EqualTo("Banda de prueba"));
                rig.Wait(1);
                Assert.That(store.State.Value.IsConnected, Is.True);
                Assert.That(store.State.Value.HasError, Is.False);
                Assert.That(store.State.Value.ChosenName, Is.Null);
            }
        }

        // AC-HRM-02-03 (DT11-15): la búsqueda que no arranca deja el error de búsqueda con el detalle del sistema.
        [Test, Category("AC-HRM-02-03")]
        public void ScanErrorKind()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Adapter.FailNextScan(new BleException("scan", "error 2", 2));
                var scan = store.StartScanAsync();
                Assert.That(scan.IsFaulted, Is.False, "la excepción queda en el estado");
                Assert.That(store.State.Value.ErrorKind, Is.EqualTo(HrmErrorKind.Scan));
                Assert.That(Card(store).ErrorHuman, Is.EqualTo("No se pudo buscar sensores"));
                Assert.That(Card(store).ErrorDetail, Is.EqualTo("scan: error 2"));
            }
        }

        // AC-APP-01-10: un error del arranque solo va al registro; la tarjeta queda sin error.
        [Test, Category("AC-APP-01-10")]
        public void AutoConnectFailureOnlyLogged()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                store.RetryAsync();
                rig.At(60);
                Assert.That(store.State.Value.HasError, Is.False);
                Assert.That(rig.HrmLines("Reintento fallido"), Is.EqualTo(6));
            }
        }

        // AC-HRM-07-04: "Conectar" en "· no encontrado" borra el error, relee la memoria y arranca la misma cadencia desde cero.
        [Test, Category("AC-HRM-07-04")]
        public void RetryRestartsSameCadence()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Hrm.AutoConnectAsync();
                rig.At(60);
                Assert.That(Card(store).RememberedSuffix, Is.EqualTo(" · no encontrado"));
                Assert.That(Card(store).ConnectEnabled, Is.True);
                rig.Preferences.SetString(HeartRateService.PrefDeviceName, "Banda renombrada");
                store.RetryAsync();
                Assert.That(store.State.Value.HasError, Is.False);
                Assert.That(store.State.Value.RememberedDeviceName, Is.EqualTo("Banda renombrada"), "relee la memoria");
                Assert.That(store.State.Value.IsRetrying, Is.True);
                Assert.That(Card(store).RememberedSuffix, Is.EqualTo(" · reconectando..."));
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(7), "el primero, en el acto");
                rig.At(60 + 50);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(12), "6 más, cada 10 s");
                rig.At(200);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(12));
            }
        }

        // AC-HRM-07-05 (DT11-12): "Pulsómetro conectado" solo con una reconexión automática (al abrir, Conectar o tras una caída).
        [Test, Category("AC-HRM-07-05")]
        public void ConnectedNoticeOnlyForAutomatic()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            using (var notices = new StateRecorder<HrmNotice>(store.Notices))
            {
                rig.Adapter.RespondAfter(FakeBleDevices.HrmId, TimeSpan.FromSeconds(2));
                rig.Hrm.AutoConnectAsync();
                rig.Wait(2);
                Assert.That(notices.Values, Is.EqualTo(new[] { new HrmNotice(HrmNoticeKind.Connected, "Banda de prueba") }), "al abrir");
                Assert.That(rig.Hrm.IsRetrying, Is.False, "la cadencia para");

                rig.Hrm.DisconnectAsync();
                rig.Adapter.Respond(FakeBleDevices.HrmId);
                rig.Connect();
                Assert.That(notices.Count, Is.EqualTo(1), "la manual no avisa (la tarjeta ya lo dice)");

                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                Assert.That(notices.Values[1].Kind, Is.EqualTo(HrmNoticeKind.Lost));
                Assert.That(notices.Values[2], Is.EqualTo(new HrmNotice(HrmNoticeKind.Connected, "Banda de prueba")), "tras una caída");
                Assert.That(notices.Count, Is.EqualTo(3));

                var text = HrmNoticeText.Toast(notices.Values[2], LocalizationFixture.Es);
                Assert.That(text.Text, Is.EqualTo("Pulsómetro conectado"));
                Assert.That(text.Duration, Is.EqualTo(TimeSpan.FromSeconds(2)));
            }
        }

        // AC-HRM-08-03: el aviso de caída sale una vez por corrida de la cadencia (no por intento), y otra vez con una caída nueva.
        [Test, Category("AC-HRM-08-03")]
        public void LostOncePerRun()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            using (var notices = new StateRecorder<HrmNotice>(store.Notices))
            {
                rig.Connected142();
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                rig.Wait(30);
                Assert.That(notices.Values, Is.EqualTo(new[] { new HrmNotice(HrmNoticeKind.Lost, "Banda de prueba") }));
                rig.Adapter.Respond(FakeBleDevices.HrmId);
                rig.Wait(3);
                Assert.That(notices.Values[1].Kind, Is.EqualTo(HrmNoticeKind.Connected));
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133", 133));
                rig.Adapter.DropLink(FakeBleDevices.HrmId);
                rig.Wait(30);
                Assert.That(notices.Count, Is.EqualTo(3));
                Assert.That(notices.Values[2].Kind, Is.EqualTo(HrmNoticeKind.Lost));
                Assert.That(HrmNoticeText.Toast(notices.Values[2], LocalizationFixture.Es).Text, Is.EqualTo("Se perdió el pulsómetro, reconectando…"));
                store.Dispose();
                rig.Adapter.Respond(FakeBleDevices.HrmId);
                rig.Wait(3);
                Assert.That(notices.Count, Is.EqualTo(3), "nada después de Dispose");
            }
        }

        // AC-HRM-10-02 (m-07): si Desconectar u Olvidar fallan, el error queda en la tarjeta ("No se pudo conectar con {name}").
        [Test, Category("AC-HRM-10-02")]
        public void ErrorStaysOnFailure()
        {
            var hrm = new ThrowingHeartRateService();
            using (var store = new HeartRateStore(hrm, new RecordingLogger()))
            {
                store.DisconnectAsync();
                Assert.That(store.State.Value.ErrorKind, Is.EqualTo(HrmErrorKind.Connect));
                Assert.That(Card(store).ErrorHuman, Is.EqualTo("No se pudo conectar con Sensor desconocido"));
                Assert.That(Card(store).ErrorDetail, Is.EqualTo("disconnect"));
                store.ForgetAsync();
                Assert.That(store.State.Value.HasError, Is.False, "empezar otra acción lo borra");
            }
        }

        // A11-01: el enlace del servicio llega al estado de la tarjeta (lectura vencida y fase de reconexión).
        [Test, Category("AC-HRM-05-02")]
        public void LinkReachesTheState()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Connected142();
                Assert.That(store.State.Value.IsStale, Is.False);
                rig.At(10);
                Assert.That(store.State.Value.IsStale, Is.True);
                Assert.That(store.State.Value.Bpm, Is.EqualTo(0));
                rig.Measure(HrPackets.Contact142);
                Assert.That(store.State.Value.IsStale, Is.False);
                Assert.That(store.State.Value.Bpm, Is.EqualTo(142));
            }
        }
    }
}
