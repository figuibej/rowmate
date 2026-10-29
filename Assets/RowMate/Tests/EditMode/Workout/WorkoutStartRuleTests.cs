using System;
using System.Diagnostics;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WSR: R3-08, una rutina que no se puede iniciar (AC-WKT-12-08, AC-RUT-06-08; 02-tech-design §5 de la It. 4a), sin expandir
    // y sin bucles por repetición.
    [TestFixture]
    public sealed class WorkoutStartRuleTests
    {
        [Test, Category("AC-WKT-12-08")]
        public void HugeSeriesIsDecidedFastAndWithoutMemory()
        {
            var huge = RoutineSeeds.Rara4a("Serie enorme").Routines[0];
            WorkoutStartRule.CanStart(huge); // JIT
            var before = AllocatedBytes.Now();
            var watch = Stopwatch.StartNew();
            var canStart = WorkoutStartRule.CanStart(huge);
            watch.Stop();
            var allocated = AllocatedBytes.Since(before);
            Assert.That(canStart, Is.False, "más de 999 repeticiones");
            Assert.That(watch.Elapsed.TotalMilliseconds, Is.LessThan(50), "en la PC, menos de 50 ms");
            if (allocated.HasValue)
            {
                Assert.That(allocated.Value, Is.LessThan(64 * 1024), "sin reservar memoria por repetición");
            }
        }

        [Test, Category("AC-WKT-12-08")]
        public void OutOfTheLimitsCannotStart()
        {
            Assert.That(WorkoutStartRule.CanStart(RoutineSeeds.Rara4a("Vatios imposibles").Routines[0]), Is.False, "50 000 W");
            Assert.That(WorkoutStartRule.CanStart(RoutineSeeds.RaraHuge.Routines[0]), Is.False);
            Assert.That(WorkoutStartRule.CanStart(RoutineSeeds.RaraWatts.Routines[0]), Is.False);
        }

        [Test, Category("AC-RUT-06-08"), Category("AC-WKT-12-08")]
        public void EmptyExpansionCannotStart()
        {
            Assert.That(WorkoutStartRule.CanStart(RoutineSeeds.Rara4a("Serie ×0").Routines[0]), Is.False, "×0: expandida no tiene pasos");
            Assert.That(WorkoutStartRule.CanStart(RoutineSeeds.RaraNegativeRepeat.Routines[0]), Is.False, "×−3");
            Assert.That(WorkoutStartRule.CanStart(new Routine("Vacía", RoutineSeeds.Day(24))), Is.False, "sin pasos");
        }

        [Test, Category("AC-WKT-12-08")]
        public void TheSeedsOfTheAppCanStart()
        {
            foreach (var seed in new[] { RoutineSeeds.Lista, RoutineSeeds.F4A, RoutineSeeds.Borde })
            {
                foreach (var routine in seed.Routines)
                {
                    Assert.That(WorkoutStartRule.CanStart(routine), Is.True, seed + ": " + routine.Name);
                }
            }

            // La progresión de un paso suelto no se ve ni se aplica (AC-RUT-12-03): no cuenta.
            Assert.That(WorkoutStartRule.CanStart(RoutineSeeds.RaraLooseProgressions.Routines[0]), Is.True);
        }

        [Test, Category("AC-WKT-12-08")]
        public void HugeNegativeSplitStartsWithoutOverflow()
        {
            // QA-G4aT-11 (DT4a-16): un split guardado ≤ 0 es "sin split" para la regla; con progresión −5 999 y ×2 pasa la regla y
            // el motor satura en int.MinValue en lugar de lanzar. Split queda siempre fuera, como en el legacy (enteros de 64 bits).
            var g = RoutineSeeds.SeriesId(RoutineSeeds.Day(24), 0);
            var routine = RoutineSeeds.Make("Split negativo enorme", RoutineSeeds.Day(24),
                RoutineSeeds.Step(StepType.Work, dur: 60, split: -2147483000, g: g, rep: 2, pSplit: -5999));
            Assert.That(WorkoutStartRule.CanStart(routine), Is.True);
            Assert.Throws<OverflowException>(() => RoutineExpansion.Flatten(routine.Steps), "Flatten(template) sigue con checked");
            var saturated = RoutineExpansion.Flatten(routine.Steps, ProgressionOverflow.Saturate);
            Assert.That(saturated[0].TargetSplitSeconds, Is.EqualTo(-2147483000));
            Assert.That(saturated[1].TargetSplitSeconds, Is.EqualTo(int.MinValue));
            using (var h = new WorkoutHarness())
            {
                h.At(0);
                Assert.That(h.Engine.StartWithRoutine(routine), Is.EqualTo(StartOutcome.Started));
                h.At(61);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1));
                Assert.That(TargetLight.Evaluate(h.State.Step.Value.Step, h.State.Data).Split, Is.EqualTo(TargetState.Off), "siempre fuera");
            }
        }

        [Test, Category("AC-WKT-12-08")]
        public void TheEngineChecksTheRuleToo()
        {
            // Otra entrada (la 4b, la It. 12) no puede saltearla: el motor la vuelve a mirar y no cambia nada.
            using (var h = new WorkoutHarness())
            {
                h.At(0);
                Assert.That(h.Engine.StartWithRoutine(RoutineSeeds.Rara4a("Serie enorme").Routines[0]), Is.EqualTo(StartOutcome.CannotStart));
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.Memory!.Sessions, Is.Empty, "ninguna sesión");
                Assert.That(h.Engine.StartFree(), Is.EqualTo(StartOutcome.Started), "después, el libre arranca normalmente");
            }
        }

        [Test, Category("AC-WKT-12-08")]
        public void SaturationClampsBothEnds()
        {
            var g = RoutineSeeds.SeriesId(RoutineSeeds.Day(24), 0);
            var up = RoutineSeeds.Make("Arriba", RoutineSeeds.Day(24),
                RoutineSeeds.Step(StepType.Work, dur: 60, spm: int.MaxValue - 1, g: g, rep: 3, pSpm: 5));
            var steps = RoutineExpansion.Flatten(up.Steps, ProgressionOverflow.Saturate);
            Assert.That(steps[1].TargetSpm, Is.EqualTo(int.MaxValue));
            Assert.That(steps[2].TargetSpm, Is.EqualTo(int.MaxValue));
            Assert.Throws<OverflowException>(() => RoutineExpansion.Flatten(up.Steps, ProgressionOverflow.Throw));
        }
    }
}
