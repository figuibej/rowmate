using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Routines
{
    // Operaciones puras sobre la plantilla del editor (legacy: lib/features/routines/routine_editor_screen.dart:75-200).
    // Cada una devuelve una lista nueva y no toca la que recibe. Como en el legacy:
    // - las operaciones de serie van por GroupId (firstWhere, lastIndexWhere, removeWhere): con ids únicos (los que da
    //   el editor) es lo mismo que "por bloque"; con un id repetido en dos bloques (solo a mano) cambian juntos (DT-14);
    // - el Order de cada paso se pone donde lo pone el legacy (agregar al final, reemplazar, la serie por defecto) y la
    //   numeración definitiva 0…n−1 la hace Renumber al guardar (:56-60).
    public static class RoutineTemplate
    {
        // :115-130: Trabajo 120 s + Descanso 60 s, ×3, sin objetivos.
        public const int DefaultSeriesWorkSeconds = 120;
        public const int DefaultSeriesRestSeconds = 60;
        public const int DefaultSeriesRepeatCount = 3;

        // :80-86 (+ Paso → Confirmar): al final, como paso suelto (aunque lo último sea una serie: queda después de su pie).
        public static IReadOnlyList<IntervalStep> AppendLoose(IReadOnlyList<IntervalStep> template, IntervalStep step)
        {
            Check(template);
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var result = new List<IntervalStep>(template.Count + 1);
            result.AddRange(template);
            result.Add(step with { Order = template.Count, GroupId = null, GroupRepeatCount = null });
            return result;
        }

        // :110-133 (Serie): dos pasos de ejemplo al final, con el id que da SeriesIdGenerator.
        public static IReadOnlyList<IntervalStep> AppendDefaultSeries(IReadOnlyList<IntervalStep> template, string groupId)
        {
            Check(template);
            CheckGroup(groupId);
            var result = new List<IntervalStep>(template.Count + 2);
            result.AddRange(template);
            result.Add(new IntervalStep(0, template.Count, StepType.Work)
            {
                DurationSeconds = DefaultSeriesWorkSeconds,
                GroupId = groupId,
                GroupRepeatCount = DefaultSeriesRepeatCount,
            });
            result.Add(new IntervalStep(0, template.Count + 1, StepType.Rest)
            {
                DurationSeconds = DefaultSeriesRestSeconds,
                GroupId = groupId,
                GroupRepeatCount = DefaultSeriesRepeatCount,
            });
            return result;
        }

        // :135-149 (+ Agregar paso a serie → Confirmar): después del último paso con ese id (lastIndexWhere + 1), con las
        // repeticiones del primero (firstWhere). El Order queda como viene del diálogo, como en el legacy.
        public static IReadOnlyList<IntervalStep> InsertIntoSeries(IReadOnlyList<IntervalStep> template, string groupId, IntervalStep step)
        {
            Check(template);
            CheckGroup(groupId);
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var last = LastIndexOf(template, groupId);
            if (last < 0)
            {
                throw new ArgumentException("RoutineTemplate: no hay ninguna serie \"" + groupId + "\".", nameof(groupId));
            }

            var repeat = template[FirstIndexOf(template, groupId)].GroupRepeatCount;
            var result = new List<IntervalStep>(template.Count + 1);
            result.AddRange(template);
            result.Insert(last + 1, step.CopyWith(groupId: groupId, groupRepeatCount: repeat));
            return result;
        }

        // :98-106 (lápiz → Confirmar): en su lugar, conservando su serie y sus repeticiones (copyWith: null = conservar, así
        // un paso suelto sigue suelto).
        public static IReadOnlyList<IntervalStep> Replace(IReadOnlyList<IntervalStep> template, int index, IntervalStep step)
        {
            CheckIndex(template, index);
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var original = template[index];
            var result = new List<IntervalStep>(template);
            result[index] = step.CopyWith(order: index, groupId: original.GroupId, groupRepeatCount: original.GroupRepeatCount);
            return result;
        }

        // :190-194 (cruz): al instante, sin confirmación.
        public static IReadOnlyList<IntervalStep> RemoveAt(IReadOnlyList<IntervalStep> template, int index)
        {
            CheckIndex(template, index);
            var result = new List<IntervalStep>(template);
            result.RemoveAt(index);
            return result;
        }

        // :196-200 (tacho): removeWhere(groupId).
        public static IReadOnlyList<IntervalStep> RemoveSeries(IReadOnlyList<IntervalStep> template, string groupId)
        {
            Check(template);
            CheckGroup(groupId);
            var result = new List<IntervalStep>(template.Count);
            for (var i = 0; i < template.Count; i++)
            {
                if (template[i].GroupId != groupId)
                {
                    result.Add(template[i]);
                }
            }

            return result;
        }

        // :179-186: ×N en todos los pasos con ese id. Las reglas del texto y los topes están en RoutineEditorStore.
        public static IReadOnlyList<IntervalStep> SetRepeatCount(IReadOnlyList<IntervalStep> template, string groupId, int repeatCount)
        {
            Check(template);
            CheckGroup(groupId);
            var result = new List<IntervalStep>(template.Count);
            for (var i = 0; i < template.Count; i++)
            {
                var step = template[i];
                result.Add(step.GroupId == groupId ? step with { GroupRepeatCount = repeatCount } : step);
            }

            return result;
        }

        // :153 y :372-373: firstWhere(groupId).groupRepeatCount ?? 1.
        public static int RepeatCountOf(IReadOnlyList<IntervalStep> template, string groupId)
        {
            Check(template);
            CheckGroup(groupId);
            var first = FirstIndexOf(template, groupId);
            if (first < 0)
            {
                throw new ArgumentException("RoutineTemplate: no hay ninguna serie \"" + groupId + "\".", nameof(groupId));
            }

            return template[first].GroupRepeatCount ?? 1;
        }

        // :56-60 (al guardar): order = posición y routineId = el de la rutina (0 si es nueva).
        public static IReadOnlyList<IntervalStep> Renumber(IReadOnlyList<IntervalStep> template, long routineId)
        {
            Check(template);
            var result = new IntervalStep[template.Count];
            for (var i = 0; i < template.Count; i++)
            {
                result[i] = template[i] with { Order = i, RoutineId = routineId };
            }

            return result;
        }

        // lastIndexWhere((s) => s.groupId == groupId); −1 si no hay.
        public static int LastIndexOf(IReadOnlyList<IntervalStep> template, string groupId)
        {
            Check(template);
            for (var i = template.Count - 1; i >= 0; i--)
            {
                if (template[i].GroupId == groupId)
                {
                    return i;
                }
            }

            return -1;
        }

        // firstWhere((s) => s.groupId == groupId) como índice; −1 si no hay.
        public static int FirstIndexOf(IReadOnlyList<IntervalStep> template, string groupId)
        {
            Check(template);
            for (var i = 0; i < template.Count; i++)
            {
                if (template[i].GroupId == groupId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Check(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
        }

        private static void CheckGroup(string groupId)
        {
            if (groupId == null)
            {
                throw new ArgumentNullException(nameof(groupId));
            }
        }

        private static void CheckIndex(IReadOnlyList<IntervalStep> template, int index)
        {
            Check(template);
            if (index < 0 || index >= template.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "RoutineTemplate: índice fuera de la plantilla.");
            }
        }
    }
}
