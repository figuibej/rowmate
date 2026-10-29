using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // LT-35 a LT-38: la fusión del pulso de WorkoutProvider (lib/features/workout/workout_provider.dart:68-70, :220-237), con
    // los dobles del legacy: el remo y el pulsómetro como broadcast sin estado inicial (emitCurrent: false) y el conteo de
    // avisos sobre WorkoutEngine.State, también con emitCurrent: false (si recibiera el valor actual, LT-35 contaría 3 y
    // LT-38, 1: 03-qa-harmony-paridad §5). El remo no corre su guion: cada lectura se manda a mano, como ble.data.add.
    [TestFixture]
    public sealed class WorkoutProviderHrTests
    {
        // legacy: test/workout_provider_hr_test.dart:80 'el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo'
        [TestCase(TestName = "el pulsómetro manda sobre el FTMS y se actualiza sin paquetes del remo", Category = "LT-35,AC-WKT-05-01")]
        public void SensorBeatsFtmsAndUpdatesWithoutRowerPackets()
        {
            using (var h = new WorkoutHarness(startRower: false))
            using (var notifications = new StateRecorder<WorkoutSnapshot>(h.Engine.State))
            {
                // Sin pulsómetro se respeta lo que diga el monitor.
                h.Rower.Emit(new RowingData { PowerWatts = 100, HeartRate = 90 });
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(90));

                // El bpm llega aunque el remo no mande nada.
                h.HeartRate.EmitBpm(142);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(142));
                Assert.That(h.State.Data.PowerWatts, Is.EqualTo(100));
                Assert.That(notifications.Count, Is.EqualTo(2));

                // El siguiente paquete del remo no pisa el pulso del sensor.
                h.Rower.Emit(new RowingData { PowerWatts = 120, HeartRate = 90 });
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(142));
                Assert.That(h.State.Data.PowerWatts, Is.EqualTo(120));

                // Un 0 del sensor (sin contacto) también manda.
                h.HeartRate.EmitBpm(0);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(0));

                // Al desconectarse el sensor vuelve a 0 y el FTMS repone su valor.
                h.HeartRate.EmitBpm(130);
                h.HeartRate.SetStatus(HrmStatus.Disconnected);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(0));
                h.Rower.Emit(new RowingData { PowerWatts = 120, HeartRate = 90 });
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(90));
            }
        }

        // legacy: test/workout_provider_hr_test.dart:125 'orden real de caída: bpm 0 y después disconnected'
        [TestCase(TestName = "orden real de caída: bpm 0 y después disconnected", Category = "LT-36,AC-WKT-05-02")]
        public void RealDropOrderZeroThenDisconnected()
        {
            using (var h = new WorkoutHarness(startRower: false))
            {
                h.Rower.Emit(new RowingData { HeartRate = 90 });
                h.HeartRate.EmitBpm(140);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(140));

                h.HeartRate.EmitBpm(0); // _cleanupConnection emite 0 antes de disconnected
                h.HeartRate.SetStatus(HrmStatus.Disconnected);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(0));

                h.Rower.Emit(new RowingData { HeartRate = 90 });
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(90), "el FTMS repone su valor");
            }
        }

        // legacy: test/workout_provider_hr_test.dart:148 'un bpm recibido durante connecting se conserva al pasar a connected'
        [TestCase(TestName = "un bpm recibido durante connecting se conserva al pasar a connected", Category = "LT-37,AC-WKT-05-03")]
        public void BpmDuringConnectingIsKeptWhenConnected()
        {
            using (var h = new WorkoutHarness(startRower: false))
            {
                h.HeartRate.SetStatus(HrmStatus.Connecting);
                h.HeartRate.EmitBpm(120);
                h.HeartRate.SetStatus(HrmStatus.Connected);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(120));

                h.Rower.Emit(new RowingData { HeartRate = 90 });
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(120), "el sensor sigue mandando");
            }
        }

        // legacy: test/workout_provider_hr_test.dart:167 'connected del sensor sin bpm previo no toca los datos'
        [TestCase(TestName = "connected del sensor sin bpm previo no toca los datos", Category = "LT-38,AC-WKT-05-04")]
        public void ConnectedWithoutPreviousBpmTouchesNothing()
        {
            using (var h = new WorkoutHarness(startRower: false))
            using (var notifications = new StateRecorder<WorkoutSnapshot>(h.Engine.State))
            {
                h.HeartRate.SetStatus(HrmStatus.Connected);
                h.HeartRate.SetStatus(HrmStatus.Disconnected);
                Assert.That(notifications.Count, Is.EqualTo(0));
            }
        }

        // AC-WKT-05-05: la fusión anda en todas las fases (también en reposo y en pausa); cualquier estado distinto de
        // conectado después de un bpm pone 0 y la lectura siguiente del remo repone el suyo; el HUD y los puntos usan el pulso
        // fusionado.
        [Test, Category("AC-WKT-05-05")]
        public void FusionInEveryPhaseFeedsHudAndPoints()
        {
            using (var h = new WorkoutHarness(k => RowerScripts.R4(k).CopyWith(heartRate: 90)))
            {
                h.At(-0.2);
                h.HeartRate.EmitBpm(150);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(150), "en reposo");
                h.Free();
                h.At(5);
                Assert.That(h.Points()[0].HeartRate, Is.EqualTo(150), "el punto lleva el pulso fusionado");
                h.Engine.Pause();
                h.HeartRate.EmitBpm(160);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(160), "en pausa");
                foreach (var status in new[] { HrmStatus.Scanning, HrmStatus.Connecting, HrmStatus.Disconnected })
                {
                    h.HeartRate.EmitBpm(140);
                    h.HeartRate.SetStatus(status);
                    Assert.That(h.State.Data.HeartRate, Is.EqualTo(0), status.ToString());
                    h.Rower.Emit(RowerScripts.R4(9).CopyWith(heartRate: 90));
                    Assert.That(h.State.Data.HeartRate, Is.EqualTo(90), "el remo repone el suyo después de " + status);
                }

                h.Engine.Resume();
                h.HeartRate.EmitBpm(171);
                h.At(10.5);
                Assert.That(h.Points()[1].HeartRate, Is.EqualTo(171), "el segundo punto también");
            }
        }
    }
}
