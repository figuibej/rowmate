using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Models;

namespace RowMate.Core.History
{
    // La métrica del gráfico del detalle (F-HIS-12; AC-HIS-12-01): el orden del selector es el de _chartMetric del legacy
    // (0 = watts, 1 = spm, 2 = split, 3 = distance, 4 = hr; session_detail_screen.dart:25, :67-73).
    public enum ChartMetric
    {
        Watts,
        Spm,
        Split,
        Distance,
        HeartRate,
    }

    // Lo que cada métrica necesita: la clave del selector, la unidad del tooltip, el color de la curva y el de la cifra del
    // tooltip (session_detail_screen.dart:334-350).
    public static class ChartMetrics
    {
        private static readonly ChartMetric[] Order =
        {
            ChartMetric.Watts, ChartMetric.Spm, ChartMetric.Split, ChartMetric.Distance, ChartMetric.HeartRate,
        };

        // El orden del selector (AC-HIS-12-01).
        public static IReadOnlyList<ChartMetric> SelectorOrder => Order;

        // sd:67-73: sessionChartWatts, sessionChartSpm, sessionChartSplit, sessionChartDistance, sessionChartHr.
        public static string LabelKey(ChartMetric metric)
        {
            switch (metric)
            {
                case ChartMetric.Watts:
                    return L10nKeys.SessionChartWatts;
                case ChartMetric.Spm:
                    return L10nKeys.SessionChartSpm;
                case ChartMetric.Split:
                    return L10nKeys.SessionChartSplit;
                case ChartMetric.Distance:
                    return L10nKeys.SessionChartDistance;
                case ChartMetric.HeartRate:
                    return L10nKeys.SessionChartHr;
                default:
                    throw Unknown(metric);
            }
        }

        // sd:343-350: W, spm, s, m, bpm (iguales en es y en, Q-06; no son claves).
        public static string Unit(ChartMetric metric)
        {
            switch (metric)
            {
                case ChartMetric.Watts:
                    return "W";
                case ChartMetric.Spm:
                    return "spm";
                case ChartMetric.Split:
                    return "s";
                case ChartMetric.Distance:
                    return "m";
                case ChartMetric.HeartRate:
                    return "bpm";
                default:
                    throw Unknown(metric);
            }
        }

        // La métrica de la paleta (MetricPalette, F-APP-06).
        public static MetricKind Kind(ChartMetric metric)
        {
            switch (metric)
            {
                case ChartMetric.Watts:
                    return MetricKind.Watts;
                case ChartMetric.Spm:
                    return MetricKind.Spm;
                case ChartMetric.Split:
                    return MetricKind.Split;
                case ChartMetric.Distance:
                    return MetricKind.Distance;
                case ChartMetric.HeartRate:
                    return MetricKind.HeartRate;
                default:
                    throw Unknown(metric);
            }
        }

        // El color base de la curva [CONTRATO] (sd:334-341; AC-HIS-13-06).
        public static ColorToken Palette(ChartMetric metric)
        {
            return MetricPalette.For(Kind(metric));
        }

        // La cifra del tooltip: metric.*.hud (D9-07, adoptado).
        public static ColorToken TooltipPalette(ChartMetric metric)
        {
            return MetricPalette.HudFor(Kind(metric));
        }

        private static ArgumentOutOfRangeException Unknown(ChartMetric metric)
        {
            return new ArgumentOutOfRangeException(nameof(metric), metric, "ChartMetric desconocida.");
        }
    }
}
