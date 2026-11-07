using System;

namespace RowMate.Core.Scene.Stroke
{
    // Los eventos de la pala (ANIMATION_BIBLE §3.7), una vez por palada: Catch cuando φ cruza 0; BladeEntry y BladeExit cuando el
    // centro de la pala visible cruza el agua bajando y subiendo; Finish cuando c cruza 0,36. Nunca con ω_s = 0 (pausa).
    [Flags]
    public enum StrokeEvents
    {
        None = 0,
        Catch = 1,
        BladeEntry = 2,
        BladeExit = 4,
        Finish = 8,
    }
}
