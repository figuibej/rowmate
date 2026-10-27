using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-02: modelo de paso (legacy: interval_step.dart:21-62, :64-77, :147-180).
    [TestFixture]
    public sealed class IntervalStepTests
    {
        private static readonly string[] OptionalIntegers =
        {
            "DurationSeconds", "DistanceMeters", "TargetWattsMin", "TargetWattsMax", "TargetSpm", "TargetSplitSeconds",
            "GroupRepeatCount", "ProgressionSpm", "ProgressionWatts", "ProgressionSplitSeconds",
        };

        // AC-RUT-02-01: rutina, orden (0-based) y tipo obligatorios; el resto opcional, todos los números enteros.
        [Test, Category("AC-RUT-02-01")]
        public void Attributes()
        {
            var step = new IntervalStep(7, 0, StepType.Work);
            Assert.That(step.RoutineId, Is.EqualTo(7L));
            Assert.That(step.Order, Is.EqualTo(0));
            Assert.That(step.Type, Is.EqualTo(StepType.Work));
            Assert.That(step.Id, Is.Null);
            Assert.That(step.GroupId, Is.Null);
            foreach (var name in OptionalIntegers)
            {
                var property = typeof(IntervalStep).GetProperty(name)!;
                Assert.That(property.PropertyType, Is.EqualTo(typeof(int?)), name + " es un entero opcional");
                Assert.That(property.GetValue(step), Is.Null, name + " vacío por defecto");
            }

            Assert.That(typeof(IntervalStep).GetProperty("Order")!.PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(typeof(IntervalStep).GetProperty("RoutineId")!.PropertyType, Is.EqualTo(typeof(long)));
            Assert.That(typeof(IntervalStep).GetProperty("Id")!.PropertyType, Is.EqualTo(typeof(long?)));

            var constructors = new List<ConstructorInfo>(typeof(IntervalStep).GetConstructors());
            Assert.That(constructors.Count, Is.EqualTo(1));
            var parameters = constructors[0].GetParameters();
            Assert.That(parameters.Length, Is.EqualTo(3), "routineId, order y type son los obligatorios");
        }

        // AC-RUT-02-02: con duración es "por tiempo" aunque tenga distancia; sin duración y con distancia, "por distancia";
        // sin ninguna, sin meta ("?").
        [Test, Category("AC-RUT-02-02")]
        public void GoalKind()
        {
            var both = RoutineFixtures.Step(StepType.Work, dur: 90, dist: 500);
            Assert.That(both.Goal, Is.EqualTo(StepGoal.Time));
            Assert.That(both.IsTimeBased && both.IsDistanceBased, Is.True);
            var distance = RoutineFixtures.Step(StepType.Work, dist: 500);
            Assert.That(distance.Goal, Is.EqualTo(StepGoal.Distance));
            Assert.That(distance.IsTimeBased, Is.False);
            var none = RoutineFixtures.Step(StepType.Rest);
            Assert.That(none.Goal, Is.EqualTo(StepGoal.None));
            Assert.That(RoutineFixtures.Step(StepType.Work, dur: 0).Goal, Is.EqualTo(StepGoal.Time));
            foreach (var l10n in LocalizationFixture.Both)
            {
                Assert.That(StepLabels.Duration(both, l10n), Is.EqualTo("1m 30s"));
                Assert.That(StepLabels.Duration(distance, l10n), Is.EqualTo("500m"));
                Assert.That(StepLabels.Duration(none, l10n), Is.EqualTo("?"));
            }
        }

        // AC-RUT-02-03: la copia con cambios nunca borra un valor (null = conservar).
        [Test, Category("AC-RUT-02-03")]
        public void CopyWithNeverClears()
        {
            var full = new IntervalStep(1, 2, StepType.Work)
            {
                Id = 9, DurationSeconds = 60, DistanceMeters = 500, TargetWattsMin = 100, TargetWattsMax = 120, TargetSpm = 20,
                TargetSplitSeconds = 125, GroupId = "a", GroupRepeatCount = 3, ProgressionSpm = 1, ProgressionWatts = 2,
                ProgressionSplitSeconds = -1,
            };
            Assert.That(full.CopyWith(targetSpm: null).TargetSpm, Is.EqualTo(20), "SPM 20 copiado con SPM vacío sigue en 20");
            Assert.That(full.CopyWith(), Is.EqualTo(full));
            Assert.That(full.CopyWith(), Is.Not.SameAs(full));

            Assert.That(full.CopyWith(targetSpm: 24), Is.EqualTo(full with { TargetSpm = 24 }), "solo cambia lo pedido");
            Assert.That(full.CopyWith(id: 1, routineId: 2, order: 3, type: StepType.Rest, durationSeconds: 4, distanceMeters: 5,
                    targetWattsMin: 6, targetWattsMax: 7, targetSpm: 8, targetSplitSeconds: 9, groupId: "b", groupRepeatCount: 10,
                    progressionSpm: 11, progressionWatts: 12, progressionSplitSeconds: 13),
                Is.EqualTo(new IntervalStep(2, 3, StepType.Rest)
                {
                    Id = 1, DurationSeconds = 4, DistanceMeters = 5, TargetWattsMin = 6, TargetWattsMax = 7, TargetSpm = 8,
                    TargetSplitSeconds = 9, GroupId = "b", GroupRepeatCount = 10, ProgressionSpm = 11, ProgressionWatts = 12,
                    ProgressionSplitSeconds = 13,
                }));
            var empty = new IntervalStep(1, 0, StepType.Warmup);
            Assert.That(empty.CopyWith(), Is.EqualTo(empty), "lo vacío sigue vacío");
        }

        // AC-RUT-02-04 (Q-25): sin validación; 0, negativos y mínimo > máximo se aceptan y se muestran tal cual.
        [Test, Category("AC-RUT-02-04")]
        public void NoValidation()
        {
            foreach (var l10n in LocalizationFixture.Both)
            {
                Assert.That(StepLabels.Targets(RoutineFixtures.Step(StepType.Work, wMin: 0), l10n), Is.EqualTo(">0W"));
                Assert.That(StepLabels.Targets(RoutineFixtures.Step(StepType.Work, wMin: -10, wMax: 5), l10n), Is.EqualTo("-10-5W"));
                Assert.That(StepLabels.Targets(RoutineFixtures.Step(StepType.Work, spm: 0), l10n), Is.EqualTo("0 spm"));
                Assert.That(StepLabels.Targets(RoutineFixtures.Step(StepType.Work, wMin: 200, wMax: 100), l10n), Is.EqualTo("200-100W"), "mínimo > máximo");
                Assert.That(StepLabels.Duration(RoutineFixtures.Step(StepType.Work, dur: -30), l10n), Is.EqualTo("30s"));
            }

            Assert.DoesNotThrow(() => new IntervalStep(-1, -5, StepType.Rest) { DurationSeconds = -1, TargetSpm = -3, GroupRepeatCount = -2 });
        }
    }
}
