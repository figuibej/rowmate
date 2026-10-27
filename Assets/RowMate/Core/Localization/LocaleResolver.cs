using System.Collections.Generic;

namespace RowMate.Core.Localization
{
    // Elección del idioma de la app desde la lista de idiomas preferidos del sistema (F-APP-09, AC-APP-09-01 a -03, -06).
    // Es el algoritmo basicLocaleListResolution de Flutter 3.47.6 (packages/flutter/lib/src/widgets/app.dart:146-238)
    // con los supportedLocales del legacy, [en, es] sin escritura ni región (lib/main.dart:103-106), como pide la
    // restricción 3 de la spec (D-06): incluye las dos filas † de AC-APP-09-02.
    public static class LocaleResolver
    {
        public static AppLanguage Resolve(IReadOnlyList<string>? preferredTags)
        {
            // Lista nula o vacía → el primer idioma soportado (app.dart:153-155).
            if (preferredTags == null || preferredTags.Count == 0)
            {
                return AppLanguage.En;
            }

            AppLanguage? matchesLanguageCode = null;
            for (var i = 0; i < preferredTags.Count; i++)
            {
                var user = TagAt(preferredTags, i);
                var supported = TrySupported(user.Language, out var language);

                // Coincidencia perfecta: "es" o "en" sin escritura ni región (app.dart:181-185).
                if (supported && user.Script == null && user.Region == null)
                {
                    return language;
                }

                // Idioma + escritura e idioma + región (app.dart:186-201): nunca coinciden, porque ningún idioma
                // soportado tiene escritura ni región. Por eso no hay código para esos dos pasos.

                // Coincidencia por idioma de una entrada anterior: gana si esta entrada no tuvo una mejor (app.dart:205-207).
                if (matchesLanguageCode.HasValue)
                {
                    return matchesLanguageCode.Value;
                }

                // Coincidencia solo por idioma (app.dart:209-221): se guarda; si es la PRIMERA entrada se devuelve
                // en el acto, salvo que la siguiente tenga el mismo idioma (podría ser una coincidencia mejor).
                if (supported)
                {
                    matchesLanguageCode = language;
                    if (i == 0 && !(i + 1 < preferredTags.Count && TagAt(preferredTags, i + 1).Language == user.Language))
                    {
                        return language;
                    }
                }

                // Coincidencia solo por país (app.dart:225-230): los idiomas soportados no tienen país, así que el
                // mapa de países solo tiene la clave null y una entrada con país nunca coincide.
            }

            // app.dart:235: matchesLanguageCode ?? matchesCountryCode (siempre null acá) ?? supportedLocales.first.
            return matchesLanguageCode ?? AppLanguage.En;
        }

        private static LanguageTag TagAt(IReadOnlyList<string> tags, int index)
        {
            var tag = tags[index];
            return tag == null ? default : LanguageTag.Parse(tag);
        }

        private static bool TrySupported(string languageCode, out AppLanguage language)
        {
            if (languageCode == "en")
            {
                language = AppLanguage.En;
                return true;
            }

            if (languageCode == "es")
            {
                language = AppLanguage.Es;
                return true;
            }

            language = AppLanguage.En;
            return false;
        }
    }
}
