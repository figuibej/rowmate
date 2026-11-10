using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RowMate.Core.Localization
{
    // ¿Este texto visible sale de un mensaje del catálogo del idioma activo? (It. 14, AC-APP-11-01 (a); DT14-25). Una expresión
    // por mensaje: literales escapados, cada {param} → ".+?" y cada plural ICU expandido a sus variantes (con # → un número). Un
    // mensaje del OTRO idioma no coincide (así se detecta un texto en español con la app en inglés). UnitTokens son las palabras
    // cortas que el catálogo pone pegadas a un número o a un parámetro (W, m, spm, s…): un texto con solo cifras y esas palabras
    // es un número con su unidad (categoría b).
    public sealed class CatalogMatcher
    {
        private const int MaxUnitLength = 4;

        // Los mensajes que solo oye el lector de pantalla (a11yLabelValue, a11yStepProgress, a11yChartSummary): nunca se ven, y
        // "{label}: {value}" describiría cualquier texto con dos puntos. No cuentan como catálogo visible.
        public const string ReaderOnlyPrefix = "a11y";

        private static readonly ISet<string> NoIntegers = new HashSet<string>();

        private readonly List<KeyValuePair<string, Regex>> _messages = new List<KeyValuePair<string, Regex>>();
        private readonly HashSet<string> _units = new HashSet<string>(StringComparer.Ordinal);

        public CatalogMatcher(LocalizationCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            Locale = catalog.Locale;
            foreach (var key in catalog.Keys)
            {
                var message = catalog.Message(key);
                CollectUnits(message, _units);
                if (key.StartsWith(ReaderOnlyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var integers = new HashSet<string>(StringComparer.Ordinal);
                foreach (var placeholder in catalog.Placeholders(key))
                {
                    if (placeholder.Type == PlaceholderInfo.IntType)
                    {
                        integers.Add(placeholder.Name);
                    }
                }

                _messages.Add(new KeyValuePair<string, Regex>(key, new Regex("^(?:" + ToPattern(message, integers) + ")$", RegexOptions.Singleline | RegexOptions.CultureInvariant)));
            }
        }

        public string Locale { get; }

        public IReadOnlyCollection<string> UnitTokens => _units;

        // La clave del primer mensaje que describe el texto entero, o null.
        public string? Match(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            for (var i = 0; i < _messages.Count; i++)
            {
                if (_messages[i].Value.IsMatch(text))
                {
                    return _messages[i].Key;
                }
            }

            return null;
        }

        public bool IsUnit(string word)
        {
            return word != null && _units.Contains(word);
        }

        // El patrón de un mensaje ICU de la app (la gramática de MessageFormatter: literal, {nombre} y {nombre, plural, …}). Un
        // parámetro entero (integers) es un número; uno de texto, cualquier texto.
        public static string ToPattern(string message, ISet<string>? integers = null)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var position = 0;
            return Sequence(message, ref position, false, integers ?? NoIntegers);
        }

        private static string Sequence(string message, ref int position, bool inPlural, ISet<string> integers)
        {
            var builder = new StringBuilder();
            while (position < message.Length)
            {
                var c = message[position];
                if (c == '}')
                {
                    break;
                }

                if (c == '{')
                {
                    builder.Append(Argument(message, ref position, integers));
                    continue;
                }

                if (c == '#' && inPlural)
                {
                    builder.Append(@"-?\d+");
                    position++;
                    continue;
                }

                builder.Append(Regex.Escape(c.ToString()));
                position++;
            }

            return builder.ToString();
        }

        // { nombre } o { nombre, plural, sel{…} sel{…} }: devuelve el patrón y deja la posición después de la '}' de cierre.
        private static string Argument(string message, ref int position, ISet<string> integers)
        {
            position++; // {
            var close = message.IndexOf('}', position);
            var comma = message.IndexOf(',', position);
            if (comma < 0 || (close >= 0 && close < comma))
            {
                var name = message.Substring(position, close - position).Trim();
                position = close + 1;
                return integers.Contains(name) ? @"(?:-?\d+)" : "(?:.+?)";
            }

            var second = message.IndexOf(',', comma + 1);
            position = second + 1;
            var alternatives = new List<string>();
            while (position < message.Length)
            {
                while (position < message.Length && char.IsWhiteSpace(message[position]))
                {
                    position++;
                }

                if (position >= message.Length || message[position] == '}')
                {
                    position++;
                    break;
                }

                var open = message.IndexOf('{', position);
                position = open + 1;
                alternatives.Add(Sequence(message, ref position, true, integers));
                position++; // } del caso
            }

            return "(?:" + string.Join("|", alternatives) + ")";
        }

        // Palabras de 1 a 4 letras que siguen a un '}' o a una cifra (con un espacio o '/' en el medio) en un mensaje, salvo las
        // que van antes de otro parámetro.
        private static void CollectUnits(string message, HashSet<string> units)
        {
            for (var i = 0; i < message.Length; i++)
            {
                if (!char.IsLetter(message[i]) || (i > 0 && char.IsLetter(message[i - 1])))
                {
                    continue;
                }

                var end = i;
                while (end < message.Length && char.IsLetter(message[end]))
                {
                    end++;
                }

                var before = i - 1;
                while (before >= 0 && (message[before] == ' ' || message[before] == '/'))
                {
                    before--;
                }

                // Una palabra que une dos parámetros ("Paso {current} de {total}") no es una unidad.
                var after = end;
                while (after < message.Length && message[after] == ' ')
                {
                    after++;
                }

                var joinsArguments = after < message.Length && message[after] == '{';
                if (end - i <= MaxUnitLength && before >= 0 && !joinsArguments && (message[before] == '}' || char.IsDigit(message[before])))
                {
                    units.Add(message.Substring(i, end - i));
                }
            }
        }
    }
}
