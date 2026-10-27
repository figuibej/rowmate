using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RowMate.EditorTools
{
    // Guardia de R-01 (ARCHITECTURE §4.2): ningún build de distribución lleva el simulador.
    // - Durante una corrida de tests del simulador (existe la marca de SimulatorDefine, QA-V-05) falla CUALQUIER build.
    // - Con ROWMATE_SIMULATOR en los defines de la plataforma, el build tiene que ser Development.
    // Los builds de simulador por línea de comandos pasan el define con extraScriptingDefines y fuerzan Development
    // (ARCHITECTURE §19, BuildCli); esta guardia mira los defines de PlayerSettings, que es lo que tocan el menú y los tests.
    // Evaluate es la misma regla sin efectos, para la prueba de AC-BLE-13-01 (SMT.NonDevelopmentBuildWithSimulatorFails).
    internal sealed class SimulatorBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        // null si el build puede seguir; si no, el motivo (el mismo texto que la excepción).
        public static string? Evaluate(bool testRunInProgress, bool simulatorEnabled, bool development, string targetName)
        {
            if (testRunInProgress)
            {
                return SimulatorDefine.LogPrefix + "hay una corrida de tests del simulador sin restaurar (" + SimulatorDefine.TestMarkerPath
                    + "). Corré tools/unity/run-unity-tests.ps1 -RestoreOnly antes de compilar.";
            }

            if (simulatorEnabled && !development)
            {
                return SimulatorDefine.LogPrefix + SimulatorDefine.Symbol + " está activo en " + targetName
                    + " y el build no es Development (R-01). Desactivalo con el menú " + SimulatorDefine.MenuPath
                    + " o compilá un build de desarrollo.";
            }

            return null;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (SimulatorDefine.TestRunInProgress)
            {
                throw new BuildFailedException(Evaluate(true, false, false, "")!);
            }

            var target = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
            var development = (report.summary.options & BuildOptions.Development) != 0;
            if (SimulatorDefine.IsEnabled(target) && !development)
            {
                throw new BuildFailedException(Evaluate(false, true, false, target.TargetName)!);
            }
        }
    }
}
