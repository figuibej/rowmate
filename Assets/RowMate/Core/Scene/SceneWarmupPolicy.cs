using System;

namespace RowMate.Core.Scene
{
    // Las unidades del armado de la escena (06a-escena-lago/02-tech-design §3.4): todas de costo acotado y conocido, ninguna
    // indivisible (QA-G6aT-03). SceneWorldBuilder corre unidades mientras quede tramo en el cuadro.
    public enum WarmupUnit
    {
        CameraLightVolumeTarget,   // (1) cámara, luz, Volume, RT
        Slot,                      // (2) un slot: resolución y validación
        TextureRow,                // (3) una fila de una textura procedural
        Mesh,                      // (4) una malla (≤ 1 200 triángulos)
        Material,                  // (5) un material con los valores del nivel
        ShaderVariant,             // (6) una variante de shader (WarmUpProgressively(1))
        SyntheticTick,             // (7) un Tick de una sesión de usar y tirar
        WarmupDraw,                // (8) el único dibujo de calentamiento: lo que cuesta un cuadro de escena
    }

    // Cuándo se puede preparar la escena y cuánto por cuadro (06a-escena-lago/02-tech-design §2.16; AC-WKT-13-15): 3 ms con la
    // pantalla negra y quieta o con Workout en reposo sin nada moviéndose; 2 ms con el inmersivo abierto, sin movimiento finito y
    // fuera del cuadro en que se arma el HUD (la escena demorada, también después de la cascada); 0 en cualquier otro caso (la
    // cascada, el vuelo, una transición).
    public static class SceneWarmupPolicy
    {
        public const double RestSliceMs = 3;
        public const double ImmersiveSliceMs = 2;

        // Lo más que puede costar una unidad (ms, en la PC de referencia; el dibujo de calentamiento, lo de un cuadro de escena
        // con los shaders calientes, §4.1). Ninguna unidad pasa de un cuadro de escena.
        public const double MaxUnitCostMs = 1.5;

        public static double SliceMs(bool workoutAtRest, bool screenBlackAndStill, bool immersiveOpen, int finiteMotions, bool hudBuildFrame)
        {
            if (hudBuildFrame || finiteMotions < 0)
            {
                return 0;
            }

            if (screenBlackAndStill)
            {
                return RestSliceMs;
            }

            if (finiteMotions > 0)
            {
                return 0;
            }

            if (workoutAtRest)
            {
                return RestSliceMs;
            }

            return immersiveOpen ? ImmersiveSliceMs : 0;
        }

        // ¿Se corre otra unidad en este cuadro? Al menos una por cuadro con tramo > 0; después, mientras quede tiempo.
        public static bool RunAnother(double sliceMs, double elapsedMs, int unitsThisFrame)
        {
            if (sliceMs <= 0)
            {
                return false;
            }

            return unitsThisFrame == 0 || elapsedMs < sliceMs;
        }

        // La cota de costo de una unidad (ms). Todas ≤ MaxUnitCostMs: no hay pasos indivisibles.
        public static double UnitCostBoundMs(WarmupUnit unit)
        {
            switch (unit)
            {
                case WarmupUnit.CameraLightVolumeTarget:
                    return 0.8;
                case WarmupUnit.Slot:
                    return 0.3;
                case WarmupUnit.TextureRow:
                    return 0.2;
                case WarmupUnit.Mesh:
                    return 1.0;
                case WarmupUnit.Material:
                    return 0.3;
                case WarmupUnit.ShaderVariant:
                    return 1.0;
                case WarmupUnit.SyntheticTick:
                    return 0.5;
                case WarmupUnit.WarmupDraw:
                    return MaxUnitCostMs;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit));
            }
        }

        // El dibujo de calentamiento se permite donde se permitiría dibujar la escena: con tramo > 0 (nunca durante un movimiento
        // finito ni en el cuadro del armado del HUD).
        public static bool AllowsWarmupDraw(double sliceMs)
        {
            return sliceMs > 0;
        }
    }
}
