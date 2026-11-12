using System;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Core.Scene.Water
{
    // El giro del par de remolinos (#12; AC-6B-17; 00-arte-agua §6.3): el eje de la pala en el finish, proyectado al agua (x; z), en
    // grados respecto de x (0-90; ≈ 40°).
    public static class SwirlOrientation
    {
        public static double AngleDeg(in ScullingPose finishPose)
        {
            var oar = finishPose.Right;
            var dx = oar.BladeCenter.X - oar.Grip.X;
            var dz = oar.BladeCenter.Z - oar.Grip.Z;
            if (Math.Abs(dx) < 1e-12 && Math.Abs(dz) < 1e-12)
            {
                return 0;
            }

            return SceneMath.RadToDeg(Math.Atan2(Math.Abs(dz), Math.Abs(dx)));
        }
    }
}
