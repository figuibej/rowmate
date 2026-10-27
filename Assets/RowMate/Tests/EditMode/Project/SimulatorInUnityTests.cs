using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // QA-V-05 de la It. 1 (AC-BASE-08, R-01): los tests del simulador dentro de Unity. Sin ROWMATE_SIMULATOR, Unity no
    // compila RowMate.Simulator ni RowMate.Tests.Simulator; el procedimiento lo activa SOLO para la corrida y lo restaura
    // siempre (tools/unity/run-unity-tests.ps1 -Simulator + Editor/Simulator/SimulatorDefine.cs), y SimulatorBuildGuard
    // frena los builds. El código de Editor no compila en dotnet: acá se revisa su texto sin comentarios. La corrida real
    // dentro de Unity se verificó en el primer open de la It. 2 (00-primer-open.md §5).
    [TestFixture]
    public sealed class SimulatorInUnityTests
    {
        private const string Symbol = "ROWMATE_SIMULATOR";
        private const string ScriptPath = "tools/unity/run-unity-tests.ps1";
        private const string DefinePath = "Assets/RowMate/Editor/Simulator/SimulatorDefine.cs";
        private const string GuardPath = "Assets/RowMate/Editor/Simulator/SimulatorBuildGuard.cs";
        private const string TestRunVariable = "ROWMATE_SIMULATOR_TEST_RUN";

        [Test, Category("AC-BASE-08"), Category("QA-V-05")]
        public void OnlySimulatorDefineChangesScriptingDefines()
        {
            // R-01: nadie más toca los defines (ni el bootstrapper, ni la app, ni los tests).
            var callers = new List<string>();
            foreach (var file in RepoPaths.Files("Assets/RowMate", "*.cs"))
            {
                if (SourceScan.StripCommentsAndLiterals(File.ReadAllText(file)).Contains("SetScriptingDefineSymbols"))
                {
                    callers.Add(RepoPaths.Relative(file));
                }
            }

            Assert.That(callers, Is.EqualTo(new[] { DefinePath }));
            var code = Code(DefinePath);
            Assert.That(Regex.Matches(code, @"\bSetScriptingDefineSymbols\s*\(").Count, Is.EqualTo(1), "un solo punto de escritura");
            Assert.That(code, Does.Contain("public const string Symbol = \"" + Symbol + "\";"));
            Assert.That(code, Does.Contain("NamedBuildTarget.Standalone"));
            Assert.That(code, Does.Contain("NamedBuildTarget.Android"));
            Assert.That(code, Does.Contain("NamedBuildTarget.iOS"));
        }

        [Test, Category("AC-BASE-08"), Category("QA-V-05")]
        public void ScriptMethodsExistInTheEditorCode()
        {
            // Lo que el script pasa a -executeMethod existe como método público y estático de SimulatorDefine.
            var script = Script();
            var code = Code(DefinePath);
            Assert.That(code, Does.Match(@"\bnamespace\s+RowMate\.EditorTools\s*\{"));
            Assert.That(code, Does.Contain("public static class SimulatorDefine"));
            var methods = Regex.Matches(script, @"'RowMate\.EditorTools\.SimulatorDefine\.(\w+)'");
            Assert.That(methods.Count, Is.EqualTo(2));
            var names = new List<string>();
            foreach (Match match in methods)
            {
                names.Add(match.Groups[1].Value);
                Assert.That(code, Does.Contain("public static void " + match.Groups[1].Value + "()"), match.Groups[1].Value);
            }

            Assert.That(names, Is.EquivalentTo(new[] { "EnableForTests", "RestoreAfterTests" }));
        }

        [Test, Category("AC-BASE-08"), Category("QA-V-05")]
        public void ScriptEnablesRunsAndAlwaysRestores()
        {
            var script = Script();
            Assert.That(script, Does.Contain("$enableMethod = 'RowMate.EditorTools.SimulatorDefine.EnableForTests'"));
            Assert.That(script, Does.Contain("$restoreMethod = 'RowMate.EditorTools.SimulatorDefine.RestoreAfterTests'"));

            // Los métodos de Editor corren con -quit; la corrida de tests, sin -quit (lo exige -runTests).
            Assert.That(script, Does.Contain("@('-batchmode', '-quit', '-projectPath', $repo, '-executeMethod', $Method, '-logFile', $log)"));
            Assert.That(script, Does.Contain("$testArgs = @('-batchmode', '-projectPath', $repo, '-runTests', '-testPlatform', $Platform, '-testResults', $xml, '-logFile', $log)"));

            // Con -Simulator: activar, correr en try y restaurar en finally (aunque los tests fallen o se corten).
            var simulator = script.IndexOf("if ($Simulator) {\n    $enableCode = Invoke-Method $enableMethod", StringComparison.Ordinal);
            Assert.That(simulator, Is.GreaterThan(0), "el bloque -Simulator activa el define primero");
            var tryRun = script.IndexOf("try {\n        $exitCode = Invoke-Unity $testArgs\n    }", simulator, StringComparison.Ordinal);
            var restore = script.IndexOf("finally {\n        Remove-Item Env:" + TestRunVariable + " -ErrorAction SilentlyContinue\n"
                + "        $restoreCode = Restore-Simulator $true $wasInSettings", simulator, StringComparison.Ordinal);
            Assert.That(tryRun, Is.GreaterThan(simulator), "los tests corren dentro de try");
            Assert.That(restore, Is.GreaterThan(tryRun), "la restauración está en finally (y saca la variable de la corrida antes)");

            // La variable de la corrida en curso se pone justo antes de los tests: ni la activación ni la restauración la ven.
            var variable = script.IndexOf("$env:" + TestRunVariable + " = '1'\n    try {\n        $exitCode = Invoke-Unity $testArgs", simulator,
                StringComparison.Ordinal);
            Assert.That(variable, Is.GreaterThan(simulator), "ROWMATE_SIMULATOR_TEST_RUN=1 solo para -runTests");
            Assert.That(Regex.Matches(script, @"\$env:" + TestRunVariable + @"\s*=").Count, Is.EqualTo(1));

            // Si no se pudo activar, igual restaura y no corre tests.
            var failedEnable = script.IndexOf("if ($enableCode -ne 0) {", simulator, StringComparison.Ordinal);
            Assert.That(failedEnable, Is.GreaterThan(simulator));
            Assert.That(script.Substring(failedEnable, tryRun - failedEnable), Does.Contain("Restore-Simulator $true $wasInSettings"));

            // Verifica después en ProjectSettings.asset que el define quedó como antes; -RestoreOnly recupera una corrida cortada.
            Assert.That(script, Does.Contain("$wasInSettings = Test-SimulatorInSettings"));
            Assert.That(script, Does.Contain("if ($Check -and $now -ne $WasInSettings) {"));
            Assert.That(script, Does.Contain("[switch]$RestoreOnly"));

            // Una sola instancia de Unity por proyecto: no arranca si el Editor tiene el proyecto abierto.
            Assert.That(script, Does.Contain("Temp\\UnityLockfile"));
            Assert.That(script, Does.Contain("exit 10"));
        }

        [Test, Category("AC-BASE-08"), Category("QA-V-05")]
        public void MarkerLivesInLibraryAndRestoreKeepsTheOriginalState()
        {
            var code = Code(DefinePath);
            Assert.That(code, Does.Contain("public const string TestMarkerPath = \"Library/RowMate/simulator-tests.txt\";"),
                "la marca va en Library/ (no se versiona)");
            var enable = BootstrapperStaticTests.Body(code, "public static void EnableForTests()");
            Assert.That(enable, Does.Contain("if (!TestRunInProgress)"), "una corrida cortada no pisa el estado original guardado");
            Assert.That(enable, Does.Contain("File.WriteAllLines(TestMarkerPath"));
            var restore = BootstrapperStaticTests.Body(code, "public static void RestoreAfterTests()");
            Assert.That(restore, Does.Contain("Set(Targets[i], before.Contains(Targets[i].TargetName));"), "cada plataforma vuelve a como estaba");
            Assert.That(restore, Does.Contain("File.Delete(TestMarkerPath);"));
            Assert.That(restore.IndexOf("File.Delete(TestMarkerPath);", StringComparison.Ordinal),
                Is.GreaterThan(restore.IndexOf("AssetDatabase.SaveAssets();", StringComparison.Ordinal)), "borra la marca después de guardar");
        }

        [Test, Category("AC-BASE-08"), Category("QA-V-05")]
        public void GuardBlocksBuildsDuringTestsAndReleaseBuildsWithTheDefine()
        {
            var guard = Code(GuardPath);
            Assert.That(guard, Does.Match(@"\bnamespace\s+RowMate\.EditorTools\s*\{"));
            Assert.That(guard, Does.Contain("class SimulatorBuildGuard : IPreprocessBuildWithReport"));
            var body = BootstrapperStaticTests.Body(guard, "public void OnPreprocessBuild(BuildReport report)");
            var duringTests = body.IndexOf("if (SimulatorDefine.TestRunInProgress)", StringComparison.Ordinal);
            Assert.That(duringTests, Is.GreaterThanOrEqualTo(0), "durante una corrida de tests falla cualquier build");
            Assert.That(body, Does.Contain("(report.summary.options & BuildOptions.Development) != 0"));
            Assert.That(body, Does.Contain("if (SimulatorDefine.IsEnabled(target) && !development)"));
            Assert.That(Regex.Matches(body, @"throw new BuildFailedException\(").Count, Is.EqualTo(2));
        }

        // R-01 en el repositorio: ROWMATE_SIMULATOR no puede quedar en los ajustes versionados (ProjectSettings/*.asset o un
        // Build Profile). Sin ProjectSettings.asset (antes del primer open) o sin el define: pasa.
        // - Con la marca de SimulatorDefine (Library/RowMate/simulator-tests.txt) y ROWMATE_SIMULATOR_TEST_RUN=1 (lo pone
        //   run-unity-tests.ps1 -Simulator solo para la corrida de tests): es este mismo test dentro de esa corrida → pasa.
        //   Antes salía Inconclusive, y Unity sale con 2 ("One or more tests failed") si hay uno (primer open real, It. 2).
        // - Con la marca y sin la variable: una corrida cortada → Inconclusive, también en el CI (-RestoreOnly).
        // - Sin la marca: en el CI falla; en la PC queda Inconclusive (el modo simulador del Editor es legítimo mientras se
        //   desarrolla), avisando que hay que sacarlo antes del commit.
        [Test, Category("AC-BASE-08"), Category("QA-V-05")]
        public void CommittedSettingsDoNotEnableTheSimulator()
        {
            var found = new List<string>();
            foreach (var file in SettingsFiles())
            {
                if (File.ReadAllText(file).Contains(Symbol))
                {
                    found.Add(RepoPaths.Relative(file));
                }
            }

            if (found.Count == 0)
            {
                Assert.Pass("ningún ajuste versionado tiene " + Symbol);
            }

            var where = Symbol + " está en " + string.Join(", ", found) + ". ";
            if (File.Exists(RepoPaths.Combine("Library/RowMate/simulator-tests.txt")))
            {
                if (Environment.GetEnvironmentVariable(TestRunVariable) == "1")
                {
                    Assert.Pass(where + "Es la corrida de tests del simulador en curso (run-unity-tests.ps1 -Simulator), que lo "
                        + "restaura al terminar.");
                }

                Assert.Inconclusive(where + "Hay una marca de corrida de tests del simulador (Library/RowMate/simulator-tests.txt) "
                    + "pero ninguna en curso: quedó cortada. Correr tools/unity/run-unity-tests.ps1 -RestoreOnly.");
            }

            var reason = where + "Desactivalo (menú RowMate/Modo simulador) antes de commitear: ningún build de distribución puede "
                + "llevarlo (R-01).";
            if (Environment.GetEnvironmentVariable("ROWMATE_CI") == "1")
            {
                Assert.Fail("[CI] " + reason);
            }

            Assert.Inconclusive(reason);
        }

        private static IEnumerable<string> SettingsFiles()
        {
            var settings = RepoPaths.Combine("ProjectSettings");
            if (Directory.Exists(settings))
            {
                foreach (var file in Directory.GetFiles(settings, "*.asset"))
                {
                    yield return file;
                }
            }

            var profiles = RepoPaths.Combine("Assets/Settings/Build Profiles");
            if (Directory.Exists(profiles))
            {
                foreach (var file in Directory.GetFiles(profiles, "*.asset", SearchOption.AllDirectories))
                {
                    yield return file;
                }
            }
        }

        private static string Code(string relative)
        {
            var path = RepoPaths.Combine(relative);
            Assert.That(File.Exists(path), Is.True, "falta " + relative);
            return SourceScan.StripComments(File.ReadAllText(path)).Replace("\r\n", "\n");
        }

        private static string Script()
        {
            var path = RepoPaths.Combine(ScriptPath);
            Assert.That(File.Exists(path), Is.True, "falta " + ScriptPath);
            return File.ReadAllText(path).Replace("\r\n", "\n");
        }
    }
}
