using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // QA-G10a-12 (AC-WKT-03-11, DV-103): el paso por distancia Y los parciales a la vez, con el motor de verdad. Un monitor que manda
    // la distancia cada 2 s (+11 m) con una lectura por segundo: con Distancia 100 el paso termina en el segundo 20 y los metros del
    // paso del tracker (los de los parciales) son los mismos que decidieron el fin (un solo saneador por sesión).
    [TestFixture]
    public sealed class MonitorCadenceStepTests
    {
        [Test, Category("AC-WKT-03-11"), Category("AC-WKT-07-09")]
        public void DistanceEveryTwoSecondsEndsTheStepAt20()
        {
            using (var h = new WorkoutHarness(k => RowerScripts.R4(k).CopyWith(distanceMeters: 1000 + 11 * (k / 2))))
            {
                h.Routine(RoutineSeeds.DistanciaCien());
                h.At(19);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(0), "99 m en el segundo 18 y 19");
                Assert.That(h.Engine.Series.State.StepMeters, Is.EqualTo(99), "los parciales ven los mismos metros");
                Assert.That(h.Engine.Series.State.CurrentLapMeters, Is.EqualTo(99));
                h.At(20);
                Assert.That(h.State.Step!.Value.StepIndex, Is.EqualTo(1), "110 m: el paso terminó en el segundo 20");
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(20));
            }
        }
    }
}
