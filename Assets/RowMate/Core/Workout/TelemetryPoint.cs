using System;
using RowMate.Core.Models;

namespace RowMate.Core.Workout
{
    // Un punto de telemetría desde la lectura fusionada y el paso en curso (F-WKT-06 + F-HIS-02; AC-WKT-06-02, -06, -08).
    // elapsed_seconds = el tiempo DEL MONITOR (Q-15, D-12), no el reloj del HUD; calories = las kcal totales; heart_rate = el
    // pulso fusionado. Con rutina, step_index (expandido, desde 0) y step_type; en libre, vacíos. El tipo queda vacío si el
    // índice quedara fuera de rango (el punto final, :270-272).
    // legacy: lib/features/workout/workout_provider.dart:160-172 (cada 5 s), :259-273 (el final).
    public static class TelemetryPoint
    {
        public static DataPoint From(long sessionId, RowingData data, int? stepIndex, IntervalStep? step)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var point = new DataPoint(sessionId, data.ElapsedSeconds)
            {
                StrokeRate = data.StrokeRate,
                StrokeCount = data.StrokeCount,
                DistanceMeters = data.DistanceMeters,
                Pace500mSeconds = data.Pace500mSeconds,
                PowerWatts = data.PowerWatts,
                Calories = data.TotalCalories,
                HeartRate = data.HeartRate,
            };
            if (!stepIndex.HasValue)
            {
                return point;
            }

            return point with
            {
                StepIndex = stepIndex.Value,
                StepTypeName = step != null ? StepTypeNames.ToStorage(step.Type) : null,
            };
        }
    }
}
