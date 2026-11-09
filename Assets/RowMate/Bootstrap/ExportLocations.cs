using System;
using System.IO;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Bootstrap
{
    // La carpeta de "Exportar TCX" (It. 12; 12-exportar-tcx/02-tech-design §3.5, DT12-10; AC-STR-09-16):
    // - Editor y builds de desarrollo con ROWMATE_DATA_DIR: {ROWMATE_DATA_DIR}\exports, con la etiqueta fija
    //   "ROWMATE_DATA_DIR\exports" (nunca Documentos: las pruebas, las capturas y los humos no tocan las carpetas del usuario). La
    //   cadena se compila SOLO bajo #if (BuildOutputInspector la busca en el build normal, R-04).
    // - Windows (y el Editor sin la variable): Documentos del usuario según Windows (GetFolderPath(MyDocuments): respeta la
    //   redirección de OneDrive) + RowMate\TCX, sin etiqueta fija (el aviso dice "Documentos\RowMate\TCX" en el idioma de la app).
    // - Android: null (el exportador de Android usa la caché de la app, cache/exports, que le da el plugin).
    // - iOS: null (sin exportación hasta la 10b).
    // Resolve solo CALCULA la ruta: nunca crea la carpeta (m-02; la crea FolderExportWriter al escribir). Si Windows no da la
    // carpeta Documentos, la ubicación queda sin ruta y la exportación falla con aviso y registro (R-07).
    public static class ExportLocations
    {
        public const string DataDirFolder = "exports";
        public const string AppFolder = "RowMate";
        public const string TcxFolder = "TCX";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // La etiqueta de la carpeta de desarrollo (AC-STR-09-19: "Guardado en ROWMATE_DATA_DIR\exports").
        public const string DataDirLabel = DataLocation.Variable + "\\" + DataDirFolder;
#endif

        // El exportador de la plataforma para esa carpeta (Platform/Export/FileExporterFactory): lo usan AppRoot y TcxExportFrameTests
        // (el exportador real de Windows sobre una carpeta temporal; PlayMode no referencia Platform).
        public static IFileExporter? CreateExporter(PlatformKind kind, ExportLocation? location, IAppLogger logger)
        {
            return RowMate.Platform.Export.FileExporterFactory.Create(kind, location, logger);
        }

        // La de la app: la variable de desarrollo (solo Editor y desarrollo) y la carpeta Documentos del sistema.
        public static ExportLocation? ForApp(PlatformKind kind)
        {
            string? dataDir = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            dataDir = Environment.GetEnvironmentVariable(DataLocation.Variable);
#endif
            return Resolve(kind, dataDir, () => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        // Pura (sin disco): la ubicación para la plataforma, la carpeta de datos de desarrollo (null o vacía = no hay) y la carpeta
        // Documentos del sistema (se lee solo si hace falta).
        public static ExportLocation? Resolve(PlatformKind kind, string? dataDir, Func<string?> myDocuments)
        {
            if (myDocuments == null)
            {
                throw new ArgumentNullException(nameof(myDocuments));
            }

            if (kind == PlatformKind.Android || kind == PlatformKind.Ios)
            {
                return null;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!string.IsNullOrWhiteSpace(dataDir))
            {
                return new ExportLocation(Path.Combine(dataDir!, DataDirFolder), DataDirLabel);
            }
#endif
            var documents = myDocuments();
            if (string.IsNullOrWhiteSpace(documents) || !Path.IsPathRooted(documents))
            {
                // R-07: sin carpeta Documentos la exportación falla (FolderExportWriter: "sin ruta"), nunca escribe en otro lado.
                return new ExportLocation("", null);
            }

            return new ExportLocation(Path.Combine(documents!, AppFolder, TcxFolder), null);
        }
    }
}
