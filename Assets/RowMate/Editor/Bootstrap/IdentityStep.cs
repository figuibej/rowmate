using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RowMate.EditorTools
{
    // Paso identity del bootstrapper v7 (It. 14, 14-pulido-plataformas/02-tech-design DT14-07, §2.4; AC-APP-16-06 a -11, AC-POL-07).
    // Tres subpasos idempotentes (cada uno asigna valores fijos):
    // - identity.import: los TextureImporter de Art/Identity (Default, sRGB, alphaIsTransparency, sin mipmaps, sin compresión, npot
    //   None, máximo 2048). Solo reimporta lo que cambia.
    // - identity.standalone: PlayerSettings.SetIcons(Standalone) con un PNG de tools/icon por cada tamaño de GetIconSizes (Windows; el
    //   de macOS lo cambia BuildCli en memoria, DT14-08).
    // - identity.mobile: Android (íconos adaptativo, redondo y legacy y la imagen de arranque) por SerializedObject, porque falta el
    //   módulo de Android (R14-05: los m_Kind quedan [Device-Android]); iOS con el storyboard propio (API de core para el tipo,
    //   SerializedObject para la ruta) y multitarea en iPad (requiresFullScreen = false).
    // Los PNG los escribe tools/icon (WP-A); este paso no dibuja nada. Ninguna otra clave de ProjectSettings cambia (§2.4).
    internal static class IdentityStep
    {
        public const string Folder = "Assets/RowMate/Art/Identity";
        public const string StandaloneFolder = Folder + "/Standalone";
        public const string AndroidFolder = Folder + "/Android";
        public const string BootMarkPath = Folder + "/boot-mark.png";
        public const string SplashMarkPath = AndroidFolder + "/splash-mark.png";
        public const string StoryboardPath = Folder + "/Apple/LaunchScreen.storyboard";
        public const int MaxTextureSize = 2048;

        // m_Kind de m_BuildTargetPlatformIcons para Android (R14-05, sin verificar contra AndroidPlatformIconKind: falta el módulo).
        public const int AndroidKindLegacy = 0;
        public const int AndroidKindRound = 1;
        public const int AndroidKindAdaptive = 2;

        public static readonly int[] AndroidAdaptiveSizes = { 432, 324, 216, 162, 108, 81 };
        public static readonly int[] AndroidLegacySizes = { 192, 144, 96, 72, 48, 36 };

        public static BootstrapState.Step ApplyImport(string id)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                throw new DirectoryNotFoundException("no está " + Folder + " (correr tools/icon)");
            }

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            var changed = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (ConfigureImporter(path))
                {
                    changed++;
                }
            }

            return BootstrapState.Step.Ok(id, guids.Length.ToString(CultureInfo.InvariantCulture) + " PNG de Art/Identity; reimportados "
                + changed.ToString(CultureInfo.InvariantCulture));
        }

        // true si cambió algo (y se reimportó).
        public static bool ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("no es una textura: " + path);
            }

            var dirty = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                dirty = true;
            }

            if (importer.textureShape != TextureImporterShape.Texture2D)
            {
                importer.textureShape = TextureImporterShape.Texture2D;
                dirty = true;
            }

            if (!importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
                dirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                dirty = true;
            }

            if (importer.maxTextureSize != MaxTextureSize)
            {
                importer.maxTextureSize = MaxTextureSize;
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

            if (dirty)
            {
                importer.SaveAndReimport();
            }

            return dirty;
        }

        // Windows (y macOS fuera de su build): un ícono por cada tamaño que pide Unity para Standalone.
        public static BootstrapState.Step ApplyStandalone(string id)
        {
            var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
            var textures = StandaloneIcons(sizes, StandaloneFolder);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, textures, IconKind.Application);
            return BootstrapState.Step.Ok(id, "Standalone: " + Join(sizes));
        }

        // Los íconos de Standalone de una carpeta con icon-{n}.png (Standalone o Apple/macOS), en el orden de sizes.
        public static Texture2D[] StandaloneIcons(int[] sizes, string folder)
        {
            var textures = new Texture2D[sizes.Length];
            for (var i = 0; i < sizes.Length; i++)
            {
                textures[i] = Load(folder + "/icon-" + sizes[i].ToString(CultureInfo.InvariantCulture) + ".png");
            }

            return textures;
        }

        public static BootstrapState.Step ApplyMobile(string id)
        {
            var settings = ProjectSettingsObject();
            var serialized = new SerializedObject(settings);
            WriteAndroidIcons(serialized);
            var splash = serialized.FindProperty("androidSplashScreen") ?? throw new InvalidOperationException("ProjectSettings sin androidSplashScreen");
            splash.objectReferenceValue = Load(SplashMarkPath);
            var scale = serialized.FindProperty("AndroidSplashScreenScale");
            if (scale != null)
            {
                scale.intValue = (int)AndroidSplashScreenScale.Center;
            }

            Require(serialized, "iOSLaunchScreenCustomStoryboardPath").stringValue = StoryboardPath;
            Require(serialized, "iOSLaunchScreeniPadCustomStoryboardPath").stringValue = StoryboardPath;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (AssetDatabase.LoadMainAssetAtPath(StoryboardPath) == null && !File.Exists(StoryboardPath))
            {
                throw new FileNotFoundException("no está " + StoryboardPath);
            }

            PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.CustomStoryboard);
            PlayerSettings.iOS.SetiPadLaunchScreenType(iOSLaunchScreenType.CustomStoryboard);
            PlayerSettings.iOS.requiresFullScreen = false;
            return BootstrapState.Step.Ok(id, "Android: adaptativo " + Join(AndroidAdaptiveSizes) + ", redondo y legacy " + Join(AndroidLegacySizes)
                + ", arranque splash-mark (centrar); iOS: LaunchScreen.storyboard, multitarea en iPad");
        }

        // m_BuildTargetPlatformIcons: una sola entrada de Android con Adaptive (fondo y frente), Round y Legacy. Se reescribe entera
        // (idempotente); las demás plataformas que hubiera quedan como estaban.
        private static void WriteAndroidIcons(SerializedObject serialized)
        {
            var platforms = Require(serialized, "m_BuildTargetPlatformIcons");
            SerializedProperty? android = null;
            for (var i = 0; i < platforms.arraySize; i++)
            {
                var entry = platforms.GetArrayElementAtIndex(i);
                if (Require(entry, "m_BuildTarget").stringValue == "Android")
                {
                    android = entry;
                    break;
                }
            }

            if (android == null)
            {
                platforms.InsertArrayElementAtIndex(platforms.arraySize);
                android = platforms.GetArrayElementAtIndex(platforms.arraySize - 1);
                Require(android, "m_BuildTarget").stringValue = "Android";
            }

            var icons = Require(android, "m_Icons");
            icons.ClearArray();
            for (var i = 0; i < AndroidAdaptiveSizes.Length; i++)
            {
                var size = AndroidAdaptiveSizes[i].ToString(CultureInfo.InvariantCulture);
                AddIcon(icons, AndroidAdaptiveSizes[i], AndroidKindAdaptive,
                    Load(AndroidFolder + "/adaptive-bg-" + size + ".png"), Load(AndroidFolder + "/adaptive-fg-" + size + ".png"));
            }

            for (var i = 0; i < AndroidLegacySizes.Length; i++)
            {
                AddIcon(icons, AndroidLegacySizes[i], AndroidKindRound, Load(AndroidFolder + "/round-" + AndroidLegacySizes[i].ToString(CultureInfo.InvariantCulture) + ".png"));
            }

            for (var i = 0; i < AndroidLegacySizes.Length; i++)
            {
                AddIcon(icons, AndroidLegacySizes[i], AndroidKindLegacy, Load(AndroidFolder + "/legacy-" + AndroidLegacySizes[i].ToString(CultureInfo.InvariantCulture) + ".png"));
            }
        }

        private static void AddIcon(SerializedProperty icons, int size, int kind, params Texture2D[] textures)
        {
            icons.InsertArrayElementAtIndex(icons.arraySize);
            var icon = icons.GetArrayElementAtIndex(icons.arraySize - 1);
            var list = Require(icon, "m_Textures");
            list.ClearArray();
            for (var i = 0; i < textures.Length; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = textures[i];
            }

            Require(icon, "m_Width").intValue = size;
            Require(icon, "m_Height").intValue = size;
            Require(icon, "m_Kind").intValue = kind;
            var subKind = icon.FindPropertyRelative("m_SubKind");
            if (subKind != null)
            {
                subKind.stringValue = "";
            }
        }

        public static UnityEngine.Object ProjectSettingsObject()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                throw new InvalidOperationException("no se pudo abrir ProjectSettings/ProjectSettings.asset");
            }

            return assets[0];
        }

        private static SerializedProperty Require(SerializedObject serialized, string name)
        {
            return serialized.FindProperty(name) ?? throw new InvalidOperationException("ProjectSettings sin " + name);
        }

        private static SerializedProperty Require(SerializedProperty property, string name)
        {
            return property.FindPropertyRelative(name) ?? throw new InvalidOperationException("ProjectSettings: " + property.propertyPath + " sin " + name);
        }

        public static Texture2D Load(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new FileNotFoundException("no está " + path + " (correr tools/icon)");
            }

            return texture;
        }

        private static string Join(IReadOnlyList<int> sizes)
        {
            var parts = new string[sizes.Count];
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = sizes[i].ToString(CultureInfo.InvariantCulture);
            }

            return string.Join(", ", parts);
        }
    }
}
