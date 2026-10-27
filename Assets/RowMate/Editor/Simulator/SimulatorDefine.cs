using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RowMate.EditorTools
{
    // ROWMATE_SIMULATOR en el Editor (ARCHITECTURE §4.2, R-01) y tests del simulador dentro de Unity (QA-V-05 de la It. 1).
    // Sin el define, Unity no compila RowMate.Simulator ni RowMate.Tests.Simulator (defineConstraints): los 69 tests del
    // simulador no existen en el Test Runner. Esta clase es el ÚNICO lugar del proyecto que llama a
    // SetScriptingDefineSymbols (lo controla SimulatorInUnityTests en dotnet). Toca Standalone, Android e iOS.
    // - Menú RowMate/Modo simulador: lo agrega o lo saca (con tilde cuando está activo). Deshabilitado durante una
    //   corrida de tests del simulador.
    // - EnableForTests / RestoreAfterTests: para -executeMethod (tools/unity/run-unity-tests.ps1 -Simulator).
    //   EnableForTests guarda en Library/RowMate/simulator-tests.txt (Library/ no se versiona) en qué plataformas ya
    //   estaba el define y lo agrega en las tres; RestoreAfterTests deja cada plataforma como estaba y borra la marca.
    //   Si una corrida se corta, la marca queda y la siguiente EnableForTests conserva el estado original guardado.
    // - Mientras exista la marca, SimulatorBuildGuard hace fallar cualquier build (el define nunca llega a un release).
    public static class SimulatorDefine
    {
        public const string Symbol = "ROWMATE_SIMULATOR";
        public const string MenuPath = "RowMate/Modo simulador";
        public const string TestMarkerPath = "Library/RowMate/simulator-tests.txt";
        public const string LogPrefix = "[SIMULATOR] ";

        private static readonly NamedBuildTarget[] Targets =
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS
        };

        // Hay una corrida de tests del simulador sin restaurar.
        public static bool TestRunInProgress => File.Exists(TestMarkerPath);

        public static bool IsEnabled(NamedBuildTarget target)
        {
            var parts = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].Trim() == Symbol)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEnabledAnywhere()
        {
            for (var i = 0; i < Targets.Length; i++)
            {
                if (IsEnabled(Targets[i]))
                {
                    return true;
                }
            }

            return false;
        }

        // -executeMethod RowMate.EditorTools.SimulatorDefine.EnableForTests
        // Una excepción acá hace que Unity salga con código 1 (el script no corre los tests).
        public static void EnableForTests()
        {
            if (!TestRunInProgress)
            {
                var lines = new List<string> { "# RowMate (QA-V-05): plataformas que ya tenian " + Symbol + " antes de la corrida de tests" };
                for (var i = 0; i < Targets.Length; i++)
                {
                    if (IsEnabled(Targets[i]))
                    {
                        lines.Add(Targets[i].TargetName);
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(TestMarkerPath)!);
                File.WriteAllLines(TestMarkerPath, lines.ToArray());
            }
            else
            {
                Debug.LogWarning(LogPrefix + "ya había una corrida sin restaurar (" + TestMarkerPath + "): se conserva el estado original que guardó.");
            }

            SetEverywhere(true);
            AssetDatabase.SaveAssets();
            Debug.Log(LogPrefix + Symbol + " activado para los tests en Standalone, Android e iOS. Restaurar con RestoreAfterTests.");
        }

        // -executeMethod RowMate.EditorTools.SimulatorDefine.RestoreAfterTests
        public static void RestoreAfterTests()
        {
            if (!TestRunInProgress)
            {
                Debug.Log(LogPrefix + "no hay corrida de tests del simulador para restaurar (" + TestMarkerPath + " no existe).");
                return;
            }

            var before = new HashSet<string>(StringComparer.Ordinal);
            foreach (var raw in File.ReadAllLines(TestMarkerPath))
            {
                var line = raw.Trim();
                if (line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                {
                    before.Add(line);
                }
            }

            for (var i = 0; i < Targets.Length; i++)
            {
                Set(Targets[i], before.Contains(Targets[i].TargetName));
            }

            AssetDatabase.SaveAssets();
            File.Delete(TestMarkerPath);
            Debug.Log(LogPrefix + "defines restaurados; " + Symbol + " queda en: "
                + (before.Count == 0 ? "ninguna plataforma" : string.Join(", ", before)) + ".");
        }

        [MenuItem(MenuPath, false, 100)]
        private static void ToggleFromMenu()
        {
            var enable = !IsEnabledAnywhere();
            SetEverywhere(enable);
            AssetDatabase.SaveAssets();
            Debug.Log(LogPrefix + Symbol + (enable
                ? " activado en Standalone, Android e iOS. Ningún build de distribución puede llevarlo (R-01)."
                : " desactivado en Standalone, Android e iOS."));
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleFromMenuValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabledAnywhere());
            return !TestRunInProgress;
        }

        private static void SetEverywhere(bool enabled)
        {
            for (var i = 0; i < Targets.Length; i++)
            {
                Set(Targets[i], enabled);
            }
        }

        // Agrega o saca el define en una plataforma, sin tocar el resto de los símbolos ni su orden.
        private static void Set(NamedBuildTarget target, bool enabled)
        {
            var kept = new List<string>();
            var had = false;
            var parts = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');
            for (var i = 0; i < parts.Length; i++)
            {
                var symbol = parts[i].Trim();
                if (symbol.Length == 0)
                {
                    continue;
                }

                if (symbol == Symbol)
                {
                    had = true;
                    continue;
                }

                kept.Add(symbol);
            }

            if (had == enabled)
            {
                return;
            }

            if (enabled)
            {
                kept.Add(Symbol);
            }

            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", kept));
        }
    }
}
