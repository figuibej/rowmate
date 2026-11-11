using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I15T · Las herramientas de la It. 15 que se revisan por texto (15-deuda-pc/02-tech-design §2, §4; AC-D15-03, -16 d, -38): la
    // matriz con los DeviceOnlyIds, "sin red" en Exportar como prueba automática, el set it15 de las capturas en su carpeta y
    // -rowmate-perf-history solo en desarrollo. Los dos últimos son de archivos de WP-B (capture-snapshots.ps1, SnapshotCli,
    // AppRoot, BuildOutputInspector): mientras WP-B no los escribió quedan ignorados con el motivo, y WP-B quita la guarda al cerrar
    // (no se cierra la iteración con un Ignore: AC-D15-16, #71).
    [TestFixture]
    public sealed class ItFifteenToolingTests
    {
        private const string RunTests = "tools/dotnet/run-tests.ps1";
        private const string Captures = "tools/unity/capture-snapshots.ps1";
        private const string SnapshotCli = "Assets/RowMate/Snapshots/SnapshotCli.cs";
        private const string Inspector = "Assets/RowMate/Editor/Build/BuildOutputInspector.cs";
        private const string AppRoot = "Assets/RowMate/Bootstrap/AppRoot.cs";
        private const string It15Folder = "docs/migration/iterations/15-deuda-pc/snapshots";

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }

        [Test, Category("AC-D15-38")]
        public void MatrixReadsDeviceOnlyIds()
        {
            // #78: -Matrix lee los bloques DeviceOnlyIds de los It*Ids.cs y los informa aparte (EN EQUIPO), no como SIN TEST.
            var script = Read(RunTests);
            Assert.That(script, Does.Contain("(Get-BlockIds $text 'DeviceOnlyIds')"), "lee el bloque");
            Assert.That(script, Does.Contain("elseif ($deviceOnly.Contains($id)) { $state = 'EN EQUIPO' }"));
            Assert.That(script, Does.Contain("$lines.Add('## EN EQUIPO')"), "la sección aparte");
            Assert.That(script, Does.Contain("', EN EQUIPO ' + $summary['EN EQUIPO']"), "en el resumen");
            var order = new[] { "{ $state = 'FALLA' }", "{ $state = 'PASA' }", "{ $state = 'EN EQUIPO' }", "{ $state = 'SIN TEST' }" };
            var at = -1;
            foreach (var step in order)
            {
                var next = script.IndexOf(step, StringComparison.Ordinal);
                Assert.That(next, Is.GreaterThan(at), "un test que pasa o falla manda sobre EN EQUIPO: " + step);
                at = next;
            }

            // La expresión de los IDs reconoce AC-D15-nn (antes solo AC-LETRAS-nn): sin eso la It. 15 no entraba en la matriz.
            var quoted = Regex.Match(script, @"\[regex\]::Matches\(\$Text, '([^']+)'\)");
            Assert.That(quoted.Success, Is.True, "Get-QuotedIds");
            var ids = new Regex(quoted.Groups[1].Value);
            var it15 = Read("Assets/RowMate/Tests/EditMode/Traceability/It15Ids.cs");
            var found = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in ids.Matches(it15))
            {
                found.Add(m.Groups[1].Value);
            }

            Assert.That(found.Count, Is.EqualTo(40), "los 40 AC de la It. 15");
            Assert.That(found, Does.Contain("AC-D15-37"));
            Assert.That(ids.IsMatch("\"AC-HIS-13-06\"") && ids.IsMatch("\"AC-BASE-09\"") && ids.IsMatch("\"LT-01\""), Is.True, "los de antes siguen");

            // El bloque de la It. 15, como lo lee el script (la misma expresión que Get-BlockIds).
            var block = Regex.Match(it15, @"DeviceOnlyIds\s*=\s*\{([^}]*)\}");
            Assert.That(block.Success, Is.True);
            Assert.That(block.Groups[1].Value, Does.Contain("\"AC-D15-03\"").And.Contain("\"AC-D15-19\"").And.Contain("\"AC-D15-37\""));
        }

        [Test, Category("AC-D15-16")]
        public void ExportHasNoNetworkAccess()
        {
            // #71 (AC-D15-16 d; AC-STR-09-11): Exportar TCX no usa la red. Antes era una revisión a mano.
            var patterns = new[]
            {
                new Regex(@"\bSystem\s*\.\s*Net\b"), new Regex(@"\busing\s+System\.Net\b"), new Regex(@"\bHttpClient\b"),
                new Regex(@"\bUnityWebRequest\b"), new Regex(@"\bWebClient\b"), new Regex(@"\bWebRequest\b"), new Regex(@"\bSocket\b"),
            };
            var files = SourceScan.CsFiles("Core/Export", "App/Export", "Infrastructure/Export", "Platform/Export");
            Assert.That(files.Count, Is.GreaterThanOrEqualTo(15), "las cuatro carpetas de Exportar");
            var offenders = new List<string>();
            foreach (var file in files)
            {
                offenders.AddRange(SourceScan.Matches(file, patterns, true));
            }

            Assert.That(offenders, Is.Empty, string.Join("\n", offenders));
        }

        [Test, Category("AC-D15-40")]
        public void CaptureSetIt15WritesToItsFolder()
        {
            var script = Read(Captures);
            if (!script.Contains("'it15'"))
            {
                Assert.Ignore("pendiente WP-B: capture-snapshots.ps1 todavía no tiene el set it15");
            }

            Assert.That(script, Does.Contain("[string]$Set = 'it15',"), "el set de la 15, por defecto");
            Assert.That(script, Does.Contain("if ($Set -eq 'it15') { return '" + It15Folder + "' }"), "su carpeta, y no la de la 14");
            Assert.That(Read(SnapshotCli), Does.Contain("public const string It15Output = \"" + It15Folder + "\";"));
        }

        [Test, Category("AC-D15-03")]
        public void PerfHistoryIsDevelopmentOnly()
        {
            var root = Read(AppRoot);
            if (!root.Contains("-rowmate-perf-history"))
            {
                Assert.Ignore("pendiente WP-B: AppRoot todavía no tiene -rowmate-perf-history");
            }

            // El argumento existe solo en desarrollo: BuildOutputInspector lo busca en el build normal (DiagnosticNeedles).
            var inspector = Read(Inspector);
            Assert.That(inspector, Does.Contain("\"-rowmate-perf-history\","), "aguja del inspector");
            var build = Read("tools/build/build.ps1");
            Assert.That(build, Does.Contain("-PerfHistory").And.Contain("ROWMATE_DATA_DIR"), "el humo siembra F-9P en datos temporales");
        }
    }
}
