using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;

namespace RowMate.Tests.EditMode.Workout
{
    // TLT: el semáforo de las tarjetas (F-WKT-10; AC-WKT-10-01 a -04, -06; lib/features/workout/immersive_workout_screen.dart
    // :579-602): instantáneo, sin promedios ni margen.
    [TestFixture]
    public sealed class TargetLightTests
    {
        [Test, Category("AC-WKT-10-01")]
        public void OnlyCardsWithTheirTarget()
        {
            var data = new RowingData { StrokeRate = 24, PowerWatts = 150, Pace500mSeconds = 133 };
            var free = TargetLight.Evaluate(null, data);
            Assert.That(free.Spm, Is.EqualTo(TargetState.None), "libre: sin paso");
            Assert.That(free.Split, Is.EqualTo(TargetState.None));
            Assert.That(free.Watts, Is.EqualTo(TargetState.None));
            var spmOnly = TargetLight.Evaluate(Step(spm: 22), data);
            Assert.That(spmOnly.Spm, Is.EqualTo(TargetState.Met));
            Assert.That(spmOnly.Split, Is.EqualTo(TargetState.None));
            Assert.That(spmOnly.Watts, Is.EqualTo(TargetState.None));
            var wattsMaxOnly = TargetLight.Evaluate(Step() with { TargetWattsMax = 100 }, data);
            Assert.That(wattsMaxOnly.Watts, Is.EqualTo(TargetState.None), "solo el máximo: sin semáforo");
        }

        [Test, Category("AC-WKT-10-02")]
        public void Spm()
        {
            Assert.That(TargetLight.Evaluate(Step(spm: 24), new RowingData { StrokeRate = 24.0 }).Spm, Is.EqualTo(TargetState.Met), "24.0 contra 24");
            Assert.That(TargetLight.Evaluate(Step(spm: 24), new RowingData { StrokeRate = 23.9 }).Spm, Is.EqualTo(TargetState.Off), "23.9");
            Assert.That(TargetLight.Evaluate(Step(spm: 24), new RowingData { StrokeRate = 30 }).Spm, Is.EqualTo(TargetState.Met));
        }

        [Test, Category("AC-WKT-10-03")]
        public void WattsIgnoresTheMax()
        {
            var range = Step(wMin: 150) with { TargetWattsMax = 180 };
            Assert.That(TargetLight.Evaluate(range, new RowingData { PowerWatts = 190 }).Watts, Is.EqualTo(TargetState.Met), "190 contra 150-180");
            Assert.That(TargetLight.Evaluate(range, new RowingData { PowerWatts = 149 }).Watts, Is.EqualTo(TargetState.Off));
            Assert.That(TargetLight.Evaluate(range, new RowingData { PowerWatts = 150 }).Watts, Is.EqualTo(TargetState.Met));
        }

        [Test, Category("AC-WKT-10-04")]
        public void SplitAndNonPositiveTargets()
        {
            // val > target, sin casos especiales (QA-G4a-P01, QA-G4aT-08): --:-- contra 130, 0 y −5 → cumple, cumple y fuera;
            // 125 contra 0 → fuera.
            Assert.That(Split(0, 130), Is.EqualTo(TargetState.Met), "--:-- contra 130");
            Assert.That(Split(0, 0), Is.EqualTo(TargetState.Met), "--:-- contra 0");
            Assert.That(Split(0, -5), Is.EqualTo(TargetState.Off), "--:-- contra −5");
            Assert.That(Split(125, 0), Is.EqualTo(TargetState.Off), "125 contra 0");
            Assert.That(Split(125, 130), Is.EqualTo(TargetState.Met), "más rápido");
            Assert.That(Split(130, 130), Is.EqualTo(TargetState.Met), "igual");
            Assert.That(Split(131, 130), Is.EqualTo(TargetState.Off), "más lento");
        }

        [Test, Category("AC-WKT-10-06")]
        public void EvaluatedWithEachReadingWithoutTolerance()
        {
            // En el instante, con la lectura misma: un decimal por debajo ya está fuera.
            var step = Step(spm: 22, wMin: 160);
            Assert.That(TargetLight.Evaluate(step, new RowingData { StrokeRate = 22, PowerWatts = 160 }).Watts, Is.EqualTo(TargetState.Met));
            Assert.That(TargetLight.Evaluate(step, new RowingData { StrokeRate = 21.5, PowerWatts = 159 }).Spm, Is.EqualTo(TargetState.Off));
            Assert.That(TargetLight.Evaluate(step, new RowingData { StrokeRate = 21.5, PowerWatts = 159 }).Watts, Is.EqualTo(TargetState.Off));
            // Parado (AC-WKT-10-07, la regla): SPM y Watts fuera; Split --:-- cumple.
            var stopped = TargetLight.Evaluate(Step(spm: 22, wMin: 160, split: 130), new RowingData());
            Assert.That(stopped.Spm, Is.EqualTo(TargetState.Off));
            Assert.That(stopped.Watts, Is.EqualTo(TargetState.Off));
            Assert.That(stopped.Split, Is.EqualTo(TargetState.Met));
        }

        private static TargetState Split(int value, int target)
        {
            return TargetLight.Evaluate(Step(split: target), new RowingData { Pace500mSeconds = value }).Split;
        }

        private static IntervalStep Step(int? spm = null, int? wMin = null, int? split = null)
        {
            return new IntervalStep(1, 0, StepType.Work)
            {
                DurationSeconds = 60,
                TargetSpm = spm,
                TargetWattsMin = wMin,
                TargetSplitSeconds = split,
            };
        }
    }
}
