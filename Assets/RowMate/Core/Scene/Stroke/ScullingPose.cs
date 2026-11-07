using System;

namespace RowMate.Core.Scene.Stroke
{
    // Las articulaciones de la pose (nombres de Mixamo sin el prefijo, más el punto de agarre de cada mano y la punta de cada pie).
    public enum RowerJoint
    {
        Hips,
        Spine,
        Spine1,
        Spine2,
        Neck,
        Head,
        HeadTop,
        LeftShoulder,
        LeftArm,
        LeftForeArm,
        LeftHand,
        LeftGrip,
        RightShoulder,
        RightArm,
        RightForeArm,
        RightHand,
        RightGrip,
        LeftUpLeg,
        LeftLeg,
        LeftFoot,
        LeftToeBase,
        LeftToeEnd,
        RightUpLeg,
        RightLeg,
        RightFoot,
        RightToeBase,
        RightToeEnd,
    }

    // Un remo en un cuadro (marco del bote): el barrido visual, el ángulo vertical de la caña (+ puño arriba), el plumeo, el centro
    // de la pala, el centro del puño, la flexión de la punta y el centro de pala de la curva (OarCurves.BladeCenterY).
    public struct OarState
    {
        public double SweepDeg;
        public double PitchDeg;
        public double FeatherDeg;
        public SceneVec3 BladeCenter;
        public SceneVec3 Grip;
        public double ShaftBendM;
        public double CurveBladeY;
    }

    // La pose del remero y de los remos en el marco del bote (origen en el punto medio entre los pernos al nivel del agua, +z proa,
    // +y arriba; el remero mira a −z). Los arreglos se reservan una vez (Create); ScullingKinematics.Evaluate solo escribe.
    public struct ScullingPose
    {
        public const int JointCount = (int)RowerJoint.RightToeEnd + 1;

        // Posición de cada articulación (índice = RowerJoint).
        public SceneVec3[] Joints;

        // El eje lateral del hueso que nace en cada articulación (para el giro del hueso alrededor de su eje): +x en el tronco, la
        // normal del plano del codo o de la rodilla en brazos y piernas, girada por la mirada en la cabeza.
        public SceneVec3[] Lateral;

        // El centro de la cara de arriba del asiento (el carro).
        public SceneVec3 SeatPosition;

        // El ángulo del tronco pedido (+ hacia popa) y la protracción de los hombros (grados).
        public double TrunkDeg;
        public double ProtractionDeg;

        // La mano izquierda del remero va del lado x > 0 (D7-02).
        public OarState Left;
        public OarState Right;

        // El punto al que mira (30 m a popa, a la altura de los ojos).
        public SceneVec3 GazeTarget;

        // Lo que le faltó a cada mano para llegar al puño (m; 0 si llegó).
        public double LeftReachError;
        public double RightReachError;

        public static ScullingPose Create()
        {
            return new ScullingPose { Joints = new SceneVec3[JointCount], Lateral = new SceneVec3[JointCount] };
        }

        public SceneVec3 this[RowerJoint joint] => Joints[(int)joint];
    }

    // Los ángulos que mide la tabla de ANIMATION_BIBLE §3.4 sobre una pose (o sobre las posiciones medidas en el rig, WP-B1):
    // rodilla, cadera y codo = ángulo interior (180° = recto); la cadera, contra el eje de la pelvis (Hips → Spine); tronco desde la
    // vertical, + hacia popa (de las articulaciones de la cadera al punto medio de los hombros); hombro = flexión del brazo respecto
    // del tronco en el plano sagital (+ adelante).
    public static class PoseAngles
    {
        public static double TrunkDeg(SceneVec3[] joints)
        {
            var up = ShoulderCenter(joints) - HipCenter(joints);
            return Math.Atan2(-up.Z, up.Y) * 180 / Math.PI;
        }

        public static double KneeDeg(SceneVec3[] joints, bool left)
        {
            var hip = joints[(int)(left ? RowerJoint.LeftUpLeg : RowerJoint.RightUpLeg)];
            var knee = joints[(int)(left ? RowerJoint.LeftLeg : RowerJoint.RightLeg)];
            var ankle = joints[(int)(left ? RowerJoint.LeftFoot : RowerJoint.RightFoot)];
            return SceneVec3.AngleDeg(hip - knee, ankle - knee);
        }

        public static double HipDeg(SceneVec3[] joints, bool left)
        {
            var pelvis = joints[(int)RowerJoint.Spine] - joints[(int)RowerJoint.Hips];
            var hip = joints[(int)(left ? RowerJoint.LeftUpLeg : RowerJoint.RightUpLeg)];
            var knee = joints[(int)(left ? RowerJoint.LeftLeg : RowerJoint.RightLeg)];
            return SceneVec3.AngleDeg(pelvis, knee - hip);
        }

        public static double ElbowDeg(SceneVec3[] joints, bool left)
        {
            var shoulder = joints[(int)(left ? RowerJoint.LeftArm : RowerJoint.RightArm)];
            var elbow = joints[(int)(left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm)];
            var wrist = joints[(int)(left ? RowerJoint.LeftHand : RowerJoint.RightHand)];
            return SceneVec3.AngleDeg(shoulder - elbow, wrist - elbow);
        }

        public static double ShoulderDeg(SceneVec3[] joints, bool left)
        {
            var up = (ShoulderCenter(joints) - HipCenter(joints)).Normalized(SceneVec3.UnitY);
            var forward = new SceneVec3(0, up.Z, -up.Y);
            var down = -up;
            var arm = joints[(int)(left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm)] - joints[(int)(left ? RowerJoint.LeftArm : RowerJoint.RightArm)];
            return Math.Atan2(SceneVec3.Dot(arm, forward), SceneVec3.Dot(arm, down)) * 180 / Math.PI;
        }

        // El ángulo de la muñeca: la mano (muñeca → agarre) contra el antebrazo; 0 = plana.
        public static double WristDeg(SceneVec3[] joints, bool left)
        {
            var elbow = joints[(int)(left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm)];
            var wrist = joints[(int)(left ? RowerJoint.LeftHand : RowerJoint.RightHand)];
            var grip = joints[(int)(left ? RowerJoint.LeftGrip : RowerJoint.RightGrip)];
            return SceneVec3.AngleDeg(wrist - elbow, grip - wrist);
        }

        // La inclinación de la canilla respecto de la vertical en el plano sagital (grados; 0 = vertical, + rodilla hacia popa).
        public static double ShinTiltDeg(SceneVec3[] joints, bool left)
        {
            var shin = joints[(int)(left ? RowerJoint.LeftLeg : RowerJoint.RightLeg)] - joints[(int)(left ? RowerJoint.LeftFoot : RowerJoint.RightFoot)];
            return Math.Atan2(-shin.Z, shin.Y) * 180 / Math.PI;
        }

        public static SceneVec3 HipCenter(SceneVec3[] joints)
        {
            return SceneVec3.Lerp(joints[(int)RowerJoint.LeftUpLeg], joints[(int)RowerJoint.RightUpLeg], 0.5);
        }

        public static SceneVec3 ShoulderCenter(SceneVec3[] joints)
        {
            return SceneVec3.Lerp(joints[(int)RowerJoint.LeftArm], joints[(int)RowerJoint.RightArm], 0.5);
        }
    }
}
