using System;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // CHT: el fin de la rutina espera al diálogo (DT4a-15; 02-tech-design §2.6 de la It. 4b): con la rutina terminada SOLA el
    // motor queda en Finished aunque el cierre haya vuelto, hasta AcknowledgeCompletion() (el Terminar del diálogo = el reset()
    // del legacy, immersive_workout_screen.dart:261-289). El terminar a mano no cambia. Con el motor de verdad, R-4 y la base en
    // memoria o de guion (sus demoras y fallas), y con SQLite real en el segundo plano largo.
    [TestFixture]
    public sealed class CompletionHoldTests
    {
        [Test, Category("AC-WKT-21-01"), Category("AC-WKT-03-05"), Category("AC-WKT-01-06")]
        public void RoutineEndHolds()
        {
            // Corta con R-4 termina sola a los 30 s: Finished con EndedBy = Completed y la sesión; el cierre vuelve en el acto
            // (Close = Saved) y el motor NO vuelve solo a reposo; ningún aviso Ended hasta el Terminar del diálogo.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                Assert.That(h.State.Session, Is.EqualTo(1));
                Assert.That(h.State.EndedBy, Is.Null);
                h.At(30);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.EndedBy, Is.EqualTo(WorkoutEndKind.Completed));
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Saved));
                Assert.That(h.State.IsSaving, Is.False, "el cierre volvió");
                Assert.That(h.Engine.CloseState, Is.EqualTo(CloseStatus.Saved));
                h.At(45);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished), "espera al diálogo");
                Assert.That(h.Notices.Count, Is.EqualTo(0), "ningún aviso todavía");
                Assert.That(h.Engine.StartFree(), Is.EqualTo(StartOutcome.Busy), "una sesión a la vez (AC-WKT-01-08)");
                var ack = h.Engine.AcknowledgeCompletion();
                Assert.That(ack.IsCompleted, Is.True, "con el cierre ya vuelto, en el acto");
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.State.EndedBy, Is.Null);
                Assert.That(h.State.Session, Is.EqualTo(1), "el reposo conserva la sesión");
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Completed, true)"));
                Assert.That(h.Engine.AcknowledgeCompletion().IsCompleted, Is.True, "fuera de ese estado, sin efecto");
                Assert.That(h.Notices.Count, Is.EqualTo(1), "idempotente");
                Assert.That(h.Engine.StartFree(), Is.EqualTo(StartOutcome.Started));
                Assert.That(h.State.Session, Is.EqualTo(2), "una sesión nueva sube la generación");
            }
        }

        [Test, Category("AC-WKT-21-04")]
        public void TotalsAreThoseOfTheFinalPoint()
        {
            // DV-43: los totales del diálogo son los del punto final (el reloj activo, la distancia y las kcal de la lectura de ese
            // momento: workout_provider.dart:259-273), fijos aunque lleguen lecturas.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(30);
                var completion = h.State.Completion!.Value;
                Assert.That(completion.ActiveSeconds, Is.EqualTo(30));
                Assert.That(completion.DistanceMeters, Is.EqualTo(1120), "L(30) = 1000 + 4·30");
                Assert.That(completion.Calories, Is.EqualTo(43), "40 + ⌊30 / 10⌋");
                var final = h.Points()[h.Points().Count - 1];
                Assert.That(completion.DistanceMeters, Is.EqualTo(final.DistanceMeters), "el punto final");
                Assert.That(completion.Calories, Is.EqualTo(final.Calories));
                h.At(80);
                Assert.That(h.State.Data.DistanceMeters, Is.GreaterThan(1120), "las lecturas siguen llegando");
                Assert.That(h.State.Completion!.Value, Is.EqualTo(completion), "los totales no cambian");
            }
        }

        [Test, Category("AC-WKT-21-07")]
        public void ManualNeverHasCompletion()
        {
            // Q-53: al terminar a mano nunca hay diálogo de completado: EndedBy = Manual, sin totales, y vuelve solo a reposo
            // cuando vuelve el cierre (AC-WKT-01-06).
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(2) }))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(12);
                h.Engine.FinishAsync();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.EndedBy, Is.EqualTo(WorkoutEndKind.Manual));
                Assert.That(h.State.Completion, Is.Null);
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Pending));
                Assert.That(h.Engine.AcknowledgeCompletion().IsCompleted, Is.True, "a mano no hay nada que confirmar");
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                h.At(14.1);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle), "vuelve solo");
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Manual, true)"));
            }
        }

        [Test, Category("AC-WKT-01-06")]
        public void ManualFinishStillReturnsAlone()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(20);
                h.Engine.FinishAsync();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle), "con la base al día, en el acto");
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Manual, true)"));
            }
        }

        [Test, Category("AC-WKT-21-06")]
        public void AcknowledgeWaitsForASlowClose()
        {
            // DV-42: tocar rápido nunca pierde el cierre. Terminar marca la intención; la vuelta a reposo espera el resultado.
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(5) }))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(30.3);
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Pending));
                var ack = h.Engine.AcknowledgeCompletion();
                Assert.That(ack.IsCompleted, Is.False);
                Assert.That(h.Engine.AcknowledgeCompletion(), Is.SameAs(ack), "idempotente");
                h.At(34.9);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                h.At(35.1);
                Assert.That(ack.IsCompleted, Is.True);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.Notices.Values[h.Notices.Count - 1].ToString(), Is.EqualTo("Ended(Completed, true)"));
                Assert.That(h.Engine.LastFinishedSessionId, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-21-06"), Category("AC-WKT-06-12")]
        public void FailedClose()
        {
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FailOnFinish = true }))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(31);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Failed));
                Assert.That(h.Notices.Count, Is.EqualTo(0), "el aviso, después del Terminar");
                h.Engine.AcknowledgeCompletion();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Completed, false)"));
            }
        }

        [Test, Category("AC-WKT-21-06")]
        public void CloseTimeout()
        {
            // El tope de 15 s de primer plano desde el fin (DT4a-13, DT4b-09): Close = Failed y sigue terminada; con Terminar antes
            // del tope, vuelve a reposo en el tope con Ended(Completed, false).
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(100) }))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(44.9);
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Pending));
                h.At(45.1);
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Failed), "el tope");
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                h.Engine.AcknowledgeCompletion();
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Completed, false)"));
            }

            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(100) }))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(31);
                var ack = h.Engine.AcknowledgeCompletion();
                h.At(44.9);
                Assert.That(ack.IsCompleted, Is.False);
                h.At(45.1);
                Assert.That(ack.IsCompleted, Is.True, "Terminar antes del tope: el reposo en el tope");
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Completed, false)"));
            }
        }

        [Test, Category("AC-WKT-21-06")]
        public void LateGoodCloseBeforeAcknowledgeCountsAsSaved()
        {
            // AC-WKT-21-06: si después del tope el cierre vuelve bien y todavía no se tocó Terminar, Close = Saved y el final es el
            // de un guardado bueno.
            using (var h = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(20) }))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(46);
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Failed));
                h.At(51);
                Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Saved), "el cierre tardío volvió bien");
                Assert.That(h.Engine.LastFinishedSessionId, Is.EqualTo(1));
                h.Engine.AcknowledgeCompletion();
                Assert.That(h.Notices.Values[0].ToString(), Is.EqualTo("Ended(Completed, true)"));
            }
        }

        [Test, Category("AC-WKT-21-06")]
        public void QuitWithTheDialogOpen()
        {
            // La app se cierra con el diálogo abierto: Dispose hace lo de la 4a (Abandon, QA-V4a-ROB-04). Con el cierre ya vuelto
            // la sesión quedó cerrada; nadie queda esperando el reposo.
            var h = new WorkoutHarness();
            h.Routine(RoutineSeeds.Corta());
            h.At(31);
            Assert.That(h.State.Close, Is.EqualTo(CloseStatus.Saved));
            h.Dispose();
            Assert.That(h.Session()!.FinishedAt, Is.EqualTo(WorkoutHarness.SessionStart.AddSeconds(30)), "cerrada");
            Assert.That(h.Engine.AcknowledgeCompletion().IsCompleted, Is.True);

            var slow = new WorkoutHarness(sessionsFor: s => new ScriptedSessionRepository(s) { FinishDelay = TimeSpan.FromSeconds(5) });
            slow.Routine(RoutineSeeds.Corta());
            slow.At(31);
            var ack = slow.Engine.AcknowledgeCompletion();
            Assert.That(ack.IsCompleted, Is.False);
            slow.Dispose();
            Assert.That(ack.IsCompleted, Is.True, "Dispose no deja a nadie esperando");
        }

        [Test, Category("AC-WKT-21-08")]
        public void NoPointsAfterTheEnd()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(30);
                var points = h.Points().Count;
                Assert.That(points, Is.EqualTo(7), "6 de 5 en 5 y el final");
                h.At(90);
                Assert.That(h.Points().Count, Is.EqualTo(points), "ningún punto más");
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(30), "el reloj quieto");
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(3), "el índice en el último");
                Assert.That(h.Engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met), "los resultados de los objetivos en su lugar");
                Assert.That(h.Engine.Targets.ResultOf(2), Is.EqualTo(StepResult.Met));
            }
        }

        [Test, Category("AC-WKT-21-08")]
        public void LiveReadingsKeepPublishing()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(30.2);
                var revision = h.State.Revision;
                var states = new StateRecorder<WorkoutSnapshot>(h.Engine.State);
                h.At(40.2);
                Assert.That(states.Count, Is.EqualTo(10), "una foto por lectura (L(31) a L(40))");
                Assert.That(h.State.Revision, Is.GreaterThan(revision));
                Assert.That(h.State.Data.ElapsedSeconds, Is.EqualTo(340), "la lectura viva");
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                states.Dispose();
            }
        }

        [Test, Category("AC-WKT-21-08")]
        public void TheHudFinishAfterTheEndChangesNothing()
        {
            // Un Terminar del HUD que llega después del fin no cambia nada (AC-WKT-20-07), ni Pausa ni Reanudar.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(30);
                var close = h.Engine.FinishTask;
                Assert.That(h.Engine.FinishAsync(), Is.SameAs(close));
                h.Engine.Pause();
                h.Engine.Resume();
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.EndedBy, Is.EqualTo(WorkoutEndKind.Completed));
            }
        }

        [Test, Category("AC-WKT-21-09"), Category("AC-WKT-02-06")]
        public void EndsInBackground()
        {
            // Corta en segundo plano de los 8 a los 40 s: al volver, terminada con los totales del segundo 30, el cierre ya vuelto
            // y UNA foto con Instant (el diálogo se abre sin entrada, AC-WKT-21-09).
            var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
            var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
            var sessions = new InMemorySessionRepository();
            using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), sessions, harness.Scheduler, harness.Lifecycle,
                       harness.Logger))
            {
                rower.Start();
                harness.RunUntil(0);
                engine.StartWithRoutine(RoutineSeeds.Corta());
                harness.Background(8);
                var states = new StateRecorder<WorkoutSnapshot>(engine.State);
                harness.Foreground(40);
                Assert.That(states.Count, Is.EqualTo(1), "una sola foto al volver");
                var state = states.Last;
                Assert.That(state.Instant, Is.True);
                Assert.That(state.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(state.EndedBy, Is.EqualTo(WorkoutEndKind.Completed));
                Assert.That(state.Close, Is.EqualTo(CloseStatus.Saved));
                Assert.That(state.Completion!.Value.ActiveSeconds, Is.EqualTo(30));
                Assert.That(state.Completion!.Value.DistanceMeters, Is.EqualTo(1120), "los del segundo 30, no los de la vuelta");
                Assert.That(sessions.Session(1)!.FinishedAt, Is.EqualTo(WorkoutHarness.SessionStart.AddSeconds(30)));
                states.Dispose();
            }

            rower.Dispose();
        }

        [Test, Category("AC-WKT-21-09"), Category("AC-WKT-02-06")]
        public void EndsInALongBackgroundWithSqlite()
        {
            // Corta con la pantalla bloqueada de los 8 a los 330 s, con SQLite real en el SerialExecutor y las continuaciones en el
            // hilo de la prueba: el tope del cierre no cuenta el segundo plano (QA-G4aT-03), así que al volver el diálogo está, el
            // cierre bueno y ningún workoutSaveError falso (AC-WKT-02-06).
            TestMainThreadContext.Run(async () =>
            {
                var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
                using (var temp = new TempDatabase())
                using (var store = temp.Store(harness.Logger, new SerialExecutor("RowMate.Db.Test", harness.Logger)))
                {
                    var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
                    using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), store.Sessions, harness.Scheduler,
                               harness.Lifecycle, harness.Logger))
                    using (var notices = new StateRecorder<WorkoutNotice>(engine.Notices))
                    {
                        await store.Sessions.ListAsync();
                        rower.Start();
                        harness.RunUntil(0);
                        engine.StartWithRoutine(RoutineSeeds.Corta());
                        await store.Sessions.ListAsync();
                        harness.Background(8);
                        harness.Foreground(330);
                        await engine.FinishTask;
                        harness.RunUntil(331);
                        Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Finished), "el diálogo espera");
                        Assert.That(engine.State.Value.Close, Is.EqualTo(CloseStatus.Saved));
                        Assert.That(notices.Count, Is.EqualTo(0), "ningún workoutSaveError");
                        await engine.AcknowledgeCompletion();
                        Assert.That(notices.Values[0].ToString(), Is.EqualTo("Ended(Completed, true)"));
                        var session = await store.Sessions.GetByIdAsync(1);
                        Assert.That(session!.FinishedAt!.Value.ToUniversalTime().TimeOfDay, Is.EqualTo(new TimeSpan(13, 0, 30)));
                        Assert.That((await store.Sessions.GetPointsAsync(1)).Count, Is.EqualTo(7));
                    }
                }
            });
        }
    }
}
