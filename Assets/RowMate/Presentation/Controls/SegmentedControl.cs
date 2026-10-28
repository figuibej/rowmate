using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Selector de una opción (03-rutinas/02-tech-design §4.5, DV-09, DN3-08; 04-design-notes §4.11):
    // - Variante por defecto (.rm-segmented): tiempo / distancia, dos opciones flex 1 1 0 en una cápsula de 40; la elegida con
    //   fondo brand.seed y texto blanco (ART_BIBLE §3.9). Legacy: SegmentedButton (routine_editor_screen.dart:587-594).
    // - Mosaicos (.rm-segmented--tiles): el tipo de paso, las 4 opciones visibles a la vez con el ícono arriba y el nombre
    //   abajo; 2 × 2 o 4 en fila según el ancho del contenido (SetColumns). Fondo (tipo @20 %) y borde (color base) de la
    //   elegida por código (USS no aplica transparencia a un var()).
    //   .rm-segmented [--tiles]
    //   └── #<nombre> .rm-segmented__option [--selected]   Button: .rm-button__state, [.rm-icon], .rm-segmented__label, anillo
    // Enter/Espacio o un toque eligen; Changed avisa con el índice (no avisa si ya estaba elegida).
    public sealed class SegmentedControl : VisualElement
    {
        public const string UssClass = "rm-segmented";
        public const string OptionClass = "rm-segmented__option";
        public const string SelectedClass = "rm-segmented__option--selected";

        private readonly List<Option> _options = new List<Option>();
        private readonly bool _tiles;
        private int _selected = -1;

        public SegmentedControl(bool tiles)
        {
            _tiles = tiles;
            AddToClassList(UssClass);
            EnableInClassList(UssClass + "--tiles", tiles);
            pickingMode = PickingMode.Ignore;
        }

        public int Selected => _selected;

        public int Count => _options.Count;

        public int Columns { get; private set; } = 4;

        public event Action<int>? Changed;

        public Button OptionAt(int index)
        {
            return _options[index].Button;
        }

        // Una opción; tileColors (solo mosaicos): borde y fondo @20 % de la elegida, e ícono en su variante legible.
        public Button AddOption(string name, string? iconCodepoint, string label, ColorToken? tileBase = null, ColorToken? tileIcon = null)
        {
            var index = _options.Count;
            var button = new Button { name = name, text = "" };
            RmButton.StripUnityChrome(button);
            button.AddToClassList(OptionClass);
            button.EnableInClassList(OptionClass + "--divided", !_tiles && index > 0);
            button.focusable = true;
            button.Add(RmButton.StateLayer());
            IconView? icon = null;
            if (!string.IsNullOrEmpty(iconCodepoint))
            {
                icon = new IconView(iconCodepoint!, false, 20);
                icon.AddToClassList(OptionClass + "-icon");
                if (tileIcon != null)
                {
                    icon.style.color = TokenColors.ToColor(tileIcon);
                }

                button.Add(icon);
            }

            var text = new Label(label);
            text.AddToClassList("rm-segmented__label");
            text.AddToClassList(_tiles ? "rm-type-button-s" : "rm-type-button");
            text.pickingMode = PickingMode.Ignore;
            button.Add(text);
            button.Add(RmButton.FocusRing());
            button.clicked += () => Select(index, true);
            Add(button);
            _options.Add(new Option(button, text, icon, tileBase));
            Paint(index);
            return button;
        }

        public void SetLabel(int index, string label)
        {
            if (_options[index].Label.text != label)
            {
                _options[index].Label.text = label;
            }
        }

        public void Select(int index, bool notify)
        {
            if (index < 0 || index >= _options.Count)
            {
                return;
            }

            if (index == _selected)
            {
                return;
            }

            var previous = _selected;
            _selected = index;
            if (previous >= 0)
            {
                Paint(previous);
            }

            Paint(index);
            if (notify)
            {
                Changed?.Invoke(index);
            }
        }

        // Mosaicos: 2 columnas (2 × 2) o 4 en fila (DN3-08: 4 desde 400 dp de contenido).
        public void SetColumns(int columns)
        {
            Columns = columns <= 2 ? 2 : 4;
            EnableInClassList(UssClass + "--two-columns", Columns == 2);
            EnableInClassList(UssClass + "--four-columns", Columns == 4);
        }

        private void Paint(int index)
        {
            var option = _options[index];
            var selected = index == _selected;
            option.Button.EnableInClassList(SelectedClass, selected);
            if (!_tiles || option.TileBase == null)
            {
                return;
            }

            var border = selected ? TokenColors.ToColor(option.TileBase) : TokenColors.ToColor(Tokens.BorderDefault);
            var style = option.Button.style;
            style.borderLeftColor = border;
            style.borderRightColor = border;
            style.borderTopColor = border;
            style.borderBottomColor = border;
            style.backgroundColor = selected ? TokenColors.WithAlpha(option.TileBase, 0.2) : new StyleColor(Color.clear);
        }

        private sealed class Option
        {
            public Option(Button button, Label label, IconView? icon, ColorToken? tileBase)
            {
                Button = button;
                Label = label;
                Icon = icon;
                TileBase = tileBase;
            }

            public Button Button { get; }

            public Label Label { get; }

            public IconView? Icon { get; }

            public ColorToken? TileBase { get; }
        }
    }
}
