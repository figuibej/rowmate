using System;

namespace RowMate.Core.Scene
{
    // Ruido determinista y puro de la escena (lo agregado al legacy: la orilla que serpentea, el talud, la nieve, el minuto de
    // cada luz). Nunca consume el generador del legacy (AC-SCN-15-07): el paisaje del legacy es el mismo con y sin esto. Un hash
    // entero (SplitMix64) a [0; 1) y un ruido de valor 1D y 2D con interpolación suave, en double.
    public static class SceneHash
    {
        public static ulong Mix(ulong x)
        {
            unchecked
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        public static ulong Combine(long a, long b)
        {
            unchecked
            {
                return Mix(Mix((ulong)a) ^ (ulong)b * 0xD6E8FEB86659FD93UL);
            }
        }

        public static ulong Combine(long a, long b, long c)
        {
            unchecked
            {
                return Mix(Combine(a, b) ^ (ulong)c * 0xA0761D6478BD642FUL);
            }
        }

        // [0; 1) con 53 bits.
        public static double Unit(ulong h)
        {
            return (h >> 11) * (1.0 / 9007199254740992.0);
        }

        public static double Unit(long a, long b)
        {
            return Unit(Combine(a, b));
        }

        public static double Unit(long a, long b, long c)
        {
            return Unit(Combine(a, b, c));
        }

        // Ruido de valor 1D en [0; 1], continuo y suave (C¹), con un canal (salt) por uso.
        public static double Value1(long salt, double x)
        {
            var i = Math.Floor(x);
            var f = x - i;
            var k = (long)i;
            var a = Unit(salt, k);
            var b = Unit(salt, k + 1);
            return a + (b - a) * SceneMath.Smooth(f);
        }

        // Ruido de valor 2D en [0; 1].
        public static double Value2(long salt, double x, double y)
        {
            var ix = Math.Floor(x);
            var iy = Math.Floor(y);
            var fx = SceneMath.Smooth(x - ix);
            var fy = SceneMath.Smooth(y - iy);
            var kx = (long)ix;
            var ky = (long)iy;
            var a = Unit(salt, kx, ky);
            var b = Unit(salt, kx + 1, ky);
            var c = Unit(salt, kx, ky + 1);
            var d = Unit(salt, kx + 1, ky + 1);
            var top = a + (b - a) * fx;
            var bottom = c + (d - c) * fx;
            return top + (bottom - top) * fy;
        }
    }
}
