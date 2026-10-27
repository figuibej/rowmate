using System;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Botón de ícono (DN-18; 04-design-notes §4.7): caja 48 × 48, ícono 24, .rm-button__state circular de 40 (WP-D) y anillo
    // de foco. Lo usan #sim-close y los ± del panel del simulador.
    public sealed class IconButton : Button
    {
        public const string UssClass = "rm-icon-button";

        public IconButton(string name, string codepoint, Action? onClick)
        {
            this.name = name;
            text = "";
            RmButton.StripUnityChrome(this);
            AddToClassList(UssClass);
            focusable = true;
            style.flexShrink = 0;
            Add(RmButton.StateLayer());
            Icon = new IconView(codepoint, false, 24);
            Icon.AddToClassList("rm-icon-button__icon");
            Add(Icon);
            Add(RmButton.FocusRing());
            if (onClick != null)
            {
                clicked += onClick;
            }
        }

        public IconView Icon { get; }
    }
}
