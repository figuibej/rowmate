using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Un material de terceros (boat.livery…). Sin asignar: los materiales propios de Art/Materials/Scene.
    [CreateAssetMenu(menuName = "RowMate/Slots/Material", fileName = "material")]
    public sealed class MaterialSlot : AssetSlot<Material>
    {
        protected override bool ValidateAsset(Material asset, out string reason)
        {
            if (asset.shader == null || !asset.shader.isSupported)
            {
                reason = "el shader del material no es compatible con este equipo";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
