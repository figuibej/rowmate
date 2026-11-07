using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene.Stroke
{
    // La huella del remero en la pantalla (AC-WKT-17-14, AC-WKT-18-23): proyecta la pose con la cámara del contrato (respiración 0)
    // y devuelve rectángulos en dp de ventana. Body: el cuerpo (cabeza, tronco, brazos y manos), agrandado por su grosor (0,12 m en
    // la cabeza y el tronco, 0,06 en los brazos); BoatToPins: el bote alrededor del remero, el remero con las piernas y los remos hasta los pernos
    // (las palas pueden salir por los costados) con el casco desde HullNearZ. El bote cabecea poco (± 1,5°): se proyecta sin cabeceo.
    public static class RowerFootprint
    {
        public const double TrunkThickness = 0.12, ArmThickness = 0.06;

        // El bote alrededor del remero: del casco, desde 1,5 m a popa de los pernos (z de mundo 8) hasta la proa. La popa de la 6a
        // (z 5,3) se proyecta debajo de la zona en horizontal (ya en la 6a; Para issues).
        public const double HullNearZ = -1.5;

        private static readonly RowerJoint[] TrunkJoints =
        {
            RowerJoint.HeadTop, RowerJoint.Head, RowerJoint.Neck, RowerJoint.Spine2, RowerJoint.Spine1, RowerJoint.Spine, RowerJoint.Hips,
            RowerJoint.LeftShoulder, RowerJoint.RightShoulder, RowerJoint.LeftArm, RowerJoint.RightArm,
        };

        private static readonly RowerJoint[] ArmJoints =
        {
            RowerJoint.LeftForeArm, RowerJoint.RightForeArm, RowerJoint.LeftHand, RowerJoint.RightHand, RowerJoint.LeftGrip, RowerJoint.RightGrip,
        };

        public static void Body(in SceneCamera cam, in ScullingPose pose, out DpRect body)
        {
            var box = Box.Empty;
            foreach (var joint in TrunkJoints)
            {
                box.Add(cam, pose.Joints[(int)joint], TrunkThickness);
            }

            foreach (var joint in ArmJoints)
            {
                box.Add(cam, pose.Joints[(int)joint], ArmThickness);
            }

            body = box.Rect;
        }

        public static void BoatToPins(in SceneCamera cam, in ScullingPose pose, in BoatSpec boat, out DpRect rect)
        {
            var box = Box.Empty;
            for (var i = 0; i < pose.Joints.Length; i++)
            {
                box.Add(cam, pose.Joints[i], ArmThickness);
            }

            var half = boat.BeamGunwale / 2;
            foreach (var z in new[] { HullNearZ, boat.HullBow - SceneCamera.BoatZ })
            {
                box.Add(cam, new SceneVec3(-half, Art.LakeArt.HullFreeboard, z), 0);
                box.Add(cam, new SceneVec3(half, Art.LakeArt.HullFreeboard, z), 0);
                box.Add(cam, new SceneVec3(-half, -Art.LakeArt.HullDraft, z), 0);
                box.Add(cam, new SceneVec3(half, -Art.LakeArt.HullDraft, z), 0);
            }

            box.Add(cam, new SceneVec3(boat.PinX, boat.PinY, 0), 0);
            box.Add(cam, new SceneVec3(-boat.PinX, boat.PinY, 0), 0);
            box.Add(cam, pose.Left.Grip, 0);
            box.Add(cam, pose.Right.Grip, 0);
            rect = box.Rect;
        }

        private struct Box
        {
            public double Left, Top, Right, Bottom;

            public static Box Empty => new Box { Left = double.MaxValue, Top = double.MaxValue, Right = double.MinValue, Bottom = double.MinValue };

            public DpRect Rect => DpRect.FromEdges(Left, Top, Right, Bottom);

            // Un punto del marco del bote (z + 9,5 en el mundo de la cámara) agrandado por su radio en metros.
            public void Add(in SceneCamera cam, in SceneVec3 p, double radius)
            {
                var z = p.Z + SceneCamera.BoatZ;
                if (!cam.TryProject(p.X, p.Y, z, out var sx, out var sy))
                {
                    return;
                }

                var r = radius * cam.ScaleAt(z);
                Left = Math.Min(Left, sx - r);
                Right = Math.Max(Right, sx + r);
                Top = Math.Min(Top, sy - r);
                Bottom = Math.Max(Bottom, sy + r);
            }
        }
    }
}
