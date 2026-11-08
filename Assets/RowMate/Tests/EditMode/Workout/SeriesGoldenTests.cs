using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SGD: el SeriesTracker de la app contra el del legacy (05-series-parciales/02-tech-design §2.9; AC-WKT-07-12). Los vectores
    // los genera tools/oracle/run-oracle.ps1 -Only series_vectors corriendo series_tracker.dart del legacy
    // (Tests/Golden/oracle/series_tracker.json); estas pruebas no necesitan Dart. Tres comparaciones (DT5-07):
    // (1) TheTrackerMatchesTheLegacy: el SeriesTracker de la app con las "ops" de cada guion (y de los 12 casos legacy) da los
    //     estados del legacy (los enteros iguales; los segundos con 1e-9);
    // (2) TheEngineFeedsTheOracleInputs: el motor de la app, con el mismo remo, la misma rutina y las mismas pausas, le da al
    //     tracker EXACTAMENTE esas entradas (si el modelo de tiempo de Dart se equivocara, falla esta y no la de paridad);
    // (3) TheEngineSeriesMatchesTheLegacy: la foto Series del motor después de cada segundo es el estado del guion.
    [TestFixture]
    public sealed class SeriesGoldenTests
    {
        private const string FileName = "series_tracker.json";
        private const double Tolerance = 1e-9;

        [Test, Category("AC-WKT-07-12")]
        public void VectorsComeFromTheLegacyTracker()
        {
            var file = GoldenVectors.Load(FileName);
            Assert.That(file.Header.Generator, Is.EqualTo("tools/oracle/bin/series_vectors.dart"));
            Assert.That(File.Exists(RepoPaths.Combine(file.Header.Generator)), Is.True, "el generador está en el repo");
            Assert.That(file.Header.LegacyCommit, Is.EqualTo(GoldenVectors.Load("session.json").Header.LegacyCommit), "el mismo legacy que el resto");
            var constants = file.Section("constants");
            Assert.That(constants["lapMeters"].AsLong(), Is.EqualTo(SeriesTracker.LapMeters));
            Assert.That(constants["maxPreviousReps"].AsLong(), Is.EqualTo(SeriesTracker.MaxPreviousReps));
            Assert.That(constants["maxSpeedMetersPerSecond"].AsF64(), Is.EqualTo(SanitizedDistance.MaxSpeedMetersPerSecond));
            Assert.That(file.Section("legacyCases").Items.Count, Is.EqualTo(11), "los 12 casos: 11 con lecturas y el de RepSummary");
            Assert.That(file.Section("summaries").Items.Count, Is.EqualTo(2));

            // Spec AC-WKT-07-12: Series 600, Cinco reps, Dos series, Pirámide 500 y Corta × los 8 remos; Trabajo 1000 y Fondo 3000;
            // las pausas (1 s, largas y una que cruza un cambio de paso) y un paso de 0 s.
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var script in file.Section("scripts").Items)
            {
                names.Add(script["name"].AsString());
            }

            foreach (var routine in new[] { "Series 600", "Cinco reps", "Dos series", "Pirámide 500", "Corta" })
            {
                foreach (var rower in new[] { "R-4", "R-3", "R-4A", "R-4R", "R-4Z", "R-4D", "R-4P", "R-4X" })
                {
                    Assert.That(names.Contains(routine + " · " + rower), Is.True, routine + " · " + rower);
                }
            }

            foreach (var name in new[]
                     {
                         "Trabajo 1000 · R-4", "Trabajo 1000 · R-4R", "Trabajo 1000 · R-4Z", "Trabajo 1000 · R-4D", "Fondo 3000 · R-4A",
                         "Trabajo 1000 · R-4 · pausa 20,7-50,7", "Series 600 · R-4 · pausa 100,2-101,2", "Series 600 · R-4 · pausa 180,3-200,3",
                         "Paso cero · R-4",
                     })
            {
                Assert.That(names.Contains(name), Is.True, name);
            }
        }

        [Test, Category("AC-WKT-07-12"), Category("AC-WKT-08-06")]
        public void TheSummariesMatchTheLegacy()
        {
            // LT-54 (:126): sin metros de trabajo no hay split; y el split de LT-51.
            foreach (var summary in GoldenVectors.Load(FileName).Section("summaries").Items)
            {
                var r = new RepSummary((int)summary["rep"].AsLong(), (int)summary["total"].AsLong(), (int)summary["work"].AsLong(),
                    summary["seconds"].AsF64());
                if (summary["split"].IsNull)
                {
                    Assert.That(r.WorkSplitSeconds, Is.Null, summary["name"].AsString());
                }
                else
                {
                    Assert.That(r.WorkSplitSeconds, Is.EqualTo(summary["split"].AsF64()), summary["name"].AsString());
                }
            }
        }

        [Test, Category("AC-WKT-07-12")]
        public void TheLegacyCasesMatchTheLegacy()
        {
            var problems = new List<string>();
            var compared = 0;
            foreach (var item in GoldenVectors.Load(FileName).Section("legacyCases").Items)
            {
                compared += Replay(item["name"].AsString(), item, problems);
            }

            Assert.That(compared, Is.GreaterThan(2000));
            Assert.That(problems, Is.Empty, string.Join("\n", Head(problems)));
        }

        [Test, Category("AC-WKT-07-12"), Category("AC-WKT-07-06"), Category("AC-WKT-08-04")]
        public void TheTrackerMatchesTheLegacy()
        {
            var problems = new List<string>();
            var compared = 0;
            foreach (var script in GoldenVectors.Load(FileName).Section("scripts").Items)
            {
                compared += Replay(script["name"].AsString(), script, problems, GapCutoff(script));
            }

            Assert.That(compared, Is.GreaterThan(20000), "todos los segundos de todos los guiones");
            Assert.That(problems, Is.Empty, string.Join("\n", Head(problems)));
        }

        [Test, Category("AC-WKT-07-12"), Category("AC-WKT-07-03"), Category("AC-WKT-07-09")]
        public void TheEngineFeedsTheOracleInputs()
        {
            var problems = new List<string>();
            foreach (var script in GoldenVectors.Load(FileName).Section("scripts").Items)
            {
                var name = script["name"].AsString();
                var run = RunEngine(script);
                var ops = Ops(script);
                var steps = Steps(script);
                var cutoff = GapCutoff(script);
                if (cutoff == null && run.Calls.Count != ops.Count)
                {
                    problems.Add(name + ": " + run.Calls.Count + " llamadas del motor y " + ops.Count + " del guion");
                }

                // R-4D (It. 10a): las entradas son las mismas hasta la primera lectura después del corte, inclusive.
                var last = cutoff == null ? ops.Count : Math.Min(ops.Count, cutoff.Value.Op + 1);
                for (var i = 0; i < Math.Min(run.Calls.Count, last); i++)
                {
                    var call = run.Calls[i];
                    var op = ops[i];
                    var problem = CompareCall(call, op, steps);
                    if (problem != null)
                    {
                        problems.Add(name + " op " + i + ": " + problem);
                        break;
                    }
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", Head(problems)));
        }

        [Test, Category("AC-WKT-07-12"), Category("AC-WKT-07-11")]
        public void TheEngineSeriesMatchesTheLegacy()
        {
            var problems = new List<string>();
            var compared = 0;
            foreach (var script in GoldenVectors.Load(FileName).Section("scripts").Items)
            {
                var name = script["name"].AsString();
                var run = RunEngine(script);
                var expected = ExpectedPerSecond(script);
                var cutoff = GapCutoff(script);
                foreach (var entry in run.SnapshotPerSecond)
                {
                    if (cutoff != null && entry.Key >= cutoff.Value.Time)
                    {
                        // R-4D (It. 10a): desde la lectura después del corte, la regla nueva suma lo que el legacy perdía.
                        break;
                    }

                    if (!expected.TryGetValue(entry.Key, out var state))
                    {
                        problems.Add(name + ": el motor publicó el segundo " + entry.Key + " que el guion no tiene");
                        break;
                    }

                    compared++;
                    var problem = Compare(entry.Value, state);
                    if (problem != null)
                    {
                        problems.Add(name + " segundo " + entry.Key + ": " + problem);
                        break;
                    }
                }

                if (cutoff == null && run.SnapshotPerSecond.Count != expected.Count)
                {
                    problems.Add(name + ": " + run.SnapshotPerSecond.Count + " fotos por segundo y " + expected.Count + " segundos del guion");
                }
            }

            Assert.That(compared, Is.GreaterThan(20000));
            Assert.That(problems, Is.Empty, string.Join("\n", Head(problems)));
        }

        [Test, Category("AC-WKT-07-12")]
        public void TheOracleRoutinesAreTheSeeds()
        {
            // Las rutinas del guion son las de Tests/Support/RoutineSeeds (F-5 y el paso de 0 s): mismos pasos, tipos, metas, series
            // y repeticiones (los objetivos no los mira el tracker).
            foreach (var routine in GoldenVectors.Load(FileName).Section("routines").Items)
            {
                var name = routine["name"].AsString();
                var seed = RoutineSeeds.F5Routine(name);
                var steps = routine["steps"].Items;
                Assert.That(seed.Steps.Count, Is.EqualTo(steps.Count), name);
                for (var i = 0; i < steps.Count; i++)
                {
                    var s = steps[i].Items;
                    var step = seed.Steps[i];
                    Assert.That(TypeName(step.Type), Is.EqualTo(s[0].AsString()), name + " paso " + i);
                    Assert.That(step.DurationSeconds, Is.EqualTo(s[1].IsNull ? (int?)null : (int)s[1].AsLong()), name + " paso " + i);
                    Assert.That(step.DistanceMeters, Is.EqualTo(s[2].IsNull ? (int?)null : (int)s[2].AsLong()), name + " paso " + i);
                    Assert.That(step.GroupId, Is.EqualTo(s[3].IsNull ? null : s[3].AsString()), name + " paso " + i);
                    Assert.That(step.GroupRepeatCount, Is.EqualTo(s[4].IsNull ? (int?)null : (int)s[4].AsLong()), name + " paso " + i);
                }
            }
        }

        // ── El estado esperado ──

        private sealed class Expected
        {
            public int CurrentLapMeters;
            public double CurrentLapSeconds;
            public int RepMeters;
            public bool IsWork;
            public List<double> Laps = new List<double>();
            public bool InSeries;
            public int Rep;
            public int RepCount;
            public List<(int Rep, int Total, int Work, double Seconds, double? Split)> Previous = new List<(int, int, int, double, double?)>();

            public Expected Clone()
            {
                return new Expected
                {
                    CurrentLapMeters = CurrentLapMeters, CurrentLapSeconds = CurrentLapSeconds, RepMeters = RepMeters, IsWork = IsWork,
                    Laps = new List<double>(Laps), InSeries = InSeries, Rep = Rep, RepCount = RepCount,
                    Previous = new List<(int, int, int, double, double?)>(Previous),
                };
            }
        }

        private sealed class Op
        {
            public string Kind = string.Empty;
            public int Step = -1;
            public int Raw;
            public int Time = -1;
            public Expected State = new Expected();
        }

        private static List<Op> Ops(JsonNode item)
        {
            var table = GoldenTable.From(item["name"].AsString() + "/ops", item["ops"]);
            var ops = new List<Op>(table.Count);
            var current = new Expected();
            for (var r = 0; r < table.Count; r++)
            {
                var row = table[r];
                var opCell = row.Cell("op");
                var op = new Op();
                if (opCell.Kind == JsonKind.String)
                {
                    op.Kind = opCell.AsString();
                }
                else
                {
                    op.Kind = "u";
                    op.Step = (int)opCell.AsLong();
                }

                op.Raw = (int)row.Int("raw");
                op.Time = row.Cell("t").IsNull ? -1 : (int)row.Int("t");
                var state = current.Clone();
                state.CurrentLapMeters = (int)row.Int("cm");
                state.CurrentLapSeconds = row.Cell("cs").AsDouble();
                state.RepMeters = (int)row.Int("rm");
                ApplyDelta(state, row.Text("x"));
                op.State = state;
                current = state;
                ops.Add(op);
            }

            return ops;
        }

        // "w1;L125.0,125.0;s1,2,3;P1:680:600:150.0:125.0|…": solo lo que cambió.
        private static void ApplyDelta(Expected state, string delta)
        {
            if (delta.Length == 0)
            {
                return;
            }

            foreach (var part in delta.Split(';'))
            {
                var body = part.Substring(1);
                switch (part[0])
                {
                    case 'w':
                        state.IsWork = body == "1";
                        break;
                    case 'L':
                        state.Laps = new List<double>();
                        if (body.Length > 0)
                        {
                            foreach (var seconds in body.Split(','))
                            {
                                state.Laps.Add(Parse(seconds));
                            }
                        }

                        break;
                    case 's':
                        var s = body.Split(',');
                        state.InSeries = s[0] == "1";
                        state.Rep = int.Parse(s[1], CultureInfo.InvariantCulture);
                        state.RepCount = int.Parse(s[2], CultureInfo.InvariantCulture);
                        break;
                    case 'P':
                        state.Previous = new List<(int, int, int, double, double?)>();
                        if (body.Length > 0)
                        {
                            foreach (var rep in body.Split('|'))
                            {
                                var f = rep.Split(':');
                                state.Previous.Add((int.Parse(f[0], CultureInfo.InvariantCulture), int.Parse(f[1], CultureInfo.InvariantCulture),
                                    int.Parse(f[2], CultureInfo.InvariantCulture), Parse(f[3]), f[4].Length == 0 ? (double?)null : Parse(f[4])));
                            }
                        }

                        break;
                    default:
                        throw new FormatException("delta desconocido: " + part);
                }
            }
        }

        private static double Parse(string text)
        {
            return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static Dictionary<int, (string Type, StepPosition Position)> Steps(JsonNode item)
        {
            var steps = new Dictionary<int, (string, StepPosition)>();
            foreach (var row in item["steps"].Items)
            {
                var r = row.Items;
                steps[(int)r[0].AsLong()] = (r[1].AsString(), new StepPosition(r[2].IsNull ? null : r[2].AsString(), (int)r[3].AsLong(), (int)r[4].AsLong()));
            }

            return steps;
        }

        // ── (1) El tracker de la app con las ops ──

        // It. 10a (QA-V4a-L01, AC-WKT-03-11, DV-103; R-07 de 10a 02-tech-design §11): el tope de 10 m/s cuenta desde el último
        // CAMBIO de la distancia. Con una lectura por segundo da lo mismo que el legacy (todos los guiones salvo R-4D siguen enteros);
        // R-4D es justo el caso que cambia: tras el corte de 3 lecturas (L(101) a L(103)), la de L(104) avanza 16 m en 4 s desde el
        // último cambio y ahora suma (el legacy la tiraba por "16 m en 1 s"). Esos guiones se comparan hasta esa lectura (op, t); lo
        // de después lo fijan SeriesTrackerTests y SeriesEngineTests con los valores nuevos.
        private static (int Op, int Time)? GapCutoff(JsonNode script)
        {
            if (script["rower"].AsString() != "R-4D")
            {
                return null;
            }

            var jumpRaw = RowerScripts.R4(104).DistanceMeters;
            var ops = Ops(script);
            for (var i = 0; i < ops.Count; i++)
            {
                if (ops[i].Kind == "u" && ops[i].Raw == jumpRaw)
                {
                    return (i, ops[i].Time);
                }
            }

            return null;
        }

        private static int Replay(string name, JsonNode item, List<string> problems, (int Op, int Time)? cutoff = null)
        {
            var steps = Steps(item);
            var ops = Ops(item);
            var tracker = new SeriesTracker();
            for (var i = 0; i < ops.Count; i++)
            {
                if (cutoff != null && i >= cutoff.Value.Op)
                {
                    return i;
                }

                var op = ops[i];
                switch (op.Kind)
                {
                    case "b":
                        tracker.Begin(op.Raw, op.Time);
                        break;
                    case "r":
                        tracker.Rebase(op.Raw);
                        break;
                    default:
                        var step = steps[op.Step];
                        tracker.Update(op.Step, new IntervalStep(1, 0, ParseType(step.Type)), step.Position, op.Raw, op.Time);
                        break;
                }

                var problem = Compare(tracker.State, op.State);
                if (problem != null)
                {
                    problems.Add(name + " op " + i + " (" + op.Kind + op.Step + ", raw " + op.Raw + ", t " + op.Time + "): " + problem);
                    return i;
                }
            }

            return ops.Count;
        }

        private static string? Compare(SeriesState actual, Expected expected)
        {
            if (actual.CurrentLapMeters != expected.CurrentLapMeters)
            {
                return "currentLapMeters " + actual.CurrentLapMeters + " y el legacy " + expected.CurrentLapMeters;
            }

            if (Math.Abs(actual.CurrentLapSeconds - expected.CurrentLapSeconds) > Tolerance)
            {
                return "currentLapSeconds " + actual.CurrentLapSeconds + " y el legacy " + expected.CurrentLapSeconds;
            }

            if (actual.RepMeters != expected.RepMeters)
            {
                return "repMeters " + actual.RepMeters + " y el legacy " + expected.RepMeters;
            }

            if (actual.IsWorkStep != expected.IsWork || actual.InSeries != expected.InSeries || actual.Rep != expected.Rep || actual.RepCount != expected.RepCount)
            {
                return "isWork/inSeries/rep/repCount " + actual.IsWorkStep + "/" + actual.InSeries + "/" + actual.Rep + "/" + actual.RepCount + " y el legacy "
                       + expected.IsWork + "/" + expected.InSeries + "/" + expected.Rep + "/" + expected.RepCount;
            }

            if (actual.Laps.Count != expected.Laps.Count)
            {
                return actual.Laps.Count + " vueltas y el legacy " + expected.Laps.Count;
            }

            for (var i = 0; i < actual.Laps.Count; i++)
            {
                if (actual.Laps[i].Number != i + 1 || Math.Abs(actual.Laps[i].Seconds - expected.Laps[i]) > Tolerance)
                {
                    return "vuelta " + i + ": #" + actual.Laps[i].Number + " " + actual.Laps[i].Seconds + " y el legacy " + expected.Laps[i];
                }
            }

            if (actual.PreviousReps.Count != expected.Previous.Count)
            {
                return actual.PreviousReps.Count + " anteriores y el legacy " + expected.Previous.Count;
            }

            for (var i = 0; i < actual.PreviousReps.Count; i++)
            {
                var a = actual.PreviousReps[i];
                var e = expected.Previous[i];
                var split = a.WorkSplitSeconds;
                if (a.Rep != e.Rep || a.TotalMeters != e.Total || a.WorkMeters != e.Work || Math.Abs(a.WorkSeconds - e.Seconds) > Tolerance
                    || split.HasValue != e.Split.HasValue || (split.HasValue && Math.Abs(split.Value - e.Split!.Value) > Tolerance))
                {
                    return "anterior " + i + ": " + a.Rep + ":" + a.TotalMeters + ":" + a.WorkMeters + ":" + a.WorkSeconds + ":" + split + " y el legacy "
                           + e.Rep + ":" + e.Total + ":" + e.Work + ":" + e.Seconds + ":" + e.Split;
                }
            }

            return null;
        }

        // ── (2) y (3) El motor de la app con el mismo guion ──

        private sealed class EngineRun
        {
            public List<SeriesCall> Calls { get; } = new List<SeriesCall>();

            public SortedDictionary<int, SeriesState> SnapshotPerSecond { get; } = new SortedDictionary<int, SeriesState>();
        }

        private static EngineRun RunEngine(JsonNode script)
        {
            var result = new EngineRun();
            var rower = RowerScripts.ByName(script["rower"].AsString());
            var routine = RoutineSeeds.F5Routine(script["routine"].AsString());
            var seconds = (int)script["seconds"].AsLong();
            var pauses = new List<(double Pause, double Resume)>();
            foreach (var pause in script["pausesMs"].Items)
            {
                pauses.Add((pause.Items[0].AsLong() / 1000.0, pause.Items[1].AsLong() / 1000.0));
            }

            pauses.Sort((a, b) => a.Pause.CompareTo(b.Pause));
            using (var h = new WorkoutHarness(rower))
            {
                h.Engine.Series.Trace = result.Calls.Add;
                var last = 0;
                using (h.Engine.State.Subscribe(s =>
                       {
                           if (s.TotalElapsedSeconds > last && s.Series != null)
                           {
                               last = s.TotalElapsedSeconds;
                               result.SnapshotPerSecond[last] = s.Series;
                           }
                       }, false))
                {
                    h.Routine(routine);
                    foreach (var pause in pauses)
                    {
                        h.At(pause.Pause);
                        h.Engine.Pause();
                        h.At(pause.Resume);
                        h.Engine.Resume();
                    }

                    var guard = 0;
                    while (h.State.Phase == WorkoutPhase.Active && h.State.TotalElapsedSeconds < seconds && guard++ < 10000)
                    {
                        h.At(h.Now + 1);
                    }

                    h.Engine.Series.Trace = null;
                }
            }

            return result;
        }

        private static SortedDictionary<int, Expected> ExpectedPerSecond(JsonNode script)
        {
            var result = new SortedDictionary<int, Expected>();
            foreach (var op in Ops(script))
            {
                if (op.Kind == "u")
                {
                    result[op.Time] = op.State;
                }
            }

            return result;
        }

        private static string? CompareCall(SeriesCall call, Op op, Dictionary<int, (string Type, StepPosition Position)> steps)
        {
            var kind = call.Kind == SeriesCallKind.Begin ? "b" : call.Kind == SeriesCallKind.Rebase ? "r" : "u";
            if (kind != op.Kind)
            {
                return "el motor llamó " + kind + " y el guion " + op.Kind;
            }

            if (call.RawMeters != op.Raw)
            {
                return "raw " + call.RawMeters + " y el guion " + op.Raw;
            }

            if (kind == "r")
            {
                return null;
            }

            if (call.ActiveSeconds != op.Time)
            {
                return "t " + call.ActiveSeconds + " y el guion " + op.Time;
            }

            if (kind == "b")
            {
                return null;
            }

            if (call.StepIndex != op.Step)
            {
                return "paso " + call.StepIndex + " y el guion " + op.Step;
            }

            var step = steps[op.Step];
            if (TypeName(call.Step!.Type) != step.Type || call.Position != step.Position)
            {
                return "paso " + op.Step + ": " + TypeName(call.Step.Type) + " " + call.Position + " y el guion " + step.Type + " " + step.Position;
            }

            return null;
        }

        private static string TypeName(StepType type)
        {
            switch (type)
            {
                case StepType.Warmup:
                    return "warmup";
                case StepType.Work:
                    return "work";
                case StepType.Rest:
                    return "rest";
                default:
                    return "cooldown";
            }
        }

        private static StepType ParseType(string name)
        {
            switch (name)
            {
                case "warmup":
                    return StepType.Warmup;
                case "work":
                    return StepType.Work;
                case "rest":
                    return StepType.Rest;
                case "cooldown":
                    return StepType.Cooldown;
                default:
                    throw new FormatException("tipo de paso desconocido: " + name);
            }
        }

        private static IEnumerable<string> Head(List<string> problems)
        {
            for (var i = 0; i < problems.Count && i < 20; i++)
            {
                yield return problems[i];
            }
        }
    }
}
