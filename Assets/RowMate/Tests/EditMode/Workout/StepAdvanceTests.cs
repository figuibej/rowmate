using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SAD: el avance de pasos y el fin automático (F-WKT-03; 02-tech-design §2.4 de la It. 4a;
    // lib/features/workout/workout_provider.dart:192-218), con Q-55 en el paso por distancia (DV-26).
    [TestFixture]
    public sealed class StepAdvanceTests
    {
        [Test, Category("AC-WKT-03-01")]
        public void Prueba4aExpandsToSixSteps()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.PruebaCuatroA());
                var steps = h.Engine.Steps!;
                var types = new List<StepType>();
                foreach (var s in steps)
                {
                    types.Add(s.Type);
                }

                Assert.That(types, Is.EqualTo(new[] { StepType.Warmup, StepType.Work, StepType.Rest, StepType.Work, StepType.Rest, StepType.Cooldown }));
                Assert.That(steps[1].TargetWattsMin, Is.EqualTo(160));
                Assert.That(steps[1].TargetSpm, Is.EqualTo(22));
                Assert.That(steps[3].TargetWattsMin, Is.EqualTo(160));
                Assert.That(steps[3].TargetSpm, Is.EqualTo(24));
                var total = 0;
                foreach (var s in steps)
                {
                    total += s.DurationSeconds!.Value;
                }

                Assert.That(total, Is.EqualTo(240));
            }
        }

        [Test, Category("AC-WKT-03-02")]
        public void TimeStepEndsAtItsDuration()
        {
            // En Corta los cambios de paso caen en los segundos 10, 15 y 25 y la rutina termina en el 30.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                var changes = new List<int>();
                var last = 0;
                using (h.Engine.State.Subscribe(s =>
                       {
                           var index = s.Step.HasValue ? s.Step.Value.StepIndex : last;
                           if (index != last)
                           {
                               changes.Add(s.TotalElapsedSeconds);
                               last = index;
                           }

                           if (s.Phase == WorkoutPhase.Finished && !changes.Contains(-s.TotalElapsedSeconds))
                           {
                               changes.Add(-s.TotalElapsedSeconds);
                           }
                       }, false))
                {
                    h.At(40);
                }

                Assert.That(changes, Is.EqualTo(new[] { 10, 15, 25, -30 }), "los cambios y el fin (negativo)");
            }
        }

        [Test, Category("AC-WKT-03-02")]
        public void TimeWinsOverDistance()
        {
            var routine = RoutineSeeds.Make("Tiempo y distancia", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 10, dist: 20),
                RoutineSeeds.Step(StepType.Rest, dur: 5));
            using (var h = new WorkoutHarness())
            {
                h.Routine(routine);
                h.At(9);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(0), "36 m remados: la distancia no manda");
                h.At(10);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1));
            }
        }

        // AC-WKT-03-03: Distancia 100 con el saneamiento de los parciales (Q-55): R-4 25; R-4R (reinicio en el 10) 26; R-4Z
        // (paquete sin distancia en el 10) 27; R-4 con 20 s de pausa a los 40 m, remando en la pausa, 25 segundos en marcha.
        // El legacy: 25, 285, 25 y 11 (WorkoutGoldenTests lo comprueba con el oráculo).
        [TestCase("R-4", 25, Category = "AC-WKT-03-03")]
        [TestCase("R-4R", 26, Category = "AC-WKT-03-03")]
        [TestCase("R-4Z", 27, Category = "AC-WKT-03-03")]
        [TestCase("pausa", 25, Category = "AC-WKT-03-03")]
        public void DistanceStepWithTheSanitizer(string rower, int endSecond)
        {
            Func<int, RowingData> reading = rower == "R-4R" ? RowerScripts.R4R : rower == "R-4Z" ? (Func<int, RowingData>)RowerScripts.R4Z : RowerScripts.R4;
            using (var h = new WorkoutHarness(reading))
            {
                h.Routine(RoutineSeeds.DistanciaCien());
                var end = -1;
                using (h.Engine.State.Subscribe(s =>
                       {
                           if (end < 0 && s.Step.HasValue && s.Step.Value.StepIndex == 1)
                           {
                               end = s.TotalElapsedSeconds;
                           }
                       }, false))
                {
                    if (rower == "pausa")
                    {
                        h.At(10);
                        h.Engine.Pause();
                        h.At(30);
                        h.Engine.Resume();
                    }

                    h.At(60);
                }

                Assert.That(end, Is.EqualTo(endSecond));
            }
        }

        [Test, Category("AC-WKT-03-03")]
        public void StartWithoutAPreviousReadingDropsTheFirstJump()
        {
            // QA-G4a-P06 (DV-26): la rutina arranca con RowingData.Empty (distancia 0) y el monitor ya está en 5000 m: el primer
            // salto (5000 en 1 s) no suma, así que el paso de 100 m termina en el segundo 26; el legacy, en el 1.
            using (var h = new WorkoutHarness(k => RowerScripts.R4(k).CopyWith(distanceMeters: 5000 + 4 * k), startRower: false))
            {
                h.Routine(RoutineSeeds.DistanciaCien());
                Assert.That(h.State.Data.DistanceMeters, Is.EqualTo(0), "sin lectura previa");
                h.At(0.5);
                h.Rower.Start();
                h.At(25);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(0));
                h.At(26);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-01-05"), Category("AC-WKT-03-03")]
        public void ResumeRebasesTheMeters()
        {
            // La distancia del monitor al reanudar es la referencia nueva: lo remado en pausa (80 m) no cuenta.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.DistanciaCien());
                h.At(10);
                h.Engine.Pause();
                h.At(30);
                h.Engine.Resume();
                h.At(31);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(0), "44 m, no 124");
                h.At(44);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(0), "96 m");
                h.At(45);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1), "100 m en el segundo 25 en marcha");
            }
        }

        [Test, Category("AC-WKT-03-04")]
        public void StepChangeKeepsTheExtraMetersInTheOldStep()
        {
            // Dos pasos de 10 m con lecturas de 6 m por segundo: el primero termina con 12 (2 de más, que se quedan en él) y el
            // segundo arranca desde la distancia de ese segundo: termina con 12 más, en el segundo 4.
            var routine = RoutineSeeds.Make("Metros", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dist: 10),
                RoutineSeeds.Step(StepType.Work, dist: 10), RoutineSeeds.Step(StepType.Rest, dur: 5));
            using (var h = new WorkoutHarness(k => RowerScripts.R4(k).CopyWith(distanceMeters: 1000 + 6 * k)))
            {
                h.Routine(routine);
                h.At(2);
                var first = h.State.Step!.Value;
                Assert.That(first.StepIndex, Is.EqualTo(1), "el primero terminó en el segundo 2 con 12 m");
                Assert.That(first.ElapsedInStep, Is.EqualTo(0), "el reloj del paso vuelve a 0");
                h.At(3);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1), "6 m del segundo");
                h.At(4);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(2), "12 m desde la distancia del segundo 2");
            }
        }

        [Test, Category("AC-WKT-03-06"), Category("AC-WKT-03-05")]
        public void FinishedRoutineStaysOnTheLastStep()
        {
            // Entre el fin y el cierre (base lenta): el índice queda en el último paso; por tiempo, su reloj en la duración (0:00);
            // por distancia, la cuenta sigue con su largo, sin restante ni progreso (QA-G4a-P10).
            using (var h = new WorkoutHarness())
            {
                var slow = new ScriptedSessionRepository(h.Scheduler) { FinishDelay = TimeSpan.FromSeconds(2) };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, slow, h.Scheduler, h.Lifecycle, h.Logger))
                {
                    h.At(0);
                    engine.StartWithRoutine(RoutineSeeds.Corta());
                    h.At(31);
                    var state = engine.State.Value;
                    Assert.That(state.Phase, Is.EqualTo(WorkoutPhase.Finished));
                    var step = state.Step!.Value;
                    Assert.That(step.StepIndex, Is.EqualTo(3), "no avanza");
                    Assert.That(step.ElapsedInStep, Is.EqualTo(5));
                    Assert.That(step.RemainingInStep, Is.EqualTo(0));
                    Assert.That(step.Progress, Is.EqualTo(1));
                    Assert.That(step.IsEndingSoon, Is.False);
                    Assert.That(step.IsUpNext, Is.False);
                    Assert.That(state.TotalElapsedSeconds, Is.EqualTo(30), "el reloj se detuvo");
                }

                var byDistance = new ScriptedSessionRepository(h.Scheduler) { FinishDelay = TimeSpan.FromSeconds(2) };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, byDistance, h.Scheduler, h.Lifecycle, h.Logger))
                {
                    var routine = RoutineSeeds.Make("Termina por distancia", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dist: 20));
                    engine.StartWithRoutine(routine);
                    h.At(37);
                    var step = engine.State.Value.Step!.Value;
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished));
                    Assert.That(step.RemainingInStep, Is.Null);
                    Assert.That(step.Progress, Is.EqualTo(0));
                    Assert.That(step.IsEndingSoon || step.IsUpNext, Is.False);
                }
            }
        }

        [Test, Category("AC-WKT-03-07")]
        public void ZeroSecondStepEndsAtTheFirstSecond()
        {
            var routine = RoutineSeeds.Make("Cero", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 0), RoutineSeeds.Step(StepType.Rest, dur: 5));
            using (var h = new WorkoutHarness())
            {
                h.Routine(routine);
                h.At(0.5);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(0));
                h.At(1);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1), "en el primer segundo");
            }
        }

        [Test, Category("AC-WKT-03-07")]
        public void NegativeDurationEndsAtTheFirstSecond()
        {
            // DV-37 (DT4a-12): un Min negativo (DV-15); el motor lo termina en el primer segundo, como el legacy (:196-197).
            var routine = RoutineSeeds.Make("Negativa", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Warmup, dur: -90), RoutineSeeds.Step(StepType.Rest, dur: 5));
            using (var h = new WorkoutHarness())
            {
                h.Routine(routine);
                Assert.That(h.State.Step!.Value.RemainingInStep, Is.EqualTo(0), "0:00 mientras dura");
                h.At(1);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-03-08")]
        public void NoGoalStepNeverEnds()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SinMeta());
                h.At(3600);
                var step = h.State.Step!.Value;
                Assert.That(step.StepIndex, Is.EqualTo(0), "nunca termina solo");
                Assert.That(step.RemainingInStep, Is.Null);
                Assert.That(step.Progress, Is.EqualTo(0));
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Active));
                h.Engine.FinishAsync();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle), "se termina con Terminar");
            }
        }

        [Test, Category("AC-WKT-03-09")]
        public void FreeHasNoStepsNorEnd()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(7200);
                Assert.That(h.Engine.Steps, Is.Null);
                Assert.That(h.State.Step.HasValue, Is.False);
                Assert.That(h.State.IsRoutine, Is.False);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Active), "solo termina a mano");
            }
        }

        [Test, Category("AC-WKT-03-10")]
        public void RowerDisconnectKeepsClockAndTimeSteps()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Make("Mixta", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 10),
                    RoutineSeeds.Step(StepType.Work, dist: 100), RoutineSeeds.Step(StepType.Rest, dur: 5)));
                h.At(3);
                h.Rower.Disconnect();
                h.At(20);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(20), "el reloj sigue");
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1), "el paso por tiempo avanzó");
                Assert.That(h.State.Step.Value.ElapsedInStep, Is.EqualTo(10));
                Assert.That(h.Points()[2].DistanceMeters, Is.EqualTo(1012), "los puntos repiten la última lectura");
                Assert.That(h.Points()[3].DistanceMeters, Is.EqualTo(1012));
                h.At(40);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1), "el paso por distancia queda quieto");
                h.Rower.Connect();
                h.At(70);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(2), "con las lecturas nuevas, sigue");
            }
        }

        [Test, Category("AC-WKT-09-07")]
        public void TargetsFromTheEngine()
        {
            // Corta con R-4: los trabajos (0 y 2) cumplidos y los descansos sin resultado; con R-4(19), el paso 2 (objetivo 20)
            // no se cumple. Terminar corta el paso en curso sin resultado; todo se reinicia al volver a reposo.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(29);
                Assert.That(h.Engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met));
                Assert.That(h.Engine.Targets.ResultOf(1), Is.Null);
                Assert.That(h.Engine.Targets.ResultOf(2), Is.EqualTo(StepResult.Met));
                Assert.That(h.Engine.Targets.ResultOf(3), Is.Null);
            }

            using (var h = new WorkoutHarness(RowerScripts.R4Spm(19)))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(29);
                Assert.That(h.Engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met), "objetivo 18");
                Assert.That(h.Engine.Targets.ResultOf(2), Is.EqualTo(StepResult.Missed), "objetivo 20");
            }

            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.PruebaCuatroA());
                h.At(60);
                h.Engine.FinishAsync();
                Assert.That(h.Engine.Targets.ResultOf(1), Is.Null, "cortado con Terminar: sin resultado (y en reposo, reiniciado)");
            }

            using (var h = new WorkoutHarness())
            {
                // El último paso también se evalúa.
                var lastWithTarget = RoutineSeeds.Make("Último", RoutineSeeds.Day(24), RoutineSeeds.Step(StepType.Work, dur: 5, wMin: 100));
                var results = new List<StepResult?>();
                using (h.Engine.State.Subscribe(s =>
                       {
                           if (s.Phase == WorkoutPhase.Finished)
                           {
                               results.Add(h.Engine.Targets.ResultOf(0));
                           }
                       }, false))
                {
                    h.Routine(lastWithTarget);
                    h.At(6);
                }

                Assert.That(results.Count, Is.GreaterThanOrEqualTo(1));
                Assert.That(results[0], Is.EqualTo(StepResult.Met));
            }
        }
    }
}
