using System;

namespace RowMate.Core.Scene.Stroke
{
    // La pose 3D del remero y de los remos sobre la fase canónica c (AC-SCN-05-01 a -09, AC-SCN-08-03, -12; ANIMATION_BIBLE §3.3 a
    // §3.6; 00-arte-remero §4), en el marco del bote. Los remos siguen la cinemática y las manos siguen a los remos:
    // 1. Remos: el barrido visual, la altura de la pala de la curva y el puño por la palanca; con |α| < 25° los puños se cruzan
    //    (la mano izquierda del remero, x > 0, arriba y a popa): la mitad del cruce en el puño del remo y la otra mitad en la mano, así
    //    la separación de las manos es de 5 cm, la mano queda a ≤ 1,5 cm de su puño y la pala sigue fuera del agua (≥ 0,06).
    // 2. Carro: la cadera en lerp(catch, finish, piernas), sin que los brazos tengan que estirarse más del 99,7 % (el cuerpo cuelga
    //    del remo: en el drive y al final de la recuperación las piernas siguen al puño).
    // 3. Tronco: lerp(+28°, −18° − 2°·e, espalda) (la tabla de §3.4; el +25/−20 del contrato sigue en StrokeCycle.TorsoLean), con la
    //    pelvis al 60 % + 6° y la columna 15/13/12 %; la respiración del reposo se suma al pecho y a los hombros.
    // 4. Piernas: IK de la cadera al tobillo fijo en la pedalera (RigFit), con la guía 0,6 m arriba y ± 0,12 afuera.
    // 5. Brazos: IK del hombro (protracción +8° con los brazos estirados, −6° con los brazos al pecho) al puño, con la mano en línea
    //    con el antebrazo (muñeca plana) y la guía del codo afuera 0,35, abajo 0,25 y a proa 0,25 m.
    // 6. Cabeza: mira 30 m a popa a la altura de los ojos con peso 0,85, más la mirada del reposo.
    // Puro y sin asignar: escribe en los arreglos de la pose.
    public static class ScullingKinematics
    {
        public const double TrunkCatchDeg = 28, TrunkFinishDeg = -18, TrunkEffortDeg = -2;
        public const double ProtractionCatchDeg = 8, ProtractionFinishDeg = -6;
        public const double PelvisShare = 0.6, PelvisBaseDeg = 6;
        public const double CrossStartDeg = 20, CrossSpanDeg = 10;
        public const double CrossLiftOar = 0.0125, CrossLiftHand = 0.0125, CrossSternOar = 0.0075, CrossSternHand = 0.0075;
        public const double KneeHintUp = 0.6, KneeHintOut = 0.12;
        public const double ElbowHintOut = 0.35, ElbowHintDown = 0.25, ElbowHintBow = 0.25;
        public const double GazeDistance = 30, HeadGazeWeight = 0.85, HandFeatherShare = 0.25;
        public const double MaxHipBehindCatch = 0.08;

        public static void Evaluate(double canonical, double effort, in BoatSpec boat, in RowerSpec rower, in RigFit fit,
                                    in RowerIdleSample idle, ref ScullingPose pose)
        {
            if (pose.Joints == null || pose.Lateral == null)
            {
                throw new ArgumentException("ScullingKinematics: la pose se arma con ScullingPose.Create.", nameof(pose));
            }

            var c = canonical - Math.Floor(canonical);
            var legs = StrokeCycle.Legs(c);
            var back = StrokeCycle.Back(c);
            var arms = StrokeCycle.Arms(c);
            var trunk = StrokeCycle.Lerp(TrunkCatchDeg, TrunkFinishDeg + TrunkEffortDeg * effort, back);
            var protraction = StrokeCycle.Lerp(ProtractionCatchDeg, ProtractionFinishDeg, arms);
            pose.TrunkDeg = trunk;
            pose.ProtractionDeg = protraction;

            // 1. Los remos.
            Oar(c, effort, boat, 1, ref pose.Left);
            Oar(c, effort, boat, -1, ref pose.Right);
            var gripLeft = HandTarget(pose.Left.Grip, CrossWeight(pose.Left.SweepDeg), 1);
            var gripRight = HandTarget(pose.Right.Grip, CrossWeight(pose.Right.SweepDeg), -1);

            // 2 y 3. El tronco con la cadera del carro; si un brazo no llega, la cadera va hacia el puño.
            var hipZ = StrokeCycle.Lerp(fit.HipZCatch, fit.HipZFinish, legs);
            Trunk(rower, fit.HipY, hipZ, trunk, protraction, idle, ref pose);
            var reach = RiggingFit.CatchExtension * rower.ArmReach;
            var shift = Math.Max(Overreach(pose.Joints[(int)RowerJoint.LeftArm], gripLeft, reach),
                Overreach(pose.Joints[(int)RowerJoint.RightArm], gripRight, reach));
            var shifted = Math.Max(fit.HipZCatch - MaxHipBehindCatch, Math.Min(hipZ, hipZ - shift));
            shifted = Math.Min(shifted, fit.HipZFinish);
            if (shifted != hipZ)
            {
                hipZ = shifted;
                Trunk(rower, fit.HipY, hipZ, trunk, protraction, idle, ref pose);
            }

            pose.SeatPosition = new SceneVec3(0, boat.SeatTopY, hipZ);

            // 4. Las piernas.
            Leg(boat, rower, fit, 1, ref pose);
            Leg(boat, rower, fit, -1, ref pose);

            // 5. Los brazos.
            pose.LeftReachError = Arm(rower, gripLeft, pose.Left.FeatherDeg, 1, ref pose);
            pose.RightReachError = Arm(rower, gripRight, pose.Right.FeatherDeg, -1, ref pose);

            // 6. La cabeza.
            Head(rower, trunk, idle, ref pose);
        }

        // El puño de un remo (sin el cruce de las manos): side +1 = el remo de la mano izquierda (x > 0). withCross = false lo da
        // sin el cruce del remo (para RiggingFit).
        public static SceneVec3 GripTarget(double c, in BoatSpec boat, int side, bool withCross)
        {
            var oar = default(OarState);
            Oar(c, 0, boat, side, ref oar, withCross);
            return oar.Grip;
        }

        // El hombro (LeftArm / RightArm) con la cadera en (hipY; hipZ), sin respiración.
        public static SceneVec3 ShoulderAt(in RowerSpec rower, double hipY, double hipZ, double trunkDeg, double protractionDeg, int side)
        {
            var neck = NeckBase(rower, hipY, hipZ, trunkDeg);
            return ShoulderJoint(rower, neck, trunkDeg, protractionDeg, side, 0);
        }

        // w = 1 − smooth((|α| − 20°)/10°).
        public static double CrossWeight(double sweepDeg)
        {
            return 1 - StrokeCycle.Ramp(Math.Abs(sweepDeg), CrossStartDeg, CrossStartDeg + CrossSpanDeg);
        }

        // La dirección en el plano sagital a `deg` de la vertical, + hacia popa (−z).
        public static SceneVec3 Up(double deg)
        {
            var a = deg * Math.PI / 180;
            return new SceneVec3(0, Math.Cos(a), -Math.Sin(a));
        }

        // La perpendicular hacia adelante del remero (hacia popa).
        public static SceneVec3 Forward(double deg)
        {
            var a = deg * Math.PI / 180;
            return new SceneVec3(0, -Math.Sin(a), -Math.Cos(a));
        }

        private static void Oar(double c, double effort, in BoatSpec boat, int side, ref OarState oar, bool withCross = true)
        {
            var sweep = OarCurves.VisualSweepDeg(c);
            var a = sweep * Math.PI / 180;
            var bladeY = OarCurves.BladeCenterY(c);
            var w = withCross ? CrossWeight(sweep) : 0;
            var lift = side * CrossLiftOar * w;
            var stern = side > 0 ? -CrossSternOar * w : 0;
            var pin = new SceneVec3(side * boat.PinX, boat.PinY, 0);
            var grip = new SceneVec3(side * (boat.PinX - boat.GripFromPin * Math.Cos(a)), OarCurves.GripY(boat, bladeY) + lift,
                boat.GripFromPin * Math.Sin(a) + stern);
            oar.SweepDeg = sweep;
            oar.Grip = grip;
            oar.BladeCenter = pin + (pin - grip) * (boat.BladeFromPin / boat.GripFromPin);
            oar.PitchDeg = Math.Atan2(grip.Y - boat.PinY, boat.GripFromPin) * 180 / Math.PI;
            oar.FeatherDeg = OarCurves.FeatherDeg(c);
            oar.ShaftBendM = OarCurves.ShaftBendM(c, effort);
            oar.CurveBladeY = bladeY;
        }

        private static SceneVec3 HandTarget(in SceneVec3 grip, double crossWeight, int side)
        {
            return new SceneVec3(grip.X, grip.Y + side * CrossLiftHand * crossWeight, grip.Z - (side > 0 ? CrossSternHand * crossWeight : 0));
        }

        // Cuánto hay que correr el hombro hacia popa (−z) para que el puño quede al alcance (negativo: lo que sobra; sin alcance
        // posible de costado, nada que limitar).
        private static double Overreach(in SceneVec3 shoulder, in SceneVec3 grip, double reach)
        {
            var dx = grip.X - shoulder.X;
            var dy = grip.Y - shoulder.Y;
            var room = reach * reach - dx * dx - dy * dy;
            if (room <= 0)
            {
                return -1;
            }

            var maxZ = grip.Z + Math.Sqrt(room);
            return shoulder.Z - maxZ;
        }

        // La columna (ANIMATION_BIBLE §3.4): el giro Δ se reparte pelvis 60 % (+ 6° de base), Spine 15 %, Spine1 13 % y Spine2 12 %,
        // acumulado (el último tramo queda en Δ) y con la base que se deshace hacia arriba (6, 4, 2, 0°). Δ se calibra para que la
        // recta de las articulaciones de la cadera a la base del cuello quede EXACTAMENTE en el tronco pedido (tres pasos de punto fijo).
        public static double SpineTurnFor(in RowerSpec rower, double trunk)
        {
            var turn = trunk;
            for (var i = 0; i < 3; i++)
            {
                var neck = Chain(rower, SceneVec3.Zero, turn, out _, out _, out _);
                turn += trunk - Math.Atan2(-neck.Z, neck.Y) * 180 / Math.PI;
            }

            return turn;
        }

        // La pelvis (Hips → Spine) con el giro calibrado: lo que mide la cadera de la tabla.
        public static double PelvisDeg(double spineTurn)
        {
            return PelvisShare * spineTurn + PelvisBaseDeg;
        }

        private static SceneVec3 Chain(in RowerSpec rower, in SceneVec3 hip, double turn, out SceneVec3 spine, out SceneVec3 spine1,
                                       out SceneVec3 spine2)
        {
            spine = hip + Up(PelvisDeg(turn)) * rower.PelvisLength;
            spine1 = spine + Up(0.75 * turn + PelvisBaseDeg * 2 / 3) * rower.SpineLength;
            spine2 = spine1 + Up(0.88 * turn + PelvisBaseDeg / 3) * rower.Spine1Length;
            return spine2 + Up(turn) * rower.Spine2Length;
        }

        private static SceneVec3 NeckBase(in RowerSpec rower, double hipY, double hipZ, double trunk)
        {
            return Chain(rower, new SceneVec3(0, hipY, hipZ), SpineTurnFor(rower, trunk), out _, out _, out _);
        }

        private static SceneVec3 ShoulderJoint(in RowerSpec rower, in SceneVec3 neck, double trunk, double protraction, int side, double lift)
        {
            var up = Up(trunk);
            var forward = Forward(trunk);
            var root = neck - up * (rower.ShoulderDrop - lift) + new SceneVec3(side * rower.ClavicleHalfWidth, 0, 0);
            var w = rower.ShoulderHalfWidth - rower.ClavicleHalfWidth;
            var p = protraction * Math.PI / 180;
            return root + new SceneVec3(side * w * Math.Cos(p), 0, 0) + forward * (w * Math.Sin(p));
        }

        private static void Trunk(in RowerSpec rower, double hipY, double hipZ, double trunk, double protraction, in RowerIdleSample idle,
                                  ref ScullingPose pose)
        {
            var j = pose.Joints;
            var lat = pose.Lateral;
            var hip = new SceneVec3(0, hipY, hipZ);
            var turn = SpineTurnFor(rower, trunk);
            var pelvisUp = Up(PelvisDeg(turn));
            var up = Up(trunk);
            var forward = Forward(trunk);
            var chest = forward * idle.ChestOffsetM;
            var neck = Chain(rower, hip, turn, out var spine, out var spine1, out var spine2) + chest;
            j[(int)RowerJoint.Hips] = hip + pelvisUp * rower.HipsAboveJoints;
            j[(int)RowerJoint.Spine] = spine;
            j[(int)RowerJoint.Spine1] = spine1;
            j[(int)RowerJoint.Spine2] = spine2 + chest * 0.5;
            j[(int)RowerJoint.Neck] = neck;
            j[(int)RowerJoint.LeftUpLeg] = hip + new SceneVec3(rower.HipHalfWidth, 0, 0);
            j[(int)RowerJoint.RightUpLeg] = hip + new SceneVec3(-rower.HipHalfWidth, 0, 0);
            for (var side = 1; side >= -1; side -= 2)
            {
                var root = neck - up * (rower.ShoulderDrop - idle.ShoulderOffsetM) + new SceneVec3(side * rower.ClavicleHalfWidth, 0, 0);
                j[(int)(side > 0 ? RowerJoint.LeftShoulder : RowerJoint.RightShoulder)] = root;
                j[(int)(side > 0 ? RowerJoint.LeftArm : RowerJoint.RightArm)] = ShoulderJoint(rower, neck, trunk, protraction, side, idle.ShoulderOffsetM);
                lat[(int)(side > 0 ? RowerJoint.LeftShoulder : RowerJoint.RightShoulder)] = up;
            }

            lat[(int)RowerJoint.Hips] = SceneVec3.UnitX;
            lat[(int)RowerJoint.Spine] = SceneVec3.UnitX;
            lat[(int)RowerJoint.Spine1] = SceneVec3.UnitX;
            lat[(int)RowerJoint.Spine2] = SceneVec3.UnitX;
            lat[(int)RowerJoint.LeftUpLeg] = SceneVec3.UnitX;
            lat[(int)RowerJoint.RightUpLeg] = SceneVec3.UnitX;
        }

        private static void Leg(in BoatSpec boat, in RowerSpec rower, in RigFit fit, int side, ref ScullingPose pose)
        {
            var j = pose.Joints;
            var lat = pose.Lateral;
            var left = side > 0;
            var hip = j[(int)(left ? RowerJoint.LeftUpLeg : RowerJoint.RightUpLeg)];
            var x = side * rower.FootHalfSpread;
            var heel = new SceneVec3(x, fit.HeelY, fit.HeelZ);
            var ankle = new SceneVec3(x, fit.AnkleY, fit.AnkleZ);
            var hint = SceneVec3.Lerp(hip, ankle, 0.5) + new SceneVec3(side * KneeHintOut, KneeHintUp, 0);
            var knee = TwoBoneIk.Solve(hip, ankle, rower.Thigh, rower.Shin, hint);
            var sole = RiggingFit.SoleDirection(boat);
            var normal = RiggingFit.SoleNormal(boat);
            j[(int)(left ? RowerJoint.LeftLeg : RowerJoint.RightLeg)] = knee;
            j[(int)(left ? RowerJoint.LeftFoot : RowerJoint.RightFoot)] = ankle;
            j[(int)(left ? RowerJoint.LeftToeBase : RowerJoint.RightToeBase)] = heel + sole * (0.72 * rower.FootLength) + normal * 0.02;
            j[(int)(left ? RowerJoint.LeftToeEnd : RowerJoint.RightToeEnd)] = heel + sole * rower.FootLength + normal * 0.01;
            lat[(int)(left ? RowerJoint.LeftToeEnd : RowerJoint.RightToeEnd)] = SceneVec3.UnitX;
            var bend = Perpendicular(knee - hip, hint - hip);
            var normalThigh = SceneVec3.Cross(knee - hip, bend).Normalized(SceneVec3.UnitX);
            lat[(int)(left ? RowerJoint.LeftUpLeg : RowerJoint.RightUpLeg)] = normalThigh;
            lat[(int)(left ? RowerJoint.LeftLeg : RowerJoint.RightLeg)] = normalThigh;
            lat[(int)(left ? RowerJoint.LeftFoot : RowerJoint.RightFoot)] = SceneVec3.UnitX;
            lat[(int)(left ? RowerJoint.LeftToeBase : RowerJoint.RightToeBase)] = SceneVec3.UnitX;
        }

        // Devuelve lo que le faltó a la mano para llegar al puño (m).
        private static double Arm(in RowerSpec rower, in SceneVec3 grip, double featherDeg, int side, ref ScullingPose pose)
        {
            var j = pose.Joints;
            var lat = pose.Lateral;
            var left = side > 0;
            var shoulder = j[(int)(left ? RowerJoint.LeftArm : RowerJoint.RightArm)];
            var hint = shoulder + new SceneVec3(side * ElbowHintOut, -ElbowHintDown, ElbowHintBow);
            var lower = rower.Forearm + rower.HandToGrip;
            var elbow = TwoBoneIk.Solve(shoulder, grip, rower.UpperArm, lower, hint);
            var toGrip = (grip - elbow).Normalized(SceneVec3.UnitZ);
            var hand = elbow + toGrip * rower.Forearm;
            var gripPoint = elbow + toGrip * lower;
            j[(int)(left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm)] = elbow;
            j[(int)(left ? RowerJoint.LeftHand : RowerJoint.RightHand)] = hand;
            j[(int)(left ? RowerJoint.LeftGrip : RowerJoint.RightGrip)] = gripPoint;

            // El eje lateral del brazo: la normal del plano del codo (continua: el doblez sale de la guía).
            var bend = Perpendicular(grip - shoulder, hint - shoulder);
            var normal = SceneVec3.Cross(elbow - shoulder, bend).Normalized(SceneVec3.UnitX);
            lat[(int)(left ? RowerJoint.LeftArm : RowerJoint.RightArm)] = normal;
            lat[(int)(left ? RowerJoint.LeftForeArm : RowerJoint.RightForeArm)] = normal;
            lat[(int)(left ? RowerJoint.LeftHand : RowerJoint.RightHand)] = normal.RotateAround(toGrip, side * HandFeatherShare * featherDeg);
            lat[(int)(left ? RowerJoint.LeftGrip : RowerJoint.RightGrip)] = lat[(int)(left ? RowerJoint.LeftHand : RowerJoint.RightHand)];
            return SceneVec3.Distance(gripPoint, grip);
        }

        private static void Head(in RowerSpec rower, double trunk, in RowerIdleSample idle, ref ScullingPose pose)
        {
            var j = pose.Joints;
            var lat = pose.Lateral;
            var up = Up(trunk);
            var neck = j[(int)RowerJoint.Neck];
            var neckDir = (up + SceneVec3.UnitY).Normalized(SceneVec3.UnitY);
            var head = neck + neckDir * rower.NeckLength;
            var headUp = SceneVec3.Lerp(up, SceneVec3.UnitY, HeadGazeWeight).Normalized(SceneVec3.UnitY);
            j[(int)RowerJoint.Head] = head;
            j[(int)RowerJoint.HeadTop] = head + headUp * rower.HeadTopAbove;
            var eye = head + headUp * (0.45 * rower.HeadTopAbove);
            pose.GazeTarget = new SceneVec3(0, eye.Y, -GazeDistance);
            var yawed = SceneVec3.UnitX.RotateAround(SceneVec3.UnitY, idle.GazeYawDeg);
            lat[(int)RowerJoint.Neck] = SceneVec3.UnitX;
            lat[(int)RowerJoint.Head] = yawed;
            lat[(int)RowerJoint.HeadTop] = yawed;
        }

        // La parte de v perpendicular a la recta de dir (unitaria; si no hay, +x).
        private static SceneVec3 Perpendicular(in SceneVec3 dir, in SceneVec3 v)
        {
            var d = dir.Normalized(SceneVec3.UnitY);
            return (v - d * SceneVec3.Dot(v, d)).Normalized(SceneVec3.UnitX);
        }
    }
}
