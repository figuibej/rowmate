using System;
using System.Threading;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Device
{
    // HeartRateStore = lib/features/device/heart_rate_provider.dart, con el pulsómetro de prueba (01-po-spec §0.4).
    // Los casos legacy (LT-32 a LT-34) están en Tests/Simulator/HeartRateProviderTests (usan el pulsómetro simulado).
    [TestFixture]
    public sealed class HeartRateStoreTests
    {
        private static readonly BleScanResult Polar = new BleScanResult("7E:1A:2B:3C", "Polar H10 7E1A2B3C", null, -60,
            new[] { HeartRateParser.Service });

        [Test, Category("AC-HRM-12-01")]
        public void InitialState()
        {
            var store = new HeartRateStore(new ScriptedHeartRateService(), new RecordingLogger());
            var state = store.State.Value;
            Assert.That(state.Status, Is.EqualTo(HrmStatus.Disconnected), "al empezar: desconectado");
            Assert.That(state.HasRemembered, Is.False, "sin recordado");
            Assert.That(state.ScanResults, Is.Empty, "lista vacía");
            Assert.That(state.IsConnected || state.IsScanning || state.IsConnecting || state.IsRetrying, Is.False);
            Assert.That(state.ConnectedDeviceName, Is.Null);
            Assert.That(state.RememberedDeviceName, Is.Null);
            Assert.That(state.Error, Is.Null);
            store.Dispose();
        }

        [Test, Category("AC-HRM-12-01")]
        public void ReflectsTheServiceAndNotifiesEveryChange()
        {
            var hrm = new ScriptedHeartRateService { ScanResults = new[] { Polar } };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            var recorder = new StateRecorder<HeartRateState>(store.State);
            store.StartScanAsync();
            Assert.That(store.State.Value.IsScanning, Is.True);
            Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(1));
            store.ConnectAsync(Polar.DeviceId, Polar.PlatformName);
            Assert.That(store.State.Value.IsConnected, Is.True);
            Assert.That(store.State.Value.ConnectedDeviceName, Is.EqualTo(ScriptedHeartRateService.RememberedSample));
            hrm.EmitBpm(120);
            hrm.EmitBpm(120);
            Assert.That(store.State.Value.Bpm, Is.EqualTo(120));
            Assert.That(recorder.Count, Is.EqualTo(7), "aviso por: búsqueda (2 + lista), conexión (2), dos pulsos iguales");
            store.Dispose();
        }

        [Test, Category("AC-HRM-04-03")]
        public void InitialBpmIsZero()
        {
            var store = new HeartRateStore(new ScriptedHeartRateService(), new RecordingLogger());
            Assert.That(store.State.Value.Bpm, Is.EqualTo(0), "0 = sin lectura válida");
            store.Dispose();
        }

        [Test, Category("AC-HRM-12-02")]
        public void ChosenNameOnlyForThatAttempt()
        {
            var hrm = new ScriptedHeartRateService { ConnectBehavior = ScriptedConnect.StayConnecting };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            store.ConnectAsync(Polar.DeviceId, "Polar H10 7E1A2B3C");
            Assert.That(store.State.Value.IsConnecting, Is.True);
            Assert.That(store.State.Value.ConnectingName, Is.EqualTo("Polar H10 7E1A2B3C"), "el elegido en la lista");

            hrm.SetStatus(HrmStatus.Disconnected);
            Assert.That(store.State.Value.ChosenName, Is.Null, "cualquier estado distinto de conectando lo descarta");
            hrm.RememberedDeviceName = "Recordado";
            hrm.IsRetrying = true;
            hrm.SetStatus(HrmStatus.Connecting);
            Assert.That(store.State.Value.ConnectingName, Is.EqualTo("Recordado"), "los reintentos muestran el recordado");
            hrm.RememberedDeviceName = null;
            hrm.SetStatus(HrmStatus.Connecting);
            Assert.That(store.State.Value.ConnectingName, Is.EqualTo(""), "si no hay ninguno, vacío");
            store.Dispose();
        }

        [Test, Category("AC-HRM-12-03")]
        public void ErrorClearedOnStartAndOnConnectedNotOnDisconnected()
        {
            var hrm = new ScriptedHeartRateService { ConnectBehavior = ScriptedConnect.ThrowError, ErrorText = "GATT 133" };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            store.ConnectAsync(Polar.DeviceId, "Polar");
            Assert.That(store.State.Value.Error!.Message, Is.EqualTo("GATT 133"), "la falla queda visible");

            hrm.SetStatus(HrmStatus.Disconnected);
            Assert.That(store.State.Value.Error, Is.Not.Null, "pasar a desconectado NO la borra");
            hrm.SetStatus(HrmStatus.Connected);
            Assert.That(store.State.Value.Error, Is.Null, "llegar a conectado la borra");

            store.ConnectAsync(Polar.DeviceId, "Polar");
            Assert.That(store.State.Value.Error, Is.Not.Null);
            hrm.ConnectBehavior = ScriptedConnect.Pending;
            WithMainThread(context =>
            {
                store.ConnectAsync(Polar.DeviceId, "Polar");
                Assert.That(store.State.Value.Error, Is.Null, "empezar cualquier acción la borra");
                hrm.FailPending(new InvalidOperationException("otra"));
                context.RunUntilIdle(); // la continuación del await vuelve al hilo principal (como con UnitySynchronizationContext)
                Assert.That(store.State.Value.Error!.Message, Is.EqualTo("otra"));
            });
            store.DisconnectAsync();
            Assert.That(store.State.Value.Error, Is.Null, "desconectar también empieza limpio");
            store.Dispose();
        }

        // Instala un TestMainThreadContext mientras corre body (las continuaciones de un await se postean a su cola, como en
        // el hilo principal de Unity) y restaura el anterior. El test bombea con RunUntilIdle cuando completa una tarea.
        private static void WithMainThread(Action<TestMainThreadContext> body)
        {
            var previous = SynchronizationContext.Current;
            var context = new TestMainThreadContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                body(context);
                context.RunUntilIdle();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        [Test, Category("AC-HRM-12-03")]
        public void EveryActionCapturesItsFailure()
        {
            var hrm = new ScriptedHeartRateService
            {
                ScanError = new InvalidOperationException("scan"),
                AutoConnectError = new InvalidOperationException("retry"),
            };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            Assert.That(store.StartScanAsync().IsFaulted, Is.False);
            Assert.That(store.State.Value.Error!.Message, Is.EqualTo("scan"));
            Assert.That(store.RetryAsync().IsFaulted, Is.False);
            Assert.That(store.State.Value.Error!.Message, Is.EqualTo("retry"));
            store.Dispose();
        }

        [Test, Category("AC-HRM-12-05")]
        public void StartScanClearsErrorAndList()
        {
            var hrm = new ScriptedHeartRateService { ConnectBehavior = ScriptedConnect.ThrowIncompatible, ScanResults = new[] { Polar } };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            hrm.EmitDevices(new[] { Polar, Polar });
            store.ConnectAsync(Polar.DeviceId, "Polar");
            Assert.That(store.State.Value.Error, Is.InstanceOf<HrmIncompatibleException>());
            Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(2));

            var recorder = new StateRecorder<HeartRateState>(store.State);
            store.StartScanAsync();
            Assert.That(recorder.First.Error, Is.Null, "antes de empezar se borra el error");
            Assert.That(recorder.First.ScanResults, Is.Empty, "y la lista anterior");
            Assert.That(recorder.First.Status, Is.EqualTo(HrmStatus.Disconnected), "todavía sin buscar");
            Assert.That(store.State.Value.ScanResults.Count, Is.EqualTo(1));
            store.Dispose();
        }

        [Test, Category("AC-HRM-12-06")]
        public void NoNotificationAfterDispose()
        {
            // Una acción lenta (hasta 15 s con un sensor real) que termina después de que la tarjeta dejó de existir.
            var hrm = new ScriptedHeartRateService { ConnectBehavior = ScriptedConnect.Pending };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            WithMainThread(context =>
            {
                var pending = store.ConnectAsync(Polar.DeviceId, "Polar");
                Assert.That(pending.IsCompleted, Is.False);
                var recorder = new StateRecorder<HeartRateState>(store.State);
                store.Dispose();
                hrm.FailPending(new InvalidOperationException("tarde"));
                context.RunUntilIdle();
                Assert.That(pending.IsCompleted, Is.True);
                Assert.That(pending.IsFaulted, Is.False, "no hay error");
                Assert.That(recorder.Count, Is.EqualTo(0), "no se intenta actualizarla");
                hrm.SetStatus(HrmStatus.Connected);
                hrm.EmitBpm(100);
                Assert.That(recorder.Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void ForgetNotifiesAtTheEnd()
        {
            var hrm = new ScriptedHeartRateService();
            hrm.Remember(ScriptedHeartRateService.RememberedSample, retrying: true);
            var store = new HeartRateStore(hrm, new RecordingLogger());
            Assert.That(store.State.Value.HasRemembered, Is.True);
            Assert.That(store.State.Value.IsRetrying, Is.True);
            var recorder = new StateRecorder<HeartRateState>(store.State);
            store.ForgetAsync();
            Assert.That(recorder.Count, Is.EqualTo(2), "el aviso del servicio y el reaviso del final");
            Assert.That(store.State.Value.HasRemembered, Is.False);
            Assert.That(store.State.Value.IsRetrying, Is.False);
            Assert.That(HrmCardRules.BodyFor(store.State.Value), Is.EqualTo(HrmCardBody.NoSensor));
            store.Dispose();
        }

        [Test]
        public void RetryClearsTheChosenNameAndCallsAutoConnect()
        {
            var hrm = new ScriptedHeartRateService { ConnectBehavior = ScriptedConnect.StayConnecting };
            var store = new HeartRateStore(hrm, new RecordingLogger());
            store.ConnectAsync(Polar.DeviceId, "Polar");
            store.RetryAsync();
            Assert.That(store.State.Value.ChosenName, Is.Null);
            Assert.That(hrm.AutoConnectCalls, Is.EqualTo(1));
            store.Dispose();
        }

        [Test]
        public void SnapshotsRetryingAndNamesOnEveryNotification()
        {
            var hrm = new ScriptedHeartRateService();
            var store = new HeartRateStore(hrm, new RecordingLogger());
            hrm.IsRetrying = true;
            hrm.RememberedDeviceName = "Polar";
            Assert.That(store.State.Value.IsRetrying, Is.False, "se fotografía en el aviso, no en vivo");
            hrm.EmitBpm(0);
            Assert.That(store.State.Value.IsRetrying, Is.True);
            Assert.That(store.State.Value.RememberedDeviceName, Is.EqualTo("Polar"));
            store.Dispose();
        }
    }
}
