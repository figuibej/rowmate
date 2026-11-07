using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Stroke;
using RowMate.Tests.EditMode.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // BYT · La boya de 500 m (P-06; AC-SCN-08-10; DT7-11): con el motor real de dotnet (WorkoutHarness) y R-4, Series 600 (F-5) pasa a
    // la altura del remero (relZ 9,5) cuando el panel cierra la vuelta 1 de la rep 1 (155 s ± 1); sin saltos; nada en el libre ni en
    // el descanso; la vuelta que cierra al terminar el paso también tiene su boya.
    [TestFixture]
    public sealed class BuoyTrackerTests
    {
        private sealed class Ride
        {
            public readonly WorkoutHarness Harness;
            public readonly StrokeTestSession Scene = new StrokeTestSession();
            public double T;

            public Ride(WorkoutHarness harness)
            {
                Harness = harness;
            }

            public BuoyTracker Buoy => Scene.Rower.Buoy;

            public double RelZ => Buoy == null ? double.NaN : Buoy.WorldZ - Scene.Frame.Distance;

            public void Step()
            {
                T += 1.0 / 60;
                Harness.At(T);
                Scene.Step(SceneInputs.From(Harness.State));
            }
        }

        [Test, Category("AC-SCN-08-10")]
        public void InSeries600TheBuoyPassesTheRowerWhenTheLapCloses()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                var ride = new Ride(h);
                var crossed = double.NaN;
                var lapClosed = double.NaN;
                var previousZ = double.NaN;
                var previousFixed = false;
                var wasVisible = false;
                while (ride.T < 165)
                {
                    var before = ride.RelZ;
                    ride.Step();
                    var buoy = ride.Buoy;
                    if (double.IsNaN(lapClosed) && h.State.Series != null && h.State.Series.Laps.Count == 1)
                    {
                        lapClosed = ride.T;
                    }

                    if (buoy.Visible && double.IsNaN(crossed) && before > SceneCamera.BoatZ && ride.RelZ <= SceneCamera.BoatZ)
                    {
                        crossed = ride.T;
                    }

                    if (buoy.Visible && wasVisible && !double.IsNaN(previousZ) && !buoy.Fixed && !previousFixed)
                    {
                        Assert.That(Math.Abs(buoy.WorldZ - previousZ), Is.LessThanOrEqualTo(0.5 / 60 + 1e-9), "sin saltos en " + ride.T);
                    }

                    if (buoy.Fixed && previousFixed)
                    {
                        Assert.That(buoy.WorldZ, Is.EqualTo(previousZ), "fija en el mundo");
                    }

                    wasVisible = buoy.Visible;
                    previousZ = buoy.WorldZ;
                    previousFixed = buoy.Fixed;
                }

                Assert.That(lapClosed, Is.EqualTo(155).Within(1.5), "el panel cerró #1");
                Assert.That(crossed, Is.EqualTo(155).Within(1), "la boya pasa a la altura del remero a los 155 s ± 1");
                Assert.That(Math.Abs(crossed - lapClosed), Is.LessThanOrEqualTo(1));
            }
        }

        [Test, Category("AC-SCN-08-10")]
        public void ItAppearsFromFarAndIsAtPlusFourAndAHalf()
        {
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                var ride = new Ride(h);
                while (ride.T < 32)
                {
                    ride.Step();
                }

                Assert.That(h.State.Series!.IsWorkStep, Is.True);
                Assert.That(ride.RelZ, Is.GreaterThan(400), "a 500 m todavía no se ve");
                Assert.That(ride.Buoy.Alpha, Is.EqualTo(0));
                while (ride.RelZ > 360)
                {
                    ride.Step();
                }

                Assert.That(ride.Buoy.Alpha, Is.EqualTo(1).Within(0.01), "aparece con el tramado de la orilla (400 → 360)");
                Assert.That(BuoyTracker.X, Is.EqualTo(4.5));
            }
        }

        [Test, Category("AC-SCN-08-10")]
        public void NoBuoyInFreeWarmUpOrRest()
        {
            using (var h = new WorkoutHarness())
            {
                h.Free();
                var ride = new Ride(h);
                while (ride.T < 40)
                {
                    ride.Step();
                    Assert.That(ride.Buoy.Visible, Is.False, "libre");
                }
            }

            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                var ride = new Ride(h);
                while (ride.T < 29)
                {
                    ride.Step();
                    Assert.That(ride.Buoy.Visible, Is.False, "calentamiento");
                }

                // El trabajo de 600 m termina a los 180 s: la boya de los 1000 m (a ≈ 409 m) se funde en el descanso.
                while (ride.T < 183)
                {
                    ride.Step();
                }

                Assert.That(h.State.Series!.IsWorkStep, Is.False, "descanso");
                Assert.That(ride.Buoy.Visible && !ride.Buoy.Fixed, Is.False, "en el descanso no hay boya en camino");
            }
        }

        [Test, Category("AC-SCN-08-10")]
        public void ALapThatClosesAtTheEndOfTheStepAlsoHasItsBuoy()
        {
            // Trabajo 1000 con R-4: la vuelta 2 cierra a los 1000 m, justo cuando termina el paso (250 s).
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.TrabajoMil());
                var ride = new Ride(h);
                var fixedNearRower = false;
                while (ride.T < 256)
                {
                    ride.Step();
                    if (ride.T > 245 && ride.Buoy.Fixed && Math.Abs(ride.RelZ - SceneCamera.BoatZ) < 25)
                    {
                        fixedNearRower = true;
                    }
                }

                Assert.That(fixedNearRower, Is.True, "la boya de los 1000 m queda fija al lado del remero");
            }
        }

        [Test, Category("AC-SCN-08-10")]
        public void TheTrackerByItself()
        {
            var buoy = new BuoyTracker();
            buoy.Step(1.0 / 60, new BuoyInputs(true, 1, 0, 100, 1), 1000, 4, 0);
            Assert.That(buoy.WorldZ, Is.EqualTo(1000 + 9.5 + 400).Within(1e-9), "D + 9,5 + (500 − d_paso)");
            Assert.That(buoy.Visible, Is.False, "a 409,5 m todavía no");
            buoy.Step(1.0 / 60, new BuoyInputs(true, 1, 0, 100, 1), 1000 + 4.0 / 60, 4, 0);
            Assert.That(buoy.WorldZ, Is.EqualTo(1000 + 9.5 + 400).Within(1e-6), "el objetivo queda fijo en el mundo si la escena avanza como el paso");
            buoy.Reset();
            Assert.That(buoy.Visible, Is.False);
        }
    }
}
