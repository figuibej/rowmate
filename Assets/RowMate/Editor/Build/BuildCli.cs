using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RowMate.EditorTools
{
    // Builds por línea de comandos (ARCHITECTURE §19, 02-tech-design §1.1; tools/build/build.ps1):
    //   Unity -batchmode -quit -projectPath <repo> -executeMethod RowMate.EditorTools.BuildCli.Build
    //         -rowmateTarget Windows|Android [-rowmateSimulator] [-rowmateBackend Mono|IL2CPP] [-rowmateOutput <carpeta>]
    // - Con -rowmateSimulator: ROWMATE_SIMULATOR por extraScriptingDefines (nunca en PlayerSettings: solo SimulatorDefine los
    //   escribe) y SIEMPRE Development (R-01, AC-BLE-13-01). SimulatorBuildGuard sigue mirando los defines del proyecto.
    // - Sin -rowmateSimulator: al terminar, BuildOutputInspector busca rastros del simulador; si encuentra alguno, falla.
    // - El backend (Mono temporal, D-16) se cambia solo para este build y se restaura en finally.
    // - Si falta el módulo de la plataforma o del backend: NO VERIFICABLE, sale con 3 y lo dice.
    // Códigos: 0 bien; 1 el build falló; 2 rastros del simulador en un build normal; 3 falta un módulo.
    public static class BuildCli
    {
        public const string Prefix = "[BUILD] ";
        private const string SimulatorSymbol = "ROWMATE_SIMULATOR";

        public static void Build()
        {
            var code = Run(BuildRequest.FromCommandLine(Environment.GetCommandLineArgs()));
            EditorApplication.Exit(code);
        }

        public static BuildPlayerOptions OptionsFor(BuildRequest request, string[] scenes)
        {
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = request.LocationPath,
                target = request.BuildTarget,
                targetGroup = request.BuildTargetGroup,
                options = request.Development ? BuildOptions.Development : BuildOptions.None,
            };

            if (request.Simulator)
            {
                options.extraScriptingDefines = new[] { SimulatorSymbol };
            }

            return options;
        }

        public static int Run(BuildRequest request)
        {
            if (!BuildPipeline.IsBuildTargetSupported(request.BuildTargetGroup, request.BuildTarget))
            {
                Debug.LogWarning(Prefix + "NO VERIFICABLE: falta el módulo de " + request.Target + " en este Editor.");
                return 3;
            }

            var scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError(Prefix + "no hay escenas en Build Settings (correr RowMate/Bootstrap/Reaplicar: paso scene.boot).");
                return 1;
            }

            var named = NamedBuildTarget.FromBuildTargetGroup(request.BuildTargetGroup);
            var previousBackend = PlayerSettings.GetScriptingBackend(named);
            try
            {
                PlayerSettings.SetScriptingBackend(named, request.ScriptingBackend);
                Directory.CreateDirectory(request.OutputFolder);
                var options = OptionsFor(request, scenes);
                Debug.Log(Prefix + request.Target + " " + request.Variant + " " + request.ScriptingBackend + (request.Development ? " Development" : "")
                    + " → " + request.LocationPath);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError(Prefix + "el build falló: " + report.summary.result + " (" + report.summary.totalErrors + " errores)");
                    if (request.ScriptingBackend == ScriptingImplementation.IL2CPP)
                    {
                        Debug.LogWarning(Prefix + "NO VERIFICABLE si falta el módulo IL2CPP de " + request.Target + ".");
                    }

                    return 1;
                }

                Debug.Log(Prefix + "listo: " + report.summary.outputPath + " (" + report.summary.totalSize + " bytes)");
                if (!request.Simulator)
                {
                    var traces = BuildOutputInspector.FindSimulatorTraces(request.OutputFolder);
                    if (traces.Count > 0)
                    {
                        Debug.LogError(Prefix + "el build normal tiene rastros del simulador (AC-BLE-13-02):\n" + string.Join("\n", traces));
                        return 2;
                    }

                    Debug.Log(Prefix + "BuildOutputInspector: sin rastros del simulador.");
                }

                return 0;
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(named, previousBackend);
                AssetDatabase.SaveAssets();
            }
        }

        private static string[] EnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var count = 0;
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                {
                    count++;
                }
            }

            var paths = new string[count];
            var i = 0;
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                {
                    paths[i++] = scene.path;
                }
            }

            return paths;
        }
    }
}
