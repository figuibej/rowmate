using System;
using System.Collections.Generic;
using RowMate.Core.Localization;

namespace RowMate.Core.Models
{
    // Resumen de una rutina (F-RUT-05; legacy: routine.dart:100-105 y workout_screen.dart:152). Cuenta la PLANTILLA:
    // sin pasos de trabajo → routineSummarySteps(count = total); si no, routineSummaryWorkRest(work, rest).
    // "Series" cuenta pasos de trabajo: una serie ×3 de trabajo + descanso cuenta 1 y 1 (AC-RUT-05-02).
    // El texto viejo del legacy ('1 pasos', sin catálogo) no se muestra (Q-01 + Q-02): los números son los mismos.
    public static class RoutineSummary
    {
        public static RoutineCounts Count(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var work = 0;
            var rest = 0;
            for (var i = 0; i < template.Count; i++)
            {
                if (template[i].Type == StepType.Work)
                {
                    work++;
                }
                else if (template[i].Type == StepType.Rest)
                {
                    rest++;
                }
            }

            return new RoutineCounts(template.Count, work, rest);
        }

        public static string Text(Routine routine, Localizer l10n)
        {
            if (routine == null)
            {
                throw new ArgumentNullException(nameof(routine));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            var counts = Count(routine.Steps);
            if (counts.Work == 0)
            {
                return l10n.Format(L10nKeys.RoutineSummarySteps, MessageArg.Number("count", counts.Total));
            }

            return l10n.Format(L10nKeys.RoutineSummaryWorkRest, MessageArg.Number("work", counts.Work), MessageArg.Number("rest", counts.Rest));
        }

        // Contenido del diálogo de inicio: workoutStartContent{summary} (workout_screen.dart:152, AC-RUT-05-03).
        public static string StartDialogContent(Routine routine, Localizer l10n)
        {
            var summary = Text(routine, l10n);
            return l10n.Format(L10nKeys.WorkoutStartContent, MessageArg.Text("summary", summary));
        }
    }
}
