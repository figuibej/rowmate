using System.Collections.Generic;

namespace RowMate.Core.Localization
{
    // Pedido de una clave que no está en el catálogo (AC-APP-10-09): falla en voz alta, para que los tests lo detecten;
    // el usuario nunca ve el nombre de una clave ni un texto vacío en su lugar.
    public sealed class MissingTranslationException : KeyNotFoundException
    {
        public MissingTranslationException(string key, string languageCode)
            : base("Falta la clave \"" + key + "\" en el catálogo \"" + languageCode + "\".")
        {
            Key = key;
            LanguageCode = languageCode;
        }

        public string Key { get; }

        public string LanguageCode { get; }
    }
}
