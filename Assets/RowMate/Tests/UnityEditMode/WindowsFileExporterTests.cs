using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Export;

namespace RowMate.Tests.UnityEditMode
{
    // It. 15 (#70, AC-D15-13, DT15-06): "Abrir carpeta" prueba el respaldo del shell si el Explorador no arranca y, si también falla,
    // lanza RevealFailedException. Con el lanzador y el respaldo falsos: NUNCA se abre el Explorador ni se llama a shell32.
    public sealed class WindowsFileExporterTests
    {
        private static readonly ExportLocation Folder = new ExportLocation("D:\\Datos\\Documents\\RowMate\\TCX", null);

        private static readonly ExportedFile File1 =
            new ExportedFile("RowMate_x.tcx", "D:\\Datos\\Documents\\RowMate\\TCX\\RowMate_x.tcx", Folder);

        [Test, Category("AC-D15-13")]
        public void FallbackOnLaunchFailure()
        {
            var fallback = new List<string>();
            var exporter = new WindowsFileExporter(Folder, PlatformKind.Windows, (_, _) => throw new InvalidOperationException("explorer"),
                path =>
                {
                    fallback.Add(path);
                    return true;
                });
            Assert.DoesNotThrow(() => exporter.Reveal(File1), "el respaldo abrió la carpeta");
            Assert.That(fallback, Is.EqualTo(new[] { File1.FullPath }), "con el archivo a seleccionar");
        }

        [Test, Category("AC-D15-13")]
        public void BothFailThrowsRevealFailed()
        {
            var exporter = new WindowsFileExporter(Folder, PlatformKind.Windows, (_, _) => throw new InvalidOperationException("explorer"), _ => false);
            var error = Assert.Throws<RevealFailedException>(() => exporter.Reveal(File1));
            Assert.That(error!.InnerException, Is.InstanceOf<InvalidOperationException>());
            var throwing = new WindowsFileExporter(Folder, PlatformKind.Windows, (_, _) => throw new InvalidOperationException("explorer"),
                _ => throw new InvalidOperationException("shell"));
            Assert.Throws<RevealFailedException>(() => throwing.Reveal(File1), "un respaldo que lanza cuenta como falla");
        }

        [Test, Category("AC-D15-13")]
        public void LaunchOkDoesNotUseTheFallback()
        {
            var used = false;
            var exporter = new WindowsFileExporter(Folder, PlatformKind.Windows, (_, _) => { }, _ => used = true);
            exporter.Reveal(File1);
            Assert.That(used, Is.False);
        }
    }
}
