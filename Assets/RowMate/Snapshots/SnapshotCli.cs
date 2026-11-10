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
    // EditorApplication.Exit (0 bien, 1 alguna toma falló, 3 venció el tope de 45 min). En batchmode sin -quit el Editor sigue
    // vivo y update corre (S-0). Casi todas las tomas necesitan ROWMATE_SIMULATOR (tools/unity/capture-snapshots.ps1 lo pone y
    // lo saca); sin el define solo hay CAP-22 y CAP-23, y lo avisa.
    public static class SnapshotCli
    {
        public const string It2Output = "docs/migration/iterations/02-dispositivo-simulador/snapshots";
        public const string It3Output = "docs/migration/iterations/03-rutinas/snapshots";
        public const string It4aOutput = "docs/migration/iterations/04a-motor-hud-funcional/snapshots";

        // It. 4b (04b-hud-exr/02-tech-design §5): el HUD EXR (HudExrCatalog), el conjunto por defecto desde la 4b.
        public const string It4bOutput = "docs/migration/iterations/04b-hud-exr/snapshots";

        // It. 5 (05-series-parciales/02-tech-design §5): los paneles de series (SeriesCatalog), el conjunto por defecto desde la 5.
        public const string It5Output = "docs/migration/iterations/05-series-parciales/snapshots";
        // It. 6a (06a-escena-lago/02-tech-design §5): la escena del lago (SceneCatalog), el conjunto por defecto desde la 6a.
        public const string It6aOutput = "docs/migration/iterations/06a-escena-lago/snapshots";
        // It. 7 (07-remero-palada/02-tech-design §5): it7 (por defecto desde la 7) = RowerCatalog.
        public const string It7Output = "docs/migration/iterations/07-remero-palada/snapshots";
        // It. 8 (08-entornos-selector/02-tech-design §6): it8 (por defecto desde la 8) = EnvironmentSnapshotCatalog; thumbs = el horneado de
        // las miniaturas (las escribe en Art/Textures/Thumbnails; las tomas, a Library/RowMate/thumbs, que no se versiona).
        public const string It8Output = "docs/migration/iterations/08-entornos-selector/snapshots";
        public const string ThumbsOutput = "Library/RowMate/thumbs";
        // It. 9 (09-historial/02-tech-design §7): it9 (por defecto desde la 9) = HistoryCatalog.
        public const string It9Output = "docs/migration/iterations/09-historial/snapshots";
        // It. 10a (10a-ble-android-windows/02-tech-design §8.4): it10a (por defecto desde la 10a) = BleCatalog, con el adaptador falso.
        public const string It10aOutput = "docs/migration/iterations/10a-ble-android-windows/snapshots";
        // It. 11 (11-ble-pulsometro/02-tech-design §7): it11 (por defecto desde la 11) = HrmCatalog, el pulsómetro real sobre el adaptador falso.
        public const string It11Output = "docs/migration/iterations/11-ble-pulsometro/snapshots";
        // It. 12 (12-exportar-tcx/02-tech-design §7): it12 (por defecto desde la 12) = ExportCatalog, "Exportar TCX" con el destino falso.
        public const string It12Output = "docs/migration/iterations/12-exportar-tcx/snapshots";
        // It. 14 (14-pulido-plataformas/02-tech-design §9): it14 (por defecto desde la 14) = PolishCatalog, el pulido, la identidad y la deuda.
        public const string It14Output = "docs/migration/iterations/14-pulido-plataformas/snapshots";
        public const string DefaultSet = "it14";
        public const string FilterPrefKey = "RowMate.Snapshots.Filter";

        public static string OutputFor(string set)
        {
            if (string.Equals(set, "it14", StringComparison.OrdinalIgnoreCase))
            {
                return It14Output;
            }

            if (string.Equals(set, "it12", StringComparison.OrdinalIgnoreCase))
            {
                return It12Output;
            }

            if (string.Equals(set, "it11", StringComparison.OrdinalIgnoreCase))
            {
                return It11Output;
            }

            if (string.Equals(set, "it10a", StringComparison.OrdinalIgnoreCase))
            {
                return It10aOutput;
            }

            if (string.Equals(set, "it2", StringComparison.OrdinalIgnoreCase))
            {
                return It2Output;
            }

            if (string.Equals(set, "it3", StringComparison.OrdinalIgnoreCase))
            {
                return It3Output;
            }

            if (string.Equals(set, "it4a", StringComparison.OrdinalIgnoreCase))
            {
                return It4aOutput;
            }

            if (string.Equals(set, "it4b", StringComparison.OrdinalIgnoreCase))
            {
                return It4bOutput;
            }

            if (string.Equals(set, "it6a", StringComparison.OrdinalIgnoreCase))
            {
                return It6aOutput;
            }

            if (string.Equals(set, "it8", StringComparison.OrdinalIgnoreCase))
            {
                return It8Output;
            }

            if (string.Equals(set, "thumbs", StringComparison.OrdinalIgnoreCase))
            {
                return ThumbsOutput;
            }

            if (string.Equals(set, "it5", StringComparison.OrdinalIgnoreCase))
            {
                return It5Output;
            }

            return string.Equals(set, "it7", StringComparison.OrdinalIgnoreCase) ? It7Output : It9Output;
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
            // It. 4b: -seed F4A (Prueba 4a, Corta, Distancia 100 y Sin meta) para probar a mano el build del simulador con una
            // rutina corta, sobre una carpeta de ROWMATE_DATA_DIR temporal (nunca la del usuario).
            // It. 5 (05-series-parciales/02-tech-design §3.7): -seed F5 (las rutinas de series) y -prefs laps,reps, que escribe prefs.json
            // con esas claves en verdadero AL LADO de la base pedida (la misma carpeta temporal de ROWMATE_DATA_DIR; nunca la del usuario):
            // la medida de la entrada con los dos paneles desde la cascada (AC-WKT-13-15).
            var prefs = Value(args, "-prefs");
            if (file == null || file.Length == 0 || (seedName != "Cap27" && seedName != "F4A" && seedName != "F4B" && seedName != "F5"))
            {
                Debug.LogError("[SNAPSHOT] SeedDatabase: falta -snapshotSeedDb <archivo> (y solo existen -seed Cap27, -seed F4A, -seed F4B y -seed F5)");
                EditorApplication.Exit(2);
                return;
            }

            try
            {
                if (seedName == "Cap27")
                {
                    RoutineSeeds.WriteFile(file, RoutineSeeds.Cap27);
                }
                else if (seedName == "F5")
                {
                    RoutineSeeds.WriteFile(file, RoutineSeeds.F5);
                }
                else if (seedName == "F4B")
                {
                    // It. 6a, ronda 1 (QA-V6a-13): Serie larga (y las demás de la 4b) para -rowmate-perf-long-series en el build.
                    RoutineSeeds.WriteFile(file, RoutineSeeds.F4B);
                }
                else
                {
                    RoutineSeeds.WriteFile(file, RoutineSeeds.F4A);
                }

                if (!string.IsNullOrWhiteSpace(prefs))
                {
                    WritePreferences(file, prefs!);
                }

                Debug.Log("[SNAPSHOT] base sintética lista (" + seedName + ")");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("[SNAPSHOT] SeedDatabase: " + error.GetType().Name + ": " + error.Message);
                EditorApplication.Exit(1);
            }
        }

        // prefs.json al lado de la base sembrada (la base ya pasó TempDatabase.Guard: es una carpeta de prueba). "laps,reps".
        private static void WritePreferences(string databaseFile, string keys)
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(databaseFile)) ?? throw new InvalidOperationException("sin carpeta");
            var parts = new List<string>();
            foreach (var raw in keys.Split(','))
            {
                var key = raw.Trim();
                if (key == "laps")
                {
                    parts.Add("\"" + RowMate.App.Workout.SeriesPanelPrefs.LapsKey + "\":true");
                }
                else if (key == "reps")
                {
                    parts.Add("\"" + RowMate.App.Workout.SeriesPanelPrefs.RepsKey + "\":true");
                }
            }

            File.WriteAllText(Path.Combine(folder, "prefs.json"), "{" + string.Join(",", parts) + "}");
            Debug.Log("[SNAPSHOT] preferencias sembradas (" + keys + ")");
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
            // It. 6a (§5.1; QA-G6aT-12): la compilación de shaders sincrónica durante TODA la corrida (el primer dibujo de cada toma sale
            // con los shaders de verdad y no con el de "compilando"). ShaderUtil.allowAsyncCompilation no se guarda en ProjectSettings
            // (EditorSettings.asyncShaderCompilation, que sí se guarda, no se toca); vuelve a su valor al salir.
            var asyncBefore = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            Action<int> done = code =>
            {
                ShaderUtil.allowAsyncCompilation = asyncBefore;
                exit(code);
            };
            new SnapshotRunner(scenarios, folder, pipeline, !string.IsNullOrWhiteSpace(filter), done, SnapshotCatalog.TitleFor(set)).Start();
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
