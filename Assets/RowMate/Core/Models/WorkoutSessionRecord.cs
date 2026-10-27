using System;

namespace RowMate.Core.Models
{
    // Fila de la tabla workout_sessions tal como la guarda el legacy (workout_session.dart:39-51, toMap): el DTO de la
    // It. 3 la mapea 1 a 1. Columnas: id, routine_id, routine_name, started_at, finished_at, total_distance_meters,
    // total_time_seconds, avg_power_watts, avg_stroke_rate, total_calories, strava_activity_id. Las fechas van como
    // texto ISO-8601 en UTC con 'Z'. Todo es anulable (lectura tolerante, AC-RUT-16-05) salvo started_at.
    // QA-05: C# 9 no tiene `required`; la única columna obligatoria entra por el constructor
    // (legacy: `map['started_at'] as String`, workout_session.dart:57).
    public sealed record WorkoutSessionRecord
    {
        private string _startedAt;

        public WorkoutSessionRecord(string startedAt)
        {
            _startedAt = startedAt ?? throw new ArgumentNullException(nameof(startedAt));
        }

        public long? Id { get; init; }

        public long? RoutineId { get; init; }

        public string? RoutineName { get; init; }

        public string StartedAt
        {
            get => _startedAt;
            init => _startedAt = value ?? throw new ArgumentNullException(nameof(StartedAt));
        }

        public string? FinishedAt { get; init; }

        public long? TotalDistanceMeters { get; init; }

        public long? TotalTimeSeconds { get; init; }

        public long? AvgPowerWatts { get; init; }

        public double? AvgStrokeRate { get; init; }

        public long? TotalCalories { get; init; }

        public string? StravaActivityId { get; init; }
    }
}
