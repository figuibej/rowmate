using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Water
{
    // La boya de la costa sobre el mar de fondo (#31; AC-6B-19; DT6b-15, DT6b-21): sube y baja con la misma ola que el casco,
    // y = 0,15·h(x; relZ) con las fases del cuadro (CoastSwell.HullScale), apagada de 120 a 300 m como B0 (CoastArt). Fuera de la
    // costa, 0. ± 0,15 m: nunca se hunde ni flota en el aire respecto del agua dibujada (el vértice del agua usa la misma fórmula).
    public static class CoastBuoySwell
    {
        public static double Heave(double x, double relZ, in WaterPhaseSet ph, in WaterProfile p)
        {
            if (!p.Swell)
            {
                return 0;
            }

            return CoastSwell.HullScale * CoastSwell.HeightAt(x, relZ, p.Scale, ph.Phase1, ph.Phase2) * SwellFade(relZ);
        }

        // B0 se apaga con la distancia (00-arte-agua §3.1): × (1 − S(120; 300; relZ)).
        public static double SwellFade(double relZ)
        {
            return 1 - SceneMath.SmoothStep(CoastArt.SwellFadeNear, CoastArt.SwellFadeFar, relZ);
        }

        // Lo que se apoya en el agua de la costa (charcos, anillos, estela; DT6b-21): la altura del agua en (x; relZ), 0 fuera de la costa.
        public static double WaterHeight(double x, double relZ, in WaterPhaseSet ph, in WaterProfile p)
        {
            return Heave(x, relZ, ph, p);
        }
    }
}
