using System;
using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I5T · Las herramientas de WP-B de la It. 5 (05-series-parciales/02-tech-design §3.7, §5, §7.2): el conjunto it5 por defecto con su
    // carpeta, la regla de toma con las fases de la 5, HudOverlap y WrenchAudit con los paneles, la semilla F5 con las preferencias al
    // lado de la base, y las medidas del build solo en desarrollo (el inspector del build normal las busca). Lee los fuentes como texto
    // (dotnet no compila el Editor ni el arnés).
    [TestFixture]
    public sealed class ItFiveToolingTests
    {
        private const string CaptureScript = "tools/unity/capture-snapshots.ps1";
        private const string Cli = "Assets/RowMate/Snapshots/SnapshotCli.cs";
        private const string Catalog = "Assets/RowMate/Snapshots/SeriesCatalog.cs";
        private const string Rule = "Assets/RowMate/Snapshots/SnapshotTakeRule.cs";
        private const string Runner = "Assets/RowMate/Snapshots/SnapshotRunner.cs";
        private const string Audit = "Assets/RowMate/Snapshots/WrenchAudit.cs";
        private const string AppRoot = "Assets/RowMate/Bootstrap/AppRoot.cs";
        private const string Inspector = "Assets/RowMate/Editor/Build/BuildOutputInspector.cs";
        private const string It5 = "docs/migration/iterations/05-series-parciales/snapshots";
        private const string DevelopmentOnly = "#if UNITY_EDITOR || DEVELOPMENT_BUILD";

        private static string Read(string path)
        {
            return File.ReadAllText(RepoPaths.Combine(path));
        }

        [Test, Category("AC-BASE-09")]
        public void TheIt5SetStaysSelectable()
        {
            // It. 6a: it6a es el por defecto desde la 6a (ItSixAToolingTests); it5 sigue con su carpeta (la regeneración única de la 6a).
            var script = Read(CaptureScript);
            Assert.That(script, Does.Contain("'it5', 'it4b'"));
            Assert.That(script, Does.Contain("if ($Set -eq 'it5') { return '" + It5 + "' }"), "su carpeta de salida");
            Assert.That(script, Does.Contain("if ($Set -eq 'it4b') { return 'docs/migration/iterations/04b-hud-exr/snapshots' }"), "it4b sigue (la regeneración)");
            var bytes = File.ReadAllBytes(RepoPaths.Combine(CaptureScript));
            for (var i = 0; i < bytes.Length; i++)
            {
                Assert.That(bytes[i], Is.LessThan(128), CaptureScript + ": byte no ASCII en la posición " + i + " (PowerShell 5.1)");
            }

            var cli = Read(Cli);
            Assert.That(cli, Does.Contain("public const string It5Output = \"" + It5 + "\";"));
            Assert.That(cli, Does.Contain("public const string DefaultSet = \"it8\";"), "desde la 6a, it6a por defecto");
            Assert.That(File.Exists(RepoPaths.Combine(Catalog)), Is.True, "SeriesCatalog");
        }

        [Test, Category("AC-BASE-09")]
        public void TheTakeRuleKnowsThePhasesOfIterationFive()
        {
            var rule = Read(Rule);
            Assert.That(rule, Does.Contain("if (motion is TabularCells cells)"), "las cifras de los paneles, como un TabularNumber");
            Assert.That(rule, Does.Contain("if (motion is SeriesPanelMotion panel)"), "las fases de SeriesPanelMotion");
            Assert.That(rule.IndexOf("motion is SeriesPanelMotion", StringComparison.Ordinal),
                Is.LessThan(rule.IndexOf("motion is ISettlingMotion", StringComparison.Ordinal)), "antes del resorte genérico (D1-02)");
            Assert.That(rule, Does.Contain("panel.IsTimedPhaseRunning"), "una fase de duración fija bloquea siempre");
            var runner = Read(Runner);
            Assert.That(runner, Does.Contain("WrenchAudit.SeriesProblems(host)"), "las áreas de toque y la zona del bote en toda toma");
            Assert.That(runner, Does.Contain("SeriesCatalog.Describe(host)"), "y los paneles en el manifest");
            var audit = Read(Audit);
            Assert.That(audit, Does.Contain("rects.AddRange(SeriesRects(host));"), "HudOverlap con los rectángulos visuales de los paneles");
            Assert.That(audit, Does.Contain("la llave tapa #"), "WrenchAudit con los paneles");
        }

        [Test, Category("AC-WKT-18-10"), Category("AC-HIS-20-04")]
        public void TheCatalogUsesAMemoryPreferencesPerTakeAndNeverAFile()
        {
            var catalog = Read(Catalog);
            Assert.That(catalog, Does.Contain("Preferences = Prefs(laps, reps),"), "una MemoryPreferences por toma");
            Assert.That(catalog, Does.Contain("var prefs = new MemoryPreferences();"));
            Assert.That(catalog, Does.Not.Contain("PreferencesFile = true"), "ninguna toma lee ni escribe un archivo de preferencias");
            Assert.That(catalog, Does.Not.Contain("JsonFilePreferences"));
            var cli = Read(Cli);
            Assert.That(cli, Does.Contain("seedName != \"F5\""), "-seed F5");
            Assert.That(cli, Does.Contain("WritePreferences(file, prefs!);"), "-prefs, al lado de la base sembrada (que pasó TempDatabase.Guard)");
        }

        [Test, Category("AC-BASE-09")]
        public void ThePerfArgumentsAreDevelopmentOnly()
        {
            var root = Read(AppRoot);
            foreach (var token in new[] { "\"-rowmate-perf-series\"", "\"-rowmate-perf-routines\"", "private async System.Threading.Tasks.Task RunPerfSeries(" })
            {
                var at = root.IndexOf(token, StringComparison.Ordinal);
                Assert.That(at, Is.GreaterThan(0), "AppRoot: " + token);
                var open = root.LastIndexOf(DevelopmentOnly, at, StringComparison.Ordinal);
                var close = root.LastIndexOf("#endif", at, StringComparison.Ordinal);
                Assert.That(open, Is.GreaterThan(close), "AppRoot: " + token + " fuera de " + DevelopmentOnly);
            }

            var perf = root.Substring(root.IndexOf("RunPerfSeries(string secondsText)", StringComparison.Ordinal));
            Assert.That(perf.Substring(0, perf.IndexOf("Application.Quit(1);", StringComparison.Ordinal)), Does.Contain("!DataLocation.IsOverridden"),
                "la medida nunca escribe en la base ni en las preferencias del usuario");
            var inspector = Read(Inspector);
            Assert.That(inspector, Does.Contain("\"-rowmate-perf-routines\",").And.Contain("\"-rowmate-perf-series\","), "el build normal se revisa también por estos");
        }
    }
}
