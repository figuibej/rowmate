using System.Text;

namespace RowMate.Presentation.Routines
{
    // Texto del usuario en una etiqueta (AC-RUT-10-03; QA-G3-13 f; 03-rutinas/02-tech-design §4.3): se dibuja tal cual. Si el
    // motor de texto no partiera una palabra larga sin espacios, la vista insertaría U+200B (espacio de ancho cero) cada 16
    // caracteres en los tramos sin espacios de más de 16, SOLO en lo que dibuja (el dato no cambia y un nombre común no se
    // toca). La sonda S-B3a (punto 12) mide qué hace TextCore en 6000.3.27f1 y fija BreakLongRuns.
    public static class DisplayText
    {
        public const int RunLimit = 16;
        public const char ZeroWidthSpace = '​';

        // S-B3a (punto 12): TextCore 6000.3.27f1 con white-space: pre-wrap ya parte una palabra que no entra en la línea, así
        // que no hace falta insertar nada (medido: 200 W en 200 dp → 194,7 × 288; 03-rutinas/02-tech-design, Anexo A.3).
        public static bool BreakLongRuns { get; set; }

        public static string ForLabel(string text)
        {
            if (!BreakLongRuns || string.IsNullOrEmpty(text) || text.Length <= RunLimit)
            {
                return text ?? "";
            }

            var sb = (StringBuilder?)null;
            var run = 0;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (char.IsWhiteSpace(c) || c == ZeroWidthSpace)
                {
                    run = 0;
                }
                else if (++run > RunLimit)
                {
                    sb ??= new StringBuilder(text, 0, i, text.Length + text.Length / RunLimit);
                    sb.Append(ZeroWidthSpace);
                    run = 1;
                }

                sb?.Append(c);
            }

            return sb?.ToString() ?? text;
        }

        // Lo que se ve, sin los U+200B que agregó ForLabel (para comparar con el dato en las pruebas).
        public static string Strip(string shown)
        {
            return shown == null ? "" : shown.Replace(ZeroWidthSpace.ToString(), "");
        }
    }
}
