using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.EditorTools;
using RowMate.Platform.Input;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.TestTools;

namespace RowMate.Tests.UnityEditMode
{
    // Modo simulador y builds (F-BLE-13; AC-BLE-13-01, -02; AC-APP-01-02; AC-APP-02-06), con la API del Editor.
    public sealed class SimulatorModeTests
    {
        [Test, Category("AC-BLE-13-01")]
        public void SimulatorMenuExists()
        {
            Assert.That(Menu.GetEnabled(SimulatorDefine.MenuPath) || SimulatorDefine.TestRunInProgress, Is.True, SimulatorDefine.MenuPath);
        }

        [Test, Category("AC-BLE-13-01")]
        public void NonDevelopmentBuildWithSimulatorFails()
        {
            Assert.That(SimulatorBuildGuard.Evaluate(false, true, false, "Standalone"), Does.Contain("R-01"), "con el define y sin Development: falla");
            Assert.That(SimulatorBuildGuard.Evaluate(false, true, true, "Standalone"), Is.Null, "Development con simulador: pasa");
            Assert.That(SimulatorBuildGuard.Evaluate(false, false, false, "Standalone"), Is.Null, "build normal: pasa");
            Assert.That(SimulatorBuildGuard.Evaluate(true, false, true, "Standalone"), Does.Contain("RestoreOnly"), "durante los tests falla cualquiera");
        }

        [Test, Category("AC-BLE-13-01")]
        public void BuildCliForcesDevelopmentWithSimulator()
        {
            var simulator = new BuildRequest("Windows", true, "Mono", "Builds/Windows/simulator", false);
            var options = BuildCli.OptionsFor(simulator, new[] { "Assets/RowMate/Scenes/Boot.unity" });
            Assert.That((options.options & BuildOptions.Development) != 0, Is.True);
            Assert.That(options.extraScriptingDefines, Is.EqualTo(new[] { "ROWMATE_SIMULATOR" }));
            var normal = BuildCli.OptionsFor(new BuildRequest("Windows", false, "Mono", "Builds/Windows/normal", false), new[] { "x" });
            Assert.That((normal.options & BuildOptions.Development) != 0, Is.False);
            Assert.That(normal.extraScriptingDefines, Is.Null.Or.Empty);
            Assert.That(simulator.ScriptingBackend, Is.EqualTo(ScriptingImplementation.Mono2x), "Mono temporal (D-16)");
        }

        // QA-V2-11 (It. 3): la guardia del ensamblado del simulador, compilada como player. Sin Development no compila (el
        // #error de R-01); con Development, sí. Se compila una copia fuera de Assets con AssemblyBuilder (sin UNITY_EDITOR).
        [UnityTest, Category("AC-BLE-13-01"), Category("QA-V2-11")]
        public IEnumerator SimulatorDoesNotCompileInANonDevelopmentPlayer()
        {
            var dir = Path.GetFullPath(Path.Combine("Temp", "rowmate-guard-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(dir);
            try
            {
                var source = Path.Combine(dir, "SimulatorReleaseGuard.cs");
                File.Copy("Assets/RowMate/Simulator/SimulatorReleaseGuard.cs", source);
                var release = new List<CompilerMessage>();
                yield return CompilePlayer(source, Path.Combine(dir, "Release.dll"), AssemblyBuilderFlags.None, release);
                Assert.That(release.Exists(m => m.type == CompilerMessageType.Error && m.message.Contains("R-01")), Is.True,
                    "sin Development: #error de R-01\n" + Describe(release));
                var development = new List<CompilerMessage>();
                yield return CompilePlayer(source, Path.Combine(dir, "Development.dll"), AssemblyBuilderFlags.DevelopmentBuild, development);
                Assert.That(development.Exists(m => m.type == CompilerMessageType.Error), Is.False, "con Development compila\n" + Describe(development));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private static IEnumerator CompilePlayer(string source, string output, AssemblyBuilderFlags flags, List<CompilerMessage> messages)
        {
            var builder = new AssemblyBuilder(output, source)
            {
                flags = flags,
                additionalDefines = new[] { "ROWMATE_SIMULATOR" },
                referencesOptions = ReferencesOptions.UseEngineModules,
            };
            var finished = false;
            builder.buildFinished += (_, result) =>
            {
                messages.AddRange(result);
                finished = true;
            };
            var started = false;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (!started && clock.Elapsed.TotalSeconds < 120)
            {
                started = builder.Build();
                if (!started)
                {
                    yield return null;
                }
            }

            Assert.That(started, Is.True, "AssemblyBuilder.Build no arrancó");
            while (!finished && clock.Elapsed.TotalSeconds < 120)
            {
                yield return null;
            }

            Assert.That(finished, Is.True, "AssemblyBuilder no terminó");
            Assert.That(builder.defaultDefines, Has.No.Member("UNITY_EDITOR"), "compila como player");
        }

        private static string Describe(List<CompilerMessage> messages)
        {
            var lines = new List<string>();
            foreach (var message in messages)
            {
                lines.Add(message.type + ": " + message.message);
            }

            return string.Join("\n", lines);
        }

        // QA-V2-03 (It. 3): sin carpetas Variations/*il2cpp* el backend IL2CPP no está instalado (BuildCli sale con 3).
        [Test, Category("AC-BASE-01"), Category("QA-V2-03")]
        public void Il2CppIsDetectedFromTheVariationsFolder()
        {
            var root = Path.Combine(Path.GetTempPath(), "rowmate-engine-" + Guid.NewGuid().ToString("N"));
            try
            {
                Assert.That(BuildCli.HasIl2CppVariation(null), Is.False);
                Assert.That(BuildCli.HasIl2CppVariation(root), Is.False, "sin carpeta");
                Directory.CreateDirectory(Path.Combine(root, "Variations", "win64_player_development_mono"));
                Assert.That(BuildCli.HasIl2CppVariation(root), Is.False, "solo Mono");
                Directory.CreateDirectory(Path.Combine(root, "Variations", "win64_player_nondevelopment_il2cpp"));
                Assert.That(BuildCli.HasIl2CppVariation(root), Is.True, "con IL2CPP");
                var windows = BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.StandaloneWindows64, BuildOptions.None);
                Assert.That(Directory.Exists(Path.Combine(windows, "Variations")), Is.True, "la carpeta real existe: " + windows);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        // QA-V2-02 (It. 3): lo que deja el paquete de tests de rendimiento se borra, con su .meta y la carpeta vacía.
        [Test, Category("AC-BLE-13-02"), Category("QA-V2-02")]
        public void PerformanceTestRunFilesAreRemovedBeforeTheBuild()
        {
            if (Directory.Exists(PerformanceTestRunCleaner.ResourcesFolder))
            {
                Assert.Ignore(PerformanceTestRunCleaner.ResourcesFolder + " ya existe: la prueba no toca una carpeta del proyecto");
            }

            Directory.CreateDirectory(PerformanceTestRunCleaner.ResourcesFolder);
            foreach (var file in PerformanceTestRunCleaner.Files)
            {
                File.WriteAllText(file, "{}");
                File.WriteAllText(file + ".meta", "fileFormatVersion: 2");
            }

            Assert.That(PerformanceTestRunCleaner.Clean(), Is.EqualTo(5), "2 json, 2 .meta y la carpeta");
            Assert.That(Directory.Exists(PerformanceTestRunCleaner.ResourcesFolder), Is.False);
            Assert.That(File.Exists(PerformanceTestRunCleaner.ResourcesFolder + ".meta"), Is.False);
            Assert.That(PerformanceTestRunCleaner.Clean(), Is.EqualTo(0), "sin nada que borrar");
        }

        // QA-V2-07 (It. 3): qué se saca de los guardados fuera del bootstrapper.
        [Test, Category("AC-BASE-10"), Category("QA-V2-07")]
        public void GeneratedFontsAndBatchModeUrpAreNotSaved()
        {
            Assert.That(GeneratedAssetGuard.IsProtected(FontAssetPathsFirst(), false), Is.True, "fuente generada: siempre");
            Assert.That(GeneratedAssetGuard.IsProtected(FontAssetPathsFirst(), true), Is.True);
            Assert.That(GeneratedAssetGuard.IsProtected("Assets/RowMate/Settings/URP/RowMate_URP_Default.asset", true), Is.True, "URP en batchmode");
            Assert.That(GeneratedAssetGuard.IsProtected("Assets/RowMate/Settings/URP/RowMate_URP_Default.asset", false), Is.False, "URP en la ventana del Editor");
            Assert.That(GeneratedAssetGuard.IsProtected("ProjectSettings/ProjectSettings.asset", true), Is.False, "los defines se guardan (SimulatorDefine)");
            Assert.That(GeneratedAssetGuard.IsProtected("Assets/RowMate/Scenes/Boot.unity", true), Is.False);
        }

        private static string FontAssetPathsFirst()
        {
            return RowMate.Core.Design.FontAssetPaths.All[0];
        }

        [Test, Category("AC-BLE-13-02")]
        public void BuildOutputInspectorFindsSimulatorTraces()
        {
            var root = Path.Combine(Path.GetTempPath(), "rowmate-inspector-" + Guid.NewGuid().ToString("N"));
            try
            {
                var managed = Path.Combine(root, "RowMate_Data", "Managed");
                Directory.CreateDirectory(managed);
                File.WriteAllBytes(Path.Combine(managed, "RowMate.Bootstrap.dll"), Encoding.UTF8.GetBytes("limpio"));
                Assert.That(BuildOutputInspector.FindSimulatorTraces(root), Is.Empty, "sin rastros");
                File.WriteAllBytes(Path.Combine(managed, "RowMate.Presentation.dll"), Encoding.Unicode.GetBytes("xx SimulatedRowerService yy"));
                Assert.That(BuildOutputInspector.FindSimulatorTraces(root), Has.Some.Contains("UTF-16"));
                File.WriteAllBytes(Path.Combine(managed, "RowMate.Simulator.dll"), new byte[] { 1, 2, 3 });
                Assert.That(BuildOutputInspector.FindSimulatorTraces(root), Has.Some.Contains("ensamblado del simulador"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        // QA-V2-02, -09 y -10 (It. 3): el build normal tampoco lleva los textos del simulador, los argumentos de diagnóstico
        // ni los metadatos de tests de rendimiento; también en *.assets y en los archivos sin extensión (level0, …).
        [Test, Category("AC-BLE-13-02"), Category("QA-V2-10")]
        public void BuildOutputInspectorFindsTextsDiagnosticsAndTestMetadata()
        {
            var root = Path.Combine(Path.GetTempPath(), "rowmate-inspector-" + Guid.NewGuid().ToString("N"));
            try
            {
                var data = Path.Combine(root, "RowMate_Data");
                Directory.CreateDirectory(Path.Combine(data, "Managed"));
                File.WriteAllBytes(Path.Combine(data, "Managed", "RowMate.Bootstrap.dll"), Encoding.UTF8.GetBytes("limpio"));
                File.WriteAllBytes(Path.Combine(data, "level0"), Encoding.UTF8.GetBytes("limpio"));
                Assert.That(BuildOutputInspector.FindForbiddenTraces(root), Is.Empty, "sin rastros");
                var cases = new[]
                {
                    ("Managed/RowMate.Presentation.dll", Encoding.Unicode, "Pulsómetro simulado"),
                    ("Managed/RowMate.App.dll", Encoding.Unicode, "Remo simulado"),
                    ("level0", Encoding.UTF8, "SIMULADOR"),
                    ("Managed/RowMate.Bootstrap.dll", Encoding.Unicode, "-rowmate-color-probe"),
                    ("Managed/RowMate.Platform.dll", Encoding.Unicode, "-rowmate-ui-pipeline"),
                    ("resources.assets", Encoding.UTF8, "PerformanceTestRunInfo"),
                    ("sharedassets0.assets", Encoding.UTF8, "PerformanceTestRunSettings"),
                };
                foreach (var (file, encoding, needle) in cases)
                {
                    var path = Path.Combine(data, file);
                    var before = File.Exists(path) ? File.ReadAllBytes(path) : null;
                    File.WriteAllBytes(path, encoding.GetBytes("xx " + needle + " yy"));
                    Assert.That(BuildOutputInspector.FindForbiddenTraces(root), Has.Some.Contains("\"" + needle + "\""), file + ": " + needle);
                    if (before == null)
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        File.WriteAllBytes(path, before);
                    }
                }

                Assert.That(BuildOutputInspector.FindSimulatorTraces(root), Is.Empty, "FindSimulatorTraces sigue siendo solo el simulador");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        // En el código que llega a un build, un solo #if ROWMATE_SIMULATOR elige servicios: el de DeviceBackendFactory.
        // SimulatorLayerFactory solo agrega la capa; BuildFlags solo informa; los de Tests/UiSupport y Snapshots no llegan.
        [Test, Category("AC-APP-01-02")]
        public void OnlyDeviceBackendFactoryChoosesTheVariant()
        {
            var choosers = new List<string>();
            foreach (var folder in new[] { "Core", "App", "Platform", "Presentation", "Bootstrap", "Infrastructure", "Scene" })
            {
                foreach (var file in Directory.GetFiles(Path.Combine("Assets/RowMate", folder), "*.cs", SearchOption.AllDirectories))
                {
                    var text = File.ReadAllText(file);
                    if (text.Contains("#if ROWMATE_SIMULATOR") && Regex.IsMatch(text, @"SimulatorBackend\s*\.\s*Create\s*\("))
                    {
                        choosers.Add(file.Replace('\\', '/'));
                    }
                }
            }

            Assert.That(choosers, Is.EqualTo(new[] { "Assets/RowMate/Bootstrap/DeviceBackendFactory.cs" }));
            Assert.That(File.ReadAllText("Assets/RowMate/Bootstrap/SimulatorLayerFactory.cs"), Does.Not.Contain("SimulatorBackend.Create"));
        }

        [Test, Category("AC-APP-02-06")]
        public void BackButtonPolicyByApiLevel()
        {
            Assert.That(BackButtonPolicy.ActionFor(true, 31), Is.EqualTo(BackButtonPolicy.BackAction.MoveTaskToBack));
            Assert.That(BackButtonPolicy.ActionFor(true, 35), Is.EqualTo(BackButtonPolicy.BackAction.MoveTaskToBack));
            Assert.That(BackButtonPolicy.ActionFor(true, 30), Is.EqualTo(BackButtonPolicy.BackAction.Quit));
            Assert.That(BackButtonPolicy.ActionFor(true, 26), Is.EqualTo(BackButtonPolicy.BackAction.Quit));
        }

        [Test, Category("AC-APP-02-06")]
        public void DesktopBackPolicyDoesNothing()
        {
            Assert.That(BackButtonPolicy.ActionFor(false, 0), Is.EqualTo(BackButtonPolicy.BackAction.Nothing));
            var policy = BackButtonPolicy.ForCurrentPlatform();
            policy.OnBackAtRoot();
            Assert.That(policy.OnBackCalls, Is.EqualTo(1), "en el Editor no cierra nada");
        }

        [Test, Category("AC-APP-02-06")]
        public void AppRootRoutesEscapeToHandleBack()
        {
            var code = File.ReadAllText("Assets/RowMate/Bootstrap/AppRoot.cs");
            var update = code.Substring(code.IndexOf("private void Update()", StringComparison.Ordinal));
            update = update.Substring(0, update.IndexOf("private void OnApplicationFocus", StringComparison.Ordinal));
            Assert.That(update, Does.Contain("escapeKey.wasPressedThisFrame"));
            Assert.That(update, Does.Contain("_shell.HandleBack(_backSource)"), "It. 3: SystemBack en Android, Escape en el resto (P-08)");
        }
    }
}
