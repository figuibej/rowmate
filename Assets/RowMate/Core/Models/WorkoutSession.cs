using System;
using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // Sesión de entrenamiento (F-RUT-16; legacy: lib/core/models/workout_session.dart:2-37).
    // Rutina (id y nombre; los dos vacíos en entrenamiento libre), inicio (obligatorio), fin (vacío mientras está en
    // curso), totales en 0 por defecto e id de la actividad subida (vacío si no se subió). Las fechas llevan su offset
    // (DateTimeOffset, D-11): el inicio y el fin son instantes; al guardarse van en UTC (WorkoutSessionCodec).
    // "Potencia promedio" y "SPM promedio" guardan el P99 de SessionStats, no un promedio (Q-14, AC-RUT-16-02).
    public sealed record WorkoutSession
    {
        public WorkoutSession(DateTimeOffset startedAt)
        {
            StartedAt = startedAt;
        }

        public long? Id { get; init; }

        public long? RoutineId { get; init; }

        public string? RoutineName { get; init; }

        public DateTimeOffset StartedAt { get; init; }

        public DateTimeOffset? FinishedAt { get; init; }

        public int TotalDistanceMeters { get; init; }

        public int TotalTimeSeconds { get; init; }

        // P99 de la potencia (W), redondeado (Q-14).
        public int AvgPowerWatts { get; init; }

        // P99 de las paladas por minuto, sin redondear (Q-14).
        public double AvgStrokeRate { get; init; }

        public int TotalCalories { get; init; }

        public string? StravaActivityId { get; init; }

        // workout_session.dart:29-37 → TimeFormats.Elapsed (AC-RUT-16-03, AC-BLE-01-06): 59 → "00:59", 3661 → "1:01:01".
        public string DurationFormatted => TimeFormats.Elapsed(TotalTimeSeconds);

        // Cierre de la sesión con las estadísticas de su telemetría (legacy: lib/features/workout/workout_provider.dart:276-292):
        // conserva id, rutina e inicio; distancia, tiempo y calorías = los máximos; potencia y SPM "promedio" = P99;
        // fin = finishedAt. El id de la actividad subida NO se copia: el legacy arma una sesión nueva sin él.
        public WorkoutSession Closed(SessionStats stats, DateTimeOffset finishedAt)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return this with
            {
                FinishedAt = finishedAt,
                TotalDistanceMeters = stats.TotalDistance,
                TotalTimeSeconds = stats.TotalTimeSeconds,
                AvgPowerWatts = stats.P99Watts,
                AvgStrokeRate = stats.P99Spm,
                TotalCalories = stats.TotalCalories,
                StravaActivityId = null,
            };
        }
    }
}
