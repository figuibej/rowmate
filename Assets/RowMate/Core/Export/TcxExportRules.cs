using System;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.Export
{
    // Reglas de la exportación (It. 12; 02-tech-design §2.3):
    // - IsExportable (AC-STR-09-10, DT12-05; legacy: profile_provider.dart:159-170): al menos un punto y que el tiempo o la
    //   distancia de los puntos no sean los dos 0 (la condición del legacy: stats.totalTimeSeconds == 0 && stats.totalDistance == 0
    //   se saltea).
    // - CalorieBasis (AC-STR-09-07, DT12-04; pp:173-188): una sesión sin fin o con tiempo total 0 usa el tiempo y las calorías de
    //   sus puntos (los máximos de SessionStats.Compute, la reparación del legacy) EN MEMORIA; la base no se toca (DV-119).
    // - LapCalories (AC-STR-09-06; tcx:177-179): (calorías × puntos / (tiempo / 5)).round() de Dart (mitad lejos de cero), en double
    //   y en ese orden. Tiempo ≤ 0 → 0 (el legacy daba Infinity/NaN.round(), una excepción; DV-119).
    public static class TcxExportRules
    {
        // El legacy estima los puntos con el intervalo de muestreo de la app (5 s).
        public const double SampleIntervalSeconds = 5.0;

        public static bool HasData(SessionStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return !(stats.TotalTimeSeconds == 0 && stats.TotalDistance == 0);
        }

        public static bool IsExportable(SessionStats stats, int pointCount)
        {
            return pointCount > 0 && HasData(stats);
        }

        // La sesión necesita la reparación del legacy (finishedAt == null || totalTimeSeconds == 0).
        public static bool NeedsRepair(WorkoutSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return session.FinishedAt == null || session.TotalTimeSeconds == 0;
        }

        public static (int TotalSeconds, int TotalCalories) CalorieBasis(WorkoutSession session, SessionStats pointStats)
        {
            if (pointStats == null)
            {
                throw new ArgumentNullException(nameof(pointStats));
            }

            return NeedsRepair(session)
                ? (pointStats.TotalTimeSeconds, pointStats.TotalCalories)
                : (session.TotalTimeSeconds, session.TotalCalories);
        }

        public static int LapCalories(int totalCalories, int lapPoints, int totalSeconds)
        {
            if (totalSeconds <= 0)
            {
                return 0;
            }

            // session.totalCalories * totalPoints (int de Dart, 64 bits) / (session.totalTimeSeconds / 5) (double).
            var estimated = (long)totalCalories * lapPoints / (totalSeconds / SampleIntervalSeconds);
            return (int)DartMath.Round(estimated);
        }
    }
}
