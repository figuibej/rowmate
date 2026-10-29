using System;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Estado del paso en curso (F-WKT-04; AC-WKT-04-01 a -07). Existe solo con rutina y fuera de reposo. Struct: el HUD lo
    // pide en cada aviso sin reservar memoria.
    // legacy: lib/features/workout/workout_provider.dart:16-42 (StepProgress), :98-112 (stepProgress);
    // immersive_workout_screen.dart:342-346 (showUpNext).
    public readonly struct StepProgress
    {
        public StepProgress(IntervalStep step, int stepIndex, int totalSteps, int elapsedInStep, IntervalStep? nextStep)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            StepIndex = stepIndex;
            TotalSteps = totalSteps;
            ElapsedInStep = elapsedInStep;
            NextStep = nextStep;
        }

        // Con los objetivos ya progresados (la rutina expandida).
        public IntervalStep Step { get; }

        // Desde 0, en la rutina expandida.
        public int StepIndex { get; }

        public int TotalSteps { get; }

        public int ElapsedInStep { get; }

        // null en el último.
        public IntervalStep? NextStep { get; }

        // :101-104: por tiempo, duración − transcurrido acotado en [0; duración]; si no, null (el −1 del legacy). Con una
        // duración negativa, 0 (DV-37: el clamp(0, d) del legacy lanza con d < 0, :103).
        public int? RemainingInStep
        {
            get
            {
                if (!Step.DurationSeconds.HasValue)
                {
                    return null;
                }

                var duration = Step.DurationSeconds.Value;
                if (duration <= 0)
                {
                    return 0;
                }

                var remaining = (long)duration - ElapsedInStep;
                return remaining < 0 ? 0 : remaining > duration ? duration : (int)remaining;
            }
        }

        // :31-36: transcurrido / duración en [0; 1] si la duración es > 0; si no, 0 (Q-51: el paso por distancia no avanza).
        public double Progress
        {
            get
            {
                if (!Step.DurationSeconds.HasValue || Step.DurationSeconds.Value <= 0)
                {
                    return 0;
                }

                var progress = (double)ElapsedInStep / Step.DurationSeconds.Value;
                return progress < 0 ? 0 : progress > 1 ? 1 : progress;
            }
        }

        // :39-41: restante de 1 a 5, inclusive.
        public bool IsEndingSoon
        {
            get
            {
                var remaining = RemainingInStep;
                return remaining.HasValue && remaining.Value >= 1 && remaining.Value <= 5;
            }
        }

        // immersive_workout_screen.dart:343-344: restante de 0 a 15, inclusive, y un paso siguiente.
        public bool IsUpNext
        {
            get
            {
                var remaining = RemainingInStep;
                return remaining.HasValue && remaining.Value >= 0 && remaining.Value <= 15 && NextStep != null;
            }
        }
    }
}
