using System;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // SEL: el sello "Objetivo cumplido / No alcanzado" de 2 s (D-ANI-05; AC-WKT-09-08, -09; P-02, P-03; 02-tech-design §2.5 de la
    // It. 4b): cuándo hay sello y qué dice, con el motor de verdad y con la regla pura. Sale del resultado que el motor calcula al
    // cerrar el paso (step_target_tracker.dart:43-62).
    [TestFixture]
    public sealed class StepSealTests
    {
        [Test, Category("AC-WKT-09-08")]
        public void WhenStepsCloseAloneWithTargets()
        {
            // Estados con R-4: sello a los 10 (paso 0, cumplido), 20 (paso 1, no alcanzado) y 50 s (paso 3, cumplido); a los 30 y
            // a los 60, nada (el descanso no tiene objetivo; el último queda en curso, Q-52).
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Estados());
                h.At(9.9);
                Assert.That(h.State.LastOutcome, Is.Null);
                h.At(10.2);
                Assert.That(Seal(h)!.Value.StepIndex, Is.EqualTo(0));
                Assert.That(h.State.LastOutcome!.Value.Result, Is.EqualTo(StepResult.Met));
                Assert.That(h.State.LastOutcomeAt, Is.EqualTo(10.5).Within(1e-9), "el instante monótono del segundo 10 (el 0 es −0,5)");
                h.At(20.2);
                Assert.That(Seal(h)!.Value.StepIndex, Is.EqualTo(1));
                Assert.That(h.State.LastOutcome!.Value.Result, Is.EqualTo(StepResult.Missed));
                h.At(30.2);
                Assert.That(h.State.LastOutcome!.Value.StepIndex, Is.EqualTo(1), "un paso sin objetivos no lo toca");
                h.At(50.2);
                Assert.That(Seal(h)!.Value.StepIndex, Is.EqualTo(3));
                Assert.That(h.State.LastOutcomeAt, Is.EqualTo(50.5).Within(1e-9));
                h.At(60.2);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.LastOutcome!.Value.StepIndex, Is.EqualTo(3), "el último nunca lo fija");
            }
        }

        [Test, Category("AC-WKT-09-08")]
        public void WhenNeverLastManualFreeOrWithoutTargets()
        {
            Assert.That(StepSeal.Applies(null, 5), Is.False, "sin objetivos (o en libre) no hay resultado");
            Assert.That(StepSeal.Applies(Outcome(4, StepResult.Met), 5), Is.False, "nunca el último (P-02)");
            Assert.That(StepSeal.Applies(Outcome(3, StepResult.Missed), 5), Is.True);
            using (var h = new WorkoutHarness())
            {
                // Un paso cortado con Terminar no tiene resultado.
                h.Routine(RoutineSeeds.Estados());
                h.At(5.2);
                h.Engine.FinishAsync();
                Assert.That(h.State.LastOutcome, Is.Null);
            }

            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(20.2);
                Assert.That(h.State.LastOutcome, Is.Null, "en libre");
            }
        }

        [TestCase("Estados", 0, "Objetivo cumplido", "24.0 / 18 spm")]
        [TestCase("Estados", 1, "No alcanzado", "24.0 / 30 spm")]
        [TestCase("Prueba 4a", 1, "No alcanzado", "150 / 160 W · 24.0 / 22 spm")]
        [TestCase("Pirámide 500", 1, "Objetivo cumplido", "150 / 150 W · 02:05 / 02:05/500m")]
        [TestCase("Pirámide 500", 2, "No alcanzado", "24.0 / 30 spm")]
        public void TextsOfTheSpecCases(string routine, int stepIndex, string title, string detail)
        {
            var template = routine == "Estados" ? RoutineSeeds.Estados() : routine == "Prueba 4a" ? RoutineSeeds.PruebaCuatroA() : RoutineSeeds.PiramideQuinientos();
            using (var h = new WorkoutHarness())
            {
                h.Routine(template);
                var t = 0.2;
                while (!(h.State.LastOutcome.HasValue && h.State.LastOutcome.Value.StepIndex == stepIndex) && t < 1500)
                {
                    t += 1;
                    h.At(t);
                }

                var seal = WorkoutHudStore.SealTexts(StepSeal.For(h.State.LastOutcome!.Value, h.Engine.Steps![stepIndex]), h.State.LastOutcomeAt,
                    LocalizationFixture.Es);
                Assert.That(seal.Title, Is.EqualTo(title));
                Assert.That(seal.Detail, Is.EqualTo(detail));
                var en = WorkoutHudStore.SealTexts(StepSeal.For(h.State.LastOutcome!.Value, h.Engine.Steps![stepIndex]), h.State.LastOutcomeAt,
                    LocalizationFixture.En);
                Assert.That(en.Detail, Is.EqualTo(detail), "los mismos textos del detalle en en");
                Assert.That(en.Title, Is.EqualTo(title == "Objetivo cumplido" ? "Target met" : "Target missed"));
            }
        }

        [Test, Category("AC-WKT-09-09")]
        public void RoundingTowardsTheUnfavourableSide()
        {
            // P-03: el número nunca contradice el veredicto.
            var watts = new StepOutcome(0, StepResult.Missed, 10, 159.6, 24, 10, 125);
            Assert.That(StepSeal.WattsAverage(watts), Is.EqualTo("159"));
            Assert.That(StepSeal.SpmAverage(new StepOutcome(0, StepResult.Missed, 10, 150, 21.96, 10, 125)), Is.EqualTo("21.9"));
            Assert.That(StepSeal.SplitAverage(new StepOutcome(0, StepResult.Missed, 10, 150, 24, 10, 125.2)), Is.EqualTo("02:06"));
            Assert.That(StepSeal.SplitAverage(new StepOutcome(0, StepResult.Met, 10, 150, 24, 10, 124.6)), Is.EqualTo("02:05"));
            Assert.That(StepSeal.SpmAverage(new StepOutcome(0, StepResult.Met, 10, 150, 24.0, 10, 125)), Is.EqualTo("24.0"));
        }

        [Test, Category("AC-WKT-09-09")]
        public void RoundingSpmFloatingPointFix()
        {
            // ⌊10x⌋ en coma flotante puede subir un décimo cuando 10·x redondea hacia arriba hasta un entero k y k / 10 > x:
            // 3,5999999999999996 × 10 da 36,0 y mostraría 3.6, más que el promedio. La corrección baja a 3.5 (k − 1). Precisión
            // al diseño (§2.5): con un promedio a un ulp por debajo de un entero, 21,999999999999996 × 10 da 219,99999999999997 en
            // IEEE 754 (no 220,0), así que ahí ya da 21.9 sin corregir; el número sigue sin pasar nunca del promedio.
            var almost = 21.999999999999996;
            Assert.That(StepSeal.SpmAverage(new StepOutcome(0, StepResult.Missed, 3, 150, almost, 3, 125)), Is.EqualTo("21.9"));
            var tricky = 3.5999999999999996;
            Assert.That(tricky * 10, Is.EqualTo(36.0), "el caso existe");
            Assert.That(Math.Floor(tricky * 10) / 10, Is.GreaterThan(tricky), "sin corregir pasaría del promedio");
            Assert.That(StepSeal.SpmAverage(new StepOutcome(0, StepResult.Missed, 3, 150, tricky, 3, 125)), Is.EqualTo("3.5"));
            Assert.That(StepSeal.SpmAverage(new StepOutcome(0, StepResult.Missed, 3, 150, 0.3, 3, 125)), Is.EqualTo("0.3"));
        }

        [Test, Category("AC-WKT-09-09")]
        public void WithoutRowingReadings()
        {
            // R-4P todo el paso, vatios mín. 160 y SPM 22: No alcanzado · -- / 160 W · -- / 22 spm (sin split informado, --:--).
            using (var h = new WorkoutHarness(RowerScripts.R4P))
            {
                h.Routine(RoutineSeeds.PruebaCuatroA());
                h.At(90.2);
                var outcome = h.State.LastOutcome!.Value;
                Assert.That(outcome.StepIndex, Is.EqualTo(1));
                Assert.That(outcome.Samples, Is.EqualTo(0));
                var seal = WorkoutHudStore.SealTexts(StepSeal.For(outcome, h.Engine.Steps![1]), h.State.LastOutcomeAt, LocalizationFixture.Es);
                Assert.That(seal.Title, Is.EqualTo("No alcanzado"));
                Assert.That(seal.Detail, Is.EqualTo("-- / 160 W · -- / 22 spm"));
            }

            Assert.That(StepSeal.SplitAverage(new StepOutcome(0, StepResult.Missed, 0, double.NaN, double.NaN, 0, double.NaN)), Is.EqualTo("--:--"));
        }

        [Test, Category("AC-WKT-09-09")]
        public void OrderOfThePillAndMaxWattsDoNotCount()
        {
            // En el orden de la pastilla: vatios, SPM, split. Los vatios máx. no son objetivo (step_target_tracker.dart:23-26).
            var step = RoutineSeeds.Step(StepType.Work, dur: 60, wMin: 150, wMax: 180, spm: 24, split: 125);
            var content = StepSeal.For(new StepOutcome(0, StepResult.Met, 10, 170, 26, 10, 120), step);
            Assert.That(content.Fragments.Count, Is.EqualTo(3));
            Assert.That(content.Fragments[0].Metric, Is.EqualTo(SealMetric.Watts));
            Assert.That(content.Fragments[1].Metric, Is.EqualTo(SealMetric.Spm));
            Assert.That(content.Fragments[2].Metric, Is.EqualTo(SealMetric.Split));
            Assert.That(content.Fragments[0].Target, Is.EqualTo("150"), "el mínimo, no el máximo");
            var maxOnly = RoutineSeeds.Step(StepType.Work, dur: 60, wMax: 180);
            Assert.That(StepTargetTracker.HasTargets(maxOnly), Is.False, "solo vatios máx.: sin sello");
            var seal = WorkoutHudStore.SealTexts(content, 1, LocalizationFixture.Es);
            Assert.That(seal.Detail, Is.EqualTo("170 / 150 W · 26.0 / 24 spm · 02:00 / 02:05/500m"));
            Assert.That(StepSeal.DurationSeconds, Is.EqualTo(2));
        }

        private static StepOutcome? Seal(WorkoutHarness h)
        {
            return StepSeal.Applies(h.State.LastOutcome, h.Engine.Steps!.Count) ? h.State.LastOutcome : null;
        }

        private static StepOutcome Outcome(int index, StepResult result)
        {
            return new StepOutcome(index, result, 1, 150, 24, 1, 125);
        }
    }
}
