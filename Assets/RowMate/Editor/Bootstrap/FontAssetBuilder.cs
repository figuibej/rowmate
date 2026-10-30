using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
    //   Inter 400/500/600/700: 48 pt, padding 5 (10 %), atlas 1024²  ·  RowMate Numeric SC 600/700/800: 90 / 9, 1024²
    //   íconos de contorno: 64 / 6, 512²  ·  íconos rellenos: 64 / 6, 256²
    // Atlas dinámico (Dynamic) con clearDynamicDataOnBuild, para que el uso en el Editor no ensucie los .asset (R2-03). Desde
    // QA-V2-07 solo los escribe quien abre GeneratedAssetGuard.AllowWrites (este paso, dentro del bootstrapper): un FontAsset
    // recién creado ya está en el estado que se versiona (sin glifos, atlas de 1 × 1).
    // DN3-24 (R3-06, 03-rutinas/04-design-notes §4.16): las 7 caras de texto quedan con getFontFeatures = false (los que ya
    // existen y los que cree, solo si hace falta): el kerning es el de la tabla versionada y nada más, así un texto se ve
    // igual en cualquier orden de dibujo (K1).
    // WP-B0 de la It. 4a (02-tech-design §6.14; 00-deuda-diseno §3.4; hecho al empezar la 4b, QA-V4a-06):
    // - K2 (DD4a-06): la tabla de cada cara que dibuja texto (Inter ×4 y la derivada 700) es EXACTAMENTE su lista de
    //   UI/Fonts/Kerning (tools/fonts/kern_pairs.py), con XAdvance = unidades / unidades por em × tamaño de muestreo y el resto
    //   en 0. Se escribe con SerializedObject solo si difiere (idempotente) y falla si la lista quedó vieja (otro SHA-256 del .ttf).
    //   K4: la derivada 600 y 800 no llevan tabla.
    // - DD4a-07: el rol numérico es RowMate Numeric SC (UI/Fonts/RowMateNumericSC, la receta tools/fonts/freeze_tnum.py), y los
    //   FontAsset de Barlow que nada nombra (FontAssetPaths.RetiredFontAssets) se borran.
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
            var kerning = new StringBuilder();
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

                var pairs = ApplyKerning(path, FontAssetPaths.SourceFor(face.Family, face.Weight), FontAssetPaths.KerningFor(face.Family, face.Weight));
                kerning.Append(kerning.Length > 0 ? ", " : "").Append(Path.GetFileNameWithoutExtension(path)).Append(' ').Append(pairs);
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

            var retired = 0;
            foreach (var path in FontAssetPaths.RetiredFontAssets)
            {
                if (AssetDatabase.LoadAssetAtPath<FontAsset>(path) != null && AssetDatabase.DeleteAsset(path))
                {
                    retired++;
                }
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
                + features + "; kerning K2 (pares): " + kerning + "; FontAsset de Barlow borrados: " + retired + "; USS reimportados");
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
                throw new InvalidOperationException("FontAsset.CreateFontAsset devolvió null para " + sourcePath);
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

        // K2 (DD4a-06) y K4: deja m_GlyphPairAdjustmentRecords igual a la lista (o vacía si la cara no lleva). Devuelve cuántos pares
        // tiene la tabla; escribe solo si difería.
        private static int ApplyKerning(string assetPath, string sourcePath, string? listPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(assetPath);
            if (asset == null)
            {
                throw new FileNotFoundException("no está el FontAsset " + assetPath);
            }

            KerningList? list = null;
            if (listPath != null)
            {
                if (!File.Exists(listPath))
                {
                    throw new FileNotFoundException("no está la lista de kerning " + listPath + " (tools/fonts/kern_pairs.py)");
                }

                list = KerningList.Parse(File.ReadAllText(listPath));
                if (!string.Equals(list.Source, sourcePath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(listPath + " es de " + list.Source + ", no de " + sourcePath);
                }

                var sha = Sha256(sourcePath);
                if (!string.Equals(list.SourceSha256, sha, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(listPath + " quedó vieja: " + sourcePath + " cambió (SHA-256 " + sha
                        + "); volver a correr tools/fonts/kern_pairs.py");
                }
            }

            // El tamaño de muestreo del FontAsset (48 en Inter, 90 en la derivada): XAdvance va en sus puntos.
            var pointSize = (int)Math.Round((double)asset.faceInfo.pointSize);
            var serialized = new SerializedObject(asset);
            var records = serialized.FindProperty("m_FontFeatureTable.m_GlyphPairAdjustmentRecords");
            if (records == null || !records.isArray)
            {
                throw new InvalidOperationException(assetPath + ": no tiene m_FontFeatureTable.m_GlyphPairAdjustmentRecords");
            }

            var count = list?.Pairs.Count ?? 0;
            if (Matches(records, list, pointSize))
            {
                return count;
            }

            records.arraySize = count;
            for (var i = 0; i < count; i++)
            {
                var pair = list!.Pairs[i];
                var record = records.GetArrayElementAtIndex(i);
                SetAdjustment(record.FindPropertyRelative("m_FirstAdjustmentRecord"), pair.First, list.XAdvance(pair, pointSize));
                SetAdjustment(record.FindPropertyRelative("m_SecondAdjustmentRecord"), pair.Second, 0f);
                record.FindPropertyRelative("m_FeatureLookupFlags").intValue = 0;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return count;
        }

        private static bool Matches(SerializedProperty records, KerningList? list, int pointSize)
        {
            var count = list?.Pairs.Count ?? 0;
            if (records.arraySize != count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var pair = list!.Pairs[i];
                var record = records.GetArrayElementAtIndex(i);
                if (!IsAdjustment(record.FindPropertyRelative("m_FirstAdjustmentRecord"), pair.First, list.XAdvance(pair, pointSize))
                    || !IsAdjustment(record.FindPropertyRelative("m_SecondAdjustmentRecord"), pair.Second, 0f)
                    || record.FindPropertyRelative("m_FeatureLookupFlags").intValue != 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetAdjustment(SerializedProperty adjustment, uint glyph, float xAdvance)
        {
            adjustment.FindPropertyRelative("m_GlyphIndex").longValue = glyph;
            var value = adjustment.FindPropertyRelative("m_GlyphValueRecord");
            value.FindPropertyRelative("m_XPlacement").floatValue = 0f;
            value.FindPropertyRelative("m_YPlacement").floatValue = 0f;
            value.FindPropertyRelative("m_XAdvance").floatValue = xAdvance;
            value.FindPropertyRelative("m_YAdvance").floatValue = 0f;
        }

        private static bool IsAdjustment(SerializedProperty adjustment, uint glyph, float xAdvance)
        {
            var value = adjustment.FindPropertyRelative("m_GlyphValueRecord");
            // Comparación exacta: el valor sale siempre del mismo cálculo (KerningList.XAdvance).
            return adjustment.FindPropertyRelative("m_GlyphIndex").longValue == glyph
                   && value.FindPropertyRelative("m_XPlacement").floatValue == 0f
                   && value.FindPropertyRelative("m_YPlacement").floatValue == 0f
                   && value.FindPropertyRelative("m_XAdvance").floatValue == xAdvance
                   && value.FindPropertyRelative("m_YAdvance").floatValue == 0f;
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var hash = sha.ComputeHash(stream);
                var text = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    text.Append(b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                }

                return text.ToString();
            }
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
