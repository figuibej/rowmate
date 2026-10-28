using RowMate.Bootstrap;
using RowMate.Core.Design;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;
using RowMate.Presentation.Controls;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.EditorTools
{
    // Paso ui.panel del bootstrapper v3 (02-tech-design §1.3): Settings/UI/RowMatePanel.asset (tema RowMateTheme.tss,
    // ConstantPixelSize, escala 1, limpia a bg.app, sortingOrder 0, sin targetTexture: la RT y la escala por densidad las pone
    // la COPIA de AppRoot y de UiTestHost) y Settings/UI/UiAssets.asset (panel y catálogos; sin material de presentador: S-B1
    // eligió el presentador por copia, sin shader) y, desde la It. 3, los FontAsset de respaldo del panel (íconos de contorno y
    // Noto Emoji: RuntimePanelFactory, QA-G3-12). Idempotente: crea lo que falta y reasigna los valores.
    internal static class UiAssetsBuilder
    {
        public const string Folder = "Assets/RowMate/Settings/UI";

        public static BootstrapState.Step Apply(string id)
        {
            FontAssetBuilder.EnsureFolder(Folder);
            var created = "";
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiAssets.ThemePath);
            if (theme == null)
            {
                throw new System.IO.FileNotFoundException("no está el tema " + UiAssets.ThemePath);
            }

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(UiAssets.PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "RowMatePanel";
                Configure(panel, theme);
                AssetDatabase.CreateAsset(panel, UiAssets.PanelPath);
                created += " panel";
            }
            else
            {
                Configure(panel, theme);
                EditorUtility.SetDirty(panel);
            }

            var es = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssets.StringsEsPath);
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssets.StringsEnPath);
            if (es == null || en == null)
            {
                throw new System.IO.FileNotFoundException("faltan los catálogos " + UiAssets.StringsEsPath + " / " + UiAssets.StringsEnPath);
            }

            var icons = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPaths.IconsOutline);
            var emoji = AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPaths.EmojiFallback);
            var assets = AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<UiAssets>();
                assets.name = "UiAssets";
                assets.Set(panel, es, en, null);
                assets.SetFallbacks(icons, emoji);
                AssetDatabase.CreateAsset(assets, UiAssets.AssetPath);
                created += " UiAssets";
            }
            else
            {
                assets.Set(panel, es, en, null);
                assets.SetFallbacks(icons, emoji);
                EditorUtility.SetDirty(assets);
            }

            AssetDatabase.SaveAssets();
            return BootstrapState.Step.Ok(id, (created.Length > 0 ? "creado:" + created + "; " : "") + "panel con RowMateTheme.tss, ConstantPixelSize 1, fondo bg.app; respaldos: "
                + (icons != null ? "íconos" : "SIN íconos") + ", " + (emoji != null ? "Noto Emoji" : "SIN Noto Emoji"));
        }

        private static void Configure(PanelSettings panel, ThemeStyleSheet theme)
        {
            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.scale = 1;
            panel.clearColor = true;
            panel.colorClearValue = TokenColors.ToColor(Tokens.BgApp);
            panel.sortingOrder = 0;
            panel.targetTexture = null;
            panel.forceGammaRendering = false;
        }
    }
}
