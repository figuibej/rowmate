using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SPG: el estado del paso en curso (F-WKT-04; AC-WKT-04-01 a -07; lib/features/workout/workout_provider.dart:16-42,
    // :98-112; immersive_workout_screen.dart:342-346).
    [TestFixture]
    public sealed class StepProgressTests
    {
        private static readonly IntervalStep Rest = Step(dur: 30);

        [Test, Category("AC-WKT-04-01")]
        public void ExistsOnlyWithRoutineOutsideIdle()
        {
            using (var h = new WorkoutHarness())
            {
                Assert.That(h.State.Step.HasValue, Is.False, "en reposo");
                h.Free();
                h.At(3);
                Assert.That(h.State.Step.HasValue, Is.False, "en libre");
                h.Engine.FinishAsync();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.State.Step.HasValue, Is.False, "de vuelta en reposo");
                Assert.That(h.Engine.StartWithRoutine(RoutineSeeds.Corta()), Is.EqualTo(StartOutcome.Started));
                h.At(6);
                var step = h.State.Step!.Value;
                Assert.That(step.StepIndex, Is.EqualTo(0));
                Assert.That(step.TotalSteps, Is.EqualTo(4));
                Assert.That(step.ElapsedInStep, Is.EqualTo(3));
                Assert.That(step.Step.TargetSpm, Is.EqualTo(18));
                Assert.That(step.RemainingInStep, Is.EqualTo(7));
                h.Engine.Pause();
                Assert.That(h.State.Step.HasValue, Is.True, "en pausa");
            }
        }

        [Test, Category("AC-WKT-04-02")]
        public void Remaining()
        {
            Assert.That(Progress(Step(dur: 60), 15).RemainingInStep, Is.EqualTo(45));
            Assert.That(Progress(Step(dur: 60), 0).RemainingInStep, Is.EqualTo(60));
            Assert.That(Progress(Step(dur: 60), 75).RemainingInStep, Is.EqualTo(0), "acotado en 0");
            Assert.That(Progress(Step(dist: 500), 15).RemainingInStep, Is.Null, "por distancia");
            Assert.That(Progress(Step(), 15).RemainingInStep, Is.Null, "sin meta");
            Assert.That(Progress(Step(dur: 60, dist: 500), 15).RemainingInStep, Is.EqualTo(45), "el tiempo manda");
        }

        [Test, Category("AC-WKT-04-02"), Category("AC-WKT-03-07")]
        public void NegativeDurationShowsZero()
        {
            // DV-37 (DT4a-12): el clamp(0, d) del legacy lanza con d < 0 (:103); acá el restante es 0 (la cuenta, 0:00).
            var negative = Progress(Step(dur: -90), 0, Rest);
            Assert.That(negative.RemainingInStep, Is.EqualTo(0));
            Assert.That(negative.Progress, Is.EqualTo(0));
            Assert.That(negative.IsEndingSoon, Is.False, "sin por terminar");
            Assert.That(negative.IsUpNext, Is.True, "con Próximo si hay un paso siguiente (AC-WKT-04-05)");
            Assert.That(Progress(Step(dur: 0), 0).RemainingInStep, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-04-03")]
        public void Progress()
        {
            Assert.That(Progress(Step(dur: 60), 15).Progress, Is.EqualTo(0.25));
            Assert.That(Progress(Step(dur: 60), 90).Progress, Is.EqualTo(1.0), "acotado");
            Assert.That(Progress(Step(dur: 0), 3).Progress, Is.EqualTo(0), "duración 0");
            Assert.That(Progress(Step(dist: 500), 30).Progress, Is.EqualTo(0), "Q-51: por distancia no avanza");
            Assert.That(Progress(Step(), 30).Progress, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-04-04")]
        public void EndingSoonOneToFive()
        {
            Assert.That(Progress(Step(dur: 60), 54).IsEndingSoon, Is.False, "6");
            for (var remaining = 5; remaining >= 1; remaining--)
            {
                Assert.That(Progress(Step(dur: 60), 60 - remaining).IsEndingSoon, Is.True, remaining.ToString());
            }

            Assert.That(Progress(Step(dur: 60), 60).IsEndingSoon, Is.False, "0");
            Assert.That(Progress(Step(dist: 500), 58).IsEndingSoon, Is.False, "sin restante");
        }

        [Test, Category("AC-WKT-04-05")]
        public void UpNextZeroToFifteen()
        {
            Assert.That(Progress(Step(dur: 60), 44, Rest).IsUpNext, Is.False, "16");
            Assert.That(Progress(Step(dur: 60), 45, Rest).IsUpNext, Is.True, "15");
            Assert.That(Progress(Step(dur: 60), 60, Rest).IsUpNext, Is.True, "0");
            Assert.That(Progress(Step(dur: 60), 50).IsUpNext, Is.False, "el último");
            Assert.That(Progress(Step(dist: 100), 50, Rest).IsUpNext, Is.False, "por distancia");
            Assert.That(Progress(Step(), 50, Rest).IsUpNext, Is.False, "sin meta");
        }

        [Test, Category("AC-WKT-04-06")]
        public void ExamplesOfTheSpec()
        {
            // Un paso de 60 s que no es el último: a continuación desde 0:15 (45 s) hasta el cambio y por terminar de 0:05 a
            // 0:01; en uno de 10 s, a continuación todo el paso; en el último, por terminar sí y a continuación no.
            for (var elapsed = 0; elapsed < 60; elapsed++)
            {
                var p = Progress(Step(dur: 60), elapsed, Rest);
                Assert.That(p.IsUpNext, Is.EqualTo(elapsed >= 45), "a continuación a los " + elapsed);
                Assert.That(p.IsEndingSoon, Is.EqualTo(elapsed >= 55), "por terminar a los " + elapsed);
            }

            for (var elapsed = 0; elapsed < 10; elapsed++)
            {
                Assert.That(Progress(Step(dur: 10), elapsed, Rest).IsUpNext, Is.True, "10 s, a los " + elapsed);
            }

            Assert.That(Progress(Step(dur: 30), 27).IsEndingSoon, Is.True);
            Assert.That(Progress(Step(dur: 30), 27).IsUpNext, Is.False);
        }

        [Test, Category("AC-WKT-04-07"), Category("AC-WKT-03-06")]
        public void FinishedRoutine()
        {
            // Corta termina en el segundo 30 con el último paso (descanso de 5 s) por tiempo: restante 0, progreso 1, ni por
            // terminar ni a continuación.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(29);
                var last = h.State.Step!.Value;
                Assert.That(last.StepIndex, Is.EqualTo(3));
                Assert.That(last.IsEndingSoon, Is.True);
                Assert.That(last.IsUpNext, Is.False);
            }

            var finished = Progress(Step(dur: 5), 5);
            Assert.That(finished.RemainingInStep, Is.EqualTo(0));
            Assert.That(finished.Progress, Is.EqualTo(1));
            Assert.That(finished.IsEndingSoon, Is.False);
            Assert.That(finished.IsUpNext, Is.False);
        }

        [Test, Category("AC-WKT-04-07"), Category("AC-WKT-03-06")]
        public void FinishedRoutineByDistance()
        {
            // QA-G4a-P10: con un último paso por distancia, sin restante (la cuenta sigue con su largo) y progreso 0.
            var finished = Progress(Step(dist: 500), 140);
            Assert.That(finished.RemainingInStep, Is.Null);
            Assert.That(finished.Progress, Is.EqualTo(0));
            Assert.That(finished.IsEndingSoon, Is.False);
            Assert.That(finished.IsUpNext, Is.False);
        }

        private static StepProgress Progress(IntervalStep step, int elapsed, IntervalStep? next = null)
        {
            return new StepProgress(step, 0, next == null ? 1 : 2, elapsed, next);
        }

        private static IntervalStep Step(int? dur = null, int? dist = null)
        {
            return new IntervalStep(1, 0, StepType.Work) { DurationSeconds = dur, DistanceMeters = dist };
        }
    }
}
