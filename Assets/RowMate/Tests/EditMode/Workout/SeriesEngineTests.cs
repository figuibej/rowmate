using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SEN: el SeriesTracker en el motor (05-series-parciales/02-tech-design §2.3; AC-WKT-07-03, -07 a -11; AC-WKT-01-05, -02-04,
    // -02-11, -03-03, -08-08). Una lectura por segundo, la del segundo, ANTES del fin del paso (workout_provider.dart:176-186);
    // Begin al iniciar y al volver a reposo; Rebase al reanudar; nada en pausa, en el libre ni después del fin; el paso por
    // distancia con los MISMOS metros del tracker (DV-26); el segundo plano y el equipo suspendido (con R-4S).
    [TestFixture]
    public sealed class SeriesEngineTests
    {
        [Test, Category("AC-WKT-07-03")]
        public void OneReadingPerSecondBeforeTheEndOfTheStep()
        {
            // Con Series 600 y R-4, el trabajo de la rep 1 recibe las lecturas de los segundos 31 a 180, y su descanso, las de 181
            // a 200; la lectura del segundo en que termina un paso es de ese paso.
            using (var h = new WorkoutHarness())
            {
                var calls = Trace(h.Engine.Series);
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(571);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(calls[0].Kind, Is.EqualTo(SeriesCallKind.Begin));
                Assert.That(calls[0].RawMeters, Is.EqualTo(1000), "la distancia del monitor al iniciar (L(0))");
                var updates = Updates(calls);
                Assert.That(updates.Count, Is.EqualTo(570), "una por segundo, ninguna de más");
                for (var i = 0; i < updates.Count; i++)
                {
                    Assert.That(updates[i].ActiveSeconds, Is.EqualTo(i + 1));
                    Assert.That(updates[i].RawMeters, Is.EqualTo(1000 + 4 * (i + 1)), "L(k) en el segundo k");
                }

                Assert.That(SecondsOf(updates, 1), Is.EqualTo(Range(31, 180)), "el trabajo de la rep 1");
                Assert.That(SecondsOf(updates, 2), Is.EqualTo(Range(181, 200)), "su descanso");
                Assert.That(updates[179].Step!.Type, Is.EqualTo(StepType.Work));
                Assert.That(updates[179].Position, Is.EqualTo(new StepPosition(RoutineSeeds.SeriesId(RoutineSeeds.F5Day(8), 0), 1, 3)));
            }
        }

        [Test, Category("AC-WKT-07-03"), Category("AC-WKT-07-11")]
        public void NothingInPauseInFreeOrAfterTheEnd()
        {
            using (var h = new WorkoutHarness())
            {
                var calls = Trace(h.Engine.Series);
                h.Free();
                h.At(30);
                Assert.That(calls, Is.Empty, "en el libre no hay parciales ni repeticiones");
                Assert.That(h.State.Series, Is.Null);
                h.Engine.FinishAsync();
                h.At(31);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
            }

            using (var h = new WorkoutHarness())
            {
                var calls = Trace(h.Engine.Series);
                h.Routine(RoutineSeeds.Corta());
                h.At(5.3);
                h.Engine.Pause();
                var before = calls.Count;
                h.At(25.3);
                Assert.That(calls.Count, Is.EqualTo(before), "en pausa nada");
                Assert.That(h.State.Series!.CurrentLapSeconds, Is.EqualTo(5.0), "quieto con el último segundo");
                h.Engine.Resume();
                Assert.That(calls[calls.Count - 1].Kind, Is.EqualTo(SeriesCallKind.Rebase));
                h.At(80);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                var count = calls.Count;
                h.At(90);
                Assert.That(calls.Count, Is.EqualTo(count), "después del fin, nada");
                Assert.That(Updates(calls).Count, Is.EqualTo(30));
            }
        }

        [Test, Category("AC-WKT-07-07"), Category("AC-WKT-01-05")]
        public void WhatIsRowedInPauseDoesNotCount()
        {
            // AC-WKT-07-07: Trabajo 1000 con R-4, Pausa a los 20,7 s y Reanudar a los 50,7 s: en pausa 0:20.0 y 80 m; el primer
            // segundo después, 0:21.0 y 84 m; #1 en el segundo 125 de reloj activo con 2:05.0. Lo que no suma son 124 m (de
            // L(20) = 1080 a L(51) = 1204: la fracción que pierde la pausa, Q-58, y el medio segundo de las lecturas).
            // AC-WKT-01-05: la referencia nueva vale para los parciales y para el paso por distancia a la vez.
            using (var h = new WorkoutHarness())
            {
                var calls = Trace(h.Engine.Series);
                h.Routine(RoutineSeeds.TrabajoMil());
                h.At(20.7);
                h.Engine.Pause();
                h.At(50.7);
                var paused = h.State.Series!;
                Assert.That(paused.CurrentLapSeconds, Is.EqualTo(20.0));
                Assert.That(paused.CurrentLapMeters, Is.EqualTo(80));
                Assert.That(WorkoutFormats.LapTime(paused.CurrentLapSeconds), Is.EqualTo("0:20.0"));
                h.Engine.Resume();
                Assert.That(calls[calls.Count - 1].Kind, Is.EqualTo(SeriesCallKind.Rebase));
                Assert.That(calls[calls.Count - 1].RawMeters, Is.EqualTo(1204), "L(51) es la referencia nueva");
                h.At(51.8);
                var first = h.Engine.Series.State;
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(21));
                Assert.That(first.CurrentLapSeconds, Is.EqualTo(21.0));
                Assert.That(first.CurrentLapMeters, Is.EqualTo(84));
                h.At(155.8);
                var s = h.Engine.Series.State;
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(125));
                Assert.That(s.Laps.Count, Is.EqualTo(1));
                Assert.That(WorkoutFormats.LapTime(s.Laps[0].Seconds), Is.EqualTo("2:05.0"));
                var raw = h.State.Data.DistanceMeters - 1000;
                Assert.That(raw - h.Engine.Series.Meters, Is.EqualTo(124), "lo remado en pausa que no suma: 124 m, no 120");
                h.At(281);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(250), "el paso por distancia con la misma referencia: como sin la pausa");
            }
        }

        [TestCase("R-4", 250)]
        [TestCase("R-4R", 251)]
        [TestCase("R-4Z", 252)]
        // It. 10a (QA-V4a-L01, AC-WKT-03-11): con R-4D la lectura después del corte suma (16 m en 4 s desde el último cambio): 250.
        [TestCase("R-4D", 250)]
        [TestCase("R-4X", -1)]
        [Category("AC-WKT-07-09"), Category("AC-WKT-03-03")]
        public void TheSameMetersAsTheDistanceStepSecondBySecond(string rower, int end)
        {
            // AC-WKT-07-09: los metros del paso que cuentan los parciales y los que deciden el fin del paso por distancia son los
            // mismos, segundo a segundo: el paso termina en el primer segundo en que SeriesState.StepMeters llega a su meta. Trabajo
            // 1000 termina en 250, 251, 252 y (R-4D, desde la It. 10a) 250; con R-4X (la distancia quieta) nunca.
            using (var h = new WorkoutHarness(RowerScripts.ByName(rower)))
            {
                var steps = new List<(int Second, int Step, double StepMeters)>();
                h.Engine.Series.Trace = c =>
                {
                    if (c.Kind == SeriesCallKind.Update)
                    {
                        steps.Add((c.ActiveSeconds, c.StepIndex, h.Engine.Series.State.StepMeters));
                    }
                };
                h.Routine(RoutineSeeds.TrabajoMil());
                h.At(300);
                CheckSameMeters(steps, h, RoutineSeeds.TrabajoMil());
                if (end > 0)
                {
                    Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                    Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(end));
                    Assert.That(h.State.Series!.Laps.Count, Is.EqualTo(2), "la vuelta 2 cierra en el mismo segundo en que termina");
                    Assert.That(h.State.Series!.StepMeters, Is.GreaterThanOrEqualTo(1000));
                }
                else
                {
                    Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Active));
                    Assert.That(h.State.Series!.StepMeters, Is.EqualTo(0));
                    Assert.That(h.State.Series!.CurrentLapMeters, Is.EqualTo(0), "nunca negativo con los extremos de FTMS");
                }
            }
        }

        [TestCase("R-4")]
        [TestCase("R-4D")]
        [TestCase("R-4R")]
        [Category("AC-WKT-07-09"), Category("AC-WKT-03-03")]
        public void TheSameMetersInEveryStepOfSeriesSeiscientos(string rower)
        {
            // Un trabajo de 600 m termina con la vuelta en curso en 100 m, o en algo más si la última lectura se pasó.
            using (var h = new WorkoutHarness(RowerScripts.ByName(rower)))
            {
                var steps = new List<(int Second, int Step, double StepMeters)>();
                h.Engine.Series.Trace = c =>
                {
                    if (c.Kind == SeriesCallKind.Update)
                    {
                        steps.Add((c.ActiveSeconds, c.StepIndex, h.Engine.Series.State.StepMeters));
                    }
                };
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(600);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                CheckSameMeters(steps, h, RoutineSeeds.SeriesSeiscientos());
            }
        }

        [Test, Category("AC-WKT-07-10"), Category("AC-WKT-02-04")]
        public void TheBackgroundGivesTheSameAsTheForeground()
        {
            // Con R-4 y segundo plano de los 100 a los 400 s: los segundos que faltan se procesan en orden, cada uno con su lectura,
            // así vueltas y repeticiones dan lo mismo que en primer plano.
            var foreground = RunWithLifecycle(h => h.RunUntil(400.25));
            var background = RunWithLifecycle(h =>
            {
                h.Background(100);
                h.Foreground(400);
                h.RunUntil(400.25);
            });
            Assert.That(background.Calls, Is.EqualTo(foreground.Calls), "las mismas llamadas, en el mismo orden");
            Assert.That(background.Final, Is.EqualTo(foreground.Final));
            Assert.That(foreground.Final, Does.Contain("rep=3"), "la rep 3 a los 400 s");
        }

        [TestCase(true, Category = "AC-WKT-07-10,AC-WKT-02-11", TestName = "SuspendedWithRFourS_RawCounts")]
        [TestCase(false, Category = "AC-WKT-07-10,AC-WKT-02-11", TestName = "SuspendedWithRFourS_RawStops")]
        public void SuspendedWithRFourS(bool monotonicCounts)
        {
            // AC-WKT-07-10: con R-4S (el monitor sigue contando con el equipo dormido) y una suspensión de 600 s a los 250,4: la
            // vuelta en curso sigue desde 0:50.0 y 200 m; a los 251, 0:51.0 y 200 m (L(851) = 4404: el salto de 2404 m no suma);
            // a los 252, 0:52.0 y 204 m. El lapso no suma tiempo a la vuelta en curso.
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var sessions = new InMemorySessionRepository();
            using (var rower = new ScriptedRower(harness.Scheduler, k => RowerScripts.R4(k), harness.Logger, () => harness.RealReadingIndex))
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), sessions, harness.Scheduler, harness.Lifecycle,
                       harness.Logger))
            {
                rower.Start();
                harness.RunUntil(0);
                Assert.That(engine.StartWithRoutine(RoutineSeeds.SeriesSeiscientos()), Is.EqualTo(StartOutcome.Started));
                harness.RunUntil(250.25);
                var before = engine.Series.State;
                Assert.That(WorkoutFormats.LapTime(before.CurrentLapSeconds), Is.EqualTo("0:50.0"));
                Assert.That(before.CurrentLapMeters, Is.EqualTo(200));
                harness.Suspend(250.4);
                harness.Wake(600, monotonicCounts);
                harness.RunUntil(251.25);
                var woke = engine.Series.State;
                Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(251));
                Assert.That(engine.State.Value.Data.DistanceMeters, Is.EqualTo(4404), "L(851): el monitor siguió contando");
                Assert.That(WorkoutFormats.LapTime(woke.CurrentLapSeconds), Is.EqualTo("0:51.0"));
                Assert.That(woke.CurrentLapMeters, Is.EqualTo(200), "el salto de 2404 m en 1 s no suma");
                harness.RunUntil(252.25);
                var next = engine.Series.State;
                Assert.That(WorkoutFormats.LapTime(next.CurrentLapSeconds), Is.EqualTo("0:52.0"));
                Assert.That(next.CurrentLapMeters, Is.EqualTo(204), "desde la siguiente suma de nuevo");
            }
        }

        [Test, Category("AC-WKT-07-11")]
        public void BeginOnStartAndOnIdleNothingFromThePreviousSession()
        {
            // :128, :315: se reinicia al iniciar cada rutina y al volver a reposo; una sesión nueva nunca muestra vueltas ni
            // repeticiones de la anterior; nada se guarda (los puntos de la sesión son los de siempre: la foto no los toca).
            using (var h = new WorkoutHarness())
            {
                var calls = Trace(h.Engine.Series);
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(380.25);
                Assert.That(h.State.Series!.PreviousReps.Count, Is.EqualTo(2));
                h.Engine.FinishAsync();
                h.At(381);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.State.Series, Is.Null, "en reposo, sin paneles");
                Assert.That(calls[calls.Count - 1].Kind, Is.EqualTo(SeriesCallKind.Begin), "ReturnToIdle: Begin(0, 0)");
                Assert.That(h.Engine.Series.State.PreviousReps, Is.Empty);
                Assert.That(h.Engine.Series.State.StepIndex, Is.Null);
                var points = h.Points();
                Assert.That(points.Count, Is.GreaterThan(0));
                h.Engine.StartWithRoutine(RoutineSeeds.SeriesSeiscientos());
                var fresh = h.State.Series!;
                Assert.That(fresh.StepIndex, Is.Null, "antes del primer segundo de la sesión nueva");
                Assert.That(fresh.Laps, Is.Empty);
                Assert.That(fresh.PreviousReps, Is.Empty);
                Assert.That(fresh.RepMeters, Is.EqualTo(0));
                Assert.That(calls[calls.Count - 1].Kind, Is.EqualTo(SeriesCallKind.Begin));
                Assert.That(calls[calls.Count - 1].RawMeters, Is.EqualTo(h.State.Data.DistanceMeters), "la distancia del monitor al iniciar");
            }
        }

        [Test, Category("AC-WKT-08-08"), Category("AC-WKT-07-11")]
        public void TheSnapshotCarriesTheTrackerStateWithoutCopying()
        {
            // §2.3: la foto lleva la instancia vigente del tracker (entre dos segundos, la misma); null en el libre y en reposo.
            using (var h = new WorkoutHarness())
            {
                Assert.That(h.State.Series, Is.Null, "reposo");
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                Assert.That(h.State.Series, Is.SameAs(h.Engine.Series.State));
                h.At(201.25);
                var atSecond = h.State.Series;
                Assert.That(atSecond, Is.SameAs(h.Engine.Series.State));
                h.At(201.6);
                Assert.That(h.State.Revision, Is.GreaterThan(0));
                Assert.That(h.State.Series, Is.SameAs(atSecond), "la lectura de 201,5 publica una foto nueva con la misma foto del tracker");
                Assert.That(atSecond!.PreviousReps[0].Rep, Is.EqualTo(1));
            }
        }

        // ── Lo común ──

        private static List<SeriesCall> Trace(SeriesTracker tracker)
        {
            var calls = new List<SeriesCall>();
            tracker.Trace = calls.Add;
            return calls;
        }

        private static List<SeriesCall> Updates(List<SeriesCall> calls)
        {
            var updates = new List<SeriesCall>();
            foreach (var c in calls)
            {
                if (c.Kind == SeriesCallKind.Update)
                {
                    updates.Add(c);
                }
            }

            return updates;
        }

        private static int[] SecondsOf(List<SeriesCall> updates, int step)
        {
            var seconds = new List<int>();
            foreach (var u in updates)
            {
                if (u.StepIndex == step)
                {
                    seconds.Add(u.ActiveSeconds);
                }
            }

            return seconds.ToArray();
        }

        private static int[] Range(int from, int to)
        {
            var result = new int[to - from + 1];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = from + i;
            }

            return result;
        }

        // Cada paso por distancia termina en el primer segundo en que los metros del tracker llegan a su meta, y no antes.
        private static void CheckSameMeters(List<(int Second, int Step, double StepMeters)> seconds, WorkoutHarness h, Routine routine)
        {
            var flat = RoutineExpansion.Flatten(routine.Steps);
            Assert.That(seconds.Count, Is.GreaterThan(0));
            for (var i = 0; i < seconds.Count; i++)
            {
                var current = seconds[i];
                var step = flat[current.Step];
                if (!step.DistanceMeters.HasValue || step.DurationSeconds.HasValue)
                {
                    continue;
                }

                bool advanced;
                if (i + 1 < seconds.Count)
                {
                    advanced = seconds[i + 1].Step != current.Step;
                }
                else
                {
                    advanced = h.State.Phase == WorkoutPhase.Finished;
                }

                Assert.That(advanced, Is.EqualTo(current.StepMeters >= step.DistanceMeters.Value),
                    "segundo " + current.Second + ", paso " + current.Step + ": " + current.StepMeters + " m del tracker");
            }
        }

        private sealed class LifecycleRun
        {
            public List<string> Calls { get; } = new List<string>();

            public string Final { get; set; } = string.Empty;
        }

        private static LifecycleRun RunWithLifecycle(Action<LifecycleHarness> drive)
        {
            var result = new LifecycleRun();
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var sessions = new InMemorySessionRepository();
            using (var rower = new ScriptedRower(harness.Scheduler, k => RowerScripts.R4(k), harness.Logger))
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), sessions, harness.Scheduler, harness.Lifecycle,
                       harness.Logger))
            {
                rower.Start();
                engine.Series.Trace = c => result.Calls.Add(c.Kind + " " + c.StepIndex + " " + c.RawMeters + " " + c.ActiveSeconds);
                harness.RunUntil(0);
                engine.StartWithRoutine(RoutineSeeds.SeriesSeiscientos());
                drive(harness);
                var s = engine.Series.State;
                result.Final = "step=" + s.StepIndex + " laps=" + s.Laps.Count + " lapM=" + s.CurrentLapMeters + " lapS=" + s.CurrentLapSeconds + " rep=" + s.Rep
                               + " repM=" + s.RepMeters + " prev=" + s.PreviousReps.Count + " t=" + engine.State.Value.TotalElapsedSeconds;
            }

            return result;
        }
    }
}
