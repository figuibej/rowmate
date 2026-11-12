using System;
using RowMate.Core.Text;

namespace RowMate.Core.Scene.Geometry
{
    // El mapa de normales procedural del agua avanzada (PA6b-04; DT6b-17, DT6b-18; slot water.normalDetail): RGBA8 lineal, sin costura,
    // con dos bandas en una textura: RG = el chop (B2: 12 senos de λ entre 1/8 y 1/2 del tile) y BA = el rizo (B3: 16 senos de λ entre
    // 1/32 y 1/8). Cada canal guarda la pendiente −∂h/∂x y −∂h/∂y codificada en 0,5 ± 0,5 (el shader arma la normal). Los vectores de
    // onda son enteros (m; n) en ciclos por tile, así no hay costura; la amplitud sigue un espectro de Phillips recortado (∝ 1/|k|²,
    // con más peso hacia el viento). TODO en enteros (tabla de senos en punto fijo): los bytes salen iguales en .NET 8 y en el Mono de
    // Unity (la huella de water.json la prueba dotnet). Lo hornea el Editor (WP-B) con la semilla fija.
    public static class WaterNormalMap
    {
        public const int DefaultSize = 512;
        public const uint DefaultSeed = 0x6B0A7E5u;
        public const int ChopWaves = 12, RippleWaves = 16;
        public const int ChopMinCycles = 2, ChopMaxCycles = 8, RippleMinCycles = 8, RippleMaxCycles = 32;
        private const int FixedOne = 1 << 16;

        public static int ByteCount(int size)
        {
            return size * size * 4;
        }

        public static void Fill(int size, uint seed, byte[] rgba)
        {
            if (size < 64 || (size & (size - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, "WaterNormalMap: el lado es una potencia de 2 ≥ 64.");
            }

            if (rgba == null || rgba.Length < ByteCount(size))
            {
                throw new ArgumentException("WaterNormalMap: hacen falta " + ByteCount(size) + " bytes.", nameof(rgba));
            }

            // La tabla de senos en punto fijo (× 2¹⁶) de un período de "size" pasos.
            var sine = new int[size];
            for (var j = 0; j < size; j++)
            {
                sine[j] = (int)DartMath.Round(Math.Sin(2 * Math.PI * j / size) * FixedOne);
            }

            var rnd = new DartRandom(seed);
            var chop = Waves(rnd, ChopWaves, ChopMinCycles, ChopMaxCycles, size);
            var ripple = Waves(rnd, RippleWaves, RippleMinCycles, RippleMaxCycles, size);
            var chopScale = MaxSlope(chop);
            var rippleScale = MaxSlope(ripple);
            var quarter = size / 4;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    Slope(chop, sine, size, quarter, x, y, out var cx, out var cy);
                    Slope(ripple, sine, size, quarter, x, y, out var rx, out var ry);
                    var o = (y * size + x) * 4;
                    rgba[o] = Encode(-cx, chopScale);
                    rgba[o + 1] = Encode(-cy, chopScale);
                    rgba[o + 2] = Encode(-rx, rippleScale);
                    rgba[o + 3] = Encode(-ry, rippleScale);
                }
            }
        }

        // Un seno: ciclos (m; n) por tile, amplitud en punto fijo y la fase (en pasos de la tabla).
        private readonly struct Wave
        {
            public Wave(int m, int n, long amplitude, int phase)
            {
                M = m;
                N = n;
                Amplitude = amplitude;
                Phase = phase;
            }

            public int M { get; }
            public int N { get; }
            public long Amplitude { get; }
            public int Phase { get; }
        }

        private static Wave[] Waves(DartRandom rnd, int count, int minCycles, int maxCycles, int size)
        {
            var waves = new Wave[count];
            var i = 0;
            var guard = 0;
            while (i < count && guard++ < 10000)
            {
                var m = rnd.NextInt(2 * maxCycles + 1) - maxCycles;
                var n = rnd.NextInt(2 * maxCycles + 1) - maxCycles;
                var k2 = m * m + n * n;
                if (k2 < minCycles * minCycles || k2 > maxCycles * maxCycles)
                {
                    continue;
                }

                // Phillips recortado: ∝ 1/|k|², y el doble de peso para las que viajan a favor del viento (+n, hacia +v).
                var along = n > 0 ? 2 : 1;
                var amplitude = (long)along * FixedOne * minCycles * minCycles / k2;
                waves[i++] = new Wave(m, n, amplitude, rnd.NextInt(size));
            }

            if (i < count)
            {
                throw new InvalidOperationException("WaterNormalMap: no se encontraron las ondas.");
            }

            return waves;
        }

        // ∂h/∂x y ∂h/∂y (en unidades de la tabla) de Σ a·sin(2π(m·x + n·y)/size + φ): a·(m; n)·cos(...), con cos(θ) = sin(θ + π/2).
        private static void Slope(Wave[] waves, int[] sine, int size, int quarter, int x, int y, out long dx, out long dy)
        {
            dx = 0;
            dy = 0;
            var mask = size - 1;
            for (var i = 0; i < waves.Length; i++)
            {
                var w = waves[i];
                var j = (w.M * x + w.N * y + w.Phase + quarter) & mask;
                var c = w.Amplitude * sine[j];
                dx += c * w.M;
                dy += c * w.N;
            }
        }

        // La pendiente máxima posible de una banda (Σ a·máx(|m|; |n|)·1): la codificación nunca recorta.
        private static long MaxSlope(Wave[] waves)
        {
            long sum = 0;
            for (var i = 0; i < waves.Length; i++)
            {
                sum += waves[i].Amplitude * Math.Max(Math.Abs(waves[i].M), Math.Abs(waves[i].N)) * FixedOne;
            }

            return Math.Max(1, sum);
        }

        // 128 + 127·v/máx, redondeado en enteros (0 → 128).
        private static byte Encode(long value, long scale)
        {
            var v = 128 + (value * 127 * 2 + (value >= 0 ? scale : -scale)) / (2 * scale);
            return (byte)Math.Max(0, Math.Min(255, v));
        }
    }
}
