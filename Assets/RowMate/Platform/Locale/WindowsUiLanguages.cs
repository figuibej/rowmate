using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RowMate.Platform.Locale
{
    // Lista de idiomas de pantalla preferidos del usuario de Windows, en orden y como etiquetas BCP 47 ("es-AR", "en-US"):
    // kernel32 GetUserPreferredUILanguages(MUI_LANGUAGE_NAME). Es la lista que Flutter Windows le pasa a la app (AC-APP-09-06).
    // Fuera de Windows (o si la llamada falla) devuelve una lista vacía.
    internal static class WindowsUiLanguages
    {
        private const uint MuiLanguageName = 0x8;

        public static IReadOnlyList<string> Read()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                uint count = 0;
                uint size = 0;
                if (!GetUserPreferredUILanguages(MuiLanguageName, out count, null, ref size) || size == 0)
                {
                    return new string[0];
                }

                var buffer = new char[size];
                if (!GetUserPreferredUILanguages(MuiLanguageName, out count, buffer, ref size))
                {
                    return new string[0];
                }

                return Split(buffer);
            }
            catch (Exception)
            {
                return new string[0];
            }
#else
            return new string[0];
#endif
        }

        // Multicadena terminada en dos '\0'.
        internal static IReadOnlyList<string> Split(char[] buffer)
        {
            var tags = new List<string>();
            var start = 0;
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] != '\0')
                {
                    continue;
                }

                if (i > start)
                {
                    tags.Add(new string(buffer, start, i - start));
                }

                start = i + 1;
            }

            return tags;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetUserPreferredUILanguages(uint flags, out uint numLanguages, [Out] char[]? languagesBuffer, ref uint bufferSize);
#endif
    }
}
