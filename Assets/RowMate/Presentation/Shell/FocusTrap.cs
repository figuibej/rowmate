using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // Tab y Mayús+Tab dentro del diálogo o menú de arriba (AC-APP-03-03, AC-RUT-13-13; 03-rutinas/02-tech-design §4.5): el
    // NavigationMoveEvent Next/Previous que llega al árbol del diálogo lleva el foco al siguiente (o anterior) enfocable del
    // diálogo en orden de árbol, con vuelta, y el FocusController ignora el evento (no sigue al resto de la pantalla). API
    // confirmada en S3-b (FocusController.IgnoreEvent). Release() la suelta (en el cuadro 0 de la salida).
    public sealed class FocusTrap
    {
        private readonly VisualElement _root;
        private readonly EventCallback<NavigationMoveEvent> _onMove;
        private bool _active = true;

        public FocusTrap(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _onMove = OnMove;
            root.RegisterCallback(_onMove, TrickleDown.TrickleDown);
        }

        public bool IsActive => _active;

        public void Release()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            _root.UnregisterCallback(_onMove, TrickleDown.TrickleDown);
        }

        // El siguiente enfocable de root después de current (o el primero), en orden de árbol, con vuelta.
        public static Focusable? Next(VisualElement root, VisualElement? current, bool forward)
        {
            var list = new List<VisualElement>();
            Collect(root, list);
            if (list.Count == 0)
            {
                return null;
            }

            var index = current == null ? -1 : IndexOf(list, current);
            if (index < 0)
            {
                return forward ? list[0] : list[list.Count - 1];
            }

            var next = forward ? index + 1 : index - 1;
            if (next >= list.Count)
            {
                next = 0;
            }
            else if (next < 0)
            {
                next = list.Count - 1;
            }

            return list[next];
        }

        public static void Collect(VisualElement element, List<VisualElement> into)
        {
            if (element.resolvedStyle.display == DisplayStyle.None || element.resolvedStyle.visibility == Visibility.Hidden || !element.enabledInHierarchy)
            {
                return;
            }

            // Un TextField cuenta como un solo control (su foco va al elemento interno); no se baja a sus hijos.
            if (element is TextField)
            {
                into.Add(element);
                return;
            }

            if (element.focusable && element.canGrabFocus && element.tabIndex >= 0 && !(element is ScrollView) && !IsInnerTextInput(element))
            {
                into.Add(element);
            }

            var count = element.hierarchy.childCount;
            for (var i = 0; i < count; i++)
            {
                Collect(element.hierarchy[i], into);
            }
        }

        private void OnMove(NavigationMoveEvent evt)
        {
            if (!_active)
            {
                return;
            }

            var direction = evt.direction;
            if (direction != NavigationMoveEvent.Direction.Next && direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            var focused = _root.panel?.focusController?.focusedElement as VisualElement;
            var current = OwnerOf(focused);
            var next = Next(_root, current, direction == NavigationMoveEvent.Direction.Next);
            _root.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
            next?.Focus();
        }

        // Un TextField recibe el foco en su elemento interno: se cuenta el TextField (el control).
        private static VisualElement? OwnerOf(VisualElement? focused)
        {
            for (var e = focused; e != null; e = e.hierarchy.parent)
            {
                if (e is TextField)
                {
                    return e;
                }
            }

            return focused;
        }

        private static int IndexOf(List<VisualElement> list, VisualElement element)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], element) || list[i].Contains(element))
                {
                    return i;
                }
            }

            return -1;
        }

        // El elemento interno del TextField (#unity-text-input) no cuenta aparte del TextField.
        private static bool IsInnerTextInput(VisualElement element)
        {
            for (var e = element.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e is TextField)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
