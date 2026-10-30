using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // BCD: cuándo hay cuenta grande y qué número (D-ANI-05; AC-WKT-14-18; 02-tech-design §2.4 de la It. 4b), con la regla pura y
    // con el motor de verdad (R-4 en el instante de la spec). Sale del restante del motor (workout_provider.dart:101-104).
    [TestFixture]
    public sealed class BigCountdownTests
    {
        [Test, Category("AC-WKT-14-18")]
        public void WindowsOfEstados()
        {
            // Estados con R-4: de los 7 a los 10 s, de los 17 a los 20, de los 27 a los 30, de los 47 a los 50 y de los 57 a los 60.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Estados());
                var shown = new List<string>();
                for (var t = 1; t <= 59; t++)
                {
                    h.At(t + 0.2);
                    var value = BigCountdown.ValueOf(h.State.Step, h.State.Phase);
                    if (value.HasValue)
                    {
                        shown.Add(t + "=" + value.Value);
                    }
                }

                Assert.That(string.Join(" ", shown), Is.EqualTo("7=3 8=2 9=1 17=3 18=2 19=1 27=3 28=2 29=1 47=3 48=2 49=1 57=3 58=2 59=1"));
                h.At(60.2);
                Assert.That(BigCountdown.ValueOf(h.State.Step, h.State.Phase), Is.Null, "terminada");
            }
        }

        [Test, Category("AC-WKT-14-18")]
        public void FinalByDistanceOnlyInTheTimedStep()
        {
            // Final por distancia: de los 7 a los 10 s, y nunca en el paso de 100 m.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.FinalPorDistancia());
                var shown = new List<int>();
                for (var t = 1; t <= 34; t++)
                {
                    h.At(t + 0.2);
                    if (BigCountdown.ValueOf(h.State.Step, h.State.Phase).HasValue)
                    {
                        shown.Add(t);
                    }
                }

                Assert.That(shown, Is.EqualTo(new[] { 7, 8, 9 }));
            }
        }

        [Test, Category("AC-WKT-14-18")]
        public void NothingInPauseFreeDistanceNoGoalZeroOrNegative()
        {
            var timed = new StepProgress(RoutineSeeds.Step(StepType.Work, dur: 10), 0, 2, 7, null);
            Assert.That(BigCountdown.ValueOf(timed, WorkoutPhase.Active), Is.EqualTo(3));
            Assert.That(BigCountdown.ValueOf(timed, WorkoutPhase.Paused), Is.Null, "en pausa va PAUSA (AC-WKT-13-12)");
            Assert.That(BigCountdown.ValueOf(timed, WorkoutPhase.Finished), Is.Null);
            Assert.That(BigCountdown.ValueOf(timed, WorkoutPhase.Idle), Is.Null);
            Assert.That(BigCountdown.ValueOf(null, WorkoutPhase.Active), Is.Null, "en libre");
            Assert.That(BigCountdown.ValueOf(new StepProgress(RoutineSeeds.Step(StepType.Work, dist: 100), 0, 1, 50, null), WorkoutPhase.Active), Is.Null);
            Assert.That(BigCountdown.ValueOf(new StepProgress(RoutineSeeds.Step(StepType.Work), 0, 1, 50, null), WorkoutPhase.Active), Is.Null);
            Assert.That(BigCountdown.ValueOf(new StepProgress(RoutineSeeds.Step(StepType.Work, dur: 0), 0, 1, 0, null), WorkoutPhase.Active), Is.Null);
            Assert.That(BigCountdown.ValueOf(new StepProgress(RoutineSeeds.Step(StepType.Work, dur: -3), 0, 1, 0, null), WorkoutPhase.Active), Is.Null);
            Assert.That(BigCountdown.ValueOf(new StepProgress(RoutineSeeds.Step(StepType.Work, dur: 10), 0, 1, 4, null), WorkoutPhase.Active), Is.Null,
                "restante 6");
            Assert.That(BigCountdown.ValueOf(new StepProgress(RoutineSeeds.Step(StepType.Work, dur: 2), 0, 1, 0, null), WorkoutPhase.Active), Is.EqualTo(2),
                "un paso más corto, desde que empieza");
        }

        [Test, Category("AC-WKT-14-18")]
        public void LastStepToo()
        {
            var last = new StepProgress(RoutineSeeds.Step(StepType.Rest, dur: 10), 4, 5, 9, null);
            Assert.That(BigCountdown.ValueOf(last, WorkoutPhase.Active), Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-14-18")]
        public void ComesBackAfterResume()
        {
            // La regla no tiene memoria: al reanudar vuelve si corresponde.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Estados());
                h.At(7.5);
                h.Engine.Pause();
                Assert.That(BigCountdown.ValueOf(h.State.Step, h.State.Phase), Is.Null);
                h.At(20);
                h.Engine.Resume();
                Assert.That(BigCountdown.ValueOf(h.State.Step, h.State.Phase), Is.EqualTo(3));
            }
        }
    }
}
