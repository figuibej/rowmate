using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RowMate.EditorTools
{
    // El aviso de licencias de terceros viaja con el build (06a-escena-lago/02-tech-design §1.1 B2, §2.10; QA-G6aT-19): DartRandom es un
    // puerto del generador de la VM de Dart (BSD-3), cuya licencia exige reproducir el aviso en la distribución binaria. En cada build de
    // Windows copia THIRD_PARTY_NOTICES.md (la raíz del repo) junto al ejecutable; BuildCli falla si no quedó (BuildOutputInspector).
    // Android lo suma en la It. 10 (en los assets del APK).
    public sealed class NoticesPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var platform = report.summary.platform;
            if (platform != BuildTarget.StandaloneWindows64 && platform != BuildTarget.StandaloneWindows)
            {
                return;
            }

            var source = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, BuildOutputInspector.NoticesFile);
            var folder = Path.GetDirectoryName(report.summary.outputPath);
            if (!File.Exists(source) || string.IsNullOrEmpty(folder))
            {
                Debug.LogError("[BUILD] no se encontró " + BuildOutputInspector.NoticesFile + " en la raíz del repo: el build no lleva el aviso de Dart");
                return;
            }

            File.Copy(source, Path.Combine(folder!, BuildOutputInspector.NoticesFile), true);
        }
    }
}
