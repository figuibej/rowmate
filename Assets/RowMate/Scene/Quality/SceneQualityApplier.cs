using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RowMate.Scene.Quality
{
    // Aplica un nivel de calidad a lo que es de la escena (06a-escena-lago/02-tech-design §4.2; ADR 018; QA-G6aT-05): la cámara (HDR,
    // MSAA, FXAA en Bajo, sombras), la RT (la resolución: DT6a-16), el Volume (bloom), los materiales (COPIAS en memoria: el fondo tenue
    // de estrellas y la capa lejana de nubes solo en Alto, el fin del pasto) y los streamers (LOD, bosque cercano, densidad del pasto).
    // Lo que solo vive en el asset de URP (la distancia de sombras, las cascadas, el mapa, el MSAA del asset) lo trae el nivel de
    // QualitySettings; en el player lo pone IRenderAssetQuality (PlayerUrpAssetQuality) y en el Editor nunca se toca (las pruebas que
    // necesitan otro nivel usan TierScope). Las palabras clave quedan constantes durante la sesión (QA-G6aT-07).
    public static class SceneQualityApplier
    {
        public static void Apply(QualityTierProfile profile, Camera camera, UniversalAdditionalCameraData data, SceneLighting lighting, Material sky,
                                 Material clouds, Material grass, SkyRenderer skyRenderer, ShoreStreamer shore, FillStreamer fill, SegmentStore store)
        {
            camera.allowHDR = profile.Hdr;
            camera.allowMSAA = profile.Msaa > 1;
            data.antialiasing = profile.Fxaa ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
            data.renderShadows = profile.Shadows;
            data.renderPostProcessing = true;
            data.dithering = false;

            lighting.BloomEnabled = profile.Bloom;
            lighting.DiskHdr = profile.Hdr;
            lighting.LowMistEnabled = profile.LowMist;
            lighting.NightShadowStrength = profile.NightShadowStrength;
            lighting.HighTier = profile.Tier == QualityTier.High;

            SetKeyword(sky, SceneShaderIds.SkyStarFieldKeyword, profile.StarField);
            SetKeyword(clouds, SceneShaderIds.CloudLayerKeyword, profile.CloudLayer);
            skyRenderer.CloudLayer = profile.CloudLayer;
            grass.SetVector(SceneShaderIds.GrassEnd, new Vector4((float)profile.GrassEnd, (float)MaterialArt.GrassFadeFraction, 0, 0));

            shore.LodScale = profile.LodScale;
            var shadowLods = !profile.Shadows ? 0 : profile.Tier == QualityTier.High ? 2 : 1;
            shore.ShadowLods = shadowLods;
            fill.ShadowLods = shadowLods;
            fill.Tier = profile.Tier;
            fill.NearForest = profile.NearForest;
            fill.GrassEnd = profile.GrassEnd;
            fill.GrassDensity = profile.FillDensity;
            store.NearForest = profile.NearForest;
            store.GrassEnd = profile.GrassEnd;
        }

        private static void SetKeyword(Material material, string keyword, bool on)
        {
            if (on)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}
