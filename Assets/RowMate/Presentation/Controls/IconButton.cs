using System;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Botón de ícono (DN-18; 04-design-notes §4.7 de la It. 2): caja 48 × 48, ícono 24 por defecto, .rm-button__state circular
    // de 40 (WP-D) y anillo de foco. Lo usan #sim-close y los ± del panel del simulador y, desde la It. 3, la flecha de volver,
    // el ⋮, el lápiz, la cruz y el tacho (ícono de 20 por parámetro, DN3-17). Variante --danger (cruz y tacho en
    // state.dangerText). Con ayuda emergente (SetTooltip, AC-APP-03-06), un toque largo que la mostró no activa el botón.
    public sealed class IconButton : Button
    {
        public const string UssClass = "rm-icon-button";
        public const float DefaultIconDp = 24;

        private readonly Action? _onClick;

        public IconButton(string name, string codepoint, Action? onClick, float iconDp = DefaultIconDp, bool danger = false)
        {
            this.name = name;
            text = "";
            RmButton.StripUnityChrome(this);
            AddToClassList(UssClass);
            EnableInClassList(UssClass + "--danger", danger);
            focusable = true;
            style.flexShrink = 0;
            Add(RmButton.StateLayer());
            Icon = new IconView(codepoint, false, iconDp);
            Icon.AddToClassList("rm-icon-button__icon");
            Add(Icon);
            Add(RmButton.FocusRing());
            _onClick = onClick;
            if (onClick != null)
            {
                clicked += OnClicked;
            }
        }

        public IconView Icon { get; }

        // La ayuda emergente (solo flecha, ⋮ y tacho).
        public Tooltip? Help { get; private set; }

        // Texto de la ayuda, que también es el nombre accesible (DT-05).
        public string? AccessibleName => Help?.AccessibleName;

        public void SetTooltip(UiContext ui, Func<string> text)
        {
            Help = Tooltip.Attach(ui, this, text);
        }

        private void OnClicked()
        {
            if (Help != null && Help.TakeConsumedClick())
            {
                return;
            }

            _onClick?.Invoke();
        }
    }
}
