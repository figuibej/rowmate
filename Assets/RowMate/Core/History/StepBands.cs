using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Models;

namespace RowMate.Core.History
{
    // Una franja de paso del gráfico (AC-HIS-14-01): de Start a End en el tiempo del monitor (s), el índice del paso y el
    // nombre guardado del tipo del punto que la abre ("work" si no tiene).
    public sealed record StepBand(double Start, double End, int StepIndex, string TypeName);

    // Las franjas (session_detail_screen.dart:313-332), el bucle del legacy literal: recorre los puntos en orden y, en cada
    // índice distinto del de la franja abierta, la cierra en el tiempo de ese punto y abre otra desde ahí, con el tipo del punto
    // (?? 'work'). Los puntos sin índice no cortan; la última franja cierra en el último punto de la sesión. Sin ningún índice,
    // no hay franjas (_hasStepData).
    public static class StepBands
    {
        private static readonly IReadOnlyList<StepBand> None = new StepBand[0];

        public static IReadOnlyList<StepBand> Build(IReadOnlyList<DataPoint> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Count == 0)
            {
                return None;
            }

            var bands = new List<StepBand>();
            int? previousStep = null;
            string? previousType = null;
            double start = 0;
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point.StepIndex.HasValue && point.StepIndex != previousStep)
                {
                    if (previousStep.HasValue && previousType != null)
                    {
                        bands.Add(new StepBand(start, point.ElapsedSeconds, previousStep.Value, previousType));
                    }

                    start = point.ElapsedSeconds;
                    previousStep = point.StepIndex;
                    previousType = point.StepTypeName ?? StepTypeNames.Work;
                }
            }

            if (previousStep.HasValue && previousType != null)
            {
                bands.Add(new StepBand(start, points[points.Count - 1].ElapsedSeconds, previousStep.Value, previousType));
            }

            return bands.Count == 0 ? None : bands;
        }
    }

    // stepColor(String) del legacy (lib/shared/theme.dart:56-69; AC-HIS-14-02): work, rest, warmup y cooldown con sus tokens
    // y cualquier otro step.unknown (#607D8B). La misma regla que StepTypeInfo.ColorForStorageName.
    public static class StepBandPalette
    {
        public static ColorToken For(string? typeName)
        {
            return StepTypeInfo.ColorForStorageName(typeName);
        }
    }
}
