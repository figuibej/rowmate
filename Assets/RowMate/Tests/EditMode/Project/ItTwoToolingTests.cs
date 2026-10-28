using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // Revisión estática de las herramientas de la It. 2 (02-tech-design §1.1 "Herramientas y CI", §4.6; WP-B): el arnés de
    // capturas (tools/unity/capture-snapshots.ps1), los builds (tools/build/build.ps1 + BuildCli) y el CI de Unity opcional
    // (.github/workflows/ci-unity.yml). Scripts en ASCII para Windows PowerShell 5.1, el define siempre restaurado, el build
    // de simulador siempre Development y el normal revisado por BuildOutputInspector.
    [TestFixture]
    public sealed class ItTwoToolingTests
    {
        private const string CaptureScript = "tools/unity/capture-snapshots.ps1";
        private const string WindowScript = "tools/unity/capture-window.ps1";
        private const string BuildScript = "tools/build/build.ps1";
        private const string CiUnity = ".github/workflows/ci-unity.yml";

        [Test, Category("QA-G2-16")]
        public void ScriptsAreAsciiForWindowsPowerShell51()
        {
            foreach (var relative in new[] { CaptureScript, WindowScript, BuildScript })
            {
                var path = RepoPaths.Combine(relative);
                Assert.That(File.Exists(path), Is.True, "falta " + relative);
                var bytes = File.ReadAllBytes(path);
                for (var i = 0; i < bytes.Length; i++)
                {
                    Assert.That(bytes[i], Is.LessThan(128), relative + ": byte no ASCII en la posición " + i);
                }

                var text = File.ReadAllText(path);
                Assert.That(text, Does.Not.Contain("?."), relative + ": sin ?. (no existe en PowerShell 5.1)");
                Assert.That(text, Does.Not.Contain("??"), relative + ": sin ??");
                Assert.That(Regex.IsMatch(text, @"\S\s*&&\s*\S"), Is.False, relative + ": sin &&");
            }
        }

        [Test, Category("AC-BASE-05")]
        public void CaptureScriptEnablesRunsTheHarnessAndAlwaysRestores()
        {
            var script = File.ReadAllText(RepoPaths.Combine(CaptureScript));
            Assert.That(script, Does.Contain("$enableMethod = 'RowMate.EditorTools.SimulatorDefine.EnableForTests'"));
            Assert.That(script, Does.Contain("$restoreMethod = 'RowMate.EditorTools.SimulatorDefine.RestoreAfterTests'"));
            Assert.That(script, Does.Contain("$harnessMethod = 'RowMate.Snapshots.SnapshotCli.Run'"));
            Assert.That(script, Does.Contain("Temp\\UnityLockfile"));
            Assert.That(script, Does.Contain("exit 10"));
            var harness = BootstrapperStaticTests.Body(script, "function Invoke-Harness");
            Assert.That(harness, Does.Not.Contain("'-quit'"), "el arnés corre SIN -quit (sale solo con EditorApplication.Exit)");
            var tryAt = script.LastIndexOf("try {", StringComparison.Ordinal);
            var finallyAt = script.LastIndexOf("finally {", StringComparison.Ordinal);
            Assert.That(tryAt, Is.GreaterThan(0));
            Assert.That(finallyAt, Is.GreaterThan(tryAt));
            Assert.That(script.Substring(tryAt, finallyAt - tryAt), Does.Contain("Invoke-Harness"), "el arnés corre dentro de try");
            Assert.That(script.Substring(finallyAt), Does.Contain("Restore-Simulator $true $wasInSettings"), "SIEMPRE restaura y verifica el define");
            Assert.That(script, Does.Contain("if ($Check -and $now -ne $WasInSettings) {"));
        }

        [Test, Category("AC-BLE-13-01")]
        public void BuildScriptForcesDevelopmentWithSimulator()
        {
            var script = File.ReadAllText(RepoPaths.Combine(BuildScript));
            Assert.That(script, Does.Contain("'RowMate.EditorTools.BuildCli.Build'"));
            Assert.That(script, Does.Contain("-rowmateSimulator"));
            Assert.That(script, Does.Not.Contain("SetScriptingDefineSymbols"), "el define va por extraScriptingDefines, nunca a PlayerSettings");
            var request = SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Build/BuildRequest.cs")));
            Assert.That(request, Does.Contain("public bool Development => Simulator || SmokeTest;"));
            var cli = SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Build/BuildCli.cs")));
            Assert.That(cli, Does.Contain("options = request.Development ? BuildOptions.Development : BuildOptions.None"));
            Assert.That(cli, Does.Contain("options.extraScriptingDefines = new[] { SimulatorSymbol };"));
            Assert.That(cli, Does.Not.Contain("SetScriptingDefineSymbols"));
        }

        [Test, Category("AC-BLE-13-02")]
        public void BuildScriptInspectsNormalBuilds()
        {
            var cli = SourceScan.StripComments(File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Build/BuildCli.cs")));
            var normal = cli.IndexOf("if (!request.Simulator)", StringComparison.Ordinal);
            Assert.That(normal, Is.GreaterThan(0));
            Assert.That(cli.Substring(normal), Does.Contain("BuildOutputInspector.FindForbiddenTraces(request.OutputFolder)"),
                "simulador, diagnósticos y metadatos de tests (QA-V2-02, -09, -10)");
            Assert.That(cli.Substring(normal), Does.Contain("return 2;"), "con rastros, el build normal falla");
            var inspector = File.ReadAllText(RepoPaths.Combine("Assets/RowMate/Editor/Build/BuildOutputInspector.cs"));
            Assert.That(inspector, Does.Contain("Encoding.Unicode.GetBytes"), "busca también en UTF-16");
            Assert.That(inspector, Does.Contain("\"RowMate.Simulator\""));
        }

        [Test, Category("AC-APP-19-05")]
        public void CiUnityRunsEditAndPlayMode()
        {
            var path = RepoPaths.Combine(CiUnity);
            Assert.That(File.Exists(path), Is.True, "falta " + CiUnity);
            var workflow = File.ReadAllText(path);
            Assert.That(workflow, Does.Contain("game-ci/unity-test-runner"));
            Assert.That(workflow, Does.Contain("testMode: [EditMode, PlayMode]"));
            Assert.That(workflow, Does.Contain("UNITY_LICENSE"), "solo con la licencia en los secretos");
            Assert.That(workflow, Does.Contain("workflow_dispatch"), "opcional: a pedido");
            Assert.That(workflow, Does.Not.Contain("ROWMATE_SIMULATOR"), "el CI no activa el simulador en ningún build");
        }

        [Test, Category("AC-BASE-05")]
        public void VideosGoThroughGitLfsAndBuildsAreIgnored()
        {
            Assert.That(File.ReadAllText(RepoPaths.Combine(".gitattributes")), Does.Contain("*.mp4 filter=lfs diff=lfs merge=lfs -text"));
            Assert.That(File.ReadAllText(RepoPaths.Combine(".gitignore")), Does.Contain("/[Bb]uilds/"));
        }
    }
}
