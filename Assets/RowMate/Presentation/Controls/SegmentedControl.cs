using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
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
    // Con un MotionDriver, la opción elegida cruza fondo, borde (mosaicos) y etiqueta en 160 ease.move, y la anterior al revés
    // (ColorFade; 04-design-notes §5.7; QA-V3-02). Sin él, o antes de estar en un panel, en el acto.
    public sealed class SegmentedControl : VisualElement
    {
        public const string UssClass = "rm-segmented";
        public const string OptionClass = "rm-segmented__option";
        public const string SelectedClass = "rm-segmented__option--selected";

        private readonly List<Option> _options = new List<Option>();
        private readonly bool _tiles;
        private readonly MotionDriver? _motion;
        private int _selected = -1;

        public SegmentedControl(bool tiles, MotionDriver? motion = null)
        {
            _tiles = tiles;
            _motion = motion;
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
            _options.Add(new Option(button, text, icon, tileBase, _motion != null ? new ColorFade(_motion) : null));
            Paint(index);
            return button;
        }

        // El cruce de colores de una opción (null sin MotionDriver): para las pruebas.
        public ColorFade? FadeOf(int index)
        {
            return _options[index].Fade;
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
            var fade = option.Fade != null && option.Button.panel != null && _motion!.Armed;
            if (fade)
            {
                // Desde lo que se ve: el estado contrario (o el color a mitad de un cruce que se interrumpe).
                var cross = option.Fade!;
                var fromBg = cross.CurrentOr(option.Button, ColorFade.Channel.Background, Background(option, !selected));
                var fromLabel = cross.CurrentOr(option.Label, ColorFade.Channel.Text, LabelColor(!selected));
                var fromBorder = cross.CurrentOr(option.Button, ColorFade.Channel.Border, BorderColor(option, !selected));
                cross.Reset();
                // Mosaicos: fondo y borde los pinta el código (quedan en línea); segmento: el fondo es del USS (--selected).
                var tiles = IsTile(option);
                cross.Add(option.Button, ColorFade.Channel.Background, fromBg, Background(option, selected), !tiles);
                if (tiles)
                {
                    cross.Add(option.Button, ColorFade.Channel.Border, fromBorder, BorderColor(option, selected), false);
                }

                cross.Add(option.Label, ColorFade.Channel.Text, fromLabel, LabelColor(selected), true);
            }

            option.Button.EnableInClassList(SelectedClass, selected);
            if (fade)
            {
                option.Fade!.Play();
                return;
            }

            if (!IsTile(option))
            {
                return;
            }

            var border = BorderColor(option, selected);
            var style = option.Button.style;
            style.borderLeftColor = border;
            style.borderRightColor = border;
            style.borderTopColor = border;
            style.borderBottomColor = border;
            style.backgroundColor = Background(option, selected);
        }

        private bool IsTile(Option option)
        {
            return _tiles && option.TileBase != null;
        }

        // Fondo de la opción: mosaico elegido tipo @20 %; segmento elegido brand.seed (el de Base.uss); si no, transparente.
        private Color Background(Option option, bool selected)
        {
            if (!selected)
            {
                return Color.clear;
            }

            return IsTile(option) ? TokenColors.WithAlpha(option.TileBase!, 0.2) : TokenColors.ToColor(Tokens.BrandSeed);
        }

        private static Color BorderColor(Option option, bool selected)
        {
            return selected && option.TileBase != null ? TokenColors.ToColor(option.TileBase) : TokenColors.ToColor(Tokens.BorderDefault);
        }

        // La etiqueta: text.primary la elegida y text.secondary las demás (Base.uss).
        private static Color LabelColor(bool selected)
        {
            return TokenColors.ToColor(selected ? Tokens.TextPrimary : Tokens.TextSecondary);
        }

        private sealed class Option
        {
            public Option(Button button, Label label, IconView? icon, ColorToken? tileBase, ColorFade? fade)
            {
                Button = button;
                Label = label;
                Icon = icon;
                TileBase = tileBase;
                Fade = fade;
            }

            public Button Button { get; }

            public Label Label { get; }

            public IconView? Icon { get; }

            public ColorToken? TileBase { get; }

            public ColorFade? Fade { get; }
        }
    }
}
