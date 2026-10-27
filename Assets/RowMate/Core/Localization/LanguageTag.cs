using System;

namespace RowMate.Core.Localization
{
    // Etiqueta de idioma del sistema ("es-AR", "zh-Hans-CN", "es_419") separada en idioma, escritura y región,
    // como el Locale(languageCode, scriptCode, countryCode) que Flutter arma con la lista del sistema.
    // Parse es tolerante (02-tech-design §3.3, Idioma, paso 2): separa por '-' o '_'; el idioma va en minúsculas;
    // una subetiqueta de 4 letras es la escritura ("Hans"); 2 letras o 3 dígitos, la región ("AR", "419");
    // corta en un singleton ("u", "x", "t"…); ignora variantes y extensiones. Por robustez también corta en el
    // juego de caracteres o el modificador de las etiquetas POSIX ("es_AR.UTF-8", "ca_ES@valencia").
    // Nunca falla con un texto no nulo: una etiqueta rara queda con un idioma que no coincide con ninguno.
    public readonly struct LanguageTag : IEquatable<LanguageTag>
    {
        private readonly string? _language;

        private LanguageTag(string language, string? script, string? region)
        {
            _language = language;
            Script = script;
            Region = region;
        }

        // En minúsculas; "" si la etiqueta no trae idioma.
        public string Language => _language ?? "";

        // Primera letra en mayúscula ("Hans"); null si no hay.
        public string? Script { get; }

        // En mayúsculas ("AR", "419"); null si no hay.
        public string? Region { get; }

        public static LanguageTag Parse(string tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(nameof(tag));
            }

            var text = tag.Trim();
            var cut = text.IndexOfAny(new[] { '.', '@' });
            if (cut >= 0)
            {
                text = text.Substring(0, cut);
            }

            var parts = text.Split('-', '_');
            var language = AsciiLower(parts[0]);
            string? script = null;
            string? region = null;
            for (var i = 1; i < parts.Length; i++)
            {
                var sub = parts[i];
                if (sub.Length == 0)
                {
                    continue;
                }

                if (sub.Length == 1)
                {
                    break; // singleton: empieza una extensión (-u-…, -x-…)
                }

                if (script == null && region == null && sub.Length == 4 && AllLetters(sub))
                {
                    script = char.ToUpperInvariant(sub[0]) + AsciiLower(sub.Substring(1));
                }
                else if (region == null && ((sub.Length == 2 && AllLetters(sub)) || (sub.Length == 3 && AllDigits(sub))))
                {
                    region = sub.ToUpperInvariant();
                }
            }

            return new LanguageTag(language, script, region);
        }

        public bool Equals(LanguageTag other)
        {
            return string.Equals(Language, other.Language, StringComparison.Ordinal)
                && string.Equals(Script, other.Script, StringComparison.Ordinal)
                && string.Equals(Region, other.Region, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is LanguageTag other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Language, Script, Region);
        }

        public static bool operator ==(LanguageTag left, LanguageTag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LanguageTag left, LanguageTag right)
        {
            return !left.Equals(right);
        }

        // "es", "es-AR", "zh-Hans-CN".
        public override string ToString()
        {
            var result = Language;
            if (Script != null)
            {
                result += "-" + Script;
            }

            if (Region != null)
            {
                result += "-" + Region;
            }

            return result;
        }

        private static string AsciiLower(string s)
        {
            return s.ToLowerInvariant();
        }

        private static bool AllLetters(string s)
        {
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllDigits(string s)
        {
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] < '0' || s[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
