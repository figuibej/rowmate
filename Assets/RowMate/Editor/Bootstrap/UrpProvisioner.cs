using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RowMate.EditorTools
{
    // URP provisorio de la It. 1 (02-tech-design §1.5, ARCHITECTURE §16 punto 2). Verificado en el primer open (It. 2, R-T2).
    // Crea, solo si no existen, Assets/RowMate/Settings/URP/RowMate_Renderer.asset (Universal Renderer) y UN SOLO
    // RowMate_URP_Default.asset, y lo asigna en Graphics y en los niveles de calidad EXISTENTES.
    // No crea, borra ni renombra niveles de calidad (QA-P-10): los definitivos llegan en la It. 6a (ADR 018).
    // El renderer lleva el PostProcessData por defecto de URP, igual que el menú Create > Rendering > URP Asset (with
    // Universal Renderer) (UniversalRenderPipelineAsset.CreateRendererData, URP 17.3.0). Sin él URP apaga el postprocesado
    // de ese renderer (bloom, tonemapping ACES y ajustes de color de ART_BIBLE). Faltaba en la versión 1 (primer open, It. 2).
    internal static class UrpProvisioner
    {
        public const string Folder = "Assets/RowMate/Settings/URP";
        public const string RendererPath = Folder + "/RowMate_Renderer.asset";
        public const string PipelinePath = Folder + "/RowMate_URP_Default.asset";
        public const string DefaultPostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";

        public static BootstrapState.Step Apply(string id)
        {
            EnsureFolder("Assets/RowMate/Settings");
            EnsureFolder(Folder);

            var created = "";
            var completed = "";
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
                // Renderer creado por la versión 1: se completa sin recrearlo (conserva su GUID y lo que tenga).
                renderer.postProcessData = LoadDefaultPostProcessData();
                EditorUtility.SetDirty(renderer);
                completed = "PostProcessData agregado al renderer existente; ";
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                created += " pipeline";
            }

            // Reasignar siempre (idempotente).
            GraphicsSettings.defaultRenderPipeline = pipeline;
            var original = QualitySettings.GetQualityLevel();
            var levels = QualitySettings.names.Length;
            for (var i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(original, false);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();

            return BootstrapState.Step.Ok(id, (created.Length > 0 ? "creado:" + created + "; " : "") + completed
                + "renderer con el PostProcessData de URP; asignado en Graphics y en " + levels + " nivel(es) de calidad existente(s)");
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
