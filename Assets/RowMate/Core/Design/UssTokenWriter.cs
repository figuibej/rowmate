using System;
using System.Collections.Generic;
using System.Text;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Genera el texto de Presentation/Styles/Tokens.uss desde los tokens (ADR 016, 02-tech-design §4.5).
    // Orden ordinal por nombre; colores opacos #RRGGBB, con alfa rgba(R, G, B, α); longitudes y tamaños en px;
    // duraciones en ms; curvas solo con palabra clave de USS; tipografía solo el tamaño (--rm-<token>-size).
    // Números con la regla de Dart (Fmt): enteros sin decimales ("14px"), el resto con DartToString ("1.5px", "0.86").
    public static class UssTokenWriter
    {
        public const string Header = "/* GENERADO por tools/tokens desde Assets/RowMate/Core/Design/Tokens.cs. NO EDITAR A MANO (ADR 016). */";

        private const string Indent = "    ";

        // Texto completo del archivo: LF y salto de línea final.
        public static string Write(IReadOnlyList<IDesignToken> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            var ordered = new List<IDesignToken>(tokens);
            ordered.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            var sb = new StringBuilder();
            sb.Append(Header).Append('\n');
            sb.Append(":root {").Append('\n');
            for (var i = 0; i < ordered.Count; i++)
            {
                var token = ordered[i];
                var value = ValueOf(token);
                if (value == null)
                {
                    continue;
                }

                var variable = VariableName(token.Name);
                if (token.Kind == TokenKind.TypeScale)
                {
                    variable += "-size";
                }

                sb.Append(Indent).Append(variable).Append(": ").Append(value).Append(';').Append('\n');
            }

            sb.Append('}').Append('\n');
            return sb.ToString();
        }

        // "text.onAccent" → "--rm-text-on-accent"; "metric.split.hud" → "--rm-metric-split-hud".
        public static string VariableName(string tokenName)
        {
            if (string.IsNullOrEmpty(tokenName))
            {
                throw new ArgumentException("Nombre de token vacío.", nameof(tokenName));
            }

            var sb = new StringBuilder("--rm-", tokenName.Length + 8);
            for (var i = 0; i < tokenName.Length; i++)
            {
                var c = tokenName[i];
                if (c == '.')
                {
                    sb.Append('-');
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    sb.Append('-').Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string? ValueOf(IDesignToken token)
        {
            switch (token)
            {
                case ColorToken color:
                    if (color.IsOpaque)
                    {
                        return color.Rgb.ToHex();
                    }

                    return "rgba(" + Fmt.Integer(color.Rgb.R) + ", " + Fmt.Integer(color.Rgb.G) + ", "
                        + Fmt.Integer(color.Rgb.B) + ", " + Number(color.Alpha) + ")";
                case LengthToken length:
                    return Number(length.Value) + "px";
                case DurationToken duration:
                    return Fmt.Integer(duration.Milliseconds) + "ms";
                case EasingToken easing:
                    return easing.UssKeyword;
                case TypeScaleToken type:
                    return Number(type.SizeDp) + "px";
                default:
                    throw new InvalidOperationException("UssTokenWriter: token desconocido " + token.Name + ".");
            }
        }

        // Entero → sin decimales ("14"); si no, toString() de Dart ("1.5", "0.86").
        private static string Number(double value)
        {
            if (value == Math.Floor(value) && Math.Abs(value) < 1e15)
            {
                return Fmt.Integer((long)value);
            }

            return Fmt.DartToString(value);
        }
    }
}
