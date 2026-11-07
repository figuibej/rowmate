using System;
using RowMate.Core.Scene.Stroke;
using UnityEngine;

namespace RowMate.Scene
{
    // Mueve el esqueleto del remero con la pose de Core (07-remero-palada/02-tech-design §3.2; DT7-01): sin Animator ni Animation
    // Rigging. Initialize mide la pose de reposo (T-pose, con el personaje ya girado 180° en y bajo boat-pitch): por hueso, la rotación,
    // la dirección a su hijo y un eje lateral de referencia en el marco del bote (el mismo significado anatómico que el eje lateral que da
    // Core: +x = la izquierda del remero en el tronco, las piernas y los pies; arriba en las clavículas; abajo en el brazo izquierdo y
    // arriba en el derecho, con las palmas hacia abajo). Apply, en cada cuadro y de la raíz a las puntas: Hips a su lugar y cada hueso
    // apuntando a la articulación de Core de su hijo desde donde quedó (los errores no se acumulan):
    //   rotación = LookRotation(dir, lateral) · inversa(LookRotation(dir_reposo, lateral_reposo)) · rotación_reposo.
    // El mismo código para el maniquí y para Y Bot. Los dedos (Y Bot) toman GripPose una vez al armar. Sin asignar en Apply.
    public sealed class RowerRigDriver
    {
        // Los huesos que se orientan, de la raíz a las puntas, con la articulación a la que apuntan.
        private static readonly RowerJoint[] Driven =
        {
            RowerJoint.Hips, RowerJoint.Spine, RowerJoint.Spine1, RowerJoint.Spine2, RowerJoint.Neck, RowerJoint.Head,
            RowerJoint.LeftShoulder, RowerJoint.LeftArm, RowerJoint.LeftForeArm, RowerJoint.LeftHand,
            RowerJoint.RightShoulder, RowerJoint.RightArm, RowerJoint.RightForeArm, RowerJoint.RightHand,
            RowerJoint.LeftUpLeg, RowerJoint.LeftLeg, RowerJoint.LeftFoot, RowerJoint.LeftToeBase,
            RowerJoint.RightUpLeg, RowerJoint.RightLeg, RowerJoint.RightFoot, RowerJoint.RightToeBase,
        };

        private static readonly RowerJoint[] Targets =
        {
            RowerJoint.Spine, RowerJoint.Spine1, RowerJoint.Spine2, RowerJoint.Neck, RowerJoint.Head, RowerJoint.HeadTop,
            RowerJoint.LeftArm, RowerJoint.LeftForeArm, RowerJoint.LeftHand, RowerJoint.LeftGrip,
            RowerJoint.RightArm, RowerJoint.RightForeArm, RowerJoint.RightHand, RowerJoint.RightGrip,
            RowerJoint.LeftLeg, RowerJoint.LeftFoot, RowerJoint.LeftToeBase, RowerJoint.LeftToeEnd,
            RowerJoint.RightLeg, RowerJoint.RightFoot, RowerJoint.RightToeBase, RowerJoint.RightToeEnd,
        };

        private readonly RowerBones _bones;
        private readonly Transform _frame;
        private readonly Transform?[] _driven = new Transform?[Driven.Length];
        private readonly Quaternion[] _correction = new Quaternion[Driven.Length];
        private readonly Vector3[] _measured = new Vector3[ScullingPose.JointCount];

        public RowerRigDriver(RowerBones bones, Transform boatFrame)
        {
            _bones = bones ?? throw new ArgumentNullException(nameof(bones));
            _frame = boatFrame != null ? boatFrame : throw new ArgumentNullException(nameof(boatFrame));
        }

        public RowerBones Bones => _bones;

        public bool Initialized { get; private set; }

        // Las posiciones de las articulaciones en el marco del bote después del último Apply (RowerJointsForTest; el punto de agarre =
        // la mano + HandToGrip en la dirección del antebrazo, como Core).
        public Vector3[] Measured => _measured;

        // Mide el reposo. El personaje tiene que estar en T-pose, girado 180° en y bajo el marco del bote (mira a −z).
        public void Initialize()
        {
            var frameRotation = _frame.rotation;
            for (var i = 0; i < Driven.Length; i++)
            {
                var bone = _bones[Driven[i]];
                _driven[i] = bone;
                if (bone == null)
                {
                    continue;
                }

                var child = _bones[Targets[i]];
                Vector3 dir;
                if (child != null)
                {
                    dir = _frame.InverseTransformDirection(child.position - bone.position);
                }
                else
                {
                    // La mano (el agarre no tiene hueso): en línea con el antebrazo.
                    var forearm = _bones[Driven[i] == RowerJoint.LeftHand ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm];
                    dir = forearm != null ? _frame.InverseTransformDirection(bone.position - forearm.position) : Vector3.right;
                }

                var rest = Quaternion.Inverse(frameRotation) * bone.rotation;
                var look = SafeLook(dir, RestLateral(Driven[i]));
                _correction[i] = Quaternion.Inverse(look) * rest;
            }

            Initialized = true;
        }

        // El eje lateral de referencia del reposo, en el marco del bote (ver arriba).
        public static Vector3 RestLateral(RowerJoint joint)
        {
            switch (joint)
            {
                case RowerJoint.LeftShoulder:
                case RowerJoint.RightShoulder:
                    return Vector3.up;
                case RowerJoint.LeftArm:
                case RowerJoint.LeftForeArm:
                case RowerJoint.LeftHand:
                    return Vector3.down;
                case RowerJoint.RightArm:
                case RowerJoint.RightForeArm:
                case RowerJoint.RightHand:
                    return Vector3.up;
                default:
                    return Vector3.right;
            }
        }

        // El agarre de los dedos (DT7-07): rotaciones locales relativas al reposo medido, una vez (los dedos siguen a la mano). Con la
        // mano en T-pose en el marco del bote (la izquierda hacia +x, las palmas abajo): la flexión gira alrededor de z (las puntas hacia
        // −y) y el pulgar, alrededor de y (hacia los dedos) con su apertura alrededor de z.
        public void ApplyGrip()
        {
            for (var k = 0; k < 2; k++)
            {
                var left = k == 0;
                var fingers = left ? _bones.LeftFingers : _bones.RightFingers;
                var sign = left ? -1f : 1f;
                for (var i = 0; i < fingers.Length; i++)
                {
                    var bone = fingers[i];
                    if (bone == null)
                    {
                        continue;
                    }

                    var finger = GripPose.Fingers[i];
                    var thumb = finger.BoneSuffix.StartsWith("HandThumb", StringComparison.Ordinal);
                    var curlAxis = _frame.TransformDirection(thumb ? Vector3.up : Vector3.forward);
                    Rotate(bone, curlAxis, sign * (float)finger.CurlDeg);
                    if (finger.SpreadDeg != 0)
                    {
                        Rotate(bone, _frame.TransformDirection(Vector3.forward), sign * (float)finger.SpreadDeg);
                    }
                }
            }
        }

        private static void Rotate(Transform bone, Vector3 worldAxis, float degrees)
        {
            var localAxis = Quaternion.Inverse(bone.rotation) * worldAxis;
            bone.localRotation = bone.localRotation * Quaternion.AngleAxis(degrees, localAxis);
        }

        // Un cuadro: la pose de Core (marco del bote) en los huesos. Sin asignar.
        public void Apply(in ScullingPose pose)
        {
            if (!Initialized)
            {
                return;
            }

            var joints = pose.Joints;
            var lateral = pose.Lateral;
            var hips = _driven[0];
            if (hips != null)
            {
                hips.position = _frame.TransformPoint(ToVector(joints[(int)RowerJoint.Hips]));
            }

            for (var i = 0; i < Driven.Length; i++)
            {
                var bone = _driven[i];
                if (bone == null)
                {
                    continue;
                }

                var target = _frame.TransformPoint(ToVector(joints[(int)Targets[i]]));
                var dir = target - bone.position;
                if (dir.sqrMagnitude < 1e-10f)
                {
                    continue;
                }

                var up = _frame.TransformDirection(ToVector(lateral[(int)Driven[i]]));
                bone.rotation = SafeLook(dir, up) * _correction[i];
            }

            Measure(pose);
        }

        private void Measure(in ScullingPose pose)
        {
            for (var j = 0; j < _measured.Length; j++)
            {
                var bone = _bones.Body[j];
                if (bone != null)
                {
                    _measured[j] = _frame.InverseTransformPoint(bone.position);
                }
            }

            for (var k = 0; k < 2; k++)
            {
                var left = k == 0;
                var hand = (int)(left ? RowerJoint.LeftHand : RowerJoint.RightHand);
                var elbow = (int)(left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm);
                var grip = (int)(left ? RowerJoint.LeftGrip : RowerJoint.RightGrip);
                var along = (_measured[hand] - _measured[elbow]).normalized;
                var coreHand = pose.Joints[hand];
                var coreGrip = pose.Joints[grip];
                var handToGrip = (float)Math.Sqrt((coreGrip.X - coreHand.X) * (coreGrip.X - coreHand.X) + (coreGrip.Y - coreHand.Y) * (coreGrip.Y - coreHand.Y)
                                                  + (coreGrip.Z - coreHand.Z) * (coreGrip.Z - coreHand.Z));
                _measured[grip] = _measured[hand] + along * handToGrip;
            }
        }

        // LookRotation con un lateral que puede ser casi paralelo (cae a otro eje en lugar de dar una rotación degenerada).
        private static Quaternion SafeLook(Vector3 forward, Vector3 up)
        {
            var f = forward.normalized;
            var u = up - f * Vector3.Dot(up, f);
            if (u.sqrMagnitude < 1e-8f)
            {
                u = Mathf.Abs(f.y) < 0.9f ? Vector3.up : Vector3.forward;
            }

            return Quaternion.LookRotation(f, u);
        }

        public static Vector3 ToVector(in SceneVec3 v)
        {
            return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        // Los largos de hueso de un rig en T-pose (el informe de B0 y SetRower con el personaje del slot), en el marco de la raíz del
        // personaje (mira a +z, la izquierda en −x, los pies en y = 0). El agarre: de la muñeca al nudillo del medio. La altura de la
        // cadera sobre el asiento es la del maniquí escalada por el alto.
        public static RowerSpec MeasureSpec(RowerBones bones)
        {
            var root = bones.Root;
            Vector3 P(RowerJoint joint) => root.InverseTransformPoint(bones[joint]!.position);
            var lUp = P(RowerJoint.LeftUpLeg);
            var rUp = P(RowerJoint.RightUpLeg);
            var hip = (lUp + rUp) * 0.5f;
            var middle = RowerBones.FindBone(root, "LeftHandMiddle1");
            var toeEnd = bones[RowerJoint.LeftToeEnd];
            var foot = P(RowerJoint.LeftFoot);
            var height = P(RowerJoint.HeadTop).y;
            var k = height / RowerSpec.Mannequin.Height;
            return new RowerSpec(
                hipHalfWidth: Math.Abs(lUp.x - rUp.x) / 2,
                hipAboveSeat: RowerSpec.Mannequin.HipAboveSeat * k,
                hipsAboveJoints: P(RowerJoint.Hips).y - hip.y,
                pelvisLength: P(RowerJoint.Spine).y - hip.y,
                spineLength: P(RowerJoint.Spine1).y - P(RowerJoint.Spine).y,
                spine1Length: P(RowerJoint.Spine2).y - P(RowerJoint.Spine1).y,
                spine2Length: P(RowerJoint.Neck).y - P(RowerJoint.Spine2).y,
                shoulderDrop: P(RowerJoint.Neck).y - P(RowerJoint.LeftArm).y,
                clavicleHalfWidth: Math.Abs(P(RowerJoint.LeftShoulder).x),
                shoulderHalfWidth: Math.Abs(P(RowerJoint.LeftArm).x),
                neckLength: P(RowerJoint.Head).y - P(RowerJoint.Neck).y,
                headTopAbove: P(RowerJoint.HeadTop).y - P(RowerJoint.Head).y,
                upperArm: Vector3.Distance(P(RowerJoint.LeftForeArm), P(RowerJoint.LeftArm)),
                forearm: Vector3.Distance(P(RowerJoint.LeftHand), P(RowerJoint.LeftForeArm)),
                // El centro del agarre: el 75 % de la muñeca al nudillo del medio (el puño cerrado rodea el puño del remo ahí).
                handToGrip: middle != null ? 0.75 * Vector3.Distance(root.InverseTransformPoint(middle.position), P(RowerJoint.LeftHand)) : RowerSpec.Mannequin.HandToGrip * k,
                thigh: Vector3.Distance(P(RowerJoint.LeftLeg), lUp),
                shin: Vector3.Distance(foot, P(RowerJoint.LeftLeg)),
                ankleHeight: foot.y,
                footLength: toeEnd != null ? (root.InverseTransformPoint(toeEnd.position).z - foot.z) / 0.8 : RowerSpec.Mannequin.FootLength * k,
                footHalfSpread: Math.Abs(foot.x));
        }
    }
}
