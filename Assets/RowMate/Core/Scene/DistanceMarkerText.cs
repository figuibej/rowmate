using System;
using RowMate.Core.Scene.Art;

namespace RowMate.Core.Scene
{
    // El texto del cartel de la regata (shore_painter.dart:272-285; AC-SCN-18-02; DT8-04): "<metros> m", sin separador de miles
    // (el '${prop.seed} m' del legacy), en un arreglo de caracteres propio (sin asignar: se arma al cambiar de segmento).
    public static class DistanceMarkerText
    {
        public const int MaxChars = 16;

        public static int Format(int meters, char[] into)
        {
            if (into == null)
            {
                throw new ArgumentNullException(nameof(into));
            }

            if (meters < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(meters), meters, "DistanceMarkerText: los metros del cartel no son negativos.");
            }

            var digits = 1;
            for (var v = meters; v >= 10; v /= 10)
            {
                digits++;
            }

            if (digits + 2 > into.Length)
            {
                throw new ArgumentException("DistanceMarkerText: el arreglo no alcanza.", nameof(into));
            }

            var n = meters;
            for (var i = digits - 1; i >= 0; i--)
            {
                into[i] = (char)('0' + n % 10);
                n /= 10;
            }

            into[digits] = ' ';
            into[digits + 1] = 'm';
            return digits + 2;
        }

        // Para pruebas y herramientas (asigna).
        public static string Text(int meters)
        {
            var buffer = new char[MaxChars];
            var n = Format(meters, buffer);
            return new string(buffer, 0, n);
        }
    }

    // Los 12 glifos del cartel (DT8-15): '0'–'9', ' ' y 'm', con el avance en "em" de las cifras de Barlow Semi Condensed Bold
    // (tabulares) que hornea el Editor en Art/Textures/Scene/MarkerGlyphs.png, y el ajuste del texto al tablero: cifras de
    // 0,55 m de alto centradas a 2,0 m, comprimidas de costado si hace falta para que el ancho no pase de 1,60 m (DT8-04). Sin el atlas, los
    // mismos glifos en 7 segmentos (el número siempre está, DV-82).
    public static class MarkerGlyphs
    {
        public const string Chars = "0123456789 m";
        public const int Count = 12;

        // Alto de las cifras (alto de mayúscula) en em, y los avances en em (cifras tabulares, espacio, la "m" al 70 % del alto).
        public const double CapHeightEm = 0.70;
        public const double DigitAdvanceEm = 0.52, SpaceAdvanceEm = 0.20, MAdvanceEm = 0.62;

        public static int IndexOf(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c == ' ')
            {
                return 10;
            }

            if (c == 'm')
            {
                return 11;
            }

            throw new ArgumentOutOfRangeException(nameof(c), c, "MarkerGlyphs: el cartel solo usa cifras, espacio y 'm'.");
        }

        public static double AdvanceEm(char c)
        {
            var i = IndexOf(c);
            return i < 10 ? DigitAdvanceEm : (i == 10 ? SpaceAdvanceEm : MAdvanceEm);
        }

        // El tamaño de la em en metros: el que da cifras de 0,55 m de alto (DT8-04). El alto no se achica nunca.
        public static double EmMeters() => RegattaArt.MarkerTextHeight / CapHeightEm;

        // La compresión horizontal (≤ 1) para que el texto no pase de 1,60 m de ancho (DT8-04: "10000 m" entra con el mismo alto).
        public static double HorizontalScale(ReadOnlySpan<char> text)
        {
            var natural = WidthEm(text) * EmMeters();
            return natural <= RegattaArt.MarkerTextMaxWidth ? 1.0 : RegattaArt.MarkerTextMaxWidth / natural;
        }

        public static double WidthEm(ReadOnlySpan<char> text)
        {
            var w = 0.0;
            for (var i = 0; i < text.Length; i++)
            {
                w += AdvanceEm(text[i]);
            }

            return w;
        }

        // El ancho en metros con el ajuste (≤ 1,60 m) y el alto de las cifras (siempre 0,55 m: se comprime de costado, no se achica).
        public static double WidthMeters(ReadOnlySpan<char> text) => WidthEm(text) * EmMeters() * HorizontalScale(text);

        public static double DigitHeightMeters(ReadOnlySpan<char> text) => CapHeightEm * EmMeters();

        // Los 7 segmentos de respaldo (bit 0 = a arriba, 1 = b arriba a la derecha, 2 = c abajo a la derecha, 3 = d abajo, 4 = e
        // abajo a la izquierda, 5 = f arriba a la izquierda, 6 = g medio). La 'm' es la de los visores: e, c, g y un trazo central
        // (bit 7). El espacio, ninguno.
        public static int SevenSegments(char c)
        {
            switch (c)
            {
                case '0': return 0b0111111;
                case '1': return 0b0000110;
                case '2': return 0b1011011;
                case '3': return 0b1001111;
                case '4': return 0b1100110;
                case '5': return 0b1101101;
                case '6': return 0b1111101;
                case '7': return 0b0000111;
                case '8': return 0b1111111;
                case '9': return 0b1101111;
                case 'm': return 0b1_1010100;
                case ' ': return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(c), c, "MarkerGlyphs: el cartel solo usa cifras, espacio y 'm'.");
            }
        }
    }
}
