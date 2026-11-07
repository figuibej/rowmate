using System;

namespace RowMate.Core.Scene.Stroke
{
    // Un vector 3D en double, mínimo y sin asignar (It. 7, 02-tech-design §1.1): la pose del remero, los remos y los efectos. Mismo
    // marco que la escena salvo que se diga otra cosa (x lateral, y arriba, z adelante).
    public readonly struct SceneVec3 : IEquatable<SceneVec3>
    {
        public static readonly SceneVec3 Zero = new SceneVec3(0, 0, 0);
        public static readonly SceneVec3 UnitX = new SceneVec3(1, 0, 0);
        public static readonly SceneVec3 UnitY = new SceneVec3(0, 1, 0);
        public static readonly SceneVec3 UnitZ = new SceneVec3(0, 0, 1);

        public SceneVec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public static SceneVec3 operator +(in SceneVec3 a, in SceneVec3 b) => new SceneVec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static SceneVec3 operator -(in SceneVec3 a, in SceneVec3 b) => new SceneVec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static SceneVec3 operator -(in SceneVec3 a) => new SceneVec3(-a.X, -a.Y, -a.Z);

        public static SceneVec3 operator *(in SceneVec3 a, double k) => new SceneVec3(a.X * k, a.Y * k, a.Z * k);

        public static SceneVec3 operator *(double k, in SceneVec3 a) => new SceneVec3(a.X * k, a.Y * k, a.Z * k);

        public static double Dot(in SceneVec3 a, in SceneVec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static SceneVec3 Cross(in SceneVec3 a, in SceneVec3 b)
        {
            return new SceneVec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }

        public static double Distance(in SceneVec3 a, in SceneVec3 b) => (a - b).Length;

        public static SceneVec3 Lerp(in SceneVec3 a, in SceneVec3 b, double t) => new SceneVec3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

        // Unitario; el vector nulo da el respaldo.
        public SceneVec3 Normalized(in SceneVec3 fallback)
        {
            var len = Length;
            return len > 1e-12 ? new SceneVec3(X / len, Y / len, Z / len) : fallback;
        }

        // El ángulo entre dos vectores, en grados (0 con alguno nulo).
        public static double AngleDeg(in SceneVec3 a, in SceneVec3 b)
        {
            var la = a.Length;
            var lb = b.Length;
            if (la < 1e-12 || lb < 1e-12)
            {
                return 0;
            }

            var c = Dot(a, b) / (la * lb);
            c = c < -1 ? -1 : (c > 1 ? 1 : c);
            return Math.Acos(c) * 180 / Math.PI;
        }

        // Rota con un cuaternión unitario (v' = q v q*).
        public SceneVec3 Rotate(in SceneQuaternion q)
        {
            var ux = q.X;
            var uy = q.Y;
            var uz = q.Z;
            var s = q.W;
            var dotUv = ux * X + uy * Y + uz * Z;
            var dotUu = ux * ux + uy * uy + uz * uz;
            var cx = uy * Z - uz * Y;
            var cy = uz * X - ux * Z;
            var cz = ux * Y - uy * X;
            return new SceneVec3(
                2 * dotUv * ux + (s * s - dotUu) * X + 2 * s * cx,
                2 * dotUv * uy + (s * s - dotUu) * Y + 2 * s * cy,
                2 * dotUv * uz + (s * s - dotUu) * Z + 2 * s * cz);
        }

        // Rota alrededor de un eje unitario por la regla de Rodrigues (grados, mano derecha).
        public SceneVec3 RotateAround(in SceneVec3 axis, double degrees)
        {
            var a = degrees * Math.PI / 180;
            var c = Math.Cos(a);
            var s = Math.Sin(a);
            var k = axis;
            var cross = Cross(k, this);
            var dot = Dot(k, this);
            return this * c + cross * s + k * (dot * (1 - c));
        }

        public bool Equals(SceneVec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object? obj) => obj is SceneVec3 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString()
        {
            return "(" + X.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "; "
                + Y.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + "; "
                + Z.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + ")";
        }
    }
}
