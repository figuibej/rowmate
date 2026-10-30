namespace RowMate.Core.Workout
{
    // El resultado de un paso cerrado con sus promedios (step_target_tracker.dart:30-62; 02-tech-design §2.5 de la It. 4b): los
    // mismos que deciden el veredicto. Lo guarda StepTargetTracker.Complete y lo lee el sello (StepSeal, AC-WKT-09-09). No se
    // guarda en la base (AC-WKT-06-14).
    public readonly struct StepOutcome
    {
        public StepOutcome(int stepIndex, StepResult result, int samples, double averageWatts, double averageSpm, int splitSamples,
                           double averageSplit)
        {
            StepIndex = stepIndex;
            Result = result;
            Samples = samples;
            AverageWatts = averageWatts;
            AverageSpm = averageSpm;
            SplitSamples = splitSamples;
            AverageSplit = averageSplit;
        }

        public int StepIndex { get; }

        public StepResult Result { get; }

        // Lecturas remando (SPM > 0 o W > 0, :30-40).
        public int Samples { get; }

        // Con Samples = 0, NaN.
        public double AverageWatts { get; }

        // Con Samples = 0, NaN.
        public double AverageSpm { get; }

        // Lecturas con split > 0.
        public int SplitSamples { get; }

        // Con SplitSamples = 0, NaN.
        public double AverageSplit { get; }
    }
}
