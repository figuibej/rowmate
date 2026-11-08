using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // Las constantes de las dos cordilleras del lago (06a-escena-lago/02-tech-design §2.9). Los valores viven en LakeArt.
    public readonly struct HorizonSpec
    {
        public HorizonSpec(double peakScreenFraction, double maxPeakDeg, double farD0, double farDMin, double nearD0, double nearDMin,
                           double approachRate, double snowThreshold, double snowEdge, double snowNoise, double snowNoisePeriodDeg)
        {
            PeakScreenFraction = peakScreenFraction;
            MaxPeakDeg = maxPeakDeg;
            FarD0 = farD0;
            FarDMin = farDMin;
            NearD0 = nearD0;
            NearDMin = nearDMin;
            ApproachRate = approachRate;
            SnowThreshold = snowThreshold;
            SnowEdge = snowEdge;
            SnowNoise = snowNoise;
            SnowNoisePeriodDeg = snowNoisePeriodDeg;
        }

        public static HorizonSpec Lake => new HorizonSpec(LakeArt.PeakScreenFraction, LakeArt.MaxPeakDeg, LakeArt.FarD0, LakeArt.FarDMin,
            LakeArt.NearD0, LakeArt.NearDMin, LakeArt.ApproachRate, LakeArt.SnowThreshold, LakeArt.SnowEdge, LakeArt.SnowNoise,
            LakeArt.SnowNoisePeriodDeg);

        // It. 8 (02-tech-design §2.5): el hMax del legacy (0,12 / 0,08 / 0,06 del alto), el tope de 9° (DV-87) y el acercamiento
        // al 4 % (Q-65) de una sola capa (lejana = cercana); sin nieve.
        public static HorizonSpec River => Single(RiverArt.HorizonFraction, RiverArt.HorizonD0, RiverArt.HorizonDMin);

        public static HorizonSpec Coast => Single(CoastArt.HorizonFraction, CoastArt.HorizonD0, CoastArt.HorizonDMin);

        public static HorizonSpec Regatta => Single(RegattaArt.HorizonFraction, RegattaArt.HorizonD0, RegattaArt.HorizonDMin);

        private static HorizonSpec Single(double fraction, double d0, double dMin)
        {
            return new HorizonSpec(fraction, LakeArt.MaxPeakDeg, d0, dMin, d0, dMin, LakeArt.ApproachRate, 2, 0, 0, 1);
        }

        public double PeakScreenFraction { get; }
        public double MaxPeakDeg { get; }
        public double FarD0 { get; }
        public double FarDMin { get; }
        public double NearD0 { get; }
        public double NearDMin { get; }
        public double ApproachRate { get; }
        public double SnowThreshold { get; }
        public double SnowEdge { get; }
        public double SnowNoise { get; }
        public double SnowNoisePeriodDeg { get; }
    }

    // Las dos cordilleras (lake_environment.dart:43-85, environment.dart:127-129; F-SCN-19; Q-65, DV-55, DV-64, DV-65): el perfil
    // del legacy por acimut (70° = un ancho del legacy, como el astro), un anillo de elevación fija que se acerca con la distancia
    // (k(d)) sin correrse de costado, y el color del contrato.
    public static class HorizonModel
    {
        public static readonly SceneColor FarTone = SceneColor.FromArgb(LakeArt.FarTone);
        public static readonly SceneColor NearTone = SceneColor.FromArgb(LakeArt.NearTone);
        public static readonly SceneColor SnowTone = SceneColor.FromArgb(LakeArt.SnowTone);

        // lake_environment.dart:62: 0,45 + 0,35·sin(u·6,3 + fase) + 0,2·sin(u·15,1 + 2·fase).
        public static double LegacyProfile(double u, double phase)
        {
            return 0.45 + 0.35 * Math.Sin(u * 6.3 + phase) + 0.2 * Math.Sin(u * 15.1 + phase * 2);
        }

        // El perfil (fracción del alto máximo) por acimut: lejano u = acimut/70° + 0,5 con fase 0; cercano u = 1,6·(acimut/70° + 0,5)
        // con fase 3,1. La forma no depende de la ventana ni de la distancia.
        public static double Profile(double azimuthDeg, bool far)
        {
            var u = azimuthDeg / SkyArt.AzimuthSpanDeg + 0.5;
            return far ? LegacyProfile(u * LakeArt.FarFrequency, LakeArt.FarPhase) : LegacyProfile(u * LakeArt.NearFrequency, LakeArt.NearPhase);
        }

        // environment.dart:127-129: lerp(tono; fog; 0,35 + 0,4·profundidad).
        public static SceneColor Color(in ScenePalette p, in SceneColor tone, double depth)
        {
            return SceneColor.Lerp(tone, p.Fog, 0.35 + 0.4 * depth);
        }

        public static SceneColor FarColor(in ScenePalette p)
        {
            return Color(p, FarTone, LakeArt.FarDepth);
        }

        public static SceneColor NearColor(in ScenePalette p)
        {
            return Color(p, NearTone, LakeArt.NearDepth);
        }

        // lake_environment.dart:78: la nieve, lerp(#F2F6FA; fog; 0,3).
        public static SceneColor SnowColor(in ScenePalette p)
        {
            return SceneColor.Lerp(SnowTone, p.Fog, LakeArt.SnowFog);
        }

        // El alto máximo del perfil (P-11, DV-65, AC-SCN-19-01): tan Θ = mín(0,12·alto/focal; tan 9°), el hMax del legacy con un
        // tope. Horizontal: atan 0,12 = 6,843°; vertical: 9°.
        public static double PeakTan(in SceneCamera cam, in HorizonSpec spec)
        {
            return Math.Min(spec.PeakScreenFraction * cam.Height / cam.Focal, Math.Tan(SceneMath.DegToRad(spec.MaxPeakDeg)));
        }

        public static double PeakAngleDeg(in SceneCamera cam, in HorizonSpec spec)
        {
            return SceneMath.RadToDeg(Math.Atan(PeakTan(cam, spec)));
        }

        // Q-65: k(d) = D₀/D(d), D(d) = Dmín + (D₀ − Dmín)·exp(−0,04·d/(D₀ − Dmín)). En double: con el bote quieto, quieta.
        public static double ApproachScale(double distance, bool far, in HorizonSpec spec)
        {
            var d0 = far ? spec.FarD0 : spec.NearD0;
            var dMin = far ? spec.FarDMin : spec.NearDMin;
            var span = d0 - dMin;
            return d0 / (dMin + span * Math.Exp(-spec.ApproachRate * distance / span));
        }

        // La elevación (grados) de la cresta en un acimut: tan(elevación) = tan Θ·p·k(d) (el cercano, a 0,7 del alto).
        public static double RidgeElevationDeg(in SceneCamera cam, in HorizonSpec spec, double azimuthDeg, bool far, double distance)
        {
            var p = Math.Max(0, Profile(azimuthDeg, far)) * (far ? 1 : LakeArt.NearHeightScale);
            return SceneMath.RadToDeg(Math.Atan(PeakTan(cam, spec) * p * ApproachScale(distance, far, spec)));
        }

        // DV-64: la nieve del lejano donde p ≥ 0,58, con borde S(± 0,03) y un ruido de valor determinista de ± 0,04 en p con período
        // de 1,7° de acimut (las lenguas). 0-1.
        public static double SnowMask(double profile, double azimuthDeg, in HorizonSpec spec)
        {
            var n = SceneHash.Value1(LakeArt.SnowNoiseSeed, azimuthDeg / spec.SnowNoisePeriodDeg) * 2 - 1;
            var p = profile + spec.SnowNoise * n;
            return SceneMath.SmoothStep(spec.SnowThreshold - spec.SnowEdge, spec.SnowThreshold + spec.SnowEdge, p);
        }

        // La regla de nieve del legacy (lake_environment.dart:63-84; contrato, no se dibuja, DV-64): los índices de los 48 pasos
        // con un pico local sobre el 75 % del alto, para una ventana y una distancia (el perfil del legacy se corre con
        // distance·0,004·60 dp). Devuelve cuántos y los escribe en `into`.
        public static int LegacySnowPeaks(double width, double height, double distance, Span<int> into)
        {
            const int Steps = 48;
            var hMax = height * 0.12;
            var shift = distance * 0.004;
            Span<double> heights = stackalloc double[Steps + 1];
            for (var i = 0; i <= Steps; i++)
            {
                var x = width * i / Steps;
                var u = (x + shift * 60) / width * 1.0;
                heights[i] = hMax * (0.45 + 0.35 * Math.Sin(u * 6.3 + 0.0) + 0.2 * Math.Sin(u * 15.1 + 0.0 * 2));
            }

            var count = 0;
            for (var i = 1; i < Steps; i++)
            {
                if (heights[i] > hMax * 0.75 && heights[i] >= heights[i - 1] && heights[i] >= heights[i + 1])
                {
                    if (count < into.Length)
                    {
                        into[count] = i;
                    }

                    count++;
                }
            }

            return count;
        }
    }
}
