using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Platform.Export
{
    // El exportador de la plataforma (It. 12; 12-exportar-tcx/02-tech-design §3.5, DT12-07): Windows (y el Editor) guardan en la
    // carpeta que da Bootstrap/ExportLocations; Android comparte desde su caché; el resto (iOS hasta la 10b) no exporta (null =
    // NoFileExporter: el Detalle no muestra el botón). Si el plugin de Android no responde, tampoco hay botón (queda en el registro).
    public static class FileExporterFactory
    {
        public const string Tag = "EXPORT";

        public static IFileExporter? Create(PlatformKind kind, ExportLocation? location, IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (kind == PlatformKind.Android)
            {
                try
                {
                    return new AndroidFileExporter();
                }
                catch (Exception error)
                {
                    logger.Log(LogLevel.Warning, Tag, "sin exportación: el plugin de Android no respondió (" + error.GetType().Name + ")");
                    return null;
                }
            }
#endif
            if ((kind == PlatformKind.Windows || kind == PlatformKind.MacOs) && location != null)
            {
                return new WindowsFileExporter(location, kind);
            }

            return null;
        }
    }
}
