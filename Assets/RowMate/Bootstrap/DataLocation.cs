using System;
using System.IO;
using RowMate.Platform;

namespace RowMate.Bootstrap
{
    // Carpeta de la base local (F-HIS-17, AC-HIS-20-04; 03-rutinas/02-tech-design §2.4): la privada de la app
    // (PlatformServices.Storage: LocalLow/iguisoft/RowMate en Windows, getDatabasePath en Android). SOLO en el Editor y en los
    // builds de desarrollo, la variable de entorno ROWMATE_DATA_DIR la reemplaza: la ponen BootSceneTests,
    // run-unity-tests.ps1 y capture-snapshots.ps1, así ninguna corrida abre las bases del usuario. En un build normal el nombre
    // de la variable ni existe (BuildOutputInspector lo busca).
    public static class DataLocation
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const string Variable = "ROWMATE_DATA_DIR";
#endif

        public static string DatabaseDirectory(PlatformServices platform)
        {
            if (platform == null)
            {
                throw new ArgumentNullException(nameof(platform));
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var folder = Environment.GetEnvironmentVariable(Variable);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Directory.CreateDirectory(folder);
                return folder;
            }
#endif
            return platform.Storage.DatabaseDirectory();
        }

        // It. 5 (05-series-parciales/02-tech-design §2.7; DT5-05; ADR 021): prefs.json en la carpeta privada de la app, LA MISMA DE LA
        // BASE (en Android, la interna; no persistentDataPath): así lo comparten el build normal y el de simulador (las bases tienen
        // nombres distintos, el archivo de preferencias no: P-11) y en el Editor y en desarrollo ROWMATE_DATA_DIR lo reemplaza, como a
        // la base. Ninguna prueba abre ni escribe la ruta real (PreferencesFileTests solo compara la cadena).
        public static string PreferencesFile(PlatformServices platform)
        {
            return Path.Combine(DatabaseDirectory(platform), RowMate.Infrastructure.Preferences.PreferencesFiles.Name);
        }

        // true si la carpeta sale de la variable de desarrollo (para el registro, sin la ruta).
        public static bool IsOverridden
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Variable));
#else
                return false;
#endif
            }
        }
    }
}
