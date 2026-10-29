using SQLite;

namespace RowMate.Infrastructure.Sessions
{
    // Una fila de `workout_sessions` como la lee sqlite-net (Query<T> genérico: sin MakeGenericMethod en IL2CPP). Los enteros
    // en long? (los de SQLite son de 64 bits, como los de Dart) y la conversión checked de la It. 3 en WorkoutSessionCodec.
    // legacy: lib/core/models/workout_session.dart:39-67 (toMap / fromMap); lib/core/database/database_service.dart:106-121.
    [Table("workout_sessions")]
    internal sealed class WorkoutSessionRow
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("routine_id")]
        public long? RoutineId { get; set; }

        [Column("routine_name")]
        public string? RoutineName { get; set; }

        [Column("started_at")]
        public string? StartedAt { get; set; }

        [Column("finished_at")]
        public string? FinishedAt { get; set; }

        [Column("total_distance_meters")]
        public long? TotalDistanceMeters { get; set; }

        [Column("total_time_seconds")]
        public long? TotalTimeSeconds { get; set; }

        [Column("avg_power_watts")]
        public long? AvgPowerWatts { get; set; }

        [Column("avg_stroke_rate")]
        public double? AvgStrokeRate { get; set; }

        [Column("total_calories")]
        public long? TotalCalories { get; set; }

        [Column("strava_activity_id")]
        public string? StravaActivityId { get; set; }
    }

    // Una fila de la lista de la herramienta de la demo (ListAsync): la sesión, su origen y la cantidad de puntos.
    [Table("workout_sessions")]
    internal sealed class SessionListingRow
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("started_at")]
        public string? StartedAt { get; set; }

        [Column("finished_at")]
        public string? FinishedAt { get; set; }

        [Column("routine_name")]
        public string? RoutineName { get; set; }

        [Column("origin")]
        public string? Origin { get; set; }

        [Column("point_count")]
        public long PointCount { get; set; }
    }
}
