using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Routines
{
    // Tramos de la mini-barra de una tarjeta (AC-RUT-10-11, Mejora EXR de ART_BIBLE §3.7): un tramo por paso de la rutina
    // EXPANDIDA, en orden. Total los cuenta sin expandir (con repeat ≤ 0 → 0 y saturación: nunca desborda) y Leading trae
    // como mucho `max` tipos, así ×2 000 000 000 no hace un bucle por repetición ni reserva memoria de más.
    public readonly struct StructureSegments
    {
        public StructureSegments(long total, IReadOnlyList<StepType> leading)
        {
            Total = total;
            Leading = leading ?? throw new ArgumentNullException(nameof(leading));
        }

        // Pasos de la rutina expandida (≥ 0; long.MaxValue si la cuenta se pasa).
        public long Total { get; }

        // Los primeros min(Total, max) tipos, en el orden de la expansión.
        public IReadOnlyList<StepType> Leading { get; }
    }

    // Recorre la plantilla por bloques como RoutineExpansion (pasos consecutivos con el mismo GroupId, repeat del primero
    // del bloque; legacy: lib/core/models/routine.dart:29-70). Escalera SPM ×3 → 8, ×4 → 10; Pirámide 500 → 14;
    // Solo calentamiento → 1; Intervalos 4× → 7.
    public static class RoutineStructure
    {
        public static StructureSegments Segments(IReadOnlyList<IntervalStep> template, int max)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var limit = max < 0 ? 0 : max;
            var leading = new List<StepType>(Math.Min(limit, 64));
            long total = 0;
            var i = 0;
            while (i < template.Count)
            {
                var step = template[i];
                if (step.GroupId == null)
                {
                    total = Add(total, 1);
                    if (leading.Count < limit)
                    {
                        leading.Add(step.Type);
                    }

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

                if (repeat <= 0)
                {
                    continue;
                }

                var size = i - start;
                total = Add(total, (long)size * repeat);
                for (var r = 0; r < repeat && leading.Count < limit; r++)
                {
                    for (var k = start; k < i && leading.Count < limit; k++)
                    {
                        leading.Add(template[k].Type);
                    }
                }
            }

            return new StructureSegments(total, leading);
        }

        private static long Add(long total, long count)
        {
            return total > long.MaxValue - count ? long.MaxValue : total + count;
        }
    }
}
