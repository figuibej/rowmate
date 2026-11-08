using System;

namespace RowMate.Core.Scene
{
    // El oleaje de la costa (AC-SCN-12-01, AC-SCN-06-04; DT8-05, DT8-06): la ola de water.frag:45-53 sin el ruido,
    // h = sin(zw/s + 0,6t)·0,6 + sin((0,7·zw + 1,3x)/s + 0,9t)·0,4, que aclara y oscurece el color × (1 + h·A·0,25) y mueve el
    // casco (0,15·h m en proa, centro y popa: arfada = promedio, cabeceo = atan((y_proa − y_popa)/8,2)). Solo con
    // WaveAmplitude ≥ 0,5 (la costa). En cinta (relZ = z − d) con las fases en double: a 42 km no tiembla.
    public static class CoastSwell
    {
        public const double ColorGain = 0.25, HullScale = 0.15, PitchBase = 8.2;
        public const double BowZ = 13.5, CenterZ = 9.5, SternZ = 5.3;   // relZ de proa, centro (BoatZ) y popa (water_painter.dart:29)
        public const double TauSeconds = 0.3;
        private const double TwoPi = 2 * Math.PI;

        // La ola en z de mundo (la fórmula de la spec tal cual).
        public static double Height(double x, double zWorld, double sceneTime, double scale)
        {
            return Math.Sin(zWorld / scale + sceneTime * 0.6) * 0.6 + Math.Sin((zWorld * 0.7 + x * 1.3) / scale + sceneTime * 0.9) * 0.4;
        }

        // DT8-06: las fases que recibe el shader, en [0; 2π).
        public static void Phases(double distance, double sceneTime, double scale, out double phase1, out double phase2)
        {
            phase1 = Text.DartMath.Mod(distance / scale + 0.6 * sceneTime, TwoPi);
            phase2 = Text.DartMath.Mod(0.7 * distance / scale + 0.9 * sceneTime, TwoPi);
        }

        // La ola en cinta con las fases (lo que calcula el shader): igual a Height(x, relZ + d) salvo el redondeo.
        public static double HeightAt(double x, double relZ, double scale, double phase1, double phase2)
        {
            return Math.Sin(relZ / scale + phase1) * 0.6 + Math.Sin((0.7 * relZ + 1.3 * x) / scale + phase2) * 0.4;
        }

        public static double ColorFactor(double h, double amplitude)
        {
            return 1 + h * amplitude * ColorGain;
        }

        // El casco sobre la ola (sin filtrar): 0,15·h en proa, centro y popa (x = 0).
        public static void SampleHull(double distance, double sceneTime, double scale, out double heaveM, out double pitchRad)
        {
            Phases(distance, sceneTime, scale, out var p1, out var p2);
            var bow = HullScale * HeightAt(0, BowZ, scale, p1, p2);
            var center = HullScale * HeightAt(0, CenterZ, scale, p1, p2);
            var stern = HullScale * HeightAt(0, SternZ, scale, p1, p2);
            heaveM = (bow + center + stern) / 3;
            pitchRad = Math.Atan((bow - stern) / PitchBase);
        }
    }

    // El suavizado del casco sobre la ola (τ 0,3 s), reservado en la sesión.
    public struct SwellFilter
    {
        public double HeaveM;
        public double PitchRad;

        public void Step(double dt, double heaveM, double pitchRad)
        {
            var step = dt < 0 || double.IsNaN(dt) ? 0 : (dt > SceneState.MaxStep ? SceneState.MaxStep : dt);
            var k = 1 - Math.Exp(-step / CoastSwell.TauSeconds);
            HeaveM += (heaveM - HeaveM) * k;
            PitchRad += (pitchRad - PitchRad) * k;
        }

        public void Snap(double heaveM, double pitchRad)
        {
            HeaveM = heaveM;
            PitchRad = pitchRad;
        }
    }

    // Bajo el puente (AC-SCN-16-03; DT8-07): o(z) = 0 afuera, 1 bajo el tablero, con una rampa de 1,5 m en cada borde; la luz
    // clave y el ambiente × (1 − 0,4·o).
    public static class BridgeShade
    {
        public const double Strength = Art.RiverArt.UnderBridgeDim;
        public const double Ramp = Art.RiverArt.UnderBridgeRamp;

        // z y el intervalo [zStart; zEnd] del tablero en la misma referencia (mundo o cinta).
        public static double Occlusion(double z, double zStart, double zEnd)
        {
            if (zEnd <= zStart)
            {
                return 0;
            }

            var inside = Math.Min(z - zStart, zEnd - z);
            return SceneMath.Clamp01(inside / Ramp + 0.5);
        }

        public static double LightFactor(double occlusion)
        {
            return 1 - Strength * occlusion;
        }

        // El tablero del puente cuyo ancla (la cara a la cámara) está en zAnchor: de zAnchor a zAnchor + 10.
        public static void DeckSpan(double zAnchor, out double zStart, out double zEnd)
        {
            zStart = zAnchor;
            zEnd = zAnchor + Art.RiverArt.BridgeDepth;
        }
    }

    // Las 28 entradas del agua del legacy en el orden de water.frag (water_painter.dart:29-60; LT-124): un contrato, no se dibuja
    // con ellas (el agua degradada usa otras). uSize (2), horizonte, focal, alto de la cámara, distancia del shader, tiempo,
    // amplitud, escala, x del astro, fuerza, waterNear, waterFar (teñidos), skyHorizon, sunColor, fog (vec3), estela y popa.
    public static class WaterShaderInputs
    {
        public const int Count = 28;
        public const double SternOffset = 4.2;   // water_painter.dart:29

        public static void Uniforms(in SceneCamera cam, double shaderDistance, double sceneTime, double speed, in ScenePalette p,
                                    in LegacyLight light, ShoreEnvironment environment, double[] into)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            if (into == null || into.Length < Count)
            {
                throw new ArgumentException("WaterShaderInputs: hacen falta 28 lugares.", nameof(into));
            }

            var near = environment.TintWater(p.WaterNear);
            var far = environment.TintWater(p.WaterFar);
            into[0] = cam.Width;
            into[1] = cam.Height;
            into[2] = cam.HorizonY;
            into[3] = cam.Focal;
            into[4] = SceneCamera.CamHeight;
            into[5] = shaderDistance;
            into[6] = sceneTime;
            into[7] = environment.WaveAmplitude;
            into[8] = environment.WaveScale;
            into[9] = light.X;
            into[10] = light.Strength;
            Put(into, 11, near);
            Put(into, 14, far);
            Put(into, 17, p.SkyHorizon);
            Put(into, 20, p.SunColor);
            Put(into, 23, p.Fog);
            into[26] = SceneMath.Clamp(speed / 4, 0.0, 1.0);
            into[27] = SceneCamera.BoatZ - SternOffset;
        }

        private static void Put(double[] into, int at, in SceneColor c)
        {
            into[at] = c.R;
            into[at + 1] = c.G;
            into[at + 2] = c.B;
        }
    }
}
