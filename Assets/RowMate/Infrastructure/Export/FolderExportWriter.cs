using System;
using System.Collections.Generic;
using System.IO;
using RowMate.Core.Export;
using RowMate.Core.Ports;

namespace RowMate.Infrastructure.Export
{
    // Escritura de un archivo exportado en una carpeta (It. 12; 02-tech-design §3.2, DT12-09; AC-STR-09-14), la misma para los
    // exportadores reales (Windows, Android) y FakeFileExporter, así las colisiones y la atomicidad se prueban una vez con dotnet:
    // 1. Crea la carpeta si falta (solo acá: ExportLocations nunca la crea, m-02) y enumera sus archivos.
    // 2. Escribe "~rowmate-{guid}.part" entero, con Flush(true) en disco.
    // 3. Lo mueve al primer nombre libre (TcxFileName.Resolve: sin distinguir mayúsculas) SIN pisar: si otro lo tomó entre la
    //    enumeración y el Move, prueba el sufijo siguiente (hasta 999).
    // 4. El .part se borra siempre (finally): un error nunca deja un archivo a medias ni temporales sueltos.
    // DeleteOlderThan purga la caché de compartir de Android (AC-STR-09-24, QA-G12-02). Corre en el hilo que la llame (los
    // exportadores reales, en Task.Run).
    public static class FolderExportWriter
    {
        public const string TempPrefix = "~rowmate-";
        public const string TempExtension = ".part";

        // Los .tcx de la caché de compartir de más de 24 h se borran en la exportación siguiente (AC-STR-09-24).
        public static readonly TimeSpan ShareCacheMaxAge = TimeSpan.FromHours(24);

        public static ExportedFile Write(ExportLocation location, string baseName, string extension, byte[] content,
                                         Func<string, Stream>? openWrite = null)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrWhiteSpace(location.Directory))
            {
                // R-07: Documentos sin ruta (GetFolderPath vacío) es una falla común, con aviso y registro.
                throw new IOException("FolderExportWriter: la carpeta de exportación no tiene ruta.");
            }

            var directory = location.Directory;
            Directory.CreateDirectory(directory);
            var taken = new List<string>();
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                taken.Add(Path.GetFileName(file));
            }

            var temp = Path.Combine(directory, TempPrefix + Guid.NewGuid().ToString("N") + TempExtension);
            try
            {
                using (var stream = openWrite != null ? openWrite(temp) : new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(content, 0, content.Length);
                    if (stream is FileStream fileStream)
                    {
                        fileStream.Flush(true);
                    }
                    else
                    {
                        stream.Flush();
                    }
                }

                for (var attempt = 0; attempt < TcxFileName.MaxCollisionSuffix; attempt++)
                {
                    var name = TcxFileName.Resolve(baseName, extension, taken);
                    var target = Path.Combine(directory, name);
                    try
                    {
                        File.Move(temp, target); // sin pisar: si existe, IOException
                        return new ExportedFile(name, target, location);
                    }
                    catch (IOException) when (File.Exists(target) && File.Exists(temp))
                    {
                        taken.Add(name); // otro lo tomó entre la enumeración y el Move: el sufijo siguiente
                    }
                }

                throw new IOException("FolderExportWriter: no queda ningún nombre libre para " + baseName + extension + ".");
            }
            finally
            {
                TryDelete(temp);
            }
        }

        // Borra los archivos de la carpeta (sin recursión) cuya extensión es EXACTAMENTE extension (sin distinguir mayúsculas: .tcx
        // y .TCX, nunca .tcx.part ni ~rowmate-*.part) y cuya última escritura es anterior a utcLimit (estricto). Uno que no se puede
        // borrar (en uso) se saltea sin lanzar. Carpeta inexistente → 0; nunca la crea. Devuelve cuántos borró.
        public static int DeleteOlderThan(string directory, string extension, DateTime utcLimit)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return 0;
            }

            var deleted = 0;
            foreach (var file in Directory.GetFiles(directory))
            {
                if (!string.Equals(Path.GetExtension(file), extension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    if (File.GetLastWriteTimeUtc(file) < utcLimit)
                    {
                        File.Delete(file);
                        deleted++;
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            return deleted;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
