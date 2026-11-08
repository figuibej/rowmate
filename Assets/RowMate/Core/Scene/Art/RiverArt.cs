namespace RowMate.Core.Scene.Art
{
    // Los números de arte del Río urbano (08-entornos-selector/00-arte-entornos §2; 02-tech-design §2.4, §2.4.1, §2.6). Los
    // crea WP-A; desde WP-D1 son del Designer. Colores como los Color(0xFF…) del legacy (ADR 016). Metros salvo que diga otra cosa.
    public static class RiverArt
    {
        // ── El corte (§2.1; AC-SCN-16-04) ──
        public const double WallX = 9.0;              // el muro de contención, plomo
        public const double QuayHeight = 1.20;        // el malecón sobre el agua
        public const double WallUnderwater = 0.5;     // lo que se ve del muro bajo el agua
        public const double CopingWidth = 0.35, CopingHeight = 0.15, CopingOverhang = 0.08;
        public const double QuayEndX = 12.5;          // el paseo hasta acá; después la vereda de las fachadas
        public const double CityEndX = 120;
        public const double RiverBed = -0.6;          // el fondo bajo el agua del canal
        public const uint WallStone = 0xFF9E9E9E, CopingStone = 0xFFB5AFA4, Cobble = 0xFF8A8378;

        // ── Colocación sin cruces (DT8-19, §2.4.1) ──
        public const double FaceMinX = 10.1;
        public const double HouseGap = 1.0;
        public const double BridgeEndX = 12.5, BridgeClearX = 12.6, BridgeClearZ = 0.5;
        public const double RampMinX = 8.5, RampMaxX = 10.0, RampLength = 6.6, RampSlopeDeg = 12, RampBottomY = -0.2;
        public const double LampClear = 0.5;
        public const double FillFromX = 22;

        // ── Edificio (§2.3; shore_painter.dart:133-148) ──
        public const double BuildingWidth = 6, BuildingDepth = 10, FloorHeight = 3;
        public const double WindowWidth = 1.0, WindowHeight = 1.2, WindowX0 = -2.4, WindowStepX = 1.8, WindowSill = 0.8;
        public const int WindowsPerFloor = 3, RiverWindowsPerFloor = 5;
        public const double PlinthHeight = 0.6, PlinthShade = 0.8, CorniceHeight = 0.30, CorniceOut = 0.2, ParapetHeight = 0.5;
        public const double MansardShare = 0.4;   // 40 % de los techos con mansarda (por semilla)
        public const double MansardHeight = 2.2;
        public static readonly uint[] BuildingTones = { 0xFF78909C, 0xFF8D6E63, 0xFFB0BEC5 };   // seed mod 3
        public const uint WindowGlass = 0xFF263238, WindowLit = 0xFFFFE082, MansardSlate = 0xFF4A5560;
        public const double WindowLitThreshold = 0.6;

        // ── Club de remo (shore_painter.dart:150-162) ──
        public const double BoathouseWidth = 8, BoathouseDepth = 12, BoathouseWall = 4, BoathouseRidge = 6, BoathouseEave = 0.5;
        public const double GateWidth = 3, GateHeight = 2.8, StripeBottom = 3.6, StripeTop = 4.0;
        public const uint BoathouseWallTone = 0xFFB0BEC5, BoathouseRoof = 0xFF546E7A, BoathouseGate = 0xFF37474F, BoathouseStripe = 0xFF1565C0;

        // ── Farol (shore_painter.dart:191-200) ──
        public const double LampHeight = 4.0, LampGlobeY = 4.1, LampGlobeRadius = 0.30, LampHaloRadius = 1.5;
        public const double LampPostRadiusBottom = 0.06, LampPostRadiusTop = 0.04, LampBaseHeight = 0.3;
        public const uint LampPost = 0xFF455A64, LampGlobeDay = 0xFF90A4AE, LampGlobeNight = 0xFFFFF8E1, LampHalo = 0xFFFFE082;
        public const double LampHaloAlpha = 0.35;

        // ── Boya (shore_painter.dart:184-189) ──
        public const double BuoyRadius = 0.25, BuoyCenterY = 0.2;
        public static readonly uint[] BuoyTones = { 0xFFE53935, 0xFFFDD835, 0xFFECEFF1 };   // seed mod 3

        // ── Puente (shore_painter.dart:164-182; AC-SCN-16-03) ──
        public const double BridgeSpringX = 8.5, BridgeSpringY = 1.5, BridgeCrownControlY = 8.5;   // la Bézier: clave = 5,0 m
        public const double BridgePierOuterX = 9.5, DeckBottom = 6.0, DeckTop = 7.2, ParapetTop = 7.7, DeckHalfWidth = 12;
        public const double BridgeDepth = 10, BridgeSideArch = 3;
        public const uint BridgeFace = 0xFF9E9E9E, BridgePier = 0xFF757575, BridgeDeck = 0xFF7A7A7A, BridgeParapet = 0xFF616161;

        // ── Bajo el puente (DT8-07, DT8-08) ──
        public const double UnderBridgeDim = 0.4, UnderBridgeRamp = 1.5;
        public const double TrailingBehind = 15;

        // ── Lo que dibujan los shaders (WP-D1; 04-design-notes §3 a §5) ──
        // El adoquín del malecón: 6 piedras por período del ruido fino (1,3/6 = 0,217 m), así el aparejo sigue la fase de d de Core sin nadar
        // (Q-68); juntas al 30 % y ± 6 % de tono por piedra.
        public const double CobbleSize = MaterialArt.NoiseFineMeters / 6, CobbleJoint = 0.3, CobbleTone = 0.12;
        // El halo de un farol se adelanta 0,6 × su radio hacia la cámara (el globo no lo corta).
        public const double GlowPull = 0.6;

        // ── Skyline (§2.2; river_environment.dart:46-71) ──
        public const uint SkylineTone = 0xFF3A4660, SkylineWindow = 0xFFFFE08A;
        public const double SkylineDepth = 0.8, SkylineWindowAlpha = 0.85, SkylineWindowThreshold = 0.5;
        public const double HorizonFraction = 0.12, HorizonD0 = 1500, HorizonDMin = 1000;

        // ── Manzanas de relleno (§2.2 M) ──
        public const double BlockFrontMin = 12, BlockFrontMax = 30, BlockDepthMin = 15, BlockDepthMax = 30;
        public const int BlockFloorsMin = 3, BlockFloorsMax = 9;
        public const double BlockToneScale = 0.85, BlockWindowsLit = 0.35;

        // ── Triángulos por LOD (§2.3) ──
        public static readonly int[] BuildingTris = { 1500, 300, 24 };
        public static readonly int[] BoathouseTris = { 900, 250, 30 };
        public static readonly int[] LampTris = { 160, 48, 4 };
        public static readonly int[] BuoyTris = { 80, 32, 8 };
        public static readonly int[] BridgeTris = { 3600, 1000, 200 };
        public static readonly int[] CityBlockTris = { 24, 24, 24 };
    }
}
