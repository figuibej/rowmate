using System;
using RowMate.Core.Scene.Stroke;
using UnityEngine;

namespace RowMate.Scene
{
    // Los huesos del remero (07-remero-palada/02-tech-design §3.2): los del cuerpo por RowerJoint (los nombres de Mixamo sin el prefijo;
    // el punto de agarre no tiene hueso) y los dedos del agarre (GripPose; solo los personajes del slot). El maniquí procedural y Y Bot
    // usan los mismos nombres ("mixamorig:Hips"…): se buscan por el sufijo después de los dos puntos, así también entra un rig con otro
    // prefijo ("mixamorig1:").
    public sealed class RowerBones
    {
        // Los sufijos por RowerJoint (null = sin hueso: el punto de agarre).
        public static readonly string?[] JointNames =
        {
            "Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "HeadTop_End",
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", null,
            "RightShoulder", "RightArm", "RightForeArm", "RightHand", null,
            "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase", "LeftToe_End",
            "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase", "RightToe_End",
        };

        private RowerBones(Transform root)
        {
            Root = root;
        }

        public Transform Root { get; }

        // Índice = RowerJoint.
        public Transform?[] Body { get; } = new Transform?[ScullingPose.JointCount];

        // Índice = GripPose.Fingers (izquierda y derecha).
        public Transform?[] LeftFingers { get; } = new Transform?[GripPose.Fingers.Count];

        public Transform?[] RightFingers { get; } = new Transform?[GripPose.Fingers.Count];

        public Transform? this[RowerJoint joint] => Body[(int)joint];

        // Los que faltan del cuerpo (vacío = completo).
        public string Missing()
        {
            var missing = "";
            for (var i = 0; i < JointNames.Length; i++)
            {
                if (JointNames[i] != null && Body[i] == null)
                {
                    missing += (missing.Length > 0 ? ", " : "") + JointNames[i];
                }
            }

            return missing;
        }

        public bool HasFingers
        {
            get
            {
                foreach (var finger in LeftFingers)
                {
                    if (finger == null)
                    {
                        return false;
                    }
                }

                foreach (var finger in RightFingers)
                {
                    if (finger == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static RowerBones Find(Transform root)
        {
            var bones = new RowerBones(root);
            for (var i = 0; i < JointNames.Length; i++)
            {
                var name = JointNames[i];
                bones.Body[i] = name != null ? FindBone(root, name) : null;
            }

            for (var i = 0; i < GripPose.Fingers.Count; i++)
            {
                bones.LeftFingers[i] = FindBone(root, "Left" + GripPose.Fingers[i].BoneSuffix);
                bones.RightFingers[i] = FindBone(root, "Right" + GripPose.Fingers[i].BoneSuffix);
            }

            return bones;
        }

        // Un hueso por su nombre de Mixamo sin el prefijo: "mixamorig:Hips", "<otro>:Hips" o "Hips".
        public static Transform? FindBone(Transform root, string suffix)
        {
            if (Matches(root.name, suffix))
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindBone(root.GetChild(i), suffix);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool Matches(string name, string suffix)
        {
            if (string.Equals(name, suffix, StringComparison.Ordinal))
            {
                return true;
            }

            var colon = name.LastIndexOf(':');
            return colon >= 0 && name.Length - colon - 1 == suffix.Length
                              && string.CompareOrdinal(name, colon + 1, suffix, 0, suffix.Length) == 0;
        }
    }
}
