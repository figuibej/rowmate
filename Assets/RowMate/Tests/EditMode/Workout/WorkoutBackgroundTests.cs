using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WBG: el segundo plano (Q-91; ADR 017; 02-tech-design §4 de la It. 4a) con el ciclo de vida de prueba: el FrameScheduler de
    // la app a 60 fps y FakeAppLifecycle, con el orden de AppRoot (LifecycleHarness). Con las mismas lecturas y su hora, la
    // sesión da lo mismo haya o no segundo plano.
    [TestFixture]
    public sealed class WorkoutBackgroundTests
    {
        [Test, Category("AC-WKT-02-05")]
        public void BackgroundIsLikeForeground()
        {
            // Corta con R-4, segundo plano de los 8 a los 23 s: el reloj en 00:23, el paso 2 (el trabajo de la repetición 2) con
            // 0:02, por terminar y con Próximo: Descanso; los puntos de 5, 10, 15 y 20 con los mismos valores e índices.
            var foreground = RunCorta(null, null, 23);
            var background = RunCorta(8, 23, 23);
            Assert.That(background.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(23));
            var step = background.Engine.State.Value.Step!.Value;
            Assert.That(step.StepIndex, Is.EqualTo(2));
            Assert.That(WorkoutFormats.Countdown(step.RemainingInStep!.Value), Is.EqualTo("0:02"));
            Assert.That(step.IsEndingSoon, Is.True);
            Assert.That(step.IsUpNext, Is.True);
            Assert.That(step.NextStep!.Type, Is.EqualTo(StepType.Rest));
            var points = background.Sessions.PointsOf(1);
            Assert.That(WorkoutHarness.Describe(points), Is.EqualTo("305/1020/40/0/work 310/1040/41/0/work 315/1060/41/1/rest 320/1080/42/2/work"));
            Assert.That(WorkoutHarness.Describe(points), Is.EqualTo(WorkoutHarness.Describe(foreground.Sessions.PointsOf(1))), "igual que en primer plano");
            Assert.That(background.Engine.Targets.ResultOf(0), Is.EqualTo(foreground.Engine.Targets.ResultOf(0)));
            Assert.That(background.Engine.Targets.ResultOf(0), Is.EqualTo(StepResult.Met));
            background.Dispose();
            foreground.Dispose();
        }

        [Test, Category("AC-WKT-02-06")]
        public void RoutineEndsWhileInBackground()
        {
            // Vuelve a los 40 s (la rutina terminó a los 30): cerrada como en primer plano, con los 7 puntos y la hora de fin del
            // segundo 30, no la de la vuelta; al volver, en reposo con el aviso del fin.
            var run = RunCorta(8, 40, 40);
            var session = run.Sessions.Session(1)!;
            Assert.That(session.FinishedAt, Is.EqualTo(new DateTimeOffset(2026, 10, 28, 13, 0, 30, TimeSpan.Zero)), "2026-10-28T13:00:30.000Z");
            Assert.That(run.Sessions.PointsOf(1).Count, Is.EqualTo(7));
            Assert.That(run.Engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
            Assert.That(run.Notices.Values[run.Notices.Count - 1].ToString(), Is.EqualTo("Ended(Completed, true)"));
            Assert.That(run.Engine.LastFinishedSessionId, Is.EqualTo(1));
            run.Dispose();
        }

        [Test, Category("AC-WKT-02-06"), Category("AC-WKT-06-11")]
        public void LongBackgroundAfterTheEndStillSaves()
        {
            // QA-G4aT-03: Corta con la pantalla bloqueada de los 8 a los 330 s, con SQLite real en el SerialExecutor y las
            // continuaciones en el hilo de la prueba (TestMainThreadContext, como en Unity). La rutina termina en el segundo
            // virtual 30 dentro del alcance; el tope del cierre (de primer plano) arranca recién a la vuelta, así que el cierre
            // real gana: Ended(Completed, true), la última terminada puesta y ningún aviso de error.
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
                        // Detrás del alta (FIFO): cuando vuelve la lista, el alta volvió y el reloj abrió en el instante 0.
                        await store.Sessions.ListAsync();
                        harness.Background(8);
                        harness.Foreground(330);
                        await engine.FinishTask;
                        Assert.That(engine.State.Value.Phase, Is.EqualTo(WorkoutPhase.Idle));
                        Assert.That(notices.Values[notices.Count - 1].ToString(), Is.EqualTo("Ended(Completed, true)"));
                        foreach (var notice in notices.Values)
                        {
                            Assert.That(notice.Kind, Is.EqualTo(WorkoutNoticeKind.Ended), "ningún workoutSaveError");
                        }

                        Assert.That(engine.LastFinishedSessionId, Is.EqualTo(1));
                        var session = await store.Sessions.GetByIdAsync(1);
                        Assert.That(session!.FinishedAt!.Value.ToUniversalTime().TimeOfDay, Is.EqualTo(new TimeSpan(13, 0, 30)), "la hora del segundo 30");
                        Assert.That((await store.Sessions.GetPointsAsync(1)).Count, Is.EqualTo(7));
                    }
                }
            });
        }

        [Test, Category("AC-WKT-02-07")]
        public void PausedSessionDoesNotMoveInBackground()
        {
            var run = new EngineRun(RowerScripts.R4);
            run.Harness.RunUntil(0);
            run.Engine.StartWithRoutine(RoutineSeeds.Corta());
            run.Harness.RunUntil(7.5);
            run.Engine.Pause();
            var before = run.Engine.State.Value;
            var points = run.Sessions.AllPoints.Count;
            run.Harness.Background(8);
            run.Harness.Foreground(68);
            var after = run.Engine.State.Value;
            Assert.That(after.Phase, Is.EqualTo(WorkoutPhase.Paused));
            Assert.That(after.TotalElapsedSeconds, Is.EqualTo(before.TotalElapsedSeconds), "el reloj quieto");
            Assert.That(after.Step!.Value.StepIndex, Is.EqualTo(before.Step!.Value.StepIndex), "el paso quieto");
            Assert.That(after.Step.Value.ElapsedInStep, Is.EqualTo(before.Step.Value.ElapsedInStep));
            Assert.That(run.Sessions.AllPoints.Count, Is.EqualTo(points), "sin puntos nuevos");
            Assert.That(after.Data.ElapsedSeconds, Is.EqualTo(368), "las lecturas llegaron en el alcance");
            run.Dispose();
        }

        [Test, Category("AC-WKT-06-15")]
        public void BackgroundFlushesPendingWrites()
        {
            // Con un lote en vuelo (la base ocupada) y otro punto pendiente, al pasar a segundo plano se manda lo pendiente y se
            // espera la base, a lo sumo 1 s: después, desde OTRA conexión, están los dos puntos.
            TestMainThreadContext.Run(async () =>
            {
                var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
                using (var temp = new TempDatabase())
                {
                    var executor = new SerialExecutor("RowMate.Db.Test", harness.Logger);
                    using (var store = temp.Store(harness.Logger, executor))
                    {
                        await store.Sessions.ListAsync();
                        var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
                        using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), store.Sessions, harness.Scheduler,
                                   harness.Lifecycle, harness.Logger))
                        using (var gate = new ManualResetEventSlim(false))
                        {
                            rower.Start();
                            harness.RunUntil(0);
                            engine.StartFree();
                            await store.Sessions.ListAsync();

                            // La base queda ocupada 300 ms desde el segundo 4: el punto del 5 queda en vuelo y el del 10, pendiente.
                            harness.RunUntil(4.2);
                            var busy = executor.Run(() => gate.Wait(TimeSpan.FromSeconds(5)));
                            harness.RunUntil(10.2);
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                Thread.Sleep(300);
                                gate.Set();
                            });
                            var watch = Stopwatch.StartNew();
                            harness.Background(10.5);
                            watch.Stop();
                            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1.5)), "a lo sumo 1 s (más el margen de la PC)");
                            await busy;
                            using (var other = temp.Open(harness.Logger))
                            {
                                var rows = other.QueryRawForTestsAsync("SELECT elapsed_seconds FROM data_points ORDER BY id").Result;
                                Assert.That(rows.Count, Is.EqualTo(2), "los dos puntos, escritos antes de congelarse");
                                Assert.That(rows[1][0], Is.EqualTo(310L));
                            }
                        }
                    }
                }
            });
        }

        [Test, Category("AC-WKT-06-15")]
        public void BackgroundFlushAlsoSendsTheFailedBatchAgain()
        {
            // QA-V4a-ROB-03 (05-qa-lens-robustez, L03): el lote del segundo 5 falla una vez y el del 10 entra. Al pasar a segundo
            // plano se manda "todo lo pendiente" (AC-WKT-06-15), también el punto del lote que falló, como al cerrar la app
            // (Abandon): si el sistema la cierra en segundo plano, ese punto ya está escrito aunque la sesión quede cortada.
            ScriptedSessionRepository? scripted = null;
            using (var h = new WorkoutHarness(sessionsFor: s => scripted = new ScriptedSessionRepository(s) { FailBatch = 1 }))
            {
                h.Free();
                h.At(10.5);
                Assert.That(h.Notices.Values.Count, Is.EqualTo(1), "el aviso de la falla, una vez");
                Assert.That(scripted!.Store.AllPoints.Count, Is.EqualTo(1), "antes de irse, solo el del 10");
                h.Lifecycle.SetBackground(true);
                var written = WorkoutHarness.Describe(scripted.Store.AllPoints);
                Assert.That(scripted.Store.AllPoints.Count, Is.EqualTo(2), "al congelarse están los dos: " + written);
                Assert.That(written, Does.Contain("305/").And.Contains("310/"), "el del 10 y, otra vez, el del 5");
                h.Lifecycle.SetBackground(false);
                h.At(12.3);
                h.Engine.FinishAsync();
                Assert.That(scripted.Store.Session(1)!.FinishedAt, Is.Not.Null, "cerrada");
                Assert.That(scripted.Store.AllPoints.Count, Is.EqualTo(3), "el del 5 una sola vez: el cierre ya no lo repite");
            }
        }

        [Test, Category("AC-WKT-06-15"), Category("AC-WKT-06-12")]
        public void FinishThenBackgroundWithABatchInFlightStillCloses()
        {
            // QA-V4a-ROB-04 (05-qa-lens-robustez, L04): Terminar confirmado con un lote en vuelo (la base ocupada desde los 4,2 s)
            // y segundo plano enseguida. El cierre esperaba la vuelta del lote EN EL HILO PRINCIPAL, que en segundo plano ya no
            // corre: la sesión quedaba cortada aunque el usuario había confirmado. Ahora FlushAndWait espera la base (la mitad del
            // tope), resuelve ahí el lote que volvió y encola el cierre; el total sigue acotado por el tope de 1 s.
            string? finishedAt = "sin leer";
            long points = -1;
            TestMainThreadContext.Run(async () =>
            {
                var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
                using (var temp = new TempDatabase())
                {
                    var executor = new SerialExecutor("RowMate.Db.Test", harness.Logger);
                    using (var store = temp.Store(harness.Logger, executor))
                    {
                        await store.Sessions.ListAsync();
                        var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
                        using (var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), store.Sessions, harness.Scheduler,
                                   harness.Lifecycle, harness.Logger))
                        using (var gate = new ManualResetEventSlim(false))
                        {
                            rower.Start();
                            harness.RunUntil(0);
                            engine.StartFree();
                            await store.Sessions.ListAsync();
                            harness.RunUntil(4.2);
                            var busy = executor.Run(() => gate.Wait(TimeSpan.FromSeconds(5)));
                            harness.RunUntil(5.5);
                            var finish = engine.FinishAsync();
                            Assert.That(engine.Phase, Is.EqualTo(WorkoutPhase.Finished));
                            Assert.That(finish.IsCompleted, Is.False, "el cierre espera el lote del segundo 5");
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                Thread.Sleep(100);
                                gate.Set();
                            });
                            var watch = Stopwatch.StartNew();
                            harness.Background(5.6);
                            watch.Stop();
                            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(1.5)), "a lo sumo el tope de 1 s (más el margen de la PC)");
                            // Congelada: desde acá el hilo principal no corre más (ninguna continuación pendiente). Lo que quede es lo
                            // que la base ya escribió; se lee desde otra conexión, en línea, antes de soltar el hilo.
                            using (var other = temp.Open(harness.Logger))
                            {
                                var query = other.QueryRawForTestsAsync("SELECT finished_at, (SELECT count(*) FROM data_points) FROM workout_sessions WHERE id = 1");
                                Assert.That(query.IsCompleted, Is.True, "otra conexión, en línea");
                                finishedAt = query.Result[0][0] as string;
                                points = (long)query.Result[0][1]!;
                            }

                            await busy;
                            // Al volver, el cierre que ya salió vuelve una sola vez.
                            harness.Foreground(6);
                            await finish;
                            Assert.That(engine.Phase, Is.EqualTo(WorkoutPhase.Idle));
                            Assert.That(engine.LastFinishedSessionId, Is.EqualTo(1));
                        }
                    }
                }
            });

            Assert.That(finishedAt, Is.Not.Null, "Terminar confirmado antes de irse: cerrada antes de congelarse");
            Assert.That(points, Is.EqualTo(2), "el punto del 5 y el final");
        }

        [Test, Category("AC-WKT-06-10"), Category("AC-WKT-06-12")]
        public void QuitRightAfterFinishKeepsTheClose()
        {
            // QA-V4a-ROB-04 (05-qa-lens-robustez, L08 y L08b): cerrar la ventana en el mismo cuadro de Terminar (AppRoot.OnDestroy:
            // el motor y después LocalStore.Dispose, que termina lo encolado). Con la base al día el cierre ya estaba encolado; con
            // el punto del 10 en vuelo, Abandon espera la base con tope, resuelve el lote y encola el cierre antes de que se cierre
            // la base. Sin Terminar, sigue quedando cortada (P-03, AbandonSendsWhatIsPendingAndDoesNotClose).
            foreach (var withBatchInFlight in new[] { false, true })
            {
                var label = withBatchInFlight ? "con un lote en vuelo: " : "con la base al día: ";
                string? finishedAt = null;
                long points = -1;
                TestMainThreadContext.Run(async () =>
                {
                    var harness = new LifecycleHarness(WorkoutHarness.SessionStart);
                    using (var temp = new TempDatabase())
                    {
                        var executor = new SerialExecutor("RowMate.Db.Test", harness.Logger);
                        var store = temp.Store(harness.Logger, executor);
                        await store.Sessions.ListAsync();
                        var rower = new ScriptedRower(harness.Scheduler, RowerScripts.R4, harness.Logger);
                        var engine = new WorkoutEngine(rower, new ScriptedHeartRateService(harness.Logger), store.Sessions, harness.Scheduler,
                            harness.Lifecycle, harness.Logger);
                        using (var gate = new ManualResetEventSlim(false))
                        {
                            rower.Start();
                            harness.RunUntil(0);
                            engine.StartFree();
                            await store.Sessions.ListAsync();
                            harness.RunUntil(9.8);
                            await store.Sessions.ListAsync(); // el punto del 5, escrito y vuelto
                            if (withBatchInFlight)
                            {
                                _ = executor.Run(() => gate.Wait(TimeSpan.FromSeconds(5))); // el punto del 10 queda en vuelo
                            }

                            harness.RunUntil(12.3);
                            if (!withBatchInFlight)
                            {
                                await store.Sessions.ListAsync(); // el punto del 10, escrito y vuelto
                            }

                            _ = engine.FinishAsync();
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                Thread.Sleep(100);
                                gate.Set();
                            });
                            engine.Dispose();
                            rower.Dispose();
                            store.Dispose();
                        }

                        using (var other = temp.Open(harness.Logger))
                        {
                            var rows = other.QueryRawForTestsAsync("SELECT finished_at, (SELECT count(*) FROM data_points) FROM workout_sessions WHERE id = 1").Result;
                            finishedAt = rows[0][0] as string;
                            points = (long)rows[0][1]!;
                        }
                    }
                });

                Assert.That(finishedAt, Is.Not.Null, label + "Terminar confirmado antes de cerrar la ventana: cerrada");
                Assert.That(points, Is.EqualTo(3), label + "los puntos del 5 y del 10 y el final");
            }
        }

        [Test, Category("AC-WKT-02-08")]
        public void EightHourCatchUpIsBounded()
        {
            // R4a-13 (QA-G4aT-07): una sesión libre olvidada 8 h en segundo plano (≈ 57 600 segundos y lecturas) se pone al día en
            // el primer cuadro de vuelta con UNA foto y en tiempo acotado (en la PC: 8 h en menos de 500 ms y 32 MB; 1 h en menos
            // de 100 ms). El valor medido va al registro de la prueba (lo copia el informe de WP-A).
            foreach (var hours in new[] { 1, 8 })
            {
                var run = new EngineRun(RowerScripts.R4);
                run.Harness.RunUntil(0);
                run.Engine.StartFree();
                run.Harness.RunUntil(2);
                run.Harness.Background(3);
                var snapshots = 0;
                using (run.Engine.State.Subscribe(_ => snapshots++, false))
                {
                    GC.Collect();
                    var allocatedBefore = AllocatedBytes.Now();
                    var watch = Stopwatch.StartNew();
                    run.Harness.Foreground(3 + hours * 3600);
                    watch.Stop();
                    var allocated = AllocatedBytes.Since(allocatedBefore);
                    TestContext.Out.WriteLine("[WBG] alcance de " + hours + " h: " + watch.ElapsedMilliseconds + " ms, "
                        + (allocated.HasValue ? (allocated.Value / (1024 * 1024)) + " MB" : "memoria sin medir"));
                    Assert.That(snapshots, Is.EqualTo(1), hours + " h: una sola foto");
                    Assert.That(run.Engine.State.Value.TotalElapsedSeconds, Is.EqualTo(3 + hours * 3600));
                    Assert.That(watch.Elapsed.TotalMilliseconds, Is.LessThan(hours == 1 ? 100 : 500), hours + " h");
                    if (allocated.HasValue)
                    {
                        Assert.That(allocated.Value, Is.LessThan(32L * 1024 * 1024), hours + " h: memoria");
                    }
                }

                run.Dispose();
            }
        }

        // Corta con R-4 en el ciclo de vida de prueba, con o sin segundo plano, corrida hasta `until`.
        private static EngineRun RunCorta(double? backgroundAt, double? returnAt, double until)
        {
            var run = new EngineRun(RowerScripts.R4);
            run.Harness.RunUntil(0);
            run.Engine.StartWithRoutine(RoutineSeeds.Corta());
            if (backgroundAt.HasValue)
            {
                run.Harness.Background(backgroundAt.Value);
                run.Harness.Foreground(returnAt!.Value);
            }

            run.Harness.RunUntil(until);
            return run;
        }

        // El motor sobre el FrameScheduler del ciclo de vida de prueba, con la base en memoria.
        private sealed class EngineRun : IDisposable
        {
            public EngineRun(Func<int, RowingData> reading)
            {
                Harness = new LifecycleHarness(WorkoutHarness.SessionStart);
                Rower = new ScriptedRower(Harness.Scheduler, reading, Harness.Logger);
                Engine = new WorkoutEngine(Rower, new ScriptedHeartRateService(Harness.Logger), Sessions, Harness.Scheduler, Harness.Lifecycle,
                    Harness.Logger);
                Notices = new StateRecorder<WorkoutNotice>(Engine.Notices);
                Rower.Start();
            }

            public LifecycleHarness Harness { get; }

            public ScriptedRower Rower { get; }

            public InMemorySessionRepository Sessions { get; } = new InMemorySessionRepository();

            public WorkoutEngine Engine { get; }

            public StateRecorder<WorkoutNotice> Notices { get; }

            public void Dispose()
            {
                Notices.Dispose();
                Engine.Dispose();
                Rower.Dispose();
            }
        }
    }
}
