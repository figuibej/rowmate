using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-07: posición de cada paso expandido en su serie (legacy: routine.dart:74-97; test/routine_positions_test.dart).
    [TestFixture]
    public sealed class RoutinePositionsTests
    {
        private static IReadOnlyList<StepPosition> Positions(string template)
        {
            return RoutineExpansion.Positions(RoutineFixtures.Named[template]);
        }

        // LT-16 (= AC-RUT-07-01, y el largo de AC-RUT-06-01). Mismas entradas y aserciones que el test legacy.
        // legacy: test/routine_positions_test.dart:6 'las posiciones quedan alineadas con flattenedSteps'
        [TestCase(TestName = "las posiciones quedan alineadas con flattenedSteps")]
        [Category("LT-16"), Category("AC-RUT-07-01"), Category("AC-RUT-06-01")]
        public void LegacyPositionsAlignedWithFlattenedSteps()
        {
            var routine = new Routine("t", RoutineFixtures.CreatedAt)
            {
                Steps = new[]
                {
                    new IntervalStep(1, 0, StepType.Warmup) { DurationSeconds = 300 },
                    new IntervalStep(1, 1, StepType.Work) { DistanceMeters = 500, GroupId = "a", GroupRepeatCount = 3 },
                    new IntervalStep(1, 2, StepType.Cooldown) { DurationSeconds = 60, GroupId = "a", GroupRepeatCount = 3 },
                    new IntervalStep(1, 3, StepType.Cooldown) { DurationSeconds = 300 },
                },
            };

            var positions = routine.FlattenedStepPositions();
            Assert.That(positions.Count, Is.EqualTo(routine.FlattenedSteps().Count));
            Assert.That(positions.Count, Is.EqualTo(8));
            Assert.That(positions[0], Is.EqualTo(new StepPosition(null, 1, 1)));
            Assert.That(positions[1], Is.EqualTo(new StepPosition("a", 1, 3)));
            Assert.That(positions[2], Is.EqualTo(new StepPosition("a", 1, 3)));
            Assert.That(positions[3], Is.EqualTo(new StepPosition("a", 2, 3)));
            Assert.That(positions[6], Is.EqualTo(new StepPosition("a", 3, 3)));
            Assert.That(positions[7], Is.EqualTo(new StepPosition(null, 1, 1)));
        }

        // AC-RUT-07-02: T1 completo (el test legacy no verifica [4] y [5]).
        [Test, Category("AC-RUT-07-02")]
        public void T1Complete()
        {
            Assert.That(Positions("T1"), Is.EqualTo(new[]
            {
                StepPosition.Loose, new StepPosition("a", 1, 3), new StepPosition("a", 1, 3), new StepPosition("a", 2, 3),
                new StepPosition("a", 2, 3), new StepPosition("a", 3, 3), new StepPosition("a", 3, 3), StepPosition.Loose,
            }));
            Assert.That(StepPosition.Loose, Is.EqualTo(new StepPosition(null, 1, 1)));
            Assert.That(new StepPosition("a", 2, 3).ToString(), Is.EqualTo("(groupId: a, rep: 2, repCount: 3)"));
        }

        // AC-RUT-07-03 (caso 7): la serie sin repeticiones da (a, 1, 1); [calentamiento, serie ×0, enfriamiento] da 2
        // posiciones sueltas; ×−1 y solo ×0 no dan ninguna.
        [Test, Category("AC-RUT-07-03")]
        public void EmptyOrZeroRepeats()
        {
            Assert.That(Positions("case7a"), Is.EqualTo(new[] { new StepPosition("a", 1, 1) }));
            Assert.That(Positions("case7b"), Is.EqualTo(new[] { StepPosition.Loose, StepPosition.Loose }));
            Assert.That(Positions("case7c"), Is.Empty);
            Assert.That(Positions("case7d"), Is.Empty);
        }

        // AC-RUT-07-04 (caso 8): cada serie contigua con su propio total.
        [Test, Category("AC-RUT-07-04")]
        public void ContiguousSeries()
        {
            Assert.That(Positions("case8"), Is.EqualTo(new[]
            {
                new StepPosition("a", 1, 2), new StepPosition("a", 2, 2), new StepPosition("b", 1, 3), new StepPosition("b", 2, 3),
                new StepPosition("b", 3, 3),
            }));
            Assert.That(Positions("rut06_11"), Is.EqualTo(new[]
            {
                new StepPosition("a", 1, 2), new StepPosition("a", 2, 2), StepPosition.Loose, new StepPosition("a", 1, 2), new StepPosition("a", 2, 2),
            }), "el mismo id no consecutivo es otra serie");
            Assert.That(Positions("rut06_10"), Is.EqualTo(new[]
            {
                new StepPosition("a", 1, 2), new StepPosition("a", 1, 2), new StepPosition("a", 2, 2), new StepPosition("a", 2, 2),
            }), "manda el ×N del primer paso");
        }

        // AC-RUT-07-05: para cada plantilla de §4.11 (y las del oráculo), posiciones y rutina expandida tienen el mismo
        // largo y el mismo orden; las posiciones son las del legacy.
        [Test, Category("AC-RUT-07-05"), Category("AC-RUT-07-01"), Category("AC-RUT-07-03"), Category("AC-RUT-07-04"), Category("AC-BASE-03")]
        public void AlwaysAligned()
        {
            var problems = new List<string>();
            foreach (var name in RoutineFixtures.ExpansionTemplates)
            {
                var template = RoutineFixtures.Named[name];
                var flat = RoutineExpansion.Flatten(template);
                var positions = RoutineExpansion.Positions(template);
                if (flat.Count != positions.Count)
                {
                    problems.Add(name + ": " + flat.Count + " pasos y " + positions.Count + " posiciones");
                    continue;
                }

                for (var i = 0; i < flat.Count; i++)
                {
                    if (flat[i].GroupId != positions[i].GroupId)
                    {
                        problems.Add(name + "[" + i + "]: el paso es de la serie " + flat[i].GroupId + " y la posición de " + positions[i].GroupId);
                    }
                }
            }

            foreach (var item in GoldenVectors.Load("routines.json").Section("templates").Items)
            {
                var name = item["name"].AsString();
                var actual = RoutineExpansion.Positions(RoutineFixtures.StepsFrom(item["steps"]));
                var expected = new List<StepPosition>();
                foreach (var triple in item["positions"].Items)
                {
                    expected.Add(RoutineFixtures.PositionFrom(triple));
                }

                if (!Same(actual, expected))
                {
                    problems.Add(name + ": posiciones distintas del legacy");
                }
            }

            var templates = RoutineExpansionTests.Group(GoldenVectors.Rows("routines.json", "fuzzTemplates"));
            var expectedFuzz = new Dictionary<long, List<StepPosition>>();
            foreach (var row in GoldenVectors.Rows("routines.json", "fuzzFlattened"))
            {
                var id = row.Int("case");
                if (!expectedFuzz.TryGetValue(id, out var list))
                {
                    list = new List<StepPosition>();
                    expectedFuzz[id] = list;
                }

                list.Add(new StepPosition(row.Cell("posGroupId").IsNull ? null : row.Text("posGroupId"), (int)row.Int("posRep"), (int)row.Int("posRepCount")));
            }

            foreach (var row in GoldenVectors.Rows("routines.json", "fuzzCounts"))
            {
                var id = row.Int("case");
                var template = templates.TryGetValue(id, out var t) ? t : new List<IntervalStep>();
                var expected = expectedFuzz.TryGetValue(id, out var e) ? e : new List<StepPosition>();
                var actual = RoutineExpansion.Positions(template);
                if (!Same(actual, expected) || actual.Count != RoutineExpansion.Flatten(template).Count)
                {
                    problems.Add("fuzz" + id + ": posiciones distintas del legacy o desalineadas");
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static bool Same(IReadOnlyList<StepPosition> a, IReadOnlyList<StepPosition> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
