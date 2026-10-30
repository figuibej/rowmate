using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // ITT4 · Revisión estática de las herramientas de la It. 4a (04a-motor-hud-funcional/02-tech-design §7.1, §8.2; WP-B3): el
    // arnés con el conjunto it4a por defecto y su carpeta, la auditoría de la llave y HudOverlap en cada foto fija (DD4a-04), y
    // ninguna ruta de la PC en lo que el arnés versiona (manifest.json e INDEX.md; QA-G4aT-05).
    [TestFixture]
    public sealed class ItFourAToolingTests
    {
        private const string CaptureScript = "tools/unity/capture-snapshots.ps1";
        private const string Runner = "Assets/RowMate/Snapshots/SnapshotRunner.cs";
        private const string Cli = "Assets/RowMate/Snapshots/SnapshotCli.cs";
        private const string Catalog = "Assets/RowMate/Snapshots/WorkoutCatalog.cs";
        private const string Snapshots = "docs/migration/iterations/04a-motor-hud-funcional/snapshots";

        // Una ruta absoluta de una PC: unidad de Windows, carpeta de usuario o de temporales.
        private static readonly Regex MachinePath = new Regex(@"(?<![A-Za-z])[A-Za-z]:[\\/]|[\\/]Users[\\/]|[\\/]home[\\/]|AppData[\\/]|\\\\[A-Za-z0-9]", RegexOptions.IgnoreCase);

        [Test, Category("DD4a-04")]
        public void CaptureScriptKeepsTheIt4aSet()
        {
            // It. 4b (§5.6): it4b es el conjunto por defecto (ItFourBToolingTests); it4a sigue con su carpeta (CAP-01 a CAP-11 y el
            // registro de las tomas reemplazadas).
            var script = File.ReadAllText(RepoPaths.Combine(CaptureScript));
            Assert.That(script, Does.Contain("[ValidateSet('it4b', 'it4a', 'it3', 'it2')]"));
            Assert.That(script, Does.Contain("if ($Set -eq 'it4a') { return '" + Snapshots + "' }"), "su carpeta de salida");
            Assert.That(script, Does.Contain("if ($Set -eq 'it3') { return 'docs/migration/iterations/03-rutinas/snapshots' }"), "it3 sigue igual");
            Assert.That(script, Does.Contain("if ($Set -eq 'it2') { return 'docs/migration/iterations/02-dispositivo-simulador/snapshots' }"), "it2 también");
            var bytes = File.ReadAllBytes(RepoPaths.Combine(CaptureScript));
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.That(bytes[i], Is.LessThan(128), CaptureScript + ": byte no ASCII en la posición " + i + " (PowerShell 5.1)");
            }
        }

        [Test, Category("DD4a-04")]
        public void CliKnowsTheIt4aSet()
        {
            var cli = File.ReadAllText(RepoPaths.Combine(Cli));
            Assert.That(cli, Does.Contain("public const string It4aOutput = \"" + Snapshots + "\";"));
            Assert.That(cli, Does.Contain("public const string DefaultSet = \"it4b\";"), "it4b por defecto desde la 4b");
            Assert.That(File.Exists(RepoPaths.Combine(Catalog)), Is.True, "WorkoutCatalog");
        }

        [Test, Category("DD4a-04")]
        public void RunnerAuditsTheWrenchAndTheHudInEveryStill()
        {
            var runner = File.ReadAllText(RepoPaths.Combine(Runner));
            var capture = BootstrapperStaticTests.Body(runner, "private void Capture(");
            Assert.That(capture, Does.Contain("WrenchAudit.Run(host)"), "la auditoría de la llave en toda foto fija (DD4a-04)");
            Assert.That(capture, Does.Contain("WrenchAudit.HudOverlap(host)"), "y HudOverlap en el inmersivo");
            Assert.That(capture, Does.Contain("record.Problems.AddRange("), "lo que encuentran hace fallar la toma");
            var audit = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Snapshots/WrenchAudit.cs"));
            Assert.That(audit, Does.Contain("public const float Margin = 8;"), "la llave agrandada 8 dp");
            foreach (var control in new[] { "\"rm-button\"", "\"rm-icon-button\"", "\"rm-chip__pill\"", "\"rm-fab\"", "\"rm-tab\"", "\"rm-segmented__option\"",
                         "\"rm-menu__item\"", "\"rm-routine-card__chevron\"", "\"rm-field__box\"" })
            {
                Assert.That(audit, Does.Contain(control), "la auditoría mira " + control);
            }
        }

        [Test, Category("QA-G4aT-05")]
        public void SnapshotOutputsHaveNoMachinePaths()
        {
            var folder = RepoPaths.Combine(Snapshots);
            if (!Directory.Exists(folder))
            {
                Assert.Ignore("todavía sin " + Snapshots + ": lo escribe tools/unity/capture-snapshots.ps1 -Set it4a");
            }

            var checkedFiles = 0;
            foreach (var name in new[] { "manifest.json", "INDEX.md" })
            {
                var path = Path.Combine(folder, name);
                if (!File.Exists(path))
                {
                    continue;
                }

                checkedFiles++;
                var lines = File.ReadAllLines(path);
                for (var i = 0; i < lines.Length; i++)
                {
                    Assert.That(MachinePath.IsMatch(lines[i]), Is.False, name + ":" + (i + 1) + ": ruta de la PC en lo que se versiona");
                }
            }

            Assert.That(checkedFiles, Is.EqualTo(2), "manifest.json e INDEX.md");
        }

        [Test, Category("QA-G4aT-05")]
        public void TheCheckCatchesMachinePaths()
        {
            Assert.That(MachinePath.IsMatch("\"output\": \"C:\\\\algo\\\\snapshots\""), Is.True);
            Assert.That(MachinePath.IsMatch("D:/datos/x.png"), Is.True);
            Assert.That(MachinePath.IsMatch("/Users/alguien/x"), Is.True);
            Assert.That(MachinePath.IsMatch("docs/migration/iterations/04a-motor-hud-funcional/snapshots/CAP-01.png"), Is.False);
            Assert.That(MachinePath.IsMatch("| CAP-12 | TV | 150 W · 24 spm: 02:13 |"), Is.False);
            Assert.That(MachinePath.IsMatch("https://github.com/figuibej/rowmate-legacy"), Is.False, "una URL no es una ruta de la PC");
        }
    }
}
