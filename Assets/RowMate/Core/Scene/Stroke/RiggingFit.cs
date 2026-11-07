using System;

namespace RowMate.Core.Scene.Stroke
{
    // El ajuste del bote al cuerpo del remero (ANIMATION_BIBLE §7.4; DA7-01; DT7-02), en el marco del bote.
    public readonly struct RigFit
    {
        public RigFit(double heelY, double heelZ, double ankleY, double ankleZ, double hipY, double hipZCatch, double hipZFinish,
                      double armExtensionCatch, double gripToRibsFinish)
        {
            HeelY = heelY;
            HeelZ = heelZ;
            AnkleY = ankleY;
            AnkleZ = ankleZ;
            HipY = hipY;
            HipZCatch = hipZCatch;
            HipZFinish = hipZFinish;
            ArmExtensionCatch = armExtensionCatch;
            GripToRibsFinish = gripToRibsFinish;
        }

        // Los talones en la pedalera (y, z) y los tobillos (Foot de Mixamo).
        public double HeelY { get; }
        public double HeelZ { get; }
        public double AnkleY { get; }
        public double AnkleZ { get; }

        // Las articulaciones de la cadera: su altura y su z en el catch y en el finish (el recorrido del carro).
        public double HipY { get; }
        public double HipZCatch { get; }
        public double HipZFinish { get; }

        public double Travel => HipZFinish - HipZCatch;

        // |hombro → puño| / largo del brazo en el catch (≥ 0,97).
        public double ArmExtensionCatch { get; }

        // La altura del puño menos la de las costillas bajas en el finish (m; ± 0,03).
        public double GripToRibsFinish { get; }
    }

    // RiggingFit (Core, testeable): con las medidas del bote y los largos del rig, la pedalera y el recorrido del carro para que en el
    // catch las canillas queden verticales y los brazos casi estirados (99,7 %: con el 98,5 % del arte el codo queda en 160°, fuera
    // de los 178 ± 8 de la tabla), y en el finish las piernas a 175° (178 ± 8 de la tabla: a 178° la rodilla casi recta salta 3 cm
    // por cada 3 mm de cadera al soltar el brazo). Cerrado (sin iterar ni asignar). Con el maniquí de 1,85 m:
    // talones ≈ −0,22, cadera ≈ +0,16 / +0,70 (recorrido ≈ 0,54); los 1,15 m y 0,6 m del legacy son imposibles con remos reales.
    public static class RiggingFit
    {
        public const double CatchExtension = 0.997;
        public const double FinishKneeDeg = 175;

        // La planta del pie sobre la placa a 42°: hacia los dedos (popa y arriba) y la normal hacia el remero.
        public static SceneVec3 SoleDirection(in BoatSpec boat)
        {
            var a = boat.StretcherAngleDeg * Math.PI / 180;
            return new SceneVec3(0, Math.Sin(a), -Math.Cos(a));
        }

        public static SceneVec3 SoleNormal(in BoatSpec boat)
        {
            var a = boat.StretcherAngleDeg * Math.PI / 180;
            return new SceneVec3(0, Math.Cos(a), Math.Sin(a));
        }

        // Del talón al tobillo: un quinto del pie sobre la planta y la altura del tobillo sobre la normal.
        public static SceneVec3 AnkleFromHeel(in BoatSpec boat, in RowerSpec rower)
        {
            return SoleDirection(boat) * (0.2 * rower.FootLength) + SoleNormal(boat) * rower.AnkleHeight;
        }

        public static RigFit Solve(in BoatSpec boat, in RowerSpec rower)
        {
            var hipY = boat.SeatTopY + rower.HipAboveSeat;
            var heelY = boat.SeatTopY - boat.HeelBelowSeat;
            var ankleOffset = AnkleFromHeel(boat, rower);
            var ankleY = heelY + ankleOffset.Y;

            // El catch: la cadera más a proa con la que cada mano llega al puño con el brazo al 99,7 %.
            var reach = CatchExtension * rower.ArmReach;
            var hipZCatch = double.PositiveInfinity;
            for (var side = 1; side >= -1; side -= 2)
            {
                var grip = ScullingKinematics.GripTarget(0, boat, side, true);
                var shoulder = ScullingKinematics.ShoulderAt(rower, hipY, 0, ScullingKinematics.TrunkCatchDeg, ScullingKinematics.ProtractionCatchDeg, side);
                var dx = grip.X - shoulder.X;
                var dy = grip.Y - shoulder.Y;
                var room = reach * reach - dx * dx - dy * dy;
                var z = room > 0 ? grip.Z + Math.Sqrt(room) - shoulder.Z : grip.Z - shoulder.Z;
                hipZCatch = Math.Min(hipZCatch, z);
            }

            // Canillas verticales en el catch: la rodilla sobre el tobillo.
            var spread = rower.FootHalfSpread - rower.HipHalfWidth;
            var rise = hipY - ankleY - rower.Shin;
            var ankleZ = hipZCatch - Math.Sqrt(Math.Max(0, rower.Thigh * rower.Thigh - rise * rise - spread * spread));
            var heelZ = ankleZ - ankleOffset.Z;

            // El finish: las piernas a 175°.
            var k = FinishKneeDeg * Math.PI / 180;
            var legs2 = rower.Thigh * rower.Thigh + rower.Shin * rower.Shin - 2 * rower.Thigh * rower.Shin * Math.Cos(k);
            var drop = hipY - ankleY;
            var hipZFinish = ankleZ + Math.Sqrt(Math.Max(0, legs2 - drop * drop - spread * spread));

            // El puño contra las costillas bajas (la mitad de Spine1) en el finish, con el tronco a −18°.
            var gripFinish = ScullingKinematics.GripTarget(0.36, boat, 1, false);
            var ribs = hipY + (rower.PelvisLength + rower.SpineLength + 0.5 * rower.Spine1Length) * Math.Cos(ScullingKinematics.TrunkFinishDeg * Math.PI / 180);
            return new RigFit(heelY, heelZ, ankleY, ankleZ, hipY, hipZCatch, hipZFinish, CatchExtension, gripFinish.Y - ribs);
        }
    }
}
