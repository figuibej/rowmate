using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I15H · El arnés de la It. 15 que se revisa por texto (RowMate.Snapshots es solo del Editor y ningún test lo referencia: AsmdefLintTests):
    // una sonda de aspecto que falla hace fallar la toma (#35, AC-D15-05), AUX-15-01 en el manifest y fuera del INDEX de arte (AC-D15-34),
    // el título de la 14 (AC-D15-31) y CAP-01/02 de la 14 con su origen tools/icon. Sin la categoría AC: esos son [Editor] (UnityOnlyIds) y los
    // cierran las tomas; esto solo fija el código.
    [TestFixture]
    public sealed class ItFifteenHarnessTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }

        [Test, Category("I15H")]
        public void FailingAspectFailsShot()
        {
            var manifest = Read("Assets/RowMate/Snapshots/SnapshotManifest.cs");
            Assert.That(manifest, Does.Contain("if (!probe.Pass)\n"), "Passed: cualquier sonda que falla, también la de aspecto");
            Assert.That(manifest, Does.Not.Contain("!probe.Pass && !probe.Aspect"), "ya no se ignora la de aspecto");
            var row = Read("Assets/RowMate/Presentation/History/StatRowView.cs");
            Assert.That(row, Does.Contain("_namePrefix = prefix;"), "SetNamePrefix guarda el prefijo (#35)");
        }

        [Test, Category("I15H")]
        public void It15HasAux01OutsideArtIndex()
        {
            var manifest = Read("Assets/RowMate/Snapshots/SnapshotManifest.cs");
            Assert.That(manifest, Does.Contain("## Tomas auxiliares (diagnóstico, fuera del índice de arte)"));
            Assert.That(manifest, Does.Contain("return id.StartsWith(\"AUX-15-\", StringComparison.Ordinal);"));
            var debt = Read("Assets/RowMate/Snapshots/DebtCatalog.cs");
            Assert.That(debt, Does.Contain("\"AUX-15-01\""));
            Assert.That(debt, Does.Contain("HullOff(h, true)").And.Contain("HullOff(h, false)"), "el casco se apaga para la toma y se vuelve a prender");
            Assert.That(debt, Does.Not.Contain("WaterMarks").And.Not.Contain("ShaderArt"), "sin estela (gate QA-G15-01)");
        }

        [Test, Category("I15H")]
        public void It14TitleAndIconOrigin()
        {
            var catalog = Read("Assets/RowMate/Snapshots/SnapshotCatalog.cs");
            Assert.That(catalog, Does.Contain("return \"It. 14 (Pulido, plataformas y deuda)\";"), "INDEX: Capturas de la It. 14");
            Assert.That(catalog, Does.Contain("return DebtCatalog.All;"));
            var polish = Read("Assets/RowMate/Snapshots/PolishCatalog.cs");
            Assert.That(polish, Does.Contain("SupersededBy = \"tools/icon\""), "CAP-01/02 con su origen tools/icon");
        }
    }
}
