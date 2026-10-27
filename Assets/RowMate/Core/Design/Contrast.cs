using System;

namespace RowMate.Core.Design
{
    // Contraste WCAG 2.x con el método de 02-tech-design §4.4 (respuesta a P-05): composición en sRGB con el
    // alfa SIN redondear y canales en double (sin pasar por bytes), luminancia con el umbral 0,04045 y relación
    // sin redondear. Canales en la escala 0-255.
    public static class Contrast
    {
        private static readonly (double R, double G, double B) White = (255.0, 255.0, 255.0);

        // Luminancia relativa WCAG de un color sRGB (canales 0-255 en double).
        public static double RelativeLuminance(double r, double g, double b)
        {
            return 0.2126 * Linearize(r) + 0.7152 * Linearize(g) + 0.0722 * Linearize(b);
        }

        // (L1 + 0,05) / (L2 + 0,05) con L1 ≥ L2, sin redondear.
        public static double Ratio(double luminanceA, double luminanceB)
        {
            var lighter = Math.Max(luminanceA, luminanceB);
            var darker = Math.Min(luminanceA, luminanceB);
            return (lighter + 0.05) / (darker + 0.05);
        }

        // α·frente + (1 − α)·fondo por canal, en sRGB, sin redondear (como mezcla Flutter/Skia y el legacy).
        public static (double R, double G, double B) CompositeSrgb(ColorToken over, (double R, double G, double B) under)
        {
            if (over == null)
            {
                throw new ArgumentNullException(nameof(over));
            }

            var a = over.Alpha;
            return (a * over.Rgb.R + (1 - a) * under.R,
                    a * over.Rgb.G + (1 - a) * under.G,
                    a * over.Rgb.B + (1 - a) * under.B);
        }

        // Mezcla en espacio lineal y vuelta a sRGB (0-255). Informativo: es lo que haría Unity con la UI en un
        // proyecto Linear (R-T1); el umbral se aplica a CompositeSrgb.
        public static (double R, double G, double B) CompositeLinear(ColorToken over, (double R, double G, double B) under)
        {
            if (over == null)
            {
                throw new ArgumentNullException(nameof(over));
            }

            var a = over.Alpha;
            return (Encode(a * Linearize(over.Rgb.R) + (1 - a) * Linearize(under.R)),
                    Encode(a * Linearize(over.Rgb.G) + (1 - a) * Linearize(under.G)),
                    Encode(a * Linearize(over.Rgb.B) + (1 - a) * Linearize(under.B)));
        }

        // Peor fondo del vidrio del HUD sin desenfoque: glass.fallback sobre blanco = (41,72; 46,88; 54,62) ≈ #2A2F37.
        public static (double R, double G, double B) WorstGlassBackground()
        {
            return CompositeSrgb(Tokens.GlassFallback, White);
        }

        // Color opaco como tupla de canales 0-255.
        public static (double R, double G, double B) Channels(Rgb rgb)
        {
            return (rgb.R, rgb.G, rgb.B);
        }

        // Relación de contraste de un frente (con o sin alfa) sobre un fondo ya compuesto.
        public static double RatioOver(ColorToken front, (double R, double G, double B) background)
        {
            var composed = CompositeSrgb(front, background);
            return Ratio(RelativeLuminance(composed.R, composed.G, composed.B),
                         RelativeLuminance(background.R, background.G, background.B));
        }

        private static double Linearize(double channel)
        {
            var c = channel / 255.0;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        private static double Encode(double linear)
        {
            var c = linear <= 0.0031308 ? linear * 12.92 : 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
            return c * 255.0;
        }
    }
}
