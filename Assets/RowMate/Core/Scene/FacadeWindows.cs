using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Las ventanas de un edificio del río (shore_painter.dart:133-148; AC-SCN-20-04; DA8-04): pisos = 3 + variante mod 4, tono
    // variante mod 3 y, de noche, una tirada de Random(variante) por ventana, piso por piso (de arriba a abajo en el legacy: f
    // crece desde el techo) y de izquierda a derecha, encendida si es < 0,6. La máscara del frente (bit f·3 + i, ≤ 18 bits) va
    // en la instancia (cabe en los 24 bits exactos de un float); la cara del río sigue la misma secuencia con 5 por piso.
    public static class FacadeWindows
    {
        public static int Floors(int seed)
        {
            return 3 + Mod(seed, 4);
        }

        public static int Tone(int seed)
        {
            return Mod(seed, 3);
        }

        public static uint FrontMask(int seed, DartRandom rnd)
        {
            Masks(seed, rnd, out var front, out _);
            return front;
        }

        // Las dos máscaras con un solo DartRandom (sin asignar: el que pasa quien llama se resiembra).
        public static void Masks(int seed, DartRandom rnd, out uint front, out ulong river)
        {
            if (rnd == null)
            {
                throw new ArgumentNullException(nameof(rnd));
            }

            rnd.Reseed(seed);
            var floors = Floors(seed);
            front = 0;
            for (var f = 0; f < floors; f++)
            {
                for (var i = 0; i < RiverArt.WindowsPerFloor; i++)
                {
                    if (rnd.NextDouble() < RiverArt.WindowLitThreshold)
                    {
                        front |= 1u << (f * RiverArt.WindowsPerFloor + i);
                    }
                }
            }

            river = 0;
            for (var f = 0; f < floors; f++)
            {
                for (var i = 0; i < RiverArt.RiverWindowsPerFloor; i++)
                {
                    if (rnd.NextDouble() < RiverArt.WindowLitThreshold)
                    {
                        river |= 1UL << (f * RiverArt.RiverWindowsPerFloor + i);
                    }
                }
            }
        }

        // ¿La ventana i del piso f (f = 0 el de arriba, como el legacy) está encendida de noche?
        public static bool IsLit(uint frontMask, int floor, int window)
        {
            return (frontMask & (1u << (floor * RiverArt.WindowsPerFloor + window))) != 0;
        }

        // La máscara como float exacto (≤ 24 bits: todo entero hasta 2²⁴ es representable).
        public static float AsFloat(uint mask)
        {
            if (mask >= 1u << 24)
            {
                throw new ArgumentOutOfRangeException(nameof(mask), mask, "FacadeWindows: la máscara del frente usa más de 24 bits.");
            }

            return mask;
        }

        private static int Mod(int a, int b)
        {
            var m = a % b;
            return m < 0 ? m + b : m;
        }
    }
}
