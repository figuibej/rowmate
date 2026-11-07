using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Workout;
using RowMate.Tests.EditMode.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SDI · Nada de la palada cambia los datos (AC-SCN-08-11): con el motor real (R-4) los puntos, los metros, el tiempo y las
    // vueltas son idénticos con y sin una escena que lee cada foto a 60 fps; la distancia de escena es la de F-SCN-03 con o sin el
    // remero. Los 0 B por cuadro de Tick (ya con el remero) los mide SceneSessionTests.TickDoesNotAllocate (solo apoyo; AC-SCN-01-16
    // se da por bueno con FrameAllocations y el Profiler del build, QA7-G-09).
    [TestFixture]
    public sealed class SceneDataIsolationTests
    {
        [Test, Category("AC-SCN-08-11")]
        public void TheSessionIsTheSameWithAndWithoutTheScene()
        {
            string withScene;
            string withoutScene;
            WorkoutSnapshot a;
            WorkoutSnapshot b;
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                var scene = new StrokeTestSession();
                var t = 0.0;
                while (t < 200)
                {
                    t += 1.0 / 60;
                    h.At(t);
                    scene.Step(SceneInputs.From(h.State));
                }

                withScene = WorkoutHarness.Describe(h.Points());
                a = h.State;
            }

            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                var t = 0.0;
                while (t < 200)
                {
                    t += 1.0 / 60;
                    h.At(t);
                }

                withoutScene = WorkoutHarness.Describe(h.Points());
                b = h.State;
            }

            Assert.That(withScene, Is.EqualTo(withoutScene));
            Assert.That(a.TotalElapsedSeconds, Is.EqualTo(b.TotalElapsedSeconds));
            Assert.That(a.Data, Is.EqualTo(b.Data));
            Assert.That(a.Series!.Laps.Count, Is.EqualTo(b.Series!.Laps.Count));
            Assert.That(a.Series.StepMeters, Is.EqualTo(b.Series.StepMeters));
        }

        [Test, Category("AC-SCN-08-11")]
        public void TheSceneDistanceIsTheContractOneWithTheRower()
        {
            var s = new StrokeTestSession();
            var bare = new SceneState();
            for (var i = 0; i < 1200; i++)
            {
                var w = i < 900 ? StrokeTestSession.Rowing(30, 280, 105) : StrokeTestSession.Pause();
                s.Step(w);
                bare.Update(s.LastDt, w.Pace500m, w.Spm, w.IsActive);
                Assert.That(s.Frame.Distance, Is.EqualTo(bare.Distance));
                Assert.That(s.Session.State.StrokePhase, Is.EqualTo(bare.StrokePhase));
            }
        }
    }
}
