using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Anillo de foco solo navegando con teclado (AC-APP-05-07; como Flutter en escritorio): pone .rm-keyboard-nav en la raíz
    // de la app al usar Tab o las flechas y la saca con el primer toque o clic. Los anillos (.rm-focus-ring) los muestra el USS
    // con .rm-keyboard-nav … :focus > .rm-focus-ring.
    // QA-V4a-04: escucha en la raíz del PANEL (panel.visualTree), no en la de la app. Sin ningún control enfocado (después de
    // un clic, DT4a-01), UI Toolkit manda el KeyDown y el NavigationMoveEvent del primer Tab a la raíz del panel: ese camino no
    // pasa por #app-root, así que el primer Tab enfocaba sin anillo y un Enter activaba algo que no se veía (en el editor de
    // Rutinas, `←`). Desde la raíz del panel, con TrickleDown, se ven todos los eventos de teclado y de puntero del panel,
    // también los que van a la raíz misma. La clase sigue en #app-root, donde la buscan el USS, ExitLock y PopupLayer.
    public sealed class FocusHighlight
    {
        public const string KeyboardNavClass = "rm-keyboard-nav";

        private readonly VisualElement _root;
        private readonly EventCallback<KeyDownEvent> _onKey;
        private readonly EventCallback<NavigationMoveEvent> _onMove;
        private readonly EventCallback<PointerDownEvent> _onPointer;
        private VisualElement? _listening;

        public FocusHighlight(VisualElement root)
        {
            _root = root;
            _onKey = OnKey;
            _onMove = _ => SetKeyboard(true);
            _onPointer = _ => SetKeyboard(false);
            root.RegisterCallback<AttachToPanelEvent>(evt => Listen(evt.destinationPanel?.visualTree));
            root.RegisterCallback<DetachFromPanelEvent>(_ => Listen(null));
            Listen(root.panel?.visualTree);
        }

        public bool IsKeyboardNavigation => _root.ClassListContains(KeyboardNavClass);

        // Dónde escucha (la raíz del panel; null fuera de un panel). Para las pruebas.
        public VisualElement? ListeningOn => _listening;

        public void SetKeyboard(bool keyboard)
        {
            _root.EnableInClassList(KeyboardNavClass, keyboard);
        }

        private void Listen(VisualElement? panelRoot)
        {
            if (ReferenceEquals(panelRoot, _listening))
            {
                return;
            }

            if (_listening != null)
            {
                _listening.UnregisterCallback(_onKey, TrickleDown.TrickleDown);
                _listening.UnregisterCallback(_onMove, TrickleDown.TrickleDown);
                _listening.UnregisterCallback(_onPointer, TrickleDown.TrickleDown);
            }

            _listening = panelRoot;
            if (panelRoot != null)
            {
                panelRoot.RegisterCallback(_onKey, TrickleDown.TrickleDown);
                panelRoot.RegisterCallback(_onMove, TrickleDown.TrickleDown);
                panelRoot.RegisterCallback(_onPointer, TrickleDown.TrickleDown);
            }
        }

        private void OnKey(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Tab:
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                    SetKeyboard(true);
                    break;
            }
        }
    }
}
