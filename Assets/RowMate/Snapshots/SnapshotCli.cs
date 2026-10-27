using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RowMate.Presentation.Surface;
using UnityEditor;
using UnityEngine;

namespace RowMate.Snapshots
{
    // Entrada del arnés de capturas (02-tech-design §4.1, §4.6):
    //   Unity.exe -batchmode -projectPath <repo> -executeMethod RowMate.Snapshots.SnapshotCli.Run -logFile <log>
    //             [-snapshotFilter "CAP-01,CAP-1*,VID-*"] [-snapshotOut <carpeta>] [-snapshotPipeline GammaRt|Overlay]
    // SIN -quit: Run() registra la máquina de estados en EditorApplication.update y vuelve; al terminar sale con
    // EditorApplication.Exit (0 bien, 1 alguna toma falló, 3 venció el tope de 15 min). En batchmode sin -quit el Editor sigue
    // vivo y update corre (S-0). Casi todas las tomas necesitan ROWMATE_SIMULATOR (tools/unity/capture-snapshots.ps1 lo pone y
    // lo saca); sin el define solo hay CAP-22 y CAP-23, y lo avisa.
    public static class SnapshotCli
    {
        public const string DefaultOutput = "docs/migration/iterations/02-dispositivo-simulador/snapshots";
        public const string FilterPrefKey = "RowMate.Snapshots.Filter";

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var filter = Value(args, "-snapshotFilter") ?? "";
            var output = Value(args, "-snapshotOut") ?? DefaultOutput;
            var pipeline = string.Equals(Value(args, "-snapshotPipeline"), "Overlay", StringComparison.OrdinalIgnoreCase)
                ? UiColorPipeline.Overlay
                : UiColorPipeline.GammaRt;
            Start(filter, output, pipeline, code => EditorApplication.Exit(code));
        }

        [MenuItem("RowMate/Capturas/Generar todas")]
        public static void RunFromMenu()
        {
            Start("", DefaultOutput, UiColorPipeline.GammaRt, code => Debug.Log("[SNAPSHOT] terminó con " + code));
        }

        // Filtro guardado en EditorPrefs (RowMate.Snapshots.Filter), p. ej. "CAP-1*,VID-01"; vacío = todas.
        [MenuItem("RowMate/Capturas/Generar seleccionadas…")]
        public static void RunFiltered()
        {
            var filter = EditorPrefs.GetString(FilterPrefKey, "CAP-01");
            Start(filter, DefaultOutput, UiColorPipeline.GammaRt, code => Debug.Log("[SNAPSHOT] terminó con " + code));
        }

        public static IReadOnlyList<SnapshotScenario> Select(string filter)
        {
            var all = SnapshotCatalog.All;
            if (string.IsNullOrWhiteSpace(filter))
            {
                return all;
            }

            var patterns = new List<Regex>();
            foreach (var part in filter.Split(','))
            {
                var trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    patterns.Add(new Regex("^" + Regex.Escape(trimmed).Replace("\\*", ".*") + "$", RegexOptions.IgnoreCase));
                }
            }

            var selected = new List<SnapshotScenario>();
            foreach (var scenario in all)
            {
                foreach (var pattern in patterns)
                {
                    if (pattern.IsMatch(scenario.Id) || pattern.IsMatch(scenario.FileStem))
                    {
                        selected.Add(scenario);
                        break;
                    }
                }
            }

            return selected;
        }

        private static void Start(string filter, string output, UiColorPipeline pipeline, Action<int> exit)
        {
#if !ROWMATE_SIMULATOR
            Debug.LogWarning("[SNAPSHOT] sin ROWMATE_SIMULATOR: solo CAP-22 y CAP-23 (usar tools/unity/capture-snapshots.ps1).");
#endif
            var scenarios = Select(filter);
            var folder = Path.GetFullPath(output);
            new SnapshotRunner(scenarios, folder, pipeline, !string.IsNullOrWhiteSpace(filter), exit).Start();
        }

        private static string? Value(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
