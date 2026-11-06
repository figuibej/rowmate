using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Una capa del terreno (terrain.layer.*; DN6a-15): una TerrainLayer del pack, cuyo albedo y normal se copian al precargar a los
    // dos Texture2DArray del shader de terreno. Tienen que entrar en el arreglo: cuadradas, potencia de 2 y hasta el tope (Budget, px).
    // Una que no entra es un asset asignado que no valida: el procedural y un [SLOT].
    [CreateAssetMenu(menuName = "RowMate/Slots/Terrain layer", fileName = "terrain.layer")]
    public sealed class TerrainLayerSlot : AssetSlot<TerrainLayer>
    {
        protected override bool ValidateAsset(TerrainLayer asset, out string reason)
        {
            if (!ValidateTexture(asset.diffuseTexture, "el albedo", out reason))
            {
                return false;
            }

            if (asset.normalMapTexture != null && (asset.normalMapTexture.width != asset.diffuseTexture.width
                                                   || asset.normalMapTexture.height != asset.diffuseTexture.height))
            {
                reason = "el albedo y la normal no miden lo mismo";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
