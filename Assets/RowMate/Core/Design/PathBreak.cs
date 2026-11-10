using System;
using System.Text;

namespace RowMate.Core.Design
{
    // Puntos de corte en una ruta para el aviso de exportación (It. 14, #69, AC-DEBT-05; DT14-29): mete un espacio de ancho cero
    // (U+200B) después de cada '\' (y '/') para que el texto pueda partirse ahí, salvo cuando lo que sigue hasta el próximo
    // separador (o el final) tiene una sola letra: así nunca queda un renglón con una sola letra. Sin separadores devuelve el
    // mismo texto.
    public static class PathBreak
    {
        public const char ZeroWidthSpace = '​';

        public static string Insert(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.IndexOf('\\') < 0 && text.IndexOf('/') < 0)
            {
                return text;
            }

            var builder = new StringBuilder(text.Length + 8);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                builder.Append(c);
                if ((c == '\\' || c == '/') && SegmentLength(text, i + 1) > 1)
                {
                    builder.Append(ZeroWidthSpace);
                }
            }

            return builder.ToString();
        }

        // Quita los cortes (para comparar con el texto del catálogo).
        public static string Strip(string text)
        {
            return text == null ? throw new ArgumentNullException(nameof(text)) : text.Replace(ZeroWidthSpace.ToString(), "");
        }

        private static int SegmentLength(string text, int start)
        {
            var end = start;
            while (end < text.Length && text[end] != '\\' && text[end] != '/' && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            return end - start;
        }
    }
}
