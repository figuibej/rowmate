using System;
using System.Text;

namespace RowMate.Core.Export
{
    // Texto dentro del TCX (AC-STR-09-09; legacy: _escapeXml, tcx_builder.dart:119-126):
    // 1. Clean quita lo que XML 1.0 no admite (DV-120): controles salvo tab, \n y \r; sustitutos sueltos; U+FFFE y U+FFFF.
    // 2. Escape escapa & → &amp; (primero), < → &lt;, > → &gt;, " → &quot;, ' → &apos;. En una sola pasada: da lo mismo que los
    //    cinco replaceAll del legacy en ese orden (el & de una entidad recién escrita no se vuelve a escapar).
    public static class TcxXml
    {
        public static string Escape(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var builder = new StringBuilder(text.Length + 16);
            AppendEscaped(builder, text);
            return builder.ToString();
        }

        public static string Clean(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    builder.Append(c).Append(text[i + 1]);
                    i++;
                    continue;
                }

                if (IsAllowed(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        // Clean + Escape sobre el StringBuilder del archivo.
        public static void AppendEscaped(StringBuilder builder, string text)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var clean = Clean(text);
            for (var i = 0; i < clean.Length; i++)
            {
                var c = clean[i];
                switch (c)
                {
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '\'':
                        builder.Append("&apos;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
        }

        // Char ::= #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] (los pares sustitutos válidos los resuelve Clean).
        private static bool IsAllowed(char c)
        {
            if (c == '\t' || c == '\n' || c == '\r')
            {
                return true;
            }

            if (c < ' ' || char.IsSurrogate(c))
            {
                return false;
            }

            return c != '\uFFFE' && c != '\uFFFF';
        }
    }
}
