using System;

namespace RowMate.Core.Design
{
    // El tinte del vidrio desenfocado del HUD y del diálogo (ART_BIBLE §3.2; P-04; F-WKT-17; AC-6B-23, -24; DT6b-09): #070D16 con
    // α = lerp(0,72; 0,86; S(0,10; 0,70; L)) por píxel, con L la luminancia del fondo desenfocado TAL COMO LA VE EL PANEL (el valor sRGB
    // 0-1 de un gris, el espacio del panel gamma y de Contrast; 02-tech-design §4.4). El compuesto es el de Contrast.CompositeSrgb
    // (sRGB, sin redondear): con L = 1 es exactamente el respaldo glass.fallback (@86 %) sobre blanco, y es monótono en L, así que el
    // peor contraste es el de hoy (13,7:1 al blanco). Con la mezcla en lineal no lo sería (5,4:1 al blanco): por eso el L y la mezcla
    // del shader (WP-D, HudBlur) van en el espacio del panel.
    public static class GlassTint
    {
        public const double AlphaMin = 0.72, AlphaMax = 0.86, LumaFrom = 0.10, LumaTo = 0.70;

        // QA-V6b-04 (AC-6B-27, ronda de verificación): el L del α es el de la ZONA (la media del fondo desenfocado a ± RegionTexels texeles
        // del vidrio, en diagonal) para lo que es MÁS CLARO que la zona, salvo que sea mucho más claro: L_α = máx(L_zona; L − RegionLumaSlack);
        // lo más oscuro que la zona sigue con su α (las sombras de la orilla se aplastan como antes, AC-6B-23). Con el α del píxel, el
        // compuesto era casi plano entre L 0,4 y 0,7 (el α sube con L y se come la diferencia): la popa blanca del bote detrás del diálogo
        // quedaba a +4-5 % del agua. Con el de la zona, el α es parejo y la popa se ve (+20 %). El compuesto nunca pasa el del peor caso
        // (L = 1 con su α, el respaldo sobre blanco): el contraste de AC-6B-24 no cambia (RegionalCompositeNeverPassesTheWorstCase).
        public const double RegionLumaSlack = 0.25;
        public const int RegionTexels = 8;

        // El α con el L de la zona (el que usa HudBlur).
        public static double RegionalAlpha(double pixelL, double regionL)
        {
            var p = Clamp01(pixelL);
            var r = Clamp01(regionL);
            return Alpha(p > r ? Math.Max(r, p - RegionLumaSlack) : p);
        }

        // El L (sRGB 0-1) del compuesto de un fondo gris de valor L con el α de la zona.
        public static double RegionalCompositeValue(double pixelL, double regionL)
        {
            var a = RegionalAlpha(pixelL, regionL);
            var tint = Tokens.GlassFallback.Rgb;
            var luma = (0.2126 * tint.R + 0.7152 * tint.G + 0.0722 * tint.B) / 255.0;
            return a * luma + (1 - a) * Clamp01(pixelL);
        }

        public static double Alpha(double backgroundL)
        {
            var l = Clamp01(backgroundL);
            var t = Clamp01((l - LumaFrom) / (LumaTo - LumaFrom));
            var s = t * t * (3 - 2 * t);
            return AlphaMin + (AlphaMax - AlphaMin) * s;
        }

        // El compuesto (sRGB 0-255) del tinte sobre un fondo gris de valor L.
        public static (double R, double G, double B) Composite(double backgroundL)
        {
            var l = Clamp01(backgroundL) * 255.0;
            var a = Alpha(backgroundL);
            var tint = Tokens.GlassFallback.Rgb;
            return (a * tint.R + (1 - a) * l, a * tint.G + (1 - a) * l, a * tint.B + (1 - a) * l);
        }

        // La luminancia relativa WCAG del compuesto: monótona en L, con L = 1 la del respaldo sobre blanco.
        public static double CompositeL(double backgroundL)
        {
            var c = Composite(backgroundL);
            return Contrast.RelativeLuminance(c.R, c.G, c.B);
        }

        // El contraste de un texto (sRGB 0-255) sobre el vidrio con ese fondo.
        public static double ContrastOver(double backgroundL, (double R, double G, double B) text)
        {
            return Contrast.Ratio(Contrast.RelativeLuminance(text.R, text.G, text.B), CompositeL(backgroundL));
        }

        public static double ContrastOver(double backgroundL, ColorToken text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return ContrastOver(backgroundL, (text.Rgb.R, text.Rgb.G, text.Rgb.B));
        }

        private static double Clamp01(double x)
        {
            if (double.IsNaN(x) || x < 0)
            {
                return 0;
            }

            return x > 1 ? 1 : x;
        }
    }
}
