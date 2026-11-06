using UnityEngine;

namespace RowMate.Scene.Slots
{
    // La vegetación del relleno (vegetation.*): un prefab del pack (pasto, helecho, arbusto) del que se toman la malla y el material
    // para instanciar. Sin asignar: las mallas de Core (SmallMeshes, PineMesh.BuildForest) con los shaders propios.
    [CreateAssetMenu(menuName = "RowMate/Slots/Vegetation", fileName = "vegetation")]
    public sealed class VegetationSlot : AssetSlot<GameObject>
    {
        protected override bool ValidateAsset(GameObject asset, out string reason)
        {
            return MeshPrefab.Validate(asset, Budget, out reason);
        }
    }
}
