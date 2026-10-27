using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Localization
{
    // Textos de un idioma (F-APP-10). Equivale a AppLocalizations del legacy: Get para las claves sin parámetros y
    // Format para las que tienen, con los nombres y los tipos EXACTOS de sus metadatos ("String" → MessageArg.Text,
    // "int" → MessageArg.Number); si no coinciden, ArgumentException. Una clave inexistente → MissingTranslationException.
    public sealed class Localizer
    {
        private static readonly MessageArg[] NoArgs = new MessageArg[0];

        private readonly LocalizationCatalog _catalog;

        public Localizer(LocalizationCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        // "es" o "en" (el @@locale del catálogo).
        public string LanguageCode => _catalog.Locale;

        public bool Contains(string key)
        {
            return _catalog.Contains(key);
        }

        // Clave sin parámetros.
        public string Get(string key)
        {
            var placeholders = _catalog.Placeholders(key);
            if (placeholders.Count > 0)
            {
                throw new ArgumentException("La clave \"" + key + "\" tiene parámetros: usar Format.", nameof(key));
            }

            return _catalog.Compiled(key).Format(NoArgs);
        }

        // Clave con parámetros: cada parámetro declarado exactamente una vez y con su tipo.
        public string Format(string key, params MessageArg[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            var placeholders = _catalog.Placeholders(key);
            if (args.Length != placeholders.Count)
            {
                throw new ArgumentException("La clave \"" + key + "\" espera " + Fmt.Integer(placeholders.Count) + " argumentos ("
                    + Describe(placeholders) + ") y recibió " + Fmt.Integer(args.Length) + ".", nameof(args));
            }

            var given = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                PlaceholderInfo? declared = null;
                for (var j = 0; j < placeholders.Count; j++)
                {
                    if (placeholders[j].Name == arg.Name)
                    {
                        declared = placeholders[j];
                    }
                }

                if (declared == null)
                {
                    throw new ArgumentException("La clave \"" + key + "\" no tiene el parámetro \"" + arg.Name + "\" ("
                        + Describe(placeholders) + ").", nameof(args));
                }

                if (!given.Add(arg.Name))
                {
                    throw new ArgumentException("La clave \"" + key + "\": parámetro repetido \"" + arg.Name + "\".", nameof(args));
                }

                if (declared.Kind != arg.Kind)
                {
                    throw new ArgumentException("La clave \"" + key + "\": el parámetro \"" + arg.Name + "\" es de tipo "
                        + declared.Type + ".", nameof(args));
                }
            }

            return _catalog.Compiled(key).Format(args);
        }

        private static string Describe(IReadOnlyList<PlaceholderInfo> placeholders)
        {
            var parts = new string[placeholders.Count];
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = placeholders[i].ToString();
            }

            return parts.Length == 0 ? "ninguno" : string.Join(", ", parts);
        }
    }
}
