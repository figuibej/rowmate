using System.Collections.Generic;

namespace RowMate.Core.Common
{
    // La cadena de un solo carácter, sin asignar en cada dibujo (O-4 de la lente de motion de la It. 5:
    // 05-series-parciales/05-qa-lens-motion §6; 06a-escena-lago/00-deuda-5 §3). La usan los textos que se dibujan carácter por
    // carácter (Presentation/Controls: DrawnFont y TabularNumber), en cada repintado.
    // - Latin-1 (U+0000-U+00FF: el español y el inglés, el × y el ·), la puntuación general (U+2000-U+206F: –, —, …, los espacios
    //   finos) y el signo menos (U+2212) se arman una vez, al cargar la clase: 369 cadenas, en lugar de las 8231 que DrawnFont
    //   armaba hasta U+2026 (unos 300 KB fijos).
    // - Cualquier otro carácter se arma la primera vez que se pide y queda guardado: a lo sumo una asignación por carácter en toda
    //   la vida de la app (antes, TabularNumber asignaba en cada dibujo todo lo que pasaba de U+00FF, y DrawnFont lo que pasaba de
    //   U+2026).
    // Hilo principal (la UI); el diccionario no es seguro entre hilos.
    public static class CharStrings
    {
        public const char PunctuationFirst = '\u2000';
        public const char PunctuationLast = '\u206F';
        public const char Minus = '\u2212';

        private static readonly string[] Latin1 = Build('\u0000', '\u00FF');
        private static readonly string[] Punctuation = Build(PunctuationFirst, PunctuationLast);
        private static readonly Dictionary<char, string> Others = new Dictionary<char, string> { { Minus, Minus.ToString() } };

        // Las que se arman al cargar la clase.
        public const int Prebuilt = 256 + (PunctuationLast - PunctuationFirst + 1) + 1;

        // Las que se armaron después, a pedido (fuera de Latin-1, la puntuación general y el signo menos).
        public static int OnDemand => Others.Count - 1;

        public static string Of(char c)
        {
            if (c <= '\u00FF')
            {
                return Latin1[c];
            }

            if (c >= PunctuationFirst && c <= PunctuationLast)
            {
                return Punctuation[c - PunctuationFirst];
            }

            if (!Others.TryGetValue(c, out var text))
            {
                text = c.ToString();
                Others[c] = text;
            }

            return text;
        }

        private static string[] Build(char first, char last)
        {
            var strings = new string[last - first + 1];
            for (var i = 0; i < strings.Length; i++)
            {
                strings[i] = ((char)(first + i)).ToString();
            }

            return strings;
        }
    }
}
