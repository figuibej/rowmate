using RowMate.Core.Design;
using UnityEngine;

namespace RowMate.Presentation.Controls
{
    // Colores de los tokens para lo que el código pone en línea (02-tech-design §3.12 regla 5): el borde superior de las
    // tarjetas con alfa k, el fundido de color de la pestaña, la muestra de R-T1. Ningún color literal (AC-APP-06-02):
    // todo sale de Tokens. Los colores de estilo de UI Toolkit van en sRGB, como los hex de los tokens.
    public static class TokenColors
    {
        public static Color ToColor(ColorToken token)
        {
            return new Color(token.Rgb.R / 255f, token.Rgb.G / 255f, token.Rgb.B / 255f, (float)token.Alpha);
        }

        // El color del token con su alfa multiplicado por factor (el acento con alfa k, DN-07).
        public static Color WithAlpha(ColorToken token, double factor)
        {
            var color = ToColor(token);
            color.a = (float)(token.Alpha * factor);
            return color;
        }

        // Interpolación en sRGB (como el fundido de color de Flutter): t ∈ [0; 1].
        public static Color Lerp(Color from, Color to, double t)
        {
            return Color.LerpUnclamped(from, to, (float)(t < 0 ? 0 : t > 1 ? 1 : t));
        }
    }
}
