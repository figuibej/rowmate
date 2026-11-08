namespace RowMate.Core.Scene.Art
{
    // Los números de arte del Canal de regata (08-entornos-selector/00-arte-entornos §4; 02-tech-design §2.4, §2.6; DT8-03,
    // DT8-04, DT8-13). Los crea WP-A; desde WP-D1 son del Designer. Metros salvo que diga otra cosa.
    public static class RegattaArt
    {
        // ── El corte (§4.1; AC-SCN-18-04, DT8-13) ──
        public const double BankFromX = 9.8, BankSlopeDeg = 25, BankHeight = 1.0, LawnEndX = 1500;
        public const double ChannelBed = -0.8;
        public const double PoplarFromX = 30, PoplarToX = 40, PoplarStep = 12;
        public const uint Lawn = 0xFF5E8C3E;
        // El césped cortado: franjas de 3 m a lo largo del canal, ± 4 % (WP-D1; 04-design-notes §3).
        public const double LawnStripeWidth = 3, LawnStripeContrast = 0.04;

        // ── Boyas (shore_painter.dart:184-189; AC-SCN-18-01) ──
        public static readonly uint[] BuoyTones = { 0xFFE53935, 0xFFFDD835, 0xFFECEFF1 };
        public const double BuoyPitch = 0.08, BuoyPitchRate = 1.7, BuoyHeave = 0.02, BuoyHeaveRate = 1.3;

        // ── Cartel (shore_painter.dart:266-285; DT8-04) ──
        public const double MarkerPost = 1.5, MarkerBoardWidth = 1.8, MarkerBoardHeight = 1.0, MarkerBoardBottom = 1.5, MarkerBoardThickness = 0.06;
        public const double MarkerTextHeight = 0.55, MarkerTextCenterY = 2.0, MarkerTextMaxWidth = 1.60;
        public const uint MarkerBoard = 0xFFFAFAFA, MarkerPostTone = 0xFF9E9E9E;
        public const double MarkerTextAlpha = 0.87;

        // ── Tribuna (shore_painter.dart:236-242; DT8-03) ──
        public const double StandLength = 12, StandDepth = 6, StepHeight = 1.2, RoofY = 5.3, RoofThickness = 0.5, RoofOverhang = 0.5;
        public const int Steps = 4;
        public const uint StandEven = 0xFF8D9DAE, StandOdd = 0xFF6E7B8C, StandRoof = 0xFF37474F;
        public const double CrowdOccupancy = 0.7, CrowdWidth = 0.5, CrowdHeight = 1.0;
        // El público se mueve apenas (QA-V8-01; ANIMATION_BIBLE §6): se mece de costado y se levanta un poco en la mitad de su período.
        public const double CrowdSway = 0.04, CrowdBob = 0.06;
        public static readonly uint[] CrowdClothes = { 0xFFE53935, 0xFF1E88E5, 0xFFFFB300, 0xFFFAFAFA, 0xFF263238, 0xFF43A047 };

        // ── Torre (shore_painter.dart:244-251) ──
        public const double TowerWidth = 3, TowerHeight = 10, BandBottom = 7.8, BandTop = 9.0, ClockY = 6.5, ClockRadius = 0.6, Antenna = 1.5;
        public const uint Tower = 0xFFCFD8DC, TowerBand = 0xFF263238, Clock = 0xFFFFFFFF, Mast = 0xFF90A4AE;

        // ── Bandera (shore_painter.dart:253-265) ──
        public const double FlagMast = 5, FlagLength = 1.6, FlagBottom = 4.0, FlagWave = 0.2, FlagWaveRate = 4;
        public static readonly uint[] FlagTones = { 0xFFE53935, 0xFFFFB300, 0xFF1E88E5, 0xFF43A047 };   // seed mod 4

        // ── Horizonte (§4.2; regatta_environment.dart:43-71) ──
        public const uint Land = 0xFF55704A, BackdropStand = 0xFF6E7B8C, BackdropTower = 0xFFB8C0CC;
        public const double LandDepth = 0.9, BackdropDepth = 0.8, FlagDepth = 0.5, HorizonFraction = 0.06;
        public const double HorizonD0 = 1500, HorizonDMin = 1000;

        // ── Triángulos por LOD (§4.3) ──
        public static readonly int[] BuoyTris = { 96, 40, 8 };
        public static readonly int[] MarkerTris = { 80, 20, 2 };
        public static readonly int[] GrandstandTris = { 1200, 300, 40 };
        public static readonly int[] TowerTris = { 600, 160, 24 };
        public static readonly int[] FlagTris = { 90, 30, 6 };
        public static readonly int[] PoplarTris = { 600, 150, 4 };
    }
}
