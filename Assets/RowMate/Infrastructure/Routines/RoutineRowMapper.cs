using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Infrastructure.Routines
{
    // Fila ↔ modelo (legacy: Routine.fromMap/toMap, lib/core/models/routine.dart:107-122; IntervalStep.fromMap/toMap,
    // interval_step.dart:111-145). Una fila que no se puede leer hace fallar la carga ENTERA (P-07, AC-RUT-08-08):
    // - type con StepTypeNames.Parse: un nombre desconocido ("sprint") es FormatException (interval_step.dart:133);
    // - cada entero pasa a int con checked: fuera de −2 147 483 648 … 2 147 483 647 es OverflowException (DT-10);
    // - created_at con IsoDate.Parse en UTC (la app guarda yyyy-MM-ddTHH:mm:ss.fffZ, Q-18; acepta otros formatos ISO).
    internal static class RoutineRowMapper
    {
        public static Routine ToRoutine(RoutineRow row, IReadOnlyList<IntervalStep> steps)
        {
            if (row.Name == null)
            {
                throw new FormatException("Rutina " + Fmt.Integer(row.Id) + " sin nombre.");
            }

            if (row.CreatedAt == null)
            {
                throw new FormatException("Rutina " + Fmt.Integer(row.Id) + " sin fecha de creación.");
            }

            return new Routine(row.Name, IsoDate.Parse(row.CreatedAt, TimeZoneInfo.Utc))
            {
                Id = row.Id,
                Description = row.Description ?? "",
                Steps = steps,
            };
        }

        public static IntervalStep ToStep(IntervalStepRow row)
        {
            return new IntervalStep(row.RoutineId, ToInt(row.StepOrder), StepTypeNames.Parse(row.Type!))
            {
                Id = row.Id,
                DurationSeconds = ToInt(row.DurationSeconds),
                DistanceMeters = ToInt(row.DistanceMeters),
                TargetWattsMin = ToInt(row.TargetWattsMin),
                TargetWattsMax = ToInt(row.TargetWattsMax),
                TargetSpm = ToInt(row.TargetSpm),
                TargetSplitSeconds = ToInt(row.TargetSplitSeconds),
                GroupId = row.GroupId,
                GroupRepeatCount = ToInt(row.GroupRepeatCount),
                ProgressionSpm = ToInt(row.ProgressionSpm),
                ProgressionWatts = ToInt(row.ProgressionWatts),
                ProgressionSplitSeconds = ToInt(row.ProgressionSplitSeconds),
            };
        }

        // created_at de una rutina nueva: ISO-8601 en UTC con 3 decimales SIEMPRE (2026-10-27T13:00:00.000Z), así el orden
        // del texto es el del tiempo (DT-08). La fecha se trunca al milisegundo.
        public static DateTimeOffset StoredCreatedAt(DateTimeOffset createdAt)
        {
            var utc = createdAt.ToUniversalTime();
            return new DateTimeOffset(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, TimeSpan.Zero);
        }

        public static string CreatedAtText(DateTimeOffset createdAt)
        {
            return IsoDate.FormatUtc(StoredCreatedAt(createdAt));
        }

        // Los 14 atributos de AC-RUT-02-01 como parámetros de INSERT INTO interval_steps (…) en el orden de StepColumns, con
        // routine_id y step_order. null = NULL; 0 = 0.
        public const string StepColumns = "routine_id, step_order, type, duration_seconds, distance_meters, target_watts_min, "
            + "target_watts_max, target_spm, target_split_seconds, group_id, group_repeat_count, progression_spm, progression_watts, "
            + "progression_split_seconds";

        public static object?[] StepArguments(IntervalStep step, long routineId, int order)
        {
            return new object?[]
            {
                routineId,
                order,
                StepTypeNames.ToStorage(step.Type),
                step.DurationSeconds,
                step.DistanceMeters,
                step.TargetWattsMin,
                step.TargetWattsMax,
                step.TargetSpm,
                step.TargetSplitSeconds,
                step.GroupId,
                step.GroupRepeatCount,
                step.ProgressionSpm,
                step.ProgressionWatts,
                step.ProgressionSplitSeconds,
            };
        }

        private static int ToInt(long value)
        {
            return checked((int)value);
        }

        private static int? ToInt(long? value)
        {
            return value.HasValue ? checked((int)value.Value) : (int?)null;
        }
    }
}
