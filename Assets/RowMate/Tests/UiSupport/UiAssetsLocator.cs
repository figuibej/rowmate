using System;
using RowMate.Bootstrap;
using UnityEngine;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace RowMate.Tests.UiSupport
{
    // Los assets que UiTestHost necesita, leídos con AssetDatabase (las pruebas PlayMode y el arnés corren en el Editor): el
    // MISMO UiAssets.asset que usa AppRoot (panel con el tema real, respaldos de fuente) y los dos catálogos. Así UiTestHost y
    // el arnés arman el panel igual que la app (RuntimePanelFactory, QA-G3-12). Si el bootstrapper todavía no lo creó, falla
    // con el paso a correr.
    public static class UiAssetsLocator
    {
        public static UiAssets Assets()
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            if (assets == null)
            {
                throw new InvalidOperationException("falta " + UiAssets.AssetPath + ": correr RowMate/Bootstrap/Reaplicar (paso ui.panel)");
            }

            return assets;
#else
            throw new InvalidOperationException("UiTestHost solo corre en el Editor.");
#endif
        }

        public static PanelSettings Panel()
        {
#if UNITY_EDITOR
            var panel = Assets().Panel ?? UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(UiAssets.PanelPath);
            if (panel == null)
            {
                throw new InvalidOperationException("falta " + UiAssets.PanelPath + ": correr RowMate/Bootstrap/Reaplicar (paso ui.panel)");
            }

            return panel;
#else
            throw new InvalidOperationException("UiTestHost solo corre en el Editor.");
#endif
        }

        // Respaldo 1 del panel (el glifo trending_up del subtítulo del paso).
        public static FontAsset? IconsOutlineFallback()
        {
            return Assets().IconsOutlineFallback;
        }

        // Respaldo 2 del panel (Noto Emoji).
        public static FontAsset? EmojiFallback()
        {
            return Assets().EmojiFallback;
        }

        public static string StringsEs()
        {
            return Text(UiAssets.StringsEsPath);
        }

        public static string StringsEn()
        {
            return Text(UiAssets.StringsEnPath);
        }

        private static string Text(string path)
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("falta " + path);
            }

            return asset.text;
#else
            throw new InvalidOperationException("UiTestHost solo corre en el Editor.");
#endif
        }
    }
}
