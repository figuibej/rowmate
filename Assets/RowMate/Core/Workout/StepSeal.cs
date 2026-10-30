using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.Workout
{
    // Las métricas de un fragmento del sello, en el orden de la pastilla de objetivo (interval_step.dart:79-109).
    public enum SealMetric
    {
        Watts,
        Spm,
        Split,
    }

    // Un fragmento del detalle del sello: el promedio del paso y su objetivo, ya formateados (`150`, `--`; `24.0`; `02:06`).
    public readonly struct SealFragment
    {
        public SealFragment(SealMetric metric, string average, string target)
        {
            Metric = metric;
            Average = average ?? throw new ArgumentNullException(nameof(average));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public SealMetric Metric { get; }

        public string Average { get; }

        public string Target { get; }
    }

    // Lo que dice el sello de un paso (AC-WKT-09-09): el veredicto y un fragmento por objetivo que define el paso.
    public sealed class SealContent
    {
        public SealContent(int stepIndex, StepResult verdict, IReadOnlyList<SealFragment> fragments)
        {
            StepIndex = stepIndex;
            Verdict = verdict;
            Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments));
        }

        public int StepIndex { get; }

        public StepResult Verdict { get; }

        // En el orden de la pastilla: vatios, SPM, split.
        public IReadOnlyList<SealFragment> Fragments { get; }
    }

    // El sello "Objetivo cumplido / No alcanzado" de 2 s (D-ANI-05; AC-WKT-09-08, -09; P-02, P-03; 02-tech-design §2.5 de la
    // It. 4b). Es una mejora EXR: sale del resultado que el motor calcula al cerrar el paso (step_target_tracker.dart:43-62).
    public static class StepSeal
    {
        public const double DurationSeconds = 2;

        // Sin lecturas remando o sin split informado.
        public const string NoAverage = "--";
        public const string NoSplitAverage = "--:--";

        // AC-WKT-09-08: hay sello si el paso tenía objetivos (StepTargetTracker.HasTargets: solo entonces hay StepOutcome) y se
        // cerró SOLO; nunca el último (queda en curso, Q-52, P-02), nunca un paso cortado con Terminar (no tiene resultado),
        // nunca en libre (no hay pasos).
        public static bool Applies(StepOutcome? outcome, int totalSteps)
        {
            return outcome.HasValue && outcome.Value.StepIndex >= 0 && outcome.Value.StepIndex < totalSteps - 1;
        }

        // AC-WKT-09-09: un fragmento por objetivo que define el paso (vatios MÍN., SPM, split; los vatios máx. no son objetivo,
        // :23-26). El promedio se redondea hacia el lado desfavorable, así el número nunca contradice el veredicto (P-03).
        public static SealContent For(StepOutcome outcome, IntervalStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var fragments = new List<SealFragment>(3);
            if (step.TargetWattsMin.HasValue)
            {
                fragments.Add(new SealFragment(SealMetric.Watts, WattsAverage(outcome), WorkoutFormats.TargetValue(step.TargetWattsMin.Value)));
            }

            if (step.TargetSpm.HasValue)
            {
                fragments.Add(new SealFragment(SealMetric.Spm, SpmAverage(outcome), WorkoutFormats.TargetValue(step.TargetSpm.Value)));
            }

            if (step.TargetSplitSeconds.HasValue)
            {
                fragments.Add(new SealFragment(SealMetric.Split, SplitAverage(outcome), WorkoutFormats.SplitTargetValue(step.TargetSplitSeconds.Value)));
            }

            return new SealContent(outcome.StepIndex, outcome.Result, fragments);
        }

        // ⌊promedio⌋ entero (159,6 → 159 contra 160: no alcanzado); sin lecturas remando, "--".
        public static string WattsAverage(in StepOutcome outcome)
        {
            if (outcome.Samples <= 0 || double.IsNaN(outcome.AverageWatts))
            {
                return NoAverage;
            }

            return Fmt.Integer((long)Math.Floor(outcome.AverageWatts));
        }

        // ⌊10·promedio⌋ / 10 con un decimal y punto (Q-06), corregido para que nunca pase del promedio: en coma flotante 10·x
        // puede redondear hacia arriba hasta un entero k con k / 10 > x (3,5999999999999996 × 10 da 36,0 y mostraría 3.6); si
        // k / 10 > promedio, k − 1. Con un objetivo entero, un promedio por debajo nunca se muestra igual al objetivo. Sin
        // lecturas, "--".
        public static string SpmAverage(in StepOutcome outcome)
        {
            if (outcome.Samples <= 0 || double.IsNaN(outcome.AverageSpm))
            {
                return NoAverage;
            }

            var average = outcome.AverageSpm;
            var tenths = Math.Floor(average * 10);
            if (tenths / 10 > average)
            {
                tenths -= 1;
            }

            return Fmt.Fixed(tenths / 10, 1);
        }

        // ⌈promedio⌉ s en MM:SS (TimeFormats.MinSec): 125,2 → 02:06 contra 02:05 (no alcanzado); sin split informado, "--:--".
        public static string SplitAverage(in StepOutcome outcome)
        {
            if (outcome.SplitSamples <= 0 || double.IsNaN(outcome.AverageSplit))
            {
                return NoSplitAverage;
            }

            return TimeFormats.MinSec((long)Math.Ceiling(outcome.AverageSplit));
        }
    }
}
