namespace RowMate.Core.Scene.Art
{
    // Los números de arte del Lago alpino (06a-escena-lago/02-tech-design §9.3; 04-design-notes §6, §7, §9, §17; 00-deuda-diseno
    // §6). Los crea WP-A; desde WP-D1 son del Designer (§7.6). Los colores van como los Color(0xFF…) del legacy (ADR 016: el hex
    // de texto solo vive en Tokens.cs). Metros salvo que el nombre diga otra cosa.
    public static class LakeArt
    {
        // ── Las cordilleras (Q-65; DV-55, DV-64, DV-65; AC-SCN-19-01, -02) ──
        public const double PeakScreenFraction = 0.12;   // el hMax del legacy (lake_environment.dart:46), del alto de pantalla
        public const double MaxPeakDeg = 9;
        public const double FarPhase = 0, FarFrequency = 1.0, NearPhase = 3.1, NearFrequency = 1.6, NearHeightScale = 0.7;
        public const double FarD0 = 3500, FarDMin = 2500, NearD0 = 1200, NearDMin = 600;   // m virtuales
        public const double ApproachRate = 0.04;         // al empezar se acercan al 4 % de la velocidad del bote
        public const double SnowThreshold = 0.58, SnowEdge = 0.03, SnowNoise = 0.04, SnowNoisePeriodDeg = 1.7;
        // La semilla del ruido de las lenguas de nieve: con ella la cobertura es la del Designer (45 % en TV y 33 % en TH, DN6a-02).
        public const long SnowNoiseSeed = 37;
        // It. 6b (#3, QA-V6a-12; 00-arte-agua §10): más contraste en los flancos y menos bruma en la base (eran 0,12, −0,10 y 0,15).
        public const double FlankToSun = 0.18, FlankAway = -0.16, BaseFog = 0.10, Alpenglow = 0.35;
        // Los flancos cambian de lado en ± 6° alrededor del acimut del sol (WP-D2): con el signo seco, a las 12 h (el sol en 0°, delante del
        // pico central) quedaba una costura vertical de 4 niveles en la cordillera, justo debajo del disco.
        public const double FlankRampDeg = 6;
        // QA-V6b-05 (#3, AC-6B-04 a): el contraluz de la cordillera lejana con el sol de día delante, en un lóbulo de 12° alrededor del
        // acimut del sol (Horizon.shader, kRmBacklight y kRmBacklightLobeRad).
        public const double Backlight = 0.6, BacklightLobeDeg = 12;
        public const double BaseFraction = 0.25;          // la base (el 25 % de abajo de cada cordón) va 0,15 más hacia fog
        public const double SnowNightBase = 0.55, SnowNightPerIllumination = 0.45;   // de noche × (0,55 + 0,45·il·vis de la luna)
        // El borde encendido de la cresta (P2, DN6a-12): los últimos 0,35° en ± 8° del sol, sunGlow·0,6·g·exp(−(Δacimut/6°)²).
        public const double RimHeightDeg = 0.35, RimAzimuthDeg = 8, RimStrength = 0.6, RimFalloffDeg = 6;
        public const double StripMinAzimuthDeg = -65, StripMaxAzimuthDeg = 65;
        public const int StripColumns = 260;
        public const uint FarTone = 0xFF5C6E8A, NearTone = 0xFF2F4A3A, SnowTone = 0xFFF2F6FA;   // environment.dart / lake_environment.dart
        public const double FarDepth = 1.0, NearDepth = 0.3, SnowFog = 0.3;

        // ── El corte transversal (04-design-notes §7.1): la orilla en |x| = 9 + 0,5·n(z/41) (solo hacia afuera) ──
        public const double ShoreMeanderAmplitude = 0.5, ShoreMeanderPeriod = 41;
        public const double BottomDepth = -0.55, BottomWidth = 2.0;     // de |x| 7 a 9: el fondo bajo el agua
        public const double BeachWidth = 1.0, BeachTop = 0.27;          // de 9 a 10: playa de canto rodado
        public const double SlopeWidth = 6.0, SlopeTop = 2.50;          // de 10 a 16: talud de pasto y roca
        public const double SlopeNoise = 0.25, SlopeNoisePeriod = 6;    // ± 0,25 m en 6 m
        public const double LeftRise = 0.12, RightRise = 0.02;          // ladera boscosa y pradera alta, desde |x| = 16
        public const double FarShoreLift = 0.4;                         // el trozo lejano: la orilla a +0,4 m (DT6a-18)

        // ── Las envolventes (DN6a-14): lo agregado no las pasa nunca ──
        // It. 6b (#3): la envolvente izquierda baja de 0,20 a 0,14 (en TH el bosque cercano deja ver el cordón lejano).
        public const double LeftEnvelopeBase = 2.2, LeftEnvelopeSlope = 0.14, LeftEnvelopeFrom = 18;
        public const double RightEnvelopeBase = 2.2, RightEnvelopeSlope = 0.06, RightEnvelopeFrom = 30;
        public const double LegacyStripHalfWidth = 18;   // en |x| < 18, solo lo bajo (P-09)
        public const double LowVegetationMax = 1.2;

        // ── Colocación (04-design-notes §7.2; DT6a-08) ──
        public const double PineSink = 0.20;
        public const double PineTrunkRadius = 0.15;      // × escala
        public static readonly double[] CabinDepths = { 4, 3, 2.5 };
        public const double CabinFrontWidth = 4, CabinWallHeight = 2.6, CabinRidgeHeight = 4.2, CabinClearance = 0.3, CabinFloorLift = 0.15;
        public const double CabinEaveSide = 0.4, CabinEaveFront = 0.5;
        public const double CabinPrunedTierBase = 3.0;   // la poda: la grada de abajo arranca sobre el alero de 2,6
        public const double PierDeckHeight = 0.40, PierDeckWidth = 1.6, PierTipX = -6, PierBeachHeight = 0.30;
        public static readonly double[] PierPileX = { -8.7, -7.5, -6.3 };
        public const double PierPileRadius = 0.10, PierPileZOffset = 0.7;

        // ── El relleno (04-design-notes §7.4; DD6a-11): semillas SeedFor(id; k) + sal·104 729 ──
        public const long FillSaltPrime = 104729;
        public const int SaltReeds = 1, SaltRocks = 2, SaltForest = 3, SaltMeadow = 4, SaltGrass = 5, SaltMeander = 6;
        public const double ReedInnerX = 8.6, ReedOuterX = 9.6, ReedRootMaxY = 0.05, ReedSpacing = 0.8;
        public const double ReedMinHeight = 0.6, ReedMaxHeight = 1.2;   // DT6a-09
        public const double ReedGapMin = 2, ReedGapMax = 6, ReedGapEveryMin = 10, ReedGapEveryMax = 25;
        public const double ReedPierClearance = 3, ReedCabinClearance = 4;
        public const double RockSpacing = 7, RockMinHeight = 0.2, RockMaxHeight = 0.7, RockSink = 0.30, RockBeachSpan = 1.6;
        public const int RockVariants = 5;
        public const double ForestInnerX = 18, ForestOuterX = 45, ForestMaxRelZ = 120, ForestPoissonRadius = 6, ForestMargin = 3;
        public const int ForestCandidates = 90;
        public const double ForestBaseHeight = 6, ForestHeightSlope = 0.12, ForestMinHeight = 1.5;
        public const double MeadowInnerX = 30, MeadowOuterX = 120, MeadowSpacing = 120, MeadowJitter = 30, MeadowSpread = 9;
        public const int MeadowMinItems = 3, MeadowMaxItems = 9;
        public const double MeadowConiferMinScale = 0.4, MeadowConiferMaxScale = 0.7, MeadowBushMinHeight = 1, MeadowBushMaxHeight = 1.5;
        public const double GrassCell = 0.5, GrassMinHeight = 0.2, GrassMaxHeight = 0.6, GrassInnerX = 10, GrassSlopeOuterX = 16, GrassMeadowOuterX = 60;
        public const double FernInnerX = 16, FernOuterX = 20, FernCell = 1.0, FernMinHeight = 0.4, FernMaxHeight = 0.8;
        public const double FillCoverageBehind = 25;     // terreno y relleno desde d − 25 m (DN6a-11)

        // ── Recetas de las mallas (04-design-notes §7.3, §7.4, §9; AC-SCN-15-06) ──
        public const double PineHeight = 6.0, PineMaxHalfWidth = 1.3, PineTrunkVisible = 1.0, PineTrunkTopRadius = 0.05;
        public static readonly double[] PineTierBase = { 0.8, 2.3, 3.8 };
        public static readonly double[] PineTierTip = { 3.0, 4.5, 6.0 };
        public static readonly double[] PineTierHalfWidth = { 1.3, 1.0, 0.7 };
        public const int PineLobesMin = 9, PineLobesMax = 12;
        public const double PineLobeMin = 0.85, PineLobeMax = 1.08, PineLobeDroop = 0.25, PineValley = 0.80, PineMaxTiltDeg = 2;
        public const double PineOcclusionInner = 0.55;
        public static readonly int[] PineTriangleBudget = { 1200, 400, 120 };
        public static readonly int[] CabinTriangleBudget = { 1200, 300 };
        public static readonly int[] PierTriangleBudget = { 600, 150 };
        public static readonly int[] RockTriangleBudget = { 80, 24 };
        public static readonly int[] ReedTriangleBudget = { 24, 8 };
        public static readonly int[] GrassTriangleBudget = { 6, 3 };
        public const int HullTriangleBudget = 6000;
        public const int ReedBladesMin = 10, ReedBladesMax = 14;
        public const int GrassBlades = 6;
        public const double DoorWidth = 0.8, DoorHeight = 1.4, WindowLeft = 0.7, WindowRight = 1.5, WindowBottom = 1.3, WindowTop = 2.0;
        public const double PlankWidth = 0.20;

        // El casco provisorio (ART_BIBLE §7.8; 04-design-notes §9).
        public const double HullStern = 5.3, HullBow = 13.5, HullBeamWaterline = 0.29, HullBeamGunwale = 0.33, HullFreeboard = 0.10;
        public const double HullDraft = 0.05, HullBowBall = 0.04;
        public const int HullSections = 24;

        // It. 6b (#85; AC-6B-21; 00-arte-agua §9): la cubierta va de #F5F5F5 a #C9D1D6 en los últimos 2,5 m hacia la popa y los últimos
        // 0,6 m se afinan hasta 0,06 m de manga con la cubierta 0,03 m más baja (la aleta). Casco, borda y cockpit sin cambios.
        public const uint HullDeckStern = 0xFFC9D1D6;
        public const double HullDeckGradientLength = 2.5, HullSternTaperLength = 0.6, HullSternTipBeam = 0.06, HullSternDeckDrop = 0.03;

        // El cockpit agrandado y corrido (It. 7, DA7-02): de z 9,05 a 10,45 (−0,45 / +0,95 del perno).
        public const double CockpitStartZ = 9.05, CockpitEndZ = 10.45;

        public static bool IsCockpit(double z) => z >= CockpitStartZ && z <= CockpitEndZ;

        // ── Colores de referencia (antes de la luz de la hora; shore_painter.dart:89-131; 04-design-notes §7.3-§7.5, §9) ──
        public const uint Trunk = 0xFF5D4037;
        public static readonly uint[] PineGreens =
        {
            0xFF1B5E20, 0xFF2E7D32, 0xFF388E3C,
            0xFF145A32, 0xFF1E8449, 0xFF27AE60,
            0xFF2E5E2A, 0xFF3E7B38, 0xFF4F9A48,
        };
        public const uint CabinWood = 0xFF6D4C41, CabinRoof = 0xFF4E342E, CabinDoor = 0xFF3E2723, CabinFrame = 0xFFD7CCC8, CabinPlinth = 0xFF6B6A66;
        public const uint WindowDay = 0xFF90CAF9, WindowNight = 0xFFFFE082;
        public const uint PierDeck = 0xFF8D6E63, PierPile = 0xFF5D4037;
        public const uint Pebbles = 0xFF7A746B, WetBand = 0xFF4E4A45, Grass = 0xFF4F7A3A, GrassAlt = 0xFF6B8A3E, ForestFloor = 0xFF4A3B2C;
        public const uint Rock = 0xFF6B6A66, RockAlt = 0xFF7A746B, Canopy = 0xFF2A4A2C, Bush = 0xFF4F6B35;
        public const uint Needles = 0xFF5A4632, Contact = 0xFFC9D2D4, Fern = 0xFF3F6A35;   // agujas del bosque, línea de contacto, helecho
        public const uint ReedBase = 0xFF6E7F4A, ReedTip = 0xFFA39E6A;                      // juncos: de la base a las puntas
        public const uint HullWhite = 0xFFF5F5F5, HullBottom = 0xFFB0BEC5, HullStripe = 0xFF00B4D8, HullCockpit = 0xFF263238;
    }
}
