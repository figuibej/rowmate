using System;
using System.Collections.Generic;
using UnityEngine;

namespace RowMate.Scene.Slots
{
    // El slot del remero (rower.character; 07-remero-palada/02-tech-design §1.2, §3.2; ASSET_SLOTS §2; DT7-13, QA7-G-04). Se versiona
    // SIEMPRE con la fuente Procedural (el maniquí) y sus rutas esperadas: primero el personaje realista, después Y Bot. La referencia al
    // FBX de Mixamo nunca entra al repo: la escribe RowerCharacterBindingBuilder (Editor) en un binding generado e ignorado
    // (RowerCharacterBinding), y la escena lo resuelve con RowerCharacterResolver. La validación es la misma en el Editor y en la app.
    [CreateAssetMenu(menuName = "RowMate/Slots/Rower character", fileName = "rower.character")]
    public sealed class RowerCharacterSlot : AssetSlot<GameObject>
    {
        public const string RealisticPath = "Assets/ThirdParty/Mixamo/RowerRealistic/RowerRealistic.fbx";
        public const string YBotPath = "Assets/ThirdParty/Mixamo/YBot/YBot.fbx";

        // Las rutas en orden de preferencia (DT7-13: el realista antes que Y Bot).
        public static readonly string[] DefaultPaths = { RealisticPath, YBotPath };

        [SerializeField] private string[] _expectedPaths = DefaultPaths;

        public IReadOnlyList<string> ExpectedPaths => _expectedPaths != null && _expectedPaths.Length > 0 ? _expectedPaths : DefaultPaths;

        public void SetExpectedPaths(string[] paths)
        {
            _expectedPaths = paths != null && paths.Length > 0 ? (string[])paths.Clone() : DefaultPaths;
        }

        protected override bool ValidateAsset(GameObject asset, out string reason)
        {
            return RowerCharacterValidation.Validate(asset, out reason, out _);
        }
    }

    // Lo que se mide de un personaje del slot (el informe de B0 y las pruebas).
    public struct RowerCharacterStats
    {
        public bool Humanoid;
        public int FingerBones;
        public double HeightM;
        public long Triangles;
        public int Bones;
        public bool Readable;
    }

    // La validación de un personaje Humanoid de Mixamo (02-tech-design §3.2; QA7-G-01): avatar válido y humano, los 30 huesos de los
    // dedos mapeados, alto de los pies a HeadTop_End entre 1,55 y 2,05 m, ≤ 60 000 triángulos (todas las submallas de todos los
    // SkinnedMeshRenderer: Y Bot 34 480 + 20 840 = 55 320), ≤ 75 huesos (Y Bot 65) y las mallas legibles (las copia RowerOutfitBaker).
    public static class RowerCharacterValidation
    {
        public const double MinHeight = 1.55, MaxHeight = 2.05;
        public const int MaxTriangles = 60000, MaxBones = 75, FingerBoneCount = 30;

        public static bool Validate(GameObject? model, out string reason, out RowerCharacterStats stats)
        {
            stats = default;
            if (model == null)
            {
                reason = "falta el modelo";
                return false;
            }

            var animator = model.GetComponentInChildren<Animator>(true);
            var avatar = animator != null ? animator.avatar : null;
            stats.Humanoid = avatar != null && avatar.isValid && avatar.isHuman;
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var bones = new HashSet<Transform>();
            stats.Readable = renderers.Length > 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    stats.Readable = false;
                    continue;
                }

                stats.Readable &= mesh.isReadable;
                for (var i = 0; i < mesh.subMeshCount; i++)
                {
                    stats.Triangles += (long)mesh.GetIndexCount(i) / 3;
                }

                foreach (var bone in renderer.bones)
                {
                    if (bone != null)
                    {
                        bones.Add(bone);
                    }
                }
            }

            stats.Bones = bones.Count;
            if (stats.Humanoid)
            {
                foreach (var human in avatar!.humanDescription.human)
                {
                    if (IsFinger(human.humanName))
                    {
                        stats.FingerBones++;
                    }
                }
            }

            stats.HeightM = Height(model.transform);
            if (!stats.Humanoid)
            {
                reason = "el modelo no es Humanoid (avatar inválido o Generic)";
                return false;
            }

            if (stats.FingerBones < FingerBoneCount)
            {
                reason = "tiene " + stats.FingerBones + " huesos de dedos mapeados (se piden " + FingerBoneCount + ")";
                return false;
            }

            if (!(stats.HeightM >= MinHeight && stats.HeightM <= MaxHeight))
            {
                reason = "mide " + stats.HeightM.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " m (de " + MinHeight + " a " + MaxHeight + ")";
                return false;
            }

            if (stats.Triangles > MaxTriangles)
            {
                reason = "tiene " + stats.Triangles + " triángulos (tope " + MaxTriangles + ")";
                return false;
            }

            if (stats.Bones > MaxBones)
            {
                reason = "tiene " + stats.Bones + " huesos (tope " + MaxBones + ")";
                return false;
            }

            if (!stats.Readable)
            {
                reason = "las mallas no son legibles (Read/Write apagado: reimportar con MixamoImportPostprocessor)";
                return false;
            }

            reason = "";
            return true;
        }

        // Del piso del modelo (la raíz) a HeadTop_End, en el marco de la raíz.
        public static double Height(Transform root)
        {
            var top = RowerBones.FindBone(root, "HeadTop_End");
            if (top == null)
            {
                return 0;
            }

            var lowest = 0.0;
            var foot = RowerBones.FindBone(root, "LeftToeBase");
            if (foot != null)
            {
                lowest = Math.Min(lowest, root.InverseTransformPoint(foot.position).y);
            }

            return root.InverseTransformPoint(top.position).y - lowest;
        }

        private static bool IsFinger(string humanName)
        {
            return humanName.IndexOf("Proximal", StringComparison.Ordinal) >= 0 || humanName.IndexOf("Intermediate", StringComparison.Ordinal) >= 0
                   || humanName.IndexOf("Distal", StringComparison.Ordinal) >= 0;
        }
    }
}
