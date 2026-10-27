using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Anillo de foco solo navegando con teclado (AC-APP-05-07; como Flutter en escritorio): pone .rm-keyboard-nav en la raíz
    // al usar Tab o las flechas y la saca con el primer toque o clic. Los anillos (.rm-focus-ring) los muestra el USS con
    // .rm-keyboard-nav … :focus > .rm-focus-ring.
    public sealed class FocusHighlight
    {
        public const string KeyboardNavClass = "rm-keyboard-nav";

        private readonly VisualElement _root;

        public FocusHighlight(VisualElement root)
        {
            _root = root;
            root.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(_ => SetKeyboard(true), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(_ => SetKeyboard(false), TrickleDown.TrickleDown);
        }

        public bool IsKeyboardNavigation => _root.ClassListContains(KeyboardNavClass);

        public void SetKeyboard(bool keyboard)
        {
            _root.EnableInClassList(KeyboardNavClass, keyboard);
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
