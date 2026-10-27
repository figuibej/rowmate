using System.Collections.Generic;
using System.Globalization;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Diagnostics
{
    // Detector de desbordes y textos cortados (02-tech-design §5.1, QA-G2-14; spec §0.3 "nada fuera de su tarjeta ni de la
    // ventana"). Lo usan los tests PlayMode y el arnés de capturas.
    // Contenedores explícitos: la ventana (la raíz que se audita), cada .rm-screen, .rm-appbar, .rm-card, .rm-tab-bar,
    // .rm-sim-panel y el contentContainer de cada ScrollView. Todo descendiente visible (display y visibility; la opacidad no
    // cuenta) tiene que caer dentro de su contenedor explícito más cercano (± 0,5 dp); lo que está dentro del contenido de un
    // ScrollView se compara con ese contenido (se alcanza desplazando), no con el viewport.
    // Exentos por diseño: .rm-focus-ring y __halo (inset −4), .rm-sim-ribbon (rotada; la ventana la corta como a la Banner)
    // y las barras de desplazamiento de Unity. Además: ningún texto de una línea más ancho que su caja sin "…", ningún texto
    // multilínea más alto que su caja, ningún TabularNumber más ancho que su caja y ningún FitBox sin entrar.
    public static class LayoutAudit
    {
        public const float Tolerance = 0.5f;

        private static readonly string[] ContainerClasses = { "rm-screen", "rm-appbar", "rm-card", "rm-tab-bar", "rm-sim-panel" };
        private static readonly string[] ExemptClasses = { "rm-focus-ring", "rm-focus-ring__halo", "rm-sim-ribbon", "unity-scroller" };

        public static LayoutAuditResult Run(VisualElement window)
        {
            var result = new LayoutAuditResult();
            Visit(window, window, window.worldBound, result);
            return result;
        }

        private static void Visit(VisualElement element, VisualElement container, Rect containerBounds, LayoutAuditResult result)
        {
            for (var i = 0; i < element.hierarchy.childCount; i++)
            {
                var child = element.hierarchy[i];
                if (!IsShown(child) || IsExempt(child))
                {
                    continue;
                }

                var bounds = child.worldBound;
                if (bounds.width > 0 || bounds.height > 0)
                {
                    if (!Inside(bounds, containerBounds))
                    {
                        result.Overflows.Add(Describe(child) + " fuera de " + Describe(container) + " (" + Format(bounds) + " en "
                            + Format(containerBounds) + ")");
                    }
                }

                CheckText(child, result);

                var nextContainer = container;
                var nextBounds = containerBounds;
                if (IsContainer(child))
                {
                    nextContainer = child;
                    nextBounds = bounds;
                }

                if (child is ScrollView scroll)
                {
                    // El viewport y su contenido: el contenido es el contenedor de lo que va adentro.
                    var content = scroll.contentContainer;
                    VisitScrollView(scroll, content, result);
                    continue;
                }

                Visit(child, nextContainer, nextBounds, result);
            }
        }

        private static void VisitScrollView(ScrollView scroll, VisualElement content, LayoutAuditResult result)
        {
            var bounds = content.worldBound;
            Visit(content, content, bounds, result);
        }

        private static void CheckText(VisualElement element, LayoutAuditResult result)
        {
            if (element is TabularNumber number)
            {
                var box = number.contentRect;
                if (number.NaturalWidth > box.width + Tolerance && !number.IsRolling)
                {
                    result.ClippedTexts.Add(Describe(element) + " \"" + number.Text + "\" más ancho que su caja ("
                        + number.NaturalWidth.ToString("0.#", CultureInfo.InvariantCulture) + " > " + box.width.ToString("0.#", CultureInfo.InvariantCulture) + ")");
                }

                return;
            }

            if (element is FitBox fit)
            {
                if (!fit.Fits)
                {
                    result.ClippedTexts.Add(Describe(element) + ": el contenido no entra (escala " + fit.AppliedScale.ToString("0.###", CultureInfo.InvariantCulture) + ")");
                }

                return;
            }

            if (!(element is Label label) || element is IconView || string.IsNullOrEmpty(label.text))
            {
                return;
            }

            var rect = label.contentRect;
            if (!(rect.width > 0))
            {
                return;
            }

            var style = label.resolvedStyle;
            if (style.whiteSpace == WhiteSpace.NoWrap)
            {
                var natural = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                var ellipsis = style.textOverflow == TextOverflow.Ellipsis;
                if (natural.x > rect.width + Tolerance && !ellipsis)
                {
                    result.ClippedTexts.Add(Describe(element) + " \"" + Short(label.text) + "\" no entra en una línea (" + natural.x.ToString("0.#", CultureInfo.InvariantCulture)
                        + " > " + rect.width.ToString("0.#", CultureInfo.InvariantCulture) + ")");
                }
            }
            else
            {
                var natural = label.MeasureTextSize(label.text, rect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                if (natural.y > rect.height + Tolerance + 1f)
                {
                    result.ClippedTexts.Add(Describe(element) + " \"" + Short(label.text) + "\" más alto que su caja (" + natural.y.ToString("0.#", CultureInfo.InvariantCulture)
                        + " > " + rect.height.ToString("0.#", CultureInfo.InvariantCulture) + ")");
                }
            }
        }

        private static bool IsShown(VisualElement element)
        {
            return element.resolvedStyle.display != DisplayStyle.None && element.resolvedStyle.visibility != Visibility.Hidden;
        }

        private static bool IsExempt(VisualElement element)
        {
            for (var i = 0; i < ExemptClasses.Length; i++)
            {
                if (element.ClassListContains(ExemptClasses[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsContainer(VisualElement element)
        {
            for (var i = 0; i < ContainerClasses.Length; i++)
            {
                if (element.ClassListContains(ContainerClasses[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Inside(Rect inner, Rect outer)
        {
            return inner.xMin >= outer.xMin - Tolerance && inner.yMin >= outer.yMin - Tolerance
                   && inner.xMax <= outer.xMax + Tolerance && inner.yMax <= outer.yMax + Tolerance;
        }

        public static string Describe(VisualElement element)
        {
            if (!string.IsNullOrEmpty(element.name))
            {
                return "#" + element.name;
            }

            foreach (var className in element.GetClasses())
            {
                return "." + className;
            }

            return element.GetType().Name;
        }

        private static string Format(Rect rect)
        {
            return rect.x.ToString("0.#", CultureInfo.InvariantCulture) + "," + rect.y.ToString("0.#", CultureInfo.InvariantCulture) + " " + rect.width.ToString("0.#", CultureInfo.InvariantCulture) + "×" + rect.height.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string Short(string text)
        {
            return text.Length <= 40 ? text : text.Substring(0, 40) + "…";
        }
    }

    public sealed class LayoutAuditResult
    {
        public List<string> Overflows { get; } = new List<string>();

        public List<string> ClippedTexts { get; } = new List<string>();

        public bool IsClean => Overflows.Count == 0 && ClippedTexts.Count == 0;

        public override string ToString()
        {
            if (IsClean)
            {
                return "sin desbordes";
            }

            return string.Join("\n", Overflows) + (Overflows.Count > 0 && ClippedTexts.Count > 0 ? "\n" : "") + string.Join("\n", ClippedTexts);
        }
    }
}
