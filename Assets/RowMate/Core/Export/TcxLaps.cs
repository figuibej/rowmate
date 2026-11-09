using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Export
{
    // Agrupa los puntos en vueltas (AC-STR-09-04; legacy: _groupPointsByInterval, tcx_builder.dart:83-116):
    // - ningún punto con índice de paso → UNA vuelta con todos;
    // - si alguno tiene, se abre una vuelta nueva cada vez que el índice (int?) cambia entre puntos consecutivos: un índice que
    //   vuelve más tarde abre otra vuelta y una racha sin índice entre puntos con índice es su propia vuelta (DV-118).
    // No recibe la rutina: el legacy indexaba steps?[currentStepIdx!] y fallaba con una rutina editada o borrada (DV-118).
    public static class TcxLaps
    {
        private static readonly TcxLap[] None = new TcxLap[0];

        public static IReadOnlyList<TcxLap> Group(IReadOnlyList<DataPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count == 0)
            {
                return None;
            }

            var hasIntervals = false;
            for (var i = 0; i < points.Count; i++)
            {
                if (points[i].StepIndex.HasValue)
                {
                    hasIntervals = true;
                    break;
                }
            }

            if (!hasIntervals)
            {
                return new[] { new TcxLap(0, points.Count) };
            }

            // tcx:101-113: corte cuando p.stepIndex != currentStepIdx con la vuelta en curso no vacía.
            var laps = new List<TcxLap>();
            var start = 0;
            for (var i = 1; i < points.Count; i++)
            {
                if (points[i].StepIndex != points[i - 1].StepIndex)
                {
                    laps.Add(new TcxLap(start, i - start));
                    start = i;
                }
            }

            laps.Add(new TcxLap(start, points.Count - start));
            return laps;
        }
    }
}
