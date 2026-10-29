using System;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Presentation.Controls;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // La barra superior del entrenamiento libre (AC-WKT-16-01; legacy immersive_workout_screen.dart:491-522; 02-tech-design
    // §6.6 de la It. 4a; 04-design-notes §3.3, DN4a-04):
    //   #hud-topbar .rm-hud-topbar .rm-hud-topbar--free .rm-hud-glass     alto 40, relleno 14 a los lados, borde 1 border.subtle
    //   └── #hud-free-title                rowing 20 en accent.primary, 8, hudFreeTitle 16/600 blanco
    // La misma caja de 40 que la de pasos: SPM queda a la misma distancia en libre y con rutina. Sin eventos. El reloj de pared
    // y el chip de fase son de la 4b (DV-34). Es la zona: HudZones la ancla arriba a la izquierda y la escala por s.
    public sealed class FreeTopBar : VisualElement
    {
        public const float IconSizeDp = 20;
        public const float TitleSizeDp = 16;

        public FreeTopBar(UiContext ui)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

            name = "hud-topbar";
            AddToClassList("rm-hud-topbar");
            AddToClassList("rm-hud-topbar--free");
            AddToClassList("rm-hud-glass");
            pickingMode = PickingMode.Ignore;
            HudTopBars.ApplyFrame(this, HudGeometry.FreeRowPadH);

            Row = new VisualElement { name = "hud-free-title" };
            Row.AddToClassList("rm-hud-free-title");
            Row.pickingMode = PickingMode.Ignore;
            Icon = new IconView(Icons.Rowing, false, IconSizeDp);
            Icon.AddToClassList("rm-hud-free-title__icon");
            Icon.style.color = TokenColors.ToColor(Tokens.AccentPrimary);
            Row.Add(Icon);
            Title = HudText.Label("hud-free-title-text", "rm-type-title-s", TitleSizeDp);
            Title.AddToClassList("rm-hud-free-title__text");
            Title.style.color = TokenColors.ToColor(Tokens.TextPrimary);
            Row.Add(Title);
            Add(Row);
        }

        public VisualElement Row { get; }

        public IconView Icon { get; }

        public Label Title { get; }

        public void RenderTexts(Localizer texts)
        {
            Title.text = texts.Get(L10nKeys.HudFreeTitle);
        }
    }

    // El marco común de las dos barras superiores (DN4a-04): vidrio, borde 1 border.subtle (dentro del relleno), radio 14 y
    // alto 40 = 1 + 8 + 22 + 8 + 1.
    internal static class HudTopBars
    {
        public static void ApplyFrame(VisualElement bar, double padH)
        {
            var border = TokenColors.ToColor(Tokens.BorderSubtle);
            bar.style.borderLeftColor = border;
            bar.style.borderRightColor = border;
            bar.style.borderTopColor = border;
            bar.style.borderBottomColor = border;
            bar.style.borderLeftWidth = 1;
            bar.style.borderRightWidth = 1;
            bar.style.borderTopWidth = 1;
            bar.style.borderBottomWidth = 1;
            bar.style.height = (float)HudGeometry.TopBarHeight;
            bar.style.paddingLeft = (float)padH;
            bar.style.paddingRight = (float)padH;
            bar.style.paddingTop = (float)HudGeometry.TopBarPadV;
            bar.style.paddingBottom = (float)HudGeometry.TopBarPadV;
        }
    }
}
