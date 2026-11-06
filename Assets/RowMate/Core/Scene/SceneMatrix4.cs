using System;

namespace RowMate.Core.Scene
{
    // Una matriz 4 × 4 en double, por filas (Mrc = fila r, columna c), para vectores columna (v' = M·v), la convención de
    // Matrix4x4 de Unity (06a-escena-lago/02-tech-design §2.1). Struct de 16 campos: sin asignaciones.
    public readonly struct SceneMatrix4
    {
        public static readonly SceneMatrix4 Identity = new SceneMatrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public SceneMatrix4(double m00, double m01, double m02, double m03,
                            double m10, double m11, double m12, double m13,
                            double m20, double m21, double m22, double m23,
                            double m30, double m31, double m32, double m33)
        {
            M00 = m00; M01 = m01; M02 = m02; M03 = m03;
            M10 = m10; M11 = m11; M12 = m12; M13 = m13;
            M20 = m20; M21 = m21; M22 = m22; M23 = m23;
            M30 = m30; M31 = m31; M32 = m32; M33 = m33;
        }

        public double M00 { get; }
        public double M01 { get; }
        public double M02 { get; }
        public double M03 { get; }
        public double M10 { get; }
        public double M11 { get; }
        public double M12 { get; }
        public double M13 { get; }
        public double M20 { get; }
        public double M21 { get; }
        public double M22 { get; }
        public double M23 { get; }
        public double M30 { get; }
        public double M31 { get; }
        public double M32 { get; }
        public double M33 { get; }

        // El elemento (fila, columna).
        public double this[int row, int column]
        {
            get
            {
                switch (row * 4 + column)
                {
                    case 0: return M00;
                    case 1: return M01;
                    case 2: return M02;
                    case 3: return M03;
                    case 4: return M10;
                    case 5: return M11;
                    case 6: return M12;
                    case 7: return M13;
                    case 8: return M20;
                    case 9: return M21;
                    case 10: return M22;
                    case 11: return M23;
                    case 12: return M30;
                    case 13: return M31;
                    case 14: return M32;
                    case 15: return M33;
                    default: throw new ArgumentOutOfRangeException(nameof(row));
                }
            }
        }

        public static SceneMatrix4 Multiply(in SceneMatrix4 a, in SceneMatrix4 b)
        {
            return new SceneMatrix4(
                a.M00 * b.M00 + a.M01 * b.M10 + a.M02 * b.M20 + a.M03 * b.M30,
                a.M00 * b.M01 + a.M01 * b.M11 + a.M02 * b.M21 + a.M03 * b.M31,
                a.M00 * b.M02 + a.M01 * b.M12 + a.M02 * b.M22 + a.M03 * b.M32,
                a.M00 * b.M03 + a.M01 * b.M13 + a.M02 * b.M23 + a.M03 * b.M33,
                a.M10 * b.M00 + a.M11 * b.M10 + a.M12 * b.M20 + a.M13 * b.M30,
                a.M10 * b.M01 + a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
                a.M10 * b.M02 + a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
                a.M10 * b.M03 + a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,
                a.M20 * b.M00 + a.M21 * b.M10 + a.M22 * b.M20 + a.M23 * b.M30,
                a.M20 * b.M01 + a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
                a.M20 * b.M02 + a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
                a.M20 * b.M03 + a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,
                a.M30 * b.M00 + a.M31 * b.M10 + a.M32 * b.M20 + a.M33 * b.M30,
                a.M30 * b.M01 + a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
                a.M30 * b.M02 + a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
                a.M30 * b.M03 + a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);
        }

        // M·(x, y, z, w).
        public void Transform(double x, double y, double z, double w, out double ox, out double oy, out double oz, out double ow)
        {
            ox = M00 * x + M01 * y + M02 * z + M03 * w;
            oy = M10 * x + M11 * y + M12 * z + M13 * w;
            oz = M20 * x + M21 * y + M22 * z + M23 * w;
            ow = M30 * x + M31 * y + M32 * z + M33 * w;
        }

        // M·(x, y, z, 1) con la división por w (MultiplyPoint de Unity). false si w ≈ 0.
        public bool TryMultiplyPoint(double x, double y, double z, out double ox, out double oy, out double oz)
        {
            Transform(x, y, z, 1, out ox, out oy, out oz, out var ow);
            if (Math.Abs(ow) < 1e-300)
            {
                return false;
            }

            ox /= ow;
            oy /= ow;
            oz /= ow;
            return true;
        }

        public SceneMatrix4 Transpose()
        {
            return new SceneMatrix4(M00, M10, M20, M30, M01, M11, M21, M31, M02, M12, M22, M32, M03, M13, M23, M33);
        }

        // La inversa por cofactores. InvalidOperationException si es singular.
        public SceneMatrix4 Inverse()
        {
            var b00 = M00 * M11 - M01 * M10;
            var b01 = M00 * M12 - M02 * M10;
            var b02 = M00 * M13 - M03 * M10;
            var b03 = M01 * M12 - M02 * M11;
            var b04 = M01 * M13 - M03 * M11;
            var b05 = M02 * M13 - M03 * M12;
            var b06 = M20 * M31 - M21 * M30;
            var b07 = M20 * M32 - M22 * M30;
            var b08 = M20 * M33 - M23 * M30;
            var b09 = M21 * M32 - M22 * M31;
            var b10 = M21 * M33 - M23 * M31;
            var b11 = M22 * M33 - M23 * M32;
            var det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
            if (det == 0 || double.IsNaN(det))
            {
                throw new InvalidOperationException("SceneMatrix4: matriz singular.");
            }

            var inv = 1.0 / det;
            return new SceneMatrix4(
                (M11 * b11 - M12 * b10 + M13 * b09) * inv,
                (-M01 * b11 + M02 * b10 - M03 * b09) * inv,
                (M31 * b05 - M32 * b04 + M33 * b03) * inv,
                (-M21 * b05 + M22 * b04 - M23 * b03) * inv,
                (-M10 * b11 + M12 * b08 - M13 * b07) * inv,
                (M00 * b11 - M02 * b08 + M03 * b07) * inv,
                (-M30 * b05 + M32 * b02 - M33 * b01) * inv,
                (M20 * b05 - M22 * b02 + M23 * b01) * inv,
                (M10 * b10 - M11 * b08 + M13 * b06) * inv,
                (-M00 * b10 + M01 * b08 - M03 * b06) * inv,
                (M30 * b04 - M31 * b02 + M33 * b00) * inv,
                (-M20 * b04 + M21 * b02 - M23 * b00) * inv,
                (-M10 * b09 + M11 * b07 - M12 * b06) * inv,
                (M00 * b09 - M01 * b07 + M02 * b06) * inv,
                (-M30 * b03 + M31 * b01 - M32 * b00) * inv,
                (M20 * b03 - M21 * b01 + M22 * b00) * inv);
        }
    }
}
