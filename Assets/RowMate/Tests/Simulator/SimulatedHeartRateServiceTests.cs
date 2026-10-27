using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // SimulatedHeartRateService = lib/core/bluetooth/simulated_heart_rate_service.dart (F-HRM-15). LT-23 a LT-25 son los de
    // test/simulated_heart_rate_service_test.dart, 1 a 1, con su nombre original; fake_async → FakeScheduler.
    [TestFixture]
    public sealed class SimulatedHeartRateServiceTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
        }

        private SimulatedHeartRateService Build(RowingSimulator? sim = null)
        {
            return new SimulatedHeartRateService(sim ?? new RowingSimulator(new SequenceRandom(), noise: false), _scheduler, _logger);
        }

        // legacy: test/simulated_heart_rate_service_test.dart:10 'startScan emite el sensor sintético y connect emite bpm cada segundo'
        [TestCase(TestName = "startScan emite el sensor sintético y connect emite bpm cada segundo",
            Category = "LT-23,AC-HRM-04-01,AC-HRM-15-02,AC-HRM-15-04,AC-HRM-15-05,AC-HRM-15-11")]
        public void ScanEmitsTheSyntheticSensorAndConnectEmitsBpm()
        {
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            var hrm = Build(sim);
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            var devices = new StateRecorder<IReadOnlyList<BleScanResult>>(hrm.Devices);
            var bpm = new StateRecorder<int>(hrm.Bpm);

            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(hrm.RememberedDeviceName, Is.Null);

            hrm.StartScanAsync();
            Assert.That(statuses.Values, Is.EqualTo(new[] { HrmStatus.Scanning }));
            Assert.That(devices.Count, Is.EqualTo(1));
            Assert.That(devices.Last.Count, Is.EqualTo(1), "un único sensor");
            var result = devices.Last[0];
            Assert.That(result.AdvName, Is.EqualTo(SimulatedHeartRateService.DeviceName));
            Assert.That(result.DeviceId, Is.EqualTo(SimulatedHeartRateService.DeviceId));

            hrm.ConnectAsync(result.DeviceId);
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Connected));
            Assert.That(hrm.ConnectedDeviceName, Is.EqualTo(SimulatedHeartRateService.DeviceName));

            sim.Tick();
            sim.Tick();
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(bpm.Count, Is.EqualTo(3));
            Assert.That(bpm.Last, Is.EqualTo(sim.HeartRate));
            Assert.That(bpm.Last, Is.GreaterThan(0));
            Assert.That(hrm.CurrentBpm, Is.EqualTo(bpm.Last));

            hrm.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // legacy: test/simulated_heart_rate_service_test.dart:49 'el escaneo expira a disconnected si no se conecta'
        [TestCase(TestName = "el escaneo expira a disconnected si no se conecta", Category = "LT-24,AC-HRM-15-02")]
        public void ScanExpiresToDisconnected()
        {
            var hrm = Build();
            hrm.StartScanAsync();
            _scheduler.Advance(TimeSpan.FromSeconds(9));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Scanning));
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            hrm.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // legacy: test/simulated_heart_rate_service_test.dart:62 'simulateDisconnect cae y reconecta a los 3 s; disconnect no reconecta'
        [TestCase(TestName = "simulateDisconnect cae y reconecta a los 3 s; disconnect no reconecta",
            Category = "LT-25,AC-HRM-04-04,AC-HRM-15-06,AC-HRM-15-08,AC-HRM-15-11")]
        public void SimulateDisconnectDropsAndReturnsDisconnectDoesNot()
        {
            var hrm = Build();
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            var bpm = new StateRecorder<int>(hrm.Bpm);
            hrm.ConnectAsync(SimulatedHeartRateService.DeviceId);
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(bpm.Count, Is.EqualTo(1));

            hrm.SimulateDisconnect();
            Assert.That(hrm.IsRetrying, Is.True);
            Assert.That(hrm.RememberedDeviceName, Is.EqualTo(SimulatedHeartRateService.DeviceName),
                "mientras reconecta la tarjeta muestra \"reconectando…\"");
            Assert.That(bpm.Last, Is.EqualTo(0), "al caer se emite 0");
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(statuses.Values, Is.EqualTo(new[] { HrmStatus.Connected, HrmStatus.Disconnected }));
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(statuses.Last, Is.EqualTo(HrmStatus.Connected));
            Assert.That(hrm.IsRetrying, Is.False);
            Assert.That(hrm.RememberedDeviceName, Is.Null);

            hrm.DisconnectAsync();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(hrm.ConnectedDeviceName, Is.Null);

            hrm.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        [Test, Category("AC-HRM-04-01")]
        public void EveryReadingIsPublishedEvenIfRepeated()
        {
            // Sin remar, el pulso del simulador no cambia entre lecturas: igual se publica cada segundo, sin suavizar.
            var sim = new RowingSimulator(new SequenceRandom(), noise: false);
            var hrm = Build(sim);
            var bpm = new StateRecorder<int>(hrm.Bpm);
            hrm.ConnectAsync(SimulatedHeartRateService.DeviceId);
            _scheduler.Advance(TimeSpan.FromSeconds(4));
            Assert.That(bpm.Values, Is.EqualTo(new[] { 70, 70, 70, 70 }), "el remo no avanza: el pulso queda en 70 y se repite");
            sim.Tick();
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(bpm.Last, Is.EqualTo(sim.HeartRate), "el valor del simulador, tal cual");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-04-04")]
        public void ZeroIsPublishedOnlyOnce()
        {
            var hrm = Build();
            var bpm = new StateRecorder<int>(hrm.Bpm);
            hrm.ConnectAsync("x");
            hrm.DisconnectAsync();
            Assert.That(bpm.Count, Is.EqualTo(0), "si el último ya era 0, no se publica otro");

            hrm.ConnectAsync("x");
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            hrm.SimulateDisconnect();
            Assert.That(bpm.Values, Is.EqualTo(new[] { 70, 0 }));
            hrm.DisconnectAsync();
            Assert.That(bpm.Values, Is.EqualTo(new[] { 70, 0 }), "ya era 0");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-04-04"), Category("AC-HRM-15-08")]
        public void DropPublishesZeroBeforeDisconnected()
        {
            var hrm = Build();
            hrm.ConnectAsync("x");
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            var order = new List<string>();
            hrm.Bpm.Subscribe(b => order.Add("bpm " + b + " retrying=" + hrm.IsRetrying));
            hrm.Status.Subscribe(s => order.Add(s + " retrying=" + hrm.IsRetrying), false);
            hrm.SimulateDisconnect();
            Assert.That(order, Is.EqualTo(new[] { "bpm 0 retrying=True", "Disconnected retrying=True" }),
                "primero 0 y después desconectado, ya con la vuelta agendada");
            hrm.Dispose();
        }

        [Test, Category("AC-APP-01-04")]
        public void AutoConnectDoesNothing()
        {
            var hrm = Build();
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            var task = hrm.AutoConnectAsync();
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(task.IsFaulted, Is.False);
            _scheduler.Advance(TimeSpan.FromSeconds(60));
            Assert.That(statuses.Count, Is.EqualTo(0));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected), "la tarjeta abre en Sin sensor");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-01")]
        public void NoMemoryAndAutoConnectDoesNothing()
        {
            var hrm = Build();
            hrm.AutoConnectAsync();
            Assert.That(hrm.RememberedDeviceName, Is.Null);
            Assert.That(hrm.IsRetrying, Is.False);
            hrm.ConnectAsync("x");
            hrm.DisconnectAsync();
            Assert.That(hrm.RememberedDeviceName, Is.Null, "no recuerda el sensor después de usarlo");
            hrm.Dispose();

            var another = Build();
            Assert.That(another.RememberedDeviceName, Is.Null, "una instancia nueva tampoco");
            Assert.That(another.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            another.Dispose();
        }

        [Test, Category("AC-HRM-15-02")]
        public void SingleResultDetails()
        {
            var result = SimulatedHeartRateService.FakeScanResult();
            Assert.That(result.DeviceId, Is.EqualTo("SIM:HRM"));
            Assert.That(result.PlatformName, Is.Null);
            Assert.That(result.AdvName, Is.EqualTo("Pulsómetro simulado"));
            Assert.That(result.Rssi, Is.EqualTo(-50));
            Assert.That(result.ServiceUuids.Count, Is.EqualTo(1));
            Assert.That(result.ServiceUuids[0], Is.EqualTo(BleUuid.FromShort(0x180D)));
        }

        [Test, Category("AC-HRM-15-02")]
        public void ScanCancelsAPendingReturnAndRestartsTheTimeout()
        {
            var hrm = Build();
            hrm.ConnectAsync("x");
            hrm.SimulateDisconnect();
            Assert.That(hrm.IsRetrying, Is.True);
            hrm.StartScanAsync();
            Assert.That(hrm.IsRetrying, Is.False, "la búsqueda cancela la vuelta simulada");
            _scheduler.Advance(TimeSpan.FromSeconds(6));
            hrm.StartScanAsync(); // reinicia el plazo
            _scheduler.Advance(TimeSpan.FromSeconds(9));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Scanning));
            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            hrm.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        [Test, Category("AC-HRM-15-02")]
        public void RepeatedScanNotifiesScanningAgain()
        {
            var hrm = Build();
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            hrm.StartScanAsync();
            hrm.StartScanAsync();
            Assert.That(statuses.Values, Is.EqualTo(new[] { HrmStatus.Scanning, HrmStatus.Scanning }), "el legacy emite siempre");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-03")]
        public void StopScan()
        {
            var hrm = Build();
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            hrm.StopScanAsync();
            Assert.That(statuses.Count, Is.EqualTo(0), "sin buscar, nada");
            hrm.StartScanAsync();
            hrm.StopScanAsync();
            Assert.That(statuses.Values, Is.EqualTo(new[] { HrmStatus.Scanning, HrmStatus.Disconnected }));
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0), "el plazo de la búsqueda se cancela");
            hrm.ConnectAsync("x");
            hrm.StopScanAsync();
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Connected), "conectado, detener la búsqueda no hace nada");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-04")]
        public void DirectToConnectedFirstReadingAfterOneSecond()
        {
            var hrm = Build();
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            var bpm = new StateRecorder<int>(hrm.Bpm);
            hrm.StartScanAsync();
            hrm.ConnectAsync("cualquier-id");
            Assert.That(statuses.Values, Is.EqualTo(new[] { HrmStatus.Scanning, HrmStatus.Connected }), "sin pasar por conectando");
            _scheduler.Advance(TimeSpan.FromMilliseconds(999));
            Assert.That(bpm.Count, Is.EqualTo(0), "hasta el segundo la tarjeta muestra --");
            _scheduler.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(bpm.Count, Is.EqualTo(1));
            _scheduler.Advance(TimeSpan.FromSeconds(20));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Connected), "el plazo de la búsqueda no lo desconecta");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-04")]
        public void ConnectWhileConnectedDoesNothing()
        {
            var hrm = Build();
            hrm.ConnectAsync("x");
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            var bpm = new StateRecorder<int>(hrm.Bpm);
            hrm.ConnectAsync("y");
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(statuses.Count, Is.EqualTo(0));
            Assert.That(bpm.Count, Is.EqualTo(3), "un solo timer de lecturas");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-05")]
        public void NameOnlyWhileConnected()
        {
            var hrm = Build();
            Assert.That(hrm.ConnectedDeviceName, Is.Null);
            hrm.StartScanAsync();
            Assert.That(hrm.ConnectedDeviceName, Is.Null, "buscando no tiene nombre conectado");
            hrm.ConnectAsync("x");
            Assert.That(hrm.ConnectedDeviceName, Is.EqualTo("Pulsómetro simulado"));
            hrm.SimulateDisconnect();
            Assert.That(hrm.ConnectedDeviceName, Is.Null);
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-06")]
        public void DisconnectCancelsTheReturn()
        {
            var hrm = Build();
            hrm.ConnectAsync("x");
            hrm.SimulateDisconnect();
            hrm.DisconnectAsync();
            Assert.That(hrm.IsRetrying, Is.False);
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected), "10 s después sigue desconectado");
            Assert.That(hrm.ConnectedDeviceName, Is.Null);
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-07")]
        public void ForgetCancelsTheReturnAndKeepsTheConnection()
        {
            var hrm = Build();
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            hrm.ConnectAsync("x");
            hrm.ForgetAsync();
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Connected), "una conexión activa no se corta");
            Assert.That(statuses.Values, Is.EqualTo(new[] { HrmStatus.Connected, HrmStatus.Connected }), "se vuelve a avisar el estado");

            hrm.SimulateDisconnect();
            Assert.That(hrm.IsRetrying, Is.True);
            hrm.ForgetAsync();
            Assert.That(hrm.IsRetrying, Is.False);
            Assert.That(hrm.RememberedDeviceName, Is.Null, "la tarjeta queda en Sin sensor");
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Disconnected), "a los 3 s no se reconecta");
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-08")]
        public void SimulateDisconnectWhenNotConnectedDoesNothing()
        {
            var hrm = Build();
            hrm.SimulateDisconnect();
            Assert.That(hrm.IsRetrying, Is.False);
            hrm.StartScanAsync();
            hrm.SimulateDisconnect();
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Scanning));
            Assert.That(hrm.IsRetrying, Is.False);
            Assert.That(_logger.Lines, Is.Empty);
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-08")]
        public void SimulateDisconnectLogsLikeTheLegacy()
        {
            var hrm = Build();
            hrm.ConnectAsync("x");
            hrm.SimulateDisconnect();
            Assert.That(_logger.Lines, Is.EqualTo(new[] { "[SIM] Info: Pulsómetro desconectado, reconectando en 3s" }));
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-12")]
        public void SearchOtherCancelsTheReturn()
        {
            // "Buscar otro" durante "Pulsómetro simulado · reconectando..." = StartScanAsync.
            var hrm = Build();
            var devices = new StateRecorder<IReadOnlyList<BleScanResult>>(hrm.Devices);
            hrm.ConnectAsync("x");
            hrm.SimulateDisconnect();
            hrm.StartScanAsync();
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Scanning));
            Assert.That(devices.Last[0].DeviceId, Is.EqualTo("SIM:HRM"), "con la fila del sensor simulado");
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(hrm.Status.Value, Is.EqualTo(HrmStatus.Scanning), "a los 3 s no se reconecta solo");
            hrm.Dispose();
        }

        [Test, Category("AC-HRM-15-11")]
        public void DisposeLeavesNoTimersAndLaterCallsDoNothing()
        {
            var hrm = Build();
            hrm.StartScanAsync();
            hrm.ConnectAsync("x");
            hrm.SimulateDisconnect();
            hrm.StartScanAsync();
            hrm.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            var statuses = new StateRecorder<HrmStatus>(hrm.Status);
            hrm.StartScanAsync();
            hrm.ConnectAsync("x");
            hrm.ForgetAsync();
            hrm.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(statuses.Count, Is.EqualTo(0));
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }
    }
}
