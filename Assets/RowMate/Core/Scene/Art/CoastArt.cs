namespace RowMate.Core.Scene.Art
{
    // Los números de arte de la Costa (08-entornos-selector/00-arte-entornos §3; 02-tech-design §2.4, §2.6, §2.7). Los crea WP-A;
    // desde WP-D1 son del Designer. Metros salvo que diga otra cosa.
    public static class CoastArt
    {
        // ── El corte (§3.1; AC-SCN-17-04) ──
        public const double RockBaseX = -12.5;
        public const double SeaFloor = -7;
        public const double BeachFromX = -9.5, BeachToX = -20, BeachSlopeDeg = 6, DuneToX = -30, DuneHeight = 2, WetBand = 1.2;
        public const double BeachTransition = 10;
        public const double CliffFootDepth = -0.6;    // el pie de las rocas, apenas bajo el agua
        public const double RockTop = 0.3;            // el suelo bajo la pared del acantilado (lo tapa el objeto)
        public const uint Sand = 0xFFD8C3A0, CliffGrass = 0xFF556B2F;

        // ── Acantilado (shore_painter.dart:202-215) ──
        public const double CliffHeight = 8, CliffDepth = 7, CliffLength = 28, CliffOverlap = 3, CliffStep = 1.4, CliffEdgeNoise = 1.5;
        public const double CliffEdgeRise = 0.4, CliffGrassHeight = 0.9, CliffGrassTop = 8.6, CrackWidth = 0.6, CrackHeight = 1.2;
        public const uint CliffRock = 0xFF5D4A42, CliffCrack = 0xFF3E2F2A;

        // ── Faro (shore_painter.dart:217-227; DT8-10) ──
        public const double LighthouseHeight = 9, LighthouseRadius = 1.1, LighthouseTopRadius = 0.9, LanternBottom = 9, LanternTop = 10.2;
        public const double LanternWidth = 1.6, LighthouseHaloRadius = 3, LighthouseHaloY = 9.6, Promontory = 2.5;
        public static readonly double[] StripeBottoms = { 1, 4, 7 };   // −8 + 3i del legacy, de 1 m
        public const uint LighthouseTower = 0xFFECEFF1, LighthouseStripe = 0xFFD32F2F, Lantern = 0xFF37474F, LighthouseHalo = 0xFFFFF59D;
        public const double LighthouseHaloAlpha = 0.35, BeamPeriodSeconds = 12, BeamLength = 60, BeamRange = 400;

        // ── Sombrilla (shore_painter.dart:229-234) ──
        public const double UmbrellaPole = 2.2, UmbrellaRadius = 0.9, UmbrellaTop = 3.1, TowelWidth = 1.2, TowelDepth = 0.6;
        public static readonly uint[] UmbrellaTones = { 0xFFF06292, 0xFF4FC3F7, 0xFFFFD54F };   // seed mod 3
        public const uint UmbrellaPoleTone = 0xFF5D4037, Towel = 0xFFFFF3E0;

        // ── Relleno (§3.1) ──
        public const double RockSpacing = 3.5, ShrubSpacing = 6, ShrubMaxHeight = 1.2;

        // ── Horizonte (§3.2; coast_environment.dart:51-66) ──
        public const uint FarCliffTone = 0xFF4A3F3A;
        public const double FarCliffDepth = 0.9, FarCliffWidthFraction = 0.35, HorizonFraction = 0.08;
        public const int FarCliffSteps = 20;
        public const double HorizonD0 = 3000, HorizonDMin = 2000;

        // ── Gaviotas (§3.4; coast_environment.dart:67-87) ──
        public const int SeagullCount = 5;
        public const uint Seagull = 0xFF2B2B2B;
        public const double SeagullFadeSeconds = 1.0, SeagullWingspan = 1.1;

        // ── Espuma (§3.4) ──
        public const double FoamPulseSeconds = 6.5;
        // WP-D1: dónde va la tira (x del lado del mar y ancho hacia la tierra) y cuánto se levanta sobre los 2 cm de la malla (a 400 m la
        // profundidad de 24 bits ya no separa 2 cm del agua).
        public const double FoamCliffX = -11.4, FoamCliffWidth = 1.6, FoamBeachX = -8.0, FoamBeachWidth = 1.6, FoamLift = 0.03;
        public const double FoamNoiseMeters = 2.5, FoamTaperMeters = 6;   // el ruido que la rompe en jirones; el apagado en el cambio de orilla
        public const uint FoamColor = 0xFFF4F6F2;

        // ── Lo que dibujan los shaders (WP-D1; 04-design-notes §2, §3, §5) ──
        // La ola sobre el color del agua se apaga de 120 a 300 m (más lejos la cresta mide menos de un píxel y titilaría).
        public const double SwellFadeNear = 120, SwellFadeFar = 300;
        // El suelo: pasto de la meseta desde 3 m sobre el agua (abajo, arena); la arena mojada × 0,62.
        public const double PlateauGrassFrom = 3, WetSandShade = 0.62;
        // El haz: #FFF59D al 18 % en la linterna, 4° de apertura, 0,4 m de ancho ahí, y el destello × 2,2 cuando apunta a la cámara (± 6°).
        public const double BeamApertureDeg = 4, BeamRootWidth = 0.4, BeamFlash = 2.2, BeamFlashDeg = 6, BeamAlpha = 0.18;

        // ── Triángulos por LOD (§3.3) ──
        public static readonly int[] CliffTris = { 1600, 450, 80 };
        public static readonly int[] LighthouseTris = { 900, 260, 40 };
        public static readonly int[] UmbrellaTris = { 140, 40, 8 };
        public const int SeagullTris = 300;
    }
}
