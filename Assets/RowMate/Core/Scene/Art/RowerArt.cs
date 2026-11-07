using System;

namespace RowMate.Core.Scene.Art
{
    // El arte del remero, los remos, los herrajes, el agua que responde y la boya (07-remero-palada/00-arte-remero §2, §5, §8, §9;
    // ART_BIBLE §7.8, §7.9). Lo crea WP-A; desde WP-D1 su dueño es el Designer. Colores de referencia (antes de la luz de la hora:
    // los tiñe RowMateLighting) y suavidades.
    public static class RowerArt
    {
        // ── Regiones del shader RowMate/Scene/Rower: el rojo del color de vértice = región / 8 (arte §2.3, DT7-09). ──
        public const int RegionSuit = 0, RegionStripe = 1, RegionPanel = 2, RegionSkin = 3, RegionSocks = 4, RegionGlasses = 5, RegionCap = 6,
            RegionJoints = 7, RegionHair = 8;
        public const int RegionCount = 9;

        public static double RegionToRed(int region) => region / 8.0;

        public static readonly uint[] RegionColors =
        {
            0xFF1565C0, // mono [contrato]
            0xFF00B4D8, // franjas
            0xFF0D3B6E, // panel lateral
            0xFFC98E6E, // piel (D7-04)
            0xFFECEFF1, // medias y zapatillas
            0xFF101418, // anteojos
            0xFF0D3B6E, // gorra (rower.cap, apagada por defecto: DT7-09)
            0xFF263238, // articulaciones (solo Y Bot)
            0xFF3E2723, // pelo (el del legacy)
        };

        public static readonly double[] RegionSmoothness = { 0.35, 0.35, 0.35, 0.45, 0.30, 0.90, 0.30, 0.40, 0.25 };

        // El relleno del frente del remero (ART_BIBLE §7.9): 0,12 · sunColor · saturate(N·V).
        public const double RimFill = 0.12;

        // ── El shader Rower (WP-D2; 04-design-notes §2): envolvente de la tela y de la piel (arte §2.3: 0,4), el terminador de la piel
        // teñido de #E89A80, el brillo de borde del mono (#FFFFFF  %), el peso del especular (tela y piel, no plástico), el reflejo
        // del cielo en los anteojos. ──
        public const double ClothWrap = 0.25, SkinWrap = 0.4, SkinScatter = 0.22, SuitSheen = 0.12, RowerSpecular = 0.25, GlassesReflect = 0.7;
        public const uint SkinTerminator = 0xFFE89A80;

        // ── El maniquí (arte §2.1, §2.2): tope de triángulos, lados del loft, exponente de la superelipse, anillos y uniones. ──
        public const int MannequinTriangleBudget = 6000;
        public const int LoftSides = 12;
        public const double LoftExponent = 2.5;
        public const double RingSpacing = 0.035;
        public const double BlendHalfWidth = 0.04, BlendHalfWidthKneeElbow = 0.05;

        // ── El remo (arte §5.1): partes por el rojo del color de vértice = parte / 4. ──
        public const int OarGrip = 0, OarShaft = 1, OarSleeve = 2, OarBlade = 3, OarTip = 4;
        public static readonly uint[] OarColors = { 0xFF5F6B73, 0xFF1B1F24, 0xFFECEFF1, 0xFF00B4D8, 0xFFFFFFFF };
        public static readonly double[] OarSmoothness = { 0.25, 0.60, 0.40, 0.55, 0.55 };
        public const int OarTriangleBudget = 1200;

        // La punta de la pala en el marco del remo (m desde el perno: centro a 1,76 + media pala de 0,47): ahí la flexión llega a _RmOarLoad.
        public const double OarTipFromPin = 2.0;

        // ── Los herrajes (arte §5.2): cockpit, rieles, riggers de carbono, chumaceras y zapatillas; el carro negro. ──
        public const uint Rails = 0xFF9AA5AD, Carbon = 0xFF1B1F24, Oarlock = 0xFFECEFF1, Shoes = 0xFFECEFF1, Seat = 0xFF15181B, Stretcher = 0xFF263238;
        public const int FittingsTriangleBudget = 2000;

        // ── La boya de 500 m (arte §9): Ø 0,50 m que asoma 0,35, naranja metric.split con una franja blanca de 0,06 arriba del ecuador. ──
        public const uint BuoyOrange = 0xFFFF9F1C;
        public const double BuoyRadius = 0.25, BuoyAboveWater = 0.35, BuoyStripe = 0.06;
        public const int BuoyTriangleBudget = 400;

        // El plástico de la boya (arte §9: suavidad 0,7 → exponente de Blinn 2^(1 + 7·0,7)), el peso del brillo y el blanco de la franja
        // (lineal).
        public const double BuoySmoothness = 0.7, BuoySpecular = 0.45, BuoyWhite = 0.94;
        public static double BuoyGloss => Math.Pow(2, 1 + 7 * BuoySmoothness);

        // ── Tamaños mínimos en pantalla (arte §8.4; DA7-07): gota del ataque 1,5 dp, trazo de la recuperación 1,0 y caña 1,2. ──
        public const double MinDropDp = 1.5, MinDripDp = 1.0, MinShaftDp = 1.2;

        // ── El agua que responde (arte §8.3): el hervidero de 0,4 × 0,25 m con alfa 0,25 + 0,35·e; los remolinos a 0,45 m. ──
        public const double BoilLength = 0.4, BoilWidth = 0.25, BoilAlphaBase = 0.25, BoilAlphaEffort = 0.35, SwirlSeparation = 0.45;

        // ── Los charcos (WP-D2; 04-design-notes §4.1): el ojo de cada remolino (radio inicial y crecimiento por segundo, m), la ganancia de
        // la espuma sobre el alfa de Core (el quad se ve de canto desde la cámara del contrato) y el alfa del ojo oscuro. La gota del ataque
        // y de la recuperación: el núcleo (el disco llega a opaco en 1/núcleo del área). ──
        public const double SwirlEye = 0.1, SwirlEyeGrowth = 0.035, MarkFoamGain = 1.6, MarkEyeAlpha = 0.55, DropCore = 2.0;

        // ── La sombra sobre el agua con mapa (DA7-04): 0,10·ambiente en sombra plena. ──
        public const double ShadowDarken = 0.10;
    }
}
