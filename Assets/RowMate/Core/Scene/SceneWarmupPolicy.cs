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

        // It. 6b (06b-agua-avanzada/02-tech-design §2.1, §3.4, §5): el preámbulo de primera vez del proceso (0a es la unidad 1 adelantada),
        // la sesión preparada, el dibujo de calentamiento partido por grupo de renderers, el post al tamaño de la sesión y lo del agua
        // avanzada (fase 10, después de Ready).
        FirstUrpRequest,           // (0b) el primer pedido de URP a una RT de 64² con la máscara en 0
        CoreJitPreamble,           // (0c) una de las 5 unidades de la sesión de usar y tirar (Open y Tick a las 12 y 22 h, vuelo, salida, aérea)
        NextSession,               // arma la sesión preparada con el escenario elegido (liviana)
        WarmupSubDraw,             // un sub-dibujo de calentamiento (cielo; terreno; objetos; bote y remero; agua)
        PostAtSessionSize,         // el post de URP en la RT del tamaño de la sesión
        WaterVariant,              // una variante de WaterAdvanced (WarmUpProgressively(1))
        WaterResource,             // una fila del perfil del horizonte, la RT del reflejo o el mapa de normales
        WaterSubDraw,              // el espejo con máscara 0 o el agua avanzada en una RT de 64²
    }

    // Liviana (corre en cualquier cuadro con tramo > 0, cota ≤ 4 ms) o pesada (solo en un cuadro quieto, DT6b-25).
    public enum UnitClass
    {
        Light,
        Heavy,
    }

    // El cuadro en que se decide si corre una unidad pesada (DT6b-25, G6b-05): quieto (Workout en reposo sin movimiento finito, o negro
    // y quieto), el anterior también quieto, y ninguna entrada del Input System en curso (puntero, toque o tecla apretados, o apretados
    // o soltados en este cuadro). Lo arma SceneBridge sin asignar.
    public readonly struct WarmupFrame
    {
        public WarmupFrame(bool quiet, bool previousQuiet, bool inputActive)
        {
            Quiet = quiet;
            PreviousQuiet = previousQuiet;
            InputActive = inputActive;
        }

        public bool Quiet { get; }
        public bool PreviousQuiet { get; }
        public bool InputActive { get; }

        // Una pesada puede correr acá.
        public bool AllowsHeavy => Quiet && PreviousQuiet && !InputActive;

        // El cuadro siguiente: el anterior cuenta como quieto solo si fue quieto y sin entrada (así ni el cuadro del toque ni el siguiente
        // corren pesadas, G6b-05).
        public WarmupFrame Next(bool quiet, bool inputActive)
        {
            return new WarmupFrame(quiet, Quiet && !InputActive, inputActive);
        }
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

        // It. 6b: la cota de una unidad visible (con el inmersivo a la vista; DT6b-25): lo que pasa de ahí es pesada. Y la de lo de primera
        // vez del proceso (el post de URP, el JIT de Core y los estados de pipeline: 11-22 ms medidos en la 6a).
        public const double VisibleUnitMaxMs = 4;
        public const double FirstTimeUnitCostMs = 22;

        // Las fases del armado (PhaseOf) y las unidades de CoreJitPreamble.
        public const int PreamblePhase = 0, NextSessionPhase = 1, WaterPhase = 10;
        public const int CoreJitPreambleUnits = 5;

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

        // It. 6b (#1; AC-6B-01, -02): como la de arriba, pero una pesada solo corre en un cuadro que lo permite (WarmupFrame.AllowsHeavy):
        // con el inmersivo a la vista o con una entrada en curso, solo las livianas.
        public static bool RunAnother(double sliceMs, double elapsedMs, int unitsThisFrame, bool nextIsHeavy, in WarmupFrame frame)
        {
            if (nextIsHeavy && !frame.AllowsHeavy)
            {
                return false;
            }

            return RunAnother(sliceMs, elapsedMs, unitsThisFrame);
        }

        // Un cuadro quieto (DT6b-25): sin movimiento finito ni armado del HUD, y la pantalla negra y quieta o Workout en reposo con el
        // inmersivo cerrado. Con el inmersivo a la vista nunca es quieto.
        public static bool IsQuiet(bool workoutAtRest, bool screenBlackAndStill, bool immersiveVisible, int finiteMotions, bool hudBuildFrame)
        {
            if (hudBuildFrame || finiteMotions != 0)
            {
                return false;
            }

            return screenBlackAndStill || workoutAtRest && !immersiveVisible;
        }

        // El preámbulo (0a-0c), el post al tamaño de la sesión y toda unidad cuya cota pase de 4 ms son pesadas.
        public static UnitClass Classify(WarmupUnit unit)
        {
            switch (unit)
            {
                case WarmupUnit.CameraLightVolumeTarget:
                case WarmupUnit.FirstUrpRequest:
                case WarmupUnit.CoreJitPreamble:
                case WarmupUnit.PostAtSessionSize:
                    return UnitClass.Heavy;
                default:
                    return UnitCostBoundMs(unit) > VisibleUnitMaxMs ? UnitClass.Heavy : UnitClass.Light;
            }
        }

        // La fase del armado (02-tech-design §2.1): 0 el preámbulo, 1 la sesión preparada, 2-9 el mundo (como en la 6a), 10 el agua
        // avanzada (después de Ready).
        public static int PhaseOf(WarmupUnit unit)
        {
            switch (unit)
            {
                case WarmupUnit.CameraLightVolumeTarget:
                case WarmupUnit.FirstUrpRequest:
                case WarmupUnit.CoreJitPreamble:
                    return PreamblePhase;
                case WarmupUnit.NextSession:
                    return NextSessionPhase;
                case WarmupUnit.Slot:
                    return 2;
                case WarmupUnit.TextureRow:
                    return 3;
                case WarmupUnit.Mesh:
                    return 4;
                case WarmupUnit.Material:
                    return 5;
                case WarmupUnit.ShaderVariant:
                    return 6;
                case WarmupUnit.SyntheticTick:
                    return 7;
                case WarmupUnit.WarmupDraw:
                case WarmupUnit.WarmupSubDraw:
                    return 8;
                case WarmupUnit.PostAtSessionSize:
                    return 9;
                case WarmupUnit.WaterVariant:
                case WarmupUnit.WaterResource:
                case WarmupUnit.WaterSubDraw:
                    return WaterPhase;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit));
            }
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
                // It. 6b: lo de primera vez del proceso tarda 11-22 ms en la PC (06a/05-qa-verification §R3.6): pesadas.
                case WarmupUnit.FirstUrpRequest:
                case WarmupUnit.CoreJitPreamble:
                case WarmupUnit.PostAtSessionSize:
                    return FirstTimeUnitCostMs;
                case WarmupUnit.NextSession:
                case WarmupUnit.WaterVariant:
                    return 1.0;
                case WarmupUnit.WaterResource:
                    return 0.3;
                case WarmupUnit.WarmupSubDraw:
                case WarmupUnit.WaterSubDraw:
                    return VisibleUnitMaxMs;
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
