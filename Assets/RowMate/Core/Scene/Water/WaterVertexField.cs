using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene.Water
{
    // Lo que el vértice del agua avanzada calcula con lo que cambia con la distancia (06b-agua-avanzada/02-tech-design §18, ronda del
    // salto; AC-6B-20 b; QA-V6b-R2-03; #91): la ola del legacy (B0/B1), su pendiente, la cresta de la columna, la espuma de las crestas
    // y los parches de calma. Es la referencia de WaterAdvanced.shader (AdvancedVertex) y de las pruebas que interpolan sobre la malla.
    public struct WaterVertexSample
    {
        public double Height;       // la ola entera (lo que sube la costa: la geometría, sin limitar)
        public double ColorFactor;  // 1 + h·A·0,25 con la ola limitada (el color base)
        public double B1Slope;      // la pendiente de B1 en z, limitada (fogAdd.w)
        public double Crest;        // la cresta de la columna del legacy (sin el pico de 0,4)
        public double FoamCrest;    // la cresta de la espuma de la costa
        public double Calm;         // el peso de los parches de calma
    }

    // DT6b-27 (ronda del salto). Antes la malla se anclaba a la grilla de 16 m (Mod(D; 16)): cada 16 m saltaba 16 m y los anillos
    // re-muestreaban lo que el vértice calcula con otra densidad (un salto visible cada ≈ 4 s a 4 m/s). Ahora la malla queda QUIETA en
    // cinta (centrada en la cámara del contrato): el paso en cada lugar de la pantalla no cambia nunca y todo lo del vértice es continuo
    // en D. Lo que no entra en el paso del anillo (λ < 3 pasos) se apaga con la distancia en vez de "caminar" (aliasing que se arrastra):
    // cada onda × Band(k·paso), con el paso máximo del anillo en ese lugar (WaterRingMesh lo guarda en el color b, continuo entre
    // anillos). Lo apagado tiende a su media: la ola a 0, la cresta a su media sobre la ola entera.
    public static class WaterVertexField
    {
        public const double BandKeep = Math.PI / 3;       // k·paso ≤ π/3 (6 vértices por λ): la onda entera
        public const double BandDrop = 2 * Math.PI / 3;   // k·paso ≥ 2π/3 (3 vértices por λ): nada
        public static readonly double Wave2K = Math.Sqrt(0.7 * 0.7 + 1.3 * 1.3);   // |k| de la segunda onda de B0/B1, × 1/escala
        public static readonly double CalmKA = Math.Sqrt(Sq(2 * Math.PI * WaterLookArt.CalmWavesA / 256) + 0.13 * 0.13);
        public static readonly double CalmKB = Math.Sqrt(Sq(2 * Math.PI * WaterLookArt.CalmWavesB / 256) + 0.21 * 0.21);

        public const double FoamOctave = 2;

        // Las medias sobre la ola entera (h = 0,6·sen a + 0,4·sen b, a y b uniformes): las comprueba WaterVertexFieldTests.
        public const double CrestMean = 0.3753837;       // E[S(0,3; 0,9; h/2 + 0,5)]
        public const double FoamCrestMean = 0.1069487;   // E[S(0,55; 0,85; h)]

        // Dónde va la malla en cinta (la z local del renderer): quieta.
        public static double MeshOffsetZ(double distance)
        {
            return 0;
        }

        // Cuánto queda de una onda de número k (rad/m) con ese paso de malla (m).
        public static double Band(double k, double step)
        {
            return 1 - SceneMath.SmoothStep(BandKeep, BandDrop, k * step);
        }

        public static void Evaluate(double x, double relZ, double maxStep, in WaterProfile p, in WaterPhaseSet ph, out WaterVertexSample into)
        {
            var s = Math.Max(p.Scale, 1e-3);
            var a1 = Band(1 / s, maxStep);
            var a2 = Band(Wave2K / s, maxStep);
            var w1 = Math.Sin(relZ / s + ph.Phase1);
            var w2 = Math.Sin((0.7 * relZ + 1.3 * x) / s + ph.Phase2);
            var h = w1 * 0.6 + w2 * 0.4;
            var hb = w1 * (0.6 * a1) + w2 * (0.4 * a2);
            into.Height = h;
            into.ColorFactor = 1 + hb * p.Amplitude * WaterModel.ColorGain;
            into.B1Slope = p.B1Slope * Math.Cos(relZ / s + ph.Phase1) * a1;
            into.Crest = SceneMath.Lerp(CrestMean, SceneMath.SmoothStep(0.3, 0.9, hb * 0.5 + 0.5), a1);
            // La espuma es un umbral angosto sobre la ola (una mancha por cresta, con armónicos): se apaga una octava antes (2·k₂).
            into.FoamCrest = SceneMath.Lerp(FoamCrestMean, SceneMath.SmoothStep(0.55, 0.85, hb), Band(FoamOctave * Wave2K / s, maxStep));
            var u = 2 * Math.PI * (relZ / 256.0 + ph.NoiseV);
            var n = 0.5 + 0.25 * Band(CalmKA, maxStep) * Math.Sin(WaterLookArt.CalmWavesA * u + 0.13 * x)
                    + 0.25 * Band(CalmKB, maxStep) * Math.Sin(WaterLookArt.CalmWavesB * u - 0.21 * x + 1.7);
            into.Calm = SceneMath.Lerp(p.CalmMin, 1, SceneMath.SmoothStep(0.3, 0.7, n));
        }

        private static double Sq(double v)
        {
            return v * v;
        }
    }
}
