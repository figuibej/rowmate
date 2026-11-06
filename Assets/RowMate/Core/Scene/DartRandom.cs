// Puerto a C# del _Random de la VM de Dart (sdk/lib/_internal/vm/lib/math_patch.dart, la clase Random con semilla y _Random).
// Copyright (c) 2012, the Dart project authors. Uso, redistribución y modificación bajo la licencia BSD-3 del SDK de Dart:
// el aviso completo (copyright, las tres condiciones y el descargo) está en THIRD_PARTY_NOTICES.md, en la raíz del repositorio,
// y viaja con los builds (06a-escena-lago/02-tech-design §2.10; QA-G6aT-19).
using System;

namespace RowMate.Core.Scene
{
    // El math.Random(seed) del legacy, bit a bit (AC-SCN-14-02; Q-61; ADR 012). La semilla se mezcla con la función de 64 bits de
    // Thomas Wang (si da 0, 0x5A17); el estado es un MWC de base 2³² con A = 0xFFFFDA61 (estado = A·bajo + alto, en 64 bits sin
    // signo); el constructor gira el estado 4 veces. NextInt(max) toma el bajo con máscara si max es potencia de 2 y, si no, por
    // rechazo (rnd32 − resultado + max > 2³²); NextDouble = (NextInt(2²⁶)·2²⁷ + NextInt(2²⁷))/2⁵³ (math_patch.dart:179-269).
    // No reemplaza a IRandom (el simulador no es el generador de Dart). Reseed reutiliza la instancia: sin asignaciones.
    public sealed class DartRandom
    {
        private const ulong A = 0xFFFFDA61UL;
        private const ulong Pow2_32 = 1UL << 32;
        private const double Pow2_27 = 134217728.0;
        private const double Pow2_53 = 9007199254740992.0;

        private ulong _state;

        public DartRandom(long seed)
        {
            Reseed(seed);
        }

        // Vuelve a empezar como un Random(seed) nuevo.
        public void Reseed(long seed)
        {
            _state = SetupSeed(unchecked((ulong)seed));
            NextState();
            NextState();
            NextState();
            NextState();
        }

        // Un entero en [0; max). max en [1; 2³¹ − 1]: el legacy pide 1 000 (y 2²⁶, 2²⁷ para el double). Fuera,
        // ArgumentOutOfRangeException, como el RangeError de Dart.
        public int NextInt(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, "DartRandom.NextInt: max tiene que ser positivo.");
            }

            var m = (ulong)max;
            if ((m & (~m + 1)) == m)
            {
                // Potencia de 2: el bajo con máscara.
                NextState();
                return (int)(_state & 0xFFFFFFFFUL & (m - 1));
            }

            ulong rnd32;
            ulong result;
            do
            {
                NextState();
                rnd32 = _state & 0xFFFFFFFFUL;
                result = rnd32 % m;
            }
            while (rnd32 - result + m > Pow2_32);

            return (int)result;
        }

        public double NextDouble()
        {
            return (NextInt(1 << 26) * Pow2_27 + NextInt(1 << 27)) / Pow2_53;
        }

        public bool NextBool()
        {
            return NextInt(2) == 0;
        }

        // Multiply with Carry de base 2³² (la constante de Numerical Recipes 3ª ed., p. 348 B1).
        private void NextState()
        {
            var lo = _state & 0xFFFFFFFFUL;
            var hi = _state >> 32;
            _state = unchecked(A * lo + hi);
        }

        // La mezcla de 64 bits de Thomas Wang (los >>> de Dart son desplazamientos sin signo).
        private static ulong SetupSeed(ulong n)
        {
            unchecked
            {
                n = ~n + (n << 21);
                n ^= n >> 24;
                n *= 265;
                n ^= n >> 14;
                n *= 21;
                n ^= n >> 28;
                n += n << 31;
                if (n == 0)
                {
                    n = 0x5A17;
                }

                return n;
            }
        }
    }
}
