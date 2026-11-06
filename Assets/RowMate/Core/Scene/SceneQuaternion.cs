using System;

namespace RowMate.Core.Scene
{
    // Un cuaternión en double para el slerp de la orientación del vuelo (06a-escena-lago/02-tech-design §2.2), con la misma
    // convención de ángulos que ChaseCameraProjection.Rotation (Unity: Quaternion.Euler(−cabeceo; guiñada; rolido)).
    public readonly struct SceneQuaternion
    {
        public static readonly SceneQuaternion Identity = new SceneQuaternion(0, 0, 0, 1);

        public SceneQuaternion(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public double X { get; }

        public double Y { get; }

        public double Z { get; }

        public double W { get; }

        // q = qy(guiñada)·qx(−cabeceo)·qz(rolido).
        public static SceneQuaternion FromAngles(double yawDeg, double pitchDeg, double rollDeg)
        {
            var qy = Axis(0, 1, 0, yawDeg);
            var qx = Axis(1, 0, 0, -pitchDeg);
            var qz = Axis(0, 0, 1, rollDeg);
            return Multiply(Multiply(qy, qx), qz);
        }

        public static SceneQuaternion Axis(double ax, double ay, double az, double degrees)
        {
            var half = degrees * Math.PI / 360;
            var s = Math.Sin(half);
            return new SceneQuaternion(ax * s, ay * s, az * s, Math.Cos(half));
        }

        public static SceneQuaternion Multiply(in SceneQuaternion a, in SceneQuaternion b)
        {
            return new SceneQuaternion(
                a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
                a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
                a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);
        }

        // El camino más corto, a velocidad angular constante; t = 0 da a y t = 1 da b.
        public static SceneQuaternion Slerp(in SceneQuaternion a, in SceneQuaternion b, double t)
        {
            var dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
            var bx = b.X;
            var by = b.Y;
            var bz = b.Z;
            var bw = b.W;
            if (dot < 0)
            {
                dot = -dot;
                bx = -bx;
                by = -by;
                bz = -bz;
                bw = -bw;
            }

            double wa;
            double wb;
            if (dot > 0.9999995)
            {
                wa = 1 - t;
                wb = t;
            }
            else
            {
                var theta = Math.Acos(dot);
                var sin = Math.Sin(theta);
                wa = Math.Sin((1 - t) * theta) / sin;
                wb = Math.Sin(t * theta) / sin;
            }

            var q = new SceneQuaternion(wa * a.X + wb * bx, wa * a.Y + wb * by, wa * a.Z + wb * bz, wa * a.W + wb * bw);
            var n = Math.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
            return new SceneQuaternion(q.X / n, q.Y / n, q.Z / n, q.W / n);
        }

        // La matriz de rotación (3 × 3 en una 4 × 4).
        public SceneMatrix4 ToMatrix()
        {
            double xx = X * X, yy = Y * Y, zz = Z * Z, xy = X * Y, xz = X * Z, yz = Y * Z, wx = W * X, wy = W * Y, wz = W * Z;
            return new SceneMatrix4(
                1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy), 0,
                2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx), 0,
                2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy), 0,
                0, 0, 0, 1);
        }

        public void ToAngles(out double yawDeg, out double pitchDeg, out double rollDeg)
        {
            ChaseCameraProjection.Angles(ToMatrix(), out yawDeg, out pitchDeg, out rollDeg);
        }
    }
}
