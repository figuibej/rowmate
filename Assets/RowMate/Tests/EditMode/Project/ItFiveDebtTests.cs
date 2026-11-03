using System;
using System.IO;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // I5D · Revisión estática de la deuda de la 4b que se salda al empezar la It. 5 (05-series-parciales/00-deuda-4b):
    // - QA-V4b-06 (§2): AppRoot precalienta el HUD después de arrancar la app y lo desarma en Update; los argumentos de la medida
    //   de la entrada (-rowmate-perf-entries, -rowmate-no-hud-warmup) solo existen en el Editor y en desarrollo, el build normal
    //   los busca, y la medida solo corre con la carpeta de datos de desarrollo.
    // - QA-V4b-R2-03 (§3): los dos scripts de tools/unity aceptan el 0xC0000005 del cierre si el define quedó como se pidió y, si
    //   no, repiten el paso una vez; siguen siendo solo ASCII (Windows PowerShell 5.1) y el README lo explica.
    // - O-R2-01 de la 4b: el TestResults.xml de la carpeta de datos del Editor se borra solo si lo escribió la corrida.
    [TestFixture]
    public sealed class ItFiveDebtTests
    {
        private const string AppRoot = "Assets/RowMate/Bootstrap/AppRoot.cs";
        private const string Inspector = "Assets/RowMate/Editor/Build/BuildOutputInspector.cs";
        private const string UnityTests = "tools/unity/run-unity-tests.ps1";
        private const string Captures = "tools/unity/capture-snapshots.ps1";
        private const string Readme = "tools/unity/README.md";
        private const string DevelopmentOnly = "#if UNITY_EDITOR || DEVELOPMENT_BUILD";

        [Test, Category("QA-V4b-06"), Category("AC-BLE-13-02")]
        public void PerfArgumentsOnlyInDevelopmentBuilds()
        {
            var root = Read(AppRoot);
            foreach (var token in new[] { "\"-rowmate-perf-entries\"", "\"-rowmate-no-hud-warmup\"", "ArgumentValue(PerfEntriesArgument)",
                         "HasArgument(NoHudWarmupArgument)", "private async System.Threading.Tasks.Task RunPerfEntries(" })
            {
                var at = root.IndexOf(token, StringComparison.Ordinal);
                Assert.That(at, Is.GreaterThan(0), "AppRoot: " + token);
                Assert.That(InsideDevelopmentOnly(root, at), Is.True, "AppRoot: " + token + " fuera de " + DevelopmentOnly);
            }

            var inspector = Read(Inspector);
            Assert.That(inspector, Does.Contain("\"-rowmate-perf-entries\",").And.Contain("\"-rowmate-no-hud-warmup\","),
                "el build normal se revisa también por los argumentos nuevos");
            var perf = root.Substring(root.IndexOf("RunPerfEntries(string countText", StringComparison.Ordinal));
            Assert.That(perf.Substring(0, perf.IndexOf("Application.Quit(1);", StringComparison.Ordinal)), Does.Contain("!DataLocation.IsOverridden"),
                "la medida nunca escribe en la base del usuario");
        }

        [Test, Category("QA-V4b-06")]
        public void AppRootWarmsUpTheHudAfterStartingTheApp()
        {
            var root = SourceScan.StripComments(Read(AppRoot));
            var start = root.IndexOf("_app.Start();", StringComparison.Ordinal);
            var warm = root.IndexOf("_warmup = new HudWarmup(_shell.Shell.Ui, root);", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            Assert.That(warm, Is.GreaterThan(start), "después de arrancar la app, en el mismo cuadro (Start)");
            Assert.That(root, Does.Contain("if (_warmup != null && _warmup.Tick())"), "lo desarma Update");
            Assert.That(root, Does.Contain("_warmup?.Dispose();"), "y OnDestroy, si se corta");
        }

        [Test, Category("QA-V4b-R2-03")]
        public void DefineStepsSurviveTheCrashOnExit()
        {
            foreach (var script in new[] { UnityTests, Captures })
            {
                var text = Read(script);
                Assert.That(text, Does.Contain("$crashCodes = @(-1073741819, 3221225477)"), script);
                Assert.That(text, Does.Contain("function Test-DefineAsAsked([string]$Method) {"), script);
                Assert.That(text, Does.Contain("if ($Method -eq $enableMethod) { return ((Test-SimulatorInSettings) -and (Test-Path $markerFile)) }"),
                    script + ": al activar, el define y la marca");
                Assert.That(text, Does.Contain("if (Test-Path $markerFile) { return $false }"), script + ": al restaurar, sin la marca");
                Assert.That(text, Does.Contain("$script:definesBefore = $wasInSettings"), script + ": y como estaba antes");
                var invoke = BootstrapperStaticTests.Body(text, "function Invoke-Method");
                Assert.That(invoke, Does.Contain("if ($crashCodes -contains $code) {"));
                Assert.That(invoke, Does.Contain("if (Test-DefineAsAsked $Method) {"));
                Assert.That(invoke, Does.Contain("-retry.log"), script + ": una sola repetición, con su registro");
                Assert.That(CountOf(invoke, "Invoke-Unity @("), Is.EqualTo(2), script + ": a lo sumo dos corridas del paso");
                var bytes = File.ReadAllBytes(RepoPaths.Combine(script));
                for (var i = 0; i < bytes.Length; i++)
                {
                    Assert.That(bytes[i], Is.LessThan(128), script + ": byte no ASCII en la posición " + i + " (PowerShell 5.1)");
                }
            }

            Assert.That(Read(Readme), Does.Contain("QA-V4b-R2-03").And.Contain("0xC0000005"));
        }

        [Test, Category("QA-V4b-R2-03")]
        public void PlayModeRunsRemoveOnlyTheirOwnEditorTestResults()
        {
            var text = Read(UnityTests);
            var remove = BootstrapperStaticTests.Body(text, "function Remove-EditorTestResults");
            Assert.That(remove, Does.Contain("(Get-Item $file).LastWriteTime -ge $Since"), "solo si lo escribió esta corrida");
            Assert.That(remove, Does.Contain("'\\TestResults.xml'"), "solo ese archivo");
            Assert.That(text, Does.Contain("$runStarted = Get-Date"));
            Assert.That(text, Does.Contain("if ($Platform -eq 'PlayMode') { Remove-EditorTestResults $runStarted }"));
        }

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }

        private static int CountOf(string text, string token)
        {
            var count = 0;
            for (var at = text.IndexOf(token, StringComparison.Ordinal); at >= 0; at = text.IndexOf(token, at + token.Length, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }

        private static bool InsideDevelopmentOnly(string code, int position)
        {
            var open = code.LastIndexOf(DevelopmentOnly, position, StringComparison.Ordinal);
            if (open < 0)
            {
                return false;
            }

            var between = code.Substring(open, position - open);
            return !between.Contains("#endif") && !between.Contains("#else");
        }
    }
}
