using System;
using System.Threading;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SRC: el grabador de la sesión (Q-12, Q-13; 02-tech-design §3.2 de la It. 4a): alta, cada punto en el acto o en lote si hay
    // una escritura en vuelo, punto final y cierre en una transacción, fallas y el paso a segundo plano.
    [TestFixture]
    public sealed class SessionRecorderTests
    {
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

        [Test, Category("AC-WKT-06-05")]
        public void PointIsWrittenAtOnce()
        {
            var base_ = new ScriptedSessionRepository(new FakeScheduler());
            var recorder = new SessionRecorder(base_, new RecordingLogger());
            Assert.That(recorder.BeginAsync(new WorkoutSession(Start)).Result, Is.True);
            Assert.That(recorder.SessionId, Is.EqualTo(1));
            recorder.Add(Point(305));
            Assert.That(base_.Store.PointsOf(1).Count, Is.EqualTo(1), "escrito en el acto (a más tardar 1 s)");
            recorder.Add(Point(310));
            Assert.That(base_.Calls, Is.EqualTo(new[] { "create", "points:1", "points:1" }), "con la base al día, un lote por punto");
            Assert.That(base_.Store.PointsOf(1)[1].SessionId, Is.EqualTo(1), "el grabador pone el id");
        }

        [Test, Category("AC-WKT-06-12")]
        public void FailuresNoticeOnceAndKeepWriting()
        {
            var logger = new RecordingLogger();
            var base_ = new ScriptedSessionRepository(new FakeScheduler()) { FailBatch = 1 };
            var recorder = new SessionRecorder(base_, logger);
            var failed = 0;
            recorder.Failed += () => failed++;
            recorder.BeginAsync(new WorkoutSession(Start));
            recorder.Add(Point(305));
            Assert.That(failed, Is.EqualTo(1));
            Assert.That(recorder.HadFailure, Is.True);
            Assert.That(recorder.PointsToRetry, Is.EqualTo(1), "los puntos del lote que falló, para el reintento del cierre");
            recorder.Add(Point(310));
            Assert.That(base_.Store.PointsOf(1).Count, Is.EqualTo(1), "los lotes siguientes se siguen intentando");
            base_.FailBatch = 3;
            recorder.Add(Point(315));
            Assert.That(failed, Is.EqualTo(1), "una sola vez por sesión");
            Assert.That(recorder.PointsToRetry, Is.EqualTo(2));
            Assert.That(logger.Lines, Has.Some.StartsWith("[WORKOUT] Warning: lote de 1 puntos"));
        }

        [Test, Category("AC-WKT-06-12")]
        public void FailedBatchIsRetriedInTheClose()
        {
            // QA-G4aT-13 (P-16): el cierre reinserta, en SU transacción, los puntos del lote que falló, antes del final.
            var base_ = new ScriptedSessionRepository(new FakeScheduler()) { FailBatch = 1 };
            var recorder = new SessionRecorder(base_, new RecordingLogger());
            recorder.BeginAsync(new WorkoutSession(Start));
            recorder.Add(Point(305));
            recorder.Add(Point(310));
            var closed = new WorkoutSession(Start) { FinishedAt = Start.AddSeconds(12), TotalDistanceMeters = 1048 };
            var saved = recorder.FinishAsync(Point(312), closed);
            Assert.That(saved.Result, Is.True, "cerrada con todos sus puntos");
            Assert.That(base_.Calls, Is.EqualTo(new[] { "create", "points:1", "points:1", "finish:2" }));
            var points = base_.Store.PointsOf(1);
            Assert.That(points.Count, Is.EqualTo(3));
            Assert.That(points[2].ElapsedSeconds, Is.EqualTo(312), "el final, al último");
            Assert.That(base_.Store.Session(1)!.FinishedAt, Is.EqualTo(Start.AddSeconds(12)));
            Assert.That(recorder.PointsToRetry, Is.EqualTo(0));
        }

        [Test, Category("AC-WKT-06-12")]
        public void PersistentFailureLeavesTheSessionCut()
        {
            // Si el reintento también falla, la sesión no se cierra: queda cortada con lo que sí se guardó.
            var base_ = new ScriptedSessionRepository(new FakeScheduler()) { FailBatch = 1, FailFinishWithRetriedPoints = true };
            var logger = new RecordingLogger();
            var recorder = new SessionRecorder(base_, logger);
            recorder.BeginAsync(new WorkoutSession(Start));
            recorder.Add(Point(305));
            recorder.Add(Point(310));
            var saved = recorder.FinishAsync(Point(312), new WorkoutSession(Start) { FinishedAt = Start.AddSeconds(12) });
            Assert.That(saved.Result, Is.False);
            Assert.That(base_.Store.Session(1)!.FinishedAt, Is.Null, "cortada");
            Assert.That(base_.Store.PointsOf(1).Count, Is.EqualTo(1), "con lo que sí se guardó mientras se remaba");
            Assert.That(logger.Lines, Has.Some.StartsWith("[WORKOUT] Warning: cierre de la sesión 1"));
        }

        [Test, Category("AC-WKT-06-13")]
        public void SlowDatabaseKeepsOrderAndBatches()
        {
            // Con un lote en vuelo (2 s por escritura) los puntos nuevos se juntan y salen en el lote siguiente; el cierre va
            // después del último lote.
            var scheduler = new FakeScheduler();
            var base_ = new ScriptedSessionRepository(scheduler) { WriteDelay = TimeSpan.FromSeconds(2), FinishDelay = TimeSpan.FromSeconds(2) };
            var recorder = new SessionRecorder(base_, new RecordingLogger());
            recorder.BeginAsync(new WorkoutSession(Start));
            recorder.Add(Point(305));
            Assert.That(recorder.BatchesInFlight, Is.EqualTo(1));
            recorder.Add(Point(306));
            recorder.Add(Point(307));
            Assert.That(recorder.PendingPoints, Is.EqualTo(2), "esperan el lote en vuelo");
            scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(base_.Calls, Is.EqualTo(new[] { "create", "points:1", "points:2" }));
            recorder.Add(Point(308));
            var saved = recorder.FinishAsync(Point(309), new WorkoutSession(Start) { FinishedAt = Start });
            Assert.That(saved.IsCompleted, Is.False);
            scheduler.Advance(TimeSpan.FromSeconds(2));
            Assert.That(saved.IsCompleted, Is.False, "el cierre espera el lote en vuelo");
            scheduler.Advance(TimeSpan.FromSeconds(4));
            Assert.That(saved.Result, Is.True);
            Assert.That(base_.Calls[base_.Calls.Count - 1], Is.EqualTo("finish:2"), "el pendiente y el final, con el cierre");
            var points = base_.Store.PointsOf(1);
            for (var i = 0; i < points.Count; i++)
            {
                Assert.That(points[i].ElapsedSeconds, Is.EqualTo(305 + i), "en el orden en que se tomaron");
            }
        }

        [Test, Category("AC-WKT-01-11")]
        public void CreateFailureDiscardsThePoints()
        {
            var base_ = new ScriptedSessionRepository(new FakeScheduler()) { FailOnCreate = true };
            var recorder = new SessionRecorder(base_, new RecordingLogger());
            var failed = 0;
            recorder.Failed += () => failed++;
            Assert.That(recorder.BeginAsync(new WorkoutSession(Start)).Result, Is.False);
            Assert.That(recorder.CreateFailed, Is.True);
            recorder.Add(Point(305));
            Assert.That(recorder.FinishAsync(Point(306), new WorkoutSession(Start)).Result, Is.False, "sin alta no hay cierre");
            Assert.That(base_.Calls, Is.EqualTo(new[] { "create" }));
            Assert.That(failed, Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-06-15")]
        public void FlushSendsThePendingBatchEvenWithOneInFlight()
        {
            var scheduler = new FakeScheduler();
            var base_ = new ScriptedSessionRepository(scheduler) { WriteDelay = TimeSpan.FromSeconds(2) };
            var recorder = new SessionRecorder(base_, new RecordingLogger());
            recorder.BeginAsync(new WorkoutSession(Start));
            recorder.Add(Point(305));
            recorder.Add(Point(310));
            Assert.That(recorder.FlushAndWait(TimeSpan.FromSeconds(1)), Is.False, "la base de guion no alcanzó (no puede esperar)");
            Assert.That(base_.WaitCalls, Is.EqualTo(1));
            Assert.That(base_.Calls, Is.EqualTo(new[] { "create", "points:1", "points:1" }), "el pendiente salió ya, detrás del lote en vuelo");
            scheduler.Advance(TimeSpan.FromSeconds(4));
            Assert.That(base_.Store.PointsOf(1).Count, Is.EqualTo(2));
        }

        [Test, Category("AC-WKT-06-15")]
        public void AbandonSendsWhatIsPendingAndDoesNotClose()
        {
            var scheduler = new FakeScheduler();
            var base_ = new ScriptedSessionRepository(scheduler) { CreateDelay = TimeSpan.FromSeconds(3) };
            var recorder = new SessionRecorder(base_, new RecordingLogger());
            recorder.BeginAsync(new WorkoutSession(Start));
            recorder.Add(Point(305));
            recorder.Abandon();
            recorder.Dispose();
            Assert.That(base_.Calls, Is.EqualTo(new[] { "create", "after-create:1" }), "detrás del alta");
            scheduler.Advance(TimeSpan.FromSeconds(3));
            Assert.That(base_.Store.PointsOf(1).Count, Is.EqualTo(1), "el punto quedó escrito con el id del alta");
            Assert.That(base_.Store.Session(1)!.FinishedAt, Is.Null, "cortada (P-03)");
        }

        [Test, Category("AC-WKT-06-15")]
        public void PointsTakenWhileCreatingAreFlushedOnBackground()
        {
            // QA-G4aT-06: un alta lenta (un trabajo que bloquea el SerialExecutor real) y el paso a segundo plano a los 6 s: el
            // punto del segundo 5, tomado con el alta en vuelo, sale con AddPointsAfterCreateAsync detrás del alta, en el hilo de
            // la base, y queda escrito antes del tope de 1 s. Se lee desde otra conexión.
            TestMainThreadContext.Run(async () =>
            {
                var logger = new RecordingLogger();
                using (var temp = new TempDatabase())
                {
                    var executor = new SerialExecutor("RowMate.Db.Test", logger);
                    using (var store = temp.Store(logger, executor))
                    {
                        Assert.That(await store.Sessions.ListAsync(), Is.Empty, "la base ya abierta (la creación no cuenta en el tope)");
                        using (var gate = new ManualResetEventSlim(false))
                        {
                            var blocker = executor.Run(() => gate.Wait(TimeSpan.FromSeconds(5)));
                            var recorder = new SessionRecorder(store.Sessions, logger);
                            var begin = recorder.BeginAsync(new WorkoutSession(Start));
                            recorder.Add(Point(305));
                            Assert.That(recorder.IsCreating, Is.True, "el alta en vuelo");
                            ThreadPool.QueueUserWorkItem(_ =>
                            {
                                Thread.Sleep(200);
                                gate.Set();
                            });
                            Assert.That(recorder.FlushAndWait(WorkoutTimings.BackgroundFlushTimeout), Is.True, "escrito antes del tope de 1 s");
                            await blocker;
                            Assert.That(await begin, Is.True);
                            using (var other = temp.Open(logger))
                            {
                                var rows = DatabaseRows(other, "SELECT session_id, elapsed_seconds FROM data_points");
                                Assert.That(rows.Count, Is.EqualTo(1));
                                Assert.That(rows[0][0], Is.EqualTo(1L), "con el id que dejó el alta");
                                Assert.That(rows[0][1], Is.EqualTo(305L));
                            }
                        }
                    }
                }
            });
        }

        private static System.Collections.Generic.IReadOnlyList<object?[]> DatabaseRows(RowMate.Infrastructure.Database.RowMateDatabase database, string sql)
        {
            var task = database.QueryRawForTestsAsync(sql);
            Assert.That(task.IsCompleted, Is.True, "otra conexión, en línea");
            return task.Result;
        }

        private static DataPoint Point(int elapsed)
        {
            return new DataPoint(0, elapsed) { StrokeRate = 24, PowerWatts = 150, DistanceMeters = 1000 + elapsed };
        }
    }
}
