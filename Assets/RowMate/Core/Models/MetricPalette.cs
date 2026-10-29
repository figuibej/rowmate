using System;
using RowMate.Core.Design;

namespace RowMate.Core.Models
{
    // Paleta por métrica (F-APP-06, AC-APP-06-01): una sola fuente de colores (los tokens de Tokens.cs, AC-APP-06-02).
    // legacy: lib/shared/theme.dart:32-40 (MetricColors)
    public static class MetricPalette
    {
        // Color del valor (y del borde superior, al 60 % en reposo): el del legacy, [CONTRATO].
        public static ColorToken For(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.Split:
                    return Tokens.MetricSplit;
                case MetricKind.Spm:
                    return Tokens.MetricSpm;
                case MetricKind.Watts:
                    return Tokens.MetricWatts;
                case MetricKind.Distance:
                    return Tokens.MetricDistance;
                case MetricKind.HeartRate:
                    return Tokens.MetricHr;
                case MetricKind.Time:
                    return Tokens.MetricTime;
                case MetricKind.Calories:
                    return Tokens.MetricKcal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "MetricPalette: métrica desconocida.");
            }
        }

        // Color del ícono y la etiqueta de la tarjeta: el de la métrica al 100 % (el legacy los pone al 70 %, que no llega a
        // AA); el pulso, metric.hr.hud, el mismo matiz más claro (01-po-spec §11.1 DV-03; 04-design-notes §1).
        public static ColorToken LabelFor(MetricKind kind)
        {
            return kind == MetricKind.HeartRate ? Tokens.MetricHrHud : For(kind);
        }

        // Color del valor en el HUD sin objetivo (AC-APP-06-01; D-ART-01, R-10): la variante .hud de su métrica, el mismo
        // matiz más claro (Split #FFAA47, SPM #3ACCF1, Watts #FFD60A, distancia #4ECBF2, tiempo #E0E0E0, kcal #06D6A0,
        // pulso #FF5D7F). Con objetivo, el HUD usa text.hudPrimary o state.offTarget.hud (AC-WKT-10-05).
        public static ColorToken HudFor(MetricKind kind)
        {
            switch (kind)
            {
                case MetricKind.Split:
                    return Tokens.MetricSplitHud;
                case MetricKind.Spm:
                    return Tokens.MetricSpmHud;
                case MetricKind.Watts:
                    return Tokens.MetricWattsHud;
                case MetricKind.Distance:
                    return Tokens.MetricDistanceHud;
                case MetricKind.HeartRate:
                    return Tokens.MetricHrHud;
                case MetricKind.Time:
                    return Tokens.MetricTimeHud;
                case MetricKind.Calories:
                    return Tokens.MetricKcalHud;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "MetricPalette: métrica desconocida.");
            }
        }
    }
}
