using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-06: expansión de series con progresiones (legacy: routine.dart:29-70; 01-po-spec §4.11, casos 1-8 de RUT §5.3).
    [TestFixture]
    public sealed class RoutineExpansionTests
    {
        private static IReadOnlyList<IntervalStep> Flatten(string template)
        {
            return RoutineExpansion.Flatten(RoutineFixtures.Named[template]);
        }

        // AC-RUT-06-01: T1 → 8 pasos; el último de la serie se incluye en todas las repeticiones.
        [Test, Category("AC-RUT-06-01")]
        public void T1ExpandsToEight()
        {
            var flat = Flatten("T1");
            Assert.That(flat.Count, Is.EqualTo(8));
            var expected = new[]
            {
                (StepType.Warmup, (int?)300, (int?)null), (StepType.Work, null, 500), (StepType.Cooldown, 60, null),
                (StepType.Work, null, 500), (StepType.Cooldown, 60, null), (StepType.Work, null, 500), (StepType.Cooldown, 60, null),
                (StepType.Cooldown, 300, null),
            };
            for (var i = 0; i < 8; i++)
            {
                Assert.That(flat[i].Type, Is.EqualTo(expected[i].Item1), "paso " + i);
                Assert.That(flat[i].DurationSeconds, Is.EqualTo(expected[i].Item2), "paso " + i);
                Assert.That(flat[i].DistanceMeters, Is.EqualTo(expected[i].Item3), "paso " + i);
            }
        }

        // AC-RUT-06-02 (caso 1): 6 pasos, SPM 18/20/22, descansos sin cambio, el último es un descanso.
        [Test, Category("AC-RUT-06-02")]
        public void Case1SpmProgression()
        {
            var flat = Flatten("case1");
            Assert.That(flat.Count, Is.EqualTo(6));
            Assert.That(Types(flat), Is.EqualTo(new[] { StepType.Work, StepType.Rest, StepType.Work, StepType.Rest, StepType.Work, StepType.Rest }));
            Assert.That(new[] { flat[0].TargetSpm, flat[2].TargetSpm, flat[4].TargetSpm }, Is.EqualTo(new int?[] { 18, 20, 22 }));
            Assert.That(flat[1], Is.EqualTo(RoutineFixtures.Named["case1"][1]), "el descanso no cambia");
            Assert.That(flat[3], Is.EqualTo(flat[1]));
            Assert.That(flat[5], Is.EqualTo(flat[1]));
        }

        // AC-RUT-06-03 (caso 2): la progresión de vatios se aplica a los dos extremos.
        [Test, Category("AC-RUT-06-03")]
        public void Case2WattsRange()
        {
            var flat = Flatten("case2");
            Assert.That(Watts(flat), Is.EqualTo(new[] { "140-160", "150-170", "160-180" }));
        }

        // AC-RUT-06-04 (caso 3): solo mínimo + SPM; el máximo sigue vacío.
        [Test, Category("AC-RUT-06-04")]
        public void Case3MinOnly()
        {
            var flat = Flatten("case3");
            Assert.That(Watts(flat), Is.EqualTo(new[] { "150-", "160-", "170-" }));
            Assert.That(Spm(flat), Is.EqualTo(new int?[] { 22, 24, 26 }));
            foreach (var step in flat)
            {
                Assert.That(step.TargetWattsMax, Is.Null);
            }
        }

        // AC-RUT-06-05 (caso 4): split 120, 118, 116 (02:00, 01:58, 01:56).
        [Test, Category("AC-RUT-06-05")]
        public void Case4Split()
        {
            var flat = Flatten("case4");
            Assert.That(new[] { flat[0].TargetSplitSeconds, flat[1].TargetSplitSeconds, flat[2].TargetSplitSeconds }, Is.EqualTo(new int?[] { 120, 118, 116 }));
            Assert.That(new[] { TimeFormats.MinSec(118), TimeFormats.MinSec(116) }, Is.EqualTo(new[] { "01:58", "01:56" }));
        }

        // AC-RUT-06-06 (caso 5): progresión sin objetivo → el objetivo sigue vacío.
        [Test, Category("AC-RUT-06-06")]
        public void Case5ProgressionWithoutTarget()
        {
            var flat = Flatten("case5");
            Assert.That(flat.Count, Is.EqualTo(3));
            Assert.That(Spm(flat), Is.EqualTo(new int?[] { null, null, null }));
            Assert.That(flat[2].ProgressionSpm, Is.EqualTo(2));
        }

        // AC-RUT-06-07 (caso 6): un paso suelto con progresión no se progresa.
        [Test, Category("AC-RUT-06-07")]
        public void Case6LooseStepNotProgressed()
        {
            var flat = Flatten("case6");
            Assert.That(flat.Count, Is.EqualTo(1));
            Assert.That(flat[0].TargetSpm, Is.EqualTo(20));
            Assert.That(flat[0], Is.SameAs(RoutineFixtures.Named["case6"][0]), "el paso suelto pasa tal cual");
        }

        // AC-RUT-06-08 (caso 7): repeticiones vacías → ×1; 0 o negativas → ningún paso.
        [Test, Category("AC-RUT-06-08")]
        public void Case7EmptyOrNonPositiveRepeats()
        {
            Assert.That(Flatten("case7a").Count, Is.EqualTo(1), "serie sin repeticiones = ×1");
            var b = Flatten("case7b");
            Assert.That(Types(b), Is.EqualTo(new[] { StepType.Warmup, StepType.Cooldown }), "la serie ×0 desaparece");
            Assert.That(Flatten("case7c"), Is.Empty, "×−1 → 0 pasos");
            Assert.That(Flatten("case7d"), Is.Empty, "una rutina que es solo una serie ×0 → 0 pasos");
        }

        // AC-RUT-06-09 (caso 8): dos series contiguas, cada una con su total.
        [Test, Category("AC-RUT-06-09")]
        public void Case8ContiguousSeries()
        {
            Assert.That(Durations(Flatten("case8")), Is.EqualTo(new int?[] { 60, 60, 30, 30, 30 }));
        }

        // AC-RUT-06-10: manda el ×N del PRIMER paso de la serie.
        [Test, Category("AC-RUT-06-10")]
        public void FirstStepRepeatsRule()
        {
            var flat = Flatten("rut06_10");
            Assert.That(flat.Count, Is.EqualTo(4));
            Assert.That(Durations(flat), Is.EqualTo(new int?[] { 60, 30, 60, 30 }));
        }

        // AC-RUT-06-11: el mismo identificador no consecutivo son dos bloques independientes.
        [Test, Category("AC-RUT-06-11")]
        public void SameIdNotConsecutive()
        {
            Assert.That(Durations(Flatten("rut06_11")), Is.EqualTo(new int?[] { 60, 60, 30, 60, 60 }));
        }

        // AC-RUT-06-12: sin piso (SPM 4 con −3 → 4, 1, −2).
        [Test, Category("AC-RUT-06-12")]
        public void NoFloor()
        {
            Assert.That(Spm(Flatten("rut06_12")), Is.EqualTo(new int?[] { 4, 1, -2 }));
        }

        // AC-RUT-06-13: en cada repetición se conservan tipo, duración, distancia, serie, repeticiones y progresiones.
        [Test, Category("AC-RUT-06-13")]
        public void OtherFieldsKept()
        {
            var template = RoutineFixtures.Named["rut06_13"];
            var flat = RoutineExpansion.Flatten(template);
            Assert.That(flat.Count, Is.EqualTo(4));
            for (var i = 0; i < flat.Count; i++)
            {
                var source = template[i % 2];
                var r = i / 2;
                var expected = source with
                {
                    TargetSpm = source.TargetSpm + source.ProgressionSpm * r,
                    TargetWattsMin = source.TargetWattsMin.HasValue ? source.TargetWattsMin + source.ProgressionWatts * r : null,
                    TargetWattsMax = source.TargetWattsMax.HasValue ? source.TargetWattsMax + source.ProgressionWatts * r : null,
                    TargetSplitSeconds = source.TargetSplitSeconds.HasValue ? source.TargetSplitSeconds + source.ProgressionSplitSeconds * r : null,
                };
                Assert.That(flat[i], Is.EqualTo(expected), "paso " + i + ": solo cambian los objetivos con progresión");
                Assert.That(flat[i].Order, Is.EqualTo(source.Order));
                Assert.That(flat[i].GroupId, Is.EqualTo("g"));
                Assert.That(flat[i].GroupRepeatCount, Is.EqualTo(2));
            }

            Assert.That(flat[2].TargetWattsMin, Is.EqualTo(105));
            Assert.That(flat[2].TargetSplitSeconds, Is.EqualTo(129));
            Assert.That(flat[3].TargetSpm, Is.Null, "el descanso no tiene SPM objetivo aunque tenga progresión");
        }

        // QA-G4aT-11 (DT4a-16, It. 4a): la expansión del motor satura la progresión en los límites de int
        // (Flatten(…, Saturate)); la de la plantilla, para mostrarla o validarla, sigue lanzando OverflowException (It. 1). El
        // legacy, con enteros de 64 bits, sigue la cuenta: lo que se ve es lo mismo (un split ≤ 0 siempre está fuera).
        [Test, Category("AC-WKT-12-08")]
        public void SaturateClampsAtTheIntLimits()
        {
            var up = new IntervalStep(0, 0, StepType.Work)
            {
                DurationSeconds = 60, TargetSpm = int.MaxValue - 1, ProgressionSpm = 2, TargetWattsMin = 100, ProgressionWatts = 10, GroupId = "g",
                GroupRepeatCount = 3,
            };
            var down = new IntervalStep(0, 1, StepType.Work)
            {
                DurationSeconds = 60, TargetSplitSeconds = -2147483000, ProgressionSplitSeconds = -5999, GroupId = "g", GroupRepeatCount = 3,
            };
            var template = new[] { up, down };
            Assert.Throws<OverflowException>(() => RoutineExpansion.Flatten(template), "la de la plantilla sigue con checked");
            Assert.Throws<OverflowException>(() => RoutineExpansion.Flatten(template, ProgressionOverflow.Throw));
            var flat = RoutineExpansion.Flatten(template, ProgressionOverflow.Saturate);
            Assert.That(flat.Count, Is.EqualTo(6));
            Assert.That(Spm(flat), Is.EqualTo(new int?[] { int.MaxValue - 1, null, int.MaxValue, null, int.MaxValue, null }), "satura arriba");
            Assert.That(flat[1].TargetSplitSeconds, Is.EqualTo(-2147483000));
            Assert.That(flat[3].TargetSplitSeconds, Is.EqualTo(int.MinValue), "satura abajo");
            Assert.That(flat[5].TargetSplitSeconds, Is.EqualTo(int.MinValue));
            Assert.That(flat[4].TargetWattsMin, Is.EqualTo(120), "lo que entra sigue la cuenta");
        }

        // Sin desborde, Saturate da exactamente la misma expansión que la de la plantilla.
        [Test, Category("AC-WKT-12-08")]
        public void SaturateWithoutOverflowIsTheSame()
        {
            foreach (var entry in RoutineFixtures.Named)
            {
                var thrown = RoutineExpansion.Flatten(entry.Value);
                var saturated = RoutineExpansion.Flatten(entry.Value, ProgressionOverflow.Saturate);
                Assert.That(saturated, Is.EqualTo(thrown), entry.Key);
            }
        }

        // Las plantillas de RoutineFixtures son las mismas que usó el oráculo.
        [Test, Category("AC-BASE-03")]
        public void FixturesMatchOracle()
        {
            var seen = 0;
            foreach (var item in GoldenVectors.Load("routines.json").Section("templates").Items)
            {
                var name = item["name"].AsString();
                Assert.That(RoutineFixtures.Named.ContainsKey(name), Is.True, "falta la plantilla " + name);
                Assert.That(RoutineFixtures.Named[name], Is.EqualTo(RoutineFixtures.StepsFrom(item["steps"])), name);
                seen++;
            }

            Assert.That(seen, Is.EqualTo(RoutineFixtures.Named.Count));
        }

        // Todo F-RUT-06 contra el oráculo: las plantillas con nombre (todos los campos de cada paso expandido) y 80
        // plantillas aleatorias (series contiguas y no contiguas, repeticiones nulas, 0 y negativas, progresiones).
        [Test, Category("AC-RUT-06-01"), Category("AC-RUT-06-02"), Category("AC-RUT-06-03"), Category("AC-RUT-06-04"), Category("AC-RUT-06-05"),
         Category("AC-RUT-06-06"), Category("AC-RUT-06-07"), Category("AC-RUT-06-08"), Category("AC-RUT-06-09"), Category("AC-RUT-06-10"),
         Category("AC-RUT-06-11"), Category("AC-RUT-06-12"), Category("AC-RUT-06-13"), Category("AC-BASE-03")]
        public void MatchesOracle()
        {
            var problems = new List<string>();
            foreach (var item in GoldenVectors.Load("routines.json").Section("templates").Items)
            {
                var name = item["name"].AsString();
                Compare(problems, name, RoutineExpansion.Flatten(RoutineFixtures.StepsFrom(item["steps"])), RoutineFixtures.StepsFrom(item["flattenedSteps"]));
            }

            var templates = Group(GoldenVectors.Rows("routines.json", "fuzzTemplates"));
            var flattened = Group(GoldenVectors.Rows("routines.json", "fuzzFlattened"));
            var cases = 0;
            foreach (var row in GoldenVectors.Rows("routines.json", "fuzzCounts"))
            {
                cases++;
                var id = row.Int("case");
                var template = templates.TryGetValue(id, out var t) ? t : new List<IntervalStep>();
                var expected = flattened.TryGetValue(id, out var f) ? f : new List<IntervalStep>();
                Assert.That(expected.Count, Is.EqualTo(row.Int("flattened")), "fuzz" + id);
                Compare(problems, "fuzz" + id, RoutineExpansion.Flatten(template), expected);
            }

            Assert.That(cases, Is.EqualTo(80));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-RUT-06-01")]
        public void ExpansionDoesNotChangeTheTemplateAndRejectsNull()
        {
            var template = RoutineFixtures.Named["case1"];
            var before = new List<IntervalStep>(template);
            RoutineExpansion.Flatten(template);
            Assert.That(template, Is.EqualTo(before));
            Assert.Throws<ArgumentNullException>(() => RoutineExpansion.Flatten(null!));
            var huge = RoutineFixtures.Template(RoutineFixtures.Step(StepType.Work, spm: int.MaxValue, pSpm: 1, g: "a", rep: 2));
            Assert.Throws<OverflowException>(() => RoutineExpansion.Flatten(huge), "un desborde se avisa, no da la vuelta");
        }

        internal static Dictionary<long, List<IntervalStep>> Group(IEnumerable<GoldenRow> rows)
        {
            var result = new Dictionary<long, List<IntervalStep>>();
            foreach (var row in rows)
            {
                var id = row.Int("case");
                if (!result.TryGetValue(id, out var list))
                {
                    list = new List<IntervalStep>();
                    result[id] = list;
                }

                Assert.That(row.Int("index"), Is.EqualTo(list.Count), "filas en orden");
                list.Add(RoutineFixtures.FromRow(row));
            }

            return result;
        }

        private static void Compare(List<string> problems, string name, IReadOnlyList<IntervalStep> actual, IReadOnlyList<IntervalStep> expected)
        {
            if (actual.Count != expected.Count)
            {
                problems.Add(name + ": " + actual.Count + " pasos expandidos, el legacy da " + expected.Count);
                return;
            }

            for (var i = 0; i < actual.Count; i++)
            {
                if (!actual[i].Equals(expected[i]))
                {
                    problems.Add(name + "[" + i + "]: " + RoutineFixtures.Describe(actual[i]) + " / legacy " + RoutineFixtures.Describe(expected[i]));
                }
            }
        }

        private static StepType[] Types(IReadOnlyList<IntervalStep> steps)
        {
            var result = new StepType[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                result[i] = steps[i].Type;
            }

            return result;
        }

        private static int?[] Durations(IReadOnlyList<IntervalStep> steps)
        {
            var result = new int?[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                result[i] = steps[i].DurationSeconds;
            }

            return result;
        }

        private static int?[] Spm(IReadOnlyList<IntervalStep> steps)
        {
            var result = new int?[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                result[i] = steps[i].TargetSpm;
            }

            return result;
        }

        private static string[] Watts(IReadOnlyList<IntervalStep> steps)
        {
            var result = new string[steps.Count];
            for (var i = 0; i < steps.Count; i++)
            {
                result[i] = (steps[i].TargetWattsMin.HasValue ? Fmt.Integer(steps[i].TargetWattsMin!.Value) : "") + "-"
                    + (steps[i].TargetWattsMax.HasValue ? Fmt.Integer(steps[i].TargetWattsMax!.Value) : "");
            }

            return result;
        }
    }
}
