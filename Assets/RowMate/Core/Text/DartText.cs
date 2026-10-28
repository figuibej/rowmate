using System;

namespace RowMate.Core.Text
{
    // Recorte de texto con la semántica de String.trim() de Dart (ADR 012). El legacy recorta el nombre, la descripción y
    // cada campo numérico del editor con trim() (lib/features/routines/routine_editor_screen.dart:42, :63, :173, :535-559).
    // Espacios de Dart (<flutter-sdk>/bin/cache/dart-sdk/lib/_internal/vm/lib/string_patch.dart:463-499): 9-13, 32, U+0085,
    // U+00A0, U+1680, U+2000-U+200A, U+2028, U+2029, U+202F, U+205F, U+3000 y U+FEFF (BOM). string.Trim() de .NET no trata
    // U+FEFF como espacio y sí trata otros (U+180E en .NET Framework), por eso no se usa.
    public static class DartText
    {
        // = _StringBase._isTwoByteWhitespace (el juego de las cadenas de un byte es el mismo, recortado a ≤ U+00FF).
        public static bool IsWhitespace(char c)
        {
            int code = c;
            if (code <= 0x20)
            {
                return code == 0x20 || (code >= 0x09 && code <= 0x0D);
            }

            if (code < 0x85)
            {
                return false;
            }

            if (code == 0x85 || code == 0xA0)
            {
                return true;
            }

            return code <= 0x200A
                ? code == 0x1680 || code >= 0x2000
                : code == 0x2028 || code == 0x2029 || code == 0x202F || code == 0x205F || code == 0x3000 || code == 0xFEFF;
        }

        // = text.trim(): sin espacios de Dart a los costados; devuelve el mismo texto si no hay nada que quitar.
        public static string Trim(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var first = 0;
            while (first < text.Length && IsWhitespace(text[first]))
            {
                first++;
            }

            if (first == text.Length)
            {
                return "";
            }

            var last = text.Length - 1;
            while (last > first && IsWhitespace(text[last]))
            {
                last--;
            }

            return first == 0 && last == text.Length - 1 ? text : text.Substring(first, last - first + 1);
        }
    }
}
