using System.Collections.Generic;

namespace RowMate.Core.Ports
{
    // Lista de idiomas preferidos del sistema, en orden (no solo el principal: AC-APP-09-06, QA-P-20), como etiquetas
    // BCP 47 ("es-AR", "zh-Hans-CN"). La implementa Platform en la It. 2 (Android LocaleList, Windows
    // GlobalizationPreferences.Languages; iOS/macOS Locale.preferredLanguages en la 10b). Lista vacía si no hay dato.
    public interface ISystemLocale
    {
        IReadOnlyList<string> PreferredLanguageTags { get; }
    }
}
