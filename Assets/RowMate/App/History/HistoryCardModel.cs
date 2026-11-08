using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.App.History
{
    // Lo que dibuja la tarjeta de una sesión (AC-HIS-07-03 a -05; legacy: history_screen.dart:63-124), con los textos ya
    // resueltos: el id (la clave estable de la lista virtual), el nombre (el de la rutina guardado en la sesión o historyFree),
    // la fecha local "dd/MM/yyyy  HH:mm" y las 5 o 6 columnas con sus etiquetas en el idioma vigente. Se vuelve a armar al
    // cambiar el idioma (gate m-03).
    public sealed record HistoryCardModel(long Id, string Name, string Date, IReadOnlyList<HistoryCardColumn> Columns)
    {
        public static HistoryCardModel From(HistoryEntry entry, Localizer l10n)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            var stats = StatColumns.Build(entry.Stats);
            var columns = new HistoryCardColumn[stats.Count];
            for (var i = 0; i < stats.Count; i++)
            {
                columns[i] = HistoryCardColumn.From(stats[i], l10n);
            }

            return new HistoryCardModel(entry.Session.Id ?? 0, entry.Session.RoutineName ?? l10n.Get(L10nKeys.HistoryFree),
                HistoryFormats.CardDate(entry.Session.StartedAt), columns);
        }
    }

    // Una columna con su etiqueta ya traducida (la tarjeta y el Resumen la usan igual).
    public sealed record HistoryCardColumn(MetricKind Metric, string Label, string Value, ColorToken Color)
    {
        public static HistoryCardColumn From(StatColumn column, Localizer l10n)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            return new HistoryCardColumn(column.Metric, l10n.Get(column.LabelKey), column.Value, column.Color);
        }

        public static IReadOnlyList<HistoryCardColumn> Row(SessionStats stats, Localizer l10n)
        {
            var source = StatColumns.Build(stats);
            var columns = new HistoryCardColumn[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                columns[i] = From(source[i], l10n);
            }

            return columns;
        }
    }
}
