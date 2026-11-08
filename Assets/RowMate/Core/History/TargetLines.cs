using System;
using System.Collections.Generic;
using RowMate.Core.Models;

namespace RowMate.Core.History
{
    // Una línea objetivo (AC-HIS-15-01 a -03): horizontal de Start a End (los de su franja), en Y (el valor dibujado: en el split,
    // invertido y al piso) con el objetivo original (RawTarget) y el tipo de su franja (el color de su área).
    public sealed record TargetLine(double Start, double End, double Y, double RawTarget, string TypeName);

    // targetLines (session_detail_screen.dart:352-388): por cada franja cuyo índice esté entre los pasos expandidos ACTUALES
    // (Q-16), en Vatios el mínimo y el máximo definidos (en ese orden), en SPM el objetivo de SPM y en Split el de split, que se
    // dibuja como la curva: max(0, lento − objetivo) (DT9-08, DV-93; el legacy no lo invierte). Distancia y Pulso, ninguna. Un
    // índice fuera de rango no tiene línea; sin rutina (borrada o libre), flatSteps va vacía y no hay ninguna (AC-RUT-20-01).
    public static class TargetLines
    {
        private static readonly IReadOnlyList<TargetLine> None = new TargetLine[0];

        public static IReadOnlyList<TargetLine> Build(IReadOnlyList<StepBand> bands, IReadOnlyList<IntervalStep> flatSteps, ChartMetric metric,
                                                      double slowestPace)
        {
            if (bands == null)
            {
                throw new ArgumentNullException(nameof(bands));
            }

            if (flatSteps == null)
            {
                throw new ArgumentNullException(nameof(flatSteps));
            }

            if (flatSteps.Count == 0 || metric == ChartMetric.Distance || metric == ChartMetric.HeartRate)
            {
                return None;
            }

            var lines = new List<TargetLine>();
            for (var i = 0; i < bands.Count; i++)
            {
                var band = bands[i];
                if (band.StepIndex < 0 || band.StepIndex >= flatSteps.Count)
                {
                    continue; // sd:356
                }

                var step = flatSteps[band.StepIndex];
                switch (metric)
                {
                    case ChartMetric.Watts:
                        Add(lines, band, step.TargetWattsMin, metric, slowestPace);
                        Add(lines, band, step.TargetWattsMax, metric, slowestPace);
                        break;
                    case ChartMetric.Spm:
                        Add(lines, band, step.TargetSpm, metric, slowestPace);
                        break;
                    case ChartMetric.Split:
                        Add(lines, band, step.TargetSplitSeconds, metric, slowestPace);
                        break;
                }
            }

            return lines.Count == 0 ? None : lines;
        }

        private static void Add(List<TargetLine> lines, StepBand band, int? target, ChartMetric metric, double slowestPace)
        {
            if (!target.HasValue)
            {
                return;
            }

            double raw = target.Value;
            var y = metric == ChartMetric.Split ? Math.Max(0, slowestPace - raw) : raw;
            lines.Add(new TargetLine(band.Start, band.End, y, raw, band.TypeName));
        }
    }
}
