using System;
using RowMate.Core.Scene;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RowMate.Platform.Display
{
    // El nivel de calidad inicial (06a-escena-lago/02-tech-design §4.2; ADR 018): Alto en escritorio con GPU dedicada, Medio en escritorio
    // con una GPU integrada conocida (ronda 1 de la verificación, QA-V6a-REN-09: una laptop o tableta con gráficos integrados arrancaba en
    // Alto, con MSAA 4×, HDR y dos cascadas de 2 048, y la adaptación tardaba ≥ 40 s en llegar al nivel) y en la gama media de teléfono,
    // Bajo con Mali-G52 o menos de 4 GB. Las listas de GPU son las de ADR 018; un teléfono que no está en ninguna va en Medio y un
    // escritorio que no está en la de integradas, en Alto (la adaptación térmica baja lo que haga falta, hasta el nivel).
    public static class GraphicsTier
    {
        private static readonly string[] LowGpus = { "Mali-G52", "Mali-G51", "Mali-G31", "Mali-T", "Adreno (TM) 5", "Adreno (TM) 4", "PowerVR" };

        // Los gráficos integrados de escritorio por su nombre (SystemInfo.graphicsDeviceName): los de Intel sin "Arc" con número de
        // modelo, las APU de AMD ("Radeon(TM) Graphics" sin modelo, Vega) y el adaptador de software de Windows.
        private static readonly string[] IntegratedDesktopGpus =
        {
            "Intel(R) UHD Graphics", "Intel(R) HD Graphics", "Intel(R) Iris", "Intel(R) Graphics", "AMD Radeon(TM) Graphics", "Radeon(TM) Vega",
            "AMD Radeon(TM) Vega", "Microsoft Basic Render",
        };

        public static bool IsIntegratedDesktopGpu(string? gpuName)
        {
            var gpu = gpuName ?? "";
            foreach (var integrated in IntegratedDesktopGpus)
            {
                if (gpu.IndexOf(integrated, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static QualityTier Choose(bool mobile, string gpuName, int systemMemoryMb)
        {
            if (!mobile)
            {
                return IsIntegratedDesktopGpu(gpuName) ? QualityTier.Medium : QualityTier.High;
            }

            if (systemMemoryMb > 0 && systemMemoryMb < 3800)
            {
                return QualityTier.Low;
            }

            var gpu = gpuName ?? "";
            foreach (var low in LowGpus)
            {
                if (gpu.IndexOf(low, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return QualityTier.Low;
                }
            }

            return QualityTier.Medium;
        }

        public static QualityTier ChooseForThisDevice()
        {
            return Choose(Application.isMobilePlatform, SystemInfo.graphicsDeviceName, SystemInfo.systemMemorySize);
        }

        // -rowmate-quality high|medium|low (solo desarrollo; lo lee AppRoot).
        public static bool TryParse(string? value, out QualityTier tier)
        {
            switch ((value ?? "").Trim().ToLowerInvariant())
            {
                case "high":
                case "alto":
                    tier = QualityTier.High;
                    return true;
                case "medium":
                case "medio":
                    tier = QualityTier.Medium;
                    return true;
                case "low":
                case "bajo":
                    tier = QualityTier.Low;
                    return true;
                default:
                    tier = QualityTier.High;
                    return false;
            }
        }
    }

    // Lo que solo vive en el asset de URP (06a-escena-lago/02-tech-design §4.2; QA-G6aT-05), en el PLAYER: el nivel de QualitySettings
    // (SetQualityLevel, estado de ejecución: no se guarda) y la distancia de sombras de la adaptación en el asset activo, que en el
    // player vive solo en memoria. En el Editor nunca se arma (el Editor no cambia ni asigna assets de URP): ahí va EditorRenderAssetQuality.
    public sealed class PlayerUrpAssetQuality : IRenderAssetQuality
    {
        public QualityTier Tier { get; private set; } = QualityTier.High;

        public void ApplyTier(QualityTier tier)
        {
            Tier = tier;
            var index = LevelIndex(tier);
            if (index >= 0 && index < QualitySettings.names.Length)
            {
                QualitySettings.SetQualityLevel(index, true);
            }

        }

        public void SetShadowDistance(double meters)
        {
            if (Application.isEditor)
            {
                return;
            }

            // En el player un cambio al asset activo vive solo en memoria (no hay asset que guardar): sin copias ni asignaciones de la
            // configuración (la regla de código: solo UrpProvisioner asigna QualitySettings.renderPipeline).
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset active)
            {
                active.shadowDistance = (float)meters;
            }
        }

        // Low, Medium, High = 0, 1, 2 (UrpProvisioner).
        public static int LevelIndex(QualityTier tier)
        {
            return tier == QualityTier.Low ? 0 : tier == QualityTier.Medium ? 1 : 2;
        }
    }

    // El IRenderAssetQuality del Editor y de las pruebas: solo anota (el Editor nunca toca la configuración del proyecto; QA-G6aT-05).
    public sealed class EditorRenderAssetQuality : IRenderAssetQuality
    {
        public QualityTier Tier { get; private set; } = QualityTier.High;

        public int TierChanges { get; private set; }

        public double? ShadowDistance { get; private set; }

        public void ApplyTier(QualityTier tier)
        {
            Tier = tier;
            TierChanges++;
        }

        public void SetShadowDistance(double meters)
        {
            ShadowDistance = meters;
        }
    }
}
