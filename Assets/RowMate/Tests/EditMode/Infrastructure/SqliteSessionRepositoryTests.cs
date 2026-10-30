using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Workout;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Sessions;
using RowMate.Infrastructure.Threading;
using RowMate.Tests.EditMode.Workout;
using RowMate.Tests.Support;
using static RowMate.Tests.EditMode.Infrastructure.DatabaseFileState;

namespace RowMate.Tests.EditMode.Infrastructure
{
    // SSR: sesiones y puntos sobre SQLite real (F-WKT-06 + F-HIS-01 a -03, F-HIS-17, F-HIS-19; 02-tech-design §3 de la It. 4a),
    // con el motor de verdad y el estado de prueba de la spec §0.4, en una carpeta temporal con ñá. Las mismas fuentes corren
    // en dotnet (sqlite-net-pcl) y en Unity EditMode (gilzoide). La durabilidad se prueba también con el SerialExecutor real,
    // las continuaciones en el hilo de la prueba (TestMainThreadContext, como en Unity) y la lectura desde OTRA conexión.
    // legacy: lib/core/database/database_service.dart:214-290.
    [TestFixture]
    public sealed class SqliteSessionRepositoryTests
    {
        private const string PointsSql = "SELECT elapsed_seconds FROM data_points ORDER BY id";

        private RecordingLogger _logger = null!;
        private TempDatabase _temp = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingLogger();
            _temp = new TempDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            _temp.Dispose();
        }

        [Test, Category("AC-WKT-06-01")]
        public void CreateWritesTheLegacyRow()
        {
            // insertSession (:216-219) con toMap (workout_session.dart:39-51): la rutina de la plantilla, el inicio en UTC con
            // milisegundos, sin fin, los totales en 0, sin Strava y origin = app. En libre, sin rutina.
            using (var store = _temp.Store(_logger))
            {
                using (var h = new WorkoutHarness(sessions: store.Sessions))
                {
                    h.Routine(WorkoutHarness.Saved(RoutineSeeds.Corta(), 7));
                    h.At(3);
                    Done(h.Engine.FinishAsync());
                    Assert.That(h.Engine.LastFinishedSessionId, Is.EqualTo(1));
                    h.Engine.StartFree();
                }

                var rows = Done(store.Database.QueryRawForTestsAsync("SELECT id, routine_id, routine_name, started_at, finished_at, total_distance_meters, "
                    + "total_time_seconds, avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id, origin FROM workout_sessions ORDER BY id"));
                Assert.That(rows.Count, Is.EqualTo(2));
                Assert.That(rows[1], Is.EqualTo(new object?[] { 2L, null, null, "2026-10-28T13:00:03.000Z", null, 0L, 0L, 0L, 0.0, 0L, null, "app" }),
                    "libre, recién creada (y cortada: la prueba no la cierra)");
                Assert.That(rows[0][1], Is.EqualTo(7L), "el id de la plantilla");
                Assert.That(rows[0][2], Is.EqualTo("Corta"));
                Assert.That(rows[0][3], Is.EqualTo("2026-10-28T13:00:00.000Z"), "AC-RUT-16-04");
                Assert.That(rows[0][11], Is.EqualTo("app"));
            }
        }

        [Test, Category("AC-WKT-06-05")]
        public void KilledAt31_5sHasSixPoints()
        {
            // Libre con R-4 y el proceso terminado a la fuerza a los 31,5 s (nadie cierra nada): otra conexión lee los 6 puntos
            // de los segundos 5 a 30. Con el ejecutor en línea cada punto se escribe en el acto.
            using (var store = _temp.Store(_logger))
            using (var h = new WorkoutHarness(sessions: store.Sessions))
            {
                h.Free();
                h.At(31.5);
                AssertKilledSession();
            }
        }

        [Test, Category("AC-WKT-06-05")]
        public void KilledAt31_5sHasSixPointsWithTheSerialExecutor()
        {
            // Lo mismo con el SerialExecutor real: el punto sale en el cuadro en que se toma y la vuelta del lote llega por el
            // contexto del hilo principal. Cada segundo la prueba deja correr ese contexto (lo que hace Unity en cada cuadro).
            TestMainThreadContext.Run(async () =>
            {
                using (var store = _temp.Store(_logger, new SerialExecutor("RowMate.Db.Test", _logger)))
                using (var h = new WorkoutHarness(sessions: store.Sessions))
                {
                    await store.Sessions.ListAsync();
                    h.Free();
                    // Detrás del alta (FIFO): cuando vuelve la lista, el alta volvió y el reloj abrió en el instante 0.
                    await store.Sessions.ListAsync();
                    for (var second = 1; second <= 31; second++)
                    {
                        h.At(second);
                        await store.Sessions.ListAsync();
                    }

                    h.At(31.5);
                    AssertKilledSession();
                }
            });
        }

        [Test, Category("AC-WKT-06-10")]
        public void CutSessionIsOpenWithItsPoints()
        {
            // La app se cierra a los 31,5 s (Dispose sin terminar): la sesión queda cortada, sin fin y con los totales en 0, con
            // sus 6 puntos. Al volver a abrir, la app no la toca: ni otra sesión ni una relectura cambian la fila.
            using (var store = _temp.Store(_logger))
            using (var h = new WorkoutHarness(sessions: store.Sessions))
            {
                h.Free();
                h.At(31.5);
            }

            using (var reopened = _temp.Store(_logger))
            {
                var session = Done(reopened.Sessions.GetByIdAsync(1))!;
                Assert.That(session.FinishedAt, Is.Null);
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(0));
                Assert.That(session.TotalTimeSeconds, Is.EqualTo(0));
                Assert.That(session.AvgStrokeRate, Is.EqualTo(0.0));
                Assert.That(Done(reopened.Sessions.GetPointsAsync(1)).Count, Is.EqualTo(6));
                var before = SessionRow(reopened.Database, 1);
                using (var h = new WorkoutHarness(sessions: reopened.Sessions))
                {
                    h.Free();
                    h.At(6);
                    Done(h.Engine.FinishAsync());
                }

                Assert.That(SessionRow(reopened.Database, 1), Is.EqualTo(before), "la cortada no cambia");
                var listing = Done(reopened.Sessions.ListAsync());
                Assert.That(listing.Count, Is.EqualTo(2));
                Assert.That(listing[1].Id, Is.EqualTo(1));
                Assert.That(listing[1].FinishedAt, Is.Null);
                Assert.That(listing[1].PointCount, Is.EqualTo(6));
                Assert.That(listing[0].FinishedAt, Is.Not.Null, "la nueva, cerrada");
            }
        }

        [Test, Category("AC-WKT-06-12")]
        public void CloseRetriesTheFailedBatchInOneTransaction()
        {
            // QA-G4aT-13: el lote del segundo 10 falla una vez (la prueba saca la falla después); el cierre lo vuelve a
            // insertar en su transacción, antes del punto final y del UPDATE: la sesión queda cerrada con sus 3 puntos y sus
            // totales salen de ellos. DT4a-17: el final es el de un guardado bueno.
            using (var store = _temp.Store(_logger))
            using (var h = new WorkoutHarness(sessions: store.Sessions))
            {
                FailPointsAt(store.Database, 310);
                h.Free();
                h.At(10.2);
                Assert.That(h.Notices.Count, Is.EqualTo(1));
                Assert.That(h.Notices.Values[0].Kind, Is.EqualTo(WorkoutNoticeKind.SaveFailed));
                Assert.That(Elapsed(store.Database), Is.EqualTo(new object?[] { 305L }), "el que falló no está");
                Done(store.Database.ExecuteRawForTestsAsync("DROP TRIGGER rm_fault"));
                h.At(12.3);
                Done(h.Engine.FinishAsync());
                Assert.That(Elapsed(store.Database), Is.EqualTo(new object?[] { 305L, 310L, 312L }), "el reintentado antes del final");
                var session = Done(store.Sessions.GetByIdAsync(1))!;
                Assert.That(session.FinishedAt!.Value.UtcDateTime, Is.EqualTo(new DateTime(2026, 10, 28, 13, 0, 12, 300, DateTimeKind.Utc)));
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(1048));
                Assert.That(session.TotalTimeSeconds, Is.EqualTo(312));
                Assert.That(h.Notices.Values[h.Notices.Count - 1].ToString(), Is.EqualTo("Ended(Manual, true)"));
                Assert.That(h.Engine.LastFinishedSessionId, Is.EqualTo(1));
            }
        }

        [Test, Category("AC-WKT-06-12")]
        public void PersistentPointFailureLeavesTheSessionCut()
        {
            // Si el punto vuelve a fallar en el cierre, rollback: ni el reintentado, ni el final, ni el cierre. La sesión queda
            // cortada con lo que se guardó mientras se remaba, y el final avisa que no se guardó.
            using (var store = _temp.Store(_logger))
            using (var h = new WorkoutHarness(sessions: store.Sessions))
            {
                FailPointsAt(store.Database, 310);
                h.Free();
                h.At(12.3);
                Done(h.Engine.FinishAsync());
                Assert.That(Elapsed(store.Database), Is.EqualTo(new object?[] { 305L }), "sin el final ni el reintentado");
                var session = Done(store.Sessions.GetByIdAsync(1))!;
                Assert.That(session.FinishedAt, Is.Null, "cortada");
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(0));
                Assert.That(h.Notices.Values[h.Notices.Count - 1].ToString(), Is.EqualTo("Ended(Manual, false)"));
                Assert.That(h.Engine.LastFinishedSessionId, Is.Null);
            }
        }

        [Test, Category("AC-WKT-06-12")]
        public void CloseIsAtomic()
        {
            // DT4a-07: un trigger que falla en el UPDATE del cierre deja la sesión cortada y SIN el punto final (la misma
            // transacción); los puntos que se guardaron mientras se remaba quedan.
            using (var store = _temp.Store(_logger))
            using (var h = new WorkoutHarness(sessions: store.Sessions))
            {
                Done(store.Database.ExecuteRawForTestsAsync(
                    "CREATE TRIGGER rm_fault BEFORE UPDATE ON workout_sessions BEGIN SELECT RAISE(ABORT, 'falla inyectada'); END"));
                h.Free();
                h.At(12.3);
                Done(h.Engine.FinishAsync());
                Assert.That(h.Notices.Values[h.Notices.Count - 1].ToString(), Is.EqualTo("Ended(Manual, false)"));
                Assert.That(Elapsed(store.Database), Is.EqualTo(new object?[] { 305L, 310L }), "sin el punto final");
                Assert.That(Done(store.Sessions.GetByIdAsync(1))!.FinishedAt, Is.Null);
            }
        }

        [Test, Category("AC-WKT-06-12")]
        public void FinishOfAMissingSessionThrows()
        {
            // 0 filas en el UPDATE: SessionNotFoundException y rollback (tampoco quedan los puntos).
            using (var store = _temp.Store(_logger))
            {
                var closed = new WorkoutSession(WorkoutHarness.SessionStart) { Id = 42, FinishedAt = WorkoutHarness.SessionStart.AddSeconds(3) };
                var error = Failure(store.Sessions.FinishAsync(new[] { new DataPoint(42, 303) }, closed));
                Assert.That(error, Is.InstanceOf<SessionNotFoundException>());
                Assert.That(Count(store.Database, "data_points"), Is.EqualTo(0L));
                Assert.That(Failure(store.Sessions.FinishAsync(new DataPoint[0], closed with { Id = null })), Is.InstanceOf<SessionNotFoundException>());
            }
        }

        [Test, Category("AC-WKT-06-14"), Category("AC-HIS-17-07")]
        public void NothingButSessionsAndPointsIsWritten()
        {
            // Después de Corta (con objetivos, parciales y repeticiones): solo hay filas en workout_sessions (1) y data_points
            // (7), con las columnas de v8; nada de objetivos, parciales ni repeticiones (paridad, workout_provider.dart:57-61).
            using (var store = _temp.Store(_logger))
            {
                RunCorta(store);
                Assert.That(Count(store.Database, "workout_sessions"), Is.EqualTo(1L));
                Assert.That(Count(store.Database, "data_points"), Is.EqualTo(7L));
                Assert.That(Count(store.Database, "routines"), Is.EqualTo(0L));
                Assert.That(Count(store.Database, "interval_steps"), Is.EqualTo(0L));
                var sequences = Done(store.Database.QueryRawForTestsAsync("SELECT name, seq FROM sqlite_sequence ORDER BY name"));
                Assert.That(sequences.Count, Is.EqualTo(2));
                Assert.That(sequences[0], Is.EqualTo(new object?[] { "data_points", 7L }));
                Assert.That(sequences[1], Is.EqualTo(new object?[] { "workout_sessions", 1L }));
                Assert.That(ColumnNames(store.Database, "workout_sessions"), Is.EqualTo(new[]
                {
                    "id", "routine_id", "routine_name", "started_at", "finished_at", "total_distance_meters", "total_time_seconds", "avg_power_watts",
                    "avg_stroke_rate", "total_calories", "strava_activity_id", "origin",
                }));
                Assert.That(ColumnNames(store.Database, "data_points"), Is.EqualTo(new[]
                {
                    "id", "session_id", "elapsed_seconds", "stroke_rate", "stroke_count", "distance_meters", "pace_500m_seconds", "power_watts",
                    "calories", "heart_rate", "step_index", "step_type",
                }));
            }
        }

        [Test, Category("AC-HIS-17-07")]
        public void SessionsAndPointsFillV8()
        {
            // Una fila de sesión con origin = app y sus 7 puntos con su session_id, con los tipos de almacenamiento del legacy:
            // enteros, REAL para el SPM y TEXT para las fechas y el tipo de paso.
            using (var store = _temp.Store(_logger))
            {
                RunCorta(store);
                var session = Done(store.Database.QueryRawForTestsAsync("SELECT typeof(started_at), typeof(finished_at), typeof(total_distance_meters), "
                    + "typeof(avg_stroke_rate), typeof(avg_power_watts), origin, total_distance_meters, total_time_seconds FROM workout_sessions"));
                Assert.That(session[0], Is.EqualTo(new object?[] { "text", "text", "integer", "real", "integer", "app", 1120L, 330L }));
                var points = Done(store.Database.QueryRawForTestsAsync("SELECT session_id, elapsed_seconds, typeof(stroke_rate), typeof(elapsed_seconds), "
                    + "step_index, step_type, typeof(step_type) FROM data_points ORDER BY id"));
                Assert.That(points.Count, Is.EqualTo(7));
                var indices = new List<object?>();
                var types = new List<object?>();
                foreach (var row in points)
                {
                    Assert.That(row[0], Is.EqualTo(1L), "su session_id");
                    Assert.That(row[2], Is.EqualTo("real"));
                    Assert.That(row[3], Is.EqualTo("integer"));
                    Assert.That(row[6], Is.EqualTo("text"));
                    indices.Add(row[4]);
                    types.Add(row[5]);
                }

                Assert.That(indices, Is.EqualTo(new object?[] { 0L, 0L, 1L, 2L, 2L, 3L, 3L }));
                Assert.That(types, Is.EqualTo(new object?[] { "work", "work", "rest", "work", "work", "rest", "rest" }));
            }
        }

        [Test, Category("AC-HIS-17-02")]
        public void SchemaStillV8()
        {
            // Las sesiones no migran nada: después de guardar, el esquema es el de la It. 3 al carácter y user_version = 8.
            using (var store = _temp.Store(_logger))
            {
                RunCorta(store);
                var master = Done(store.Database.QueryRawForTestsAsync("SELECT type, name, sql FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY rowid"));
                Assert.That(master.Count, Is.EqualTo(DatabaseSchema.Statements.Count));
                for (var i = 0; i < master.Count; i++)
                {
                    Assert.That(master[i][2], Is.EqualTo(DatabaseSchema.Statements[i]), master[i][1] + " al carácter");
                }

                Assert.That(Done(store.Database.QueryRawForTestsAsync("PRAGMA user_version"))[0][0], Is.EqualTo(8L));
            }
        }

        [Test, Category("AC-HIS-19-01")]
        public void GetByIdInLocalTime()
        {
            // getSessionById (:241-246): la sesión con las fechas en hora local (AC-RUT-16-04; acá UTC−3 fija), o "no existe".
            var zone = TimeZoneInfo.CreateCustomTimeZone("RowMate-UTC-3", TimeSpan.FromHours(-3), "RowMate UTC-3", "RowMate UTC-3");
            using (var store = _temp.Store(_logger))
            {
                using (var h = new WorkoutHarness(sessions: store.Sessions))
                {
                    h.Free();
                    h.At(12.3);
                    Done(h.Engine.FinishAsync());
                }

                var repository = new SqliteSessionRepository(store.Database, _logger, zone);
                var session = Done(repository.GetByIdAsync(1))!;
                Assert.That(session.Id, Is.EqualTo(1));
                Assert.That(session.StartedAt.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
                Assert.That(session.StartedAt, Is.EqualTo(new DateTimeOffset(2026, 10, 28, 10, 0, 0, TimeSpan.FromHours(-3))));
                Assert.That(session.FinishedAt!.Value.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
                Assert.That(session.FinishedAt.Value, Is.EqualTo(new DateTimeOffset(2026, 10, 28, 10, 0, 12, 300, TimeSpan.FromHours(-3))));
                Assert.That(session.TotalDistanceMeters, Is.EqualTo(1048));
                Assert.That(session.TotalTimeSeconds, Is.EqualTo(312));
                Assert.That(session.AvgPowerWatts, Is.EqualTo(150));
                Assert.That(session.AvgStrokeRate, Is.EqualTo(24.0));
                Assert.That(session.TotalCalories, Is.EqualTo(41));
                Assert.That(session.RoutineId, Is.Null);
                Assert.That(session.StravaActivityId, Is.Null);
                Assert.That(Done(repository.GetByIdAsync(99)), Is.Null, "no existe");
            }
        }

        [Test, Category("AC-HIS-19-02")]
        public void PointsOrderedByMonitorTime()
        {
            // getDataPointsForSession (:263-287): por el tiempo del monitor de menor a mayor; el id desempata (DT4a-05).
            using (var store = _temp.Store(_logger))
            {
                var id = Done(store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart)));
                Done(store.Sessions.AddPointsAsync(new[]
                {
                    new DataPoint(id, 312) { DistanceMeters = 1048 }, new DataPoint(id, 305) { DistanceMeters = 1020 },
                    new DataPoint(id, 310) { DistanceMeters = 1040, StrokeRate = 24.0, StepIndex = 2, StepTypeName = "work" },
                }));
                Done(store.Sessions.AddPointsAsync(new[] { new DataPoint(id, 312) { DistanceMeters = 1049 } }));
                var other = Done(store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart)));
                Done(store.Sessions.AddPointsAsync(new[] { new DataPoint(other, 1) }));
                var points = Done(store.Sessions.GetPointsAsync(id));
                var described = new List<string>();
                foreach (var point in points)
                {
                    described.Add(point.ElapsedSeconds + "/" + point.DistanceMeters);
                    Assert.That(point.SessionId, Is.EqualTo(id));
                    Assert.That(point.Id, Is.Not.Null);
                }

                Assert.That(described, Is.EqualTo(new[] { "305/1020", "310/1040", "312/1048", "312/1049" }));
                Assert.That(points[1].StrokeRate, Is.EqualTo(24.0));
                Assert.That(points[1].StepIndex, Is.EqualTo(2));
                Assert.That(points[1].StepTypeName, Is.EqualTo("work"));
                Assert.That(points[0].StepIndex, Is.Null);
                Assert.That(points[0].StepTypeName, Is.Null);
                Assert.That(Done(store.Sessions.GetPointsAsync(99)), Is.Empty);
            }
        }

        [Test, Category("AC-WKT-06-15")]
        public void PointsAfterCreateWaitForTheCreate()
        {
            // QA-G4aT-06: con el alta encolada detrás de un trabajo que bloquea la base, el lote se encola en el acto DETRÁS del
            // alta y toma el id que dejó, en el hilo de la base, sin que el hilo de la prueba espere ninguna continuación.
            var executor = new SerialExecutor("RowMate.Db.Test", _logger);
            using (var store = _temp.Store(_logger, executor))
            using (var gate = new ManualResetEventSlim(false))
            {
                Assert.That(store.Sessions.ListAsync().Wait(TimeSpan.FromSeconds(5)), Is.True);
                var busy = executor.Run(() => gate.Wait(TimeSpan.FromSeconds(5)));
                var creation = store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart));
                var batch = store.Sessions.AddPointsAfterCreateAsync(creation, new[] { new DataPoint(0, 305), new DataPoint(0, 310) });
                Assert.That(creation.IsCompleted, Is.False, "la base está ocupada");
                gate.Set();
                Assert.That(batch.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(busy.Result, Is.True);
                var points = store.Sessions.GetPointsAsync(creation.Result).Result;
                Assert.That(points.Count, Is.EqualTo(2));
                Assert.That(points[0].SessionId, Is.EqualTo(creation.Result), "el id del alta, no el del punto");
                Assert.That(points[1].ElapsedSeconds, Is.EqualTo(310));
            }
        }

        [Test, Category("AC-WKT-06-15")]
        public void PointsAfterAFailedCreateWriteNothing()
        {
            // Si el alta falla, el lote no escribe nada (no hay sesión). Si la tarea del alta no terminó cuando le toca al lote
            // (el orden FIFO se rompió), el lote falla y no escribe.
            using (var store = _temp.Store(_logger))
            {
                Done(store.Database.ExecuteRawForTestsAsync(
                    "CREATE TRIGGER rm_fault BEFORE INSERT ON workout_sessions BEGIN SELECT RAISE(ABORT, 'falla inyectada'); END"));
                var creation = store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart));
                Assert.That(creation.IsFaulted, Is.True);
                Done(store.Sessions.AddPointsAfterCreateAsync(creation, new[] { new DataPoint(0, 305) }));
                Assert.That(Count(store.Database, "data_points"), Is.EqualTo(0L));
                Assert.That(Count(store.Database, "workout_sessions"), Is.EqualTo(0L));

                var pending = new TaskCompletionSource<long>();
                Assert.That(Failure(store.Sessions.AddPointsAfterCreateAsync(pending.Task, new[] { new DataPoint(0, 305) })),
                    Is.InstanceOf<InvalidOperationException>());
                Assert.That(Count(store.Database, "data_points"), Is.EqualTo(0L));
            }
        }

        [Test, Category("AC-WKT-06-13")]
        public void BatchIsOneTransactionInOrder()
        {
            // Un lote de varios puntos es una transacción: si el tercero falla, no queda ninguno; los que entran, en su orden.
            using (var store = _temp.Store(_logger))
            {
                var id = Done(store.Sessions.CreateAsync(new WorkoutSession(WorkoutHarness.SessionStart)));
                FailPointsAt(store.Database, 315);
                Assert.That(Failure(store.Sessions.AddPointsAsync(new[] { new DataPoint(id, 305), new DataPoint(id, 310), new DataPoint(id, 315) })),
                    Is.Not.Null);
                Assert.That(Count(store.Database, "data_points"), Is.EqualTo(0L), "todo o nada");
                Done(store.Sessions.AddPointsAsync(new[] { new DataPoint(id, 305), new DataPoint(id, 310) }));
                Assert.That(Elapsed(store.Database), Is.EqualTo(new object?[] { 305L, 310L }));
            }
        }

        // La sesión libre "matada" a los 31,5 s, leída desde OTRA conexión (la de la app sigue abierta).
        private void AssertKilledSession()
        {
            using (var other = _temp.Open(_logger))
            {
                var rows = Done(other.QueryRawForTestsAsync(PointsSql));
                var seconds = new List<object?>();
                foreach (var row in rows)
                {
                    seconds.Add(row[0]);
                }

                Assert.That(seconds, Is.EqualTo(new object?[] { 305L, 310L, 315L, 320L, 325L, 330L }), "los 6 puntos de los segundos 5 a 30");
                var session = Done(other.QueryRawForTestsAsync("SELECT finished_at, total_distance_meters FROM workout_sessions"));
                Assert.That(session[0], Is.EqualTo(new object?[] { null, 0L }), "cortada: sin fin y con los totales en 0");
            }
        }

        // Corta con R-4 hasta su fin automático (en el segundo 30), con la base en línea.
        private static void RunCorta(LocalStore store)
        {
            using (var h = new WorkoutHarness(sessions: store.Sessions))
            {
                h.Routine(WorkoutHarness.Saved(RoutineSeeds.Corta(), 3));
                h.At(31);
                // It. 4b (DT4a-15): el cierre volvió y el motor espera al Terminar del diálogo de completado.
                Assert.That(h.Engine.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.Engine.LastFinishedSessionId, Is.EqualTo(1));
                h.Engine.AcknowledgeCompletion();
                Assert.That(h.Engine.Phase, Is.EqualTo(WorkoutPhase.Idle));
            }
        }

        // Un trigger que hace fallar toda inserción del punto con ese tiempo del monitor (hasta DROP TRIGGER rm_fault).
        private static void FailPointsAt(RowMateDatabase database, int elapsedSeconds)
        {
            Done(database.ExecuteRawForTestsAsync("CREATE TRIGGER rm_fault BEFORE INSERT ON data_points WHEN NEW.elapsed_seconds = "
                + elapsedSeconds + " BEGIN SELECT RAISE(ABORT, 'falla inyectada'); END"));
        }

        private static object?[] Elapsed(RowMateDatabase database)
        {
            var rows = Done(database.QueryRawForTestsAsync(PointsSql));
            var values = new object?[rows.Count];
            for (var i = 0; i < rows.Count; i++)
            {
                values[i] = rows[i][0];
            }

            return values;
        }

        private static long Count(RowMateDatabase database, string table)
        {
            return (long)Done(database.QueryRawForTestsAsync("SELECT count(*) FROM " + table))[0][0]!;
        }

        private static object?[] SessionRow(RowMateDatabase database, long id)
        {
            return Done(database.QueryRawForTestsAsync("SELECT * FROM workout_sessions WHERE id = ?", id))[0];
        }

        private static string[] ColumnNames(RowMateDatabase database, string table)
        {
            var rows = Done(database.QueryRawForTestsAsync("PRAGMA table_info(" + table + ")"));
            var names = new string[rows.Count];
            for (var i = 0; i < rows.Count; i++)
            {
                names[i] = (string)rows[i][1]!;
            }

            return names;
        }
    }
}
