using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Cumplimiento de objetivos por paso durante una rutina (F-WKT-09; LT-39 a LT-44). Clase pura, en memoria: el motor la
    // alimenta una vez por segundo y la cierra al terminar cada paso. El criterio es el mismo que el de las tarjetas en vivo,
    // aplicado al promedio del paso: vatios ≥ mínimo, SPM ≥ objetivo, split ≤ objetivo. No se guarda (AC-WKT-06-14); la
    // muestran las píldoras de la 4b. legacy: lib/features/workout/step_target_tracker.dart:1-71, sin cambios de regla.
    public sealed class StepTargetTracker
    {
        private readonly Dictionary<int, Accumulator> _accumulators = new Dictionary<int, Accumulator>();
        private readonly Dictionary<int, StepResult> _results = new Dictionary<int, StepResult>();

        // :23-26: vatios mín., SPM o split (los vatios máx. no cuentan).
        public static bool HasTargets(IntervalStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            return step.TargetWattsMin.HasValue || step.TargetSpm.HasValue || step.TargetSplitSeconds.HasValue;
        }

        // :30-40: una lectura del paso. Sin remar (SPM ≤ 0 y vatios ≤ 0) no cuenta; el split solo cuando el monitor lo informa.
        public void Update(int stepIndex, RowingData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.StrokeRate <= 0 && data.PowerWatts <= 0)
            {
                return;
            }

            if (!_accumulators.TryGetValue(stepIndex, out var a))
            {
                a = new Accumulator();
                _accumulators[stepIndex] = a;
            }

            a.Samples++;
            a.Watts += data.PowerWatts;
            a.Spm += data.StrokeRate;
            if (data.Pace500mSeconds > 0)
            {
                a.SplitSamples++;
                a.Split += data.Pace500mSeconds;
            }
        }

        // :43-62: cierra el paso comparando los promedios con sus objetivos. Sin objetivos no hay resultado; sin lecturas
        // remando, Missed ("nunca remó"); sin dato de split, ese objetivo no se evalúa.
        public void Complete(int stepIndex, IntervalStep step)
        {
            if (!HasTargets(step))
            {
                return;
            }

            if (!_accumulators.TryGetValue(stepIndex, out var a) || a.Samples == 0)
            {
                _results[stepIndex] = StepResult.Missed;
                return;
            }

            var avgWatts = a.Watts / a.Samples;
            var avgSpm = a.Spm / a.Samples;
            double? avgSplit = a.SplitSamples == 0 ? (double?)null : a.Split / a.SplitSamples;
            var wattsOk = !step.TargetWattsMin.HasValue || avgWatts >= step.TargetWattsMin.Value;
            var spmOk = !step.TargetSpm.HasValue || avgSpm >= step.TargetSpm.Value;
            var splitOk = !step.TargetSplitSeconds.HasValue || !avgSplit.HasValue || avgSplit.Value <= step.TargetSplitSeconds.Value;
            _results[stepIndex] = wattsOk && spmOk && splitOk ? StepResult.Met : StepResult.Missed;
        }

        // :65: null = sin objetivo o todavía no completado.
        public StepResult? ResultOf(int stepIndex)
        {
            return _results.TryGetValue(stepIndex, out var result) ? result : (StepResult?)null;
        }

        // :67-70.
        public void Reset()
        {
            _accumulators.Clear();
            _results.Clear();
        }

        private sealed class Accumulator
        {
            public int Samples;
            public double Watts;
            public double Spm;
            public int SplitSamples;
            public double Split;
        }
    }
}
