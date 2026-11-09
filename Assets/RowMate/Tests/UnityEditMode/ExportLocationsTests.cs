using System;
using System.IO;
using NUnit.Framework;
using RowMate.Bootstrap;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Export;
using RowMate.Tests.Support;

namespace RowMate.Tests.UnityEditMode
{
    // ELT · La carpeta de "Exportar TCX" (12-exportar-tcx/02-tech-design §3.5, DT12-10; AC-STR-09-16; m-02): con ROWMATE_DATA_DIR va a
    // {dir}\exports con su etiqueta fija y NUNCA a Documentos; sin la variable, Documentos\RowMate\TCX (solo la cadena: ninguna
    // prueba escribe en Documentos); calcular la ruta no crea nada; Android e iOS no tienen carpeta de escritorio.
    public sealed class ExportLocationsTests
    {
        private const string FakeDocuments = "D:\\Datos\\Documents";

        [Test, Category("AC-STR-09-16")]
        public void DataDirGoesToExportsNeverDocuments()
        {
            var documentsRead = false;
            var dataDir = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "elt-" + Guid.NewGuid().ToString("N"));
            var location = ExportLocations.Resolve(PlatformKind.Windows, dataDir, () =>
            {
                documentsRead = true;
                return FakeDocuments;
            })!;
            Assert.That(location.Directory, Is.EqualTo(Path.Combine(dataDir, "exports")));
            Assert.That(location.FixedLabel, Is.EqualTo("ROWMATE_DATA_DIR\\exports"), "la etiqueta del aviso (AC-STR-09-19)");
            Assert.That(documentsRead, Is.False, "con la variable, Documentos ni se lee");
            Assert.That(location.Directory, Does.Not.Contain("Documents"));
        }

        [Test, Category("AC-STR-09-16")]
        public void WindowsUsesMyDocumentsRowMateTcx()
        {
            var location = ExportLocations.Resolve(PlatformKind.Windows, null, () => FakeDocuments)!;
            Assert.That(location.Directory, Is.EqualTo(Path.Combine(FakeDocuments, "RowMate", "TCX")));
            Assert.That(location.FixedLabel, Is.Null, "sin etiqueta fija: el aviso dice Documentos\\RowMate\\TCX en el idioma de la app");
            Assert.That(ExportLocations.Resolve(PlatformKind.Windows, "  ", () => FakeDocuments)!.Directory,
                Is.EqualTo(Path.Combine(FakeDocuments, "RowMate", "TCX")), "una variable en blanco no cuenta");

            // La de la app usa la carpeta Documentos del sistema (GetFolderPath respeta la redirección de OneDrive).
            var source = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Bootstrap/ExportLocations.cs"));
            Assert.That(source, Does.Contain("Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)"));
        }

        [Test, Category("AC-STR-09-16")]
        public void WithoutDocumentsTheLocationHasNoPath()
        {
            // R-07: sin carpeta Documentos la exportación falla (FolderExportWriter: "sin ruta"), nunca escribe en otro lado.
            Assert.That(ExportLocations.Resolve(PlatformKind.Windows, null, () => "")!.Directory, Is.Empty);
            Assert.That(ExportLocations.Resolve(PlatformKind.Windows, null, () => null)!.Directory, Is.Empty);
            Assert.That(ExportLocations.Resolve(PlatformKind.Windows, null, () => "relativa")!.Directory, Is.Empty);
        }

        [Test, Category("AC-STR-09-16")]
        public void MobileHasNoDesktopFolder()
        {
            Assert.That(ExportLocations.Resolve(PlatformKind.Android, null, () => FakeDocuments), Is.Null, "Android: la caché del plugin");
            Assert.That(ExportLocations.Resolve(PlatformKind.Ios, null, () => FakeDocuments), Is.Null, "iOS: sin exportación hasta la 10b");
            Assert.That(FileExporterFactory.Create(PlatformKind.Ios, null, new RecordingLogger()), Is.Null);
            var windows = FileExporterFactory.Create(PlatformKind.Windows, new ExportLocation(FakeDocuments, null), new RecordingLogger());
            Assert.That(windows, Is.InstanceOf<WindowsFileExporter>());
            Assert.That(windows!.Delivery, Is.EqualTo(ExportDelivery.SaveToFolder));
        }

        [Test, Category("AC-STR-09-16")]
        public void ResolveCreatesNothing()
        {
            // m-02: calcular la ruta (también la de la app, con la variable del arnés) no crea carpetas; solo FolderExportWriter al escribir.
            var dataDir = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "elt-" + Guid.NewGuid().ToString("N"));
            var documents = Path.Combine(Path.GetTempPath(), "RowMate.Tests", "elt-docs-" + Guid.NewGuid().ToString("N"));
            var withVariable = ExportLocations.Resolve(PlatformKind.Windows, dataDir, () => documents)!;
            var withoutVariable = ExportLocations.Resolve(PlatformKind.Windows, null, () => documents)!;
            Assert.That(Directory.Exists(dataDir), Is.False);
            Assert.That(Directory.Exists(withVariable.Directory), Is.False);
            Assert.That(Directory.Exists(documents), Is.False);
            Assert.That(Directory.Exists(withoutVariable.Directory), Is.False);
            var exporter = new WindowsFileExporter(withVariable);
            Assert.That(Directory.Exists(withVariable.Directory), Is.False, "construir el exportador tampoco crea la carpeta");
            Assert.That(exporter.Location, Is.SameAs(withVariable));
        }
    }
}
