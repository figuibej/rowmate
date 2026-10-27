using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-05: resumen de la rutina con el catálogo (Q-01 + Q-02) y el mismo conteo que el legacy
    // (legacy: routine.dart:100-105; workout_screen.dart:152). Caso 11 de RUT §5.3.
    [TestFixture]
    public sealed class RoutineSummaryTests
    {
        // AC-RUT-05-02: tabla de la spec (la columna "Legacy" es la del oráculo; se muestran es y en).
        [TestCase("sum_warmup", "calentamiento", "1 pasos", "1 paso", "1 step")]
        [TestCase("sum_warmup_cooldown", "calentamiento + enfriamiento", "2 pasos", "2 pasos", "2 steps")]
        [TestCase("sum_rest_rest", "descanso + descanso", "2 pasos", "2 pasos", "2 steps")]
        [TestCase("sum_work", "trabajo", "1 series · 0 descansos", "1 serie · 0 descansos", "1 interval · 0 rests")]
        [TestCase("sum_warmup_series3_cooldown", "calentamiento + serie ×3 (trabajo, descanso) + enfriamiento", "1 series · 1 descansos",
            "1 serie · 1 descanso", "1 interval · 1 rest")]
        [TestCase("sum_2work_1rest", "2 trabajos sueltos + 1 descanso", "2 series · 1 descansos", "2 series · 1 descanso", "2 intervals · 1 rest")]
        [TestCase("sum_1work_2rest", "1 trabajo + 2 descansos", "1 series · 2 descansos", "1 serie · 2 descansos", "1 interval · 2 rests")]
        [TestCase("sum_4work_3rest", "4 trabajos y 3 descansos sueltos", "4 series · 3 descansos", "4 series · 3 descansos", "4 intervals · 3 rests")]
        [TestCase("sum_empty", "sin pasos", "0 pasos", "0 pasos", "0 steps")]
        [Category("AC-RUT-05-02"), Category("Demo")]
        public void SummaryTable(string template, string description, string legacy, string es, string en)
        {
            var routine = RoutineFixtures.MakeRoutine(template, RoutineFixtures.Named[template]);
            var actualEs = RoutineSummary.Text(routine, LocalizationFixture.Es);
            var actualEn = RoutineSummary.Text(routine, LocalizationFixture.En);
            TestContext.Out.WriteLine("[DEMO] Resumen " + description + " -> es \"" + actualEs + "\" / en \"" + actualEn + "\"");
            Assert.That(actualEs, Is.EqualTo(es));
            Assert.That(actualEn, Is.EqualTo(en));
            Assert.That(LegacyText(RoutineSummary.Count(routine.Steps)), Is.EqualTo(legacy), "la columna legacy sale del mismo conteo");
        }

        // AC-RUT-05-03: contenido del diálogo de inicio (workoutStartContent{summary}).
        [Test, Category("AC-RUT-05-03")]
        public void WorkoutStartContent()
        {
            var routine = RoutineFixtures.MakeRoutine("r", RoutineFixtures.Named["sum_warmup_series3_cooldown"]);
            Assert.That(RoutineSummary.StartDialogContent(routine, LocalizationFixture.Es), Is.EqualTo("1 serie · 1 descanso\n\n¿Iniciar el entrenamiento?"));
            Assert.That(RoutineSummary.StartDialogContent(routine, LocalizationFixture.En), Is.EqualTo("1 interval · 1 rest\n\nStart the workout?"));
        }

        [Test, Category("AC-RUT-05-03")]
        [Ignore("NO VERIFICABLE → It. 4a: [Editor] el diálogo de inicio de rutina")]
        public void StartDialogOnScreen()
        {
        }

        // AC-RUT-05-04 + AC-BASE-03: para cada plantilla del oráculo, los números del texto nuevo son los del texto legacy.
        [Test, Category("AC-RUT-05-04"), Category("AC-RUT-05-02"), Category("AC-BASE-03")]
        public void CountsMatchLegacy()
        {
            var problems = new List<string>();
            var templates = 0;
            foreach (var item in GoldenVectors.Load("routines.json").Section("templates").Items)
            {
                templates++;
                var name = item["name"].AsString();
                var steps = RoutineFixtures.StepsFrom(item["steps"]);
                var counts = RoutineSummary.Count(steps);
                var expected = new RoutineCounts((int)item["total"].AsLong(), (int)item["work"].AsLong(), (int)item["rest"].AsLong());
                if (!counts.Equals(expected))
                {
                    problems.Add(name + ": conteos " + Describe(counts) + ", legacy " + Describe(expected));
                }

                if (LegacyText(counts) != item["summary"].AsString())
                {
                    problems.Add(name + ": \"" + LegacyText(counts) + "\" frente al legacy \"" + item["summary"].AsString() + "\"");
                }

                CheckNumbersInNewTexts(problems, name, RoutineFixtures.MakeRoutine(name, steps), counts);
            }

            var fuzz = 0;
            foreach (var row in GoldenVectors.Rows("routines.json", "fuzzCounts"))
            {
                fuzz++;
                var expected = new RoutineCounts((int)row.Int("total"), (int)row.Int("work"), (int)row.Int("rest"));
                if (LegacyText(expected) != row.Text("summary"))
                {
                    problems.Add("fuzz" + row.Int("case") + ": \"" + LegacyText(expected) + "\" frente al legacy \"" + row.Text("summary") + "\"");
                }
            }

            Assert.That(templates, Is.GreaterThanOrEqualTo(26));
            Assert.That(fuzz, Is.EqualTo(80));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test, Category("AC-RUT-05-04")]
        public void CountsUseTheTemplateNotTheExpansion()
        {
            // "Series" cuenta pasos de trabajo de la plantilla: una serie ×3 de trabajo + descanso cuenta 1 y 1.
            var counts = RoutineSummary.Count(RoutineFixtures.Named["sum_warmup_series3_cooldown"]);
            Assert.That(counts, Is.EqualTo(new RoutineCounts(4, 1, 1)));
            Assert.That(RoutineExpansion.Flatten(RoutineFixtures.Named["sum_warmup_series3_cooldown"]).Count, Is.EqualTo(8));
            // Una serie ×−1 no tiene pasos expandidos pero el resumen cuenta la plantilla (QA §4, caso 16).
            var negative = RoutineFixtures.MakeRoutine("x", RoutineFixtures.Named["case7c"]);
            Assert.That(negative.FlattenedSteps(), Is.Empty);
            Assert.That(RoutineSummary.Text(negative, LocalizationFixture.Es), Is.EqualTo("1 serie · 0 descansos"));
            Assert.That(RoutineSummary.Text(negative, LocalizationFixture.En), Is.EqualTo("1 interval · 0 rests"));
        }

        // El texto del legacy ('${steps.length} pasos' / '$work series · $rest descansos', routine.dart:103-104): solo
        // para comprobar que los números son los mismos; no se muestra en la app nueva.
        private static string LegacyText(RoutineCounts counts)
        {
            return counts.Work == 0
                ? Fmt.Integer(counts.Total) + " pasos"
                : Fmt.Integer(counts.Work) + " series · " + Fmt.Integer(counts.Rest) + " descansos";
        }

        private static void CheckNumbersInNewTexts(List<string> problems, string name, Routine routine, RoutineCounts counts)
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                var text = RoutineSummary.Text(routine, l10n);
                var numbers = counts.Work == 0 ? Fmt.Integer(counts.Total) : Fmt.Integer(counts.Work) + "|" + Fmt.Integer(counts.Rest);
                var found = Numbers(text);
                if (found != numbers)
                {
                    problems.Add(name + " " + l10n.LanguageCode + ": \"" + text + "\" tiene los números " + found + ", se esperaban " + numbers);
                }
            }
        }

        private static string Numbers(string text)
        {
            var parts = new List<string>();
            var current = "";
            foreach (var c in text + " ")
            {
                if (c >= '0' && c <= '9')
                {
                    current += c;
                }
                else if (current.Length > 0)
                {
                    parts.Add(current);
                    current = "";
                }
            }

            return string.Join("|", parts);
        }

        private static string Describe(RoutineCounts c)
        {
            return "total=" + Fmt.Integer(c.Total) + " work=" + Fmt.Integer(c.Work) + " rest=" + Fmt.Integer(c.Rest);
        }
    }
}
