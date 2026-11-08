using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using SQLite;

namespace RowMate.Infrastructure.Sessions
{
    // La consulta del historial (F-HIS-06, F-HIS-07, F-HIS-19; 02-tech-design DT9-01 y §4 de la It. 9): dos lecturas en UNA
    // tarea del ejecutor de la base y el cálculo de las estadísticas ahí mismo, así el hilo principal recibe la lista lista.
    // 1. getSessions (database_service.dart:248-252): todas las sesiones, ORDER BY started_at DESC, con el id mayor primero
    //    en un empate (en el legacy el orden no está definido).
    // 2. getStatsForSessions (:273-321): los puntos de TODAS las sesiones (las columnas que usa SessionStats; heart_rate nulo
    //    = 0), ORDER BY session_id (usa idx_data_points_session), agrupados con una sola lista que se reusa, y
    //    SessionStats.Compute por sesión. Una sesión sin puntos queda con SessionStats.Empty (statsFor, history_provider.dart:20).
    // Esquema v8 sin migración: las sesiones de las It. 4a a 8 se leen tal cual (AC-HIS-19-03).
    internal static class HistoryQuery
    {
        public const string SelectSessions = "SELECT id, routine_id, routine_name, started_at, finished_at, total_distance_meters, "
            + "total_time_seconds, avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id FROM workout_sessions "
            + "ORDER BY started_at DESC, id DESC";

        public const string SelectPoints = "SELECT session_id, power_watts, stroke_rate, pace_500m_seconds, distance_meters, "
            + "elapsed_seconds, calories, heart_rate FROM data_points ORDER BY session_id";

        public static IReadOnlyList<HistoryEntry> Read(SQLiteConnection connection, TimeZoneInfo localZone)
        {
            var sessions = connection.Query<WorkoutSessionRow>(SelectSessions);
            var stats = new Dictionary<long, SessionStats>(sessions.Count);
            var rows = connection.Query<HistoryPointRow>(SelectPoints);
            var group = new List<DataPoint>();
            long current = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (group.Count > 0 && row.SessionId != current)
                {
                    stats[current] = SessionStats.Compute(group);
                    group.Clear();
                }

                current = row.SessionId;
                group.Add(ToPoint(row));
            }

            if (group.Count > 0)
            {
                stats[current] = SessionStats.Compute(group);
            }

            var entries = new HistoryEntry[sessions.Count];
            for (var i = 0; i < sessions.Count; i++)
            {
                var session = SessionRowMapper.ToSession(sessions[i], localZone);
                entries[i] = new HistoryEntry(session, stats.TryGetValue(sessions[i].Id, out var found) ? found : SessionStats.Empty);
            }

            return entries;
        }

        // :302-310: el punto con las columnas de las estadísticas (los nulos, 0).
        private static DataPoint ToPoint(HistoryPointRow row)
        {
            return new DataPoint(row.SessionId, checked((int)row.ElapsedSeconds))
            {
                PowerWatts = checked((int)(row.PowerWatts ?? 0)),
                StrokeRate = row.StrokeRate ?? 0.0,
                Pace500mSeconds = checked((int)(row.Pace500mSeconds ?? 0)),
                DistanceMeters = checked((int)(row.DistanceMeters ?? 0)),
                Calories = checked((int)(row.Calories ?? 0)),
                HeartRate = checked((int)(row.HeartRate ?? 0)),
            };
        }
    }

    // Una fila de data_points con las columnas de getStatsForSessions (database_service.dart:279-284).
    [Table("data_points")]
    internal sealed class HistoryPointRow
    {
        [Column("session_id")]
        public long SessionId { get; set; }

        [Column("power_watts")]
        public long? PowerWatts { get; set; }

        [Column("stroke_rate")]
        public double? StrokeRate { get; set; }

        [Column("pace_500m_seconds")]
        public long? Pace500mSeconds { get; set; }

        [Column("distance_meters")]
        public long? DistanceMeters { get; set; }

        [Column("elapsed_seconds")]
        public long ElapsedSeconds { get; set; }

        [Column("calories")]
        public long? Calories { get; set; }

        [Column("heart_rate")]
        public long? HeartRate { get; set; }
    }
}
