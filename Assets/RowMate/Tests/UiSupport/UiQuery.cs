using System.Collections.Generic;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Tests.UiSupport
{
    // Consultas sobre el árbol de UI para las pruebas y el arnés (= find.byKey / find.text del legacy; QA-G2-14): los textos
    // salen de Label.text y de TabularNumber.Text (los números del tablero y del pulso se dibujan con DrawText, sin Label).
    public static class UiQuery
    {
        public static VisualElement? Find(VisualElement root, string elementName)
        {
            return root.Q(elementName);
        }

        // "nombre" o "nombre .clase" (el primer descendiente con esa clase dentro del elemento con ese nombre).
        public static VisualElement? Resolve(VisualElement root, string spec)
        {
            var space = spec.IndexOf(' ');
            if (space < 0)
            {
                return root.Q(spec);
            }

            var parent = root.Q(spec.Substring(0, space));
            var className = spec.Substring(space + 1).Trim().TrimStart('.');
            return parent?.Q(className: className);
        }

        public static string? TextOf(VisualElement? element)
        {
            switch (element)
            {
                case null:
                    return null;
                case TabularNumber number:
                    return number.Text;
                case RmButton button:
                    return button.Label.text;
                case Label label:
                    return label.text;
                case Button plain:
                    var buttonLabel = plain.Q<Label>();
                    return buttonLabel != null ? buttonLabel.text : plain.text;
                default:
                    var inner = element.Q<Label>();
                    return inner?.text;
            }
        }

        // Visible = display y visibility en toda la cadena, opacidad > 0 en la cadena y dentro de la ventana.
        public static bool IsShown(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                var style = e.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden || style.opacity <= 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInside(VisualElement element, Rect window)
        {
            var bounds = element.worldBound;
            return bounds.width > 0 && bounds.height > 0 && bounds.xMax > window.xMin && bounds.xMin < window.xMax
                   && bounds.yMax > window.yMin && bounds.yMin < window.yMax;
        }

        // ≈ find.text: algún Label o TabularNumber visible y dentro de la ventana con ese texto exacto.
        public static bool IsTextVisible(VisualElement root, string text)
        {
            var window = root.worldBound;
            foreach (var element in TextElements(root))
            {
                if (TextOf(element) == text && IsShown(element) && IsInside(element, window))
                {
                    return true;
                }
            }

            // It. 5 (05-series-parciales/02-tech-design §3.1; QA-G5P-05): las ranuras de lo que se dibuja sin Label (los paneles de
            // series), con coincidencia exacta y su alfa efectivo (≤ 0,01 no se ve).
            foreach (var (owner, slot) in Slots(root))
            {
                if (slot.Text == text && slot.IsVisible && IsShown(owner) && Overlaps(slot.WorldRect, window))
                {
                    return true;
                }
            }

            return false;
        }

        // ≈ find.text(skipOffstage: false): el texto existe en el árbol (sin mirar posición ni visibilidad).
        public static bool HasText(VisualElement root, string text)
        {
            foreach (var element in TextElements(root))
            {
                if (TextOf(element) == text)
                {
                    return true;
                }
            }

            foreach (var (_, slot) in Slots(root))
            {
                if (slot.Text == text)
                {
                    return true;
                }
            }

            return false;
        }

        public static VisualElement? FindByText(VisualElement root, string text)
        {
            var window = root.worldBound;
            VisualElement? fallback = null;
            foreach (var element in TextElements(root))
            {
                if (TextOf(element) != text)
                {
                    continue;
                }

                if (IsShown(element) && IsInside(element, window))
                {
                    return element;
                }

                fallback ??= element;
            }

            return fallback;
        }

        // Todos los textos visibles en la ventana (para "ningún otro texto", CAP-21).
        public static List<string> VisibleTexts(VisualElement root)
        {
            var texts = new List<string>();
            var window = root.worldBound;
            foreach (var element in TextElements(root))
            {
                var text = TextOf(element);
                if (!string.IsNullOrEmpty(text) && !(element is IconView) && IsShown(element) && IsInside(element, window))
                {
                    texts.Add(text!);
                }
            }

            foreach (var (owner, slot) in Slots(root))
            {
                if (!string.IsNullOrEmpty(slot.Text) && slot.IsVisible && IsShown(owner) && Overlaps(slot.WorldRect, window))
                {
                    texts.Add(slot.Text);
                }
            }

            return texts;
        }

        public static IEnumerable<VisualElement> TextElements(VisualElement root)
        {
            var stack = new Stack<VisualElement>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var element = stack.Pop();
                if (element is Label || element is TabularNumber)
                {
                    yield return element;
                }

                for (var i = element.hierarchy.childCount - 1; i >= 0; i--)
                {
                    stack.Push(element.hierarchy[i]);
                }
            }
        }

        // It. 5: los elementos que dibujan sus textos (IDrawnTextSource) y sus ranuras.
        public static IEnumerable<(VisualElement Owner, DrawnSlot Slot)> Slots(VisualElement root)
        {
            var sources = new List<VisualElement>();
            foreach (var element in root.Query<VisualElement>().Build())
            {
                if (element is IDrawnTextSource)
                {
                    sources.Add(element);
                }
            }

            foreach (var owner in sources)
            {
                var source = (IDrawnTextSource)owner;
                var count = source.SlotCount;
                for (var i = 0; i < count; i++)
                {
                    yield return (owner, source.SlotAt(i));
                }
            }
        }

        // La ranura con ese nombre (la junta de nombres de §3.6: primero un elemento, después una ranura), o null.
        public static (VisualElement Owner, DrawnSlot Slot)? FindSlot(VisualElement root, string slotName)
        {
            foreach (var pair in Slots(root))
            {
                if (pair.Slot.Name == slotName)
                {
                    return pair;
                }
            }

            return null;
        }

        // La ranura visible con ese texto exacto (la de TapText), o null.
        public static (VisualElement Owner, DrawnSlot Slot)? FindSlotByText(VisualElement root, string text)
        {
            var window = root.worldBound;
            foreach (var pair in Slots(root))
            {
                if (pair.Slot.Text == text && pair.Slot.IsVisible && IsShown(pair.Owner) && Overlaps(pair.Slot.WorldRect, window))
                {
                    return pair;
                }
            }

            return null;
        }

        private static bool Overlaps(Rect a, Rect window)
        {
            return a.width > 0 && a.height > 0 && a.xMax > window.xMin && a.xMin < window.xMax && a.yMax > window.yMin && a.yMin < window.yMax;
        }

        // El ancestro que es un ScrollView (para ScrollTo), o null.
        public static ScrollView? ScrollViewOf(VisualElement element)
        {
            for (var e = element.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e is ScrollView scroll)
                {
                    return scroll;
                }
            }

            return null;
        }

        public static string Describe(VisualElement? element)
        {
            if (element == null)
            {
                return "(nada)";
            }

            if (!string.IsNullOrEmpty(element.name))
            {
                return "#" + element.name;
            }

            for (var e = element.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (!string.IsNullOrEmpty(e.name))
                {
                    return element.GetType().Name + " dentro de #" + e.name;
                }
            }

            return element.GetType().Name;
        }
    }
}
