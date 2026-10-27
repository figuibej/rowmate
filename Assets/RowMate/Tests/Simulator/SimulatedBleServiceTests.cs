using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // SimulatedRowerService = lib/core/bluetooth/simulated_ble_service.dart (F-BLE-15). LT-20 a LT-22 son los de
    // test/simulated_ble_service_test.dart, 1 a 1, con su nombre original; fake_async → FakeScheduler. Entrega sincrónica
    // (D-04): donde el legacy hace flushMicrotasks, acá el aviso ya llegó.
    [TestFixture]
    public sealed class SimulatedBleServiceTests
    {
        private FakeScheduler _scheduler = null!;
        private RecordingLogger _logger = null!;

        [SetUp]
        public void SetUp()
        {
            _scheduler = new FakeScheduler();
            _logger = new RecordingLogger();
        }

        // = build() del legacy: SimulatedBleService(simulator: RowingSimulator(noise: false)).
        private SimulatedRowerService Build()
        {
            return new SimulatedRowerService(new RowingSimulator(new SequenceRandom(), noise: false), _scheduler, _logger);
        }

        // legacy: test/simulated_ble_service_test.dart:13 'emite connected y adapter on al primer listener, y datos cada segundo'
        [TestCase(TestName = "emite connected y adapter on al primer listener, y datos cada segundo",
            Category = "LT-20,AC-BLE-15-01,AC-BLE-15-02,AC-BLE-15-03")]
        public void ConnectedAndAdapterOnForTheFirstListener()
        {
            var ble = Build();
            // Status se graba SIN el valor actual (como el statusStream del legacy, que no reemite al suscribirse) y
            // AdapterState CON el actual: es un observable constante, y grabarlo con el actual es la única forma de obtener
            // [On], como el adapterStateStream del legacy, que emite al primer oyente (observación de 03-qa-harmony §2).
            var statuses = new StateRecorder<BleStatus>(ble.Status, emitCurrent: false);
            var adapter = new StateRecorder<BleAdapterState>(ble.AdapterState, emitCurrent: true);
            var data = new StateRecorder<RowingData>(ble.Data);

            ble.Start(); // = el primer oyente (onListen, D-05)
            Assert.That(statuses.Values, Is.EqualTo(new[] { BleStatus.Connected }));
            Assert.That(adapter.Values, Is.EqualTo(new[] { BleAdapterState.On }));
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Connected));
            Assert.That(ble.ConnectedDeviceName, Is.EqualTo(SimulatedRowerService.DeviceName));

            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(data.Count, Is.EqualTo(3));
            foreach (var reading in data.Values)
            {
                Assert.That(reading.HeartRate, Is.EqualTo(0), "el pulso llega solo por el pulsómetro simulado");
            }

            Assert.That(data.Last.DistanceMeters, Is.GreaterThan(data.First.DistanceMeters));

            ble.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        // legacy: test/simulated_ble_service_test.dart:39 'simulateDisconnect emite disconnected y reconecta a los 3 s'
        [TestCase(TestName = "simulateDisconnect emite disconnected y reconecta a los 3 s", Category = "LT-21,AC-BLE-15-06")]
        public void SimulateDisconnectReconnectsAfterThreeSeconds()
        {
            var ble = Build();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            var data = new StateRecorder<RowingData>(ble.Data);
            ble.Start();

            ble.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(statuses.Values, Is.EqualTo(new[] { BleStatus.Connected, BleStatus.Disconnected }));
            Assert.That(data.Count, Is.EqualTo(0), "no hay datos mientras está desconectado");

            _scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(statuses.Values, Is.EqualTo(new[] { BleStatus.Connected, BleStatus.Disconnected, BleStatus.Connected }));

            ble.Dispose();
        }

        // legacy: test/simulated_ble_service_test.dart:64 'disconnect no reconecta solo; startScan reconecta'
        [TestCase(TestName = "disconnect no reconecta solo; startScan reconecta",
            Category = "LT-22,AC-BLE-07-03,AC-BLE-15-04,AC-BLE-15-05,AC-BLE-15-01")]
        public void DisconnectDoesNotReconnectStartScanDoes()
        {
            var ble = Build();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            ble.Start();

            ble.DisconnectAsync();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(ble.ConnectedDeviceName, Is.Null);

            ble.StartScanAsync();
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Connected));
            Assert.That(statuses.Last, Is.EqualTo(BleStatus.Connected));

            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-01")]
        public void NameOnlyWhileConnected()
        {
            var ble = Build();
            Assert.That(ble.ConnectedDeviceName, Is.Null, "sin conectar no tiene nombre");
            ble.Start();
            Assert.That(ble.ConnectedDeviceName, Is.EqualTo("Remo simulado"));
            ble.SimulateDisconnect();
            Assert.That(ble.ConnectedDeviceName, Is.Null, "durante la caída tampoco");
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(ble.ConnectedDeviceName, Is.EqualTo("Remo simulado"));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-02")]
        public void StartConnectsOnlyOnce()
        {
            var ble = Build();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected), "arranca desconectado hasta que alguien escucha");
            ble.Start();
            ble.DisconnectAsync();
            ble.Start(); // una sola vez en la vida del servicio (:18-19)
            ble.Start();
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(statuses.Values, Is.EqualTo(new[] { BleStatus.Connected, BleStatus.Disconnected }));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-02")]
        public void AdapterIsAlwaysOn()
        {
            var ble = Build();
            var adapter = new StateRecorder<BleAdapterState>(ble.AdapterState);
            Assert.That(ble.AdapterState.Value, Is.EqualTo(BleAdapterState.On));
            ble.Start();
            ble.SimulateDisconnect();
            ble.DisconnectAsync();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(ble.AdapterState.Value, Is.EqualTo(BleAdapterState.On), "con el simulador nunca se ve Bluetooth apagado");
            Assert.That(adapter.Count, Is.EqualTo(0), "no cambia nunca");
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-03")]
        public void OneReadingPerSecondFromThePhysicsWithPulseZero()
        {
            // Cada lectura es un paso de la física de la It. 1 con el pulso en 0: a 150 W y 24 spm sin ruido, 3,770 m por segundo.
            var reference = new RowingSimulator(new SequenceRandom(), noise: false);
            var ble = Build();
            var data = new StateRecorder<RowingData>(ble.Data);
            ble.Start();
            _scheduler.Advance(TimeSpan.FromMilliseconds(999));
            Assert.That(data.Count, Is.EqualTo(0), "la primera lectura llega al segundo");
            _scheduler.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(data.Count, Is.EqualTo(1));
            _scheduler.Advance(TimeSpan.FromSeconds(9));
            Assert.That(data.Count, Is.EqualTo(10));
            for (var i = 0; i < data.Count; i++)
            {
                var expected = reference.Tick().WithHeartRate(0);
                Assert.That(data.Values[i], Is.EqualTo(expected), "lectura " + (i + 1));
            }

            Assert.That(data.Last.DistanceMeters, Is.EqualTo(37), "10 lecturas = 37 m");
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-04")]
        public void ConnectReconnectsAtOnce()
        {
            var ble = Build();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            ble.ConnectAsync("cualquiera");
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Connected), "sin estado buscando ni lista: conectado en el acto");
            Assert.That(statuses.Values, Is.EqualTo(new[] { BleStatus.Connected }));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-04")]
        public void StopScanDoesNothing()
        {
            var ble = Build();
            ble.Start();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            var task = ble.StopScanAsync();
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Connected));
            Assert.That(statuses.Count, Is.EqualTo(0));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-07-02"), Category("AC-BLE-15-05")]
        public void DisconnectStopsReadingsAtOnce()
        {
            var ble = Build();
            var data = new StateRecorder<RowingData>(ble.Data);
            ble.Start();
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(data.Count, Is.EqualTo(2));
            var task = ble.DisconnectAsync();
            Assert.That(task.IsCompleted, Is.True, "sin confirmación ni espera");
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected), "en el acto");
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(data.Count, Is.EqualTo(2), "no llegan más lecturas");
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected), "no se reconecta solo");
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-07-04")]
        public void DisconnectBeatsAPendingReconnect()
        {
            var ble = Build();
            ble.Start();
            ble.SimulateDisconnect();
            Assert.That(ble.IsReconnectPending, Is.True);
            _scheduler.Advance(TimeSpan.FromSeconds(2));
            ble.DisconnectAsync();
            Assert.That(ble.IsReconnectPending, Is.False);
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected), "a los 3 s no se reconecta");
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-06")]
        public void SimulateDisconnectWhenNotConnectedDoesNothing()
        {
            var ble = Build();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            ble.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(5));
            Assert.That(statuses.Count, Is.EqualTo(0));
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            Assert.That(_logger.Lines, Is.Empty);
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-06")]
        public void SimulateDisconnectLogsLikeTheLegacy()
        {
            var ble = Build();
            ble.Start();
            ble.SimulateDisconnect();
            Assert.That(_logger.Lines, Is.EqualTo(new[] { "[SIM] Info: Desconexión simulada, reconectando en 3s" }));
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-07")]
        public void ConnectWhileConnectedDoesNotRepeat()
        {
            var ble = Build();
            ble.Start();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            var data = new StateRecorder<RowingData>(ble.Data);
            ble.ConnectAsync("x");
            ble.StartScanAsync();
            _scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(statuses.Count, Is.EqualTo(0), "no se repite el aviso de conectado");
            Assert.That(data.Count, Is.EqualTo(3), "un solo timer de lecturas");
            ble.Dispose();
        }

        [Test, Category("AC-BLE-15-08")]
        public void NoRawBytesNorDeviceList()
        {
            var ble = Build();
            var raw = new StateRecorder<byte[]>(ble.RawBytes);
            var devices = new StateRecorder<IReadOnlyList<BleScanResult>>(ble.Devices);
            ble.Start();
            ble.StartScanAsync();
            ble.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(20));
            Assert.That(raw.Count, Is.EqualTo(0), "el diagnóstico mostraría NO DATA");
            Assert.That(devices.Count, Is.EqualTo(0));
            ble.Dispose();
        }

        [Test, Category("AC-APP-01-08")]
        public void DisposeCancelsEverythingAndLaterCallsDoNothing()
        {
            var ble = Build();
            var statuses = new StateRecorder<BleStatus>(ble.Status);
            var data = new StateRecorder<RowingData>(ble.Data);
            ble.Start();
            ble.SimulateDisconnect();
            ble.Dispose();
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0), "ni lecturas ni caída simulada");
            ble.Start();
            ble.StartScanAsync();
            ble.ConnectAsync("x");
            ble.SimulateDisconnect();
            _scheduler.Advance(TimeSpan.FromSeconds(10));
            Assert.That(statuses.Values, Is.EqualTo(new[] { BleStatus.Connected, BleStatus.Disconnected }));
            Assert.That(data.Count, Is.EqualTo(0));
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
        }

        [Test]
        public void ListenerThatDisconnectsOnConnectedLeavesNoReadingTimer()
        {
            // Con la entrega sincrónica (D-04), un oyente puede reaccionar dentro del aviso: el timer ya está agendado y Stop lo corta.
            var ble = Build();
            ble.Status.Subscribe(s =>
            {
                if (s == BleStatus.Connected)
                {
                    ble.DisconnectAsync();
                }
            }, false);
            ble.Start();
            Assert.That(ble.Status.Value, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(_scheduler.PendingTimers, Is.EqualTo(0));
            ble.Dispose();
        }
    }
}
