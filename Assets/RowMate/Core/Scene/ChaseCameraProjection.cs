using System;

namespace RowMate.Core.Scene
{
    // La proyección que reproduce SceneCamera.TryProject (06a-escena-lago/02-tech-design §2.1; F-SCN-02; S6a-6): Unity la usa
    // tal cual en Camera.projectionMatrix, así el signo del lensShift y el gateFit dejan de ser un riesgo. Convención de Unity: la
    // cámara mira a −z de vista. Con ancho W, alto H, focal f y horizonte h (dp):
    //   P = [ 2f/W, 0,    0,              0             ]
    //       [ 0,    2f/H, 2h/H − 1,       0             ]
    //       [ 0,    0,    −(F + N)/(F − N), −2FN/(F − N) ]
    //       [ 0,    0,    −1,             0             ]
    // Con h = 0,40·H el término 2h/H − 1 vale −0,20 (el lensShift.y = −0,10 de ART_BIBLE §8.1); con 0,42, −0,16.
    public static class ChaseCameraProjection
    {
        public const double NearPlane = SceneCamera.MinZ;   // AC-SCN-02-03, LT-64
        public const double FarPlane = 2000;                // §2.1: el terreno termina en 1 500 m (DT6a-18)

        public static SceneMatrix4 Contract(in SceneCamera cam, double near = NearPlane, double far = FarPlane)
        {
            return Blend(cam, cam.Focal, cam.HorizonY, near, far);
        }

        // La del vuelo (focal y horizonte interpolados) o la de cualquier pose: Blend(cam, pose.FocalDp, pose.HorizonDp, …).
        public static SceneMatrix4 Blend(in SceneCamera cam, double focalDp, double horizonDp, double near = NearPlane, double far = FarPlane)
        {
            var w = cam.Width;
            var h = cam.Height;
            return new SceneMatrix4(
                2 * focalDp / w, 0, 0, 0,
                0, 2 * focalDp / h, 2 * horizonDp / h - 1, 0,
                0, 0, -(far + near) / (far - near), -2 * far * near / (far - near),
                0, 0, -1, 0);
        }

        // La rotación de la cámara (columnas = sus ejes en el mundo): Ry(guiñada)·Rx(−cabeceo)·Rz(rolido), las matrices de mano
        // izquierda de Unity (= Quaternion.Euler(−cabeceo; guiñada; rolido)).
        public static SceneMatrix4 Rotation(double yawDeg, double pitchDeg, double rollDeg)
        {
            var b = yawDeg * Math.PI / 180;
            var a = -pitchDeg * Math.PI / 180;
            var c = rollDeg * Math.PI / 180;
            double ca = Math.Cos(a), sa = Math.Sin(a), cb = Math.Cos(b), sb = Math.Sin(b), cc = Math.Cos(c), sc = Math.Sin(c);
            return new SceneMatrix4(
                cb * cc + sb * sa * sc, -cb * sc + sb * sa * cc, sb * ca, 0,
                ca * sc, ca * cc, -sa, 0,
                -sb * cc + cb * sa * sc, sb * sc + cb * sa * cc, cb * ca, 0,
                0, 0, 0, 1);
        }

        // Los ángulos (grados) de una rotación armada como Rotation: la inversa exacta de la convención.
        public static void Angles(in SceneMatrix4 rotation, out double yawDeg, out double pitchDeg, out double rollDeg)
        {
            var sa = -rotation.M12;
            sa = sa > 1 ? 1 : (sa < -1 ? -1 : sa);
            var a = Math.Asin(sa);
            var b = Math.Atan2(rotation.M02, rotation.M22);
            var c = Math.Atan2(rotation.M10, rotation.M11);
            yawDeg = b * 180 / Math.PI;
            pitchDeg = -a * 180 / Math.PI;
            rollDeg = c * 180 / Math.PI;
        }

        // La de vista (worldToCameraMatrix de Unity): escala (1; 1; −1)·Rᵀ·T(−posición).
        public static SceneMatrix4 View(in CameraPose pose)
        {
            var r = Rotation(pose.YawDeg, pose.PitchDeg, pose.RollDeg);
            var tx = -(r.M00 * pose.X + r.M10 * pose.Y + r.M20 * pose.Z);
            var ty = -(r.M01 * pose.X + r.M11 * pose.Y + r.M21 * pose.Z);
            var tz = -(r.M02 * pose.X + r.M12 * pose.Y + r.M22 * pose.Z);
            return new SceneMatrix4(
                r.M00, r.M10, r.M20, tx,
                r.M01, r.M11, r.M21, ty,
                -r.M02, -r.M12, -r.M22, -tz,
                0, 0, 0, 1);
        }

        // Lo que daría Unity con esa proyección y esa pose: el píxel en dp con el origen arriba (para las pruebas de dotnet y para
        // comparar con la cámara real, WorldToViewportPoint × el tamaño de la RT). false si el punto queda detrás del plano cercano.
        public static bool TryProjectWithPose(in SceneMatrix4 projection, in CameraPose pose, in SceneCamera cam,
                                              double x, double y, double z, out double sx, out double sy)
        {
            var view = View(pose);
            view.Transform(x, y, z, 1, out var vx, out var vy, out var vz, out _);
            projection.Transform(vx, vy, vz, 1, out var cx, out var cy, out _, out var cw);
            if (-vz <= NearPlane || cw <= 0)
            {
                sx = 0;
                sy = 0;
                return false;
            }

            var nx = cx / cw;
            var ny = cy / cw;
            sx = (nx + 1) / 2 * cam.Width;
            sy = (1 - ny) / 2 * cam.Height;
            return true;
        }
    }
}
