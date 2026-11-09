using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateService: la búsqueda (hrs:108-183; 11-ble-pulsometro/02-tech-design §2.3) sobre el adaptador falso K-11.
    [TestFixture]
    public sealed class HeartRateServiceScanTests
    {
        private static IReadOnlyList<string> Ids(IReadOnlyList<BleScanResult> list)
        {
            var ids = new List<string>();
            foreach (var r in list)
            {
                ids.Add(r.DeviceId);
            }

            return ids;
        }

        // AC-HRM-02-01: Buscar cancela las cadencias y el intento en vuelo (que cierra su enlace), y la lista arranca vacía.
        [Test, Category("AC-HRM-02-01")]
        public void CancelsLoopsAndInFlightThenEmptyList()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Adapter.NeverRespond(FakeBleDevices.HrmId);
                rig.Hrm.AutoConnectAsync();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Connecting));
                Assert.That(rig.Hrm.IsRetrying, Is.True);
                rig.Wait(2);
                var lists = rig.Lists.Count;
                rig.Hrm.StartScanAsync();
                Assert.That(rig.Hrm.IsRetrying, Is.False, "las dos cadencias, canceladas");
                Assert.That(rig.Adapter.ConnectCancels(FakeBleDevices.HrmId), Is.EqualTo(1), "el intento en vuelo se cierra");
                Assert.That(rig.Adapter.LiveSessions(FakeBleDevices.HrmId), Is.EqualTo(0));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning));
                Assert.That(rig.Lists.Count, Is.EqualTo(lists + 1));
                Assert.That(rig.Lists.Last, Is.Empty, "la lista arranca vacía");
                rig.Wait(60);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(1), "ningún reintento después");
            }
        }

        // AC-HRM-02-01: dura 10 s; al vencer sin elección vuelve a Sin sensor o a "{recordado} · no encontrado".
        [Test, Category("AC-HRM-02-01")]
        public void TenSecondsBackToNoSensorOrNotFound()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Scan(FakeBleDevices.Hrm);
                rig.At(9.9);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning));
                Assert.That(rig.Adapter.Scanning, Is.True);
                rig.At(10);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.Scanning, Is.False);
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(1));
                Assert.That(HrmCardRules.BodyFor(store.State.Value), Is.EqualTo(HrmCardBody.NoSensor));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }

            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            using (var store = rig.Store())
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133"));
                rig.Hrm.AutoConnectAsync();
                rig.Scan(FakeBleDevices.Watch);
                rig.At(10);
                var card = HrmCardRules.Build(store.State.Value, BleStatus.Disconnected, LocalizationFixture.Es);
                Assert.That(card.Body, Is.EqualTo(HrmCardBody.Remembered));
                Assert.That(card.RememberedLine, Is.EqualTo("Banda de prueba · no encontrado"));
            }
        }

        // AC-HRM-02-01 (Q-43): al vencer, la reconexión automática NO se reanuda.
        [Test, Category("AC-HRM-02-01")]
        public void ExpiryDoesNotResumeReconnect()
        {
            using (var rig = new HrmRig(seed: HrmRig.Remembered(FakeBleDevices.Hrm)))
            {
                rig.Adapter.Fail(FakeBleDevices.HrmId, new BleException("connect", "gatt error 133"));
                rig.Hrm.AutoConnectAsync();
                rig.At(5);
                rig.Hrm.StartScanAsync();
                rig.At(15);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Hrm.IsRetrying, Is.False);
                var calls = rig.Adapter.ConnectCalls(FakeBleDevices.HrmId);
                rig.Wait(120);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HrmId), Is.EqualTo(calls));
                Assert.That(rig.StoredId, Is.EqualTo(FakeBleDevices.HrmId), "buscar otro no olvida");
            }
        }

        // QA-G11-02: una búsqueda nueva termina la anterior YA, sin cortar la nueva ni pasar por Disconnected; vence a los 10 s de la nueva.
        [Test, Category("AC-HRM-02-01")]
        public void PreviousScanEndsNow()
        {
            using (var rig = new HrmRig())
            {
                rig.Scan(FakeBleDevices.Hrm);
                rig.At(4);
                var statuses = new StateRecorder<HrmStatus>(rig.Hrm.Status);
                rig.Hrm.StartScanAsync();
                Assert.That(rig.Adapter.Scanning, Is.True, "el adaptador sigue buscando");
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(2));
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(0), "el finally viejo no corta la búsqueda nueva");
                Assert.That(statuses.Values, Has.No.Member(HrmStatus.Disconnected));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning));
                rig.At(13.9);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning), "a los 10 s de la vieja sigue");
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(0));
                rig.At(14);
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(1));
                statuses.Dispose();
            }
        }

        // hrs:170-177: StopScan termina la espera una sola vez; si no busca el pulsómetro, no toca el adaptador (podría ser del remo).
        [Test, Category("AC-HRM-02-01")]
        public void StopScanEndsWaitOnce()
        {
            using (var rig = new HrmRig())
            {
                rig.Hrm.StopScanAsync();
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(0), "sin búsqueda propia no detiene nada");
                rig.Scan();
                rig.Hrm.StopScanAsync();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(1));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
                rig.Wait(15);
                Assert.That(rig.Adapter.ScanStops, Is.EqualTo(1), "el fin de la espera no repite el stop");
            }
        }

        // AC-HRM-02-02: K-HP, K-HRM, K-ROW, K-WATCH, K-HRANON, K-HRM → Banda, Reloj, Sensor desconocido; en Windows filtra la app y
        // en Android el sistema (180d de 16 bits = el de 128 bits).
        [TestCase(false, TestName = "K11OrderDedupOnlyHr(Windows)")]
        [TestCase(true, TestName = "K11OrderDedupOnlyHr(Android)")]
        [Category("AC-HRM-02-02")]
        public void K11OrderDedupOnlyHr(bool filtersScanInOs)
        {
            using (var rig = new HrmRig(filtersScanInOs))
            {
                rig.Scan(FakeBleDevices.Hp, FakeBleDevices.Hrm, FakeBleDevices.Row, FakeBleDevices.Watch, FakeBleDevices.HrAnon, FakeBleDevices.Hrm);
                var list = rig.Lists.Last;
                Assert.That(Ids(list), Is.EqualTo(new[] { FakeBleDevices.HrmId, FakeBleDevices.WatchId, FakeBleDevices.HrAnonId }));
                var es = LocalizationFixture.Es;
                Assert.That(HrmCardRules.RowTitle(list[0], es), Is.EqualTo("Banda de prueba"));
                Assert.That(HrmCardRules.RowTitle(list[1], es), Is.EqualTo("Reloj de prueba"));
                Assert.That(HrmCardRules.RowTitle(list[2], es), Is.EqualTo("Sensor desconocido"));
                Assert.That(rig.Adapter.LastScanServices, Is.EqualTo(new[] { HeartRateParser.Service }), "solo 0x180D");
            }
        }

        // AC-HRM-02-02: solo cuentan los resultados de ESTA búsqueda.
        [Test, Category("AC-HRM-02-02")]
        public void OnlyThisScanCounts()
        {
            using (var rig = new HrmRig())
            {
                rig.Scan(FakeBleDevices.Hrm);
                rig.Hrm.StartScanAsync();
                Assert.That(rig.Lists.Last, Is.Empty);
                rig.Adapter.Advertise(FakeBleDevices.Watch);
                Assert.That(Ids(rig.Lists.Last), Is.EqualTo(new[] { FakeBleDevices.WatchId }), "la anterior no se arrastra");
                rig.At(10);
                var count = rig.Lists.Count;
                rig.Adapter.Advertise(FakeBleDevices.Hrm);
                Assert.That(rig.Lists.Count, Is.EqualTo(count), "terminada, no escucha más");
            }
        }

        // AC-HRM-02-03: un error nativo durante el escaneo termina la búsqueda en silencio (la línea va al registro, m-02).
        [Test, Category("AC-HRM-02-03")]
        public void NativeScanErrorEndsSilently()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                var scan = store.StartScanAsync();
                rig.Adapter.EmitScanFailure(new BleException("scan", "error 2", 2));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(scan.IsCompleted && !scan.IsFaulted, Is.True);
                Assert.That(store.State.Value.HasError, Is.False, "sin error en la tarjeta");
                Assert.That(rig.HrmLines("Error en el escaneo: scan: error 2"), Is.EqualTo(1));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }
        }

        // AC-HRM-02-03: si arrancar la búsqueda falla, desconectado y relanza (la tarjeta muestra el error de búsqueda).
        [Test, Category("AC-HRM-02-03")]
        public void StartFailureDisconnectsAndThrows()
        {
            using (var rig = new HrmRig())
            {
                rig.Adapter.FailNextScan(new BleException("scan", "error 2", 2));
                var scan = rig.Hrm.StartScanAsync();
                Assert.That(scan.IsFaulted, Is.True);
                Assert.That(scan.Exception!.InnerException!.Message, Is.EqualTo("scan: error 2"));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(rig.HrmLines("Error escaneando: scan: error 2"), Is.EqualTo(1));
                Assert.That(rig.Scheduler.PendingTimers, Is.EqualTo(0));
            }

            using (var rig = new HrmRig(initial: BleAdapterState.Off))
            {
                var scan = rig.Hrm.StartScanAsync();
                Assert.That(scan.IsFaulted, Is.True, "DT11-08: con el Bluetooth apagado, error de búsqueda");
                Assert.That(scan.Exception!.InnerException!.Message, Is.EqualTo("scan: adapter off"));
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));
            }
        }

        // AC-HRM-02-05 (DT11-10): el registro de la búsqueda, con la dirección enmascarada.
        [Test, Category("AC-HRM-02-05")]
        public void LogsScanStartAndFoundMasked()
        {
            using (var rig = new HrmRig())
            {
                rig.Scan(FakeBleDevices.Hrm, FakeBleDevices.Hrm);
                Assert.That(rig.Logger.Lines, Has.Member("[HRM] Info: Escaneando sensores 0x180D durante 10s"));
                Assert.That(rig.Logger.Lines, Has.Member("[HRM] Info: Encontrado AA:BB:…:0D \"Banda de prueba\" rssi=-58 uuids=[180d]"));
                Assert.That(rig.HrmLines("Encontrado"), Is.EqualTo(1), "una vez por sensor");
                foreach (var line in rig.Logger.Lines)
                {
                    Assert.That(line, Does.Not.Contain(FakeBleDevices.HrmId), "nunca la dirección completa");
                }
            }
        }

        // AC-HRM-16-02 (DT11-08): buscar pide el mismo permiso que el remo; negado = desconectado y sin error.
        [Test, Category("AC-HRM-16-02")]
        public void SamePermissionAsRower()
        {
            using (var rig = new HrmRig())
            using (var store = rig.Store())
            {
                rig.Adapter.PermissionNotAsked();
                rig.Adapter.AnswerPermission(false);
                var scan = store.StartScanAsync();
                Assert.That(scan.IsFaulted, Is.False);
                Assert.That(rig.Adapter.PermissionRequests, Is.EqualTo(1));
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Disconnected));
                Assert.That(store.State.Value.HasError, Is.False);
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unauthorized));
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));

                rig.Adapter.GrantPermission();
                rig.Hrm.StartScanAsync();
                Assert.That(rig.Status, Is.EqualTo(HrmStatus.Scanning));
            }
        }

        // AC-HRM-SEC-01: unos auriculares nunca aparecen y nunca se conectan (solo ids de la búsqueda o el recordado).
        [Test, Category("AC-HRM-SEC-01")]
        public void HeadphonesNeverListedOrConnected()
        {
            using (var rig = new HrmRig())
            {
                rig.Scan(FakeBleDevices.Hp);
                Assert.That(rig.Lists.Last, Is.Empty);
                Assert.Throws<ArgumentException>(() => rig.Hrm.ConnectAsync(FakeBleDevices.HpId).GetAwaiter().GetResult());
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HpId), Is.EqualTo(0));
                Assert.That(rig.Adapter.TotalConnectCalls, Is.EqualTo(0));
            }
        }
    }
}
