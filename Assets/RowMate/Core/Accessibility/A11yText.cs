using System;
using RowMate.Core.Localization;

namespace RowMate.Core.Accessibility
{
    // Los textos que solo oye el lector (It. 14, spec §4; AC-APP-03-12, AC-APP-03-14): las métricas como "{label}: {value}", el
    // cambio de paso como "Paso 2 de 5: Trabajo" y el resumen del gráfico como "Watts: de 98 a 212". {type} y {metric} llegan ya
    // traducidos (las claves existentes); {value}, {min} y {max}, con los formatos de F-APP-12 y su unidad.
    public static class A11yText
    {
        public static string LabelValue(Localizer texts, string label, string value)
        {
            Require(texts);
            return texts.Format(L10nKeys.A11yLabelValue, MessageArg.Text("label", label ?? ""), MessageArg.Text("value", value ?? ""));
        }

        // current: desde 1 (StepIndex + 1).
        public static string StepProgress(Localizer texts, int current, int total, string type)
        {
            Require(texts);
            return texts.Format(L10nKeys.A11yStepProgress, MessageArg.Number("current", current), MessageArg.Number("total", total),
                MessageArg.Text("type", type ?? ""));
        }

        public static string ChartSummary(Localizer texts, string metric, string min, string max)
        {
            Require(texts);
            return texts.Format(L10nKeys.A11yChartSummary, MessageArg.Text("metric", metric ?? ""), MessageArg.Text("min", min ?? ""),
                MessageArg.Text("max", max ?? ""));
        }

        private static void Require(Localizer texts)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }
        }
    }
}
