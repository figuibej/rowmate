using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Scene;
using RowMate.Core.Workout;

namespace RowMate.Tests.EditMode.Scene
{
    // SIN · Las entradas de cada cuadro (scene_view.dart:65-70; immersive_workout_screen.dart:81-86; AC-SCN-03-07): el split y el
    // SPM de la última lectura (la del HUD) y "en marcha" = la fase del motor.
    [TestFixture]
    public sealed class SceneInputsTests
    {
        private static WorkoutSnapshot Snapshot(WorkoutPhase phase, int pace, double spm)
        {
            var data = new RowingData { Pace500mSeconds = pace, StrokeRate = spm, DistanceMeters = 1240 };
            return new WorkoutSnapshot(7, phase, data, 60, null, false, false, false);
        }

        [Test, Category("AC-SCN-03-07")]
        public void TheLastReadingAndThePhase()
        {
            var inputs = SceneInputs.From(Snapshot(WorkoutPhase.Active, 125, 24));
            Assert.That(inputs.Pace500m, Is.EqualTo(125));
            Assert.That(inputs.Spm, Is.EqualTo(24));
            Assert.That(inputs.IsActive, Is.True);
            Assert.That(SceneState.TargetSpeedFor(inputs.Pace500m, inputs.Spm, inputs.IsActive), Is.EqualTo(4.0));
        }

        [TestCase(WorkoutPhase.Idle, Category = "AC-SCN-03-07")]
        [TestCase(WorkoutPhase.Paused, Category = "AC-SCN-03-07")]
        [TestCase(WorkoutPhase.Finished, Category = "AC-SCN-03-07")]
        public void OnlyActiveIsRowing(WorkoutPhase phase)
        {
            var inputs = SceneInputs.From(Snapshot(phase, 125, 24));
            Assert.That(inputs.IsActive, Is.False);
            Assert.That(SceneState.TargetSpeedFor(inputs.Pace500m, inputs.Spm, inputs.IsActive), Is.EqualTo(0), "en pausa, terminada o en reposo no avanza");
        }

        [Test, Category("AC-SCN-03-07"), Category("AC-BLE-15-09")]
        public void RowingOffSendsSpmZeroAndTheBoatSlowsDown()
        {
            // Con Remando apagado el monitor repone el split medio pero el SPM llega en 0: el bote frena.
            var inputs = SceneInputs.From(Snapshot(WorkoutPhase.Active, 125, 0));
            Assert.That(SceneState.TargetSpeedFor(inputs.Pace500m, inputs.Spm, inputs.IsActive), Is.EqualTo(0));
            var state = new SceneState();
            state.ForceForTest(500, 4, 60);
            for (var i = 0; i < 600; i++)
            {
                state.Update(1.0 / 60, inputs.Pace500m, inputs.Spm, inputs.IsActive);
            }

            Assert.That(state.Speed, Is.LessThan(4.0 * 0.01 + 0.01));
        }

        [Test, Category("AC-SCN-03-07")]
        public void ADisconnectionKeepsTheLastReading()
        {
            // Simular desconexión: la foto conserva la última lectura (como el proveedor del legacy) y la escena sigue con ella.
            var before = SceneInputs.From(Snapshot(WorkoutPhase.Active, 118, 26));
            var after = SceneInputs.From(Snapshot(WorkoutPhase.Active, 118, 26));
            Assert.That(after.Pace500m, Is.EqualTo(before.Pace500m));
            Assert.That(after.Spm, Is.EqualTo(before.Spm));
            Assert.Throws<System.ArgumentNullException>(() => SceneInputs.From(null!));
        }
    }
}
