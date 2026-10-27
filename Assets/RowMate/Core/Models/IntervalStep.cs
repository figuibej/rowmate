namespace RowMate.Core.Models
{
    // Un paso de una rutina (F-RUT-02; legacy: lib/core/models/interval_step.dart:21-62, :147-180).
    // Obligatorios: rutina, orden (0-based) y tipo. El resto es opcional (null = no tiene) y entero, sin validación
    // (Q-25, AC-RUT-02-04): se aceptan 0, negativos y mínimo > máximo. Los textos de duración y objetivos están en
    // StepLabels (pasan por el catálogo). Igualdad por valor (record), como la comparación de campos que hace el legacy.
    public sealed record IntervalStep
    {
        public IntervalStep(long routineId, int order, StepType type)
        {
            RoutineId = routineId;
            Order = order;
            Type = type;
        }

        public long? Id { get; init; }

        public long RoutineId { get; init; }

        // step_order: posición en la plantilla (0-based).
        public int Order { get; init; }

        public StepType Type { get; init; }

        // Duración fija (s). Si está, el paso es por tiempo aunque también tenga distancia.
        public int? DurationSeconds { get; init; }

        // Distancia fija (m).
        public int? DistanceMeters { get; init; }

        public int? TargetWattsMin { get; init; }

        public int? TargetWattsMax { get; init; }

        // Paladas por minuto objetivo.
        public int? TargetSpm { get; init; }

        // Split objetivo (s / 500 m).
        public int? TargetSplitSeconds { get; init; }

        // Serie a la que pertenece (null = paso suelto). Los pasos CONSECUTIVOS con el mismo id forman un bloque.
        public string? GroupId { get; init; }

        // Repeticiones de la serie: manda la del primer paso del bloque; null = 1 (RoutineExpansion).
        public int? GroupRepeatCount { get; init; }

        // Incrementos por repetición (solo dentro de una serie y solo si el objetivo existe).
        public int? ProgressionSpm { get; init; }

        public int? ProgressionWatts { get; init; }

        public int? ProgressionSplitSeconds { get; init; }

        public bool IsTimeBased => DurationSeconds.HasValue;

        public bool IsDistanceBased => DistanceMeters.HasValue;

        // La duración manda aunque haya distancia (interval_step.dart:68-76).
        public StepGoal Goal => IsTimeBased ? StepGoal.Time : IsDistanceBased ? StepGoal.Distance : StepGoal.None;

        // = copyWith del legacy: null = conservar. Nunca borra un valor (AC-RUT-02-03); para borrar, usar `with`.
        public IntervalStep CopyWith(long? id = null, long? routineId = null, int? order = null, StepType? type = null,
            int? durationSeconds = null, int? distanceMeters = null, int? targetWattsMin = null, int? targetWattsMax = null,
            int? targetSpm = null, int? targetSplitSeconds = null, string? groupId = null, int? groupRepeatCount = null,
            int? progressionSpm = null, int? progressionWatts = null, int? progressionSplitSeconds = null)
        {
            return this with
            {
                Id = id ?? Id,
                RoutineId = routineId ?? RoutineId,
                Order = order ?? Order,
                Type = type ?? Type,
                DurationSeconds = durationSeconds ?? DurationSeconds,
                DistanceMeters = distanceMeters ?? DistanceMeters,
                TargetWattsMin = targetWattsMin ?? TargetWattsMin,
                TargetWattsMax = targetWattsMax ?? TargetWattsMax,
                TargetSpm = targetSpm ?? TargetSpm,
                TargetSplitSeconds = targetSplitSeconds ?? TargetSplitSeconds,
                GroupId = groupId ?? GroupId,
                GroupRepeatCount = groupRepeatCount ?? GroupRepeatCount,
                ProgressionSpm = progressionSpm ?? ProgressionSpm,
                ProgressionWatts = progressionWatts ?? ProgressionWatts,
                ProgressionSplitSeconds = progressionSplitSeconds ?? ProgressionSplitSeconds,
            };
        }
    }
}
