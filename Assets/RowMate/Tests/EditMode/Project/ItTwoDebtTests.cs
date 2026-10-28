using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Project
{
    // Deuda de la It. 2 saldada al empezar la It. 3 (docs/migration/iterations/03-rutinas/00-deuda-it2.md): revisión estática
    // de lo que no se puede probar sin Unity, y que el CI (ci-core.yml) vigila en cada cambio. Lo que necesita el Editor está
    // en Tests/UnityEditMode (SimulatorModeTests, WindowsTitleBarTests) y en PlayMode.
    [TestFixture]
    public sealed class ItTwoDebtTests
    {
        private const string DevelopmentOnly = "#if UNITY_EDITOR || DEVELOPMENT_BUILD";

        // QA-V2-07: los FontAsset generados se versionan sin datos dinámicos (sin glifos, sin caracteres, atlas de 1 × 1),
        // con su tabla de kerning (GeneratedAssetGuard explica por qué). Si un build o una corrida los volviera a escribir con lo
        // que dibujó, este test lo ve en el próximo commit (y en ci-core).
        [Test, Category("AC-APP-05-03"), Category("QA-V2-07")]
        public void GeneratedFontAssetsAreCommittedWithoutDynamicData()
        {
            Assert.That(FontAssetPaths.All.Count, Is.EqualTo(10), "It. 3: + Noto Emoji (QA-G3-12)");
            foreach (var relative in FontAssetPaths.All)
            {
                var path = RepoPaths.Combine(relative);
                if (!File.Exists(path))
                {
                    Assert.Inconclusive(relative + " no existe todavía (lo crea el bootstrapper en el primer open)");
                }

                var text = File.ReadAllText(path).Replace("\r\n", "\n");
                foreach (var empty in new[] { "m_GlyphTable: []", "m_CharacterTable: []", "m_UsedGlyphRects: []" })
                {
                    Assert.That(text, Does.Contain("\n  " + empty + "\n"), relative + ": " + empty);
                }

                Assert.That(text, Does.Contain("\n  m_AtlasPopulationMode: 1\n"), relative + ": dinámico");
                Assert.That(text, Does.Contain("\n  m_ClearDynamicDataOnBuild: 1\n"), relative);
                Assert.That(text, Does.Contain("\n    m_GlyphPairAdjustmentRecords:"), relative + ": con su tabla de kerning");
                Assert.That(Regex.IsMatch(text, @"\n  m_Width: 1\n  m_Height: 1\n"), Is.True, relative + ": atlas de 1 × 1");
                Assert.That(Regex.Matches(text, @"\n  image data: (\d+)\n").Count, Is.EqualTo(1), relative + ": un solo atlas");
                Assert.That(Regex.Match(text, @"\n  image data: (\d+)\n").Groups[1].Value, Is.EqualTo("1"), relative + ": atlas vacío");
            }
        }

        [Test, Category("AC-BASE-10"), Category("QA-V2-07")]
        public void OnlyTheBootstrapperWritesGeneratedAssets()
        {
            var guard = SourceScan.StripComments(Read("Assets/RowMate/Editor/Bootstrap/GeneratedAssetGuard.cs"));
            Assert.That(guard, Does.Contain("class GeneratedAssetGuard : AssetModificationProcessor"));
            Assert.That(guard, Does.Contain("private static string[] OnWillSaveAssets(string[] paths)"));
            Assert.That(guard, Does.Contain("FontAssetPaths.All"));
            Assert.That(guard, Does.Contain("Application.isBatchMode"));
            var bootstrapper = SourceScan.StripComments(Read("Assets/RowMate/Editor/Bootstrap/ProjectBootstrapper.cs"));
            var scope = bootstrapper.IndexOf("using (GeneratedAssetGuard.AllowWrites())", StringComparison.Ordinal);
            Assert.That(scope, Is.GreaterThan(0), "el bootstrapper abre el permiso");
            Assert.That(bootstrapper.IndexOf("RunStep(results, \"urp.provisional\"", StringComparison.Ordinal), Is.GreaterThan(scope));
            Assert.That(bootstrapper.IndexOf("RunStep(results, \"ui.fonts\"", StringComparison.Ordinal), Is.GreaterThan(scope));
            var clears = 0;
            foreach (var file in SourceScan.CsFiles(""))
            {
                if (SourceScan.StripCommentsAndLiterals(File.ReadAllText(file)).Contains("ClearFontAssetData"))
                {
                    clears++;
                }
            }

            Assert.That(clears, Is.EqualTo(0), "nadie vacía los FontAsset: ClearFontAssetData borra también la tabla de kerning");
            var writers = 0;
            foreach (var file in SourceScan.CsFiles("Editor"))
            {
                if (SourceScan.StripComments(File.ReadAllText(file)).Contains("GeneratedAssetGuard.AllowWrites()"))
                {
                    writers++;
                }
            }

            Assert.That(writers, Is.EqualTo(1), "solo ProjectBootstrapper abre AllowWrites fuera de GeneratedAssetGuard");
        }

        // QA-V2-09 (decisión del PO): -rowmate-color-probe y -rowmate-ui-pipeline solo en el Editor y en desarrollo.
        [Test, Category("AC-BLE-13-02"), Category("QA-V2-09")]
        public void CommandLineDiagnosticsOnlyInDevelopmentBuilds()
        {
            var root = Read("Assets/RowMate/Bootstrap/AppRoot.cs");
            foreach (var token in new[] { "\"-rowmate-ui-pipeline\"", "\"-rowmate-color-probe\"", "HasArgument(ColorProbeArgument)",
                         "AddComponent<ColorProbe>()", "ArgumentValue(PipelineArgument)" })
            {
                var at = root.IndexOf(token, StringComparison.Ordinal);
                Assert.That(at, Is.GreaterThan(0), "AppRoot: " + token);
                Assert.That(InsideDevelopmentOnly(root, at), Is.True, "AppRoot: " + token + " fuera de " + DevelopmentOnly);
            }

            var probe = Read("Assets/RowMate/Bootstrap/ColorProbe.cs").Replace("\r\n", "\n");
            var code = SourceScan.StripComments(probe).Trim();
            Assert.That(code, Does.StartWith(DevelopmentOnly), "ColorProbe entero solo en desarrollo");
            Assert.That(code, Does.EndWith("#endif"));
            Assert.That(Regex.Matches(code, @"^#endif", RegexOptions.Multiline).Count, Is.EqualTo(1), "un solo bloque");
            var inspector = Read("Assets/RowMate/Editor/Build/BuildOutputInspector.cs");
            Assert.That(inspector, Does.Contain("\"-rowmate-color-probe\"").And.Contain("\"-rowmate-ui-pipeline\""),
                "el build normal se revisa también por los argumentos");
        }

        // QA-V2-10: el inspector busca los textos que el simulador muestra, tomados de su propio código.
        [Test, Category("AC-BLE-13-02"), Category("QA-V2-10")]
        public void InspectorLooksForTheTextsTheSimulatorShows()
        {
            var inspector = Read("Assets/RowMate/Editor/Build/BuildOutputInspector.cs");
            var sources = new[]
            {
                ("Assets/RowMate/Simulator/SimulatedRowerService.cs", @"public const string DeviceName = ""([^""]+)"";"),
                ("Assets/RowMate/Simulator/SimulatedHeartRateService.cs", @"public const string DeviceName = ""([^""]+)"";"),
                ("Assets/RowMate/Simulator/SimulatorTexts.cs", @"public const string Ribbon = ""([^""]+)"";"),
            };
            foreach (var (file, pattern) in sources)
            {
                var match = Regex.Match(Read(file), pattern);
                Assert.That(match.Success, Is.True, file + ": " + pattern);
                Assert.That(inspector, Does.Contain("\"" + match.Groups[1].Value + "\""), "aguja " + match.Groups[1].Value);
            }

            Assert.That(inspector, Does.Contain("Encoding.Unicode.GetBytes"), "también en UTF-16 (cadenas de los ensamblados)");
            Assert.That(inspector, Does.Contain("\".assets\""), "también en los datos del player");
        }

        // QA-V2-11: el ensamblado del simulador no compila en un player sin Development (dotnet queda afuera).
        [Test, Category("AC-BLE-13-01"), Category("QA-V2-11")]
        public void SimulatorAssemblyRefusesNonDevelopmentPlayers()
        {
            var guard = Read("Assets/RowMate/Simulator/SimulatorReleaseGuard.cs").Replace("\r\n", "\n");
            Assert.That(guard, Does.Contain("\n#if UNITY_5_3_OR_NEWER && ROWMATE_SIMULATOR && !UNITY_EDITOR && !DEVELOPMENT_BUILD\n#error "));
            Assert.That(guard, Does.Contain("R-01"));
            Assert.That(guard.TrimEnd(), Does.EndWith("#endif"));
        }

        // QA-V2-02: los archivos del paquete de tests de rendimiento nunca llegan al build normal ni al repositorio.
        [Test, Category("AC-BLE-13-02"), Category("QA-V2-02")]
        public void PerformanceTestRunFilesNeverReachTheBuildOrTheRepository()
        {
            Assert.That(Read(".gitignore"), Does.Contain("\n/Assets/Resources/PerformanceTestRun*"));
            var cleaner = SourceScan.StripComments(Read("Assets/RowMate/Editor/Build/PerformanceTestRunCleaner.cs"));
            Assert.That(cleaner, Does.Contain("IPreprocessBuildWithReport"));
            var order = Regex.Match(cleaner, @"public int callbackOrder => (\d+);");
            Assert.That(order.Success && int.Parse(order.Groups[1].Value) > 0, Is.True, "corre después de TestRunBuilder (callbackOrder 0)");
            Assert.That(cleaner, Does.Contain("\"/PerformanceTestRunInfo.json\"").And.Contain("\"/PerformanceTestRunSettings.json\""));
            var inspector = Read("Assets/RowMate/Editor/Build/BuildOutputInspector.cs");
            Assert.That(inspector, Does.Contain("\"PerformanceTestRunInfo\"").And.Contain("\"PerformanceTestRunSettings\""));
        }

        // QA-V2-03: sin el módulo IL2CPP el build es NO VERIFICABLE (3) antes de compilar, y cada backend tiene su log.
        [Test, Category("AC-BASE-01"), Category("QA-V2-03")]
        public void MissingIl2CppIsNotVerifiableAndLogsDoNotOverwrite()
        {
            var cli = SourceScan.StripComments(Read("Assets/RowMate/Editor/Build/BuildCli.cs"));
            var check = cli.IndexOf("!HasIl2CppVariation(BuildPipeline.GetPlaybackEngineDirectory(", StringComparison.Ordinal);
            var build = cli.IndexOf("BuildPipeline.BuildPlayer(options)", StringComparison.Ordinal);
            Assert.That(check, Is.GreaterThan(0));
            Assert.That(build, Is.GreaterThan(check), "se comprueba antes de compilar");
            Assert.That(cli.Substring(check, 400), Does.Contain("return 3;"));
            var script = Read("tools/build/build.ps1");
            Assert.That(script, Does.Contain("'-' + $variant + '-' + $Backend.ToLowerInvariant() + '.log'"));
        }

        // QA-V2-14: el CI de Unity también corre las suites del simulador, con el define solo en el checkout del runner.
        [Test, Category("AC-APP-19-05"), Category("QA-V2-14")]
        public void CiUnityRunsTheSimulatorSuitesWithoutCommittingTheDefine()
        {
            var workflow = Read(".github/workflows/ci-unity.yml").Replace("\r\n", "\n");
            Assert.That(workflow, Does.Contain("        simulator: [false, true]\n"));
            Assert.That(workflow, Does.Contain("        if: ${{ matrix.simulator }}\n        run: bash tools/unity/ci-simulator-define.sh enable\n"));
            Assert.That(workflow, Does.Contain("        if: ${{ always() && matrix.simulator }}\n        run: bash tools/unity/ci-simulator-define.sh restore\n"));
            Assert.That(workflow, Does.Contain("customParameters: ${{ matrix.simulator && '" + SimulatorInUnityTests.TestRunArgument + "' || '' }}"));
            Assert.That(workflow, Does.Contain("sim-${{ matrix.simulator }}-${{ hashFiles("), "caché de Library/ separada con y sin el define");
            Assert.That(workflow, Does.Not.Contain("unity-builder"), "no compila players");
            Assert.That(Regex.IsMatch(workflow, @"^    if:.*secrets\.", RegexOptions.Multiline), Is.False, "secrets no va en jobs.<id>.if");
            var script = File.ReadAllBytes(RepoPaths.Combine("tools/unity/ci-simulator-define.sh"));
            for (var i = 0; i < script.Length; i++)
            {
                Assert.That(script[i], Is.LessThan(128), "ci-simulator-define.sh: byte no ASCII en " + i);
                Assert.That(script[i], Is.Not.EqualTo((byte)'\r'), "ci-simulator-define.sh con fin de línea LF");
            }

            var text = Encoding.ASCII.GetString(script);
            Assert.That(text, Does.Contain("set -euo pipefail"));
            Assert.That(text, Does.Contain("marker=\"Library/RowMate/simulator-tests.txt\""), "la misma marca que SimulatorDefine");
            Assert.That(text, Does.Contain("git checkout -- \"$settings\""));
        }

        private static string Read(string relative)
        {
            return File.ReadAllText(RepoPaths.Combine(relative));
        }

        // ¿La posición cae dentro de un bloque #if UNITY_EDITOR || DEVELOPMENT_BUILD (sin un #else ni #endif en el medio)?
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
