using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Export;

namespace RowMate.Tests.UnityEditMode
{
    // ERT · "Abrir carpeta" (12-exportar-tcx/02-tech-design §3.3; AC-STR-09-20; R-01): el Explorador con el archivo seleccionado o, si
    // ya no está, la carpeta. Solo se prueban las cadenas y el lanzador falso: NUNCA se abre el Explorador.
    public sealed class ExplorerRevealTests
    {
        private static readonly ExportLocation Folder = new ExportLocation("D:\\Datos\\Documents\\RowMate\\TCX", null);

        private static readonly ExportedFile File1 =
            new ExportedFile("RowMate_2026-11-05_1840_Escalera.tcx", "D:\\Datos\\Documents\\RowMate\\TCX\\RowMate_2026-11-05_1840_Escalera.tcx", Folder);

        [Test, Category("AC-STR-09-20")]
        public void SelectWhenFileExistsElseFolder()
        {
            Assert.That(ExplorerReveal.Program(PlatformKind.Windows), Is.EqualTo("explorer.exe"));
            Assert.That(ExplorerReveal.Arguments(PlatformKind.Windows, File1, _ => true),
                Is.EqualTo("/select,\"D:\\Datos\\Documents\\RowMate\\TCX\\RowMate_2026-11-05_1840_Escalera.tcx\""));
            Assert.That(ExplorerReveal.Arguments(PlatformKind.Windows, File1, _ => false),
                Is.EqualTo("\"D:\\Datos\\Documents\\RowMate\\TCX\""), "si el archivo ya no está, la carpeta");
            var spaced = new ExportedFile("a b.tcx", "C:\\Mis Documentos\\RowMate\\TCX\\a b.tcx", new ExportLocation("C:\\Mis Documentos\\RowMate\\TCX", null));
            Assert.That(ExplorerReveal.Arguments(PlatformKind.Windows, spaced, _ => true), Is.EqualTo("/select,\"C:\\Mis Documentos\\RowMate\\TCX\\a b.tcx\""),
                "con espacios, entre comillas");
            Assert.That(ExplorerReveal.Program(PlatformKind.MacOs), Is.EqualTo("open"));
            Assert.That(ExplorerReveal.Arguments(PlatformKind.MacOs, File1, _ => true), Does.StartWith("-R \""));
        }

        [Test, Category("AC-STR-09-20")]
        public void RevealUsesTheLauncherNeverTheRealExplorer()
        {
            var launched = new List<string>();
            var folder = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "ert-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                var file = Path.Combine(folder, "RowMate_x.tcx");
                File.WriteAllText(file, "x");
                var exporter = new WindowsFileExporter(new ExportLocation(folder, null), PlatformKind.Windows, (program, args) => launched.Add(program + " " + args));
                var exported = new ExportedFile("RowMate_x.tcx", file, exporter.Location);
                exporter.Reveal(exported);
                File.Delete(file);
                exporter.Reveal(exported);
                Assert.That(launched, Is.EqualTo(new[] { "explorer.exe /select,\"" + file + "\"", "explorer.exe \"" + folder + "\"" }));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
