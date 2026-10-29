using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Database;
using SQLite;

namespace RowMate.Infrastructure.Sessions
{
    // Sesiones y puntos sobre la base propia (F-WKT-06 + F-HIS-01 a -03, F-HIS-17, F-HIS-19; 02-tech-design §3.3 de la It. 4a),
    // con el SQL exacto sobre el esquema v8, sin migración. Todo corre en el ejecutor de la base, en orden (FIFO): el alta,
    // cada lote y el cierre. Con lo que el legacy no tiene: cada punto escrito al tomarse (Q-12), el cierre con el punto final
    // en UNA transacción (DT4a-07) y el reintento de los puntos de un lote fallado dentro de esa transacción (QA-G4aT-13).
    // legacy: lib/core/database/database_service.dart:214-290. No es dueño de la base: la arma y la dispone LocalStore.
    public sealed class SqliteSessionRepository : ISessionRepository
    {
        // insertSession (:216-219) con toMap (workout_session.dart:39-51): los totales en 0 explícitos y origin = 'app'.
        private const string InsertSession = "INSERT INTO workout_sessions (routine_id, routine_name, started_at, finished_at, "
            + "total_distance_meters, total_time_seconds, avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id, origin) "
            + "VALUES (?, ?, ?, NULL, 0, 0, 0, 0, 0, NULL, 'app')";

        private const string InsertPoint = "INSERT INTO data_points (" + SessionRowMapper.PointColumns + ") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        // updateSession (:221-229), la fila entera salvo el id y origin (DT4a-06).
        private const string UpdateSession = "UPDATE workout_sessions SET routine_id = ?, routine_name = ?, started_at = ?, finished_at = ?, "
            + "total_distance_meters = ?, total_time_seconds = ?, avg_power_watts = ?, avg_stroke_rate = ?, total_calories = ?, "
            + "strava_activity_id = NULL WHERE id = ?";

        // getSessionById (:241-246).
        private const string SelectSession = "SELECT id, routine_id, routine_name, started_at, finished_at, total_distance_meters, "
            + "total_time_seconds, avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id FROM workout_sessions WHERE id = ?";

        // getDataPointsForSession (:263-287): ORDER BY elapsed_seconds ASC, con el id para desempatar (DT4a-05).
        private const string SelectPoints = "SELECT id, session_id, elapsed_seconds, stroke_rate, stroke_count, distance_meters, "
            + "pace_500m_seconds, power_watts, calories, heart_rate, step_index, step_type FROM data_points WHERE session_id = ? "
            + "ORDER BY elapsed_seconds, id";

        // La herramienta de la demo: el orden de getSessions (:248-252), con el id para desempatar.
        private const string SelectListing = "SELECT s.id AS id, s.started_at AS started_at, s.finished_at AS finished_at, "
            + "s.routine_name AS routine_name, s.origin AS origin, "
            + "(SELECT count(*) FROM data_points p WHERE p.session_id = s.id) AS point_count "
            + "FROM workout_sessions s ORDER BY s.started_at DESC, s.id DESC";

        private readonly RowMateDatabase _database;
        private readonly TimeZoneInfo _localZone;

        public SqliteSessionRepository(RowMateDatabase database, IAppLogger logger, TimeZoneInfo? localZone = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _localZone = localZone ?? TimeZoneInfo.Local;
        }

        // La base de este repositorio (las pruebas la consultan con QueryRawForTestsAsync).
        public RowMateDatabase Database => _database;

        // La tarea del ejecutor, sin async/await en el medio: se completa en el hilo de la base, antes del trabajo siguiente.
        public Task<long> CreateAsync(WorkoutSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var record = WorkoutSessionCodec.ToRecord(session);
            return _database.RunAsync(connection =>
            {
                long id = 0;
                connection.RunInTransaction(() =>
                {
                    connection.Execute(InsertSession, record.RoutineId, record.RoutineName, record.StartedAt);
                    id = connection.ExecuteScalar<long>("SELECT last_insert_rowid()");
                });

                return id;
            });
        }

        public Task AddPointsAsync(IReadOnlyList<DataPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            var copy = Copy(points);
            return _database.RunAsync(connection =>
            {
                connection.RunInTransaction(() => InsertPoints(connection, copy, null));
                return true;
            });
        }

        public Task AddPointsAfterCreateAsync(Task<long> creation, IReadOnlyList<DataPoint> points)
        {
            if (creation == null)
            {
                throw new ArgumentNullException(nameof(creation));
            }

            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            var copy = Copy(points);
            return _database.RunAsync(connection =>
            {
                // Detrás del alta (FIFO): su tarea ya se completó en este hilo. Si no, el contrato se rompió.
                if (!creation.IsCompleted)
                {
                    throw new InvalidOperationException("SqliteSessionRepository: el alta todavía no terminó (orden FIFO roto).");
                }

                if (creation.IsFaulted || creation.IsCanceled)
                {
                    return false;
                }

                var id = creation.Result;
                connection.RunInTransaction(() => InsertPoints(connection, copy, id));
                return true;
            });
        }

        // Una transacción: los puntos (los reintentados y el final, en orden) y la fila de cierre. 0 filas →
        // SessionNotFoundException; cualquier falla → rollback: no queda ni un punto ni el cierre (QA-G4aT-13).
        public Task FinishAsync(IReadOnlyList<DataPoint> points, WorkoutSession closed)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (closed == null)
            {
                throw new ArgumentNullException(nameof(closed));
            }

            if (!closed.Id.HasValue)
            {
                return Task.FromException(new SessionNotFoundException(null));
            }

            var id = closed.Id.Value;
            var copy = Copy(points);
            var record = WorkoutSessionCodec.ToRecord(closed);
            return _database.RunAsync(connection =>
            {
                connection.RunInTransaction(() =>
                {
                    InsertPoints(connection, copy, id);
                    var changed = connection.Execute(UpdateSession, record.RoutineId, record.RoutineName, record.StartedAt, record.FinishedAt,
                        record.TotalDistanceMeters, record.TotalTimeSeconds, record.AvgPowerWatts, record.AvgStrokeRate, record.TotalCalories,
                        id);
                    if (changed == 0)
                    {
                        throw new SessionNotFoundException(id);
                    }
                });

                return true;
            });
        }

        public Task<WorkoutSession?> GetByIdAsync(long id)
        {
            return _database.RunAsync(connection =>
            {
                var rows = connection.Query<WorkoutSessionRow>(SelectSession, id);
                return rows.Count == 0 ? (WorkoutSession?)null : SessionRowMapper.ToSession(rows[0], _localZone);
            });
        }

        public Task<IReadOnlyList<DataPoint>> GetPointsAsync(long sessionId)
        {
            return _database.RunAsync<IReadOnlyList<DataPoint>>(connection =>
            {
                var rows = connection.Query<DataPointRow>(SelectPoints, sessionId);
                var points = new DataPoint[rows.Count];
                for (var i = 0; i < rows.Count; i++)
                {
                    points[i] = SessionRowMapper.ToPoint(rows[i]);
                }

                return points;
            });
        }

        public Task<IReadOnlyList<SessionListing>> ListAsync()
        {
            return _database.RunAsync<IReadOnlyList<SessionListing>>(connection =>
            {
                var rows = connection.Query<SessionListingRow>(SelectListing);
                var listings = new SessionListing[rows.Count];
                for (var i = 0; i < rows.Count; i++)
                {
                    listings[i] = SessionRowMapper.ToListing(rows[i], _localZone);
                }

                return listings;
            });
        }

        public bool WaitForPendingWork(TimeSpan timeout)
        {
            return _database.WaitForPendingWork(timeout);
        }

        // Una inserción por punto, en orden (toMap de DataPoint, :169-182). sessionId: el de la sesión (o el del punto).
        private static void InsertPoints(SQLiteConnection connection, IReadOnlyList<DataPoint> points, long? sessionId)
        {
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                connection.Execute(InsertPoint, SessionRowMapper.PointArguments(point, sessionId ?? point.SessionId));
            }
        }

        // El lote se copia al encolarlo: el que llama puede seguir usando su lista.
        private static DataPoint[] Copy(IReadOnlyList<DataPoint> points)
        {
            var copy = new DataPoint[points.Count];
            for (var i = 0; i < copy.Length; i++)
            {
                copy[i] = points[i] ?? throw new ArgumentException("SqliteSessionRepository: un punto nulo.", nameof(points));
            }

            return copy;
        }
    }
}
