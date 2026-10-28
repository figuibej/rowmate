using SQLite;

namespace RowMate.Infrastructure.Routines
{
    // Una fila de `interval_steps`. Los enteros se leen como long? (los de SQLite son de 64 bits, como los de Dart) y pasan
    // a int con `checked` en RoutineRowMapper: un valor que no entra hace fallar la carga entera (DT-10, AC-RUT-08-08).
    // null = NULL; 0 = 0 (AC-RUT-08-01).
    [Table("interval_steps")]
    internal sealed class IntervalStepRow
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("routine_id")]
        public long RoutineId { get; set; }

        [Column("step_order")]
        public long StepOrder { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("duration_seconds")]
        public long? DurationSeconds { get; set; }

        [Column("distance_meters")]
        public long? DistanceMeters { get; set; }

        [Column("target_watts_min")]
        public long? TargetWattsMin { get; set; }

        [Column("target_watts_max")]
        public long? TargetWattsMax { get; set; }

        [Column("target_spm")]
        public long? TargetSpm { get; set; }

        [Column("target_split_seconds")]
        public long? TargetSplitSeconds { get; set; }

        [Column("group_id")]
        public string? GroupId { get; set; }

        [Column("group_repeat_count")]
        public long? GroupRepeatCount { get; set; }

        [Column("progression_spm")]
        public long? ProgressionSpm { get; set; }

        [Column("progression_watts")]
        public long? ProgressionWatts { get; set; }

        [Column("progression_split_seconds")]
        public long? ProgressionSplitSeconds { get; set; }
    }
}
