using System;
using RowMate.Bootstrap;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Tests.UiSupport
{
    // Los assets que UiTestHost necesita (el PanelSettings del proyecto con el tema real y los dos catálogos), leídos con
    // AssetDatabase: las pruebas PlayMode y el arnés corren en el Editor. Si el bootstrapper v3 todavía no creó el panel,
    // falla con el paso a correr.
    public static class UiAssetsLocator
    {
        public static PanelSettings Panel()
        {
#if UNITY_EDITOR
            var panel = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(UiAssets.PanelPath);
            if (panel == null)
            {
                throw new InvalidOperationException("falta " + UiAssets.PanelPath + ": correr RowMate/Bootstrap/Reaplicar (paso ui.panel)");
            }

            return panel;
#else
            throw new InvalidOperationException("UiTestHost solo corre en el Editor.");
#endif
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
