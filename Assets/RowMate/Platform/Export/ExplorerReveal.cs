using System;
using System.IO;
using RowMate.Core.Common;
using RowMate.Core.Ports;

namespace RowMate.Platform.Export
{
    // "Abrir carpeta" (It. 12; 12-exportar-tcx/02-tech-design §3.3; AC-STR-09-20): qué programa se abre y con qué argumentos, como
    // función pura (ExplorerRevealTests prueba las cadenas y NUNCA abre nada).
    // - Windows: explorer.exe /select,"<archivo>" si el archivo sigue ahí (la carpeta con el archivo seleccionado); si ya no está,
    //   explorer.exe "<carpeta>".
    // - macOS (el Editor de Mac; la app de Mac es de la 10b): open -R "<archivo>" o open "<carpeta>".
    // Las rutas de Windows no pueden tener comillas, así que alcanza con encerrarlas entre comillas.
    public static class ExplorerReveal
    {
        public const string WindowsProgram = "explorer.exe";
        public const string MacProgram = "open";

        public static string Program(PlatformKind kind)
        {
            return kind == PlatformKind.MacOs ? MacProgram : WindowsProgram;
        }

        public static string Arguments(PlatformKind kind, ExportedFile file, Func<string, bool> fileExists)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (fileExists == null)
            {
                throw new ArgumentNullException(nameof(fileExists));
            }

            var selectFile = !string.IsNullOrEmpty(file.FullPath) && fileExists(file.FullPath);
            if (selectFile)
            {
                return kind == PlatformKind.MacOs ? "-R " + Quote(file.FullPath) : "/select," + Quote(file.FullPath);
            }

            return Quote(Folder(file));
        }

        // La carpeta del archivo (la de la ubicación si la ruta no tiene carpeta).
        public static string Folder(ExportedFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var folder = string.IsNullOrEmpty(file.FullPath) ? null : Path.GetDirectoryName(file.FullPath);
            return string.IsNullOrEmpty(folder) ? file.Location.Directory : folder!;
        }

        private static string Quote(string path)
        {
            return "\"" + path + "\"";
        }
    }
}
