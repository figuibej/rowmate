using System;
using RowMate.Core.Localization;
using RowMate.Core.Ports;

namespace RowMate.Core.Export
{
    // La carpeta como se muestra (AC-STR-09-19; 02-tech-design §2.5, A12-06 simplificado): la etiqueta fija de la ubicación
    // (ROWMATE_DATA_DIR\exports en el Editor y los builds de desarrollo, la pone Bootstrap) o, sin etiqueta, la carpeta Documentos
    // en el idioma de la app: "Documentos\RowMate\TCX" / "Documents\RowMate\TCX". Nunca la unidad, el usuario ni la ruta real.
    public static class ExportFolderLabel
    {
        // Subcarpeta de Documentos (DT12-10), como se muestra.
        public const string DocumentsSubfolder = "RowMate\\TCX";

        public static string For(ExportLocation location, Localizer l10n)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (l10n == null)
            {
                throw new ArgumentNullException(nameof(l10n));
            }

            return location.FixedLabel ?? l10n.Get(L10nKeys.ExportTcxDocuments) + "\\" + DocumentsSubfolder;
        }
    }
}
