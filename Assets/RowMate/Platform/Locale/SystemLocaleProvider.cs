using System.Collections.Generic;
using RowMate.Core.Ports;
using UnityEngine;

namespace RowMate.Platform.Locale
{
    // ISystemLocale de la app (F-APP-09, AC-APP-09-06): la lista de idiomas preferidos del sistema, en orden.
    // Windows: GetUserPreferredUILanguages; Android: LocaleList por JNI; resto (y si la lista nativa viene vacía):
    // Application.systemLanguage como una sola etiqueta. En el Editor, el menú RowMate/Simular idioma la reemplaza.
    // Refresh() vuelve a leerla (AppRoot.OnApplicationFocus, AC-APP-09-05).
    public sealed class SystemLocaleProvider : ISystemLocale
    {
        private IReadOnlyList<string> _tags;

        public SystemLocaleProvider()
        {
            _tags = ReadNow();
        }

        public IReadOnlyList<string> PreferredLanguageTags => _tags;

        public void Refresh()
        {
            _tags = ReadNow();
        }

        public static IReadOnlyList<string> ReadNow()
        {
#if UNITY_EDITOR
            var simulated = DeveloperOverrides.Languages;
            if (!string.IsNullOrEmpty(simulated))
            {
                return AndroidLocaleList.Parse(simulated);
            }
#endif
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var windows = WindowsUiLanguages.Read();
            if (windows.Count > 0)
            {
                return windows;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            var android = AndroidLocaleList.Read();
            if (android.Count > 0)
            {
                return android;
            }
#endif
            return FromSystemLanguage(Application.systemLanguage);
        }

        // Respaldo: un idioma sin región (lo que da Unity en todas las plataformas).
        internal static IReadOnlyList<string> FromSystemLanguage(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Spanish:
                    return new[] { "es" };
                case SystemLanguage.English:
                    return new[] { "en" };
                case SystemLanguage.Unknown:
                    return new string[0];
                default:
                    return new[] { language.ToString().ToLowerInvariant() };
            }
        }
    }
}
