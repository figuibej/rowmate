using System;
using System.Collections.Generic;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.History
{
    // Una fila del "Rendimiento por paso" (AC-HIS-11-01, -03, -04): el índice del paso en la rutina expandida (la insignia
    // muestra índice + 1), cuántos puntos tiene, su duración (puntos × intervalo), la distancia (último − primero, puede ser
    // negativa), la potencia y el split promedio redondeados y el SPM promedio (se muestra con 1 decimal).
    public sealed record StepBreakdownRow(int StepIndex, int PointCount, int DurationSeconds, int DistanceMeters, int AvgWatts, double AvgSpm,
                                          int AvgSplitSeconds)
    {
        public string Badge => Fmt.Integer(StepIndex + 1L);

        public string Line1 => HistoryFormats.StepLine1(DurationSeconds, DistanceMeters);

        public string Watts => HistoryFormats.StepWatts(AvgWatts);

        public string Spm => HistoryFormats.StepSpm(AvgSpm);

        public string Split => HistoryFormats.StepSplit(AvgSplitSeconds);
    }

    // _buildStepBreakdown y _buildStepRow (session_detail_screen.dart:183-224): los puntos con índice se agrupan por índice
    // (los nulos quedan afuera), una fila por índice con al menos un punto, en orden ascendente; los tramos no contiguos de un
    // mismo índice se suman en una fila. Promedios simples CON los ceros, en el orden de los puntos (la suma de Dart).
    public static class StepBreakdown
    {
        public static IReadOnlyList<StepBreakdownRow> Compute(IReadOnlyList<DataPoint> points, int sampleIntervalSeconds)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (sampleIntervalSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds), sampleIntervalSeconds, "El intervalo es positivo.");
            }

            var grouped = new SortedDictionary<int, List<DataPoint>>();
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point == null || !point.StepIndex.HasValue)
                {
                    continue;
                }

                if (!grouped.TryGetValue(point.StepIndex.Value, out var list))
                {
                    list = new List<DataPoint>();
                    grouped.Add(point.StepIndex.Value, list);
                }

                list.Add(point);
            }

            var rows = new List<StepBreakdownRow>(grouped.Count);
            foreach (var entry in grouped)
            {
                rows.Add(Row(entry.Key, entry.Value, sampleIntervalSeconds));
            }

            return rows;
        }

        // sd:214-219.
        private static StepBreakdownRow Row(int stepIndex, List<DataPoint> points, int interval)
        {
            long watts = 0;
            double spm = 0;
            long split = 0;
            for (var i = 0; i < points.Count; i++)
            {
                watts += points[i].PowerWatts;
                spm += points[i].StrokeRate;
                split += points[i].Pace500mSeconds;
            }

            var n = points.Count;
            var avgWatts = (int)DartMath.Round((double)watts / n);
            var avgSplit = (int)DartMath.Round((double)split / n);
            var distance = points[n - 1].DistanceMeters - points[0].DistanceMeters;
            return new StepBreakdownRow(stepIndex, n, checked(n * interval), distance, avgWatts, spm / n, avgSplit);
        }
    }
}
