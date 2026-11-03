using System;
using System.IO;

namespace RowMate.Infrastructure.Preferences
{
    // El archivo de preferencias de la app (ARCHITECTURE §9.4; ADR 021; 02-tech-design §2.7 de la It. 5): "prefs.json", EL MISMO
    // para el build normal y el de simulador (P-11: como el legacy, un solo archivo de preferencias), en la carpeta de la base
    // (la privada de la app; en el Editor y en desarrollo, la de ROWMATE_DATA_DIR). Nunca el archivo de preferencias del
    // legacy (Q-11).
    public static class PreferencesFiles
    {
        public const string Name = "prefs.json";

        // El temporal del guardado entero o nada: se escribe al lado y después se pone en lugar del archivo.
        public const string TempSuffix = ".tmp";

        public static string PathIn(string directory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            return Path.Combine(directory, Name);
        }
    }
}
