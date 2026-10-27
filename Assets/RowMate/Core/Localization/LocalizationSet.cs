using System;
using System.Collections.Generic;

namespace RowMate.Core.Localization
{
    // Los dos catálogos de la app (es y en, F-APP-10). Load los valida juntos: cada uno con su @@locale y los dos con
    // las mismas claves y los mismos parámetros (nombre y tipo, AC-APP-10-01 y -04); si no, FormatException.
    // Quién lee los archivos (TextAsset en Unity, File en los tests) queda afuera: Core recibe el texto.
    public sealed class LocalizationSet
    {
        private LocalizationSet(Localizer es, Localizer en)
        {
            Es = es;
            En = en;
        }

        public Localizer Es { get; }

        public Localizer En { get; }

        public static LocalizationSet Load(string esJson, string enJson)
        {
            var es = LocalizationCatalog.Parse(esJson);
            var en = LocalizationCatalog.Parse(enJson);
            if (es.Locale != "es")
            {
                throw new FormatException("LocalizationSet: el catálogo español tiene @@locale \"" + es.Locale + "\".");
            }

            if (en.Locale != "en")
            {
                throw new FormatException("LocalizationSet: el catálogo inglés tiene @@locale \"" + en.Locale + "\".");
            }

            CheckSameKeys(es, en);
            CheckSameKeys(en, es);
            return new LocalizationSet(new Localizer(es), new Localizer(en));
        }

        public Localizer For(AppLanguage language)
        {
            switch (language)
            {
                case AppLanguage.Es:
                    return Es;
                case AppLanguage.En:
                    return En;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, "Idioma no soportado.");
            }
        }

        private static void CheckSameKeys(LocalizationCatalog a, LocalizationCatalog b)
        {
            var missing = new List<string>();
            foreach (var key in a.Keys)
            {
                if (!b.Contains(key))
                {
                    missing.Add(key);
                    continue;
                }

                var pa = a.Placeholders(key);
                var pb = b.Placeholders(key);
                if (!SamePlaceholders(pa, pb))
                {
                    throw new FormatException("LocalizationSet: \"" + key + "\" tiene parámetros distintos en \"" + a.Locale
                        + "\" y en \"" + b.Locale + "\".");
                }
            }

            if (missing.Count > 0)
            {
                throw new FormatException("LocalizationSet: claves de \"" + a.Locale + "\" que faltan en \"" + b.Locale + "\": "
                    + string.Join(", ", missing) + ".");
            }
        }

        private static bool SamePlaceholders(IReadOnlyList<PlaceholderInfo> a, IReadOnlyList<PlaceholderInfo> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            foreach (var pa in a)
            {
                var found = false;
                foreach (var pb in b)
                {
                    if (pa.Name == pb.Name && pa.Type == pb.Type)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
