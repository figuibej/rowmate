using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Una textura (sky.clouds, sky.moon, sky.stars, water.normalDetail, water.foam). Sin asignar: la procedural (SkyTextures de Core,
    // horneada al precargar por ProceduralTextures). El presupuesto es el lado máximo en px. D-ART-08 (P-01): sin texturas CC0 ni la
    // luna de la NASA; sky.moon es procedural.
    [CreateAssetMenu(menuName = "RowMate/Slots/Texture", fileName = "texture")]
    public sealed class TextureSlot : AssetSlot<Texture2D>
    {
        protected override bool ValidateAsset(Texture2D asset, out string reason)
        {
            if (Budget > 0 && (asset.width > Budget || asset.height > Budget))
            {
                reason = "la textura mide " + asset.width + " × " + asset.height + " px (tope " + Budget + ")";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
