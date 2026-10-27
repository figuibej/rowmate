using System;
using System.Collections.Generic;
using System.IO;
using RowMate.Core.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Localization
{
    // Catálogos reales de la app (Assets/RowMate/Localization/strings.{es,en}.json), cargados una vez con LocalizationSet
    // (que ya valida mensajes, metadatos y paridad de claves). Lo usan los tests de catálogo y los de textos de rutina.
    public static class LocalizationFixture
    {
        private static readonly object Gate = new object();
        private static LocalizationSet? _set;

        public static LocalizationSet Set
        {
            get
            {
                lock (Gate)
                {
                    if (_set == null)
                    {
                        _set = LocalizationSet.Load(ReadCatalog("es"), ReadCatalog("en"));
                    }

                    return _set;
                }
            }
        }

        public static Localizer Es => Set.Es;

        public static Localizer En => Set.En;

        // Los dos idiomas, para los tests que piden el mismo resultado en es y en.
        public static IEnumerable<Localizer> Both
        {
            get
            {
                yield return Es;
                yield return En;
            }
        }

        public static LocalizationCatalog Catalog(string languageCode)
        {
            return LocalizationCatalog.Parse(ReadCatalog(languageCode));
        }

        public static string ReadCatalog(string languageCode)
        {
            var path = RepoPaths.Catalog(languageCode);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Falta el catálogo " + RepoPaths.Relative(path)
                    + " (se genera una vez con tools/oracle/run-oracle.ps1 -L10n).", path);
            }

            return File.ReadAllText(path);
        }

        // Instantánea de los .arb del legacy (l10n_legacy.json): clave → texto, en el orden del archivo.
        public static IReadOnlyList<KeyValuePair<string, string>> LegacyTexts(string languageCode)
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach (var row in GoldenVectors.Rows("l10n_legacy.json", languageCode))
            {
                result.Add(new KeyValuePair<string, string>(row.Text("key"), row.Text("text")));
            }

            return result;
        }

        // Instantánea de los metadatos del legacy: "clave" → "nombre:tipo, …" en el orden del archivo.
        public static IReadOnlyDictionary<string, string> LegacyPlaceholders(string languageCode)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var row in GoldenVectors.Rows("l10n_legacy.json", "placeholders"))
            {
                if (row.Text("locale") != languageCode)
                {
                    continue;
                }

                var key = row.Text("key");
                var item = row.Text("name") + ":" + row.Text("type");
                result[key] = result.TryGetValue(key, out var previous) ? previous + ", " + item : item;
            }

            return result;
        }

        // "nombre:tipo, …" de una clave del catálogo nuevo ("" si no tiene parámetros).
        public static string Describe(IReadOnlyList<PlaceholderInfo> placeholders)
        {
            var parts = new string[placeholders.Count];
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = placeholders[i].Name + ":" + placeholders[i].Type;
            }

            return string.Join(", ", parts);
        }
    }
}
