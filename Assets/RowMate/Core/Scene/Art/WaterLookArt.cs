namespace RowMate.Core.Scene.Art
{
    // Los números visuales del agua avanzada por escenario (06b-agua-avanzada/00-arte-agua §3.2, §4.2, §6, §7; ART_BIBLE §7.7). Los
    // crea WP-A con la tabla del arte; desde que WP-A cierra son del Designer (como WaterArt en la 6a). θ es el ángulo hacia donde
    // VIAJA la onda, desde +z y positivo hacia +x (180° = hacia la cámara). Los tonos van como Color(0xFF…) del legacy.
    public static class WaterLookArt
    {
        // ── B1 (la ola del legacy sin el ruido): la pendiente k₁ con que mueve la normal ──
        public const double LakeB1Slope = 0.05, RiverB1Slope = 0.07, CoastB1Slope = 0, RegattaB1Slope = 0.03;

        // ── B2 (chop) y B3 (rizo): tile (m), deriva (m/s), θ (grados) y fuerza ──
        // WP-D (04-design-notes §2.2): el río y la costa bajan su fuerza (0,45/0,30 → 0,38/0,26 y 0,55/0,35 → 0,42/0,28): con las del arte
        // las pendientes cerca llegaban a 0,5 y el astro brillaba en casi toda el agua (una alfombra de destellos en vez del camino).
        public const double LakeB2Tile = 3.5, LakeB2Drift = 0.25, LakeB2Deg = 200, LakeB2Strength = 0.30;
        public const double LakeB3Tile = 1.3, LakeB3Drift = 0.12, LakeB3Deg = 160, LakeB3Strength = 0.20;
        public const double RiverB2Tile = 2.0, RiverB2Drift = 0.45, RiverB2Deg = 180, RiverB2Strength = 0.38;
        public const double RiverB3Tile = 0.8, RiverB3Drift = 0.30, RiverB3Deg = 135, RiverB3Strength = 0.26;
        public const double CoastB2Tile = 4.0, CoastB2Drift = 0.50, CoastB2Deg = 200, CoastB2Strength = 0.42;
        public const double CoastB3Tile = 1.5, CoastB3Drift = 0.25, CoastB3Deg = 230, CoastB3Strength = 0.28;
        public const double RegattaB2Tile = 1.5, RegattaB2Drift = 0.15, RegattaB2Deg = 190, RegattaB2Strength = 0.15;
        public const double RegattaB3Tile = 0.6, RegattaB3Drift = 0.08, RegattaB3Deg = 165, RegattaB3Strength = 0.10;

        // B4 (solo Alto): B3 otra vez a 0,4 × su tile, girado 37°, solo en z < 25 m.
        public const double B4TileScale = 0.4, B4RotationDeg = 37, B4FarZ = 25;

        // El desvanecido por distancia de cada banda (antialias): × (1 − S(cerca; lejos; z)).
        public const double B2FadeNear = 60, B2FadeFar = 250, B3FadeNear = 25, B3FadeFar = 80, B4FadeNear = 10, B4FadeFar = 25;

        // Los parches de calma ("viento" a 40 m del _RmNoiseTex): × lerp(mínimo; 1; n). La costa no tiene (1).
        public const double LakeCalmMin = 0.35, RiverCalmMin = 0.7, CoastCalmMin = 1.0, RegattaCalmMin = 0.5;
        public const double CalmPatchMeters = 40;

        // La rugosidad GGX (α) y la distorsión del reflejo k_d. WP-D: río 0,10 → 0,09 y costa 0,14 → 0,11 (el lóbulo de 1/α² = 51 abría el
        // brillo de la costa sobre todo el mar a las 12 h).
        public const double LakeRoughness = 0.05, RiverRoughness = 0.09, CoastRoughness = 0.11, RegattaRoughness = 0.04;
        public const double LakeDistortion = 0.035, RiverDistortion = 0.05, CoastDistortion = 0.06, RegattaDistortion = 0.025;

        // La corriente del río hacia la cámara (m/s; ART_BIBLE §7.7): corre el ruido de los parches.
        public const double RiverCurrent = 0.25;

        // ── La banda de orilla (Medio): x_s, H_s y tono (× el factor, × la luz de la hora); el tramo bajo (muro o talud) aparte ──
        public const double LakeLeftX = 14, LakeLeftHeight = 9;
        public const uint LakeLeftTone = 0xFF2F4A3A;          // bosque
        public const double LakeRightX = 12, LakeRightHeight = 3, LakeRightScale = 0.8;
        public const uint LakeRightTone = 0xFF4F7A3A;         // pradera
        public const double RiverBandX = 13, RiverBandHeight = 14, RiverBandScale = 0.85;
        public const uint RiverBandTone = 0xFF78909C;         // fachadas
        public const double RiverWallX = 9, RiverWallHeight = 1.2;
        public const uint RiverWallTone = 0xFF9E9E9E;
        public const double CoastLeftX = 14, CoastLeftHeight = 8;
        public const uint CoastLeftTone = 0xFF5D4A42;         // acantilado (a la derecha, mar abierto: sin banda)
        public const double RegattaBandX = 35, RegattaBandHeight = 12;
        public const uint RegattaBandTone = 0xFF2F4A3A;       // álamos
        public const double RegattaSlopeX = 9.8, RegattaSlopeHeight = 1.0;
        public const uint RegattaSlopeTone = 0xFF4F7A3A;      // talud

        // ── La estela (§6.1): la línea de cada brazo y el centro turbulento crecen con la edad; alfa 0,35·I₀·e^(−a/4) ──
        public const double WakeArmWidthBase = 0.35, WakeArmWidthPerSecond = 0.07;
        public const double WakeCenterWidthBase = 0.5, WakeCenterWidthEnd = 2.5;
        public const double WakeAlphaPeak = 0.35, WakeAlphaTau = 4;
        public const uint WakeColor = 0xFFF4F6F2;
        // WP-D (04-design-notes §3): el centro × 0,6, el corte de la espuma por punto (97 vueltas del ruido de 256 m ≈ 2,6 m) y la fase del
        // batido a lo largo (171 vueltas ≈ 1,5 m), en la z de mundo.
        public const double WakeCenterGain = 0.6, WakeBreakWaves = 97, WakeFoamWaves = 171;
        // QA-V6b-02 (ronda de verificación): la cabeza nace en la popa que se dibuja con alfa 0 y llega a pleno a 1,2 m detrás (S(0; 1,2;
        // retroceso)): sin el borde recto del primer punto. El apagado hacia la cámara del legacy, S(0; 5,3; z), va AL CUADRADO en el vértice
        // (en TV los brazos salen por los costados a z ≈ 2,5 m, donde S vale 0,45: se leían como dos haces hasta el borde).
        public const double WakeHeadLength = 1.2;

        // ── Los micro-anillos (§6.2): gotas y ataque ──
        public const double DropRingRadius0 = 0.05, DropRingGrowth = 0.35, DropRingWidth0 = 0.02, DropRingWidthGrowth = 0.03;
        public const double DropRingLife = 1.2, DropRingAlpha = 0.5;
        public const double CatchRingRadius0 = 0.15, CatchRingRadius1 = 0.9, CatchRingLife = 1.0;

        // ── Las columnas de luz (§7.1, §7.2, §7.3): ancho × largo (m) y alfa ──
        // QA-V6b-07 (AC-6B-18, ronda de verificación): los alfas × 2 (con los de WP-B la sonda daba 4 de 19 columnas más claras que sus
        // costados en el río y QA las veía "casi invisibles").
        public const double LampColumnWidth = 0.5, LampColumnLength = 8, LampColumnAlpha = 0.5;
        public const double WindowColumnWidth = 0.3, WindowColumnLength = 4, WindowColumnAlpha = 0.24, WindowColumnRange = 60;
        public const int WindowColumnMax = 16;
        public const double CabinColumnWidth = 0.3, CabinColumnLength = 5, CabinColumnAlpha = 0.3;
        public const double TowerColumnWidth = 0.5, TowerColumnLength = 8, TowerColumnAlpha = 0.4;
        public const double BeaconColumnWidth = 1.2, BeaconColumnLength = 25, BeaconColumnAlpha = 0.2, BeaconFacingDeg = 20;
        public const int LiteColumnMax = 8;
        // QA-V6b-07 (AC-6B-18): la columna bajo la x de pantalla de la luz, de 3 m de la cámara al pie, con el alfa de 0 cerca a pleno en el pie y cortada en franjas de ColumnStripeMeters.
        public const double ColumnNear = 3, ColumnNearAlpha = 0;
        public const uint LampColumnTone = 0xFFFFE082, BeaconColumnTone = 0xFFFFF59D;
        // Las franjas que cortan la columna (§7.2): 0,6 + 0,4·sin(z·2π/0,9 + fase de B2).
        public const double ColumnStripeBase = 0.6, ColumnStripeGain = 0.4, ColumnStripeMeters = 0.9;

        // ── La espuma de las crestas de la costa (§7.3, con el tope de la spec, DT6b-06) ──
        public const double CrestFoamHeightFrom = 0.55, CrestFoamHeightTo = 0.85, CrestFoamNoiseFrom = 0.45, CrestFoamNoiseTo = 0.75;
        public const double CrestFoamNear = 15, CrestFoamFar = 150, CrestFoamMax = 0.35, CrestFoamNoiseMeters = 3, CrestFoamDrift = 0.3;

        // ── #12: los charcos y el hervidero visibles a 47 dp (§6.3) ──
        public const double PuddleQuadScale = 1.3, PuddleMinAlpha = 0.65, PuddleMinAlphaUntil = 2, BoilAlphaGain = 1.5;
        // QA-V6b-03 (#12; AC-6B-17, ronda de verificación): el charco aplana el oleaje (un disco liso con el agua cercana × (1 − 0,6), alfa 1
        // × el de Core), los ojos de los remolinos × (1 − 0,85) y el borde de espuma + 0,25; el alfa mínimo hasta los 2 s, 0,35 → 0,65 (tapa los destellos de adentro; con
        // 0,35 el charco daba 1,02-1,09:1 contra el agua de alrededor). El hervidero: alfa × 1,5 (era 1,25) y 0,2 m a popa del centro de la
        // pala (era 0,1): el agua que empuja la pala sale hacia atrás y se ve debajo de la pala desde la cámara de atrás.
        public const double PuddleFlatAlpha = 1.0, PuddleFlatDarken = 0.6, PuddleEyeDarken = 0.85, PuddleEdgeGain = 0.25, BoilAftOffset = 0.2;
        public const double SwirlAngleDeg = 40, SwirlToleranceDeg = 5;

        // ── La línea de contacto del casco (§4.3) ──
        public const double ContactDarken = 0.75, ContactFalloff = 0.06;

        // ── WP-D (04-design-notes §2): el resto de las capas de WaterAdvanced ──
        // La absorción bajo la cámara (§3.4): col·(1 − Absorb·(1 − F)·(1 − S(AbsorbNear; AbsorbFar; z))).
        public const double Absorb = 0.18, AbsorbNear = 3, AbsorbFar = 25;
        // El sombreado de las facetas cerca (sin él, el agua bajo la cámara, con F ≈ 0, es un piso liso): base·(1 + Shade·(pendiente · astro)).
        public const double FacetShade = 0.8;
        // La turbidez del río (§3.4): lerp(col; #3E5A3A × ambiente; 0,20) antes del reflejo.
        public const double RiverTurbidity = 0.2;
        public const uint RiverTurbidTone = 0xFF3E5A3A;
        // El borde de la banda de orilla reflejada: el margen de alto (m) en que el tono entra; el tramo bajo, más duro.
        public const double ShoreSoftMeters = 2.0, ShoreLowSoftMeters = 0.25;
        // El brillo del astro sobre la normal perturbada (Alto y Medio, DT6b-04): exponente 1/α², los destellos × (1 + GlintGain·máscara)
        // y un velo ancho (exponente × SheenExponentScale) con SheenGain. Se recorta en 1 (el bloom de la hora hace el resplandor).
        public const double GlintGain = 2.0, SheenGain = 0.10, SheenExponentScale = 0.06;
        // La costura del horizonte (§2.8): en los últimos SeamDp sobre el horizonte, lerp(col; skyHorizon; 1 − dist/SeamDp).
        public const double SeamDp = 3;
        // Los parches de calma (§3.2): el ruido de 256 m de Core con 6 y 11 vueltas (≈ 43 y 23 m) y un corrimiento lateral.
        public const double CalmWavesA = 6, CalmWavesB = 11;
    }
}
