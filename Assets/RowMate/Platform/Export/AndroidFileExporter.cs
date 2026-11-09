using System;
using System.IO;
using System.Threading.Tasks;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Export;
using UnityEngine;

namespace RowMate.Platform.Export
{
    // Exportar en Android: la caché de la app y la hoja de compartir (It. 12; 12-exportar-tcx/02-tech-design §3.4, DT12-18;
    // AC-STR-09-24, DV-122). Plugin propio com.rowmate.export.RmExport (Assets/Plugins/Android/RowMateExport), sin androidx ni
    // permisos: un ContentProvider propio (RmExportProvider) da el permiso de lectura TEMPORAL a la app elegida.
    // - La carpeta (cacheDir/exports) se lee al construir, en el hilo principal (RmExport.exportDir).
    // - ExportAsync, en Task.Run: borra los .tcx de más de 24 h (FolderExportWriter.ShareCacheMaxAge) y escribe sin pisar
    //   (FolderExportWriter). Después del await (hilo principal), RmExport.share abre la hoja; false o una excepción de JNI = falla
    //   (AC-STR-09-21). Delivery = ExportDelivery.Share: el servicio no muestra aviso de éxito (la hoja es la confirmación).
    // - Reveal: sin efecto (no hay "Abrir carpeta" en Android).
    // Sin #if: compila en todas las plataformas; solo se crea en Android (FileExporterFactory). NO VERIFICABLE sin el módulo de
    // Android y un teléfono (native/android/check.ps1 compila el Kotlin y revisa sus firmas).
    public sealed class AndroidFileExporter : IFileExporter
    {
        public const string PluginClass = "com.rowmate.export.RmExport";
        public const string ExportDirMethod = "exportDir";
        public const string ShareMethod = "share";

        // La etiqueta de la caché para el registro (I-12): el aviso de Android no la muestra.
        public const string CacheLabel = "cache\\exports";

        public AndroidFileExporter()
        {
            string directory;
            using (var plugin = new AndroidJavaClass(PluginClass))
            using (var activity = CurrentActivity())
            {
                directory = plugin.CallStatic<string>(ExportDirMethod, activity);
            }

            if (string.IsNullOrEmpty(directory))
            {
                throw new IOException("RmExport.exportDir no devolvió la carpeta");
            }

            Location = new ExportLocation(directory, CacheLabel);
        }

        public ExportDelivery Delivery => ExportDelivery.Share;

        public ExportLocation Location { get; }

        ExportLocation? IFileExporter.Location => Location;

        public async Task<ExportedFile> ExportAsync(ExportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var location = Location;
            var file = await Task.Run(() =>
            {
                // AC-STR-09-24: los archivos de la caché de compartir de más de 24 h se borran en la exportación siguiente.
                FolderExportWriter.DeleteOlderThan(location.Directory, request.Extension, DateTime.UtcNow - FolderExportWriter.ShareCacheMaxAge);
                return FolderExportWriter.Write(location, request.BaseName, request.Extension, request.Content());
            });

            // De vuelta en el hilo principal: la hoja de compartir (el plugin la abre en el hilo de UI de la actividad).
            bool shown;
            using (var plugin = new AndroidJavaClass(PluginClass))
            using (var activity = CurrentActivity())
            {
                shown = plugin.CallStatic<bool>(ShareMethod, activity, file.FullPath, request.MimeType, request.ShareTitle);
            }

            if (!shown)
            {
                throw new IOException("RmExport.share no pudo abrir la hoja de compartir");
            }

            return file;
        }

        public void Reveal(ExportedFile file)
        {
        }

        private static AndroidJavaObject CurrentActivity()
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return player.GetStatic<AndroidJavaObject>("currentActivity");
            }
        }
    }
}
