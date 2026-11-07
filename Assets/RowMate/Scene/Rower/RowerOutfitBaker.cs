using System;
using System.Collections.Generic;
using RowMate.Core.Scene.Art;
using UnityEngine;

namespace RowMate.Scene
{
    // La ropa de un personaje del slot (07-remero-palada/02-tech-design §3.2; DA7-05): una copia de cada malla con la región del shader
    // Rower en el rojo del color de vértice, por el hueso dominante de cada vértice: Hips, Spine*, UpLeg y Shoulder → mono (con las franjas
    // donde |normal.x| > 0,85: los costados del tronco y del muslo); Arm, ForeArm, Hand, Leg, Neck y Head → piel; Foot y ToeBase → medias;
    // la malla de las articulaciones de Y Bot (Alpha_Joints) → región 7. Las copias quedan en `owned` (las destruye el mundo).
    public static class RowerOutfitBaker
    {
        public const double StripeNormalX = 0.85;

        public static void Bake(GameObject character, Material material, List<UnityEngine.Object> owned)
        {
            foreach (var renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var source = renderer.sharedMesh;
                if (source == null)
                {
                    continue;
                }

                var copy = UnityEngine.Object.Instantiate(source);
                copy.name = source.name + "-rowmate";
                copy.hideFlags = HideFlags.HideAndDontSave;
                owned.Add(copy);
                var joints = renderer.name.IndexOf("Joints", StringComparison.OrdinalIgnoreCase) >= 0;
                var weights = copy.boneWeights;
                var normals = copy.normals;
                var bones = renderer.bones;
                var colors = new Color[copy.vertexCount];
                for (var v = 0; v < colors.Length; v++)
                {
                    int region;
                    if (joints)
                    {
                        region = RowerArt.RegionJoints;
                    }
                    else
                    {
                        var index = v < weights.Length ? weights[v].boneIndex0 : 0;
                        var name = index >= 0 && index < bones.Length && bones[index] != null ? bones[index].name : "";
                        region = RegionOf(name);
                        if (region == RowerArt.RegionSuit && v < normals.Length && Math.Abs(normals[v].x) > StripeNormalX)
                        {
                            region = RowerArt.RegionStripe;
                        }
                    }

                    colors[v] = new Color((float)RowerArt.RegionToRed(region), 0, 0, 1);
                }

                copy.colors = colors;
                renderer.sharedMesh = copy;
                var materials = new Material[Math.Max(1, copy.subMeshCount)];
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        // La región de un hueso por su nombre de Mixamo (con o sin el prefijo).
        public static int RegionOf(string boneName)
        {
            var colon = boneName.LastIndexOf(':');
            var name = colon >= 0 ? boneName.Substring(colon + 1) : boneName;
            if (name.StartsWith("Hips", StringComparison.Ordinal) || name.StartsWith("Spine", StringComparison.Ordinal)
                || name.EndsWith("UpLeg", StringComparison.Ordinal) || name.EndsWith("Shoulder", StringComparison.Ordinal))
            {
                return RowerArt.RegionSuit;
            }

            if (name.EndsWith("Foot", StringComparison.Ordinal) || name.IndexOf("Toe", StringComparison.Ordinal) >= 0)
            {
                return RowerArt.RegionSocks;
            }

            return RowerArt.RegionSkin;
        }
    }
}
