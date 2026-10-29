using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // WGD: el motor nuevo contra el motor del legacy (02-tech-design §3.7 de la It. 4a; QA-P-23). Los vectores los genera
    // tools/oracle/run-oracle.ps1 -Engine corriendo el WorkoutProvider del legacy con los mismos dobles y las mismas lecturas
    // (Tests/Golden/oracle/workout_engine.json); esta prueba no necesita Flutter. Cada escenario se maneja igual que en el
    // oráculo: L(0) en el instante −0,5, el inicio en 0, pasos de 100 ms, la pausa, la reanudación y Terminar en su
    // milisegundo, y el corte cuando la sesión deja de estar en marcha. Se comparan, uno por uno, los puntos, la fila de
    // cierre, los resultados de objetivos, los cambios de paso y el reloj. La única diferencia permitida es Q-55 (DV-26): en
    // Distancia 100 con R-4R, R-4Z y la pausa el paso por distancia termina donde dice la columna nueva de la spec.
    [TestFixture]
    public sealed class WorkoutGoldenTests
    {
        private const string FileName = "workout_engine.json";

        // Q-55: los escenarios en los que el saneamiento de la distancia cambia a propósito el fin del paso.
        private static readonly HashSet<string> SanitizerChanges = new HashSet<string>(StringComparer.Ordinal)
        {
            "dist100_r4r", "dist100_r4z", "dist100_r4_pause",
        };

        [Test, Category("AC-WKT-06-07")]
        public void VectorsComeFromTheLegacyEngine()
        {
            var file = GoldenVectors.Load(FileName);
            Assert.That(file.Header.Generator, Is.EqualTo("tools/oracle/engine/test/engine_vectors_test.dart"));
            Assert.That(File.Exists(RepoPaths.Combine(file.Header.Generator)), Is.True, "el generador está en el repo");
            Assert.That(file.Header.LegacyCommit, Is.EqualTo(GoldenVectors.Load("session.json").Header.LegacyCommit), "el mismo legacy que el resto");
            var names = new List<string>();
            foreach (var scenario in Scenarios())
            {
                names.Add(scenario["name"].AsString());
                Assert.That(scenario["phase"].AsString(), Is.EqualTo("finished"), scenario["name"].AsString());
                Assert.That(scenario["lastFinishedSessionId"].AsLong(), Is.EqualTo(1L));
            }

            Assert.That(names, Is.EqualTo(new[]
            {
                "free_r4_finish_12_3", "corta_r4", "corta_r4_19", "prueba4a_r4", "dist100_r4", "dist100_r4r", "dist100_r4z", "dist100_r4_pause",
                "free_r4_pause_q58", "free_r4_pause_sampling", "free_r4_short",
            }));
        }

        [Test, Category("AC-WKT-06-07")]
        public void SessionsAreIdenticalToTheLegacy()
        {
            // Los puntos (cantidad, orden y valores, el SPM por sus bits), los totales del cierre, los objetivos, los cambios
            // de paso y el reloj, iguales al legacy en los 8 escenarios sin Q-55.
            var problems = new List<string>();
            var compared = 0;
            foreach (var scenario in Scenarios())
            {
                var name = scenario["name"].AsString();
                if (SanitizerChanges.Contains(name))
                {
                    continue;
                }

                compared++;
                var run = Run(scenario);
                Compare(name, scenario, run, problems);
            }

            Assert.That(compared, Is.EqualTo(8));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-WKT-06-03"), Category("AC-WKT-06-07")]
        public void Corta()
        {
            // AC-WKT-06-03: 7 puntos (5, 10, 15, 20, 25, 30 y el final) con step_index 0, 0, 1, 2, 2, 3, 3; los dos últimos
            // iguales (el cierre cae en un segundo múltiplo de 5). Lo mismo en el legacy y en el motor nuevo.
            var scenario = Scenario("corta_r4");
            var legacy = scenario["points"].Items;
            Assert.That(legacy.Count, Is.EqualTo(7));
            var indices = new List<long>();
            var types = new List<string>();
            foreach (var point in legacy)
            {
                indices.Add(point["step_index"].AsLong());
                types.Add(point["step_type"].AsString());
            }

            Assert.That(indices, Is.EqualTo(new long[] { 0, 0, 1, 2, 2, 3, 3 }));
            Assert.That(types, Is.EqualTo(new[] { "work", "work", "rest", "work", "work", "rest", "rest" }));
            Assert.That(Describe(legacy[5]), Is.EqualTo(Describe(legacy[6])), "legacy: los dos últimos iguales");
            var run = Run(scenario);
            Assert.That(run.Points.Count, Is.EqualTo(7));
            for (var i = 0; i < 7; i++)
            {
                Assert.That(Describe(run.Points[i]), Is.EqualTo(Describe(legacy[i])), "punto " + i);
            }

            Assert.That(run.Targets, Is.EqualTo(new[] { "0:met", "2:met" }));
        }

        [Test, Category("AC-WKT-06-07"), Category("AC-WKT-03-03")]
        public void SanitizerScenariosMatchUntilTheFirstStepChange()
        {
            // QA-V4a-L02 (05-qa-lens-paridad §2): SessionsAreIdenticalToTheLegacy no compara los 3 escenarios de Q-55, porque el
            // paso por distancia termina en otro segundo a propósito (DV-26). Hasta el primer cambio de paso, en cambio, los dos
            // motores tienen que dar los mismos puntos: los del paso 0 que tienen los dos, uno por uno y con todos sus campos.
            var compared = 0;
            foreach (var name in new[] { "dist100_r4r", "dist100_r4z", "dist100_r4_pause" })
            {
                Assert.That(SanitizerChanges.Contains(name), Is.True, name);
                var scenario = Scenario(name);
                var legacy = new List<string>();
                foreach (var point in scenario["points"].Items)
                {
                    if (!point["step_index"].IsNull && point["step_index"].AsLong() == 0)
                    {
                        legacy.Add(Describe(point));
                    }
                }

                var mine = new List<string>();
                foreach (var point in Run(scenario).Points)
                {
                    if (point.StepIndex == 0)
                    {
                        mine.Add(Describe(point));
                    }
                }

                var count = Math.Min(legacy.Count, mine.Count);
                for (var i = 0; i < count; i++)
                {
                    Assert.That(mine[i], Is.EqualTo(legacy[i]), name + ": punto " + i + " del paso 0");
                }

                compared += count;
            }

            Assert.That(compared, Is.EqualTo(5 + 5 + 2), "los segundos 5 a 25 con R-4R y con R-4Z; 5 y 10 con la pausa");
        }

        [Test, Category("AC-WKT-03-03")]
        public void LegacyDistanceColumn()
        {
            // Q-55 (DV-26): el oráculo da la columna "Legacy" de la spec (el paso de 100 m termina en los segundos activos 25,
            // 285, 25 y 11 con R-4, R-4R, R-4Z y la pausa de 20 s a los 40 m) y el motor nuevo la columna nueva (25, 26, 27 y 25).
            var expectedLegacy = new Dictionary<string, long> { ["dist100_r4"] = 25, ["dist100_r4r"] = 285, ["dist100_r4z"] = 25, ["dist100_r4_pause"] = 11 };
            var expectedNew = new Dictionary<string, long> { ["dist100_r4"] = 25, ["dist100_r4r"] = 26, ["dist100_r4z"] = 27, ["dist100_r4_pause"] = 25 };
            foreach (var entry in expectedLegacy)
            {
                var scenario = Scenario(entry.Key);
                Assert.That(StepOneAt(StepChanges(scenario["steps"])), Is.EqualTo(entry.Value), entry.Key + ": legacy");
                var run = Run(scenario);
                Assert.That(StepOneAt(run.Steps), Is.EqualTo(expectedNew[entry.Key]), entry.Key + ": motor nuevo");
                Assert.That(run.FinishedAtMs, Is.GreaterThan(0), entry.Key + ": la rutina termina sola");
            }
        }

        // ── El motor nuevo manejado como el oráculo ──

        private static EngineRun Run(JsonNode scenario)
        {
            var routineName = scenario["routine"].IsNull ? null : scenario["routine"].AsString();
            var finishAtMs = scenario["finishAtMs"].AsLong();
            var endMs = scenario["runUntilMs"].AsLong();
            var pauses = new List<long[]>();
            foreach (var pause in scenario["pausesMs"].Items)
            {
                var start = pause.Items[0].AsLong();
                pauses.Add(new[] { start, 0L });
                pauses.Add(new[] { start + pause.Items[1].AsLong(), 1L });
            }

            pauses.Sort((a, b) => a[0].CompareTo(b[0]));
            var result = new EngineRun();
            using (var h = new WorkoutHarness(Reading(scenario["reading"].AsString())))
            {
                // El reloj y los objetivos en la foto "terminada": el legacy los lee ahí (sigue en finished hasta reset(),
                // :305-318); el motor nuevo, al volver solo a reposo, los borra como ese reset.
                var finishedTotal = -1;
                using (h.Engine.State.Subscribe(s =>
                       {
                           if (s.Phase != WorkoutPhase.Finished || finishedTotal >= 0)
                           {
                               return;
                           }

                           finishedTotal = s.TotalElapsedSeconds;
                           if (routineName == null)
                           {
                               return;
                           }

                           for (var i = 0; i < 64; i++)
                           {
                               var r = h.Engine.Targets.ResultOf(i);
                               if (r.HasValue)
                               {
                                   result.Targets.Add(Fmt.Integer(i) + ":" + (r.Value == StepResult.Met ? "met" : "missed"));
                               }
                           }
                       }, false))
                {
                    h.At(0);
                    var outcome = routineName == null ? h.Engine.StartFree() : h.Engine.StartWithRoutine(Routine(routineName, scenario["routineId"].AsLong()));
                    Assert.That(outcome, Is.EqualTo(StartOutcome.Started));
                    var lastStep = -2;
                    long tMs = 0;
                    while (tMs < endMs)
                    {
                        var nextMs = (tMs / 100) * 100 + 100;
                        foreach (var e in pauses)
                        {
                            if (e[0] > tMs && e[0] < nextMs)
                            {
                                nextMs = e[0];
                            }
                        }

                        if (finishAtMs > tMs && finishAtMs < nextMs)
                        {
                            nextMs = finishAtMs;
                        }

                        if (nextMs > endMs)
                        {
                            nextMs = endMs;
                        }

                        h.At(nextMs / 1000.0);
                        tMs = nextMs;
                        foreach (var e in pauses)
                        {
                            if (e[0] == tMs)
                            {
                                if (e[1] == 0)
                                {
                                    h.Engine.Pause();
                                }
                                else
                                {
                                    h.Engine.Resume();
                                }
                            }
                        }

                        // El legacy se queda en "terminada" con el paso; el motor nuevo vuelve solo a reposo: se corta antes.
                        if (Ended(h.Engine))
                        {
                            result.FinishedAtMs = tMs;
                            break;
                        }

                        var step = h.Engine.State.Value.Step;
                        var index = step.HasValue ? step.Value.StepIndex : -1;
                        if (index != lastStep)
                        {
                            result.Steps.Add(new[] { tMs, (long)h.Engine.State.Value.TotalElapsedSeconds, index });
                            lastStep = index;
                        }

                        if (finishAtMs > 0 && tMs == finishAtMs && !Ended(h.Engine))
                        {
                            h.Engine.FinishAsync();
                        }

                        if (Ended(h.Engine))
                        {
                            result.FinishedAtMs = tMs;
                            break;
                        }
                    }
                }

                result.Total = finishedTotal;
                result.Points = h.Memory!.PointsOf(1);
                result.Session = h.Session(1);
                result.LastFinished = h.Engine.LastFinishedSessionId;
            }

            return result;
        }

        private static void Compare(string name, JsonNode scenario, EngineRun run, List<string> problems)
        {
            var legacyPoints = scenario["points"].Items;
            if (legacyPoints.Count != run.Points.Count)
            {
                problems.Add(name + ": " + legacyPoints.Count + " puntos en el legacy y " + run.Points.Count + " en el motor nuevo");
            }
            else
            {
                for (var i = 0; i < legacyPoints.Count; i++)
                {
                    var expected = Describe(legacyPoints[i]);
                    var actual = Describe(run.Points[i]);
                    if (expected != actual)
                    {
                        problems.Add(name + " punto " + i + ": legacy " + expected + ", nuevo " + actual);
                    }
                }
            }

            var close = scenario["close"];
            var session = run.Session;
            if (session == null || !session.FinishedAt.HasValue)
            {
                problems.Add(name + ": la sesión no quedó cerrada en el motor nuevo");
            }
            else
            {
                var expected = DescribeClose(close);
                var actual = DescribeClose(session);
                if (expected != actual)
                {
                    problems.Add(name + " cierre: legacy " + expected + ", nuevo " + actual);
                }
            }

            if (!scenario["closeHasFinishedAt"].AsBool())
            {
                problems.Add(name + ": el cierre del legacy no tiene fin");
            }

            var targets = new List<string>();
            foreach (var target in scenario["targets"].Items)
            {
                targets.Add(Fmt.Integer(target.Items[0].AsLong()) + ":" + target.Items[1].AsString());
            }

            if (string.Join(",", targets) != string.Join(",", run.Targets))
            {
                problems.Add(name + " objetivos: legacy " + string.Join(",", targets) + ", nuevo " + string.Join(",", run.Targets));
            }

            var steps = StepChanges(scenario["steps"]);
            if (DescribeSteps(steps) != DescribeSteps(run.Steps))
            {
                problems.Add(name + " pasos: legacy " + DescribeSteps(steps) + ", nuevo " + DescribeSteps(run.Steps));
            }

            if (scenario["finishedAtMs"].AsLong() != run.FinishedAtMs)
            {
                problems.Add(name + ": termina a los " + scenario["finishedAtMs"].AsLong() + " ms en el legacy y a los " + run.FinishedAtMs + " en el nuevo");
            }

            if (scenario["total"].AsLong() != run.Total)
            {
                problems.Add(name + " reloj: legacy " + scenario["total"].AsLong() + ", nuevo " + run.Total);
            }

            if (run.LastFinished != 1)
            {
                problems.Add(name + ": sin la última terminada");
            }
        }

        private static bool Ended(WorkoutEngine engine)
        {
            var phase = engine.State.Value.Phase;
            return phase == WorkoutPhase.Finished || phase == WorkoutPhase.Idle;
        }

        private static Func<int, RowingData> Reading(string name)
        {
            switch (name)
            {
                case "R-4":
                    return RowerScripts.R4;
                case "R-4(19)":
                    return RowerScripts.R4Spm(19);
                case "R-4R":
                    return RowerScripts.R4R;
                case "R-4Z":
                    return RowerScripts.R4Z;
                default:
                    throw new InvalidOperationException("lectura desconocida en " + FileName + ": " + name);
            }
        }

        private static Routine Routine(string name, long id)
        {
            switch (name)
            {
                case "Corta":
                    return WorkoutHarness.Saved(RoutineSeeds.Corta(), id);
                case "Prueba 4a":
                    return WorkoutHarness.Saved(RoutineSeeds.PruebaCuatroA(), id);
                case "Distancia 100":
                    return WorkoutHarness.Saved(RoutineSeeds.DistanciaCien(), id);
                default:
                    throw new InvalidOperationException("rutina desconocida en " + FileName + ": " + name);
            }
        }

        private static IEnumerable<JsonNode> Scenarios()
        {
            return GoldenVectors.Load(FileName).Section("scenarios").Items;
        }

        private static JsonNode Scenario(string name)
        {
            foreach (var scenario in Scenarios())
            {
                if (scenario["name"].AsString() == name)
                {
                    return scenario;
                }
            }

            throw new InvalidOperationException("no está el escenario " + name);
        }

        private static List<long[]> StepChanges(JsonNode steps)
        {
            var list = new List<long[]>();
            foreach (var step in steps.Items)
            {
                list.Add(new[] { step.Items[0].AsLong(), step.Items[1].AsLong(), step.Items[2].AsLong() });
            }

            return list;
        }

        // Los segundos activos en los que empieza el paso 1 (el fin del paso por distancia).
        private static long StepOneAt(List<long[]> steps)
        {
            foreach (var step in steps)
            {
                if (step[2] == 1)
                {
                    return step[1];
                }
            }

            return -1;
        }

        private static string DescribeSteps(List<long[]> steps)
        {
            var parts = new List<string>();
            foreach (var step in steps)
            {
                parts.Add(step[0] + "/" + step[1] + "/" + step[2]);
            }

            return string.Join(" ", parts);
        }

        private static string Describe(JsonNode p)
        {
            return p["session_id"].AsLong() + "|" + p["elapsed_seconds"].AsLong() + "|" + GoldenJsonExtensions.ToF64Bits(p["stroke_rate"].AsF64()) + "|"
                + p["stroke_count"].AsLong() + "|" + p["distance_meters"].AsLong() + "|" + p["pace_500m_seconds"].AsLong() + "|"
                + p["power_watts"].AsLong() + "|" + p["calories"].AsLong() + "|" + p["heart_rate"].AsLong() + "|"
                + (p["step_index"].IsNull ? "-" : Fmt.Integer(p["step_index"].AsLong())) + "|" + (p["step_type"].IsNull ? "-" : p["step_type"].AsString());
        }

        private static string Describe(DataPoint p)
        {
            return p.SessionId + "|" + p.ElapsedSeconds + "|" + GoldenJsonExtensions.ToF64Bits(p.StrokeRate) + "|" + p.StrokeCount + "|"
                + p.DistanceMeters + "|" + p.Pace500mSeconds + "|" + p.PowerWatts + "|" + p.Calories + "|" + p.HeartRate + "|"
                + (p.StepIndex.HasValue ? Fmt.Integer(p.StepIndex.Value) : "-") + "|" + (p.StepTypeName ?? "-");
        }

        private static string DescribeClose(JsonNode c)
        {
            return (c["routine_id"].IsNull ? "-" : Fmt.Integer(c["routine_id"].AsLong())) + "|" + (c["routine_name"].IsNull ? "-" : c["routine_name"].AsString())
                + "|" + c["total_distance_meters"].AsLong() + "|" + c["total_time_seconds"].AsLong() + "|" + c["avg_power_watts"].AsLong() + "|"
                + GoldenJsonExtensions.ToF64Bits(c["avg_stroke_rate"].AsF64()) + "|" + c["total_calories"].AsLong();
        }

        private static string DescribeClose(WorkoutSession s)
        {
            return (s.RoutineId.HasValue ? Fmt.Integer(s.RoutineId.Value) : "-") + "|" + (s.RoutineName ?? "-") + "|" + s.TotalDistanceMeters + "|"
                + s.TotalTimeSeconds + "|" + s.AvgPowerWatts + "|" + GoldenJsonExtensions.ToF64Bits(s.AvgStrokeRate) + "|" + s.TotalCalories;
        }

        private sealed class EngineRun
        {
            public List<long[]> Steps { get; } = new List<long[]>();

            public List<string> Targets { get; } = new List<string>();

            public IReadOnlyList<DataPoint> Points { get; set; } = new DataPoint[0];

            public WorkoutSession? Session { get; set; }

            public long? LastFinished { get; set; }

            public long FinishedAtMs { get; set; } = -1;

            public long Total { get; set; }
        }
    }
}
