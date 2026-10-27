using System;
using System.Collections.Generic;
using System.Text;
using RowMate.Core.Text;

namespace RowMate.Core.Design
{
    // Genera el texto de Presentation/Styles/Typography.uss desde los tokens tipográficos de Tokens.cs (02-tech-design §2.9,
    // ADR 016). Lo escribe tools/tokens junto con Tokens.uss y lo verifica TypographyUssSyncTests.
    // - :root lleva la fuente de texto por defecto (Inter 400).
    // - .rm-icon / .rm-icon--filled: las fuentes de íconos (ASSET_SLOTS §4.2); el tamaño del ícono lo pone cada elemento.
    // - Por cada TypeScaleToken, .rm-type-<token> con su tamaño y su fuente (familia y peso → FontAssetPaths). Nombre de clase
    //   = nombre del token en kebab case con '.' → '-' (titleS → rm-type-title-s, num.xl → rm-type-num-xl).
    // - .rm-type-label suma letter-spacing: 8px. En UI Toolkit 6000.3 la unidad de letter-spacing es la de TextCore
    //   (N px = N/100 em por letra; QA-G2-02, DN-02): 8px = 0,08 em a cualquier tamaño, y ya escala con la letra.
    // - Por cada escala de letra distinta de 1 (TextScaleSteps): .rm-text-scale-<n> .rm-type-<token> con el tamaño × escala
    //   (dos clases: le gana a la regla base), sin repetir letter-spacing. USS no tiene line-height (D-13).
    // Orden ordinal por nombre de token, LF y salto de línea final, números con la regla de Dart (Fmt).
    public static class TypographyUssWriter
    {
        public const string Header = "/* GENERADO por tools/tokens desde Assets/RowMate/Core/Design/Tokens.cs (TypographyUssWriter). NO EDITAR A MANO (ADR 016). */";

        // Tracking de las etiquetas de métrica (ART_BIBLE §4.3): 0,08 em. El generador escribe em × 100 (unidad de TextCore).
        public const double LabelTrackingEm = 0.08;

        public const string LabelToken = "label";

        public const string LetterSpacingComment = "/* unidad de TextCore: N px = N/100 em por letra; 8px = 0,08 em a cualquier tamaño */";

        private const string Indent = "    ";

        // Desde Tokens.cs (por reflexión: solo para la herramienta y los tests, TokenCatalog).
        public static string Write()
        {
            return Write(TokenCatalog.All);
        }

        public static string Write(IReadOnlyList<IDesignToken> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            var types = new List<TypeScaleToken>();
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] is TypeScaleToken type)
                {
                    types.Add(type);
                }
            }

            types.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            var sb = new StringBuilder();
            sb.Append(Header).Append('\n');
            sb.Append('\n');
            sb.Append(":root {").Append('\n');
            Property(sb, "-unity-font-definition", Url(FontAssetPaths.For(FontFamilyRole.Ui, 400)));
            sb.Append('}').Append('\n');

            sb.Append('\n');
            sb.Append(".rm-icon {").Append('\n');
            Property(sb, "-unity-font-definition", Url(FontAssetPaths.IconsOutline));
            Property(sb, "-unity-text-align", "middle-center");
            Property(sb, "white-space", "nowrap");
            Property(sb, "letter-spacing", "0");
            sb.Append('}').Append('\n');

            sb.Append('\n');
            sb.Append(".rm-icon--filled {").Append('\n');
            Property(sb, "-unity-font-definition", Url(FontAssetPaths.IconsFilled));
            sb.Append('}').Append('\n');

            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                sb.Append('\n');
                sb.Append('.').Append(ClassName(type)).Append(" {").Append('\n');
                Property(sb, "font-size", Px(type.SizeDp));
                Property(sb, "-unity-font-definition", Url(FontAssetPaths.For(type.Family, type.Weight)));
                if (type.Name == LabelToken)
                {
                    sb.Append(Indent).Append(LetterSpacingComment).Append('\n');
                    Property(sb, "letter-spacing", LetterSpacingPx(LabelTrackingEm));
                }

                sb.Append('}').Append('\n');
            }

            var steps = TextScaleSteps.Steps;
            for (var s = 0; s < steps.Count; s++)
            {
                var step = steps[s];
                if (step == 1.0)
                {
                    continue;
                }

                var scaleClass = TextScaleSteps.ClassName(step);
                for (var i = 0; i < types.Count; i++)
                {
                    sb.Append('\n');
                    sb.Append('.').Append(scaleClass).Append(" .").Append(ClassName(types[i])).Append(" {").Append('\n');
                    Property(sb, "font-size", Px(ScaledSize(types[i], step)));
                    sb.Append('}').Append('\n');
                }
            }

            return sb.ToString();
        }

        // "rm-type-title-s" para titleS (sin el punto inicial).
        public static string ClassName(TypeScaleToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return "rm-type-" + UssTokenWriter.VariableName(token.Name).Substring("--rm-".Length);
        }

        // Tamaño en dp con una escala de letra, redondeado a centésimas como Dart (15 × 1,15 = 17,25; 52 × 1,3 = 67,6).
        public static double ScaledSize(TypeScaleToken token, double step)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return DartMath.RoundToFixed(token.SizeDp * step, 2);
        }

        // em → valor de letter-spacing de USS en la unidad de TextCore (0,08 em → "8px").
        public static string LetterSpacingPx(double em)
        {
            return Px(DartMath.RoundToFixed(em * 100, 2));
        }

        private static void Property(StringBuilder sb, string name, string value)
        {
            sb.Append(Indent).Append(name).Append(": ").Append(value).Append(';').Append('\n');
        }

        private static string Url(string assetPath)
        {
            return "url(\"project://database/" + assetPath + "\")";
        }

        private static string Px(double value)
        {
            return Number(value) + "px";
        }

        // Entero → sin decimales ("14"); si no, toString() de Dart ("12.75").
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
