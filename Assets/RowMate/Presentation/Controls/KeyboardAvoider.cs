using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // El campo con foco sobre el teclado del teléfono (AC-RUT-11-10, F-APP-15.4; ARCHITECTURE §7.3; 03-rutinas/02-tech-design
    // §4.10; 04-design-notes §4.14, DN3-18). Lee ISoftKeyboard en cada Tick (AppShellView.Tick, desde ShellHandle.Tick):
    // - con el teclado a la vista, su alto en dp (px / densidad) se suma al relleno inferior de la página de arriba (por sus
    //   ShellLayout.RegisterClearance) y el diálogo de arriba se recentra en el área sobre el teclado con su alto máximo en
    //   área − 48 (DialogLayer.SetKeyboardInset);
    // - el campo con foco, con su texto de error, queda 16 dp por encima del teclado (FocusScroller, ω 22).
    // Reducir movimiento: en el acto. En escritorio el teclado nunca está a la vista. [Device-And]: NO VERIFICABLE sin módulo;
    // en PlayMode, con FakeSoftKeyboard.
    // El diálogo recentrado y los rellenos nuevos recién tienen su geometría en el layout del cuadro siguiente: el
    // desplazamiento se vuelve a apuntar cada vez que cambia el viewport del contenido (no cuando el usuario desplaza: eso
    // mueve el campo, no el viewport).
    public sealed class KeyboardAvoider
    {
        private readonly UiContext _ui;
        private double _occludedDp;
        private VisualElement? _lastFocus;
        private Rect _lastViewport;

        public KeyboardAvoider(UiContext ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        // Alto que tapa el teclado, en dp (0 si está oculto).
        public double OccludedDp => _occludedDp;

        public void Tick()
        {
            var keyboard = _ui.Keyboard;
            var density = _ui.Display.DpScale > 0 ? _ui.Display.DpScale : 1;
            var occluded = keyboard.IsVisible ? Math.Max(0, keyboard.OccludedHeightPx) / density : 0;
            var focused = FocusedField();
            var changed = Math.Abs(occluded - _occludedDp) > 0.5;
            if (changed)
            {
                _occludedDp = occluded;
                _ui.Dialogs.SetKeyboardInset(occluded);
                _ui.Layout.RefreshClearances();
            }

            if (occluded <= 0 || focused == null)
            {
                _lastFocus = focused;
                return;
            }

            var scroller = ScrollerOf(focused);
            var viewport = scroller != null ? scroller.Scroll.contentViewport.worldBound : Rect.zero;
            if (!changed && ReferenceEquals(focused, _lastFocus) && viewport == _lastViewport)
            {
                return;
            }

            _lastFocus = focused;
            _lastViewport = viewport;
            scroller?.ScrollIntoView(focused, MotionSpecs.KeyboardOmega, focused.ErrorLabel);
        }

        private RmTextField? FocusedField()
        {
            var focused = _ui.Pages.panel?.focusController?.focusedElement as VisualElement;
            for (var e = focused; e != null; e = e.hierarchy.parent)
            {
                if (e is RmTextField field)
                {
                    return field;
                }
            }

            return null;
        }

        // El FocusScroller del ScrollView que contiene al campo (el editor o el contenido del diálogo).
        private static FocusScroller? ScrollerOf(VisualElement element)
        {
            for (var e = element.hierarchy.parent; e != null; e = e.hierarchy.parent)
            {
                if (e is ScrollView scroll && scroll.userData is FocusScroller scroller)
                {
                    return scroller;
                }
            }

            return null;
        }
    }
}
