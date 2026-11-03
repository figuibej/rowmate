using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // STR: el SeriesTracker (05-series-parciales/02-tech-design §2.2; F-WKT-07, F-WKT-08), puerto de
    // lib/features/workout/series_tracker.dart. Los 12 casos de test/series_tracker_test.dart (LT-47 a LT-58) con sus
    // afirmaciones una por una y en su orden, con los alimentadores del legacy (_Feeder, :20-42; _Raw, :133-152); y los casos de
    // la app (spec §4.1, §4.2) con el motor de verdad y los remos de prueba (R-4, R-3, R-4A, R-4R, R-4Z, R-4D, R-4P).
    [TestFixture]
    public sealed class SeriesTrackerTests
    {
        private const string G = "g1";

        // ── Los 12 casos del legacy ──

        // legacy: test/series_tracker_test.dart:46 'interpola el tiempo de la vuelta entre lecturas'
        [TestCase(TestName = "interpola el tiempo de la vuelta entre lecturas", Category = "LT-47,AC-WKT-07-04,AC-WKT-07-05")]
        public void InterpolatesTheLapTimeBetweenReadings()
        {
            var f = new Feeder(new SeriesTracker());
            // 3 m/s: 498 m en t=166 y 501 m en t=167 → cruza 500 m en 166.67 s
            f.Run(0, Work(1000), Pos(1), 170, 3);
            var t = f.Tracker.State;
            Assert.That(t.Laps.Count, Is.EqualTo(1));
            Assert.That(t.Laps[0].Number, Is.EqualTo(1));
            Assert.That(t.Laps[0].Seconds, Is.EqualTo(166.67).Within(0.01));
            Assert.That(t.CurrentLapMeters, Is.EqualTo(10), "510 - 500");
            Assert.That(t.CurrentLapSeconds, Is.EqualTo(3.33).Within(0.01));
            Assert.That(t.IsWorkStep, Is.True);
        }

        // legacy: test/series_tracker_test.dart:59 'dos vueltas dentro del mismo paso'
        [TestCase(TestName = "dos vueltas dentro del mismo paso", Category = "LT-48,AC-WKT-07-02,AC-WKT-07-04")]
        public void TwoLapsWithinTheSameStep()
        {
            var f = new Feeder(new SeriesTracker());
            f.Run(0, Work(2000), Pos(1), 220, 5);
            Assert.That(Seconds(f.Tracker.State.Laps), Is.EqualTo(new[] { 100.0, 100.0 }));
        }

        // legacy: test/series_tracker_test.dart:65 'un paso de trabajo de menos de 500 m no genera vueltas'
        [TestCase(TestName = "un paso de trabajo de menos de 500 m no genera vueltas", Category = "LT-49,AC-WKT-07-02")]
        public void AWorkStepUnderFiveHundredMetersHasNoLaps()
        {
            var f = new Feeder(new SeriesTracker());
            f.Run(0, Work(400), Pos(1), 80, 5);
            Assert.That(f.Tracker.State.Laps, Is.Empty);
        }

        // legacy: test/series_tracker_test.dart:71 'el enfriamiento no genera vueltas pero suma metros a la repetición'
        [TestCase(TestName = "el enfriamiento no genera vueltas pero suma metros a la repetición",
            Category = "LT-50,AC-WKT-07-01,AC-WKT-07-05,AC-WKT-08-01,AC-WKT-08-02")]
        public void CooldownHasNoLapsButAddsToTheRep()
        {
            var f = new Feeder(new SeriesTracker());
            f.Run(0, Work(500), Pos(1), 100, 5);
            f.Run(1, Cool(60), Pos(1), 60, 2);
            var t = f.Tracker.State;
            Assert.That(t.IsWorkStep, Is.False);
            Assert.That(t.Laps, Is.Empty);
            Assert.That(t.CurrentLapMeters, Is.EqualTo(0));
            Assert.That(t.InSeries, Is.True);
            Assert.That(t.Rep, Is.EqualTo(1));
            Assert.That(t.RepCount, Is.EqualTo(5));
            Assert.That(t.RepMeters, Is.EqualTo(620));
        }

        // legacy: test/series_tracker_test.dart:85 'al cambiar de repetición guarda el resumen de la anterior'
        [TestCase(TestName = "al cambiar de repetición guarda el resumen de la anterior", Category = "LT-51,AC-WKT-08-03,AC-WKT-08-04,AC-WKT-08-06")]
        public void ChangingRepKeepsTheSummaryOfThePreviousOne()
        {
            var f = new Feeder(new SeriesTracker());
            f.Run(0, Work(500), Pos(1), 100, 5);
            f.Run(1, Cool(60), Pos(1), 60, 2);
            f.Run(2, Work(500), Pos(2), 10, 5);
            var t = f.Tracker.State;
            Assert.That(t.Rep, Is.EqualTo(2));
            Assert.That(t.RepMeters, Is.EqualTo(50));
            Assert.That(t.PreviousReps.Count, Is.EqualTo(1));
            var r = t.PreviousReps[0];
            Assert.That(r.Rep, Is.EqualTo(1));
            Assert.That(r.TotalMeters, Is.EqualTo(620));
            Assert.That(r.WorkMeters, Is.EqualTo(500));
            Assert.That(r.WorkSeconds, Is.EqualTo(100));
            Assert.That(r.WorkSplitSeconds, Is.EqualTo(100).Within(0.001));
        }

        // legacy: test/series_tracker_test.dart:102 'guarda solo las últimas 3 repeticiones, la más reciente primero'
        [TestCase(TestName = "guarda solo las últimas 3 repeticiones, la más reciente primero", Category = "LT-52,AC-WKT-08-04")]
        public void KeepsOnlyTheLastThreeRepsMostRecentFirst()
        {
            var f = new Feeder(new SeriesTracker());
            for (var rep = 1; rep <= 5; rep++)
            {
                f.Run((rep - 1) * 2, Work(500), Pos(rep), 100, 5);
                f.Run((rep - 1) * 2 + 1, Cool(10), Pos(rep), 10, 2);
            }

            Assert.That(f.Tracker.State.Rep, Is.EqualTo(5));
            Assert.That(RepNumbers(f.Tracker.State.PreviousReps), Is.EqualTo(new[] { 4, 3, 2 }));
        }

        // legacy: test/series_tracker_test.dart:112 'una serie nueva o un paso suelto vacían la comparación'
        [TestCase(TestName = "una serie nueva o un paso suelto vacían la comparación", Category = "LT-53,AC-WKT-08-05")]
        public void ANewSeriesOrALooseStepClearsTheComparison()
        {
            var f = new Feeder(new SeriesTracker());
            f.Run(0, Work(500), Pos(1), 100, 5);
            f.Run(1, Work(500), Pos(2), 10, 5);
            Assert.That(f.Tracker.State.PreviousReps.Count, Is.EqualTo(1));

            f.Run(2, Work(500, "g2"), Pos(1, "g2"), 10, 5);
            Assert.That(f.Tracker.State.PreviousReps, Is.Empty);

            f.Run(3, Cool(60, null), Pos(1, null), 10, 2);
            Assert.That(f.Tracker.State.InSeries, Is.False);
            Assert.That(f.Tracker.State.PreviousReps, Is.Empty);
        }

        // legacy: test/series_tracker_test.dart:126 'una repetición sin metros de trabajo no tiene split'
        [TestCase(TestName = "una repetición sin metros de trabajo no tiene split", Category = "LT-54,AC-WKT-08-06")]
        public void ARepWithoutWorkMetersHasNoSplit()
        {
            var r = new RepSummary(1, 100, 0, 0);
            Assert.That(r.WorkSplitSeconds, Is.Null);
        }

        // legacy: test/series_tracker_test.dart:155 'una lectura de 0 dentro del paso no genera metros negativos ni dobles'
        [TestCase(TestName = "una lectura de 0 dentro del paso no genera metros negativos ni dobles", Category = "LT-55,AC-WKT-07-06,AC-WKT-08-04")]
        public void AZeroReadingInsideTheStepMakesNoNegativeNorDoubleMeters()
        {
            var r = new Raw(new SeriesTracker(), Work(2000), Pos(1));
            for (var i = 1; i <= 250; i++)
            {
                r.Feed(i * 4);
                Assert.That(r.Tracker.State.CurrentLapMeters, Is.GreaterThanOrEqualTo(0));
                Assert.That(r.Tracker.State.RepMeters, Is.GreaterThanOrEqualTo(0));
            }

            r.Feed(0);
            Assert.That(r.Tracker.State.RepMeters, Is.GreaterThanOrEqualTo(0));
            for (var raw = 1004; raw <= 1100; raw += 4)
            {
                r.Feed(raw);
                Assert.That(r.Tracker.State.CurrentLapMeters, Is.GreaterThanOrEqualTo(0));
                Assert.That(r.Tracker.State.RepMeters, Is.GreaterThanOrEqualTo(0));
            }

            var laps = r.Tracker.State.Laps;
            Assert.That(laps.Count, Is.EqualTo(2));
            foreach (var l in laps)
            {
                Assert.That(l.Seconds, Is.EqualTo(125).Within(2));
            }

            r.Position = Pos(2);
            r.Feed(1104, 1);
            var s = r.Tracker.State.PreviousReps[0];
            Assert.That(s.WorkMeters, Is.InRange(1060, 1110));
        }

        // legacy: test/series_tracker_test.dart:180 'un primer salto desde 0 no crea vueltas falsas'
        [TestCase(TestName = "un primer salto desde 0 no crea vueltas falsas", Category = "LT-56,AC-WKT-07-06,AC-WKT-07-08")]
        public void AFirstJumpFromZeroMakesNoFakeLaps()
        {
            var r = new Raw(new SeriesTracker(), Work(2000), Pos(1));
            r.Feed(5000);
            Assert.That(r.Tracker.State.Laps, Is.Empty);
            for (var i = 1; i <= 140; i++)
            {
                r.Feed(5000 + i * 4);
            }

            Assert.That(r.Tracker.State.Laps.Count, Is.EqualTo(1));
            Assert.That(r.Tracker.State.Laps[0].Seconds, Is.EqualTo(125).Within(2));
        }

        // legacy: test/series_tracker_test.dart:191 'un reinicio del monitor no genera valores negativos'
        [TestCase(TestName = "un reinicio del monitor no genera valores negativos", Category = "LT-57,AC-WKT-07-06,AC-WKT-08-02")]
        public void AMonitorResetMakesNoNegativeValues()
        {
            var r = new Raw(new SeriesTracker(), Work(2000), Pos(1));
            var prev = 0;

            void Check()
            {
                Assert.That(r.Tracker.State.RepMeters, Is.GreaterThanOrEqualTo(prev));
                Assert.That(r.Tracker.State.CurrentLapMeters, Is.GreaterThanOrEqualTo(0));
                prev = r.Tracker.State.RepMeters;
            }

            for (var i = 1; i <= 200; i++)
            {
                r.Feed(i * 4);
                Check();
            }

            for (var i = 0; i <= 60; i++)
            {
                r.Feed(i * 4);
                Check();
            }

            Assert.That(r.Tracker.State.Laps, Is.Not.Empty);
            Assert.That(r.Tracker.State.Laps[0].Seconds, Is.EqualTo(125).Within(2));
        }

        // legacy: test/series_tracker_test.dart:211 'rebase no cuenta el salto al reanudar'
        [TestCase(TestName = "rebase no cuenta el salto al reanudar", Category = "LT-58,AC-WKT-07-07")]
        public void RebaseDoesNotCountTheJumpOnResume()
        {
            var r = new Raw(new SeriesTracker(), Work(2000), Pos(1));
            for (var i = 1; i <= 5; i++)
            {
                r.Feed(i * 4);
            }

            Assert.That(r.Tracker.State.RepMeters, Is.EqualTo(20));
            r.Tracker.Rebase(9000);
            r.Feed(9004);
            Assert.That(r.Tracker.State.RepMeters, Is.EqualTo(24));
        }

        // ── La foto ──

        [Test, Category("AC-WKT-07-11")]
        public void TheStateIsImmutableCopyOnWriteAndBumpsItsRevisionOnlyWhenItChanges()
        {
            // 02-tech-design §2.2: la foto no cambia después de publicada; las listas se reemplazan al cerrar una vuelta o al cambiar
            // de repetición (copia al escribir), y entre dos lecturas que no cierran nada la lista es la misma instancia.
            var f = new Feeder(new SeriesTracker());
            Assert.That(f.Tracker.State.StepIndex, Is.Null, "antes de la primera lectura (DV-48 la mira)");
            f.Run(0, Work(1000), Pos(1), 100, 5);
            var lapOne = f.Tracker.State;
            Assert.That(lapOne.Laps.Count, Is.EqualTo(1));
            var revision = lapOne.Revision;
            f.Run(0, Work(1000), Pos(1), 1, 5);
            var after = f.Tracker.State;
            Assert.That(after, Is.Not.SameAs(lapOne));
            Assert.That(after.Revision, Is.EqualTo(revision + 1));
            Assert.That(after.Laps, Is.SameAs(lapOne.Laps), "sin vuelta nueva, la misma lista");
            Assert.That(lapOne.CurrentLapMeters, Is.EqualTo(0), "la foto vieja no cambió");
            Assert.That(after.CurrentLapMeters, Is.EqualTo(5));
            f.Tracker.Rebase(505);
            Assert.That(f.Tracker.State, Is.SameAs(after), "Rebase no cambia la foto");
            f.Run(0, Work(1000), Pos(1), 99, 5);
            Assert.That(f.Tracker.State.Laps, Is.Not.SameAs(lapOne.Laps));
            Assert.That(lapOne.Laps.Count, Is.EqualTo(1), "la lista vieja quedó como estaba");
            Assert.That(f.Tracker.State.StepMeters, Is.EqualTo(f.Tracker.Meters), "un solo paso: los metros del paso son los de la sesión");
        }

        [Test, Category("AC-WKT-07-11")]
        public void BeginResetsEverything()
        {
            // :86-102: reinicia todo (al iniciar cada rutina y al volver a reposo).
            var f = new Feeder(new SeriesTracker());
            f.Run(0, Work(500), Pos(1), 100, 5);
            f.Run(1, Work(500), Pos(2), 10, 5);
            Assert.That(f.Tracker.State.PreviousReps.Count, Is.EqualTo(1));
            f.Tracker.Begin(777);
            var s = f.Tracker.State;
            Assert.That(s.StepIndex, Is.Null);
            Assert.That(s.Laps, Is.Empty);
            Assert.That(s.PreviousReps, Is.Empty);
            Assert.That(s.InSeries, Is.False);
            Assert.That(s.Rep, Is.EqualTo(0));
            Assert.That(s.RepMeters, Is.EqualTo(0));
            Assert.That(f.Tracker.Meters, Is.EqualTo(0));
        }

        // ── Los casos de la app (spec §4.1, §4.2), con el motor y los remos de prueba ──

        [TestCase("R-4", 250, 125.0, 125.0, TestName = "Trabajo 1000 con R-4: #1 2:05.0 a los 125 y #2 2:05.0 a los 250")]
        [TestCase("R-4R", 251, 126.0, 125.0, TestName = "Trabajo 1000 con R-4R: #1 2:06.0 y #2 2:05.0, termina en 251")]
        [TestCase("R-4Z", 252, 127.0, 125.0, TestName = "Trabajo 1000 con R-4Z: #1 2:07.0 y #2 2:05.0, termina en 252")]
        [TestCase("R-4D", 254, 129.0, 125.0, TestName = "Trabajo 1000 con R-4D: #1 2:09.0 y #2 2:05.0, termina en 254")]
        [Category("AC-WKT-07-06"), Category("AC-WKT-07-09"), Category("AC-WKT-07-02")]
        public void TrabajoMilWithFailingMonitors(string rower, int end, double first, double second)
        {
            // AC-WKT-07-06 y -09: los saltos no suman y el paso por distancia termina en el mismo segundo en que cierra su vuelta 2.
            using (var h = new WorkoutHarness(RowerScripts.ByName(rower)))
            {
                h.Routine(RoutineSeeds.TrabajoMil());
                var laps = new List<(int Second, Lap Lap)>();
                var seen = 0;
                using (h.Engine.State.Subscribe(s =>
                       {
                           var series = s.Series;
                           if (series != null && series.Laps.Count > seen)
                           {
                               laps.Add((s.TotalElapsedSeconds, series.Laps[series.Laps.Count - 1]));
                               seen = series.Laps.Count;
                           }
                       }, false))
                {
                    h.At(end + 1);
                }

                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(end), "el fin del paso por distancia");
                Assert.That(laps.Count, Is.EqualTo(2));
                Assert.That(laps[0].Lap.Seconds, Is.EqualTo(first).Within(1e-9));
                Assert.That(laps[1].Lap.Seconds, Is.EqualTo(second).Within(1e-9));
                Assert.That(laps[1].Second, Is.EqualTo(end), "la vuelta 2 cierra en el segundo en que termina el paso");
                Assert.That(WorkoutFormats.LapTime(laps[0].Lap.Seconds), Is.EqualTo(WorkoutFormats.LapTime(first)));
            }
        }

        [Test, Category("AC-WKT-07-04"), Category("AC-WKT-07-05"), Category("AC-WKT-18-04")]
        public void TrabajoMilWithRThreeInterpolates()
        {
            // A 3 m/s, con 498 m en el segundo 166 y 501 m en el 167, la vuelta da 166,67 s: 2:46.7; a los 170, 10 m y 3,33 s.
            using (var h = new WorkoutHarness(k => RowerScripts.R3(k)))
            {
                h.Routine(RoutineSeeds.TrabajoMil());
                h.At(170.25);
                var s = h.Engine.Series.State;
                Assert.That(s.Laps.Count, Is.EqualTo(1));
                Assert.That(s.Laps[0].Seconds, Is.EqualTo(166.6666666).Within(0.0001));
                Assert.That(WorkoutFormats.LapTime(s.Laps[0].Seconds), Is.EqualTo("2:46.7"));
                Assert.That(s.CurrentLapMeters, Is.EqualTo(10));
                Assert.That(WorkoutFormats.LapTime(s.CurrentLapSeconds), Is.EqualTo("0:03.3"));
            }
        }

        [Test, Category("AC-WKT-07-05"), Category("AC-WKT-08-02"), Category("AC-WKT-07-01")]
        public void SeriesSeiscientosCurrentLapAndRepMeters()
        {
            // AC-WKT-07-05: a los 170 s, 60 m y 15,0 s; a los 180, 100 m y 25,0 s (el último segundo del trabajo). AC-WKT-08-02:
            // 600 al terminar el trabajo de la rep 1 y 680 al terminar su descanso. AC-WKT-07-01: fuera de un trabajo, 0 m y 0 s.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(10.25);
                Assert.That(h.Engine.Series.State.IsWorkStep, Is.False, "calentamiento");
                Assert.That(h.Engine.Series.State.CurrentLapMeters, Is.EqualTo(0));
                Assert.That(h.Engine.Series.State.CurrentLapSeconds, Is.EqualTo(0));
                Assert.That(h.Engine.Series.State.InSeries, Is.False);
                h.At(170.25);
                var s = h.Engine.Series.State;
                Assert.That(s.CurrentLapMeters, Is.EqualTo(60));
                Assert.That(s.CurrentLapSeconds, Is.EqualTo(15.0));
                Assert.That(s.RepMeters, Is.EqualTo(560));
                Assert.That(s.Laps.Count, Is.EqualTo(1));
                Assert.That(s.Laps[0].Seconds, Is.EqualTo(125.0));
                h.At(180.25);
                s = h.Engine.Series.State;
                Assert.That(s.CurrentLapMeters, Is.EqualTo(100));
                Assert.That(s.CurrentLapSeconds, Is.EqualTo(25.0));
                Assert.That(s.RepMeters, Is.EqualTo(600));
                Assert.That(s.IsWorkStep, Is.True, "en el segundo en que termina, todavía el trabajo (P-01)");
                h.At(190.25);
                s = h.Engine.Series.State;
                Assert.That(s.IsWorkStep, Is.False);
                Assert.That(s.CurrentLapMeters, Is.EqualTo(0));
                Assert.That(s.CurrentLapSeconds, Is.EqualTo(0));
                Assert.That(s.RepMeters, Is.EqualTo(640));
                h.At(200.25);
                Assert.That(h.Engine.Series.State.RepMeters, Is.EqualTo(680));
            }
        }

        [Test, Category("AC-WKT-08-08"), Category("AC-WKT-08-04"), Category("AC-WKT-08-07"), Category("AC-WKT-08-03")]
        public void SeriesSeiscientosReps()
        {
            // AC-WKT-08-08: a los 201 s, Rep 1 con 680 m, 600 de trabajo, 150 s y 2:05; a los 371, [Rep 2, Rep 1]; a los 541 (el
            // enfriamiento, suelto), fuera de la serie y con la lista vacía (AC-WKT-08-07: la última nunca entra).
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(200.25);
                Assert.That(h.Engine.Series.State.PreviousReps, Is.Empty, "recién en el segundo siguiente (P-01)");
                h.At(201.25);
                var s = h.Engine.Series.State;
                Assert.That(s.Rep, Is.EqualTo(2));
                Assert.That(s.PreviousReps.Count, Is.EqualTo(1));
                AssertRep(s.PreviousReps[0], 1, 680, 600, 150, "2:05");
                h.At(371.25);
                s = h.Engine.Series.State;
                Assert.That(RepNumbers(s.PreviousReps), Is.EqualTo(new[] { 2, 1 }));
                AssertRep(s.PreviousReps[0], 2, 680, 600, 150, "2:05");
                h.At(540.25);
                Assert.That(h.Engine.Series.State.InSeries, Is.True);
                Assert.That(h.Engine.Series.State.Rep, Is.EqualTo(3));
                h.At(541.25);
                s = h.Engine.Series.State;
                Assert.That(s.InSeries, Is.False);
                Assert.That(s.PreviousReps, Is.Empty);
                h.At(571);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.Engine.Series.State.PreviousReps, Is.Empty, "la última repetición nunca entra");
            }
        }

        [Test, Category("AC-WKT-08-04"), Category("AC-WKT-08-08")]
        public void CincoRepsKeepsTheLastThree()
        {
            // AC-WKT-08-04: Cinco reps con R-4, a los 541 s: reps 4, 3 y 2, cada una con 540 m y 2:05.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.CincoReps());
                h.At(541.25);
                var s = h.Engine.Series.State;
                Assert.That(s.Rep, Is.EqualTo(5));
                Assert.That(RepNumbers(s.PreviousReps), Is.EqualTo(new[] { 4, 3, 2 }));
                foreach (var rep in s.PreviousReps)
                {
                    Assert.That(rep.TotalMeters, Is.EqualTo(540));
                    Assert.That(WorkoutFormats.RepSplit(rep.WorkSplitSeconds), Is.EqualTo("2:05"));
                }
            }
        }

        [Test, Category("AC-WKT-08-05"), Category("AC-WKT-08-01"), Category("AC-WKT-08-03")]
        public void DosSeriesClearsTheComparison()
        {
            // AC-WKT-08-05: a los 120 s, Rep 1 de la serie A (2:05, 240 m); a los 121, vacía (serie B, rep 1); a los 181, Rep 1 de
            // la B; a los 241, en el descanso suelto, ya no está en serie.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.DosSeries());
                h.At(120.25);
                var s = h.Engine.Series.State;
                Assert.That(s.PreviousReps.Count, Is.EqualTo(1));
                AssertRep(s.PreviousReps[0], 1, 240, 240, 60, "2:05");
                h.At(121.25);
                Assert.That(h.Engine.Series.State.PreviousReps, Is.Empty);
                Assert.That(h.Engine.Series.State.Rep, Is.EqualTo(1));
                h.At(181.25);
                Assert.That(RepNumbers(h.Engine.Series.State.PreviousReps), Is.EqualTo(new[] { 1 }));
                h.At(241.25);
                Assert.That(h.Engine.Series.State.InSeries, Is.False);
                Assert.That(h.Engine.Series.State.PreviousReps, Is.Empty);
            }
        }

        [Test, Category("AC-WKT-07-02"), Category("AC-WKT-08-08")]
        public void PiramideQuinientos()
        {
            // AC-WKT-07-02: el trabajo de 500 m tiene #1 con 2:05.0 y el de 250 m que le sigue no tiene ninguna. AC-WKT-08-08: a los
            // 879 s, Rep 1 con 1112 m (1.112 m), 752 de trabajo, 188 s y 2:05.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.PiramideQuinientos());
                h.At(725.25);
                var s = h.Engine.Series.State;
                Assert.That(s.Laps.Count, Is.EqualTo(1), "el de 500 m termina con su vuelta en el mismo segundo");
                Assert.That(WorkoutFormats.LapTime(s.Laps[0].Seconds), Is.EqualTo("2:05.0"));
                h.At(788.25);
                Assert.That(h.Engine.Series.State.IsWorkStep, Is.True, "el de 250 m");
                Assert.That(h.Engine.Series.State.Laps, Is.Empty);
                h.At(879.25);
                s = h.Engine.Series.State;
                Assert.That(s.Rep, Is.EqualTo(2));
                AssertRep(s.PreviousReps[0], 1, 1112, 752, 188, "2:05");
                Assert.That(WorkoutFormats.PanelMeters(s.PreviousReps[0].TotalMeters), Is.EqualTo("1.112 m"));
            }
        }

        [Test, Category("AC-WKT-08-08"), Category("AC-WKT-08-07")]
        public void PruebaCuatroAAndCorta()
        {
            // AC-WKT-08-08: Prueba 4a a los 121 s, Rep 1 con 360 m y 2:05; Corta a los 16 s, Rep 1 con 60 m y 2:05, y la rutina
            // termina a los 30 s sin agregar la rep 2.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.PruebaCuatroA());
                h.At(121.25);
                var s = h.Engine.Series.State;
                Assert.That(s.PreviousReps.Count, Is.EqualTo(1));
                Assert.That(s.PreviousReps[0].Rep, Is.EqualTo(1));
                Assert.That(s.PreviousReps[0].TotalMeters, Is.EqualTo(360));
                Assert.That(WorkoutFormats.RepSplit(s.PreviousReps[0].WorkSplitSeconds), Is.EqualTo("2:05"));
            }

            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(16.25);
                var s = h.Engine.Series.State;
                AssertRep(s.PreviousReps[0], 1, 60, 40, 10, "2:05");
                h.At(31);
                Assert.That(h.State.Phase, Is.EqualTo(WorkoutPhase.Finished));
                Assert.That(h.State.TotalElapsedSeconds, Is.EqualTo(30));
                Assert.That(RepNumbers(h.Engine.Series.State.PreviousReps), Is.EqualTo(new[] { 1 }), "la rep 2 no entra");
            }
        }

        [Test, Category("AC-WKT-08-06")]
        public void TheWorkSplitCountsTheSecondsOfAJumpButNotItsMeters()
        {
            // AC-WKT-08-06: Series 600 con R-4: 150 s y 600 m dan 2:05; con R-4D, 154 s y 600 m dan 128,3 s: 2:08; Corta con R-4P
            // (parado): —.
            using (var h = new WorkoutHarness(RowerScripts.ByName("R-4D")))
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(205.25);
                var rep = h.Engine.Series.State.PreviousReps[0];
                Assert.That(rep.WorkMeters, Is.EqualTo(600));
                Assert.That(rep.WorkSeconds, Is.EqualTo(154));
                Assert.That(rep.WorkSplitSeconds, Is.EqualTo(128.333).Within(0.001));
                Assert.That(WorkoutFormats.RepSplit(rep.WorkSplitSeconds), Is.EqualTo("2:08"));
            }

            using (var h = new WorkoutHarness(k => RowerScripts.R4P(k)))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(16.25);
                var rep = h.Engine.Series.State.PreviousReps[0];
                Assert.That(rep.WorkMeters, Is.EqualTo(0));
                Assert.That(rep.WorkSplitSeconds, Is.Null);
                Assert.That(WorkoutFormats.RepSplit(rep.WorkSplitSeconds), Is.EqualTo("—"));
            }
        }

        [Test, Category("AC-WKT-07-02")]
        public void FondoTresMilWithRFourAKeepsEveryLapOfTheStep()
        {
            // AC-WKT-18-04 (la regla): Fondo 3000 con R-4A, a los 600 s, 0:50.0 250 m y cinco vueltas: #1 y #2 de 125 s, #3 a #5 de
            // 100 s.
            using (var h = new WorkoutHarness(k => RowerScripts.R4A(k)))
            {
                h.Routine(RoutineSeeds.FondoTresMil());
                h.At(600.25);
                var s = h.Engine.Series.State;
                Assert.That(Seconds(s.Laps), Is.EqualTo(new[] { 125.0, 125.0, 100.0, 100.0, 100.0 }));
                Assert.That(s.CurrentLapMeters, Is.EqualTo(250));
                Assert.That(s.CurrentLapSeconds, Is.EqualTo(50.0));
            }
        }

        [Test, Category("AC-WKT-07-08")]
        public void CountsFromTheMonitorDistanceAtTheStart()
        {
            // AC-WKT-07-08: con R-4 el monitor trae 1000 m y la rutina arranca en 0 (Q-15).
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.TrabajoMil());
                h.At(0.25);
                Assert.That(h.Engine.Series.Meters, Is.EqualTo(0));
                h.At(10.25);
                Assert.That(h.Engine.Series.State.CurrentLapMeters, Is.EqualTo(40));
                Assert.That(h.State.Data.DistanceMeters, Is.EqualTo(1040), "el monitor no vuelve a 0");
            }
        }

        // ── Lo común ──

        private static void AssertRep(RepSummary rep, int number, int total, int work, double seconds, string split)
        {
            Assert.That(rep.Rep, Is.EqualTo(number));
            Assert.That(rep.TotalMeters, Is.EqualTo(total));
            Assert.That(rep.WorkMeters, Is.EqualTo(work));
            Assert.That(rep.WorkSeconds, Is.EqualTo(seconds).Within(1e-9));
            Assert.That(WorkoutFormats.RepSplit(rep.WorkSplitSeconds), Is.EqualTo(split));
        }

        private static IntervalStep Work(int meters, string? group = G)
        {
            return new IntervalStep(1, 0, StepType.Work) { DistanceMeters = meters, GroupId = group, GroupRepeatCount = 5 };
        }

        private static IntervalStep Cool(int seconds, string? group = G)
        {
            return new IntervalStep(1, 1, StepType.Cooldown) { DurationSeconds = seconds, GroupId = group, GroupRepeatCount = 5 };
        }

        private static StepPosition Pos(int rep, string? group = G)
        {
            return new StepPosition(group, rep, group == null ? 1 : 5);
        }

        private static double[] Seconds(IReadOnlyList<Lap> laps)
        {
            var result = new double[laps.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = laps[i].Seconds;
            }

            return result;
        }

        private static int[] RepNumbers(IReadOnlyList<RepSummary> reps)
        {
            var result = new int[reps.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = reps[i].Rep;
            }

            return result;
        }

        // :20-42: lecturas de 1 s a velocidad constante, como WorkoutProvider._tick.
        private sealed class Feeder
        {
            private int _t;
            private double _d;

            public Feeder(SeriesTracker tracker)
            {
                Tracker = tracker;
                tracker.Begin(0);
            }

            public SeriesTracker Tracker { get; }

            public void Run(int stepIndex, IntervalStep step, StepPosition pos, int seconds, double speed)
            {
                for (var i = 0; i < seconds; i++)
                {
                    _t++;
                    _d += speed;
                    Tracker.Update(stepIndex, step, pos, (int)Math.Floor(_d), _t);
                }
            }
        }

        // :133-152: lecturas crudas del monitor de a 1 s (para simular fallas).
        private sealed class Raw
        {
            private readonly IntervalStep _step;
            private int _t;

            public Raw(SeriesTracker tracker, IntervalStep step, StepPosition pos, int startRaw = 0)
            {
                Tracker = tracker;
                _step = step;
                Position = pos;
                tracker.Begin(startRaw);
            }

            public SeriesTracker Tracker { get; }

            public StepPosition Position { get; set; }

            public void Feed(int raw, int stepIndex = 0)
            {
                _t++;
                Tracker.Update(stepIndex, _step, Position, raw, _t);
            }
        }
    }
}
