using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;

namespace RowMate.Tests.EditMode.Workout
{
    // LT-39 a LT-44: StepTargetTracker = lib/features/workout/step_target_tracker.dart:1-71, con los casos de
    // test/step_target_tracker_test.dart 1 a 1 (lecturas de 150 W, 24 spm y split 130 salvo lo que se indica, sobre un
    // trabajo de 60 s).
    [TestFixture]
    public sealed class StepTargetTrackerTests
    {
        // legacy: test/step_target_tracker_test.dart:20 'cumplido cuando el promedio de vatios alcanza el mínimo'
        [TestCase(TestName = "cumplido cuando el promedio de vatios alcanza el mínimo", Category = "LT-39,AC-WKT-09-01")]
        public void MetWhenTheWattsAverageReachesTheMinimum()
        {
            var t = new StepTargetTracker();
            t.Update(0, D(watts: 140));
            t.Update(0, D(watts: 160));
            t.Complete(0, Work(watts: 150));
            Assert.That(t.ResultOf(0), Is.EqualTo(StepResult.Met));
        }

        // legacy: test/step_target_tracker_test.dart:28 'no cumplido si falla cualquiera de los objetivos definidos'
        [TestCase(TestName = "no cumplido si falla cualquiera de los objetivos definidos", Category = "LT-40,AC-WKT-09-02")]
        public void MissedIfAnyDefinedTargetFails()
        {
            var t = new StepTargetTracker();
            t.Update(0, D(watts: 200, spm: 22));
            t.Complete(0, Work(watts: 150, spm: 26));
            Assert.That(t.ResultOf(0), Is.EqualTo(StepResult.Missed), "vatios ok, SPM por debajo");
        }

        // legacy: test/step_target_tracker_test.dart:35 'el split se cumple por debajo del objetivo (más rápido)'
        [TestCase(TestName = "el split se cumple por debajo del objetivo (más rápido)", Category = "LT-41,AC-WKT-09-03")]
        public void SplitIsMetBelowTheTarget()
        {
            var t = new StepTargetTracker();
            t.Update(0, D(split: 125));
            t.Complete(0, Work(split: 130));
            Assert.That(t.ResultOf(0), Is.EqualTo(StepResult.Met));

            t.Update(1, D(split: 140));
            t.Complete(1, Work(split: 130));
            Assert.That(t.ResultOf(1), Is.EqualTo(StepResult.Missed));
        }

        // legacy: test/step_target_tracker_test.dart:46 'un paso sin objetivos no tiene resultado'
        [TestCase(TestName = "un paso sin objetivos no tiene resultado", Category = "LT-42,AC-WKT-09-04")]
        public void StepWithoutTargetsHasNoResult()
        {
            var t = new StepTargetTracker();
            t.Update(0, D());
            t.Complete(0, Work());
            Assert.That(t.ResultOf(0), Is.Null);
        }

        // legacy: test/step_target_tracker_test.dart:53 'sin lecturas remando el objetivo no se cumple; los ceros no cuentan'
        [TestCase(TestName = "sin lecturas remando el objetivo no se cumple; los ceros no cuentan", Category = "LT-43,AC-WKT-09-05")]
        public void WithoutRowingItIsMissedAndZerosDoNotCount()
        {
            var t = new StepTargetTracker();
            t.Complete(0, Work(watts: 100));
            Assert.That(t.ResultOf(0), Is.EqualTo(StepResult.Missed), "nunca remó");

            t.Update(1, D(watts: 0, spm: 0, split: 0)); // parado
            t.Update(1, D(watts: 120, spm: 24, split: 0)); // split sin dato
            t.Complete(1, Work(watts: 100, split: 200));
            Assert.That(t.ResultOf(1), Is.EqualTo(StepResult.Met),
                "el 0 de parado no baja el promedio; el split sin dato no se evalúa como 0");
        }

        // legacy: test/step_target_tracker_test.dart:65 'un paso no completado no tiene resultado y reset borra todo'
        [TestCase(TestName = "un paso no completado no tiene resultado y reset borra todo", Category = "LT-44,AC-WKT-09-06")]
        public void UncompletedStepHasNoResultAndResetClears()
        {
            var t = new StepTargetTracker();
            t.Update(0, D());
            Assert.That(t.ResultOf(0), Is.Null);
            t.Complete(0, Work(watts: 100));
            Assert.That(t.ResultOf(0), Is.EqualTo(StepResult.Met));
            t.Reset();
            Assert.That(t.ResultOf(0), Is.Null);
        }

        // AC-WKT-09-07 (la regla): tiene objetivos con vatios mín., SPM o split; los vatios máx. no cuentan; parado = SPM ≤ 0
        // Y vatios ≤ 0 (una lectura con SPM 0 y vatios > 0 sí cuenta).
        [Test, Category("AC-WKT-09-07")]
        public void WhatCountsAsATargetAndAsRowing()
        {
            Assert.That(StepTargetTracker.HasTargets(Work(watts: 1)), Is.True);
            Assert.That(StepTargetTracker.HasTargets(Work(spm: 1)), Is.True);
            Assert.That(StepTargetTracker.HasTargets(Work(split: 1)), Is.True);
            Assert.That(StepTargetTracker.HasTargets(Work() with { TargetWattsMax = 180 }), Is.False, "el máximo no cuenta");
            var t = new StepTargetTracker();
            t.Update(0, D(watts: 200, spm: 0));
            t.Complete(0, Work(watts: 150));
            Assert.That(t.ResultOf(0), Is.EqualTo(StepResult.Met), "con vatios no es parado");
        }

        private static IntervalStep Work(int? watts = null, int? spm = null, int? split = null)
        {
            return new IntervalStep(1, 0, StepType.Work)
            {
                DurationSeconds = 60,
                TargetWattsMin = watts,
                TargetSpm = spm,
                TargetSplitSeconds = split,
            };
        }

        private static RowingData D(int watts = 150, double spm = 24, int split = 130)
        {
            return new RowingData { PowerWatts = watts, StrokeRate = spm, Pace500mSeconds = split };
        }
    }
}
