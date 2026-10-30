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
    // - Sin -rowmateSimulator: al terminar, BuildOutputInspector busca lo que un build normal no puede llevar (simulador,
    //   diagnósticos de desarrollo, metadatos de tests); si encuentra algo, falla.
    // - El backend (Mono temporal, D-16) se cambia solo para este build y se restaura en finally.
    // - Si falta el módulo de la plataforma o del backend (IL2CPP, QA-V2-03): NO VERIFICABLE, sale con 3 y lo dice, antes
    //   de compilar nada.
    // Códigos: 0 bien; 1 el build falló; 2 el build normal lleva algo prohibido; 3 falta un módulo.
    public static class BuildCli
    {
        public const string Prefix = "[BUILD] ";
        private const string SimulatorSymbol = "ROWMATE_SIMULATOR";

        // Lo que Unity escribe en el reporte cuando falta el módulo del backend (visto en la It. 2 con IL2CPP de Windows).
        private const string BackendNotInstalled = "is not installed";

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

        // QA-V2-03: el módulo IL2CPP de una plataforma deja sus variantes en <PlaybackEngines>/<plataforma>/Variations/*il2cpp*
        // (Windows: win64_player_development_il2cpp, …; Android: il2cpp). Sin ninguna, IL2CPP no está instalado.
        public static bool HasIl2CppVariation(string? playbackEngineDirectory)
        {
            if (string.IsNullOrEmpty(playbackEngineDirectory))
            {
                return false;
            }

            var variations = Path.Combine(playbackEngineDirectory, "Variations");
            return Directory.Exists(variations) && Directory.GetDirectories(variations, "*il2cpp*").Length > 0;
        }

        public static int Run(BuildRequest request)
        {
            if (!BuildPipeline.IsBuildTargetSupported(request.BuildTargetGroup, request.BuildTarget))
            {
                Debug.LogWarning(Prefix + "NO VERIFICABLE: falta el módulo de " + request.Target + " en este Editor.");
                return 3;
            }

            if (request.ScriptingBackend == ScriptingImplementation.IL2CPP
                && !HasIl2CppVariation(BuildPipeline.GetPlaybackEngineDirectory(request.BuildTarget, BuildOptions.None)))
            {
                Debug.LogWarning(Prefix + "NO VERIFICABLE: falta el módulo IL2CPP de " + request.Target + " en este Editor.");
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
            // It. 4b (AC-WKT-14-19; 04b-hud-exr/02-tech-design §3.13): FrameTimeProbe necesita las estadísticas de tiempo de cuadro;
            // se prenden SOLO en el build de desarrollo con simulador y se restauran al terminar (ProjectSettings no cambia).
            var previousFrameTiming = PlayerSettings.enableFrameTimingStats;
            try
            {
                PlayerSettings.SetScriptingBackend(named, request.ScriptingBackend);
                if (request.Simulator)
                {
                    PlayerSettings.enableFrameTimingStats = true;
                }

                Directory.CreateDirectory(request.OutputFolder);
                var options = OptionsFor(request, scenes);
                Debug.Log(Prefix + request.Target + " " + request.Variant + " " + request.ScriptingBackend + (request.Development ? " Development" : "")
                    + " → " + request.LocationPath);
                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError(Prefix + "el build falló: " + report.summary.result + " (" + report.summary.totalErrors + " errores)");
                    if (MentionsMissingBackend(report))
                    {
                        Debug.LogWarning(Prefix + "NO VERIFICABLE: falta el módulo de " + request.ScriptingBackend + " de " + request.Target + ".");
                        return 3;
                    }

                    return 1;
                }

                Debug.Log(Prefix + "listo: " + report.summary.outputPath + " (" + report.summary.totalSize + " bytes)");
                if (!request.Simulator)
                {
                    if (request.Development)
                    {
                        // Un build de desarrollo de la variante normal (-SmokeTest, -DbSmoke de la It. 3) lleva los diagnósticos a
                        // propósito y nunca se publica: el inspector es para el release.
                        Debug.Log(Prefix + "build de desarrollo: BuildOutputInspector no aplica (no es un release).");
                        return 0;
                    }

                    var traces = BuildOutputInspector.FindForbiddenTraces(request.OutputFolder);
                    if (traces.Count > 0)
                    {
                        Debug.LogError(Prefix + "el build normal lleva lo que no puede llevar (simulador AC-BLE-13-02, diagnósticos QA-V2-09 o tests QA-V2-02):\n"
                            + string.Join("\n", traces));
                        return 2;
                    }

                    Debug.Log(Prefix + "BuildOutputInspector: sin rastros del simulador, de diagnósticos ni de tests.");
                }

                return 0;
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(named, previousBackend);
                PlayerSettings.enableFrameTimingStats = previousFrameTiming;
                AssetDatabase.SaveAssets();
            }
        }

        // Respaldo de la comprobación previa: el reporte dice "Currently selected scripting backend (IL2CPP) is not installed."
        private static bool MentionsMissingBackend(BuildReport report)
        {
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.content != null && message.content.Contains("scripting backend") && message.content.Contains(BackendNotInstalled))
                    {
                        return true;
                    }
                }
            }

            return false;
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
