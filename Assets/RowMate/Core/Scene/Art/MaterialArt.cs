using System;

namespace RowMate.Core.Scene.Art
{
    // Los números de los materiales de la escena (06a-escena-lago/04-design-notes §5.5, §7.3 a §7.6, §8.3, §9, §10.1, §10.3, §12.3,
    // §15.2; WP-D1, dueño el Designer): el modelo de luz, la niebla, la aparición y los tramados, el viento, el terreno, la madera,
    // el casco, la sombra del bote y los degradés de pantalla. Los shaders de Art/Shaders/Scene leen estos mismos números (como
    // valores por defecto del material o como constantes kRm*: SceneShaderContractTests los compara uno por uno). Las funciones
    // son las que usan los streamers de WP-B1 para cada instancia (puras, en double).
    public static class MaterialArt
    {
        // ── El modelo de luz (§10.1; DN6a-13): luz(N) = T(a) ⊙ [0,65·Hemi(N) + 0,55·Kn·envolvente(N·L; w)·sombra] ──
        // T(a) = (a; a; mín(1; a + 0,12)) es el tinte del legacy (shore_painter.dart:47). Kn = sunColor/máx(sunColor)·(I/1,75).
        public const double HemiWeight = 0.80, KeyWeight = 0.45, NoonIntensity = 1.75, LegacyBlueLift = 0.12;
        public const double SolidWrap = 0, FoliageWrap = 0.50, GrassWrap = 0.20;

        // La luz se calcula como un multiplicador en el espacio del legacy (sRGB, donde el tinte y Color.lerp multiplican y mezclan)
        // y pasa al lineal del pipeline con esta potencia; la sombreada se hace en "gamma 2" (√ del lineal), que reproduce la mezcla
        // de Color.lerp a ± 1 nivel y cuesta un producto al final (04-design-notes, "Cambios por implementación de WP-D1").
        public const double FactorGamma = 2.2;

        public static double Wrap(double nDotL, double wrap)
        {
            return Math.Max(0, (nDotL + wrap) / (1 + wrap));
        }

        // ── La niebla (§10.3; RowMateFog.hlsl): f = 0,8·clamp((z − 80)/320) + 0,2·clamp((z − 400)/800); bruma baja
        // máx(f; m(h)·exp(−máx(y; 0)/1,2)·S(10; 150; z)) con el color lerp(fog; sunGlow; 0,25·g). _RmFogParams = (80; 320; 0,8; 1 200). ──
        public static readonly double[] FogParams = { 80, 320, 0.8, 1200 };
        public const double MistHeight = 1.2;

        // ── Aparición y tramados (§7.6; DV-62 corregida, DN6a-24): Bayer 4 × 4 fijo en la pantalla ──
        public const double AppearFar = 400, AppearNear = 360, NearFadeNear = 1, NearFadeFar = 2, LodCrossFraction = 0.10;

        // La matriz de Bayer 4 × 4 (RowMateDither.hlsl, kRmBayer4): el umbral del píxel (x; y) es (M[y mod 4][x mod 4] + 0,5)/16.
        public static readonly int[] Bayer4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        public static double BayerThreshold(int x, int y)
        {
            return (Bayer4[(y & 3) * 4 + (x & 3)] + 0.5) / 16;
        }

        // RmDitherClip: con fade ≥ 0 se ve el píxel si el umbral < fade; con fade < 0 (el LOD que entra), si el umbral ≥ 1 + fade, que
        // es exactamente el resto. Así el par (w; w − 1) cubre cada píxel una vez.
        public static bool DitherKeeps(double fade, int x, int y)
        {
            var t = BayerThreshold(x, y);
            return fade >= 0 ? t < fade : t >= 1 + fade;
        }

        // El fundido de un objeto de la orilla por su distancia relativa (relZ = z − d): aparece de 400 a 360 m (casi perdido en la
        // niebla) y se va con el tramado de 2 a 1 m (la lista del legacy es [d + 1; d + 400]). Lineal: con 5 m/s, 2 % por cuadro.
        public static double InstanceFade(double relZ)
        {
            var appear = SceneMath.Clamp01((AppearFar - relZ) / (AppearFar - AppearNear));
            var near = SceneMath.Clamp01((relZ - NearFadeNear) / (NearFadeFar - NearFadeNear));
            return appear * near;
        }

        // El peso del LOD cercano en el cambio a `boundary` m: 1 hasta el 90 % del límite, 0 desde el 110 %, lineal entre ellos.
        public static double LodWeight(double distance, double boundary)
        {
            var from = boundary * (1 - LodCrossFraction);
            var to = boundary * (1 + LodCrossFraction);
            return SceneMath.Clamp01((to - distance) / (to - from));
        }

        // Lo que va a _RmFade de los dos LOD en el cambio: el cercano con w, el lejano con w − 1 (el patrón complementario). Cada uno
        // se multiplica además por InstanceFade (el signo se conserva).
        public static void LodFadePair(double distance, double boundary, out double near, out double far)
        {
            near = LodWeight(distance, boundary);
            far = near - 1;
        }

        // Los LOD y los fines (§7.4, §7.6), en m de la cámara; el nivel Bajo los multiplica por 0,7 (§15.2).
        public static readonly double[] PineLods = { 40, 120, 400 };
        public static readonly double[] CabinLods = { 60, 400 };
        public static readonly double[] ForestLods = { 40, 120 };
        public static readonly double[] MeadowLods = { 60, 400 };
        public static readonly double[] RockLods = { 40, 200 };
        public static readonly double[] ReedLods = { 30, 120 };
        public const double ForestFadeFrom = 110, RockFadeFrom = 180, ReedFadeFrom = 100;
        public const double GrassLod0 = 20, GrassEndHigh = 60, GrassEndMedium = 40, GrassEndLow = 30, GrassFadeFraction = 0.2;
        public const double LowTierDistanceScale = 0.7, LowTierGrassDensity = 0.5;

        public static double TierDistance(double meters, QualityTier tier)
        {
            return tier == QualityTier.Low ? meters * LowTierDistanceScale : meters;
        }

        public static double GrassEnd(QualityTier tier)
        {
            return tier == QualityTier.High ? GrassEndHigh : tier == QualityTier.Medium ? GrassEndMedium : GrassEndLow;
        }

        // El pasto no se corta: en el último 20 % del fin se achica hasta 0 en el vértice (sin alpha test; el terreno lleva el mismo
        // verde). _RmGrassEnd = (fin; 0,2; 0; 0).
        public static double GrassScale(double distance, double end)
        {
            return SceneMath.Clamp01((end - distance) / (end * GrassFadeFraction));
        }

        // ── El viento (§5.5; uno solo, hacia +x): (amplitud en rad; frecuencia en rad/s; ráfaga en rad; período de la ráfaga en s) ──
        // La rotación va con peso altura² (el color de vértice g); la ráfaga es un ruido de valor del tiempo de escena / 9 s.
        public static readonly double[] PineWind = { 0.03, 1.1, 0.012, 9 };
        public static readonly double[] ReedWind = { 0.06, 1.7, 0.024, 9 };
        public static readonly double[] GrassWind = { 0.08, 2.3, 0, 9 };
        public const double MeadowWindScale = 1.5;

        // La fase por instancia (_RmWindPhase.x), en double y acotada a [0; 2π): no salta a ninguna distancia.
        public static double PineWindPhase(double worldX)
        {
            return Wrap2Pi(worldX * 0.13);
        }

        public static double ReedWindPhase(double worldZ, int seed)
        {
            return Wrap2Pi(worldZ * 0.4 + seed);
        }

        public static double GrassWindPhase(double worldX, double worldZ)
        {
            return Wrap2Pi(worldX * 0.9 + worldZ * 0.7);
        }

        private static double Wrap2Pi(double phase)
        {
            var twoPi = 2 * Math.PI;
            var r = phase % twoPi;
            return r < 0 ? r + twoPi : r;
        }

        // ── El terreno procedural (§7.5) ──
        public const double WetTop = 0.35, WetNoise = 0.05, WetReflect = 0.15, ContactTop = 0.04, ContactAlpha = 0.20;
        public const double PebbleLuminance = 0.12, RockSlopeDeg = 35, NoiseFineMeters = 1.3, NoiseCoarseMeters = 7;
        public const double CanopyOcclusion = 0.35;     // la copa lejana se oscurece hasta 0,35 en los valles del ruido

        // ── La madera y la ventana (§7.3): tablas de 0,18 m con una línea de 1 cm; tablones de 0,20 m con juntas de 2 cm ──
        public static readonly double[] PlankParams = { 0.18, 0.01, 0.20, 0.02 };
        public const double PlankShade = 0.55;          // lo que oscurece la línea entre tablas
        public const double WindowFresnel = 0.4;        // de día #90CAF9 × (0,6 + 0,4·Fresnel hacia skyHorizon)
        public const double ForestLuminance = 0.85;     // el bosque cercano: los verdes del legacy × 0,85

        // ── El casco (§9): gelcoat (smoothness 0,85; Blinn 64; Fresnel 0,3 hacia skyHorizon) ──
        public static readonly double[] Gelcoat = { 0.85, 64, 0.3 };

        // La exposición del casco y cuánto del matiz del cielo le llega (WP-D2, B6a-22): sin tonemapping, la cubierta blanca del mediodía con
        // la luz del modelo (≈ 1,2 del tinte) se recortaba en una silueta plana. El ambiente del cielo entra con la mitad de su croma (un
        // gelcoat blanco muestra el color del cielo más que la madera o el pasto) y el casco con × 0,78 (en gamma 2, en el vértice): a las
        // 12 h la cubierta queda en 0,80-0,95 (con 0,84 el post todavía la recortaba: #F1FEFE en CAP-02) y los costados, a la sombra,
        // más grises; el especular sigue encima. La prueba es MaterialArtTests.TheHullDeckStaysWhiteBelowClippingAtNoon.
        public const double HullExposure = 0.78, HullAmbientChroma = 0.5;
        // It. 6b (#85; 00-arte-agua §9): en la cubierta (N.y > 0,8, con rampa de 0,7 a 0,9) el Fresnel y el especular del gelcoat se recortan
        // a 0,15: a 22° desde la cámara el sol bajo de enfrente se reflejaba entero y la popa se leía como una tabla blanca.
        public const double HullDeckGloss = 0.15;

        // ── La sombra del bote (§8.3): óvalo de 8,6 × 0,9 m, corrido 0,35 m al lado opuesto del astro, alfa 0,08·ambiente en el centro
        // y exp(−r²·4) hacia el borde ──
        public static readonly double[] BoatShadow = { 8.6, 0.9, 0.35, 0.08 };
        public const double BoatShadowFalloff = 4;

        // El lado de la sombra: el opuesto al acimut del astro (con el astro al frente, a la derecha).
        public static int BoatShadowSide(double diskAzimuthDeg)
        {
            return diskAzimuthDeg > 0 ? -1 : 1;
        }

        // ── Los degradés de pantalla (§12.3; DV-67: capa del HUD, nunca en la escena sola) ──
        public const uint ScrimColor = 0xFF06122A;
        public static readonly double[] ScrimTop = { 0.38, 0.24 };
        public static readonly double[] ScrimBottom = { 0.32, 0.18 };

        // El alfa del degradé en la fracción y de la pantalla (0 arriba, 1 abajo): lineal, en el espacio del HUD (sRGB).
        public static double ScrimAlpha(double y)
        {
            var top = ScrimTop[0] * SceneMath.Clamp01(1 - y / ScrimTop[1]);
            var bottom = ScrimBottom[0] * SceneMath.Clamp01(1 - (1 - y) / ScrimBottom[1]);
            return Math.Max(top, bottom);
        }
    }
}
