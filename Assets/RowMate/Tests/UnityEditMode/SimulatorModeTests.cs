using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.EditorTools;
using RowMate.Platform.Input;
using UnityEditor;

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
            Assert.That(update, Does.Contain("_shell.HandleBack()"));
        }
    }
}
