using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // F-APP-19 (también toca): .github/workflows/ci-core.yml y tools/dotnet/run-tests.ps1 (02-tech-design §2.6 y §7).
    // La corrida del CI no se verifica desde un test (ci-core.yml corre en GitHub Actions desde que el repositorio es público): acá se revisa el texto.
    [TestFixture]
    public sealed class CiWorkflowTests
    {
        private const string BuildArgs = "'build', 'tools/dotnet/RowMate.sln', '--configuration', 'Release', '--artifacts-path', $ArtifactsPath";
        private const string TestArgs = "'test', 'tools/dotnet/RowMate.sln', '--configuration', 'Release', '--no-build', '--artifacts-path', $ArtifactsPath";

        [Test, Category("AC-APP-19-01")]
        public void RunsOnPushAndPrOnWindowsAndLinuxWithoutUnityDartOrSecrets()
        {
            var workflow = Workflow();
            Assert.That(workflow, Does.Match(@"(?m)^on:\s*$"));
            Assert.That(workflow, Does.Match(@"(?m)^\s+push:"));
            Assert.That(workflow, Does.Match(@"(?m)^\s+pull_request:"));
            Assert.That(workflow, Does.Contain("os: [ubuntu-latest, windows-latest]"));
            Assert.That(workflow, Does.Contain("actions/setup-dotnet@v4"));
            Assert.That(workflow, Does.Contain("global-json-file: global.json"));
            Assert.That(workflow, Does.Contain("ROWMATE_CI: \"1\""));
            foreach (var forbidden in new[] { "secrets.", "UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD", "game-ci", "unityci", "setup-dart",
                "dart-lang", "flutter", "STRAVA_" })
            {
                Assert.That(workflow.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase), Is.LessThan(0), "ci-core.yml no puede usar " + forbidden);
            }

            var globalJson = File.ReadAllText(RepoPaths.Combine("global.json"));
            Assert.That(globalJson, Does.Contain("\"version\": \"8.0.427\""));
            Assert.That(globalJson, Does.Contain("\"rollForward\": \"latestFeature\""));
        }

        [Test, Category("AC-APP-19-02")]
        public void FailingTestFailsJob()
        {
            // Sin continue-on-error: un test rojo deja el job rojo y la consola (verbosity=normal) lo nombra.
            Assert.That(Workflow(), Does.Not.Contain("continue-on-error"));
            var script = Script();
            Assert.That(script, Does.Contain("console;verbosity=$verbosity"));
            Assert.That(script, Does.Contain("$verbosity = 'normal'"));
            Assert.That(script, Does.Contain("exit $buildExit"));
            Assert.That(script, Does.Contain("exit $testExit"));
        }

        [Test, Category("AC-APP-19-04")]
        public void LocalEqualsCi()
        {
            // El workflow corre el mismo script que la PC, y el script tiene los dos comandos de §2.6.
            Assert.That(Workflow(), Does.Contain("run: ./tools/dotnet/run-tests.ps1 -Ci"));
            Assert.That(Workflow(), Does.Contain("shell: pwsh"));
            var script = Script();
            Assert.That(script, Does.Contain(BuildArgs));
            Assert.That(script, Does.Contain(TestArgs));
            Assert.That(script, Does.Contain("[string]$ArtifactsPath = 'tools/dotnet/.artifacts'"));
            Assert.That(script, Does.Contain("'--results-directory', $results"));
            Assert.That(script, Does.Contain("$results = $ArtifactsPath + '/TestResults'"));
            Assert.That(script, Does.Contain("'trx;LogFilePrefix=rowmate-core'"));
            Assert.That(Workflow(), Does.Contain("path: tools/dotnet/.artifacts/TestResults/"));
        }

        [Test, Category("AC-APP-19-04"), Category("AC-BASE-08")]
        public void ScriptBuildsWholeSolutionThenTestsWithoutRebuilding()
        {
            // QA-01: dotnet test solo compila los proyectos de test; UnityApiCheck y Tokens se compilan con dotnet build.
            var script = Script();
            var buildArgs = script.IndexOf(BuildArgs, StringComparison.Ordinal);
            var testArgs = script.IndexOf(TestArgs, StringComparison.Ordinal);
            var buildRun = script.IndexOf("& $dotnet @build", StringComparison.Ordinal);
            var exitOnBuild = script.IndexOf("if ($buildExit -ne 0)", StringComparison.Ordinal);
            var testRun = script.IndexOf("& $dotnet @test", StringComparison.Ordinal);
            Assert.That(buildArgs, Is.GreaterThanOrEqualTo(0), "falta el dotnet build de tools/dotnet/RowMate.sln");
            Assert.That(testArgs, Is.GreaterThanOrEqualTo(0), "falta el dotnet test --no-build de tools/dotnet/RowMate.sln");
            Assert.That(buildRun, Is.GreaterThan(0));
            Assert.That(exitOnBuild, Is.GreaterThan(buildRun), "después del build tiene que cortar si falla");
            Assert.That(testRun, Is.GreaterThan(exitOnBuild), "el test va después del build y de su chequeo");
            Assert.That(script.Substring(buildRun, testRun - buildRun), Does.Contain("$buildExit = $LASTEXITCODE"));
            Assert.That(script, Does.Not.Match(@"'build',\s*'[^']*\.csproj'"), "el build es de la solución entera, no de un proyecto");
        }

        [Test, Category("AC-APP-19-04")]
        public void ScriptsAreAsciiForWindowsPowerShell51()
        {
            // PowerShell 5.1 lee un .ps1 UTF-8 sin BOM como ANSI: los scripts son solo ASCII (02-tech-design §2.6, §6.1).
            foreach (var relative in new[] { "tools/dotnet/run-tests.ps1", "tools/oracle/run-oracle.ps1", "tools/unity/run-unity-tests.ps1" })
            {
                var bytes = File.ReadAllBytes(RepoPaths.Combine(relative));
                for (var i = 0; i < bytes.Length; i++)
                {
                    Assert.That(bytes[i], Is.LessThan(128), relative + ": byte no ASCII en la posición " + i);
                }

                var text = File.ReadAllText(RepoPaths.Combine(relative));
                Assert.That(text, Does.Not.Contain("?."), relative + ": sin ?. (no existe en PowerShell 5.1)");
                Assert.That(text, Does.Not.Contain("??"), relative + ": sin ?? (no existe en PowerShell 5.1)");
                Assert.That(Regex.IsMatch(text, @"\S\s*&&\s*\S"), Is.False, relative + ": sin && (no existe en PowerShell 5.1)");
            }
        }

        // QA-V-07 de la It. 1: con -Demo cada línea [DEMO] sale UNA vez. Con verbosity=detailed el logger de consola repetía
        // la salida de cada test en "Standard Output Messages" después del eco en vivo del adaptador de NUnit.
        [Test, Category("AC-BASE-09"), Category("QA-V-07")]
        public void DemoPrintsEachLineOnce()
        {
            var script = Script();
            Assert.That(script, Does.Not.Contain("'detailed'"), "-Demo no usa verbosity=detailed");
            Assert.That(script, Does.Contain("$verbosity = 'normal'"));
            var demo = script.IndexOf("if ($Demo) {", StringComparison.Ordinal);
            Assert.That(demo, Is.GreaterThan(0));
            var block = script.Substring(demo, script.IndexOf('}', demo) - demo);
            Assert.That(block, Does.Contain("$Filter = 'TestCategory=Demo'"));
            Assert.That(block, Does.Contain("$runSettings += 'NUnit.ConsoleOut=1'"), "el eco en vivo queda explícito, no depende del valor por defecto");
            Assert.That(block, Does.Not.Contain("$verbosity ="), "-Demo no cambia la verbosidad");
            Assert.That(script, Does.Contain("if ($runSettings.Count -gt 0) { $test += @('--') + $runSettings }"));
        }

        [Test, Category("AC-APP-19-01"), Ignore("[CI] no se verifica desde un test: es la corrida de ci-core.yml en GitHub Actions (ubuntu-latest y windows-latest) del repositorio público")]
        public void RunsOnGitHub()
        {
        }

        [Test, Category("AC-APP-19-02"), Ignore("[CI] no se verifica desde un test: en GitHub Actions un test rojo deja rojo el job de ci-core.yml (run-tests.ps1 sale con el código de dotnet test)")]
        public void RedJobOnGitHub()
        {
        }

        private static string Workflow()
        {
            return File.ReadAllText(RepoPaths.Combine(".github/workflows/ci-core.yml")).Replace("\r\n", "\n");
        }

        private static string Script()
        {
            return File.ReadAllText(RepoPaths.Combine("tools/dotnet/run-tests.ps1")).Replace("\r\n", "\n");
        }
    }
}
