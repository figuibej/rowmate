using System;
using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // WorkoutSession ↔ fila de workout_sessions (legacy: workout_session.dart:39-67, toMap/fromMap).
    // - Al guardar, inicio y fin en UTC, ISO-8601 con 'Z', milisegundos siempre y microsegundos solo si no son 0
    //   (IsoDate.FormatUtc = toUtc().toIso8601String(), AC-RUT-16-04).
    // - Al leer, las fechas pasan a la hora local de localZone (IsoDate.Parse = DateTime.parse(...).toLocal()) y lo
    //   que falta vale 0 / 0.0 / null (lectura tolerante, AC-RUT-16-05).
    public static class WorkoutSessionCodec
    {
        public static WorkoutSessionRecord ToRecord(WorkoutSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return new WorkoutSessionRecord(IsoDate.FormatUtc(session.StartedAt))
            {
                Id = session.Id,
                RoutineId = session.RoutineId,
                RoutineName = session.RoutineName,
                FinishedAt = session.FinishedAt.HasValue ? IsoDate.FormatUtc(session.FinishedAt.Value) : null,
                TotalDistanceMeters = session.TotalDistanceMeters,
                TotalTimeSeconds = session.TotalTimeSeconds,
                AvgPowerWatts = session.AvgPowerWatts,
                AvgStrokeRate = session.AvgStrokeRate,
                TotalCalories = session.TotalCalories,
                StravaActivityId = session.StravaActivityId,
            };
        }

        public static WorkoutSession FromRecord(WorkoutSessionRecord record, TimeZoneInfo localZone)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            if (localZone == null)
            {
                throw new ArgumentNullException(nameof(localZone));
            }

            return new WorkoutSession(IsoDate.Parse(record.StartedAt, localZone))
            {
                Id = record.Id,
                RoutineId = record.RoutineId,
                RoutineName = record.RoutineName,
                FinishedAt = record.FinishedAt != null ? IsoDate.Parse(record.FinishedAt, localZone) : (DateTimeOffset?)null,
                TotalDistanceMeters = IntOrZero(record.TotalDistanceMeters, nameof(record.TotalDistanceMeters)),
                TotalTimeSeconds = IntOrZero(record.TotalTimeSeconds, nameof(record.TotalTimeSeconds)),
                AvgPowerWatts = IntOrZero(record.AvgPowerWatts, nameof(record.AvgPowerWatts)),
                AvgStrokeRate = record.AvgStrokeRate ?? 0.0,
                TotalCalories = IntOrZero(record.TotalCalories, nameof(record.TotalCalories)),
                StravaActivityId = record.StravaActivityId,
            };
        }

        // `as int? ?? 0`. Los enteros de Dart son de 64 bits y los del modelo de 32: un valor que no entra es un dato
        // corrupto y se avisa (OverflowException) en lugar de truncarlo en silencio.
        private static int IntOrZero(long? value, string column)
        {
            if (!value.HasValue)
            {
                return 0;
            }

            if (value.Value < int.MinValue || value.Value > int.MaxValue)
            {
                throw new OverflowException("WorkoutSessionRecord." + column + " = " + Fmt.Integer(value.Value) + " no entra en un int.");
            }

            return (int)value.Value;
        }
    }
}
