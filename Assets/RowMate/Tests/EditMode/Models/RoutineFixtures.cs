using System;
using System.Collections.Generic;
using System.Text;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // Plantillas de prueba de F-RUT-05/-06/-07 (01-po-spec §4.10-§4.12, casos de RUT §5.3). Son las MISMAS que usa el
    // oráculo (tools/oracle/bin/routine_vectors.dart, sección "templates"): RoutineExpansionTests.FixturesMatchOracle
    // lo verifica campo por campo. Clase y no record (QA-03).
    public static class RoutineFixtures
    {
        // Como DateTime(2026) del test legacy (test/routine_positions_test.dart:9); en UTC para no depender del equipo.
        public static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private const StepType W = StepType.Warmup;
        private const StepType K = StepType.Work;
        private const StepType R = StepType.Rest;
        private const StepType C = StepType.Cooldown;

        // Paso de plantilla con routineId 1 (el order lo pone Template).
        public static IntervalStep Step(StepType type, int? dur = null, int? dist = null, int? wMin = null, int? wMax = null, int? spm = null,
            int? split = null, string? g = null, int? rep = null, int? pSpm = null, int? pW = null, int? pSplit = null)
        {
            return new IntervalStep(1, 0, type)
            {
                DurationSeconds = dur,
                DistanceMeters = dist,
                TargetWattsMin = wMin,
                TargetWattsMax = wMax,
                TargetSpm = spm,
                TargetSplitSeconds = split,
                GroupId = g,
                GroupRepeatCount = rep,
                ProgressionSpm = pSpm,
                ProgressionWatts = pW,
                ProgressionSplitSeconds = pSplit,
            };
        }

        // Plantilla con order = posición (0-based).
        public static IReadOnlyList<IntervalStep> Template(params IntervalStep[] steps)
        {
            var result = new IntervalStep[steps.Length];
            for (var i = 0; i < steps.Length; i++)
            {
                result[i] = steps[i] with { Order = i };
            }

            return result;
        }

        public static Routine MakeRoutine(string name, IReadOnlyList<IntervalStep> steps)
        {
            return new Routine(name, CreatedAt) { Steps = steps };
        }

        // T1 (la del test legacy): calentamiento 300 s · serie "a" ×3 [trabajo 500 m, enfriamiento 60 s] · enfriamiento 300 s.
        public static IReadOnlyList<IntervalStep> T1 => Named["T1"];

        // Todas las plantillas con nombre, en el orden del oráculo.
        public static IReadOnlyDictionary<string, IReadOnlyList<IntervalStep>> Named { get; } = BuildNamed();

        // Nombres de las plantillas de F-RUT-06 (01-po-spec §4.11): las que tienen que quedar alineadas (AC-RUT-07-05).
        public static readonly string[] ExpansionTemplates =
        {
            "T1", "case1", "case2", "case3", "case4", "case5", "case6", "case7a", "case7b", "case7c", "case7d", "case8",
            "rut04_05", "rut06_10", "rut06_11", "rut06_12", "rut06_13",
        };

        // Las 9 plantillas de AC-RUT-05-02, en el orden de la tabla.
        public static readonly string[] SummaryTemplates =
        {
            "sum_warmup", "sum_warmup_cooldown", "sum_rest_rest", "sum_work", "sum_warmup_series3_cooldown", "sum_2work_1rest",
            "sum_1work_2rest", "sum_4work_3rest", "sum_empty",
        };

        private static Dictionary<string, IReadOnlyList<IntervalStep>> BuildNamed()
        {
            return new Dictionary<string, IReadOnlyList<IntervalStep>>(StringComparer.Ordinal)
            {
                { "T1", Template(Step(W, dur: 300), Step(K, dist: 500, g: "a", rep: 3), Step(C, dur: 60, g: "a", rep: 3), Step(C, dur: 300)) },
                { "case1", Template(Step(K, dur: 300, spm: 18, pSpm: 2, g: "a", rep: 3), Step(R, dur: 120, g: "a", rep: 3)) },
                { "case2", Template(Step(K, dur: 300, wMin: 140, wMax: 160, pW: 10, g: "a", rep: 3)) },
                { "case3", Template(Step(K, dur: 300, wMin: 150, spm: 22, pW: 10, pSpm: 2, g: "a", rep: 3)) },
                { "case4", Template(Step(K, dur: 300, split: 120, pSplit: -2, g: "a", rep: 3)) },
                { "case5", Template(Step(K, dur: 300, pSpm: 2, g: "a", rep: 3)) },
                { "case6", Template(Step(K, dur: 300, spm: 20, pSpm: 2)) },
                { "case7a", Template(Step(K, dur: 60, g: "a")) },
                { "case7b", Template(Step(W, dur: 60), Step(K, dur: 60, g: "a", rep: 0), Step(C, dur: 60)) },
                { "case7c", Template(Step(K, dur: 60, g: "a", rep: -1)) },
                { "case7d", Template(Step(K, dur: 60, g: "a", rep: 0)) },
                { "case8", Template(Step(K, dur: 60, g: "a", rep: 2), Step(K, dur: 30, g: "b", rep: 3)) },
                { "rut04_05", Template(Step(K, dur: 300, wMin: 140, wMax: 160, pW: 10, g: "a", rep: 2)) },
                { "rut06_10", Template(Step(K, dur: 60, g: "a", rep: 2), Step(R, dur: 30, g: "a", rep: 5)) },
                { "rut06_11", Template(Step(K, dur: 60, g: "a", rep: 2), Step(R, dur: 30), Step(K, dur: 60, g: "a", rep: 2)) },
                { "rut06_12", Template(Step(K, dur: 60, spm: 4, pSpm: -3, g: "a", rep: 3)) },
                {
                    "rut06_13", Template(
                        Step(K, dur: 90, dist: 400, wMin: 100, wMax: 120, spm: 20, split: 130, g: "g", rep: 2, pSpm: 1, pW: 5, pSplit: -1),
                        Step(R, dist: 250, g: "g", rep: 2, pSpm: 3, pW: -4, pSplit: 2))
                },
                { "sum_warmup", Template(Step(W, dur: 300)) },
                { "sum_warmup_cooldown", Template(Step(W, dur: 300), Step(C, dur: 300)) },
                { "sum_rest_rest", Template(Step(R, dur: 60), Step(R, dur: 60)) },
                { "sum_work", Template(Step(K, dur: 60)) },
                {
                    "sum_warmup_series3_cooldown", Template(Step(W, dur: 300), Step(K, dist: 500, g: "a", rep: 3), Step(R, dur: 60, g: "a", rep: 3),
                        Step(C, dur: 300))
                },
                { "sum_2work_1rest", Template(Step(K, dur: 60), Step(K, dur: 60), Step(R, dur: 60)) },
                { "sum_1work_2rest", Template(Step(K, dur: 60), Step(R, dur: 60), Step(R, dur: 60)) },
                {
                    "sum_4work_3rest", Template(Step(K, dur: 60), Step(R, dur: 30), Step(K, dur: 60), Step(R, dur: 30), Step(K, dur: 60),
                        Step(R, dur: 30), Step(K, dur: 60))
                },
                { "sum_empty", Template() },
            };
        }

        // ---- Lectura de los vectores del oráculo (routines.json) ----

        // IntervalStep desde IntervalStep.toMap() del legacy (interval_step.dart:111-127).
        public static IntervalStep FromMap(JsonNode map)
        {
            var step = new IntervalStep(map["routine_id"].AsLong(), (int)map["step_order"].AsLong(), StepTypeNames.Parse(map["type"].AsString()))
            {
                DurationSeconds = IntOrNull(map["duration_seconds"]),
                DistanceMeters = IntOrNull(map["distance_meters"]),
                TargetWattsMin = IntOrNull(map["target_watts_min"]),
                TargetWattsMax = IntOrNull(map["target_watts_max"]),
                TargetSpm = IntOrNull(map["target_spm"]),
                TargetSplitSeconds = IntOrNull(map["target_split_seconds"]),
                GroupId = map["group_id"].IsNull ? null : map["group_id"].AsString(),
                GroupRepeatCount = IntOrNull(map["group_repeat_count"]),
                ProgressionSpm = IntOrNull(map["progression_spm"]),
                ProgressionWatts = IntOrNull(map["progression_watts"]),
                ProgressionSplitSeconds = IntOrNull(map["progression_split_seconds"]),
            };
            if (map.TryGet("id", out var id) && !id.IsNull)
            {
                step = step with { Id = id.AsLong() };
            }

            return step;
        }

        // Paso desde una fila de fuzzTemplates / fuzzFlattened (routineId 1).
        public static IntervalStep FromRow(GoldenRow row)
        {
            return new IntervalStep(1, (int)row.Int("order"), StepTypeNames.Parse(row.Text("type")))
            {
                DurationSeconds = IntOrNull(row.Cell("durationSeconds")),
                DistanceMeters = IntOrNull(row.Cell("distanceMeters")),
                TargetWattsMin = IntOrNull(row.Cell("targetWattsMin")),
                TargetWattsMax = IntOrNull(row.Cell("targetWattsMax")),
                TargetSpm = IntOrNull(row.Cell("targetSpm")),
                TargetSplitSeconds = IntOrNull(row.Cell("targetSplitSeconds")),
                GroupId = row.Cell("groupId").IsNull ? null : row.Text("groupId"),
                GroupRepeatCount = IntOrNull(row.Cell("groupRepeatCount")),
                ProgressionSpm = IntOrNull(row.Cell("progressionSpm")),
                ProgressionWatts = IntOrNull(row.Cell("progressionWatts")),
                ProgressionSplitSeconds = IntOrNull(row.Cell("progressionSplitSeconds")),
            };
        }

        // StepPosition desde [groupId, rep, repCount].
        public static StepPosition PositionFrom(JsonNode triple)
        {
            var items = triple.Items;
            return new StepPosition(items[0].IsNull ? null : items[0].AsString(), (int)items[1].AsLong(), (int)items[2].AsLong());
        }

        public static IReadOnlyList<IntervalStep> StepsFrom(JsonNode array)
        {
            var result = new List<IntervalStep>();
            foreach (var item in array.Items)
            {
                result.Add(FromMap(item));
            }

            return result;
        }

        // Texto legible de un paso para los mensajes de diferencias.
        public static string Describe(IntervalStep s)
        {
            var sb = new StringBuilder();
            sb.Append(StepTypeNames.ToStorage(s.Type)).Append("#").Append(Fmt.Integer(s.Order));
            Add(sb, "dur", s.DurationSeconds);
            Add(sb, "dist", s.DistanceMeters);
            Add(sb, "wMin", s.TargetWattsMin);
            Add(sb, "wMax", s.TargetWattsMax);
            Add(sb, "spm", s.TargetSpm);
            Add(sb, "split", s.TargetSplitSeconds);
            if (s.GroupId != null)
            {
                sb.Append(" g=").Append(s.GroupId);
            }

            Add(sb, "rep", s.GroupRepeatCount);
            Add(sb, "pSpm", s.ProgressionSpm);
            Add(sb, "pW", s.ProgressionWatts);
            Add(sb, "pSplit", s.ProgressionSplitSeconds);
            return sb.ToString();
        }

        private static void Add(StringBuilder sb, string name, int? value)
        {
            if (value.HasValue)
            {
                sb.Append(' ').Append(name).Append('=').Append(Fmt.Integer(value.Value));
            }
        }

        private static int? IntOrNull(JsonNode node)
        {
            return node.IsNull ? (int?)null : (int)node.AsLong();
        }
    }
}
