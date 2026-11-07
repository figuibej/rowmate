using System;
using RowMate.Core.Design;

namespace RowMate.Core.Scene.Stroke
{
    // El esfuerzo visual según los vatios (ANIMATION_BIBLE §3.8; AC-SCN-08-12): e = clamp((W − 60)/240; 0; 1). Con R-4 (150 W)
    // e = 0,375; Suave (100 W) 0,167; Fuerte (280 W) 0,917.
    public static class EffortModel
    {
        public static double Effort(double watts)
        {
            return SceneMath.Clamp((watts - MotionSpecs.EffortWattsFloor) / MotionSpecs.EffortWattsSpan, 0, 1);
        }
    }

    // e suavizado con τ 1 s (sin asignar). Snap pone el valor en el acto (al abrir la sesión).
    public struct EffortFilter
    {
        public double Value;

        public void Step(double dt, int watts)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            Value += (EffortModel.Effort(watts) - Value) * (1 - Math.Exp(-step / MotionSpecs.EffortTauSeconds));
        }

        public void Snap(int watts)
        {
            Value = EffortModel.Effort(watts);
        }
    }
}
