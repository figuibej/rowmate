namespace RowMate.Core.Scene.Art
{
    // Los agregados del agua degradada (04-design-notes §8.1; DT6a-07, ≤ 24 ALU con RM_WATER_EXTRAS): encima de los tres tramos y
    // las 10 líneas del legacy, sin moverlos (AC-SCN-13-04). Los crea WP-A; desde WP-D1 son del Designer (§7.6).
    public static class WaterArt
    {
        public const double ReflectStrength = 0.25, ReflectNear = 30, ReflectFar = 400;     // lerp(col; skyHorizon; 0,25·S(30; 400; z))
        public const uint ShallowColor = 0xFF5A6656;
        public const double ShallowStrength = 0.25, ShallowInner = 8.4, ShallowOuter = 9.0;
        public const double MistStrength = 0.6, MistNear = 10, MistFar = 150, MistGolden = 0.25;
        public const double LineThicknessDp = 1.5, LineSoftDp = 0.5;
        public const int Lines = 10;
        public const double PlaneWidth = 1600, PlaneLength = 1500;
        // It. 8: 24 de la 6a + las dos comparaciones de los saltos de la ola y del puente y su producto (27; 08-entornos-selector/04-design-notes §6).
        public const int MaxAlu = 27;
        // It. 8: la ola de la costa y la luz bajo el puente, en saltos por uniformes (el lago no los paga; 08-entornos-selector/04-design-notes §6).
        public const int MaxBranchAlu = 40;

        // Las líneas del legacy (water_painter.dart:92-103; contrato, DegradedWater las calcula igual): t = Mod(i·0,1 + fase; 1),
        // medio ancho ancho·(0,15 + 0,35·t), alfa 0,05 + 0,1·(1 − t)·fuerza.
        public const double LineSpacing = 0.1, LineHalfWidthBase = 0.15, LineHalfWidthSlope = 0.35;
        public const double ShimmerBaseAlpha = 0.05, ShimmerStrengthAlpha = 0.1;

        // La grilla del plano (WP-D1, para WaterSurface de WP-B1): los agregados se pesan por vértice (el fragmento solo los mezcla,
        // DT6a-07), así que el plano lleva columnas donde cambia la franja somera (|x| de 8,4 a 9,0) y filas donde cambian el reflejo
        // (30-400 m) y la bruma (10-150 m). En metros desde la cámara del contrato (x lateral, z hacia adelante); el plano va de
        // −PlaneWidth/2 a +PlaneWidth/2 y de −PlaneLength/2 a +PlaneLength/2, centrado en la cámara (cinta).
        public static readonly double[] GridX =
        {
            -800, -200, -60, -20, -12, -9.6, -9.0, -8.85, -8.7, -8.55, -8.4, -7, -4, 0,
            4, 7, 8.4, 8.55, 8.7, 8.85, 9.0, 9.6, 12, 20, 60, 200, 800,
        };

        public static readonly double[] GridZ =
        {
            -750, -100, -25, -5, 0, 5, 10, 20, 30, 45, 65, 90, 120, 150, 200, 260, 330, 400, 520, 750,
        };
    }
}
