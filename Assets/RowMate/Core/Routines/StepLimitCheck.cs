using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Routines
{
    // Validación 3 de AC-RUT-11-05 (Q-92): los mismos topes del diálogo de paso, sobre los valores GUARDADOS de cada paso.
    // Solo puede fallar con una base tocada a mano (F-RARA): lo que el diálogo deja confirmar siempre pasa.
    // - Cada valor guardado dentro de su tope (StepLimits). Un split ≤ 0 es "sin split" y no se controla (AC-RUT-13-08).
    // - Las progresiones solo en una serie (inSeries): las de un paso suelto no se ven ni se aplican (AC-RUT-12-03).
    // - En una serie con N ≥ 1, además cada repetición progresada (AC-RUT-13-12), en forma cerrada.
    // - N > 999 cuenta como fuera de rango (P-01); N ≤ 0 (×0 o negativo) no se controla: se muestra y no aporta pasos
    //   al expandir (DT-13, AC-RUT-14-03).
    // La usa RoutineEditorStore.SaveAsync y queda en Core para que la It. 4a la use antes de expandir (R3-08).
    public static class StepLimitCheck
    {
        public static bool StepIsWithinLimits(IntervalStep step, int repeatCount, bool inSeries)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            if (!Within(step.DurationSeconds, StepLimits.MaxDurationSeconds)
                || !Within(step.DistanceMeters, StepLimits.MaxDistanceMeters)
                || !Within(step.TargetWattsMin, StepLimits.MaxWatts)
                || !Within(step.TargetWattsMax, StepLimits.MaxWatts)
                || !Within(step.TargetSpm, StepLimits.MaxSpm)
                || !Within(PositiveSplit(step), StepLimits.MaxSplitSeconds))
            {
                return false;
            }

            if (!inSeries)
            {
                return true;
            }

            if (!Within(step.ProgressionSpm, StepLimits.MaxProgressionSpm)
                || !Within(step.ProgressionWatts, StepLimits.MaxProgressionWatts)
                || !Within(step.ProgressionSplitSeconds, StepLimits.MaxProgressionSplitSeconds))
            {
                return false;
            }

            if (repeatCount > StepLimits.MaxRepeatCount)
            {
                return false;
            }

            return repeatCount < 1 || RepetitionsWithinLimits(step, repeatCount);
        }

        // Toda la plantilla, por bloques como la expansión (RoutineExpansion: pasos consecutivos con el mismo GroupId y las
        // repeticiones del primero del bloque), que es lo que importa para no trabar el entrenamiento (R3-08).
        public static bool TemplateIsWithinLimits(IReadOnlyList<IntervalStep> template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var i = 0;
            while (i < template.Count)
            {
                var step = template[i];
                if (step.GroupId == null)
                {
                    if (!StepIsWithinLimits(step, 1, false))
                    {
                        return false;
                    }

                    i++;
                    continue;
                }

                var gid = step.GroupId;
                var repeat = step.GroupRepeatCount ?? 1;
                while (i < template.Count && template[i].GroupId == gid)
                {
                    if (!StepIsWithinLimits(template[i], repeat, true))
                    {
                        return false;
                    }

                    i++;
                }
            }

            return true;
        }

        // Los objetivos con progresión, repetición por repetición con ×repeatCount (W mín y W máx comparten la progresión de
        // vatios). Una progresión sin su objetivo no tiene efecto y no se controla. Lo usa también el diálogo de repeticiones
        // (AC-RUT-14-06: "con ×N, un objetivo queda fuera de rango").
        public static bool RepetitionsWithinLimits(IntervalStep step, int repeatCount)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            return Fits(step.TargetSpm, step.ProgressionSpm, repeatCount, StepLimits.MaxSpm)
                && Fits(step.TargetWattsMin, step.ProgressionWatts, repeatCount, StepLimits.MaxWatts)
                && Fits(step.TargetWattsMax, step.ProgressionWatts, repeatCount, StepLimits.MaxWatts)
                && Fits(PositiveSplit(step), step.ProgressionSplitSeconds, repeatCount, StepLimits.MaxSplitSeconds);
        }

        // El split guardado solo cuenta si es positivo (≤ 0 = sin split, como _buildSplitSeconds del legacy).
        internal static int? PositiveSplit(IntervalStep step)
        {
            var split = step.TargetSplitSeconds;
            return split.HasValue && split.Value > 0 ? split : null;
        }

        private static bool Fits(int? target, int? progression, int repeatCount, int max)
        {
            if (!target.HasValue || !progression.HasValue)
            {
                return true;
            }

            return StepLimits.FirstRepetitionOutOfRange(target.Value, progression.Value, repeatCount, max) == null;
        }

        private static bool Within(int? value, int max)
        {
            return !value.HasValue || (value.Value <= max && value.Value >= -max);
        }
    }
}
