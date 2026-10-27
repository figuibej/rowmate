using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // HeartRateStore = lib/features/device/heart_rate_provider.dart (F-HRM-12). LT-32 a LT-34 son los de
    // test/heart_rate_provider_test.dart, 1 a 1, con su nombre original. HeartRateProvider → HeartRateStore,
    // addListener → suscripción al estado, _ThrowingHrm → ThrowingHeartRateService.
    // Entrega sincrónica (D-04): el legacy afirma connectingName justo después de connect(), antes del microtask que trae
    // "conectado"; acá ese estado intermedio se ve en la secuencia grabada (el PRIMER estado después de ConnectAsync).
    [TestFixture]
    public sealed class HeartRateProviderTests
    {
        // legacy: test/heart_rate_provider_test.dart:54 'refleja escaneo, conexión, bpm y desconexión del servicio'
        [TestCase(TestName = "refleja escaneo, conexión, bpm y desconexión del servicio",
            Category = "LT-32,AC-HRM-12-01,AC-HRM-12-02,AC-HRM-15-08,AC-HRM-15-11")]
        public void ReflectsScanConnectionBpmAndDisconnection()
        {
            var scheduler = new FakeScheduler();
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            var hrm = new SimulatedHeartRateService(sim, scheduler, NullAppLogger.Instance);
            var p = new HeartRateStore(hrm, NullAppLogger.Instance);
            var notifications = 0;
            p.State.Subscribe(_ => notifications++, false);

            Assert.That(p.State.Value.Status, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(p.State.Value.HasRemembered, Is.False);
            Assert.That(p.State.Value.ScanResults, Is.Empty);

            p.StartScanAsync();
            Assert.That(p.State.Value.IsScanning, Is.True);
            Assert.That(p.State.Value.ScanResults.Count, Is.EqualTo(1));

            var r = p.State.Value.ScanResults[0];
            var recorder = new StateRecorder<HeartRateState>(p.State);
            p.ConnectAsync(r.DeviceId, r.AdvName);
            Assert.That(recorder.First.ConnectingName, Is.EqualTo(SimulatedHeartRateService.DeviceName),
                "mientras conecta se muestra el nombre elegido");
            recorder.Dispose();
            Assert.That(p.State.Value.IsConnected, Is.True);
            Assert.That(p.State.Value.ConnectedDeviceName, Is.EqualTo(SimulatedHeartRateService.DeviceName));
            Assert.That(p.State.Value.ConnectingName, Is.EqualTo(""), "al conectar se descarta el nombre elegido");

            sim.Tick();
            scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(p.State.Value.Bpm, Is.EqualTo(sim.HeartRate));
            Assert.That(p.State.Value.Bpm, Is.GreaterThan(0));

            // Caída simulada: la tarjeta ve "recordado · reconectando…" y vuelve sola
            hrm.SimulateDisconnect();
            Assert.That(p.State.Value.IsConnected, Is.False);
            Assert.That(p.State.Value.IsRetrying, Is.True);
            Assert.That(p.State.Value.HasRemembered, Is.True);
            scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(p.State.Value.IsConnected, Is.True);
            Assert.That(p.State.Value.IsRetrying, Is.False);

            p.DisconnectAsync();
            Assert.That(p.State.Value.IsConnected, Is.False);
            Assert.That(p.State.Value.Bpm, Is.EqualTo(0));
            Assert.That(notifications, Is.GreaterThan(3));

            p.Dispose();
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0), "el flujo no deja timers vivos");
            hrm.Dispose();
        }

        // legacy: test/heart_rate_provider_test.dart:107 'una excepción del servicio queda en error y se limpia al reintentar'
        [TestCase(TestName = "una excepción del servicio queda en error y se limpia al reintentar", Category = "LT-33,AC-HRM-12-03")]
        public void ServiceExceptionIsKeptAndClearedOnRetry()
        {
            var hrm = new ThrowingHeartRateService();
            var p = new HeartRateStore(hrm, NullAppLogger.Instance);
            var connect = p.ConnectAsync("AA:BB", "X");
            Assert.That(connect.IsCompleted, Is.True);
            Assert.That(connect.IsFaulted, Is.False, "la excepción no sube: queda en el estado");
            Assert.That(p.State.Value.Error, Is.InstanceOf<HrmIncompatibleException>());
            Assert.That(HeartRateStore.IsIncompatible(p.State.Value.Error!), Is.True);

            p.RetryAsync();
            Assert.That(p.State.Value.Error, Is.Null);

            var disconnect = p.DisconnectAsync(); // no lanza aunque el servicio falle
            Assert.That(disconnect.IsFaulted, Is.False);
            Assert.That(p.State.Value.Error, Is.InstanceOf<InvalidOperationException>());
            p.Dispose();
            hrm.Dispose();
        }

        // legacy: test/heart_rate_provider_test.dart:122 'isIncompatible distingue el sensor incompatible'
        [TestCase(TestName = "isIncompatible distingue el sensor incompatible", Category = "LT-34,AC-HRM-12-04")]
        public void IsIncompatibleTellsTheIncompatibleSensorApart()
        {
            var incompatible = new HrmIncompatibleException();
            Assert.That(HeartRateStore.IsIncompatible(incompatible), Is.True);
            Assert.That(HeartRateStore.IsIncompatible(new InvalidOperationException("x")), Is.False);
        }

        [Test, Category("AC-HRM-12-01")]
        public void DropIsSeenWithRetryingAndRememberedInTheSameNotification()
        {
            // El 0 del pulso llega antes que "desconectado" y los dos ya traen IsRetrying y el recordado.
            var scheduler = new FakeScheduler();
            var hrm = new SimulatedHeartRateService(new RowingSimulator(new SequenceRandom(), noise: false), scheduler, NullAppLogger.Instance);
            var p = new HeartRateStore(hrm, NullAppLogger.Instance);
            p.ConnectAsync(SimulatedHeartRateService.DeviceId, SimulatedHeartRateService.DeviceName);
            scheduler.Advance(TimeSpan.FromSeconds(1));
            var recorder = new StateRecorder<HeartRateState>(p.State);
            hrm.SimulateDisconnect();
            Assert.That(recorder.Count, Is.EqualTo(2));
            Assert.That(recorder.Values[0].Bpm, Is.EqualTo(0));
            Assert.That(recorder.Values[0].Status, Is.EqualTo(HrmStatus.Connected));
            Assert.That(recorder.Values[1].Status, Is.EqualTo(HrmStatus.Disconnected));
            foreach (var state in recorder.Values)
            {
                Assert.That(state.IsRetrying, Is.True);
                Assert.That(state.RememberedDeviceName, Is.EqualTo(SimulatedHeartRateService.DeviceName));
            }

            Assert.That(HrmCardRules.BodyFor(recorder.Last), Is.EqualTo(HrmCardBody.Remembered));
            p.Dispose();
            hrm.Dispose();
        }
    }
}
