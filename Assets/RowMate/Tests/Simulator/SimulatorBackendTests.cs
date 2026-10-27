using System;
using NUnit.Framework;
using RowMate.App.Composition;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Simulator;
using RowMate.Tests.Support;

namespace RowMate.Tests.Simulator
{
    // SimulatorBackend = main.dart:51-54 en modo simulador: el remo y el pulsómetro simulados sobre UN SOLO RowingSimulator.
    [TestFixture]
    public sealed class SimulatorBackendTests
    {
        [Test, Category("AC-APP-01-02"), Category("AC-BLE-13-03")]
        public void SimulatorReplacesBothServicesWithOneAthlete()
        {
            var scheduler = new FakeScheduler();
            var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: false);
            DeviceBackend asBase = backend;
            Assert.That(asBase.Rower, Is.InstanceOf<SimulatedRowerService>(), "el remo es el simulado");
            Assert.That(asBase.HeartRate, Is.InstanceOf<SimulatedHeartRateService>(), "y el pulsómetro también");
            Assert.That(backend.SimRower, Is.SameAs(asBase.Rower));
            Assert.That(backend.SimHeartRate, Is.SameAs(asBase.HeartRate));
            Assert.That(backend.SimRower.Simulator, Is.SameAs(backend.Simulator), "un solo atleta");
            Assert.That(backend.SimHeartRate.Simulator, Is.SameAs(backend.Simulator));
            Assert.That(backend.Simulator.Noise, Is.False);
            backend.Dispose();
        }

        [Test, Category("AC-APP-01-02")]
        public void HeartRateComesFromRowerPhysics()
        {
            // El pulso del sensor simulado sale de la física del remo: sube al remar y baja hacia 70 al dejar.
            var scheduler = new FakeScheduler();
            var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: false);
            var bpm = new StateRecorder<int>(backend.HeartRate.Bpm);
            backend.Rower.Start();
            backend.HeartRate.ConnectAsync(SimulatedHeartRateService.DeviceId);
            scheduler.Advance(TimeSpan.FromSeconds(30));
            Assert.That(bpm.Last, Is.EqualTo(backend.Simulator.HeartRate));
            Assert.That(bpm.Last, Is.GreaterThan(120), "remando a 150 W el pulso sube");
            backend.Simulator.Rowing = false;
            scheduler.Advance(TimeSpan.FromSeconds(60));
            Assert.That(bpm.Last, Is.LessThan(75), "sin remar baja hacia 70");
            backend.Dispose();
        }

        [Test, Category("AC-HRM-15-09")]
        public void HeartRateIs142At60s()
        {
            // Sin ruido, a 150 W y 24 spm desde el arranque: 142,5 − 72,5·0,9⁶⁰ = 142,37 → 142 (y 142 de 58 a 61 s: el orden
            // entre el timer del remo y el del pulsómetro no lo cambia, 03-qa-harmony §6 #19).
            foreach (var hrmFirst in new[] { false, true })
            {
                var scheduler = new FakeScheduler();
                var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: false);
                var bpm = new StateRecorder<int>(backend.HeartRate.Bpm);
                if (hrmFirst)
                {
                    backend.HeartRate.ConnectAsync(SimulatedHeartRateService.DeviceId);
                    backend.Rower.Start();
                }
                else
                {
                    backend.Rower.Start();
                    backend.HeartRate.ConnectAsync(SimulatedHeartRateService.DeviceId);
                }

                scheduler.Advance(TimeSpan.FromSeconds(58));
                for (var second = 58; second <= 61; second++)
                {
                    Assert.That(bpm.Last, Is.EqualTo(142), "a los " + second + " s (pulsómetro primero: " + hrmFirst + ")");
                    scheduler.Advance(TimeSpan.FromSeconds(1));
                }

                backend.Dispose();
            }
        }

        [Test, Category("AC-APP-01-08")]
        public void DisposeLeavesNoTimers()
        {
            var scheduler = new FakeScheduler();
            var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: false);
            backend.Rower.Start();
            backend.HeartRate.StartScanAsync();
            backend.HeartRate.ConnectAsync(SimulatedHeartRateService.DeviceId);
            backend.SimRower.SimulateDisconnect();
            backend.SimHeartRate.SimulateDisconnect();
            Assert.That(scheduler.PendingTimers, Is.GreaterThan(0));
            backend.Dispose();
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0), "ni lecturas, ni caída simulada, ni búsqueda");
            Assert.That(backend.IsDisposed, Is.True);
            Assert.DoesNotThrow(() => backend.Dispose(), "disponer dos veces no falla");
        }

        [Test, Category("AC-APP-01-02")]
        public void NoiseUsesTheInjectedRandom()
        {
            // noise = true: la app; el azar es el inyectado (AC-BASE-07): dos números por lectura (vatios y ritmo).
            var scheduler = new FakeScheduler();
            var random = new SequenceRandom(0.5, 0.5, 0.5, 0.5);
            var backend = SimulatorBackend.Create(scheduler, NullAppLogger.Instance, noise: true, random: random);
            Assert.That(backend.Simulator.Noise, Is.True);
            backend.Rower.Start();
            scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(random.Calls, Is.EqualTo(4));
            backend.Dispose();
        }

        [Test]
        public void BackendRejectsServicesOfAnotherAthlete()
        {
            var scheduler = new FakeScheduler();
            var a = new RowingSimulator(new SequenceRandom(), noise: false);
            var b = new RowingSimulator(new SequenceRandom(), noise: false);
            var rower = new SimulatedRowerService(a, scheduler, NullAppLogger.Instance);
            var hrm = new SimulatedHeartRateService(b, scheduler, NullAppLogger.Instance);
            Assert.Throws<ArgumentException>(() => new SimulatorDeviceBackend(a, rower, hrm));
        }

        [Test, Category("AC-BLE-13-03")]
        public void StatusOfBothServicesStartsLikeTheLegacy()
        {
            var backend = SimulatorBackend.Create(new FakeScheduler(), NullAppLogger.Instance, noise: false);
            Assert.That(backend.Rower.Status.Value, Is.EqualTo(BleStatus.Disconnected), "hasta que la app empieza a escuchar");
            Assert.That(backend.Rower.AdapterState.Value, Is.EqualTo(BleAdapterState.On));
            Assert.That(backend.HeartRate.Status.Value, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(backend.HeartRate.CurrentBpm, Is.EqualTo(0));
            backend.Dispose();
        }
    }
}
