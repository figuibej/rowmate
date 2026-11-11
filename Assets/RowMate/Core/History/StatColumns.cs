using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Core.History
{
    // Una columna de la fila de números de la tarjeta y del resumen (AC-HIS-07-04, AC-HIS-10-01): la métrica, la clave de su
    // etiqueta, el valor como un solo texto (A9-05: la unidad va pegada, como el legacy) y el color del valor.
    public sealed record StatColumn(MetricKind Metric, string LabelKey, string Value, ColorToken Color);

    // La fila de la tarjeta del historial y del resumen del detalle, que es la misma (hs:109-123; sd:160-176): Tiempo,
    // Distancia "{n}m", Vatios, SPM con 1 decimal, Split (m:ss desde la It. 15, AC-D15-23; o "--:--") y, solo con pulso (máximo > 0), Pulso
    // "{prom}/{máx}". Sin calorías (Q-28). Los colores son los base de la paleta (§10.2); el tiempo va en blanco
    // (`color ?? Colors.white`, hs:146; text.primary), no en metric.time.
    public static class StatColumns
    {
        public static IReadOnlyList<StatColumn> Build(SessionStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            var columns = new List<StatColumn>(6)
            {
                new StatColumn(MetricKind.Time, L10nKeys.HistoryStatTime, stats.DurationFormatted, Tokens.TextPrimary),
                new StatColumn(MetricKind.Distance, L10nKeys.HistoryStatDistance, Fmt.Integer(stats.TotalDistance) + "m",
                    MetricPalette.For(MetricKind.Distance)),
                new StatColumn(MetricKind.Watts, L10nKeys.HistoryStatPower, Fmt.Integer(stats.P99Watts), MetricPalette.For(MetricKind.Watts)),
                new StatColumn(MetricKind.Spm, L10nKeys.HistoryStatSpm, Fmt.Fixed(stats.P99Spm, 1), MetricPalette.For(MetricKind.Spm)),
                new StatColumn(MetricKind.Split, L10nKeys.HistoryStatSplit, HistoryFormats.SummarySplit(stats.P99SplitSeconds), MetricPalette.For(MetricKind.Split)),
            };
            if (stats.HasHeartRate)
            {
                columns.Add(new StatColumn(MetricKind.HeartRate, L10nKeys.HistoryStatHeartRate,
                    Fmt.Integer(stats.AvgHeartRate) + "/" + Fmt.Integer(stats.MaxHeartRate), MetricPalette.For(MetricKind.HeartRate)));
            }

            return columns;
        }
    }
}
