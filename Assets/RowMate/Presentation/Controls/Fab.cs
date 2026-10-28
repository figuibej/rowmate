using System;
using RowMate.Core.Design;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Controls
{
    // Botón flotante de 56 (AC-RUT-10-01, -04; legacy FloatingActionButton, routines_screen.dart:17-23; 04-design-notes §4.1):
    //   #<nombre> .rm-fab   círculo 56, accent.primary, ícono 24 en text.onAccent; capa de estado y anillo de foco como RmButton
    // La posición la pone quien lo usa (right 16, bottom = alto real de la barra + 16).
    public sealed class Fab : Button
    {
        public const string UssClass = "rm-fab";
        public const float SizeDp = 56;
        public const float RightDp = 16;
        public const float GapAboveTabBarDp = 16;

        public Fab(string name, string codepoint, Action onClick)
        {
            this.name = name;
            text = "";
            RmButton.StripUnityChrome(this);
            AddToClassList(UssClass);
            focusable = true;
            style.position = Position.Absolute;
            style.right = RightDp;
            Add(RmButton.StateLayer());
            Icon = new IconView(codepoint, false, 24);
            Icon.AddToClassList("rm-fab__icon");
            Add(Icon);
            Add(RmButton.FocusRing());
            if (onClick != null)
            {
                clicked += onClick;
            }
        }

        public IconView Icon { get; }

        // El rectángulo del botón como obstáculo inferior (dp de la ventana: x desde la izquierda, y desde abajo; DN3-14).
        public static BottomObstacle ObstacleFor(double windowWidthDp, double bottomDp)
        {
            var right = windowWidthDp - RightDp;
            return new BottomObstacle(right - SizeDp, right, bottomDp, SizeDp);
        }
    }
}
