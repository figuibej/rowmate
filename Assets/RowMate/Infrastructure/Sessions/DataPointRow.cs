using SQLite;

namespace RowMate.Infrastructure.Sessions
{
    // Una fila de `data_points` (legacy: lib/core/models/workout_session.dart:136-183, DataPoint.toMap / fromMap;
    // lib/core/database/database_service.dart:123-138). step_type como texto, igual que el legacy (StepTypeNames).
    [Table("data_points")]
    internal sealed class DataPointRow
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("session_id")]
        public long SessionId { get; set; }

        [Column("elapsed_seconds")]
        public long ElapsedSeconds { get; set; }

        [Column("stroke_rate")]
        public double? StrokeRate { get; set; }

        [Column("stroke_count")]
        public long? StrokeCount { get; set; }

        [Column("distance_meters")]
        public long? DistanceMeters { get; set; }

        [Column("pace_500m_seconds")]
        public long? Pace500mSeconds { get; set; }

        [Column("power_watts")]
        public long? PowerWatts { get; set; }

        [Column("calories")]
        public long? Calories { get; set; }

        [Column("heart_rate")]
        public long? HeartRate { get; set; }

        [Column("step_index")]
        public long? StepIndex { get; set; }

        [Column("step_type")]
        public string? StepType { get; set; }
    }
}
