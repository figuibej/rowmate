using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Routines;

namespace RowMate.Tests.Support
{
    // Una base sembrada (spec §0.4; 02-tech-design §5.3): las rutinas que se guardan por el repositorio de la app, de la más
    // vieja a la más nueva, y los cambios a mano en SQL que la app no deja hacer (F-RARA que no entran en el modelo: un tipo
    // `sprint`, un entero de más de 32 bits). Las F-RARA que el modelo sí puede llevar (50 000 W, ×2 000 000 000, ×0, ×−3,
    // una progresión de un paso suelto) se guardan tal cual: el repositorio no controla topes; el editor sí (AC-RUT-11-05).
    public sealed class RoutineSeed
    {
        internal RoutineSeed(string name, IReadOnlyList<Routine> routines, IReadOnlyList<KeyValuePair<int, string>> rawSteps)
        {
            Name = name;
            Routines = routines;
            RawSteps = rawSteps;
        }

        public string Name { get; }

        // En el orden en que se guardan (la más vieja primero).
        public IReadOnlyList<Routine> Routines { get; }

        // (índice de la rutina en Routines, UPDATE de sus pasos con un ? para routine_id), solo comillas simples (SQLITE_DQS 0).
        internal IReadOnlyList<KeyValuePair<int, string>> RawSteps { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    public static class RoutineSeeds
    {
        private const StepType W = StepType.Warmup;
        private const StepType K = StepType.Work;
        private const StepType R = StepType.Rest;
        private const StepType C = StepType.Cooldown;

        private static readonly IReadOnlyList<KeyValuePair<int, string>> NoRaw = new KeyValuePair<int, string>[0];

        // F-VACIA: base nueva, sin rutinas.
        public static RoutineSeed Empty => new RoutineSeed("F-VACIA", new Routine[0], NoRaw);

        // F-LISTA: las 4 rutinas, de la más vieja a la más nueva (2026-10-20 a -23).
        public static RoutineSeed Lista => new RoutineSeed("F-LISTA", ListaRoutines(), NoRaw);

        // F-BORDE: Casos borde con los 11 pasos de AC-RUT-12-06.
        public static RoutineSeed Borde => new RoutineSeed("F-BORDE", new[] { CasosBorde() }, NoRaw);

        // F-RARA: un paso suelto con vatios mín. 50 000.
        public static RoutineSeed RaraWatts => Single("F-RARA-W", Make("Vatios imposibles", Day(24), Step(K, dur: 60, wMin: 50000)));

        // F-RARA: una serie ×2 000 000 000 (trabajo 60 s + descanso 30 s).
        public static RoutineSeed RaraHuge => Single("F-RARA-HUGE",
            Make("Serie enorme", Day(24), Step(K, dur: 60, g: SeriesId(Day(24), 0), rep: 2000000000), Step(R, dur: 30, g: SeriesId(Day(24), 0), rep: 2000000000)));

        // F-RARA: solo una serie ×0: expandida no tiene pasos (pista vacía en la mini-barra, ×0 en el chip).
        public static RoutineSeed RaraZero => Single("F-RARA-ZERO",
            Make("Serie vacía", Day(24), Step(K, dur: 60, g: SeriesId(Day(24), 0), rep: 0), Step(R, dur: 30, g: SeriesId(Day(24), 0), rep: 0)));

        // F-RARA: una serie ×−3 (DT-13: se guarda tal cual).
        public static RoutineSeed RaraNegativeRepeat => Single("F-RARA-NEG",
            Make("Serie negativa", Day(24), Step(K, dur: 60, g: SeriesId(Day(24), 0), rep: -3)));

        // F-RARA: un paso SUELTO con progresión SPM 5 000 (no se ve ni se controla: QA-G3-13 c).
        public static RoutineSeed RaraLooseProgressions => Single("F-RARA-PROG",
            Make("Progresión suelta", Day(24), Step(K, dur: 300, spm: 18, pSpm: 5000)));

        // F-RARA: un paso con tipo `sprint` (AC-RUT-08-08, AC-RUT-01-05): se guarda como trabajo y se cambia en SQL.
        public static RoutineSeed RaraSprint => new RoutineSeed("F-RARA-SPRINT",
            new[] { Make("Sprint", Day(24), Step(W, dur: 300), Step(K, dur: 30)) },
            new[] { new KeyValuePair<int, string>(0, "UPDATE interval_steps SET type = 'sprint' WHERE routine_id = ? AND step_order = 1") });

        // F-RARA: target_watts_min = 3 000 000 000, que no entra en el int del modelo (DT-10): se cambia en SQL.
        public static RoutineSeed RaraInt32 => new RoutineSeed("F-RARA-INT32",
            new[] { Make("Vatios de 64 bits", Day(24), Step(K, dur: 60, wMin: 150)) },
            new[] { new KeyValuePair<int, string>(0, "UPDATE interval_steps SET target_watts_min = 3000000000 WHERE routine_id = ?") });

        // CAP-13: Pirámide 500 con una serie ×2 [trabajo 60 s] justo después de la ×4 (dos series contiguas).
        public static RoutineSeed PiramideContiguous => Single("CAP-13", PiramideQuinientos(withSecondSeries: true));

        // CAP-20b: una serie ×10 con trabajo SPM 18.
        public static RoutineSeed SeriesTimesTen => Single("CAP-20b",
            Make("Diez repeticiones", Day(24), Step(K, dur: 300, spm: 18, g: SeriesId(Day(24), 0), rep: 10)));

        // F-LISTA + 50 rutinas de 20 pasos (AC-HIS-17-06, R3-09).
        public static RoutineSeed Performance
        {
            get
            {
                var routines = new List<Routine>(ListaRoutines());
                for (var n = 1; n <= 50; n++)
                {
                    var steps = new List<IntervalStep>(20);
                    for (var i = 0; i < 20; i++)
                    {
                        steps.Add(Step(i % 2 == 0 ? K : R, dur: 30 + i, spm: i % 2 == 0 ? 20 + i : (int?)null));
                    }

                    routines.Add(Make("Rendimiento " + Fmt.Integer(n), Day(24).AddMinutes(n), steps.ToArray()));
                }

                return new RoutineSeed("PERFORMANCE", routines, NoRaw);
            }
        }

        // F-LISTA + Sesenta pasos (60 pasos sueltos; NAV.TabScrollsTheFocusedIntoView, R3-09).
        public static RoutineSeed Sixty
        {
            get
            {
                var routines = new List<Routine>(ListaRoutines());
                var steps = new IntervalStep[60];
                for (var i = 0; i < steps.Length; i++)
                {
                    steps[i] = Step(i % 3 == 0 ? K : i % 3 == 1 ? R : W, dur: 60);
                }

                routines.Add(Make("Sesenta pasos", Day(24), steps));
                return new RoutineSeed("SIXTY", routines, NoRaw);
            }
        }

        // CAP-27: la rutina SINTÉTICA de la ventana del build normal: Escalera SPM de F-LISTA, sola (nunca datos del usuario).
        public static RoutineSeed Cap27 => Single("CAP-27", EscaleraSpm());

        // F-4A (04a-motor-hud-funcional/01-po-spec §0.4; 02-tech-design §7.2 de la It. 4a), de la más nueva a la más vieja:
        // Prueba 4a, Corta, Distancia 100 y Sin meta (se guardan de la más vieja a la más nueva).
        public static RoutineSeed F4A => new RoutineSeed("F-4A", new[] { SinMeta(), DistanciaCien(), Corta(), PruebaCuatroA() }, NoRaw);

        // Las F-RARA con los nombres de la spec de la 4a (§0.4), cada una en su base: "Serie enorme" (una serie ×2 000 000 000
        // de trabajo 60 s), "Vatios imposibles" (un trabajo de 60 s con vatios mín. 50 000), "Serie ×0" (solo una serie ×0 de
        // trabajo 60 s) y "Sprint" (un paso de tipo sprint, cambiado en SQL: la carga falla, AC-WKT-12-06).
        public static RoutineSeed Rara4a(string name)
        {
            switch (name)
            {
                case "Serie enorme":
                    return Single("F-RARA-4A-HUGE", Make("Serie enorme", Day(24), Step(K, dur: 60, g: SeriesId(Day(24), 0), rep: 2000000000)));
                case "Vatios imposibles":
                    return Single("F-RARA-4A-W", Make("Vatios imposibles", Day(24), Step(K, dur: 60, wMin: 50000)));
                case "Serie ×0":
                    return Single("F-RARA-4A-ZERO", Make("Serie ×0", Day(24), Step(K, dur: 60, g: SeriesId(Day(24), 0), rep: 0)));
                case "Sprint":
                    return new RoutineSeed("F-RARA-4A-SPRINT", new[] { Make("Sprint", Day(24), Step(K, dur: 30)) },
                        new[] { new KeyValuePair<int, string>(0, "UPDATE interval_steps SET type = 'sprint' WHERE routine_id = ?") });
                default:
                    throw new ArgumentException("F-RARA de la 4a desconocida: " + name, nameof(name));
            }
        }

        // F-LISTA + Rutina 01…Rutina nn (CAP-06: 26), más nuevas que las de F-LISTA.
        public static RoutineSeed ListaPlus(int extra)
        {
            if (extra < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(extra));
            }

            var routines = new List<Routine>(ListaRoutines());
            for (var n = 1; n <= extra; n++)
            {
                var number = n < 10 ? "0" + Fmt.Integer(n) : Fmt.Integer(n);
                routines.Add(Make("Rutina " + number, Day(24).AddMinutes(n), Step(K, dur: 60), Step(R, dur: 60)));
            }

            return new RoutineSeed("F-LISTA+" + Fmt.Integer(extra), routines, NoRaw);
        }

        // ── Las rutinas de F-LISTA y F-BORDE ──

        // 2026-10-20: siete pasos sueltos, trabajo y descanso de 60 s alternados; sin descripción.
        public static Routine IntervalosCuatro()
        {
            return Make("Intervalos 4×", Day(20), Step(K, dur: 60), Step(R, dur: 60), Step(K, dur: 60), Step(R, dur: 60), Step(K, dur: 60),
                Step(R, dur: 60), Step(K, dur: 60));
        }

        // 2026-10-21: calentamiento 45 s.
        public static Routine SoloCalentamiento()
        {
            return Make("Solo calentamiento", Day(21), Step(W, dur: 45));
        }

        // 2026-10-22: calentamiento 600 s + serie ×4 [trabajo 500 m, 150-180 W, split 125, W +10 y split −2; trabajo 250 m,
        // SPM 30; descanso 90 s] + enfriamiento 300 s, con la descripción.
        public static Routine PiramideQuinientos(bool withSecondSeries = false)
        {
            var g = SeriesId(Day(22), 0);
            var steps = new List<IntervalStep>
            {
                Step(W, dur: 600),
                Step(K, dist: 500, wMin: 150, wMax: 180, split: 125, g: g, rep: 4, pW: 10, pSplit: -2),
                Step(K, dist: 250, spm: 30, g: g, rep: 4),
                Step(R, dur: 90, g: g, rep: 4),
            };
            if (withSecondSeries)
            {
                steps.Add(Step(K, dur: 60, g: SeriesId(Day(22), 1), rep: 2));
            }

            steps.Add(Step(C, dur: 300));
            return Make("Pirámide 500", Day(22), steps.ToArray()) with { Description = "Bajar 2 s el split en cada vuelta" };
        }

        // 2026-10-23: calentamiento 300 s + serie ×3 [trabajo 300 s, SPM 18, +2/rep; descanso 120 s] + enfriamiento 300 s.
        public static Routine EscaleraSpm()
        {
            var g = SeriesId(Day(23), 0);
            return Make("Escalera SPM", Day(23), Step(W, dur: 300), Step(K, dur: 300, spm: 18, g: g, rep: 3, pSpm: 2), Step(R, dur: 120, g: g, rep: 3),
                Step(C, dur: 300));
        }

        // ── Las rutinas de F-4A (04a §0.4) ──

        // 2026-10-27: calentamiento 30 s; serie ×2 [trabajo 60 s, vatios mín. 160, SPM 22, progresión SPM +2; descanso 30 s];
        // enfriamiento 30 s. Expandida: 6 pasos, 240 s; resumen "1 serie · 1 descanso". La de la demo (b).
        public static Routine PruebaCuatroA()
        {
            var g = SeriesId(Day(27), 0);
            return Make("Prueba 4a", Day(27), Step(W, dur: 30), Step(K, dur: 60, wMin: 160, spm: 22, g: g, rep: 2, pSpm: 2),
                Step(R, dur: 30, g: g, rep: 2), Step(C, dur: 30));
        }

        // 2026-10-26: serie ×2 [trabajo 10 s, SPM 18, progresión SPM +2; descanso 5 s]: trabajo 10 (18), descanso 5, trabajo
        // 10 (20), descanso 5 = 30 s.
        public static Routine Corta()
        {
            var g = SeriesId(Day(26), 0);
            return Make("Corta", Day(26), Step(K, dur: 10, spm: 18, g: g, rep: 2, pSpm: 2), Step(R, dur: 5, g: g, rep: 2));
        }

        // 2026-10-25: trabajo 100 m; descanso 5 s (sueltos).
        public static Routine DistanciaCien()
        {
            return Make("Distancia 100", Day(25), Step(K, dist: 100), Step(R, dur: 5));
        }

        // 2026-10-24 12:00: un trabajo sin meta (ni duración ni distancia: la cuenta muestra ?) y descanso 5 s.
        public static Routine SinMeta()
        {
            return Make("Sin meta", Day(24).AddHours(-1), Step(K), Step(R, dur: 5));
        }

        // F-BORDE (AC-RUT-12-06): los 11 pasos, en el orden de la tabla.
        public static Routine CasosBorde()
        {
            var once = SeriesId(Day(25), 0);
            var twice = SeriesId(Day(25), 1);
            return Make("Casos borde", Day(25),
                Step(K, dur: 0),
                Step(R),
                Step(K, dur: 90, wMin: 150),
                Step(K, dur: 3600, wMax: 180),
                Step(C, dur: 3725),
                Step(K, dist: 2000, spm: 0),
                Step(W, dur: -90),
                Step(K, dur: 60, split: 59, g: once),
                Step(K, dur: 120, wMin: 120, wMax: 180, spm: 22, split: 125),
                Step(K, dur: 300, spm: 20, g: twice, rep: 2, pSpm: -1),
                Step(R, dur: 60, g: twice, rep: 2, pSplit: 3));
        }

        // Guarda la semilla por el repositorio de la app y aplica sus cambios en SQL. Sincrónico: con el ejecutor en línea las
        // tareas ya terminaron; con el SerialExecutor espera al hilo de la base (que no necesita el hilo que llama).
        public static IReadOnlyList<Routine> Apply(SqliteRoutineRepository repository, RoutineSeed seed)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            var saved = new Routine[seed.Routines.Count];
            for (var i = 0; i < saved.Length; i++)
            {
                saved[i] = repository.InsertAsync(seed.Routines[i]).GetAwaiter().GetResult();
            }

            foreach (var raw in seed.RawSteps)
            {
                repository.Database.ExecuteRawForTestsAsync(raw.Value, saved[raw.Key].Id!.Value).GetAwaiter().GetResult();
            }

            return saved;
        }

        // Una base NUEVA con la semilla, en el archivo pedido (si ya existe, lo reemplaza con su -wal y su -shm). Es el paso 1 de
        // tools/unity/capture-cap27.ps1: lo llama SnapshotCli.SeedDatabase y lo corre de verdad ITT.Cap27SeedStepRunsForReal.
        // Pasa por TempDatabase.Guard: nunca en la carpeta de datos de la app ni con el nombre de su base fuera de la carpeta de
        // pruebas; por eso el script siembra cap27.db y recién después la copia como rower_app.db (QA-V3-01). Al cerrar, SQLite
        // pasa el WAL al archivo: el .db solo alcanza.
        public static void WriteFile(string path, RoutineSeed seed)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            var full = Path.GetFullPath(path);
            TempDatabase.Guard(full);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                if (File.Exists(full + suffix))
                {
                    File.Delete(full + suffix);
                }
            }

            var logger = new RecordingLogger();
            using (var database = new RowMateDatabase(full, new InlineDbExecutor(), logger))
            using (var repository = new SqliteRoutineRepository(database, logger))
            {
                Apply(repository, seed);
            }
        }

        // F-ROTA: un archivo con el nombre de la base que no es una base de SQLite (texto cualquiera).
        public static void WriteRota(string path)
        {
            TempDatabase.Guard(path);
            File.WriteAllText(path, "esto no es una base de SQLite\n", new UTF8Encoding(false));
        }

        // "g{milisegundos}_{n}", el formato del legacy (routine_editor_screen.dart:111).
        public static string SeriesId(DateTimeOffset at, int counter)
        {
            return "g" + Fmt.Integer(at.ToUnixTimeMilliseconds()) + "_" + Fmt.Integer(counter);
        }

        // 2026-10-<day> 13:00 UTC.
        public static DateTimeOffset Day(int day)
        {
            return new DateTimeOffset(2026, 10, day, 13, 0, 0, TimeSpan.Zero);
        }

        // Un paso de plantilla (routineId 0; el order lo pone el repositorio al guardar).
        public static IntervalStep Step(StepType type, int? dur = null, int? dist = null, int? wMin = null, int? wMax = null, int? spm = null,
            int? split = null, string? g = null, int? rep = null, int? pSpm = null, int? pW = null, int? pSplit = null)
        {
            return new IntervalStep(0, 0, type)
            {
                DurationSeconds = dur,
                DistanceMeters = dist,
                TargetWattsMin = wMin,
                TargetWattsMax = wMax,
                TargetSpm = spm,
                TargetSplitSeconds = split,
                GroupId = g,
                GroupRepeatCount = rep,
                ProgressionSpm = pSpm,
                ProgressionWatts = pW,
                ProgressionSplitSeconds = pSplit,
            };
        }

        public static Routine Make(string name, DateTimeOffset createdAt, params IntervalStep[] steps)
        {
            var numbered = new IntervalStep[steps.Length];
            for (var i = 0; i < steps.Length; i++)
            {
                numbered[i] = steps[i] with { Order = i };
            }

            return new Routine(name, createdAt) { Steps = numbered };
        }

        private static IReadOnlyList<Routine> ListaRoutines()
        {
            return new[] { IntervalosCuatro(), SoloCalentamiento(), PiramideQuinientos(), EscaleraSpm() };
        }

        private static RoutineSeed Single(string name, Routine routine)
        {
            return new RoutineSeed(name, new[] { routine }, NoRaw);
        }
    }
}
