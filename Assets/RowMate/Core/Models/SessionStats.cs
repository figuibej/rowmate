using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // Estadísticas de una sesión desde su telemetría (F-RUT-18 + F-HIS-04; legacy: workout_session.dart:71-136).
    // - P99 de vatios, SPM y split: solo valores > 0, ordenados de menor a mayor, índice ⌊(n − 1) · 0,99⌋ en double;
    //   vatios y split redondeados (DartMath.Round), SPM sin redondear (AC-RUT-18-02 a -04).
    // - Distancia, tiempo y calorías: el MÁXIMO de cada columna, ceros incluidos y en cualquier orden (AC-RUT-18-05).
    // - Pulso: promedio redondeado (la mitad sube) y máximo de los puntos con pulso > 0; 0 si no hay (AC-RUT-18-06 a -08).
    public sealed record SessionStats
    {
        public static readonly SessionStats Empty = new SessionStats();

        public int P99Watts { get; init; }

        public double P99Spm { get; init; }

        public int P99SplitSeconds { get; init; }

        public int TotalDistance { get; init; }

        public int TotalTimeSeconds { get; init; }

        public int TotalCalories { get; init; }

        // Media de los puntos con pulso > 0 (0 si no hubo).
        public int AvgHeartRate { get; init; }

        // 0 si no hubo.
        public int MaxHeartRate { get; init; }

        // workout_session.dart:92.
        public bool HasHeartRate => MaxHeartRate > 0;

        // workout_session.dart:94-99 → TimeFormats.SplitOrDashes: "--:--" si ≤ 0, si no MM:SS (AC-RUT-18-09).
        public string SplitFormatted => TimeFormats.SplitOrDashes(P99SplitSeconds);

        // workout_session.dart:101-109 → TimeFormats.Elapsed.
        public string DurationFormatted => TimeFormats.Elapsed(TotalTimeSeconds);

        // ((count − 1) * 0.99).floor() en double (workout_session.dart:114): 1 → 0, 3 → 1, 101 → 99, 1000 → 989.
        public static int Percentile99Index(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "El P99 necesita al menos un valor.");
            }

            return (int)Math.Floor((count - 1) * 0.99);
        }

        // _percentile99 (workout_session.dart:111-116): ordena una copia de menor a mayor y toma el índice del P99;
        // sin valores, 0. No filtra: Compute le pasa solo los valores > 0.
        public static double Percentile99(IReadOnlyList<double> positiveValues)
        {
            if (positiveValues == null)
            {
                throw new ArgumentNullException(nameof(positiveValues));
            }

            if (positiveValues.Count == 0)
            {
                return 0;
            }

            var sorted = new double[positiveValues.Count];
            for (var i = 0; i < sorted.Length; i++)
            {
                sorted[i] = positiveValues[i];
            }

            Array.Sort(sorted);
            return sorted[Percentile99Index(sorted.Length)];
        }

        // compute (workout_session.dart:118-136).
        public static SessionStats Compute(IReadOnlyList<DataPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count == 0)
            {
                return Empty;
            }

            var watts = new List<double>(points.Count);
            var spms = new List<double>(points.Count);
            var splits = new List<double>(points.Count);
            long hrSum = 0;
            var hrCount = 0;
            var hrMax = 0;
            var first = points[0];
            var maxDistance = first.DistanceMeters;
            var maxTime = first.ElapsedSeconds;
            var maxCalories = first.Calories;
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.PowerWatts > 0)
                {
                    watts.Add(p.PowerWatts);
                }

                if (p.StrokeRate > 0)
                {
                    spms.Add(p.StrokeRate);
                }

                if (p.Pace500mSeconds > 0)
                {
                    splits.Add(p.Pace500mSeconds);
                }

                if (p.HeartRate > 0)
                {
                    hrSum += p.HeartRate;
                    hrCount++;
                    if (hrCount == 1 || p.HeartRate > hrMax)
                    {
                        hrMax = p.HeartRate;
                    }
                }

                if (p.DistanceMeters > maxDistance)
                {
                    maxDistance = p.DistanceMeters;
                }

                if (p.ElapsedSeconds > maxTime)
                {
                    maxTime = p.ElapsedSeconds;
                }

                if (p.Calories > maxCalories)
                {
                    maxCalories = p.Calories;
                }
            }

            return new SessionStats
            {
                P99Watts = (int)DartMath.Round(Percentile99(watts)),
                P99Spm = Percentile99(spms),
                P99SplitSeconds = (int)DartMath.Round(Percentile99(splits)),
                TotalDistance = maxDistance,
                TotalTimeSeconds = maxTime,
                TotalCalories = maxCalories,
                // (hrs.reduce(+) / hrs.length).round(): la división de Dart da double y round() sube la mitad.
                AvgHeartRate = hrCount == 0 ? 0 : (int)DartMath.Round((double)hrSum / hrCount),
                MaxHeartRate = hrCount == 0 ? 0 : hrMax,
            };
        }
    }
}
