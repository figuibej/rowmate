using System;
using System.Collections.Generic;

namespace RowMate.Core.Models
{
    // Expansión de las series de una plantilla (F-RUT-06) y posición de cada paso expandido (F-RUT-07).
    // legacy: lib/core/models/routine.dart:29-70 (flattenedSteps) y :74-97 (flattenedStepPositions).
    // - Un bloque = pasos CONSECUTIVOS con el mismo GroupId (el mismo id más adelante es otro bloque, AC-RUT-06-11).
    // - repeat = GroupRepeatCount ?? 1 del PRIMER paso del bloque (AC-RUT-06-10); repeat ≤ 0 → ningún paso (AC-RUT-06-08).
    // - En la repetición r (desde 0), cada objetivo con progresión vale objetivo + progresión · r, sin piso
    //   (AC-RUT-06-12); si falta el objetivo o la progresión, queda como estaba (AC-RUT-06-06). Lo demás se conserva
    //   (AC-RUT-06-13). Los pasos sueltos no se progresan (AC-RUT-06-07).
    // - Las posiciones siguen el mismo recorrido: (gid, r, repeat) con r desde 1; suelto (null, 1, 1). Siempre del mismo
    //   largo y orden que Flatten (AC-RUT-07-05).
    public static class RoutineExpansion
    {
        public static IReadOnlyList<IntervalStep> Flatten(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var result = new List<IntervalStep>(template.Count);
            var i = 0;
            while (i < template.Count)
            {
                var step = template[i];
                if (step.GroupId == null)
                {
                    result.Add(step);
                    i++;
                    continue;
                }

                var gid = step.GroupId;
                var repeat = step.GroupRepeatCount ?? 1;
                var start = i;
                while (i < template.Count && template[i].GroupId == gid)
                {
                    i++;
                }

                for (var r = 0; r < repeat; r++)
                {
                    for (var k = start; k < i; k++)
                    {
                        result.Add(Progress(template[k], r));
                    }
                }
            }

            return result;
        }

        public static IReadOnlyList<StepPosition> Positions(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var result = new List<StepPosition>(template.Count);
            var i = 0;
            while (i < template.Count)
            {
                var gid = template[i].GroupId;
                if (gid == null)
                {
                    result.Add(StepPosition.Loose);
                    i++;
                    continue;
                }

                var repeat = template[i].GroupRepeatCount ?? 1;
                var size = 0;
                while (i < template.Count && template[i].GroupId == gid)
                {
                    size++;
                    i++;
                }

                for (var r = 1; r <= repeat; r++)
                {
                    for (var k = 0; k < size; k++)
                    {
                        result.Add(new StepPosition(gid, r, repeat));
                    }
                }
            }

            return result;
        }

        // routine.dart:50-63. Los enteros de Dart son de 64 bits: si la cuenta no entra en un int de C#, se avisa
        // (OverflowException) en lugar de dar la vuelta en silencio.
        private static IntervalStep Progress(IntervalStep step, int r)
        {
            return step with
            {
                TargetSpm = Apply(step.TargetSpm, step.ProgressionSpm, r),
                TargetWattsMin = Apply(step.TargetWattsMin, step.ProgressionWatts, r),
                TargetWattsMax = Apply(step.TargetWattsMax, step.ProgressionWatts, r),
                TargetSplitSeconds = Apply(step.TargetSplitSeconds, step.ProgressionSplitSeconds, r),
            };
        }

        private static int? Apply(int? target, int? progression, int r)
        {
            if (!target.HasValue || !progression.HasValue)
            {
                return target;
            }

            return checked(target.Value + progression.Value * r);
        }
    }
}
