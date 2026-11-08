using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // NBA: el puente nativo puro (10a 02-tech-design §2.6) con el plugin falso (FakeNativeBleApi, su propio reloj y el formato v1):
    // correlación de pedidos, caídas, sesiones por id (DT10-17, QA-G10a-03), permiso de Android (DT10-15, DT10-16, QA-G10a-02),
    // versión de la API y la bomba sin asignar cuando no hay nada.
    [TestFixture]
    public sealed class NativeBleAdapterTests
    {
        private sealed class Rig : IDisposable
        {
            private readonly IDisposable _frames;
            private readonly InlineContinuations _inline = new InlineContinuations();

            public Rig(FakeBlePermissions? permissions = null, bool android = false, int apiVersion = BleWire.ApiVersion, int initCode = 0)
            {
                Logger = new RecordingLogger();
                Scheduler = new FakeScheduler();
                Lifecycle = new FakeAppLifecycle(Logger);
                Api = new FakeNativeBleApi(() => Scheduler.MonotonicSeconds + 1000.25, Scheduler, android, android)
                {
                    ApiVersion = apiVersion,
                    InitCode = initCode,
                };
                Permissions = permissions;
                Adapter = new NativeBleAdapter(Api, permissions, Scheduler, Lifecycle, Logger);
                // Un "cuadro" cada 1/60 s: la bomba antes del resto (AppRoot).
                _frames = Scheduler.SchedulePeriodic(TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60), () => Adapter.Drain(false));
            }

            public RecordingLogger Logger { get; }

            public FakeScheduler Scheduler { get; }

            public FakeAppLifecycle Lifecycle { get; }

            public FakeNativeBleApi Api { get; }

            public FakeBlePermissions? Permissions { get; }

            public NativeBleAdapter Adapter { get; }

            public void Frames(double seconds)
            {
                Scheduler.Advance(TimeSpan.FromTicks((long)(seconds * TimeSpan.TicksPerSecond + 0.5)));
            }

            // Conecta K-ROW (con el plazo que se pase) y deja pasar unos cuadros.
            public Task<IBlePeripheral> Connect(CancellationToken ct = default)
            {
                var task = Adapter.ConnectAsync(FakeBleDevices.RowId, true, ct);
                Frames(0.05);
                return task;
            }

            public void Dispose()
            {
                _frames.Dispose();
                Adapter.Dispose();
                _inline.Dispose();
            }
        }

        // ── Pedidos, eventos y versión ──

        [Test, Category("AC-BLE-06-02")]
        public void RequestIdsResolveTheirOwnTasks()
        {
            using (var rig = new Rig())
            {
                var connect = rig.Connect();
                Assert.That(connect.Status, Is.EqualTo(TaskStatus.RanToCompletion));
                var peripheral = connect.Result;
                Assert.That(peripheral.DeviceId, Is.EqualTo(FakeBleDevices.RowId));
                var discover = peripheral.DiscoverServicesAsync();
                var write = peripheral.WriteAsync(FtmsUuids.Service, FtmsUuids.ControlPoint, new byte[] { 0x00 }, true);
                var notify = peripheral.SetNotifyAsync(FtmsUuids.Service, FtmsUuids.RowerData, true);
                Assert.That(rig.Adapter.PendingRequests, Is.EqualTo(3));
                rig.Frames(0.05);
                Assert.That(rig.Adapter.PendingRequests, Is.EqualTo(0));
                Assert.That(write.Status, Is.EqualTo(TaskStatus.RanToCompletion));
                Assert.That(notify.Status, Is.EqualTo(TaskStatus.RanToCompletion));
                Assert.That(discover.Result.Count, Is.EqualTo(2));
                Assert.That(discover.Result[1].Uuid, Is.EqualTo(FtmsUuids.Service));
                Assert.That(discover.Result[1].Characteristics[2].Uuid, Is.EqualTo(FtmsUuids.ControlPoint));
                Assert.That(discover.Result[1].Characteristics[2].Properties,
                    Is.EqualTo(BleCharacteristicProperties.Write | BleCharacteristicProperties.Indicate));
                Assert.That(rig.Api.Writes[0].Characteristic, Is.EqualTo(FtmsUuids.ControlPointText));
            }
        }

        [Test, Category("AC-BLE-06-02")]
        public void NotificationsCarryTheAppClock()
        {
            // Primer plano: el evento sale en el acto con su hora en el reloj de la app (el desfase calibrado).
            using (var rig = new Rig())
            {
                var peripheral = rig.Connect().Result;
                var received = new List<BleNotification>();
                peripheral.Notifications.Subscribe(received.Add);
                rig.Frames(1);
                var sent = rig.Scheduler.MonotonicSeconds;
                rig.Api.NotifyNow(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
                rig.Frames(1.0 / 60);
                Assert.That(received.Count, Is.EqualTo(1));
                Assert.That(received[0].Value, Is.EqualTo(FakeBleDevices.RowerData));
                Assert.That(received[0].Characteristic, Is.EqualTo(FtmsUuids.RowerData));
                Assert.That(received[0].MonotonicSeconds, Is.EqualTo(sent).Within(1e-6));
            }
        }

        [Test, Category("AC-BLE-05-05")]
        public void ScanResultsOnlyWhileScanning()
        {
            using (var rig = new Rig())
            {
                var results = new List<BleScanResult>();
                rig.Adapter.ScanResults.Subscribe(results.Add);
                rig.Api.AdvertiseNow(FakeBleDevices.Row);
                rig.Frames(0.05);
                Assert.That(results, Is.Empty, "sin escaneo no hay resultados");
                rig.Adapter.StartScanAsync(new[] { FtmsUuids.Service });
                Assert.That(rig.Api.LastScanCsv, Is.EqualTo(FtmsUuids.ServiceText));
                rig.Api.AdvertiseNow(FakeBleDevices.Row);
                rig.Api.AdvertiseNow(FakeBleDevices.Anon);
                rig.Frames(0.05);
                Assert.That(results.Count, Is.EqualTo(2));
                Assert.That(results[0].PlatformName, Is.EqualTo("AMS-670B"));
                Assert.That(results[1].PlatformName, Is.Null);
                Assert.That(results[0].ServiceUuids, Is.EqualTo(new[] { FtmsUuids.Service }));
                rig.Adapter.StopScanAsync();
                rig.Api.AdvertiseNow(FakeBleDevices.Bike);
                rig.Frames(0.05);
                Assert.That(results.Count, Is.EqualTo(2));
            }
        }

        [Test, Category("AC-BLE-08-02")]
        public void UnrequestedDisconnectRaisesLinkLost()
        {
            using (var rig = new Rig())
            {
                var peripheral = rig.Connect().Result;
                var lost = new List<BleException>();
                peripheral.LinkLost.Subscribe(lost.Add);
                rig.Api.DropLinkNow(FakeBleDevices.RowId, 8);
                rig.Frames(0.05);
                Assert.That(lost.Count, Is.EqualTo(1));
                Assert.That(lost[0].Code, Is.EqualTo(8));
                var again = peripheral.WriteAsync(FtmsUuids.Service, FtmsUuids.ControlPoint, new byte[] { 0x07 }, true);
                Assert.That(again.IsFaulted, Is.True, "el periférico caído ya no escribe");
            }
        }

        [Test, Category("AC-BLE-04-06")]
        public void OtherApiVersionIsUnsupported()
        {
            using (var rig = new Rig(apiVersion: 2))
            {
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unsupported));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Plugin nativo no disponible"));
                Assert.That(rig.Adapter.StartScanAsync(new[] { FtmsUuids.Service }).IsFaulted, Is.True);
            }

            using (var rig = new Rig(initCode: -1))
            {
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unsupported));
            }

            using (var rig = new Rig())
            {
                rig.Api.AdapterStateNow(BleAdapterState.Unsupported);
                rig.Frames(0.05);
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unsupported), "el plugin dice que no hay Bluetooth LE");
            }
        }

        [Test, Category("AC-BLE-04-01")]
        public void AdapterEventsUpdateTheState()
        {
            using (var rig = new Rig())
            {
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.On), "lectura inicial");
                rig.Api.AdapterStateNow(BleAdapterState.Off);
                rig.Frames(0.05);
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Off));
            }
        }

        [Test]
        public void PendingZeroDoesNotDrain()
        {
            // Sin eventos, la bomba solo pregunta cuántos bytes hay (sin asignar: no llama a Drain).
            using (var rig = new Rig())
            {
                rig.Frames(1);
                Assert.That(rig.Api.PendingCalls, Is.GreaterThanOrEqualTo(60));
                Assert.That(rig.Api.DrainCalls, Is.EqualTo(0));
                rig.Api.AdapterStateNow(BleAdapterState.On);
                rig.Frames(1.0 / 60);
                Assert.That(rig.Api.DrainCalls, Is.EqualTo(1));
            }
        }

        // ── Sesiones (DT10-17, QA-G10a-03) ──

        [Test, Category("AC-BLE-06-01")]
        public void TimeoutCancelsNativeConnect()
        {
            using (var rig = new Rig())
            {
                rig.Api.NeverRespond(FakeBleDevices.RowId);
                var cts = new CancellationTokenSource();
                var connect = rig.Connect(cts.Token);
                Assert.That(rig.Api.LiveSessions(FakeBleDevices.RowId), Is.EqualTo(1));
                cts.Cancel();
                Assert.That(connect.IsCanceled, Is.True);
                Assert.That(rig.Api.Disconnects(FakeBleDevices.RowId), Is.EqualTo(1), "cierra la sesión por id");
                Assert.That(rig.Api.LiveSessions(FakeBleDevices.RowId), Is.EqualTo(0));
                rig.Frames(0.1);
                Assert.That(rig.Adapter.PendingRequests, Is.EqualTo(0));
            }
        }

        [Test, Category("AC-BLE-06-01")]
        public void LateConnectAfterTimeoutIsClosed()
        {
            // Un plugin que igual termina el connect después de cancelado: el éxito tardío cierra la sesión (y nada más).
            using (var rig = new Rig())
            {
                rig.Api.RespondAfter(FakeBleDevices.RowId, TimeSpan.FromSeconds(20), ignoreDisconnect: true);
                var cts = new CancellationTokenSource();
                var connect = rig.Connect(cts.Token);
                rig.Frames(15);
                cts.Cancel();
                Assert.That(connect.IsCanceled, Is.True);
                Assert.That(rig.Api.Disconnects(FakeBleDevices.RowId), Is.EqualTo(1));
                rig.Frames(6);
                Assert.That(rig.Api.Disconnects(FakeBleDevices.RowId), Is.EqualTo(2), "el éxito tardío se cierra");
                Assert.That(rig.Api.LiveSessions(FakeBleDevices.RowId), Is.EqualTo(0));
                Assert.That(rig.Logger.Lines, Has.Some.Contains("Conexión tardía cerrada: AA:BB:…:70"));
            }
        }

        [Test, Category("AC-BLE-08-03")]
        public void LinkLostClosesSession()
        {
            using (var rig = new Rig())
            {
                var peripheral = rig.Connect().Result;
                var disconnectsAtLost = -1;
                peripheral.LinkLost.Subscribe(_ => disconnectsAtLost = rig.Api.Disconnects(FakeBleDevices.RowId));
                rig.Api.DropLinkNow(FakeBleDevices.RowId);
                rig.Frames(0.05);
                Assert.That(disconnectsAtLost, Is.EqualTo(1), "la sesión se cierra ANTES de avisar");
                Assert.That(rig.Api.LiveSessions(FakeBleDevices.RowId), Is.EqualTo(0));
                peripheral.Dispose();
                Assert.That(rig.Api.Disconnects(FakeBleDevices.RowId), Is.EqualTo(1), "Dispose después no vuelve a cerrar");
            }
        }

        [Test, Category("AC-BLE-08-03")]
        public void RetriesDoNotLeakSessions()
        {
            // 60 s de reintentos de RowerService contra un remo que no responde: nunca más de una sesión viva.
            using (var rig = new Rig())
            {
                var rower = new RowerService(rig.Adapter, rig.Scheduler, rig.Lifecycle, rig.Logger);
                rower.StartScanAsync();
                rig.Api.AdvertiseNow(FakeBleDevices.Row);
                rig.Frames(0.1);
                rower.ConnectAsync(FakeBleDevices.RowId);
                rig.Frames(1);
                Assert.That(rower.Status.Value, Is.EqualTo(BleStatus.Connected));
                rig.Api.NeverRespond(FakeBleDevices.RowId);
                rig.Api.DropLinkNow(FakeBleDevices.RowId);
                var max = 0;
                using (rig.Scheduler.SchedulePeriodic(TimeSpan.FromMilliseconds(100), () => max = Math.Max(max, rig.Api.LiveSessions(FakeBleDevices.RowId))))
                {
                    rig.Frames(60);
                }

                Assert.That(rig.Api.Connects(FakeBleDevices.RowId), Is.GreaterThanOrEqualTo(5), "reintenta");
                Assert.That(max, Is.LessThanOrEqualTo(1));
                Assert.That(rig.Api.Replaced, Is.EqualTo(0), "cada intento cerró el anterior");
                rower.Dispose();
            }
        }

        // ── Permiso (DT10-15, DT10-16) ──

        [Test, Category("AC-BLE-04-02"), Category("AC-APP-13-02")]
        public void NotAskedIsNotUnauthorized()
        {
            using (var rig = new Rig(new FakeBlePermissions(BlePermissionState.NotAsked), android: true))
            {
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.On));
            }
        }

        [Test, Category("AC-BLE-04-02")]
        public void DeniedIsUnauthorized()
        {
            foreach (var state in new[] { BlePermissionState.Denied, BlePermissionState.DeniedForever })
            {
                using (var rig = new Rig(new FakeBlePermissions(state), android: true))
                {
                    Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unauthorized), state.ToString());
                }
            }
        }

        [Test, Category("AC-APP-13-02")]
        public void EnsureScanPermissionAsksOnlyWhenItCan()
        {
            var permissions = new FakeBlePermissions(BlePermissionState.NotAsked);
            using (var rig = new Rig(permissions, android: true))
            {
                permissions.Answer(BlePermissionState.Denied);
                Assert.That(rig.Adapter.EnsureScanPermissionAsync().Result, Is.False);
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unauthorized));
                permissions.Answer(BlePermissionState.Granted);
                Assert.That(rig.Adapter.EnsureScanPermissionAsync().Result, Is.True, "Denied: se vuelve a pedir");
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.On));
                Assert.That(permissions.Requests, Is.EqualTo(2));
                Assert.That(rig.Adapter.EnsureScanPermissionAsync().Result, Is.True);
                Assert.That(permissions.Requests, Is.EqualTo(2), "concedido: sin diálogo");
                permissions.State = BlePermissionState.DeniedForever;
                Assert.That(rig.Adapter.EnsureScanPermissionAsync().Result, Is.False);
                Assert.That(permissions.Requests, Is.EqualTo(2), "para siempre: sin diálogo");
            }

            using (var rig = new Rig())
            {
                Assert.That(rig.Adapter.EnsureScanPermissionAsync().Result, Is.True, "Windows: sin permisos");
            }
        }

        [Test, Category("AC-BLE-04-04")]
        public void RequestEnableAsksPermissionFirst()
        {
            var permissions = new FakeBlePermissions(BlePermissionState.Denied);
            using (var rig = new Rig(permissions, android: true))
            {
                rig.Api.AdapterStateValue = (int)BleAdapterState.Off;
                rig.Api.AdapterStateNow(BleAdapterState.Off);
                rig.Frames(0.05);
                rig.Adapter.RequestEnableAsync();
                Assert.That(permissions.Requests, Is.EqualTo(1), "primero BLUETOOTH_CONNECT");
                Assert.That(rig.Api.RequestEnableCalls, Is.EqualTo(1), "concedido: el pedido de encender");
                Assert.That(permissions.SettingsOpens, Is.EqualTo(0));
            }
        }

        [Test, Category("AC-BLE-04-04")]
        public void RequestEnableDeniedForeverOpensAppSettings()
        {
            var permissions = new FakeBlePermissions(BlePermissionState.DeniedForever);
            using (var rig = new Rig(permissions, android: true))
            {
                var task = rig.Adapter.RequestEnableAsync();
                Assert.That(task.IsCompleted && !task.IsFaulted, Is.True);
                Assert.That(permissions.Requests, Is.EqualTo(0));
                Assert.That(permissions.SettingsOpens, Is.EqualTo(1));
                Assert.That(rig.Api.RequestEnableCalls, Is.EqualTo(0));
            }

            permissions = new FakeBlePermissions(BlePermissionState.Denied);
            using (var rig = new Rig(permissions, android: true))
            {
                // El sistema ya no muestra el diálogo (queda para siempre): los ajustes de la app.
                permissions.Answer(BlePermissionState.DeniedForever);
                rig.Adapter.RequestEnableAsync();
                Assert.That(permissions.SettingsOpens, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-BLE-04-04")]
        public void RequestEnableSecurityErrorOpensAppSettings()
        {
            var permissions = new FakeBlePermissions(BlePermissionState.Granted);
            using (var rig = new Rig(permissions, android: true))
            {
                rig.Api.AdapterStateNow(BleAdapterState.Off);
                rig.Frames(0.05);
                rig.Api.RequestEnableCode = BleWire.ErrorPermission;
                var task = rig.Adapter.RequestEnableAsync();
                Assert.That(task.IsCompleted && !task.IsFaulted, Is.True, "nunca un error");
                Assert.That(rig.Api.RequestEnableCalls, Is.EqualTo(1));
                Assert.That(permissions.SettingsOpens, Is.EqualTo(1));
            }

            using (var rig = new Rig())
            {
                // Windows: abre los ajustes de Bluetooth (la API) y no hay permisos.
                rig.Api.AdapterStateNow(BleAdapterState.Off);
                rig.Frames(0.05);
                rig.Adapter.RequestEnableAsync();
                Assert.That(rig.Api.RequestEnableCalls, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-APP-13-02")]
        public void ResumeRereadsPermission()
        {
            // Al volver de los ajustes con el permiso dado, la vista pasa a la búsqueda.
            var permissions = new FakeBlePermissions(BlePermissionState.Denied);
            using (var rig = new Rig(permissions, android: true))
            {
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unauthorized));
                permissions.ChangeInSettings(BlePermissionState.Granted);
                rig.Adapter.Drain(true);
                Assert.That(permissions.Refreshes, Is.EqualTo(1));
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.On));
            }
        }

        [Test, Category("AC-APP-13-02")]
        public void ScanFailedWithoutPermissionIsNotABanner()
        {
            var permissions = new FakeBlePermissions(BlePermissionState.Granted);
            using (var rig = new Rig(permissions, android: true))
            {
                var failures = new List<BleException>();
                rig.Adapter.ScanFailed.Subscribe(failures.Add);
                rig.Adapter.StartScanAsync(new[] { FtmsUuids.Service });
                permissions.ChangeInSettings(BlePermissionState.DeniedForever);
                rig.Api.ScanFailedNow(BleWire.ErrorPermission, "SecurityException");
                rig.Frames(0.05);
                Assert.That(failures.Count, Is.EqualTo(1));
                Assert.That(failures[0], Is.InstanceOf<BlePermissionDeniedException>());
                Assert.That(rig.Adapter.State.Value, Is.EqualTo(BleAdapterState.Unauthorized));

                rig.Api.ScanFailedNow(2, "SCAN_FAILED_APPLICATION_REGISTRATION_FAILED");
                rig.Frames(0.05);
                Assert.That(failures[1], Is.Not.InstanceOf<BlePermissionDeniedException>());
                Assert.That(failures[1].Message, Is.EqualTo("scan: SCAN_FAILED_APPLICATION_REGISTRATION_FAILED"));
                Assert.That(failures[1].Code, Is.EqualTo(2));
            }
        }
    }
}
