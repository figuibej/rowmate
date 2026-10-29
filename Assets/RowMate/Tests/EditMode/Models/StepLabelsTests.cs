using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-03 y F-RUT-04: etiquetas de duración y de objetivos del paso con el catálogo (Q-01), idénticas al legacy en es
    // y en (legacy: interval_step.dart:67-109; routine_editor_screen.dart:345-349). Casos 9 y 10 de RUT §5.3.
    [TestFixture]
    public sealed class StepLabelsTests
    {
        private static IntervalStep S(int? dur = null, int? dist = null, int? wMin = null, int? wMax = null, int? spm = null, int? split = null,
            int? pSpm = null, int? pW = null, int? pSplit = null)
        {
            return RoutineFixtures.Step(StepType.Work, dur, dist, wMin, wMax, spm, split, null, null, pSpm, pW, pSplit);
        }

        private static void ExpectDuration(IntervalStep step, string expected)
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                Assert.That(StepLabels.Duration(step, l10n), Is.EqualTo(expected), l10n.LanguageCode + ": " + RoutineFixtures.Describe(step));
            }
        }

        private static void ExpectTargets(IntervalStep step, string expected)
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                Assert.That(StepLabels.Targets(step, l10n), Is.EqualTo(expected), l10n.LanguageCode + ": " + RoutineFixtures.Describe(step));
            }
        }

        // AC-RUT-03-01 (caso 9): m = s ÷ 60 entero, s = resto; sin horas.
        [TestCase(0, "0s")]
        [TestCase(45, "45s")]
        [TestCase(59, "59s")]
        [TestCase(60, "1m")]
        [TestCase(61, "1m 1s")]
        [TestCase(90, "1m 30s")]
        [TestCase(120, "2m")]
        [TestCase(3600, "60m")]
        [TestCase(3725, "62m 5s")]
        [TestCase(7200, "120m")]
        [Category("AC-RUT-03-01")]
        public void DurationByTime(int seconds, string expected)
        {
            ExpectDuration(S(dur: seconds), expected);
        }

        // AC-RUT-03-02 (caso 9): metros sin separador de miles.
        [TestCase(500, "500m")]
        [TestCase(2000, "2000m")]
        [TestCase(10000, "10000m")]
        [Category("AC-RUT-03-02")]
        public void DurationByDistance(int meters, string expected)
        {
            ExpectDuration(S(dist: meters), expected);
        }

        // AC-RUT-03-03 (caso 9).
        [Test, Category("AC-RUT-03-03")]
        public void DurationNoneAndBoth()
        {
            ExpectDuration(S(), "?");
            ExpectDuration(S(dur: 90, dist: 500), "1m 30s");
        }

        // AC-RUT-03-04: la división entera trunca y el resto nunca es negativo (reglas de Dart).
        [TestCase(-30, "30s")]
        [TestCase(-60, "0s")]
        [TestCase(-90, "30s")]
        [TestCase(-1, "59s")]
        [Category("AC-RUT-03-04")]
        public void DurationNegatives(int seconds, string expected)
        {
            ExpectDuration(S(dur: seconds), expected);
        }

        // AC-RUT-03-05: mismo texto en los dos idiomas para todas las etiquetas de AC-RUT-03-01 a -04.
        [Test, Category("AC-RUT-03-05")]
        public void DurationSameInBothLanguages()
        {
            var steps = new List<IntervalStep>();
            foreach (var d in new[] { 0, 45, 59, 60, 61, 90, 120, 3600, 3725, 7200, -30, -60, -90 })
            {
                steps.Add(S(dur: d));
            }

            foreach (var m in new[] { 500, 2000, 10000 })
            {
                steps.Add(S(dist: m));
            }

            steps.Add(S());
            steps.Add(S(dur: 90, dist: 500));
            foreach (var step in steps)
            {
                Assert.That(StepLabels.Duration(step, LocalizationFixture.En), Is.EqualTo(StepLabels.Duration(step, LocalizationFixture.Es)),
                    RoutineFixtures.Describe(step));
            }
        }

        // AC-RUT-03-05 + AC-RUT-04 + AC-BASE-03: las etiquetas son las del legacy (oráculo), en es y en en.
        [Test, Category("AC-RUT-03-05"), Category("AC-RUT-03-01"), Category("AC-RUT-03-04"), Category("AC-RUT-04-01"), Category("AC-RUT-04-02"),
         Category("AC-RUT-04-04"), Category("AC-BASE-03")]
        public void MatchesLegacyLabels()
        {
            var golden = GoldenVectors.Load("routines.json");
            var problems = new List<string>();
            foreach (var item in golden.Section("durationLabel").Items)
            {
                var step = S(dur: IntOrNull(item["durationSeconds"]), dist: IntOrNull(item["distanceMeters"]));
                Check(problems, "durationLabel", step, item["label"].AsString(), StepLabels.Duration);
            }

            foreach (var item in golden.Section("targetLabel").Items)
            {
                Check(problems, "targetLabel", RoutineFixtures.FromMap(item["step"]), item["label"].AsString(), StepLabels.Targets);
            }

            foreach (var template in golden.Section("templates").Items)
            {
                var flattened = RoutineFixtures.StepsFrom(template["flattenedSteps"]);
                var labels = template["labels"].Items;
                Assert.That(labels.Count, Is.EqualTo(flattened.Count), template["name"].AsString());
                for (var i = 0; i < flattened.Count; i++)
                {
                    var where = template["name"].AsString() + "[" + i + "]";
                    Check(problems, where, flattened[i], labels[i].Items[0].AsString(), StepLabels.Duration);
                    Check(problems, where, flattened[i], labels[i].Items[1].AsString(), StepLabels.Targets);
                }
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // AC-RUT-03 + AC-BASE-03: durationLabel del legacy para -200…7300 s.
        [Test, Category("AC-RUT-03-01"), Category("AC-RUT-03-04"), Category("AC-BASE-03")]
        public void MatchesLegacyDurationSweep()
        {
            var count = GoldenVectors.CheckRows("routines.json", "durationSweep", row =>
            {
                var step = S(dur: (int)row.Int("durationSeconds"));
                var expected = row.Text("label");
                var es = StepLabels.Duration(step, LocalizationFixture.Es);
                var en = StepLabels.Duration(step, LocalizationFixture.En);
                return es == expected && en == expected ? null : "esperado \"" + expected + "\", es \"" + es + "\", en \"" + en + "\"";
            });
            Assert.That(count, Is.EqualTo(7501));
        }

        // AC-RUT-04 + AC-BASE-03: targetLabel del legacy para 400 pasos aleatorios (cualquier campo puede faltar).
        [Test, Category("AC-RUT-04-01"), Category("AC-RUT-04-02"), Category("AC-RUT-04-04"), Category("AC-BASE-03")]
        public void MatchesLegacyTargetFuzz()
        {
            GoldenVectors.CheckRows("routines.json", "targetFuzz", row =>
            {
                var step = S(wMin: IntOrNull(row.Cell("targetWattsMin")), wMax: IntOrNull(row.Cell("targetWattsMax")),
                    spm: IntOrNull(row.Cell("targetSpm")), split: IntOrNull(row.Cell("targetSplitSeconds")),
                    pSpm: IntOrNull(row.Cell("progressionSpm")), pW: IntOrNull(row.Cell("progressionWatts")),
                    pSplit: IntOrNull(row.Cell("progressionSplitSeconds")));
                var expected = row.Text("label");
                var es = StepLabels.Targets(step, LocalizationFixture.Es);
                var en = StepLabels.Targets(step, LocalizationFixture.En);
                return es == expected && en == expected ? null : "esperado \"" + expected + "\", es \"" + es + "\", en \"" + en + "\"";
            });
        }

        // AC-RUT-04-01 (caso 10): vatios, spm y split; las partes se unen con " · ".
        [Test, Category("AC-RUT-04-01")]
        public void Targets()
        {
            ExpectTargets(S(), "");
            ExpectTargets(S(wMin: 150), ">150W");
            ExpectTargets(S(wMin: 120, wMax: 180), "120-180W");
            ExpectTargets(S(wMax: 180), "");
            ExpectTargets(S(spm: 22), "22 spm");
            ExpectTargets(S(split: 59), "00:59/500m");
            ExpectTargets(S(split: 125), "02:05/500m");
            ExpectTargets(S(split: 600), "10:00/500m");
            ExpectTargets(S(split: 6000), "100:00/500m");
            ExpectTargets(S(wMin: 120, wMax: 180, spm: 22, split: 125), "120-180W · 22 spm · 02:05/500m");
        }

        // AC-RUT-04-02 (caso 10): progresiones ≠ 0 en orden SPM, W, split, con "📈 " y "+" solo si es positiva.
        [Test, Category("AC-RUT-04-02")]
        public void Progressions()
        {
            ExpectTargets(S(pSpm: 2), "📈 SPM +2/rep");
            ExpectTargets(S(spm: 20, pSpm: -1), "20 spm · 📈 SPM -1/rep");
            ExpectTargets(S(pW: 10), "📈 W +10/rep");
            ExpectTargets(S(pSplit: 3), "📈 Split +3s/rep");
            ExpectTargets(S(pSpm: 0, pW: 0, pSplit: 0), "");
            ExpectTargets(S(wMax: 180, pW: 10), "📈 W +10/rep");
            ExpectTargets(S(pSplit: -2, pSpm: 1), "📈 SPM +1/rep, Split -2s/rep");
        }

        // AC-RUT-04-03 (caso 10).
        [Test, Category("AC-RUT-04-03")]
        public void FullLine()
        {
            ExpectTargets(S(wMin: 120, wMax: 180, spm: 22, split: 125, pSpm: 2, pW: 5, pSplit: -2),
                "120-180W · 22 spm · 02:05/500m · 📈 SPM +2/rep, W +5/rep, Split -2s/rep");
        }

        // AC-RUT-04-04: split sin piso (objetivos que dejó una progresión negativa).
        [TestCase(0, "00:00/500m")]
        [TestCase(-5, "00:55/500m")]
        [TestCase(-65, "-1:55/500m")]
        [Category("AC-RUT-04-04")]
        public void SplitWithoutFloor(int split, string expected)
        {
            ExpectTargets(S(split: split), expected);
        }

        // AC-RUT-04-05: el paso expandido conserva la progresión (140–160 W con +10, ×2 → repetición 2).
        [Test, Category("AC-RUT-04-05")]
        public void ExpandedStepKeepsProgression()
        {
            var flattened = RoutineExpansion.Flatten(RoutineFixtures.Named["rut04_05"]);
            Assert.That(flattened.Count, Is.EqualTo(2));
            ExpectTargets(flattened[0], "140-160W · 📈 W +10/rep");
            ExpectTargets(flattened[1], "150-170W · 📈 W +10/rep");
        }

        // AC-RUT-04-06: subtítulo del editor = duración, o "{duración}  ·  {objetivos}".
        [Test, Category("AC-RUT-04-06")]
        public void EditorSubtitle()
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                Assert.That(StepLabels.EditorSubtitle(S(dur: 300), l10n), Is.EqualTo("5m"));
                Assert.That(StepLabels.EditorSubtitle(S(dur: 300, spm: 18, pSpm: 2), l10n), Is.EqualTo("5m  ·  18 spm · 📈 SPM +2/rep"));
                Assert.That(StepLabels.EditorSubtitle(S(dist: 500, wMax: 180), l10n), Is.EqualTo("500m"), "objetivos vacíos");
                Assert.That(StepLabels.EditorSubtitle(S(wMin: 150), l10n), Is.EqualTo("?  ·  >150W"));
            }

            Assert.That(StepLabels.SubtitleSeparator, Is.EqualTo("  ·  "));
            Assert.That(StepLabels.PartSeparator, Is.EqualTo(" · "));
            Assert.That(StepLabels.ProgressionSeparator, Is.EqualTo(", "));
        }

        [Test, Category("AC-RUT-04-06")]
        [Ignore("Se verifica en PlayMode: RoutineEditorTests.BorderSubtitlesOnScreen (It. 3, los subtítulos de las 11 filas de AC-RUT-12-06) y en CAP-12a / CAP-12b")]
        public void EditorSubtitleOnScreen()
        {
        }

        private static void Check(List<string> problems, string where, IntervalStep step, string expected,
            System.Func<IntervalStep, Localizer, string> label)
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                var actual = label(step, l10n);
                if (actual != expected)
                {
                    problems.Add(where + " " + l10n.LanguageCode + " " + RoutineFixtures.Describe(step) + ": esperado \"" + expected + "\", obtenido \"" + actual + "\"");
                }
            }
        }

        private static int? IntOrNull(JsonNode node)
        {
            return node.IsNull ? (int?)null : (int)node.AsLong();
        }
    }
}
