using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Export;

namespace RowMate.Platform.Export
{
    // Exportar en una carpeta visible (It. 12; 12-exportar-tcx/02-tech-design §3.3, DT12-08, DT12-09; AC-STR-09-14, -16, -20):
    // - ExportAsync: arma los bytes (request.Content) y escribe con FolderExportWriter en Task.Run, fuera del hilo principal (el
    //   cuadro de la UI no espera el disco, AC-STR-09-12). La continuación vuelve al hilo principal (contexto de Unity).
    // - Reveal: el Explorador con el archivo seleccionado o la carpeta (ExplorerReveal), con UseShellExecute = false. Una falla
    //   la registra TcxExportService (spec §10.4). En las pruebas, launch reemplaza al proceso (nunca se abre nada).
    // Lo usan Windows y el Editor (también el de Mac, con open -R). La carpeta la da Bootstrap/ExportLocations.
    // - It. 15 (#70, AC-D15-13, DT15-06): si el Explorador no arranca, prueba el respaldo (ShellReveal.TrySelect en Windows); si
    //   también falla, lanza RevealFailedException y TcxExportService publica OpenFolderFailed. Las pruebas inyectan los dos.
    public sealed class WindowsFileExporter : IFileExporter
    {
        private readonly PlatformKind _kind;
        private readonly Action<string, string> _launch;
        private readonly Func<string, bool> _fallback;

        public WindowsFileExporter(ExportLocation location, PlatformKind kind = PlatformKind.Windows, Action<string, string>? launch = null,
                                   Func<string, bool>? fallback = null)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            _kind = kind;
            _launch = launch ?? Launch;
            _fallback = fallback ?? (kind == PlatformKind.MacOs ? (Func<string, bool>)(_ => false) : ShellReveal.TrySelect);
        }

        public ExportDelivery Delivery => ExportDelivery.SaveToFolder;

        public ExportLocation Location { get; }

        ExportLocation? IFileExporter.Location => Location;

        public Task<ExportedFile> ExportAsync(ExportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var location = Location;
            return Task.Run(() => FolderExportWriter.Write(location, request.BaseName, request.Extension, request.Content()));
        }

        public void Reveal(ExportedFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            Exception? launchError;
            try
            {
                _launch(ExplorerReveal.Program(_kind), ExplorerReveal.Arguments(_kind, file, File.Exists));
                return;
            }
            catch (Exception e)
            {
                launchError = e;
            }

            bool revealed;
            try
            {
                revealed = _fallback(string.IsNullOrEmpty(file.FullPath) ? ExplorerReveal.Folder(file) : file.FullPath);
            }
            catch (Exception)
            {
                revealed = false;
            }

            if (!revealed)
            {
                throw new RevealFailedException("Reveal: no abrió el Explorador ni el respaldo del shell (" + launchError.GetType().Name + ")", launchError);
            }
        }

        private static void Launch(string program, string arguments)
        {
            var info = new ProcessStartInfo(program, arguments) { UseShellExecute = false, CreateNoWindow = false };
            using (Process.Start(info))
            {
            }
        }
    }
}
