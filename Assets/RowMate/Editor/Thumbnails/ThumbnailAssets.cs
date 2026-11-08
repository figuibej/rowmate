using RowMate.Bootstrap;
using RowMate.Core.Scene;
using UnityEditor;
using UnityEngine;

namespace RowMate.EditorTools
{
    // Las miniaturas horneadas del selector (08-entornos-selector/02-tech-design §3.4; AC-SCN-22-04, -05): la importación (sin mipmaps,
    // Clamp, Bilinear, sRGB, sin compresión con pérdida visible: BC7 en escritorio y ASTC 4×4 en Android) y su asignación a UiAssets en el
    // orden de EnvironmentId. Las hornea el arnés (EnvironmentSnapshotCatalog, conjunto "thumbs"; tools/unity/bake-thumbnails.ps1).
    public static class ThumbnailAssets
    {
        public static string PathFor(EnvironmentId id)
        {
            return UiAssets.ThumbnailsFolder + "/" + EnvironmentKeys.ToKey(id) + ".png";
        }

        // -executeMethod RowMate.EditorTools.ThumbnailAssets.AssignFromCommandLine (tools/unity/bake-thumbnails.ps1, después del horneado).
        [MenuItem("RowMate/Escena/Asignar miniaturas")]
        public static void AssignFromCommandLine()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var assets = AssetDatabase.LoadAssetAtPath<UiAssets>(UiAssets.AssetPath);
            if (assets == null)
            {
                Debug.LogError("[THUMBS] falta " + UiAssets.AssetPath + ": correr RowMate/Bootstrap/Reaplicar");
                return;
            }

            if (Assign(assets))
            {
                EditorUtility.SetDirty(assets);
                AssetDatabase.SaveAssets();
            }

            var count = 0;
            foreach (var t in assets.Thumbnails)
            {
                count += t != null ? 1 : 0;
            }

            Debug.Log("[THUMBS] " + count + " de 4 miniaturas asignadas a UiAssets");
        }

        // true si UiAssets cambió. Una que falta queda en null (la tarjeta usa el degradé y avisa una vez).
        public static bool Assign(UiAssets assets)
        {
            var changed = false;
            var thumbnails = new Texture2D?[4];
            foreach (var id in EnvironmentCatalog.All)
            {
                var path = PathFor(id);
                Configure(path);
                thumbnails[(int)id] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            var current = assets.Thumbnails;
            for (var i = 0; i < thumbnails.Length; i++)
            {
                if (i >= current.Length || current[i] != thumbnails[i])
                {
                    changed = true;
                }
            }

            if (changed)
            {
                assets.SetThumbnails(thumbnails);
            }

            return changed;
        }

        public static void Configure(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var dirty = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                dirty = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                // 456 × 288 tal cual (no a 512 × 256).
                importer.npotScale = TextureImporterNPOTScale.None;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                dirty = true;
            }

            if (!importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
                dirty = true;
            }

            if (importer.alphaSource != TextureImporterAlphaSource.None)
            {
                importer.alphaSource = TextureImporterAlphaSource.None;
                dirty = true;
            }

            var standalone = importer.GetPlatformTextureSettings("Standalone");
            if (!standalone.overridden || standalone.format != TextureImporterFormat.BC7)
            {
                standalone.overridden = true;
                standalone.format = TextureImporterFormat.BC7;
                importer.SetPlatformTextureSettings(standalone);
                dirty = true;
            }

            var android = importer.GetPlatformTextureSettings("Android");
            if (!android.overridden || android.format != TextureImporterFormat.ASTC_4x4)
            {
                android.overridden = true;
                android.format = TextureImporterFormat.ASTC_4x4;
                importer.SetPlatformTextureSettings(android);
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
