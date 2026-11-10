using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WH · La háptica del entrenamiento (It. 14, AC-POL-03; DT14-23): Light al pausar y reanudar, Medium al cambiar de paso (no al
    // empezar) y Success al completar la rutina, una vez. Con el motor real del arnés (FakeScheduler).
    [TestFixture]
    public sealed class WorkoutHapticsTests
    {
        private static Routine TwoSteps()
        {
            return RoutineSeeds.Make("Dos pasos", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 10), RoutineSeeds.Step(StepType.Rest, dur: 5));
        }

        [Test, Category("AC-POL-03")]
        public void PauseResumeStepAndCompletion()
        {
            using (var h = new WorkoutHarness())
            {
                var haptics = new FakeHaptics();
                using (new WorkoutHaptics(h.Engine, haptics))
                {
                    h.Routine(TwoSteps());
                    h.At(3);
                    Assert.That(haptics.Kinds, Is.Empty, "al empezar, nada");
                    h.At(10.5);
                    Assert.That(haptics.Kinds, Is.EqualTo(new[] { HapticKind.Medium }), "cambio de paso");
                    h.Engine.Pause();
                    h.At(11.5);
                    h.Engine.Resume();
                    Assert.That(haptics.Kinds, Is.EqualTo(new[] { HapticKind.Medium, HapticKind.Light, HapticKind.Light }));
                    h.At(40);
                    Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                    Assert.That(h.State.EndedBy, Is.EqualTo(WorkoutEndKind.Completed));
                    Assert.That(haptics.Kinds, Is.EqualTo(new[] { HapticKind.Medium, HapticKind.Light, HapticKind.Light, HapticKind.Success }),
                        "completada: Success, una vez (el último paso que cierra no suma un Medium)");
                    h.At(45);
                    Assert.That(haptics.Kinds.Count, Is.EqualTo(4));
                }
            }
        }

        [Test, Category("AC-POL-03")]
        public void FreeSessionOnlyPauseAndResume()
        {
            using (var h = new WorkoutHarness())
            {
                var haptics = new FakeHaptics();
                var subject = new WorkoutHaptics(h.Engine, haptics);
                h.Free();
                h.At(5);
                h.Engine.Pause();
                h.Engine.Resume();
                Assert.That(haptics.Kinds, Is.EqualTo(new[] { HapticKind.Light, HapticKind.Light }));
                subject.Dispose();
                h.Engine.Pause();
                Assert.That(haptics.Kinds.Count, Is.EqualTo(2), "dispuesta, ya no escucha");
            }
        }
    }
}
