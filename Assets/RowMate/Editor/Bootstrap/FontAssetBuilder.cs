using System.IO;
using RowMate.Core.Design;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;
using AtlasPopulationMode = UnityEngine.TextCore.Text.AtlasPopulationMode;

namespace RowMate.EditorTools
{
    // Paso ui.fonts del bootstrapper v3 (02-tech-design §1.3): crea los 10 FontAsset SOLO si no existen (desde la It. 3, también
    // el respaldo de emojis Noto Emoji, DN3-21: 64 / 6, 512²), con atlas y material
    // como subassets (verificado en S-0), y después reimporta los USS que los nombran (un USS importado antes de que exista
    // el FontAsset queda con MissingAssetReference y no se arregla solo, S-0). Las rutas salen de FontAssetPaths (las mismas
    // que usa el generador de Typography.uss). Ajustes de ASSET_SLOTS §3 y §4.2:
    //   Inter 400/500/600/700: 48 pt, padding 5 (10 %), atlas 1024²  ·  Barlow SC 600/700/800: 90 / 9, 1024²
    //   íconos de contorno: 64 / 6, 512²  ·  íconos rellenos: 64 / 6, 256²
    // Atlas dinámico (Dynamic) con clearDynamicDataOnBuild, para que el uso en el Editor no ensucie los .asset (R2-03). Desde
    // QA-V2-07 solo los escribe quien abre GeneratedAssetGuard.AllowWrites (este paso, dentro del bootstrapper): un FontAsset
    // recién creado ya está en el estado que se versiona (sin glifos, atlas de 1 × 1).
    // DN3-24 (R3-06, 03-rutinas/04-design-notes §4.16): las 7 caras de texto quedan con getFontFeatures = false (los que ya
    // existen y los que cree, solo si hace falta): el kerning es el de la tabla versionada y nada más, así un texto se ve
    // igual en cualquier orden de dibujo.
    internal static class FontAssetBuilder
    {
        public static readonly string[] StyleSheetsToReimport =
        {
            "Assets/RowMate/Presentation/Styles/Typography.uss",
            "Assets/RowMate/Presentation/Styles/Base.uss",
            "Assets/RowMate/SimulatorPresentation/Styles/SimulatorBase.uss",
            "Assets/RowMate/Presentation/Styles/RowMateTheme.tss",
        };

        public static BootstrapState.Step Apply(string id)
        {
            var created = 0;
            var existing = 0;
            var features = 0;
            foreach (var face in FontAssetPaths.TextFaces)
            {
                var numeric = face.Family == FontFamilyRole.Numeric;
                var path = FontAssetPaths.For(face.Family, face.Weight);
                if (Ensure(FontAssetPaths.SourceFor(face.Family, face.Weight), path, numeric ? 90 : 48, numeric ? 9 : 5, 1024, 1024))
                {
                    created++;
                }
                else
                {
                    existing++;
                }

                if (FreezeFontFeatures(path))
                {
                    features++;
                }
            }

            if (Ensure(FontAssetPaths.IconsOutlineSource, FontAssetPaths.IconsOutline, 64, 6, 512, 512))
            {
                created++;
            }
            else
            {
                existing++;
            }

            if (Ensure(FontAssetPaths.IconsFilledSource, FontAssetPaths.IconsFilled, 64, 6, 256, 256))
            {
                created++;
            }
            else
            {
                existing++;
            }

            if (Ensure(FontAssetPaths.EmojiFallbackSource, FontAssetPaths.EmojiFallback, 64, 6, 512, 512))
            {
                created++;
            }
            else
            {
                existing++;
            }

            AssetDatabase.SaveAssets();
            foreach (var path in StyleSheetsToReimport)
            {
                if (File.Exists(path))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }

            return BootstrapState.Step.Ok(id, "FontAsset creados: " + created + ", ya existían: " + existing + "; getFontFeatures apagado en "
                + features + "; USS reimportados");
        }

        // true si lo creó.
        private static bool Ensure(string sourcePath, string assetPath, int pointSize, int padding, int width, int height)
        {
            if (AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath) != null)
            {
                return false;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                throw new FileNotFoundException("no está la fuente " + sourcePath + " (ASSET_SLOTS §3-§4)");
            }

            EnsureFolder(Path.GetDirectoryName(assetPath)!.Replace('\\', '/'));
            var asset = FontAsset.CreateFontAsset(font, pointSize, padding, GlyphRenderMode.SDFAA, width, height, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                throw new System.InvalidOperationException("FontAsset.CreateFontAsset devolvió null para " + sourcePath);
            }

            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            var atlases = asset.atlasTextures;
            for (var i = 0; i < atlases.Length; i++)
            {
                if (atlases[i] != null)
                {
                    atlases[i].name = asset.name + " Atlas" + (i > 0 ? " " + i : "");
                    AssetDatabase.AddObjectToAsset(atlases[i], asset);
                }
            }

            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            var serialized = new SerializedObject(asset);
            var clear = serialized.FindProperty("m_ClearDynamicDataOnBuild");
            if (clear != null)
            {
                clear.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            return true;
        }

        // DN3-24: getFontFeatures = false (kerning solo de la tabla versionada). true si lo cambió (escribe solo si hace falta).
        private static bool FreezeFontFeatures(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);
            if (asset == null)
            {
                return false;
            }

            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("m_GetFontFeatures");
            if (property == null || !property.boolValue)
            {
                return false;
            }

            property.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return true;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
