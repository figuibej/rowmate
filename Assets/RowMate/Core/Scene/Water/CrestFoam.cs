using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Water
{
    // La espuma de las crestas de la costa (#31; AC-6B-19; DT6b-06): S(0,55; 0,85; h)·S(0,45; 0,75; ruido)·(1 − S(15; 150; z)) × 0,35,
    // solo arriba de la ola (h del legacy en [−1; 1]) y nunca más del 35 %.
    public static class CrestFoam
    {
        public static double Amount(double h, double noise, double z)
        {
            var crest = SceneMath.SmoothStep(WaterLookArt.CrestFoamHeightFrom, WaterLookArt.CrestFoamHeightTo, h);
            var broken = SceneMath.SmoothStep(WaterLookArt.CrestFoamNoiseFrom, WaterLookArt.CrestFoamNoiseTo, noise);
            var near = 1 - SceneMath.SmoothStep(WaterLookArt.CrestFoamNear, WaterLookArt.CrestFoamFar, z);
            return WaterLookArt.CrestFoamMax * crest * broken * near;
        }
    }
}
