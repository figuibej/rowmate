using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WEL: el ciclo de vida y las fases del motor (F-WKT-01, -02; 02-tech-design §2.1-§2.3, §2.9, §2.10 de la It. 4a;
    // lib/features/workout/workout_provider.dart:44-328), con el remo de prueba R-4 y el reloj de prueba (spec §0.4).
    [TestFixture]
    public sealed class WorkoutEngineLifecycleTests
    {
        [Test, Category("AC-WKT-01-01")]
        public void FourPhasesStartingIdle()
        {
            using (var h = new WorkoutHarness())
            {
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle), "la app abre en reposo");
                Assert.That(h.State.Data, Is.EqualTo(RowingData.Empty).Or.EqualTo(RowerScripts.R4(0)));
                var phases = new List<WorkoutPhase>();
                using (h.Engine.State.Subscribe(s => { if (phases.Count == 0 || phases[phases.Count - 1] != s.Phase) phases.Add(s.Phase); }, false))
                {
                    h.Free();
                    h.At(3);
                    h.Engine.Pause();
                    h.Engine.Resume();
                    h.Engine.FinishAsync();
                }

                Assert.That(phases, Is.EqualTo(new[] { WorkoutPhase.Active, WorkoutPhase.Paused, WorkoutPhase.Active, WorkoutPhase.Finished, WorkoutPhase.Idle }));
                Assert.That(Enum.GetValues(typeof(WorkoutPhase)).Length, Is.EqualTo(4));
            }
        }

        [Test, Category("AC-WKT-01-01")]
        public void ReadingsArriveInEveryPhase()
        {
            using (var h = new WorkoutHarness())
            {
                h.At(1);
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(301), "en reposo: L(1)");
                h.Engine.StartFree();
                h.At(3);
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(303), "en marcha");
                h.Engine.Pause();
                h.At(6);
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(306), "en pausa");
                h.HeartRate.EmitBpm(140);
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(140), "el pulsómetro también");
            }
        }

        [Test, Category("AC-WKT-01-02")]
        public void StartFreeCreatesTheSessionThenTheClock()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Active));
                Assert.That(h.Memory!.Operations, Is.EqualTo(new[] { "create" }), "el alta en el acto");
                var session = h.Session()!;
                Assert.That(session.StartedAt, Is.EqualTo(WorkoutHarness.SessionStart), "la hora de inicio, del reloj de prueba");
                Assert.That(session.RoutineId, Is.Null);
                Assert.That(session.RoutineName, Is.Null);
                h.At(0.999);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(0));
                h.At(1);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(1), "el primer segundo, 1 s después (el alta volvió en el acto)");
                Assert.That(h.Points(), Is.Empty, "la cuenta de muestreo arrancó en 0");
            }
        }

        [Test, Category("AC-WKT-01-02"), Category("AC-WKT-06-13")]
        public void ClockNeverWaitsMoreThan100Ms()
        {
            // DT4a-02 (la condición de QA): con un alta de 2 s, el reloj arranca a los 100 ms del toque y los puntos tomados
            // mientras tanto esperan el id en el grabador.
            using (var h = new WorkoutHarness(sessions: null))
            {
                var slow = new ScriptedSessionRepository(h.Scheduler) { CreateDelay = TimeSpan.FromSeconds(2) };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, slow, h.Scheduler, h.Lifecycle, h.Logger))
                {
                    h.At(0);
                    engine.StartFree();
                    h.At(1.099);
                    Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(0));
                    h.At(1.1);
                    Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(1), "el segundo 1 a los 1,1 s: el tramo abrió a los 100 ms");
                    h.At(5.1);
                    Assert.That(slow.Store.AllPoints.Count, Is.EqualTo(1), "el punto del segundo 5 salió cuando volvió el alta");
                    Assert.That(slow.Store.AllPoints[0].SessionId, Is.EqualTo(1));
                }
            }
        }

        [Test, Category("AC-WKT-01-03")]
        public void RoutineStartExpandsAndPositions()
        {
            using (var h = new WorkoutHarness())
            {
                h.At(-0.1);
                h.Routine(RoutineSeeds.PruebaCuatroA());
                var steps = h.Engine.Steps!;
                Assert.That(steps.Count, Is.EqualTo(6));
                Assert.That(steps[1].TargetSpm, Is.EqualTo(22));
                Assert.That(steps[3].TargetSpm, Is.EqualTo(24), "la repetición 2, progresada");
                var positions = h.Engine.Positions!;
                Assert.That(positions.Count, Is.EqualTo(6));
                Assert.That(positions[0], Is.EqualTo(StepPosition.Loose));
                Assert.That(positions[1].Rep, Is.EqualTo(1));
                Assert.That(positions[3].Rep, Is.EqualTo(2));
                Assert.That(positions[3].RepCount, Is.EqualTo(2));
                var step = h.State.Step!.Value;
                Assert.That(step.StepIndex, Is.EqualTo(0));
                Assert.That(step.ElapsedInStep, Is.EqualTo(0));
                Assert.That(h.State.IsRoutine, Is.True);
            }
        }

        [Test, Category("AC-WKT-01-03"), Category("AC-WKT-06-01")]
        public void SessionKeepsTheTemplateIdAndName()
        {
            using (var h = new WorkoutHarness())
            {
                var template = WorkoutHarness.Saved(RoutineSeeds.Corta(), 42);
                h.Routine(template);
                var renamed = template with { Name = "Otra" };
                h.At(31);
                var session = h.Session()!;
                Assert.That(session.RoutineId, Is.EqualTo(42), "copiado de la plantilla");
                Assert.That(session.RoutineName, Is.EqualTo("Corta"), "renombrar después no lo cambia");
                Assert.That(renamed.Name, Is.EqualTo("Otra"));
                Assert.That(session.FinishedAt.HasValue, Is.True);
            }
        }

        [Test, Category("AC-WKT-01-04")]
        public void PauseFreezesClocksSamplingTargetsAndMeters()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.DistanciaCien());
                h.At(4.5);
                h.Engine.Pause();
                var step = h.State.Step!.Value;
                h.At(60);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(4), "el reloj total quieto");
                Assert.That(h.State.Step!.Value.ElapsedInStep, Is.EqualTo(step.ElapsedInStep), "el del paso quieto");
                Assert.That(h.State.Step.Value.StepIndex, Is.EqualTo(0), "los metros del paso por distancia no avanzan");
                Assert.That(h.Points(), Is.Empty, "sin muestreo");
                Assert.That(h.Engine.Targets.ResultOf(0), Is.Null);
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(360), "las lecturas siguen llegando");
            }
        }

        [Test, Category("AC-WKT-01-06")]
        public void FinishSavesThenGoesIdle()
        {
            using (var h = new WorkoutHarness())
            {
                var slow = new ScriptedSessionRepository(h.Scheduler) { FinishDelay = TimeSpan.FromSeconds(1) };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, slow, h.Scheduler, h.Lifecycle, h.Logger))
                using (var notices = new StateRecorder<WorkoutNotice>(engine.Notices))
                {
                    h.At(0);
                    engine.StartFree();
                    h.At(12.3);
                    var finish = engine.FinishAsync();
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished), "terminada, el reloj detenido");
                    Assert.That(engine.State.Value.IsSaving, Is.True);
                    h.At(13);
                    Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(12), "no cuenta más");
                    Assert.That(finish.IsCompleted, Is.False);
                    h.At(13.3);
                    Assert.That(finish.IsCompleted, Is.True);
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle), "cuando el guardado termina, vuelve a reposo");
                    Assert.That(notices.Values.Count, Is.EqualTo(1));
                    Assert.That(notices.Values[0].ToString(), Is.EqualTo("Ended(Manual, true)"));
                }
            }
        }

        [Test, Category("AC-WKT-01-07")]
        public void IdleKeepsTheLastReadingAndTheLastFinished()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.HeartRate.EmitBpm(133);
                h.At(31);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(0));
                Assert.That(h.State.Step.HasValue, Is.False);
                Assert.That(h.State.IsRoutine, Is.False);
                Assert.That(h.Engine.Steps, Is.Null);
                Assert.That(h.Engine.Positions, Is.Null);
                Assert.That(h.Engine.Targets.ResultOf(0), Is.Null, "los objetivos se reinician");
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(331), "la última lectura queda");
                Assert.That(h.State.Data.HeartRate, Is.EqualTo(133), "el pulso del sensor queda");
                Assert.That(h.Engine.LastFinishedSessionId, Is.EqualTo(1));
                h.Engine.StartFree();
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(331), "al iniciar otra, el HUD muestra los últimos valores");
            }
        }

        [Test, Category("AC-WKT-01-08")]
        public void NoUndoAndStartOnlyFromIdle()
        {
            using (var h = new WorkoutHarness())
            {
                var slow = new ScriptedSessionRepository(h.Scheduler) { FinishDelay = TimeSpan.FromSeconds(3) };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, slow, h.Scheduler, h.Lifecycle, h.Logger))
                {
                    engine.Pause();
                    engine.Resume();
                    Assert.That(engine.FinishAsync().IsCompleted, Is.True, "en reposo, terminar no hace nada");
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                    h.At(0);
                    Assert.That(engine.StartFree(), Is.EqualTo(StartOutcome.Started));
                    Assert.That(engine.StartFree(), Is.EqualTo(StartOutcome.Busy), "en marcha, ni sesión nueva ni cambio");
                    Assert.That(engine.StartWithRoutine(RoutineSeeds.Corta()), Is.EqualTo(StartOutcome.Busy));
                    h.At(4);
                    var first = engine.FinishAsync();
                    var second = engine.FinishAsync();
                    Assert.That(second, Is.SameAs(first), "dos Terminar seguidos cierran una vez");
                    engine.Resume();
                    engine.Pause();
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished), "sin deshacer terminar");
                    Assert.That(engine.StartFree(), Is.EqualTo(StartOutcome.Busy));
                    h.At(8);
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                    Assert.That(slow.Calls, Is.EqualTo(new[] { "create", "finish:1" }), "una sesión y un cierre");
                }
            }
        }

        [Test, Category("AC-WKT-01-11")]
        public void CreateFailureStillRuns()
        {
            // DV-29: con el alta que falla el entrenamiento anda igual (el reloj, los pasos y el semáforo), avisa una vez y no
            // guarda nada; al terminar vuelve a reposo con el mismo aviso.
            using (var h = new WorkoutHarness())
            {
                var failing = new ScriptedSessionRepository(h.Scheduler) { FailOnCreate = true };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, failing, h.Scheduler, h.Lifecycle, h.Logger))
                using (var notices = new StateRecorder<WorkoutNotice>(engine.Notices))
                {
                    h.At(0);
                    engine.StartWithRoutine(RoutineSeeds.Corta());
                    Assert.That(notices.Values.Count, Is.EqualTo(1));
                    Assert.That(notices.Values[0].Kind, Is.EqualTo(WorkoutNoticeKind.SaveFailed));
                    h.At(12);
                    Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(12), "el reloj arranca");
                    Assert.That(engine.State.Value.Step!.Value.StepIndex, Is.EqualTo(1), "los pasos avanzan");
                    Assert.That(engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met), "los objetivos se evalúan");
                    h.At(31);
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                    Assert.That(notices.Values[notices.Count - 1].ToString(), Is.EqualTo("Ended(Completed, false)"));
                    Assert.That(failing.Calls, Is.EqualTo(new[] { "create" }), "ni puntos ni cierre");
                    Assert.That(failing.Store.Sessions, Is.Empty);
                    Assert.That(engine.LastFinishedSessionId, Is.Null);
                }
            }
        }

        [Test, Category("AC-WKT-02-02")]
        public void SecondOrderIsTheLegacyOrder()
        {
            // (1) relojes y muestreo; (2) el punto con el paso en curso; (3) objetivos y fin del paso; (4) el aviso. En Corta el
            // punto del segundo 10 es del trabajo que termina ahí y el aviso de ese segundo ya trae el descanso.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                var seen = new List<string>();
                using (h.Engine.State.Subscribe(s => seen.Add(Fmt(s)), false))
                {
                    h.At(10);
                }

                Assert.That(h.Points()[1].StepIndex, Is.EqualTo(0), "el punto pertenece al paso que termina");
                Assert.That(h.Points()[1].StepTypeName, Is.EqualTo("work"));
                Assert.That(h.Engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met), "se cerró la evaluación del paso");
                Assert.That(seen[seen.Count - 1], Is.EqualTo("10/1/0"), "el aviso del segundo 10, con el paso siguiente");
            }
        }

        [Test, Category("AC-WKT-02-03")]
        public void HudClockIsTheActiveClock()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(12);
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(12), "00:12 en el HUD");
                Assert.That(WorkoutFormats.SessionClock(h.State.TotalElapsedSeconds), Is.EqualTo("00:12"));
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(312), "el del monitor va a los puntos");
            }
        }

        [Test, Category("AC-WKT-03-05")]
        public void LastStepFinishesAndSaves()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                var phases = new List<WorkoutPhase>();
                using (h.Engine.State.Subscribe(s => phases.Add(s.Phase), false))
                {
                    h.At(30);
                }

                Assert.That(phases, Does.Contain(WorkoutPhase.Finished), "terminada en el segundo 30");
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle), "con la base al día, el cierre vuelve en el acto");
                Assert.That(h.Notices.Values[h.Notices.Count - 1].ToString(), Is.EqualTo("Ended(Completed, true)"));
                var session = h.Session()!;
                Assert.That(session.FinishedAt, Is.EqualTo(WorkoutHarness.SessionStart.AddSeconds(30)));
                h.At(40);
                Assert.That(h.Points().Count, Is.EqualTo(7), "ningún otro segundo vuelve a evaluar");
            }
        }

        [Test, Category("AC-WKT-06-10")]
        public void ReopenDoesNotTouchACutSession()
        {
            // Una sesión cortada (la app se cerró) queda con sus puntos y sin fin; al volver a abrir, el motor nuevo arranca en
            // reposo y no la toca.
            var memory = new InMemorySessionRepository();
            using (var h = new WorkoutHarness(sessions: memory))
            {
                h.Free();
                h.At(31.5);
                h.Engine.Dispose();
            }

            var operations = memory.Operations.Count;
            using (var reopened = new WorkoutHarness(sessions: memory))
            {
                reopened.At(10);
                Assert.That(reopened.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(memory.Operations.Count, Is.EqualTo(operations), "no la modifica ni ofrece retomarla");
                Assert.That(memory.Session(1)!.FinishedAt, Is.Null);
                Assert.That(memory.PointsOf(1).Count, Is.EqualTo(6));
            }
        }

        [Test, Category("AC-WKT-06-11")]
        public void LastFinishedOnlyWhenTheCloseSucceeds()
        {
            using (var h = new WorkoutHarness())
            {
                var scripted = new ScriptedSessionRepository(h.Scheduler);
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, scripted, h.Scheduler, h.Lifecycle, h.Logger))
                {
                    h.At(0);
                    engine.StartFree();
                    h.At(3);
                    engine.FinishAsync();
                    Assert.That(engine.LastFinishedSessionId, Is.EqualTo(1));
                    scripted.FailOnFinish = true;
                    engine.StartFree();
                    h.At(6);
                    engine.FinishAsync();
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                    Assert.That(engine.LastFinishedSessionId, Is.EqualTo(1), "si falló, sigue la anterior");
                }
            }
        }

        [Test, Category("AC-WKT-06-11"), Category("AC-WKT-06-12")]
        public void LateCloseStillBecomesTheLastFinished()
        {
            // DT4a-13: un cierre que no vuelve en 15 s de primer plano lleva a reposo con saved = false; si después vuelve
            // bien, la sesión pasa a ser la última terminada y el aviso que ya salió no cambia. Nunca queda colgado.
            using (var h = new WorkoutHarness())
            {
                var slow = new ScriptedSessionRepository(h.Scheduler) { FinishDelay = TimeSpan.FromSeconds(20) };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, slow, h.Scheduler, h.Lifecycle, h.Logger))
                using (var notices = new StateRecorder<WorkoutNotice>(engine.Notices))
                {
                    h.At(0);
                    engine.StartFree();
                    h.At(5);
                    engine.FinishAsync();
                    h.At(19.99);
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished));
                    h.At(20);
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle), "a los 15 s de primer plano");
                    Assert.That(notices.Values[notices.Count - 1].ToString(), Is.EqualTo("Ended(Manual, false)"));
                    Assert.That(engine.LastFinishedSessionId, Is.Null);
                    Assert.That(h.Logger.Lines, Has.Some.EqualTo("[WORKOUT] Warning: cierre sin respuesta"));
                    h.At(25);
                    Assert.That(engine.LastFinishedSessionId, Is.EqualTo(1), "el cierre tardío");
                    Assert.That(h.Logger.Lines, Has.Some.EqualTo("[WORKOUT] Info: cierre tardío"));
                    Assert.That(notices.Count, Is.EqualTo(1), "el aviso que ya salió no cambia");
                    Assert.That(slow.Store.Session(1)!.FinishedAt.HasValue, Is.True);
                }
            }
        }

        [Test, Category("AC-BLE-10-07"), Category("AC-WKT-03-10")]
        public void EngineIgnoresTheConnection()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                var count = h.Notices.Count;
                h.At(3);
                h.Rower.Disconnect();
                h.At(8);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Active), "no cambia de fase");
                Assert.That(h.Notices.Count, Is.EqualTo(count), "ni avisa");
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(8), "el reloj sigue");
                Assert.That(h.Points()[0].ElapsedSeconds, Is.EqualTo(303), "el punto repite la última lectura");
                h.Rower.Connect();
                h.At(10.6);
                Assert.That(h.Points()[1].ElapsedSeconds, Is.EqualTo(310), "vuelve con las lecturas nuevas");
                h.Rower.SetStatus(BleStatus.Off);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Active));
            }
        }

        [Test, Category("AC-WKT-02-08")]
        public void OneSnapshotPerReturn()
        {
            // QA-G4aT-07: en segundo plano el motor no publica fotos; al volver publica UNA, con Instant.
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(3);
                var snapshots = new List<WorkoutSnapshot>();
                using (h.Engine.State.Subscribe(snapshots.Add, false))
                {
                    h.Lifecycle.SetBackground(true);
                    h.At(120);
                    Assert.That(snapshots, Is.Empty, "ni una foto en segundo plano");
                    h.Lifecycle.SetBackground(false);
                    Assert.That(snapshots.Count, Is.EqualTo(1));
                    Assert.That(snapshots[0].Instant, Is.True);
                    Assert.That(snapshots[0].TotalElapsedSeconds, Is.EqualTo(120), "el presente");
                    h.At(121);
                    Assert.That(snapshots[snapshots.Count - 1].Instant, Is.False, "después, las fotos de siempre");
                }
            }
        }

        [Test, Category("AC-WKT-06-13")]
        public void SlowDatabaseDoesNotDelaySeconds()
        {
            using (var h = new WorkoutHarness())
            {
                var slow = new ScriptedSessionRepository(h.Scheduler)
                {
                    CreateDelay = TimeSpan.FromSeconds(2), WriteDelay = TimeSpan.FromSeconds(2), FinishDelay = TimeSpan.FromSeconds(2),
                };
                using (var engine = new WorkoutEngine(h.Rower, h.HeartRate, slow, h.Scheduler, h.Lifecycle, h.Logger))
                {
                    h.At(0);
                    engine.StartWithRoutine(RoutineSeeds.Corta());
                    for (var second = 1; second <= 29; second++)
                    {
                        h.At(second + 0.1);
                        Assert.That(engine.State.Value.TotalElapsedSeconds, Is.EqualTo(second), "al día en el segundo " + second);
                    }

                    h.At(60);
                    Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                    var calls = slow.Calls;
                    Assert.That(calls[0], Is.EqualTo("create"));
                    Assert.That(calls[calls.Count - 1], Does.StartWith("finish:"), "el cierre después del último lote");
                    var points = slow.Store.PointsOf(1);
                    Assert.That(points.Count, Is.EqualTo(7));
                    for (var i = 1; i < points.Count; i++)
                    {
                        Assert.That(points[i].ElapsedSeconds, Is.GreaterThanOrEqualTo(points[i - 1].ElapsedSeconds), "en el orden en que se tomaron");
                    }
                }
            }
        }

        private static string Fmt(WorkoutSnapshot s)
        {
            return s.TotalElapsedSeconds + "/" + (s.Step.HasValue ? s.Step.Value.StepIndex.ToString() : "-") + "/" +
                (s.Step.HasValue ? s.Step.Value.ElapsedInStep.ToString() : "-");
        }
    }
}
