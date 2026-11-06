namespace RowMate.Core.Scene.Art
{
    // Los números de arte del cielo (06a-escena-lago/02-tech-design §9.3; 04-design-notes §3, §4, §5, §17). Los crea WP-A con los
    // valores de las biblias y del Designer; desde WP-D1 son del Designer (excepción de dueño, §7.6). Las pruebas leen de acá,
    // nunca un literal. Lo que es contrato del legacy (las proporciones del disco y del halo, AC-SCN-11-05) tiene su prueba en
    // SkyArtTests.
    public static class SkyArt
    {
        // ── El astro que se ve (D-ART-05; DV-52, DV-54; AC-SCN-10-04, -05) ──
        public const double ArcDeg = 10.5;            // 1,5° + 10,5°·e: 12° al mediodía (C-01)
        public const double BaseDeg = 1.5;
        public const double HiddenDeg = -2.5;         // detrás del paisaje
        public const double AzimuthSpanDeg = 70;      // (xFrac − 0,5)·70°
        public const double LightMinIntensity = 0;    // 0 en el cambio de astro (C-02, DN6a-04)
        public const double MaxDegPerMinute = 0.3;    // DT6a-02 con DN6a-03

        // Ventanas de visibilidad: sol S(4,45; 5,50)·(1 − S(18,50; 19,55)); luna S(NightStart; 20,6) a la tarde y
        // S(NightEnd; 3,40) a la mañana.
        public const double SunRiseStart = 4.45, SunRiseEnd = 5.50, SunSetStart = 18.50, SunSetEnd = 19.55;
        public const double MoonRiseEnd = 20.6, MoonSetEnd = 3.40;

        // La luz principal: sol 6° + 49°·e; luna 10° + 40°·e(h'). Intensidad del sol K(h)·vis; de la luna (0,04 + 0,12·il)·vis.
        public const double SunLightBaseDeg = 6, SunLightArcDeg = 49;
        public const double MoonLightBaseDeg = 10, MoonLightArcDeg = 40;
        public const double MoonLightBase = 0.04, MoonLightPerIllumination = 0.12;

        // K(h): 0,55 hasta las 5, 1,25 a las 7, 1,75 a las 12 y 0,85 desde las 18, lineal entre ellos (04-design-notes §3.2).
        public static readonly double[] KeyHours = { 5, 7, 12, 18 };
        public static readonly double[] KeyIntensities = { 0.55, 1.25, 1.75, 0.85 };

        // ── Disco y halo (DV-63; AC-SCN-11-05, AC-SCN-10-07) ──
        public const double SunDiskRadiusDeg = 1.6;
        public const double MoonDiskScale = 0.8;      // la luna, 0,8 × el sol (contrato del legacy, sky_painter.dart:85)
        public const double HaloScale = 4;            // el halo, 4 × el disco de cada astro (sky_painter.dart:76)
        public static readonly double[] HaloAlphas = { 0.7, 0.25, 0 };   // sky_painter.dart:79-81, en 0, 0,5 y 1 del radio
        public const double MoonHaloBase = 0.4, MoonHaloPerIllumination = 0.6;   // DV-57
        public const double DiskEdgeDeg = 0.12;
        public const double SunHdrBase = 3, SunHdrPerElevation = 5, MoonHdr = 1.6;   // DN6a-21
        public const double SunLimbDarkening = 0.85;  // el borde del sol, 0,85 del centro (04-design-notes §4.2)

        // La luna (04-design-notes §4.3; DV-57, C-19): lo iluminado es S(−0,06; 0,06; n·L); el lado oscuro suma 0,06·sunColor (luz
        // cenicienta, así el disco se ubica también en luna nueva); los mares del albedo, de 0,70 a 0,80 de luminancia sobre tierras
        // altas de 0,94 (WP-D2: con 0,25-0,45 y 8,7 dp de radio la luna de CAP-04 se leía como una cara; ahora, un disco claro con manchas).
        public const double MoonTerminator = 0.06, MoonEarthshine = 0.06;
        public const double MoonMariaMin = 0.68, MoonMariaMax = 0.82, MoonHighlands = 0.94;

        // ── El telón (DV-66, DN6a-09) y la hora dorada ──
        public const double BackdropExponent = 2;     // 1 − (1 − q)²
        public const double MistBandDeg = 1.5;
        public const double GlowStrength = 0.35, GlowFalloffDeg = 14, GlowTopDeg = 20;
        public const double GoldenLowDeg = 1.5, GoldenHighDeg = 9;    // g = (1 − S(1,5°; 9°; elevación del sol))·vis_sol
        public const double DitherAmplitude = 0.5 / 255;

        // ── Estrellas (Q-62; DV-53; P-10) ──
        public const double StarsMaxAmbient = 0.85;   // con ambiente > 0,85 no hay estrellas
        public const double StarFieldCellDeg = 3, StarFieldAlphaMin = 0.12, StarFieldAlphaMax = 0.30;
        public const double StarFieldRadiusMinDp = 0.5, StarFieldRadiusMaxDp = 0.9;

        // ── Nubes (D-ART-06; DV-56, QA-6a-PAR-08) ──
        public const double CloudWrapRight = 1.3, CloudWrapLeft = 1.15;   // × el tamaño de la nube
        public const double CloudFbm = 0.18;
        public const double CloudLayerMinDeg = 2, CloudLayerMaxDeg = 9, CloudLayerCoverage = 0.35, CloudLayerAlpha = 0.6;

        // La luz de las nubes (04-design-notes §5.3): arriba (desde 0,6 del alto) + 0,10·sunColor de día; el lado del sol
        // + 0,12·sunColor·máx(0; cos Δacimut); el 40 % de abajo lerp(cuerpo; sunGlow; 0,45·g); de noche, un borde plateado de
        // 0,15·sunColor·vis de la luna si la luna está a menos de 25°.
        public const double CloudTopFrom = 0.6, CloudTopLight = 0.10, CloudSunSide = 0.12, CloudBottom = 0.4, CloudBottomGlow = 0.45;
        public const double CloudNightRim = 0.15, CloudNightRimDeg = 25;

        // La capa lejana (solo Alto; 04-design-notes §5.4): el plano de la banda mapea tan(acimut) × CloudLayerScale a la textura y
        // deriva con el viento a la mitad de las 7 (en TV, 0,0011 de textura por segundo).
        public const double CloudLayerScale = 0.5, CloudLayerDrift = 0.0011, CloudLayerHalfSpanDeg = 65;
        public const double CloudLayerEdge = 0.15;    // el borde de la banda se apaga en el 15 % de arriba y de abajo

        // ── Lo emisivo (la ventana encendida, 04-design-notes §7.3) ──
        // WP-D2: 1 (era 1,6 y después 1,25). Sin tonemapping (HourLookTable.NeutralTonemapping) el rojo de #FFE082 ya está en 1: cualquier
        // ganancia lo recorta antes del balance frío de la noche y la ventana de CAP-04 salía verde amarilla (matiz 114°). Con 1 pasa el
        // umbral del bloom de la noche (0,8) y queda cálida (MaterialArtTests.TheLitWindowDoesNotClipItsHue).
        public const double EmissiveGain = 1.0;
    }
}
