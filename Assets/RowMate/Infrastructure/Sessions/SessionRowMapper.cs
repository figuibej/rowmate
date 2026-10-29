using System;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Infrastructure.Sessions
{
    // Fila ↔ modelo de las sesiones y los puntos (02-tech-design §3.3 de la It. 4a). La sesión pasa por WorkoutSessionCodec
    // (It. 1): fechas con IsoDate.FormatUtc al escribir (2026-10-28T13:00:12.300Z) y IsoDate.Parse a la hora local al leer
    // (AC-HIS-19-01). Un entero que no entra en un int es un dato corrupto: OverflowException (DT-10, como las rutinas).
    internal static class SessionRowMapper
    {
        // Las columnas de INSERT INTO data_points (…), en el orden de PointArguments (toMap de DataPoint, :169-182).
        public const string PointColumns = "session_id, elapsed_seconds, stroke_rate, stroke_count, distance_meters, pace_500m_seconds, "
            + "power_watts, calories, heart_rate, step_index, step_type";

        public static object?[] PointArguments(DataPoint point, long sessionId)
        {
            return new object?[]
            {
                sessionId,
                point.ElapsedSeconds,
                point.StrokeRate,
                point.StrokeCount,
                point.DistanceMeters,
                point.Pace500mSeconds,
                point.PowerWatts,
                point.Calories,
                point.HeartRate,
                point.StepIndex,
                point.StepTypeName,
            };
        }

        public static DataPoint ToPoint(DataPointRow row)
        {
            return new DataPoint(row.SessionId, ToInt(row.ElapsedSeconds))
            {
                Id = row.Id,
                StrokeRate = row.StrokeRate ?? 0.0,
                StrokeCount = ToInt(row.StrokeCount ?? 0),
                DistanceMeters = ToInt(row.DistanceMeters ?? 0),
                Pace500mSeconds = ToInt(row.Pace500mSeconds ?? 0),
                PowerWatts = ToInt(row.PowerWatts ?? 0),
                Calories = ToInt(row.Calories ?? 0),
                HeartRate = ToInt(row.HeartRate ?? 0),
                StepIndex = row.StepIndex.HasValue ? ToInt(row.StepIndex.Value) : (int?)null,
                StepTypeName = row.StepType,
            };
        }

        public static WorkoutSession ToSession(WorkoutSessionRow row, TimeZoneInfo localZone)
        {
            if (row.StartedAt == null)
            {
                throw new FormatException("Sesión " + Fmt.Integer(row.Id) + " sin fecha de inicio.");
            }

            var record = new WorkoutSessionRecord(row.StartedAt)
            {
                Id = row.Id,
                RoutineId = row.RoutineId,
                RoutineName = row.RoutineName,
                FinishedAt = row.FinishedAt,
                TotalDistanceMeters = row.TotalDistanceMeters,
                TotalTimeSeconds = row.TotalTimeSeconds,
                AvgPowerWatts = row.AvgPowerWatts,
                AvgStrokeRate = row.AvgStrokeRate,
                TotalCalories = row.TotalCalories,
                StravaActivityId = row.StravaActivityId,
            };
            return WorkoutSessionCodec.FromRecord(record, localZone);
        }

        public static SessionListing ToListing(SessionListingRow row, TimeZoneInfo localZone)
        {
            if (row.StartedAt == null)
            {
                throw new FormatException("Sesión " + Fmt.Integer(row.Id) + " sin fecha de inicio.");
            }

            return new SessionListing(row.Id, IsoDate.Parse(row.StartedAt, localZone),
                row.FinishedAt != null ? IsoDate.Parse(row.FinishedAt, localZone) : (DateTimeOffset?)null, row.RoutineName,
                row.Origin ?? "app", row.PointCount);
        }

        private static int ToInt(long value)
        {
            return checked((int)value);
        }
    }
}
