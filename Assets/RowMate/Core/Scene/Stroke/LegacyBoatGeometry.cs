using System;

namespace RowMate.Core.Scene.Stroke
{
    // La geometría 2,5D exagerada del legacy (scene/painters/boat_painter.dart:25-56): carro, mango y punta de la pala en el mundo
    // de la cámara. Contrato solo para LT-96 y la lista de charcos del contrato; lo que se ve usa las medidas reales (BoatSpec,
    // ScullingKinematics; DV-68, DV-77). Mismas operaciones y en el mismo orden que el legacy.
    public static class LegacyBoatGeometry
    {
        public const double HullY = 0.08;                          // :25
        public const double SternZ = SceneCamera.BoatZ - 4.2;      // :26
        public const double BowZ = SceneCamera.BoatZ + 4.0;        // :27
        public const double RiggerX = SceneState.RiggerX;          // :28
        public const double OarLength = SceneState.OarLength;      // :29
        public const double Inboard = 0.9;                         // :30
        public const double FeetZ = SceneCamera.BoatZ - 1.15;      // :31
        public const double HipY = 0.25;                           // :32

        // :35: z del carro; en el catch (piernas = 0) va hacia la popa, cerca de los pies.
        public static double SeatZ(double p)
        {
            return SceneCamera.BoatZ - 0.6 * (1 - StrokeCycle.Legs(p));
        }

        // :38-45: el mango del remo (donde van las manos).
        public static SceneVec3 Handle(double side, double p)
        {
            var sweep = StrokeCycle.OarSweep(p);
            return new SceneVec3(side * (RiggerX - Math.Cos(sweep) * Inboard), 0.45, SceneCamera.BoatZ + Math.Sin(sweep) * Inboard);
        }

        // :48-56: la punta de la pala.
        public static SceneVec3 BladeTip(double side, double p)
        {
            var sweep = StrokeCycle.OarSweep(p);
            var inWater = StrokeCycle.BladeInWater(p);
            return new SceneVec3(side * (RiggerX + Math.Cos(sweep) * OarLength), inWater ? -0.05 : 0.35, SceneCamera.BoatZ - Math.Sin(sweep) * OarLength);
        }
    }
}
