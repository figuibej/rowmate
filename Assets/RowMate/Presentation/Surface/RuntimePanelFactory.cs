using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Presentation.Surface
{
    // La ÚNICA fábrica de la copia del PanelSettings de la app (QA-G3-12; 03-rutinas/02-tech-design §4.4; 04-design-notes
    // §4.15, DN3-21): instancia la copia (el asset del proyecto no se toca, R2-05) y le asigna una copia propia de
    // PanelTextSettings con los respaldos de fuente en este orden: RowMateIcons-Outline-SDF (el glifo trending_up del
    // subtítulo del paso, U+E8E5) y Noto Emoji (los emojis que escribe el usuario, P-09). Inter va primero por la
    // -unity-font-definition de cada clase .rm-type-*. La llaman AppRoot.Awake (antes del OnEnable del UIDocument, QA-G2-01),
    // UiTestHost.Create (con el host inactivo) y el arnés, que usa UiTestHost: los tres arman el panel igual. Sin la copia,
    // PlayMode y las capturas dibujarían □ en lugar del ícono de tendencia. Presentation no conoce UiAssets (Bootstrap):
    // cada anfitrión le pasa los campos del suyo.
    public static class RuntimePanelFactory
    {
        public static PanelSettings Create(PanelSettings source, FontAsset? iconsOutline, FontAsset? emoji, string name)
        {
            var panel = Object.Instantiate(source);
            panel.name = name;
            var text = ScriptableObject.CreateInstance<PanelTextSettings>();
            text.name = name + " (texto)";
            text.hideFlags = HideFlags.DontSave;
            text.fallbackFontAssets = FallbacksFor(iconsOutline, emoji);
            panel.textSettings = text;
            return panel;
        }

        // Los respaldos en el orden de 04-design-notes §4.15 (los que falten se saltean).
        public static List<FontAsset> FallbacksFor(FontAsset? iconsOutline, FontAsset? emoji)
        {
            var list = new List<FontAsset>(2);
            if (iconsOutline != null)
            {
                list.Add(iconsOutline);
            }

            if (emoji != null)
            {
                list.Add(emoji);
            }

            return list;
        }

        // Libera la copia del texto junto con el panel (la crea Create).
        public static void Destroy(PanelSettings panel)
        {
            if (panel == null)
            {
                return;
            }

            var text = panel.textSettings;
            panel.textSettings = null;
            DestroyObject(text);
            DestroyObject(panel);
        }

        private static void DestroyObject(Object? target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
