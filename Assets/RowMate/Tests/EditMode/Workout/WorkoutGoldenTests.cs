using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Ble;
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
    // Los guiones (sección "scripts", QA-V4a-L02, al empezar la 4b) cubren los caminos que la lente de paridad comprobó a mano:
    // se corren acción por acción igual que en el oráculo. Diferencias esperadas: DV-37 (con un paso de duración negativa el
    // stepProgress del legacy lanza y el motor nuevo da 0 restante, con el mismo fin) y QA-V4a-L01 (el monitor que informa la
    // distancia cada 2 s: se comparan los puntos hasta el primer cambio de paso del legacy; lo demás lo decide la 10a).
    [TestFixture]
    public sealed class WorkoutGoldenTests
    {
        private const string FileName = "workout_engine.json";

        // Q-55: los escenarios en los que el saneamiento de la distancia cambia a propósito el fin del paso.
        private static readonly HashSet<string> SanitizerChanges = new HashSet<string>(StringComparer.Ordinal)
        {
            "dist100_r4r", "dist100_r4z", "dist100_r4_pause",
        };

        // Los guiones del oráculo (QA-V4a-L02), en el orden del archivo.
        private static readonly string[] ScriptNames =
        {
            "zero_steps", "neg_step", "nogoal_finish", "finish_in_pause", "dist_last", "time_and_distance", "hr_points", "pause_early",
            "corta_pause_across_change", "two_sessions", "pause_resume_jitter", "finish_at_step_change", "last_step_zero", "free_finish_at_5",
            "dist100_every_2s", "completed_hold_reset",
        };

        // It. 4b (AC-WKT-21-13; 02-tech-design §2.13): Corta con R-4 termina sola a los 30 s, un bpm a los 32 y el reset (el Terminar
        // del diálogo) a los 34.
        private const string CompletedHoldResetScript = "completed_hold_reset";

        // DV-37: el paso de duración negativa (el legacy lanza en stepProgress; el motor nuevo muestra 0:00).
        private const string NegativeStepScript = "neg_step";

        // QA-V4a-L01: el monitor que informa la distancia cada 2 s (Q-55 lo frena; decide la 10a).
        private const string EveryTwoSecondsScript = "dist100_every_2s";

        // stepProgress lanzó en el legacy (el oráculo anota -99).
        private const long LegacyStepThrew = -99;

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

        // ── Guiones (QA-V4a-L02) ──

        [Test, Category("AC-WKT-06-07")]
        public void ScriptsComeFromTheLegacyEngine()
        {
            var names = new List<string>();
            foreach (var script in Scripts())
            {
                names.Add(script["name"].AsString());
                Assert.That(script["closes"].Items.Count, Is.GreaterThan(0), script["name"].AsString() + ": el legacy cerró la sesión");
                foreach (var finished in script["closesHaveFinishedAt"].Items)
                {
                    Assert.That(finished.AsBool(), Is.True, script["name"].AsString() + ": el cierre del legacy tiene fin");
                }
            }

            Assert.That(names, Is.EqualTo(ScriptNames));
        }

        [Test, Category("AC-WKT-06-07"), Category("AC-WKT-03-01"), Category("AC-WKT-03-06"), Category("AC-WKT-05-02"), Category("AC-WKT-21-13")]
        public void ScriptsAreIdenticalToTheLegacy()
        {
            // It. 4b (AC-WKT-21-13): el reset de los guiones es AcknowledgeCompletion() con la rutina terminada sola, así se comparan
            // también el reloj y la fase después del reset y lo que pasa entre el fin y el reset (los cambios de paso de la grilla
            // de 100 ms y los puntos: ninguno). Queda afuera solo la fase después de un finish a mano (vuelve solo a reposo,
            // AC-WKT-01-06; su reset va en el mismo instante), más DV-37 (neg_step).
            // Pasos de 0 s y último de 0 s, sin meta con Terminar, terminar en pausa, último por distancia, tiempo y distancia,
            // pulso del sensor en los puntos, pausa antes del primer segundo, pausa que cruza un cambio de paso, dos sesiones,
            // varias pausas en un segundo, terminar en el segundo del cambio y en un múltiplo de 5: todo igual al legacy (los
            // puntos, las altas, los cierres, los objetivos, los cambios de paso, el fin solo y el reloj de cada acción).
            var problems = new List<string>();
            var compared = 0;
            foreach (var script in Scripts())
            {
                var name = script["name"].AsString();
                if (name == NegativeStepScript || name == EveryTwoSecondsScript)
                {
                    continue;
                }

                compared++;
                CompareScript(name, script, RunScript(script), problems, false);
            }

            Assert.That(compared, Is.EqualTo(14));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-WKT-21-13"), Category("AC-WKT-21-08")]
        public void CompletedHoldReset()
        {
            // AC-WKT-21-13: entre los 30 y los 34 s la fase es finished en los dos motores, el reloj queda en 30, no hay puntos y
            // los resultados de los objetivos siguen; el reset (AcknowledgeCompletion) vuelve a reposo a los 34 y el paso a −1 en
            // la grilla siguiente (34,1).
            var script = Script(CompletedHoldResetScript);
            Assert.That(DescribeSteps(StepChanges(script["steps"])), Is.EqualTo("100/0/0 10000/10/1 15000/15/2 25000/25/3 34100/0/-1"));
            var problems = new List<string>();
            var run = RunScript(script);
            CompareScript(CompletedHoldResetScript, script, run, problems, false);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(string.Join(" ", run.Events), Is.EqualTo("0/routine/0/active 32000/bpm/30/finished 34000/reset/0/idle"));
            Assert.That(run.Points.Count, Is.EqualTo(7), "ningún punto entre el fin y el reset");
            Assert.That(string.Join(",", run.Targets), Is.EqualTo("1:0:met,1:2:met"));
        }

        [Test, Category("AC-WKT-03-07"), Category("AC-WKT-06-07")]
        public void NegativeStepIsTheExpectedDifference()
        {
            // DV-37: mientras dura el paso de −5 s, el stepProgress del legacy lanza (`Invalid argument(s): 0`, clamp(0, d) con
            // d < 0, workout_provider.dart:103) y su pantalla falla; el motor nuevo da ese paso con 0 restante. Todo lo demás,
            // igual: termina el paso en el segundo 1 y la rutina en el 4, con el mismo punto y el mismo cierre.
            var script = Script(NegativeStepScript);
            var threw = 0;
            foreach (var step in script["steps"].Items)
            {
                threw += step.Items[2].AsLong() == LegacyStepThrew ? 1 : 0;
            }

            Assert.That(threw, Is.EqualTo(1), "el legacy lanza en el paso negativo (evidencia de DV-37)");
            var problems = new List<string>();
            CompareScript(NegativeStepScript, script, RunScript(script), problems, true);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-WKT-03-03"), Category("AC-WKT-06-07")]
        public void MonitorEveryTwoSecondsMatchesUntilTheFirstStepChange()
        {
            // QA-V4a-L01: con un monitor que suma 11 m cada 2 s, el legacy (distancia cruda) cambia de paso a los 20 s y termina a
            // los 25; el saneamiento de Q-55 frena el paso por distancia (lo decide la 10a, con el AMS-670B). Hasta el primer
            // cambio de paso del legacy, los dos motores dan los mismos puntos, campo por campo.
            var script = Script(EveryTwoSecondsScript);
            var legacySteps = StepChanges(script["steps"]);
            Assert.That(DescribeSteps(legacySteps), Is.EqualTo("100/0/0 20000/20/1"), "el legacy cambia de paso a los 20 s");
            var legacy = new List<string>();
            foreach (var point in script["points"].Items)
            {
                if (!point["step_index"].IsNull && point["step_index"].AsLong() == 0)
                {
                    legacy.Add(Describe(point));
                }
            }

            var run = RunScript(script);
            Assert.That(legacy.Count, Is.EqualTo(4), "los puntos de los segundos 5 a 20");
            Assert.That(run.Points.Count, Is.GreaterThanOrEqualTo(legacy.Count));
            for (var i = 0; i < legacy.Count; i++)
            {
                Assert.That(Describe(run.Points[i]), Is.EqualTo(legacy[i]), "punto " + i + " del paso 0");
            }
        }

        private static ScriptRun RunScript(JsonNode script)
        {
            var routines = new Dictionary<string, Routine>(StringComparer.Ordinal);
            foreach (var entry in script["routines"].Properties)
            {
                routines[entry.Key] = BuildRoutine(entry.Value);
            }

            var actions = new List<(long Ms, string Kind, JsonNode? Arg)>();
            foreach (var action in script["actions"].Items)
            {
                actions.Add((action.Items[0].AsLong(), action.Items[1].AsString(), action.Items.Count > 2 ? action.Items[2] : null));
            }

            var endMs = script["endMs"].AsLong();
            var run = new ScriptRun();
            using (var h = new WorkoutHarness(Reading(script["reading"].AsString())))
            {
                var inAction = false;
                var finished = false;
                var endedThisTick = false;
                // La rutina que termina sola: el legacy queda en "terminada" con su último paso y sus objetivos hasta el reset;
                // el motor nuevo también (It. 4b, DT4a-15: espera a AcknowledgeCompletion).
                using (h.Engine.State.Subscribe(s =>
                       {
                           if (s.Phase != WorkoutPhase.Finished)
                           {
                               finished = false;
                               return;
                           }

                           if (finished)
                           {
                               return;
                           }

                           finished = true;
                           if (inAction)
                           {
                               return;
                           }

                           endedThisTick = true;
                           run.AutoFinishes++;
                           for (var i = 0; i < 64; i++)
                           {
                               var r = h.Engine.Targets.ResultOf(i);
                               if (r.HasValue)
                               {
                                   run.Targets.Add(Fmt.Integer(run.AutoFinishes) + ":" + Fmt.Integer(i) + ":" + (r.Value == StepResult.Met ? "met" : "missed"));
                               }
                           }
                       }, false))
                {
                    void DoActions(long t)
                    {
                        foreach (var a in actions)
                        {
                            if (a.Ms != t)
                            {
                                continue;
                            }

                            inAction = true;
                            switch (a.Kind)
                            {
                                case "free":
                                    Assert.That(h.Engine.StartFree(), Is.EqualTo(StartOutcome.Started), t + " ms: free");
                                    break;
                                case "routine":
                                    Assert.That(h.Engine.StartWithRoutine(routines[a.Arg!.AsString()]), Is.EqualTo(StartOutcome.Started), t + " ms: routine");
                                    break;
                                case "pause":
                                    h.Engine.Pause();
                                    break;
                                case "resume":
                                    h.Engine.Resume();
                                    break;
                                case "finish":
                                    h.Engine.FinishAsync();
                                    break;
                                case "reset":
                                    // It. 4b: con la rutina terminada sola, el Terminar del diálogo (AcknowledgeCompletion). A mano el
                                    // motor ya volvió solo a reposo al cerrar (AC-WKT-01-06): no hay nada que tocar.
                                    if (h.Engine.Phase == WorkoutPhase.Finished && h.State.EndedBy == WorkoutEndKind.Completed)
                                    {
                                        h.Engine.AcknowledgeCompletion();
                                    }

                                    break;
                                case "bpm":
                                    h.HeartRate.EmitBpm((int)a.Arg!.AsLong());
                                    break;
                                case "status":
                                    h.HeartRate.SetStatus((HrmStatus)Enum.Parse(typeof(HrmStatus), a.Arg!.AsString(), true));
                                    break;
                                default:
                                    throw new InvalidOperationException("acción desconocida en " + FileName + ": " + a.Kind);
                            }

                            inAction = false;
                            if (a.Kind != "finish")
                            {
                                // Terminar a mano es distinto a propósito (AC-WKT-01-06): el legacy queda "terminado" con el reloj
                                // hasta su reset; el motor nuevo vuelve solo a reposo cuando vuelve el cierre.
                                run.Events.Add(t + "/" + a.Kind + "/" + h.State.TotalElapsedSeconds + "/" + h.State.Phase.ToString().ToLowerInvariant());
                            }
                        }
                    }

                    h.At(0);
                    DoActions(0);
                    long tMs = 0;
                    var lastStep = -2;
                    var afterAutoFinish = false;
                    while (tMs < endMs)
                    {
                        var nextMs = tMs / 100 * 100 + 100;
                        foreach (var a in actions)
                        {
                            if (a.Ms > tMs && a.Ms < nextMs)
                            {
                                nextMs = a.Ms;
                            }
                        }

                        if (nextMs > endMs)
                        {
                            nextMs = endMs;
                        }

                        endedThisTick = false;
                        h.At(nextMs / 1000.0);
                        tMs = nextMs;
                        if (endedThisTick)
                        {
                            run.AutoFinishMs.Add(tMs);
                            afterAutoFinish = true;
                        }

                        var step = h.State.Step;
                        var index = step.HasValue ? step.Value.StepIndex : -1;
                        if (index != lastStep)
                        {
                            // It. 4b: el motor espera al reset como el legacy, así que la vuelta a reposo es un cambio de paso de los
                            // dos (antes, la vuelta sola después del fin automático se salteaba).
                            run.Steps.Add(new[] { tMs, (long)h.State.TotalElapsedSeconds, index });
                            run.ShowsZeroOfANegativeStep.Add(step.HasValue && step.Value.Step.DurationSeconds < 0 && step.Value.RemainingInStep == 0);
                            run.AfterAutoFinish.Add(afterAutoFinish);
                            lastStep = index;
                        }

                        if (index >= 0)
                        {
                            afterAutoFinish = false;
                        }

                        DoActions(tMs);
                    }
                }

                run.Points = h.Points();
                run.Sessions = h.Memory!.Sessions;
            }

            return run;
        }

        private static void CompareScript(string name, JsonNode script, ScriptRun run, List<string> problems, bool negativeStep)
        {
            var legacyPoints = script["points"].Items;
            if (legacyPoints.Count != run.Points.Count)
            {
                problems.Add(name + ": " + legacyPoints.Count + " puntos en el legacy y " + run.Points.Count + " en el motor nuevo");
            }
            else
            {
                for (var i = 0; i < legacyPoints.Count; i++)
                {
                    if (Describe(legacyPoints[i]) != Describe(run.Points[i]))
                    {
                        problems.Add(name + " punto " + i + ": legacy " + Describe(legacyPoints[i]) + ", nuevo " + Describe(run.Points[i]));
                    }
                }
            }

            // Las altas (rutina de la plantilla) y los cierres, en orden; todas las sesiones del motor nuevo quedan cerradas.
            var created = script["sessions"].Items;
            if (created.Count != run.Sessions.Count)
            {
                problems.Add(name + ": " + created.Count + " sesiones en el legacy y " + run.Sessions.Count + " en el motor nuevo");
            }
            else
            {
                for (var i = 0; i < created.Count; i++)
                {
                    var expected = (created[i]["routine_id"].IsNull ? "-" : Fmt.Integer(created[i]["routine_id"].AsLong())) + "|"
                                   + (created[i]["routine_name"].IsNull ? "-" : created[i]["routine_name"].AsString());
                    var s = run.Sessions[i];
                    var actual = (s.RoutineId.HasValue ? Fmt.Integer(s.RoutineId.Value) : "-") + "|" + (s.RoutineName ?? "-");
                    if (expected != actual)
                    {
                        problems.Add(name + " alta " + i + ": legacy " + expected + ", nuevo " + actual);
                    }
                }
            }

            var closes = script["closes"].Items;
            var closed = new List<WorkoutSession>();
            foreach (var s in run.Sessions)
            {
                if (s.FinishedAt.HasValue)
                {
                    closed.Add(s);
                }
            }

            if (closes.Count != closed.Count)
            {
                problems.Add(name + ": " + closes.Count + " cierres en el legacy y " + closed.Count + " en el motor nuevo");
            }
            else
            {
                for (var i = 0; i < closes.Count; i++)
                {
                    if (DescribeClose(closes[i]) != DescribeClose(closed[i]))
                    {
                        problems.Add(name + " cierre " + i + ": legacy " + DescribeClose(closes[i]) + ", nuevo " + DescribeClose(closed[i]));
                    }
                }
            }

            var targets = new List<string>();
            foreach (var target in script["targets"].Items)
            {
                targets.Add(Fmt.Integer(target.Items[0].AsLong()) + ":" + Fmt.Integer(target.Items[1].AsLong()) + ":" + target.Items[2].AsString());
            }

            if (string.Join(",", targets) != string.Join(",", run.Targets))
            {
                problems.Add(name + " objetivos: legacy " + string.Join(",", targets) + ", nuevo " + string.Join(",", run.Targets));
            }

            var autoFinish = new List<long>();
            foreach (var t in script["finishedAtMs"].Items)
            {
                autoFinish.Add(t.AsLong());
            }

            if (string.Join(",", autoFinish) != string.Join(",", run.AutoFinishMs))
            {
                problems.Add(name + ": la rutina termina sola a los " + string.Join(",", autoFinish) + " ms en el legacy y a los "
                             + string.Join(",", run.AutoFinishMs) + " en el nuevo");
            }

            // Los cambios de paso. Con DV-37, donde el legacy lanza (-99) el motor nuevo tiene que estar en un paso de duración
            // negativa mostrando 0 restante; lo demás, igual.
            var legacySteps = StepChanges(script["steps"]);
            var sameSteps = legacySteps.Count == run.Steps.Count;
            for (var i = 0; sameSteps && i < legacySteps.Count; i++)
            {
                var l = legacySteps[i];
                var n = run.Steps[i];
                if (l[0] != n[0] || l[1] != n[1])
                {
                    sameSteps = false;
                }
                else if (l[2] == LegacyStepThrew)
                {
                    sameSteps = negativeStep && n[2] >= 0 && run.ShowsZeroOfANegativeStep[i];
                }
                else if (l[2] != n[2])
                {
                    sameSteps = false;
                }
            }

            if (!sameSteps)
            {
                problems.Add(name + " pasos: legacy " + DescribeSteps(legacySteps) + ", nuevo " + DescribeSteps(run.Steps));
            }

            // El reloj y la fase después de cada acción (salvo Terminar a mano: AC-WKT-01-06). It. 4b: también después del reset.
            var events = new List<string>();
            foreach (var e in script["events"].Items)
            {
                var kind = e.Items[1].AsString();
                if (kind != "finish")
                {
                    events.Add(e.Items[0].AsLong() + "/" + kind + "/" + e.Items[2].AsLong() + "/" + e.Items[3].AsString());
                }
            }

            if (string.Join(" ", events) != string.Join(" ", run.Events))
            {
                problems.Add(name + " acciones: legacy " + string.Join(" ", events) + ", nuevo " + string.Join(" ", run.Events));
            }
        }

        private static Routine BuildRoutine(JsonNode routine)
        {
            var steps = new List<IntervalStep>();
            foreach (var s in routine["steps"].Items)
            {
                steps.Add(RoutineSeeds.Step((StepType)Enum.Parse(typeof(StepType), s["type"].AsString(), true), dur: OptionalInt(s["dur"]),
                    dist: OptionalInt(s["dist"]), wMin: OptionalInt(s["wMin"]), spm: OptionalInt(s["spm"]),
                    g: s["g"].IsNull ? null : s["g"].AsString(), rep: OptionalInt(s["rep"]), pSpm: OptionalInt(s["pSpm"])));
            }

            var createdAt = DateTimeOffset.Parse(routine["createdAt"].AsString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            return WorkoutHarness.Saved(RoutineSeeds.Make(routine["name"].AsString(), createdAt, steps.ToArray()), routine["id"].AsLong());
        }

        private static int? OptionalInt(JsonNode node)
        {
            return node.IsNull ? (int?)null : (int)node.AsLong();
        }

        private static IEnumerable<JsonNode> Scripts()
        {
            return GoldenVectors.Load(FileName).Section("scripts").Items;
        }

        private static JsonNode Script(string name)
        {
            foreach (var script in Scripts())
            {
                if (script["name"].AsString() == name)
                {
                    return script;
                }
            }

            throw new InvalidOperationException("no está el guion " + name);
        }

        private sealed class ScriptRun
        {
            public List<long[]> Steps { get; } = new List<long[]>();

            // Paralelo a Steps: el paso de ese cambio tiene duración negativa y muestra 0 restante (DV-37).
            public List<bool> ShowsZeroOfANegativeStep { get; } = new List<bool>();

            // Paralelo a Steps: el cambio llegó después de un fin automático (con la It. 4b, solo el del reset).
            public List<bool> AfterAutoFinish { get; } = new List<bool>();

            public List<long> AutoFinishMs { get; } = new List<long>();

            public int AutoFinishes { get; set; }

            public List<string> Targets { get; } = new List<string>();

            public List<string> Events { get; } = new List<string>();

            public IReadOnlyList<DataPoint> Points { get; set; } = new DataPoint[0];

            public IReadOnlyList<WorkoutSession> Sessions { get; set; } = new WorkoutSession[0];
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
                case "R-4/2s":
                    // QA-V4a-L01: +11 m en los k pares (5,5 m/s informados cada 2 s), como r4every2s del oráculo.
                    return k => RowerScripts.R4(k).CopyWith(distanceMeters: 1000 + 11 * (k / 2));
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
