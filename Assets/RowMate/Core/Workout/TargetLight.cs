using System;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // El estado de una tarjeta del HUD respecto a su objetivo (F-WKT-10): sin objetivo, cumple o fuera.
    public enum TargetState
    {
        None,
        Met,
        Off,
    }

    // El semáforo de las tres tarjetas con objetivo posible (AC-WKT-10-01).
    public readonly struct TargetLights
    {
        public static readonly TargetLights None = new TargetLights(TargetState.None, TargetState.None, TargetState.None);

        public TargetLights(TargetState spm, TargetState split, TargetState watts)
        {
            Spm = spm;
            Split = split;
            Watts = watts;
        }

        public TargetState Spm { get; }

        public TargetState Split { get; }

        public TargetState Watts { get; }
    }

    // Semáforo de las tarjetas (F-WKT-10; AC-WKT-10-01 a -04, -06): instantáneo, con cada lectura y también en pausa, sin
    // promedios ni margen. legacy: lib/features/workout/immersive_workout_screen.dart:579-602.
    public static class TargetLight
    {
        // SPM: fuera si el actual es menor que el objetivo. Watts: fuera si el actual es menor que el mínimo (el máximo no se
        // mira). Split: fuera si el actual es mayor (más lento) que el objetivo, sin casos especiales (paridad de val > target,
        // :581; AC-WKT-10-04): un split 0 (--:--) cumple con un objetivo ≥ 0 y queda fuera con uno < 0; con un objetivo 0,
        // todo split > 0 queda fuera (QA-G4a-P01, QA-G4aT-08). Sin paso (libre) o sin ese objetivo → None.
        public static TargetLights Evaluate(IntervalStep? step, RowingData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (step == null)
            {
                return TargetLights.None;
            }

            var spm = step.TargetSpm.HasValue
                ? (data.StrokeRate < step.TargetSpm.Value ? TargetState.Off : TargetState.Met)
                : TargetState.None;
            var watts = step.TargetWattsMin.HasValue
                ? (data.PowerWatts < step.TargetWattsMin.Value ? TargetState.Off : TargetState.Met)
                : TargetState.None;
            var split = step.TargetSplitSeconds.HasValue
                ? (data.Pace500mSeconds > step.TargetSplitSeconds.Value ? TargetState.Off : TargetState.Met)
                : TargetState.None;
            return new TargetLights(spm, split, watts);
        }
    }
}
