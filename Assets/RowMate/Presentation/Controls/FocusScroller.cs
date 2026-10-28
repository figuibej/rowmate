using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Lleva a la vista lo que importa dentro de un ScrollView (QA-G3-11, DN3-10, DN3-18; 03-rutinas/02-tech-design §4.11):
    // - el elemento que recibe el foco por Tab (FocusInEvent, que burbujea desde el hijo): un ScrollView de UI Toolkit no se
    //   desplaza solo, como hace Flutter con Scrollable.ensureVisible (<flutter-sdk>/packages/flutter/lib/src/widgets/
    //   focus_traversal.dart:204-213); con 16 dp de aire y un resorte ω 22;
    // - lo nuevo (una fila o una serie agregada; ω 12) y el primer campo con error (ω 22), por código (ScrollIntoView).
    // Si ya se ve entero con su aire, no se mueve. Reducir movimiento: en el acto. bottomInset() descuenta lo que tapa el
    // borde inferior del viewport (la barra de pestañas o el teclado).
    public sealed class FocusScroller
    {
        private readonly UiContext _ui;
        private readonly ScrollView _scroll;
        private readonly SpringMotion _offset;
        private Func<double>? _bottomInset;

        public FocusScroller(UiContext ui, ScrollView scroll)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            _offset = new SpringMotion(ui.Motion, ApplyOffset);
            scroll.userData = this;
            scroll.contentContainer.RegisterCallback<FocusInEvent>(OnFocusIn);
            scroll.RegisterCallback<WheelEvent>(_ => _offset.Stop(), TrickleDown.TrickleDown);
            scroll.RegisterCallback<PointerDownEvent>(_ => _offset.Stop(), TrickleDown.TrickleDown);
        }

        public ScrollView Scroll => _scroll;

        public bool IsAnimating => _offset.IsRunning;

        public double Margin { get; set; } = MotionSpecs.ScrollMarginDp;

        public void SetBottomInset(Func<double>? inset)
        {
            _bottomInset = inset;
        }

        // Desplaza hasta que element se vea entero con Margin de aire (y su texto de error, si se pasa extraBelow).
        public void ScrollIntoView(VisualElement element, double omega, VisualElement? extraBelow = null)
        {
            if (element == null || element.panel == null)
            {
                return;
            }

            var viewport = _scroll.contentViewport.worldBound;
            if (!(viewport.height > 0))
            {
                return;
            }

            var top = viewport.yMin;
            var bottom = viewport.yMax - (float)(_bottomInset?.Invoke() ?? 0);
            var bounds = element.worldBound;
            if (extraBelow != null && extraBelow.resolvedStyle.display != DisplayStyle.None && extraBelow.worldBound.height > 0)
            {
                bounds = Rect.MinMaxRect(bounds.xMin, bounds.yMin, bounds.xMax, Mathf.Max(bounds.yMax, extraBelow.worldBound.yMax));
            }

            var margin = (float)Margin;
            var current = (double)_scroll.scrollOffset.y;
            var target = current;
            if (bounds.yMax + margin > bottom)
            {
                target = current + (bounds.yMax + margin - bottom);
            }

            if (bounds.yMin - margin < top + (target - current))
            {
                target = current + (bounds.yMin - margin - top);
            }

            var max = MaxOffset();
            target = target < 0 ? 0 : target > max ? max : target;
            if (Math.Abs(target - current) < 0.5)
            {
                return;
            }

            if (!_offset.HasValue || !_offset.IsRunning)
            {
                _offset.Snap(current);
            }

            _offset.MoveTo(target, omega);
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            if (!(evt.target is VisualElement element))
            {
                return;
            }

            // Solo el foco por teclado (Tab): un toque ya está a la vista.
            if (!IsKeyboardNavigation())
            {
                return;
            }

            ScrollIntoView(OwnerOf(element), MotionSpecs.ScrollToFocusOmega);
        }

        private double MaxOffset()
        {
            var content = _scroll.contentContainer.layout.height;
            var viewport = _scroll.contentViewport.layout.height;
            return Math.Max(0, content - viewport);
        }

        private void ApplyOffset(double value)
        {
            var offset = _scroll.scrollOffset;
            offset.y = (float)value;
            _scroll.scrollOffset = offset;
        }

        private bool IsKeyboardNavigation()
        {
            for (var e = _scroll.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e.ClassListContains(FocusHighlight.KeyboardNavClass))
                {
                    return true;
                }
            }

            return false;
        }

        // El control que se ve (un TextField recibe el foco en su elemento interno; se muestra el campo entero).
        private static VisualElement OwnerOf(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e.ClassListContains(RmTextField.UssClass))
                {
                    return e;
                }
            }

            return element;
        }
    }
}
