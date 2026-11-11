using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // It. 14 (14-pulido-plataformas/02-tech-design §8.2, §9, DT14-12, DT14-31; AC-APP-19-11, AC-APP-16-06, -10): las herramientas de la 14
    // (dotnet, sin Unity): la sección "It. 14: CI" del README con los 7 secretos, el conjunto it14 por defecto en su carpeta, el humo del
    // arranque aislado y la verificación del VS_VERSIONINFO en todo build de Windows.
    [TestFixture]
    public sealed class ItFourteenToolingTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative)).Replace("\r\n", "\n");
        }

        [Test, Category("AC-APP-19-11")]
        public void ReadmeCiSection()
        {
            var readme = Read("tools/unity/README.md");
            var start = readme.IndexOf("## It. 14: CI", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), "falta la sección It. 14: CI");
            var next = readme.IndexOf("\n## ", start + 5, StringComparison.Ordinal);
            var section = next > 0 ? readme.Substring(start, next - start) : readme.Substring(start);
            foreach (var secret in new[] { "UNITY_LICENSE", "UNITY_EMAIL", "UNITY_PASSWORD", "ANDROID_KEYSTORE_BASE64", "ANDROID_KEYSTORE_PASS",
                "ANDROID_KEYALIAS_NAME", "ANDROID_KEYALIAS_PASS" })
            {
                Assert.That(section, Does.Contain("`" + secret + "`"), secret);
            }

            foreach (var artifact in new[] { "RowMate-Windows", "RowMate-Android", "-debugsigned", "RowMate-iOS-unsigned", "RowMate-macOS-unsigned" })
            {
                Assert.That(section, Does.Contain(artifact), artifact);
            }

            Assert.That(section, Does.Contain("Run workflow"));
            Assert.That(Regex.IsMatch(section, @"[A-Za-z]:\\|/Users/|/home/|\\Users\\"), Is.False, "sin rutas de la PC");
        }

        [Test, Category("AC-APP-11-01")]
        public void SnapshotSetIt14ByDefaultInItsFolder()
        {
            var script = Read("tools/unity/capture-snapshots.ps1");
            // It. 15 (WP-A-14): desde la 15 el defecto es it15 (ItFifteenToolingTests.CaptureSetIt15WritesToItsFolder); it14 sigue con su carpeta.
            Assert.That(script, Does.Contain("'it14'"));
            Assert.That(script, Does.Not.Contain("[string]$Set = 'it14',"));
            Assert.That(script, Does.Contain("if ($Set -eq 'it14') { return 'docs/migration/iterations/14-pulido-plataformas/snapshots' }"));
            var cli = Read("Assets/RowMate/Snapshots/SnapshotCli.cs");
            Assert.That(cli, Does.Contain("public const string It14Output = \"docs/migration/iterations/14-pulido-plataformas/snapshots\";"));
            Assert.That(cli, Does.Contain("return It14Output;"));
            var catalog = Read("Assets/RowMate/Snapshots/SnapshotCatalog.cs");
            Assert.That(catalog, Does.Contain("return PolishCatalog.All;"));
            Assert.That(Read("Assets/RowMate/Snapshots/PolishCatalog.cs"), Does.Contain("TextAuditProbe.Violations"), "toda toma pasa por la auditoría de textos");
        }

        [Test, Category("AC-APP-16-10"), Category("AC-APP-15-05")]
        public void BootSmokeIsIsolated()
        {
            var script = Read("tools/build/build.ps1");
            var start = script.IndexOf("if ($BootSmoke -and $code -eq 0) {", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            var block = script.Substring(start, script.IndexOf("[build] -BootSmoke: ok", start, StringComparison.Ordinal) - start);
            Assert.That(block, Does.Contain("'RowMate.Tests\\boot-smoke-'"), "carpeta de datos temporal propia");
            Assert.That(block, Does.Contain("$env:ROWMATE_DATA_DIR = $bootDir"));
            Assert.That(block, Does.Contain("$env:ROWMATE_BLE = 'off'"));
            Assert.That(block, Does.Contain("finally {"));
            Assert.That(block, Does.Contain("Remove-Item -Recurse -Force -Path $bootDir"));
            Assert.That(block, Does.Contain("'-screen-fullscreen', '0', '-screen-width', '800', '-screen-height', '480'"));
            Assert.That(block, Does.Contain("'-rowmate-perf-boot'").And.Contain("'-rowmate-boot-splash', 'off'"));
            Assert.That(block, Does.Contain("WaitForExit(30000)"));
            Assert.That(block, Does.Contain("Crashes"));
            Assert.That(script, Does.Contain("$development = $Simulator -or $SmokeTest -or $DbSmoke -or $BleSmoke -or $ExportSmoke -or $BootSmoke -or $CloseSmoke"), "build de desarrollo");
        }

        // QA-V14-01: el humo de cierre. 5 WM_CLOSE en ventana (sin -screen-*: la ventana inicial y la subclase como en la app) y 2 en
        // pantalla completa, cada uno con salida 0 y sin carpetas nuevas en Crashes, con datos temporales y sin Bluetooth (código 11).
        [Test, Category("AC-APP-15-05")]
        public void CloseSmokeIsIsolated()
        {
            var script = Read("tools/build/build.ps1");
            var start = script.IndexOf("if ($CloseSmoke -and $code -eq 0) {", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            var block = script.Substring(start, script.IndexOf("[build] -CloseSmoke: ok", start, StringComparison.Ordinal) - start);
            Assert.That(block, Does.Contain("'RowMate.Tests\\close-smoke-'"), "carpeta de datos temporal propia");
            Assert.That(block, Does.Contain("$env:ROWMATE_DATA_DIR = $closeDir"));
            Assert.That(block, Does.Contain("$env:ROWMATE_BLE = 'off'"));
            Assert.That(block, Does.Contain("finally {"));
            Assert.That(block, Does.Contain("Remove-Item -Recurse -Force -Path $closeDir"));
            Assert.That(block, Does.Contain("CloseMainWindow()"), "WM_CLOSE, el camino de la X y de Alt+F4");
            Assert.That(block, Does.Contain("'-screen-fullscreen', '1'"), "también en pantalla completa");
            Assert.That(block, Does.Contain("$closeRuns = 7"));
            Assert.That(block, Does.Contain("ExitCode -ne 0"));
            Assert.That(block, Does.Contain("Crashes"));
            Assert.That(block, Does.Contain("subclase de la ventana retirada"), "el registro dice que el original volvió");
            Assert.That(block, Does.Contain("$code = 11"));
            Assert.That(script, Does.Contain("[switch]$CloseSmoke,"));
        }

        [Test, Category("AC-APP-16-06")]
        public void ExeVersionInfoChecked()
        {
            var script = Read("tools/build/build.ps1");
            Assert.That(script, Does.Contain("if ($code -eq 0 -and $Target -eq 'Windows') {"));
            Assert.That(script, Does.Contain("(Get-Item (Join-Path (Get-FullPath $Out) 'RowMate.exe')).VersionInfo"));
            Assert.That(script, Does.Contain("$code = 9"));
            var post = Read("Assets/RowMate/Editor/Build/WindowsVersionInfoPostprocess.cs");
            Assert.That(post, Does.Contain("VersionInfoBlock.Build(PlayerSettings.productName, PlayerSettings.companyName, PlayerSettings.bundleVersion)"));
            Assert.That(post, Does.Contain("UpdateResourceW").And.Contain("BuildFailedException"));
            var bytes = File.ReadAllBytes(RepoPaths.Combine("tools/build/build.ps1"));
            foreach (var b in bytes)
            {
                Assert.That(b, Is.LessThan(128), "build.ps1 en ASCII (Windows PowerShell 5.1)");
            }
        }

        [Test, Category("AC-POL-03")]
        public void AndroidHapticsWithoutPermissionsAndGolden()
        {
            var kotlin = Read("Assets/Plugins/Android/RowMateUi/RmHaptics.kt");
            Assert.That(kotlin, Does.Contain("HapticFeedbackConstants.CLOCK_TICK").And.Contain("HapticFeedbackConstants.VIRTUAL_KEY")
                .And.Contain("HapticFeedbackConstants.CONFIRM"));
            Assert.That(kotlin, Does.Not.Contain("VIBRATE").And.Not.Contain("UnityPlayer"));
            Assert.That(File.Exists(RepoPaths.Combine("Assets/Plugins/Android/RowMateUi/AndroidManifest.xml")), Is.False, "sin manifiesto");
            var golden = Read("native/android/golden/ui-signatures.txt");
            Assert.That(golden, Does.Contain("bind (Landroid/app/Activity;)V").And.Contain("perform (I)Z"));
            Assert.That(Read("native/android/check.ps1"), Does.Contain("ui-signatures.txt"));
        }
    }
}
