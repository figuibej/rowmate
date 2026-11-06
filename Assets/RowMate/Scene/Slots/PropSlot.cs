using UnityEngine;

namespace RowMate.Scene.Slots
{
    // Un objeto de la orilla o una pieza del bote (prop.*, boat.*): un prefab con su malla y su material (de él se toman para
    // instanciar). El presupuesto es de triángulos del LOD 0 (0 = sin tope). Sin asignar: el procedural de Core (PineMesh,
    // CabinMesh, PierMesh, HullMesh…).
    [CreateAssetMenu(menuName = "RowMate/Slots/Prop", fileName = "prop")]
    public sealed class PropSlot : AssetSlot<GameObject>
    {
        protected override bool ValidateAsset(GameObject asset, out string reason)
        {
            return MeshPrefab.Validate(asset, Budget, out reason);
        }
    }

    // Lo que se toma de un prefab de terceros para dibujarlo instanciado: la primera malla con su material.
    public static class MeshPrefab
    {
        public static bool TryGet(GameObject? prefab, out Mesh? mesh, out Material? material)
        {
            mesh = null;
            material = null;
            if (prefab == null)
            {
                return false;
            }

            var filter = prefab.GetComponentInChildren<MeshFilter>(true);
            var renderer = prefab.GetComponentInChildren<MeshRenderer>(true);
            mesh = filter != null ? filter.sharedMesh : null;
            material = renderer != null ? renderer.sharedMaterial : null;
            return mesh != null && material != null;
        }

        public static bool Validate(GameObject asset, int budget, out string reason)
        {
            if (!TryGet(asset, out var mesh, out _))
            {
                reason = "el prefab no tiene una malla con su material";
                return false;
            }

            var triangles = 0L;
            for (var i = 0; i < mesh!.subMeshCount; i++)
            {
                triangles += (long)mesh.GetIndexCount(i) / 3;
            }

            if (budget > 0 && triangles > budget)
            {
                reason = "la malla tiene " + triangles + " triángulos (tope " + budget + ")";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
