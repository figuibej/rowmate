using System;
using System.Collections.Generic;
using RowMate.Core.Scene;
using RowMate.Scene;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RowMate.EditorTools
{
    // URP del proyecto. It. 1: un asset provisorio (RowMate_URP_Default) en los niveles existentes (QA-P-10). It. 6a (ADR 018;
    // 06a-escena-lago/02-tech-design §1.1, §4.2, §7.2 B0; QA-G6aT-05): los TRES niveles definitivos, los únicos del proyecto, cada uno
    // con su asset RowMate_URP_{High,Medium,Low} (el renderer RowMate_Renderer, con el PostProcessData por defecto de URP, es de los
    // tres; renderScale 1 en los tres: la resolución del nivel es el tamaño de la RT de la escena, DT6a-16). Windows en Alto, Android e
    // iOS en Medio. Además la capa RowMateScene (la cámara de la UI no la ve) y Frame Timing Stats (FrameTimingManager, ThermalAdapter).
    // Es el ÚNICO archivo que asigna un asset de URP a la configuración del proyecto (PureCodeRulesTests). Idempotente: crea lo que
    // falta y vuelve a poner los valores; el asset provisorio de la It. 1 ya no se usa y se borra.
    internal static class UrpProvisioner
    {
        public const string Folder = "Assets/RowMate/Settings/URP";
        public const string RendererPath = Folder + "/RowMate_Renderer.asset";
        public const string LegacyPipelinePath = Folder + "/RowMate_URP_Default.asset";
        public const string DefaultPostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
        public const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";
        public const string TagManagerPath = "ProjectSettings/TagManager.asset";

        // Los niveles en el orden de QualitySettings (0 = el más bajo, como en Unity).
        public static readonly QualityTier[] LevelOrder = { QualityTier.Low, QualityTier.Medium, QualityTier.High };
        public static readonly string[] LevelNames = { "Low", "Medium", "High" };

        public static string PipelinePath(QualityTier tier)
        {
            return Folder + "/RowMate_URP_" + tier + ".asset";
        }

        public static int LevelIndex(QualityTier tier)
        {
            return Array.IndexOf(LevelOrder, tier);
        }

        // Paso urp.tiers (bootstrapper v4).
        public static BootstrapState.Step Apply(string id)
        {
            EnsureFolder("Assets/RowMate/Settings");
            EnsureFolder(Folder);
            var created = "";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.postProcessData = LoadDefaultPostProcessData();
                AssetDatabase.CreateAsset(renderer, RendererPath);
                created += " renderer";
            }
            else if (renderer.postProcessData == null)
            {
                renderer.postProcessData = LoadDefaultPostProcessData();
                EditorUtility.SetDirty(renderer);
            }

            var assets = new UniversalRenderPipelineAsset[LevelOrder.Length];
            for (var i = 0; i < LevelOrder.Length; i++)
            {
                var tier = LevelOrder[i];
                var path = PipelinePath(tier);
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(pipeline, path);
                    created += " " + tier;
                }

                ConfigurePipeline(pipeline, tier);
                assets[i] = pipeline;
            }

            var levels = EnsureQualityLevels();

            // Los assets de cada nivel (el ÚNICO lugar que asigna la configuración de URP del proyecto).
            for (var i = 0; i < LevelOrder.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = assets[i];
            }

            var high = assets[LevelIndex(QualityTier.High)];
            GraphicsSettings.defaultRenderPipeline = high;
            // Lo que el constructor de UniversalRenderPipeline fija en cada arranque (intensidad lineal y temperatura de color de las
            // luces): escrito acá, la primera corrida que crea el pipeline no deja GraphicsSettings.asset cambiado (la guardia de
            // ProjectSettings, QA-G6aT-05).
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;
            QualitySettings.SetQualityLevel(LevelIndex(QualityTier.High), false);

            var layer = EnsureLayer(SceneShaderIds.LayerName);
            PlayerSettings.enableFrameTimingStats = true;

            // El asset provisorio de la It. 1 ya no lo usa nadie.
            var removed = "";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(LegacyPipelinePath) != null && AssetDatabase.DeleteAsset(LegacyPipelinePath))
            {
                removed = "; borrado el provisorio de la It. 1";
            }

            AssetDatabase.SaveAssets();
            return BootstrapState.Step.Ok(id, (created.Length > 0 ? "creado:" + created + "; " : "") + levels + "; capa " + SceneShaderIds.LayerName + " = "
                + layer + "; Frame Timing Stats; Graphics = High" + removed);
        }

        // Los valores de cada nivel que viven en el asset (04-design-notes §15.2; §9.3): MSAA 4 / 2 / 1, HDR (sí / sí / no), sombras de
        // la luz principal (2 cascadas de 2 048 a 80 m / 1 de 1 024 a 40 m / sin mapa), suaves en Alto y Medio. Sin luces adicionales,
        // sin texturas de profundidad ni de opacos, SRP Batcher, renderScale 1.
        public static void ConfigurePipeline(UniversalRenderPipelineAsset pipeline, QualityTier tier)
        {
            var so = new SerializedObject(pipeline);
            var high = tier == QualityTier.High;
            var low = tier == QualityTier.Low;
            Set(so, "m_MSAA", high ? 4 : tier == QualityTier.Medium ? 2 : 1);
            Set(so, "m_SupportsHDR", !low);
            Set(so, "m_RenderScale", 1f);
            Set(so, "m_RequireDepthTexture", false);
            Set(so, "m_RequireOpaqueTexture", false);
            Set(so, "m_MainLightRenderingMode", 1);
            Set(so, "m_MainLightShadowsSupported", !low);
            Set(so, "m_MainLightShadowmapResolution", high ? 2048 : tier == QualityTier.Medium ? 1024 : 512);
            Set(so, "m_AdditionalLightsRenderingMode", 0);
            Set(so, "m_AdditionalLightShadowsSupported", false);
            Set(so, "m_ShadowDistance", high ? 80f : tier == QualityTier.Medium ? 40f : 0f);
            Set(so, "m_ShadowCascadeCount", high ? 2 : 1);
            Set(so, "m_Cascade2Split", 0.25f);
            Set(so, "m_SoftShadowsSupported", !low);
            Set(so, "m_UseSRPBatcher", true);
            Set(so, "m_SupportsTerrainHoles", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
        }

        // Exactamente tres niveles, Low, Medium y High (0 a 2). Desde los seis por defecto de Unity se conservan Medium, High y Ultra (sus
        // ajustes generales: vSync, filtrado, mipmaps) renombrados a Low, Medium y High: Windows sigue con los de Ultra. Por plataforma:
        // Android e iOS en Medio; el resto en Alto. Devuelve el resumen.
        private static string EnsureQualityLevels()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath(QualitySettingsPath);
            if (settings == null || settings.Length == 0)
            {
                throw new InvalidOperationException("no se pudo cargar " + QualitySettingsPath);
            }

            var so = new SerializedObject(settings[0]);
            var levels = so.FindProperty("m_QualitySettings");
            var names = new List<string>();
            for (var i = 0; i < levels.arraySize; i++)
            {
                names.Add(levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
            }

            var before = string.Join(", ", names);
            var unityDefaults = names.Count == 6 && names[0] == "Very Low" && names[5] == "Ultra";
            if (unityDefaults)
            {
                // Se quedan Medium (2), High (3) y Ultra (5).
                levels.DeleteArrayElementAtIndex(4);
                levels.DeleteArrayElementAtIndex(1);
                levels.DeleteArrayElementAtIndex(0);
            }
            else
            {
                while (levels.arraySize > LevelNames.Length)
                {
                    levels.DeleteArrayElementAtIndex(0);
                }

                while (levels.arraySize < LevelNames.Length)
                {
                    levels.InsertArrayElementAtIndex(levels.arraySize - 1);
                }
            }

            for (var i = 0; i < LevelNames.Length; i++)
            {
                var level = levels.GetArrayElementAtIndex(i);
                level.FindPropertyRelative("name").stringValue = LevelNames[i];
                level.FindPropertyRelative("globalTextureMipmapLimit").intValue = 0;
                level.FindPropertyRelative("vSyncCount").intValue = 1;
                level.FindPropertyRelative("lodBias").floatValue = 1f;
            }

            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform != null && perPlatform.isArray)
            {
                for (var i = 0; i < perPlatform.arraySize; i++)
                {
                    var pair = perPlatform.GetArrayElementAtIndex(i);
                    var platform = pair.FindPropertyRelative("first").stringValue;
                    var mobile = platform == "Android" || platform == "iPhone" || platform == "tvOS";
                    pair.FindPropertyRelative("second").intValue = LevelIndex(mobile ? QualityTier.Medium : QualityTier.High);
                }
            }

            so.FindProperty("m_CurrentQuality").intValue = LevelIndex(QualityTier.High);
            so.ApplyModifiedPropertiesWithoutUndo();
            return "niveles " + string.Join(", ", LevelNames) + (before != string.Join(", ", LevelNames) ? " (antes: " + before + ")" : "");
        }

        // La capa de la escena en el primer lugar libre desde la 8 (o la que ya tiene ese nombre).
        private static int EnsureLayer(string name)
        {
            var manager = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            var so = new SerializedObject(manager[0]);
            var layers = so.FindProperty("layers");
            var free = -1;
            for (var i = 8; i < layers.arraySize; i++)
            {
                var value = layers.GetArrayElementAtIndex(i).stringValue;
                if (value == name)
                {
                    return i;
                }

                if (free < 0 && string.IsNullOrEmpty(value))
                {
                    free = i;
                }
            }

            if (free < 0)
            {
                throw new InvalidOperationException("no hay una capa libre para " + name);
            }

            layers.GetArrayElementAtIndex(free).stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
            return free;
        }

        private static void Set(SerializedObject so, string name, int value)
        {
            var property = so.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException("el asset de URP no tiene " + name);
            }

            // intValue escribe el valor del enum (no su índice): MsaaQuality, ShadowResolution y LightRenderingMode tienen valores propios.
            property.intValue = value;
        }

        private static void Set(SerializedObject so, string name, bool value)
        {
            var property = so.FindProperty(name) ?? throw new InvalidOperationException("el asset de URP no tiene " + name);
            property.boolValue = value;
        }

        private static void Set(SerializedObject so, string name, float value)
        {
            var property = so.FindProperty(name) ?? throw new InvalidOperationException("el asset de URP no tiene " + name);
            property.floatValue = value;
        }

        // Paso urp.tidy (v3, 00-primer-open §8.3, D-18): los dos assets que URP crea en la raíz de Assets/ pasan a
        // Settings/URP/ con MoveAsset (conserva el GUID: Graphics los sigue encontrando). Si ya están, no hace nada.
        public static readonly string[] RootAssets = { "UniversalRenderPipelineGlobalSettings.asset", "DefaultVolumeProfile.asset" };

        public static BootstrapState.Step Tidy(string id)
        {
            EnsureFolder("Assets/RowMate/Settings");
            EnsureFolder(Folder);
            var moved = "";
            foreach (var name in RootAssets)
            {
                var from = "Assets/" + name;
                var to = Folder + "/" + name;
                if (!System.IO.File.Exists(from))
                {
                    continue;
                }

                if (System.IO.File.Exists(to))
                {
                    throw new InvalidOperationException("están los dos: " + from + " y " + to);
                }

                var error = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException("MoveAsset " + from + ": " + error);
                }

                moved += " " + name;
            }

            return moved.Length == 0
                ? BootstrapState.Step.Skipped(id, "los assets de URP ya están en " + Folder)
                : BootstrapState.Step.Ok(id, "movidos a " + Folder + ":" + moved);
        }

        private static PostProcessData LoadDefaultPostProcessData()
        {
            var data = AssetDatabase.LoadAssetAtPath<PostProcessData>(DefaultPostProcessDataPath);
            if (data == null)
            {
                throw new InvalidOperationException("no se encontró " + DefaultPostProcessDataPath);
            }

            return data;
        }

        private static void EnsureFolder(string path)
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
