using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // RSS: el adaptador y la búsqueda del remo real con el adaptador falso (F-BLE-04, F-BLE-05, F-APP-13; 10a 02-tech-design §2.2).
    // legacy: lib/core/bluetooth/ble_service.dart:46-55, :64-132; lib/core/bluetooth/scan_filter.dart:12-20
    [TestFixture]
    public sealed class RowerServiceScanTests
    {
        private static IReadOnlyList<BleScanResult> LastList(StateRecorder<IReadOnlyList<BleScanResult>> lists)
        {
            return lists.Count == 0 ? new BleScanResult[0] : lists.Last;
        }

        private static string[] Ids(IReadOnlyList<BleScanResult> results)
        {
            var ids = new string[results.Count];
            for (var i = 0; i < results.Count; i++)
            {
                ids[i] = results[i].DeviceId;
            }

            return ids;
        }

        [Test, Category("AC-BLE-04-01")]
        public void AdapterOffSetsStatusOff()
        {
            // bs:47-54: el estado del adaptador se refleja y "apagado" pone el remo en Off.
            using (var rig = new BleRig(initial: BleAdapterState.Unknown))
            {
                Assert.That(rig.Rower.AdapterState.Value, Is.EqualTo(BleAdapterState.Unknown));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                rig.Adapter.SetState(BleAdapterState.Off);
                Assert.That(rig.Rower.AdapterState.Value, Is.EqualTo(BleAdapterState.Off));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Off));
                Assert.That(DeviceScreenRules.ViewFor(rig.Status, rig.Rower.AdapterState.Value), Is.EqualTo(DeviceViewKind.BluetoothState));
            }

            using (var rig = new BleRig(initial: BleAdapterState.Off))
            {
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Off), "el estado inicial también");
            }
        }

        [Test, Category("AC-BLE-04-01"), Category("AC-BLE-08-04")]
        public void ConnectedThenBluetoothOffShowsTheStateViewAndCleansUp()
        {
            // QA-G10a-11: con el remo conectado, apagar el Bluetooth corta (sin esperar el aviso nativo) y lleva a la vista de estado.
            using (var rig = new BleRig())
            {
                rig.Connected();
                var peripheral = rig.Adapter.Peripheral(FakeBleDevices.RowId)!;
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                rig.Adapter.SetState(BleAdapterState.Off);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Off));
                Assert.That(peripheral.Closed, Is.True, "la sesión se cierra");
                Assert.That(rig.Rower.ConnectedDeviceName, Is.Null);
                Assert.That(DeviceScreenRules.ViewFor(rig.Status, rig.Rower.AdapterState.Value), Is.EqualTo(DeviceViewKind.BluetoothState));
                Assert.That(rig.Link.Reconnect, Is.EqualTo(ReconnectPhase.Waiting), "la reconexión espera el On (DV-97)");
                var writes = rig.WritesOf(0x07);
                rig.Wait(10);
                Assert.That(rig.WritesOf(0x07), Is.EqualTo(writes), "sin keepalive");
            }
        }

        [Test, Category("AC-BLE-04-05")]
        public void WaitsFiveSecondsForOn()
        {
            // bs:69-84: desconocido o encendiendo → espera hasta 5 s; si se enciende, la búsqueda arranca.
            using (var rig = new BleRig(initial: BleAdapterState.TurningOn))
            {
                var scan = rig.Rower.StartScanAsync();
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));
                rig.At(4.9);
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));
                rig.Adapter.SetState(BleAdapterState.On);
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(1));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Scanning));
                rig.Wait(10);
                Assert.That(scan.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
            }
        }

        [Test, Category("AC-BLE-04-05")]
        public void AdapterNeverOnSetsOffWithoutMessage()
        {
            using (var rig = new BleRig(initial: BleAdapterState.Unknown))
            {
                var store = rig.Store();
                var scan = store.StartScanAsync();
                rig.At(4.999);
                Assert.That(scan.IsCompleted, Is.False);
                rig.At(5);
                Assert.That(scan.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Off), "el remo pasa a apagado");
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0), "la búsqueda no arranca");
                Assert.That(store.State.Value.Error, Is.Null, "sin mensaje");
                Assert.That(DeviceScreenRules.ViewFor(rig.Status, rig.Rower.AdapterState.Value), Is.EqualTo(DeviceViewKind.Scan), "la vista sigue en búsqueda");
                Assert.That(DeviceScreenRules.Scan(rig.Status, false, 0).SearchEnabled, Is.True, "Buscar sigue habilitado");
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-04-06")]
        public void UnsupportedDoesNothing()
        {
            // bs:66-67 (Q-47): sin Bluetooth LE, buscar no hace nada; la vista de estado lo muestra.
            using (var rig = new BleRig(initial: BleAdapterState.Unsupported))
            {
                var scan = rig.Rower.StartScanAsync();
                Assert.That(scan.IsCompleted, Is.True);
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));
                Assert.That(rig.Adapter.PermissionRequests, Is.EqualTo(0));
                Assert.That(rig.Statuses.Count, Is.EqualTo(0), "el estado no cambia");
            }
        }

        [Test, Category("AC-APP-13-02")]
        public void FirstLaunchShowsScanNotDenied()
        {
            // QA-G10a-02: sin pedir el permiso todavía, la vista es la de búsqueda (no "Permiso de Bluetooth denegado").
            using (var rig = new BleRig())
            {
                rig.Adapter.PermissionNotAsked();
                Assert.That(rig.Rower.AdapterState.Value, Is.EqualTo(BleAdapterState.On));
                Assert.That(DeviceScreenRules.ViewFor(rig.Status, rig.Rower.AdapterState.Value), Is.EqualTo(DeviceViewKind.Scan));
            }
        }

        [Test, Category("AC-APP-13-02")]
        public void ScanAsksPermissionBeforeAdapterWait()
        {
            // DT10-16: el permiso se pide al tocar Buscar, ANTES de la espera de 5 s (con el orden del legacy no aparecería).
            using (var rig = new BleRig(initial: BleAdapterState.Unknown))
            {
                rig.Adapter.PermissionNotAsked();
                rig.Rower.StartScanAsync();
                Assert.That(rig.Adapter.PermissionRequests, Is.EqualTo(1), "en el acto, sin esperar el adaptador");
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0), "la búsqueda espera el On");
                rig.Adapter.SetState(BleAdapterState.On);
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-APP-13-02")]
        public void PermissionGrantedScansWithoutAnotherTap()
        {
            using (var rig = new BleRig())
            {
                rig.Adapter.PermissionNotAsked();
                rig.Adapter.AnswerPermission(true);
                var lists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices);
                rig.Scan(FakeBleDevices.Row);
                Assert.That(rig.Adapter.PermissionRequests, Is.EqualTo(1));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Scanning), "concedido, la búsqueda arranca sola");
                Assert.That(Ids(LastList(lists)), Is.EqualTo(new[] { FakeBleDevices.RowId }));
                rig.Wait(10);
                rig.Scan();
                Assert.That(rig.Adapter.PermissionRequests, Is.EqualTo(1), "concedido una vez, no se vuelve a pedir");
            }
        }

        [Test, Category("AC-APP-13-02"), Category("AC-BLE-04-02")]
        public void PermissionDeniedIsNotABanner()
        {
            // DV-100: negado, Dispositivo muestra "sin permiso", no "Error al escanear".
            using (var rig = new BleRig())
            {
                rig.Adapter.PermissionNotAsked();
                rig.Adapter.AnswerPermission(false);
                var store = rig.Store();
                var scan = store.StartScanAsync();
                Assert.That(scan.IsCompleted, Is.True);
                Assert.That(store.State.Value.Error, Is.Null, "sin banner");
                Assert.That(rig.Adapter.ScanStarts, Is.EqualTo(0));
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(rig.Rower.AdapterState.Value, Is.EqualTo(BleAdapterState.Unauthorized));
                Assert.That(DeviceScreenRules.ViewFor(rig.Status, rig.Rower.AdapterState.Value), Is.EqualTo(DeviceViewKind.BluetoothState));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-05-04")]
        public void TenSecondsFtmsOnly()
        {
            // bs:65, :86, :93-96, :113-121: buscando 10 s, solo FTMS; después, desconectado.
            using (var rig = new BleRig())
            {
                var scan = rig.Rower.StartScanAsync();
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Scanning));
                Assert.That(rig.Adapter.LastScanServices, Is.EqualTo(new[] { FtmsUuids.Service }));
                Assert.That(DeviceScreenRules.Scan(rig.Status, false, 0).ShowEmptyState, Is.False, "buscando sin resultados no se ve el vacío");
                rig.At(9.999);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Scanning));
                rig.At(10);
                Assert.That(scan.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(rig.Adapter.Scanning, Is.False, "el escaneo del adaptador se detiene a los 10 s");
                Assert.That(rig.Statuses.Values, Is.EqualTo(new[] { BleStatus.Scanning, BleStatus.Disconnected }));
            }
        }

        [Test, Category("AC-BLE-05-04")]
        public void ClearsErrorAndList()
        {
            // dp:104-114: buscar borra el error y la lista anterior.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                rig.Adapter.FailNextScan(new BleException("scan", "error 2", 2));
                store.StartScanAsync();
                Assert.That(store.State.Value.Error, Is.Not.Null);
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(1));
                rig.Wait(10);
                Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(1), "la lista queda hasta la próxima búsqueda");
                store.StartScanAsync();
                Assert.That(store.State.Value.Error, Is.Null);
                Assert.That(store.State.Value.ScanResults, Is.Empty, "la nueva búsqueda borra la lista");
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-05-05")]
        public void K10OrderDedupFirstSeen()
        {
            // bs:87, :97-109: K-HP, K-ROW, K-ANON, K-BIKE, K-ROW → AMS-670B, sin nombre y Bici FTMS, una vez cada uno, en el orden del
            // primer descubrimiento; el repetido se reemplaza en su lugar. La lista se publica en cada resultado aceptado.
            using (var rig = new BleRig())
            {
                var lists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices);
                rig.Scan(FakeBleDevices.Hp, FakeBleDevices.Row, FakeBleDevices.Anon, FakeBleDevices.Bike, FakeBleDevices.Row);
                Assert.That(lists.Count, Is.EqualTo(4), "en vivo: uno por resultado aceptado");
                Assert.That(Ids(lists.Values[0]), Is.EqualTo(new[] { FakeBleDevices.RowId }));
                Assert.That(Ids(LastList(lists)), Is.EqualTo(new[] { FakeBleDevices.RowId, FakeBleDevices.AnonId, FakeBleDevices.BikeId }));
                var store = rig.Store();
                Assert.That(store.State.Value.ScanResults, Is.Empty, "el store nuevo no ve los resultados anteriores");
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-05-05"), Category("AC-BLE-SEC-01")]
        public void ClientSideFilterDropsHeadphones()
        {
            // sf:12-20: en Windows el plugin no filtra y lo hace la app; en Android lo hace el sistema. En los dos, Auriculares nunca.
            foreach (var osFilter in new[] { false, true })
            {
                using (var rig = new BleRig(filtersScanInOs: osFilter))
                {
                    var lists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices);
                    rig.Scan(FakeBleDevices.Hp, FakeBleDevices.Row);
                    Assert.That(Ids(LastList(lists)), Is.EqualTo(new[] { FakeBleDevices.RowId }), "filtro del sistema: " + osFilter);
                }
            }
        }

        [Test, Category("AC-BLE-05-05")]
        public void OnlyThisScanCounts()
        {
            // bs:97-99: un anuncio fuera de la búsqueda (antes o después) no aparece; tampoco los de una búsqueda anterior.
            using (var rig = new BleRig())
            {
                var lists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices);
                rig.Adapter.Advertise(FakeBleDevices.Bike);
                rig.Scan(FakeBleDevices.Row);
                rig.Wait(10);
                rig.Adapter.Advertise(FakeBleDevices.Anon);
                Assert.That(Ids(LastList(lists)), Is.EqualTo(new[] { FakeBleDevices.RowId }));
                rig.Scan(FakeBleDevices.NoCp);
                Assert.That(Ids(LastList(lists)), Is.EqualTo(new[] { FakeBleDevices.NoCpId }), "la lista nueva no trae la anterior");
            }
        }

        [Test, Category("AC-BLE-05-06")]
        public void EndsToDisconnectedUnlessConnecting()
        {
            // bs:118-121: a los 10 s vuelve a desconectado salvo que ya esté conectando o conectado.
            using (var rig = new BleRig())
            {
                rig.Adapter.NeverRespond(FakeBleDevices.RowId);
                rig.Scan(FakeBleDevices.Row);
                rig.At(2);
                rig.Rower.ConnectAsync(FakeBleDevices.RowId);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connecting));
                rig.At(10);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connecting), "conectando: la búsqueda no lo pisa");
            }

            using (var rig = new BleRig())
            {
                rig.Scan(FakeBleDevices.Row);
                rig.At(2);
                rig.Rower.ConnectAsync(FakeBleDevices.RowId);
                rig.At(10);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected), "conectado: tampoco");
            }
        }

        [Test, Category("AC-BLE-05-06")]
        public void ListSurvivesConnectDisconnect()
        {
            // dp:116, bs:88-90: conectar no cambia la lista; queda hasta la próxima búsqueda, también después de desconectar.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                rig.Adapter.Advertise(FakeBleDevices.Anon);
                rig.At(1);
                store.ConnectAsync(FakeBleDevices.RowId);
                rig.At(2);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Connected));
                Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(2));
                store.DisconnectAsync();
                rig.At(12);
                Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(2));
                Assert.That(DeviceScreenRules.ViewFor(store.State.Value.Status, store.State.Value.AdapterState), Is.EqualTo(DeviceViewKind.Scan));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-05-06")]
        public void OldScanDoesNotClobberNew()
        {
            // Q-39 (bs:88-90, :114-122): una búsqueda nueva que empieza mientras termina la vieja no pierde ni el estado ni la lista.
            using (var rig = new BleRig())
            {
                var lists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices);
                var first = rig.Rower.StartScanAsync();
                rig.At(9);
                var second = rig.Rower.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Row);
                rig.At(10);
                Assert.That(first.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Scanning), "la vieja no baja el estado de la nueva");
                Assert.That(rig.Adapter.Scanning, Is.True, "ni detiene el escaneo de la nueva");
                rig.Adapter.Advertise(FakeBleDevices.Anon);
                Assert.That(Ids(LastList(lists)), Is.EqualTo(new[] { FakeBleDevices.RowId, FakeBleDevices.AnonId }));
                rig.At(19);
                Assert.That(second.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
            }
        }

        [Test, Category("AC-BLE-05-08")]
        public void ScanFailureResetsStatus()
        {
            // bs:118-121, dp:110-112: si startScan lanza, vuelve a desconectado (Buscar no queda deshabilitado) y hay banner.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                rig.Adapter.FailNextScan(new BleException("scan", "error 2", 2));
                var scan = store.StartScanAsync();
                Assert.That(scan.IsCompleted, Is.True);
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(store.State.Value.ErrorKind, Is.EqualTo(DeviceErrorKind.Scan));
                Assert.That(DeviceScreenRules.ErrorText(store.State.Value, LocalizationFixture.Es), Is.EqualTo("Error al escanear: scan: error 2"));
                Assert.That(DeviceScreenRules.Scan(rig.Status, false, 0).SearchEnabled, Is.True);
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-05-08")]
        public void AsyncScanFailureEndsTheScanWithABanner()
        {
            // QA-G10a-08: el fallo asíncrono de Android (onScanFailed) termina la búsqueda con el banner; el de permiso no.
            using (var rig = new BleRig())
            {
                var store = rig.Store();
                var scan = store.StartScanAsync();
                rig.At(3);
                rig.Adapter.EmitScanFailure(new BleException("scan", "SCAN_FAILED_APPLICATION_REGISTRATION_FAILED", 2));
                Assert.That(scan.IsCompleted, Is.True, "no espera los 10 s");
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                Assert.That(store.State.Value.ErrorKind, Is.EqualTo(DeviceErrorKind.Scan));
                Assert.That(store.State.Value.Error!.Message, Does.Contain("SCAN_FAILED"));

                var denied = store.StartScanAsync();
                rig.At(4);
                rig.Adapter.EmitScanFailure(new BlePermissionDeniedException("scan"));
                Assert.That(denied.IsCompleted, Is.True);
                Assert.That(store.State.Value.Error, Is.Null, "sin permiso no es un banner (DV-100)");
                Assert.That(rig.Status, Is.EqualTo(BleStatus.Disconnected));
                store.Dispose();
            }
        }

        [Test, Category("AC-BLE-SEC-01")]
        public void HeadphonesNeverListedOrConnected()
        {
            // Solo se conecta a lo que vino de una búsqueda FTMS: K-HP nunca aparece y tampoco se le puede conectar (DT10-04).
            using (var rig = new BleRig())
            {
                var lists = new StateRecorder<IReadOnlyList<BleScanResult>>(rig.Rower.Devices);
                var store = rig.Store();
                store.StartScanAsync();
                rig.Adapter.Advertise(FakeBleDevices.Hp);
                rig.Wait(10);
                Assert.That(lists.Count, Is.EqualTo(0));
                store.ConnectAsync(FakeBleDevices.HpId);
                Assert.That(rig.Adapter.ConnectCalls(FakeBleDevices.HpId), Is.EqualTo(0), "nunca hay conexión");
                Assert.That(store.State.Value.Error, Is.InstanceOf<ArgumentException>());
                Assert.That(rig.Logger.Lines, Has.None.Contains("Auriculares"), "ni se registra su nombre");
                store.Dispose();
            }
        }
    }
}
