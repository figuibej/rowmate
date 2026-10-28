using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RowMate.EditorTools
{
    // QA-V2-02 (It. 2): com.unity.test-framework.performance (dependencia de com.unity.collections) escribe en su
    // IPreprocessBuildWithReport (callbackOrder 0, TestRunBuilder) Assets/Resources/PerformanceTestRun{Info,Settings}.json
    // (lista de paquetes, versión del Editor, ajustes del player) para los tests de rendimiento en un player, y solo los borra
    // en IPostprocessBuildWithReport, que no corre si el build falla: quedaban en el repo y entraban al build siguiente.
    // RowMate no corre tests de rendimiento en players: este paso corre después (callbackOrder 100), antes de que Unity junte
    // los activos, y borra los dos archivos (con su .meta) y Assets/Resources si quedó vacío. El .gitignore los ignora por si
    // un Editor los deja igual (un build cortado a mano).
    internal sealed class PerformanceTestRunCleaner : IPreprocessBuildWithReport
    {
        public const string ResourcesFolder = "Assets/Resources";

        public static readonly string[] Files =
        {
            ResourcesFolder + "/PerformanceTestRunInfo.json",
            ResourcesFolder + "/PerformanceTestRunSettings.json",
        };

        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            var removed = Clean();
            if (removed > 0)
            {
                Debug.Log(BuildCli.Prefix + "PerformanceTestRunCleaner: " + removed + " entradas de tests de rendimiento fuera del build (archivos, .meta y Assets/Resources vacía; QA-V2-02).");
            }
        }

        // Cantidad de entradas borradas (los .json, sus .meta si ya existían, la carpeta vacía y su .meta).
        public static int Clean()
        {
            var removed = 0;
            foreach (var file in Files)
            {
                removed += Delete(file);
                removed += Delete(file + ".meta");
            }

            if (Directory.Exists(ResourcesFolder) && Directory.GetFileSystemEntries(ResourcesFolder).Length == 0)
            {
                Directory.Delete(ResourcesFolder);
                removed++;
                removed += Delete(ResourcesFolder + ".meta");
            }

            if (removed > 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            return removed;
        }

        private static int Delete(string path)
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            File.Delete(path);
            return 1;
        }
    }
}
