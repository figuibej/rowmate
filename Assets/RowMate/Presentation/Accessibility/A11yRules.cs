using System;
using System.Collections.Generic;
using System.Globalization;
using RowMate.Core.Accessibility;
using RowMate.Core.History;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using RowMate.Presentation.History;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Accessibility
{
    // Qué lee el lector de pantalla (It. 14, AC-APP-03-12; DT14-18): reglas POR CLASE USS, en un solo archivo, sin tocar los controles ni
    // agregar VisualElement. Cada elemento visible con una clase de la tabla da un A11yItem (rol, nombre, valor y estado); lo que no
    // tiene regla no se lee (la escena y lo decorativo quedan afuera solos). Los nombres son el texto del catálogo que se ve; los
    // botones de solo ícono, su ayuda (Atrás, Mostrar menú, Eliminar serie, Exportar TCX; AC-APP-03-06); las métricas,
    // "{nombre}: {valor}" (a11yLabelValue); el gráfico del Detalle, su resumen (a11yChartSummary).
    public static class A11yRules
    {
        public enum Kind
        {
            None,
            Header,
            Tab,
            Button,
            IconButton,
            Metric,
            Card,
            Field,
            Switch,
            Text,
            Chart,
        }

        // Las clases con regla, en orden de prioridad (un elemento con dos clases toma la primera).
        private static readonly (string Class, Kind Kind)[] Table =
        {
            ("rm-appbar__title", Kind.Header),
            ("rm-dialog__title", Kind.Header),
            ("rm-completion__title", Kind.Header),
            ("rm-session-card__title", Kind.Header),
            ("rm-tab", Kind.Tab),
            ("rm-segmented__option", Kind.Tab),
            ("rm-icon-button", Kind.IconButton),
            ("rm-fab", Kind.IconButton),
            ("rm-button", Kind.Button),
            ("rm-metric-tile", Kind.Metric),
            ("rm-hud-card", Kind.Metric),
            ("rm-stat", Kind.Metric),
            ("rm-routine-card", Kind.Card),
            ("rm-history-card", Kind.Card),
            ("rm-scan-card", Kind.Card),
            ("rm-step-row", Kind.Card),
            ("rm-step-perf", Kind.Card), // It. 15 (#33, AC-D15-02): la fila se elige (botón, con Selected si está marcada)
            ("rm-field", Kind.Field),
            ("rm-switch-row", Kind.Switch),
            ("rm-toast", Kind.Text),
            // It. 15 (#80, AC-D15-28): los paneles "Series anteriores" y "Parciales 500 m" (texto dibujado): su título y si están expandidos.
            ("rm-hud-series-panel", Kind.Text),
            ("rm-dialog__message", Kind.Text),
            ("rm-empty__title", Kind.Text),
            ("rm-chart", Kind.Chart),
        };

        public static Kind KindOf(VisualElement element)
        {
            for (var i = 0; i < Table.Length; i++)
            {
                if (element.ClassListContains(Table[i].Class))
                {
                    return Table[i].Kind;
                }
            }

            return Kind.None;
        }

        public static A11yRole RoleOf(Kind kind)
        {
            switch (kind)
            {
                case Kind.Header:
                    return A11yRole.Header;
                case Kind.Tab:
                    return A11yRole.TabButton;
                case Kind.Button:
                case Kind.IconButton:
                case Kind.Card:
                    return A11yRole.Button;
                case Kind.Field:
                    return A11yRole.TextField;
                case Kind.Switch:
                    return A11yRole.Toggle;
                case Kind.Chart:
                    return A11yRole.Image;
                case Kind.Metric:
                case Kind.Text:
                    return A11yRole.StaticText;
                default:
                    return A11yRole.None;
            }
        }

        // Recorre root (en orden de árbol) y agrega un A11yItem por elemento visible con regla. kinds guarda el tipo de cada clave.
        public static void Collect(VisualElement root, Localizer texts, List<A11yItem> into, Dictionary<VisualElement, Kind> kinds)
        {
            if (root == null || texts == null || into == null || kinds == null)
            {
                throw new ArgumentNullException(root == null ? nameof(root) : texts == null ? nameof(texts) : into == null ? nameof(into) : nameof(kinds));
            }

            Walk(root, texts, into, kinds);
        }

        private static void Walk(VisualElement element, Localizer texts, List<A11yItem> into, Dictionary<VisualElement, Kind> kinds)
        {
            if (!IsVisible(element))
            {
                return;
            }

            var kind = KindOf(element);
            if (kind != Kind.None)
            {
                var item = Describe(element, kind, texts);
                if (item != null)
                {
                    into.Add(item);
                    kinds[element] = kind;
                }

                // Solo las tarjetas y los avisos siguen hacia adentro (sus botones se leen aparte).
                if (kind != Kind.Card && kind != Kind.Text)
                {
                    return;
                }
            }

            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                Walk(element.hierarchy[i], texts, into, kinds);
            }
        }

        public static bool IsVisible(VisualElement element)
        {
            var style = element.resolvedStyle;
            return style.display != DisplayStyle.None && style.visibility != Visibility.Hidden && style.opacity > 0.001f;
        }

        // El A11yItem de un elemento con regla (null si no tiene nombre).
        public static A11yItem? Describe(VisualElement element, Kind kind, Localizer texts)
        {
            var label = "";
            var value = "";
            var state = A11yState.None;
            switch (kind)
            {
                case Kind.Header:
                    label = element is TextElement header ? header.text : Gather(element, false);
                    break;
                case Kind.Tab:
                    label = Gather(element, false);
                    if (element.ClassListContains("rm-tab--selected") || element.ClassListContains("rm-segmented__option--selected"))
                    {
                        state |= A11yState.Selected;
                    }

                    break;
                case Kind.IconButton:
                    label = element is IconButton icon && (icon.SpokenName != null || icon.Help != null) ? icon.AccessibleName ?? "" : element.tooltip ?? "";
                    if (label.Length == 0)
                    {
                        label = Gather(element, false);
                    }

                    break;
                case Kind.Button:
                    label = element is RmButton button ? button.Label.text : Gather(element, false);
                    if (string.IsNullOrEmpty(label))
                    {
                        label = element.tooltip ?? "";
                    }

                    break;
                case Kind.Metric:
                    SplitLabelValue(element, out var name, out var number);
                    label = number.Length > 0 ? A11yText.LabelValue(texts, name, number) : name;
                    break;
                case Kind.Card:
                case Kind.Text:
                    if (element is RowMate.Presentation.Workout.SeriesPanelView series)
                    {
                        label = series.TitleText;
                        if (series.Motion.Expanded)
                        {
                            state |= A11yState.Expanded;
                        }

                        break;
                    }

                    label = Gather(element, true);
                    if (element.ClassListContains("rm-step-perf--selected"))
                    {
                        state |= A11yState.Selected;
                    }

                    break;
                case Kind.Field:
                    if (element is RmTextField field)
                    {
                        label = field.LabelElement.text.Length > 0 ? field.LabelElement.text : field.Placeholder;
                        value = field.Value;
                    }
                    else
                    {
                        label = Gather(element, true);
                    }

                    break;
                case Kind.Switch:
                    if (element is Toggle toggle)
                    {
                        label = toggle.label ?? "";
                        if (toggle.value)
                        {
                            state |= A11yState.Selected;
                        }
                    }

                    break;
                case Kind.Chart:
                    label = element is SessionChartView chart ? ChartSummary(chart, texts) : element.tooltip ?? "";
                    break;
            }

            label = (label ?? "").Replace(RowMate.Core.Design.PathBreak.ZeroWidthSpace.ToString(), "").Trim();
            if (label.Length == 0)
            {
                return null;
            }

            if (!element.enabledInHierarchy)
            {
                state |= A11yState.Disabled;
            }

            if (element.ClassListContains("rm-expanded") || element.ClassListContains("--expanded"))
            {
                state |= A11yState.Expanded;
            }

            var bounds = element.worldBound;
            return new A11yItem(element, RoleOf(kind), label, bounds.x, bounds.y, bounds.width, bounds.height) { Value = value ?? "", State = state };
        }

        // El nombre (el primer texto) y el valor (el resto) de una tarjeta de métrica.
        private static void SplitLabelValue(VisualElement element, out string name, out string value)
        {
            var parts = new List<string>(4);
            CollectTexts(element, parts, false);
            // Las cifras del resumen del Detalle (.rm-stat) van con el valor arriba y el nombre abajo.
            if (element.ClassListContains("rm-stat") && parts.Count > 1)
            {
                name = parts[parts.Count - 1];
                value = string.Join(" ", parts.GetRange(0, parts.Count - 1));
                return;
            }

            name = parts.Count > 0 ? parts[0] : "";
            value = parts.Count > 1 ? string.Join(" ", parts.GetRange(1, parts.Count - 1)) : "";
        }

        // Los textos visibles de los descendientes, separados por ", " (skipButtons: sin los de los botones de adentro).
        public static string Gather(VisualElement element, bool skipButtons)
        {
            var parts = new List<string>(4);
            CollectTexts(element, parts, skipButtons);
            return string.Join(", ", parts);
        }

        private static void CollectTexts(VisualElement element, List<string> parts, bool skipButtons)
        {
            // Los íconos son glifos de la fuente de íconos: no se leen.
            if (!IsVisible(element) || element.ClassListContains(IconView.UssClass))
            {
                return;
            }

            if (element is TextElement text && !string.IsNullOrWhiteSpace(text.text))
            {
                parts.Add(text.text.Trim());
            }
            else if (element is TabularNumber number && number.Text.Length > 0)
            {
                parts.Add((number.PrefixText + number.Text + number.SuffixText).Trim());
            }

            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                var child = element.hierarchy[i];
                if (skipButtons && (child.ClassListContains("rm-button") || child.ClassListContains("rm-icon-button")))
                {
                    continue;
                }

                CollectTexts(child, parts, skipButtons);
            }
        }

        // "Watts: de 98 a 212" (a11yChartSummary): la métrica elegida y el mínimo y el máximo de lo medido (sin los ceros), con su unidad.
        public static string ChartSummary(SessionChartView chart, Localizer texts)
        {
            var data = chart.Data;
            if (data == null)
            {
                return "";
            }

            var metric = chart.Metric;
            var raw = data.Raw(metric);
            var min = double.MaxValue;
            var max = double.MinValue;
            for (var i = 0; i < raw.Length; i++)
            {
                var v = raw[i];
                if (v > 0)
                {
                    min = Math.Min(min, v);
                    max = Math.Max(max, v);
                }
            }

            if (max < min)
            {
                min = 0;
                max = 0;
            }

            var unit = ChartMetrics.Unit(metric);
            return A11yText.ChartSummary(texts, texts.Get(ChartMetrics.LabelKey(metric)), Format(min) + " " + unit, Format(max) + " " + unit);
        }

        private static string Format(double value)
        {
            return Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
        }

        // Para el volcado de las pruebas y de CAP-27.
        public static string Dump(IReadOnlyList<A11yItem> items)
        {
            return A11yTreeDump.Dump(items);
        }
    }
}
