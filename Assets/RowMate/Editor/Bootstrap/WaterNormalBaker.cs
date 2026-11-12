using System;
using System.IO;
using System.Security.Cryptography;
using RowMate.Core.Scene.Geometry;
using UnityEditor;
using UnityEngine;

namespace RowMate.EditorTools
{
    // El mapa de normales del agua avanzada (06b-agua-avanzada/02-tech-design §4.1; DT6b-17, DT6b-18; PA6b-04; IA6b-07): hornea los bytes
    // de WaterNormalMap (Core, en enteros: los mismos en .NET 8 y en Mono) a Art/Textures/Water/water-normal.png (512², RGBA lineal: RG el
    // chop, BA el rizo; BC7 en escritorio y ASTC 4 × 4 en Android, con mips) y su huella a water.json (semilla, lado y SHA-256 de los
    // bytes RGBA; ItSixBToolingTests.WaterNormalFootprint la recalcula en dotnet). El slot water.normalDetail queda en Procedural
    // (AC-6B-37): este PNG ES el procedural, y lo lleva el material WaterAdvanced (SceneMaterialsBuilder). Idempotente.
    internal static class WaterNormalBaker
    {
        public const string Folder = "Assets/RowMate/Art/Textures/Water";
        public const string PngPath = Folder + "/water-normal.png";
        public const string JsonPath = Folder + "/water.json";

        // -executeMethod RowMate.EditorTools.WaterNormalBaker.ApplyFromCommandLine -quit: hornea y rehace los materiales de la escena
        // (scene.materials) sin pasar por el bootstrapper (que reescribiría ProjectSettings/RowMateBootstrap.json). Sale con 1 si falla.
        public static void ApplyFromCommandLine()
        {
            try
            {
                Bake();
                var step = SceneMaterialsBuilder.Apply("scene.materials");
                Debug.Log("[WATER] " + step.id + ": " + step.status + " - " + step.detail);
                if (step.status != BootstrapState.StatusOk)
                {
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        public static Texture2D Bake()
        {
            FontAssetBuilder.EnsureFolder(Folder);
            var size = WaterNormalMap.DefaultSize;
            var seed = WaterNormalMap.DefaultSeed;
            var bytes = new byte[WaterNormalMap.ByteCount(size)];
            WaterNormalMap.Fill(size, seed, bytes);
            string sha;
            using (var hash = SHA256.Create())
            {
                sha = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            }

            var json = "{\n  \"seed\": " + seed.ToString(System.Globalization.CultureInfo.InvariantCulture)
                       + ",\n  \"size\": " + size.ToString(System.Globalization.CultureInfo.InvariantCulture)
                       + ",\n  \"sha256\": \"" + sha + "\"\n}\n";
            var jsonFull = Path.GetFullPath(JsonPath);
            var existing = File.Exists(jsonFull) ? File.ReadAllText(jsonFull) : "";
            var pngFull = Path.GetFullPath(PngPath);
            if (existing != json || !File.Exists(pngFull))
            {
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                texture.SetPixelData(bytes, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(pngFull, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllText(jsonFull, json);
                AssetDatabase.ImportAsset(PngPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(JsonPath, ImportAssetOptions.ForceUpdate);
            }

            Configure();
            Debug.Log("[WATER] mapa de normales " + size + "² (semilla " + seed + ", sha256 " + sha.Substring(0, 12) + "…) en " + PngPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(PngPath);
        }

        // Lineal (no es un color), con mips, que se repite; BC7 en escritorio y ASTC 4 × 4 en Android (≈ 0,33 MB con mips, DT6b-17).
        private static void Configure()
        {
            if (!(AssetImporter.GetAtPath(PngPath) is TextureImporter importer))
            {
                return;
            }

            var changed = false;
            changed |= Set(importer.textureType != TextureImporterType.Default, () => importer.textureType = TextureImporterType.Default);
            changed |= Set(importer.sRGBTexture, () => importer.sRGBTexture = false);
            changed |= Set(!importer.mipmapEnabled, () => importer.mipmapEnabled = true);
            changed |= Set(importer.wrapMode != TextureWrapMode.Repeat, () => importer.wrapMode = TextureWrapMode.Repeat);
            changed |= Set(importer.alphaIsTransparency, () => importer.alphaIsTransparency = false);
            changed |= Set(importer.maxTextureSize != WaterNormalMap.DefaultSize, () => importer.maxTextureSize = WaterNormalMap.DefaultSize);
            changed |= Platform(importer, "Standalone", TextureImporterFormat.BC7);
            changed |= Platform(importer, "Android", TextureImporterFormat.ASTC_4x4);
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool Platform(TextureImporter importer, string platform, TextureImporterFormat format)
        {
            var settings = importer.GetPlatformTextureSettings(platform);
            if (settings.overridden && settings.format == format && settings.maxTextureSize == WaterNormalMap.DefaultSize)
            {
                return false;
            }

            settings.overridden = true;
            settings.format = format;
            settings.maxTextureSize = WaterNormalMap.DefaultSize;
            importer.SetPlatformTextureSettings(settings);
            return true;
        }

        private static bool Set(bool needed, Action apply)
        {
            if (needed)
            {
                apply();
            }

            return needed;
        }
    }
}
