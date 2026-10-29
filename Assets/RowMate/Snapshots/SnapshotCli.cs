using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RowMate.Presentation.Surface;
using RowMate.Tests.Support;
using UnityEditor;
using UnityEngine;

namespace RowMate.Snapshots
{
    // Entrada del arnés de capturas (02-tech-design §4.1, §4.6):
    //   Unity.exe -batchmode -projectPath <repo> -executeMethod RowMate.Snapshots.SnapshotCli.Run -logFile <log>
    //             [-snapshotSet it4a|it3|it2] [-snapshotFilter "CAP-01,CAP-1*,VID-*"] [-snapshotOut <carpeta>]
    //             [-snapshotPipeline GammaRt|Overlay]
    // It. 3 (03-rutinas/02-tech-design §5.1): -snapshotSet elige el catálogo (it3 por defecto: Rutinas; it2: Dispositivo) y su
    // carpeta de salida. It. 4a (04a-motor-hud-funcional/02-tech-design §7.1): it4a (el entrenamiento, WorkoutCatalog) pasa a ser el
    // conjunto por defecto; it3 e it2 siguen igual. SeedDatabase (otro -executeMethod, con -quit) escribe la base sintética de CAP-27
    // (-snapshotSeedDb <carpeta>/cap27.db -seed Cap27) para tools/unity/capture-cap27.ps1.
    // SIN -quit: Run() registra la máquina de estados en EditorApplication.update y vuelve; al terminar sale con
    // EditorApplication.Exit (0 bien, 1 alguna toma falló, 3 venció el tope de 15 min). En batchmode sin -quit el Editor sigue
    // vivo y update corre (S-0). Casi todas las tomas necesitan ROWMATE_SIMULATOR (tools/unity/capture-snapshots.ps1 lo pone y
    // lo saca); sin el define solo hay CAP-22 y CAP-23, y lo avisa.
    public static class SnapshotCli
    {
        public const string It2Output = "docs/migration/iterations/02-dispositivo-simulador/snapshots";
        public const string It3Output = "docs/migration/iterations/03-rutinas/snapshots";
        public const string It4aOutput = "docs/migration/iterations/04a-motor-hud-funcional/snapshots";
        public const string DefaultSet = "it4a";
        public const string FilterPrefKey = "RowMate.Snapshots.Filter";

        public static string OutputFor(string set)
        {
            if (string.Equals(set, "it2", StringComparison.OrdinalIgnoreCase))
            {
                return It2Output;
            }

            return string.Equals(set, "it3", StringComparison.OrdinalIgnoreCase) ? It3Output : It4aOutput;
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            var set = Value(args, "-snapshotSet") ?? DefaultSet;
            var filter = Value(args, "-snapshotFilter") ?? "";
            var output = Value(args, "-snapshotOut") ?? OutputFor(set);
            var pipeline = string.Equals(Value(args, "-snapshotPipeline"), "Overlay", StringComparison.OrdinalIgnoreCase)
                ? UiColorPipeline.Overlay
                : UiColorPipeline.GammaRt;
            Start(set, filter, output, pipeline, code => EditorApplication.Exit(code));
        }

        [MenuItem("RowMate/Capturas/Generar todas")]
        public static void RunFromMenu()
        {
            Start(DefaultSet, "", OutputFor(DefaultSet), UiColorPipeline.GammaRt, code => Debug.Log("[SNAPSHOT] terminó con " + code));
        }

        // Filtro guardado en EditorPrefs (RowMate.Snapshots.Filter), p. ej. "CAP-1*,VID-01"; vacío = todas.
        [MenuItem("RowMate/Capturas/Generar seleccionadas…")]
        public static void RunFiltered()
        {
            var filter = EditorPrefs.GetString(FilterPrefKey, "CAP-01");
            Start(DefaultSet, filter, OutputFor(DefaultSet), UiColorPipeline.GammaRt, code => Debug.Log("[SNAPSHOT] terminó con " + code));
        }

        // La base sintética de CAP-27 (03-rutinas/02-tech-design §5.5): SOLO la semilla pedida (Cap27 = Escalera SPM de F-LISTA,
        // nunca datos del usuario), en el archivo pedido, que no puede estar en la carpeta de datos del usuario ni llamarse como
        // la base de la app fuera de la carpeta de pruebas (TempDatabase.Guard, dentro de RoutineSeeds.WriteFile): el script la
        // pide como cap27.db (QA-V3-01). Sale con 0 si la escribió, 1 si no pudo y 2 si faltan los argumentos.
        public static void SeedDatabase()
        {
            var args = Environment.GetCommandLineArgs();
            var file = Value(args, "-snapshotSeedDb");
            var seedName = Value(args, "-seed") ?? "Cap27";
            if (file == null || file.Length == 0 || seedName != "Cap27")
            {
                Debug.LogError("[SNAPSHOT] SeedDatabase: falta -snapshotSeedDb <archivo> (y solo existe -seed Cap27)");
                EditorApplication.Exit(2);
                return;
            }

            try
            {
                RoutineSeeds.WriteFile(file, RoutineSeeds.Cap27);
                Debug.Log("[SNAPSHOT] base sintética de CAP-27 lista (Escalera SPM)");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("[SNAPSHOT] SeedDatabase: " + error.GetType().Name + ": " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        public static IReadOnlyList<SnapshotScenario> Select(string filter)
        {
            return Select(DefaultSet, filter);
        }

        public static IReadOnlyList<SnapshotScenario> Select(string set, string filter)
        {
            var all = SnapshotCatalog.For(set);
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

        private static void Start(string set, string filter, string output, UiColorPipeline pipeline, Action<int> exit)
        {
#if !ROWMATE_SIMULATOR
            Debug.LogWarning("[SNAPSHOT] sin ROWMATE_SIMULATOR: el catálogo queda casi vacío (usar tools/unity/capture-snapshots.ps1).");
#endif
            var scenarios = Select(set, filter);
            var folder = Path.GetFullPath(output);
            new SnapshotRunner(scenarios, folder, pipeline, !string.IsNullOrWhiteSpace(filter), exit, SnapshotCatalog.TitleFor(set)).Start();
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
