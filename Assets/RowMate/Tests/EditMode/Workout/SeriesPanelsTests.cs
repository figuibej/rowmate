using NUnit.Framework;
using RowMate.Core.Models;
using RowMate.Core.Workout;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // LT-45: los formatos de los paneles de series (lib/features/workout/series_panels.dart:9-24).
    // It. 5 (SPN; 05-series-parciales/02-tech-design §2.4): qué muestra cada panel (SeriesPanels.For): la visibilidad (solo con
    // rutina; repeticiones solo en serie, LT-61), las 4 últimas vueltas con la más reciente primero, el segundo de atraso en el
    // cambio de paso (P-01) y DV-48 (el primer cuadro ya muestra el primer paso). El caso formatos (LT-45) queda intacto.
    [TestFixture]
    public sealed class SeriesPanelsTests
    {
        // legacy: test/series_panels_test.dart:32 'formatos'
        [TestCase(TestName = "formatos", Category = "LT-45,AC-WKT-11-09,AC-WKT-11-10,AC-WKT-11-11")]
        public void Formats()
        {
            Assert.That(WorkoutFormats.LapTime(166.667), Is.EqualTo("2:46.7"));
            Assert.That(WorkoutFormats.LapTime(65), Is.EqualTo("1:05.0"));
            Assert.That(WorkoutFormats.RepSplit(114.4), Is.EqualTo("1:54"));
            Assert.That(WorkoutFormats.PanelMeters(1245), Is.EqualTo("1.245 m"));
        }

        // legacy: test/series_panels_test.dart:68 'sin serie no aparece el panel de repeticiones'
        [TestCase(TestName = "sin serie no aparece el panel de repeticiones", Category = "LT-61,AC-WKT-18-01")]
        public void WithoutASeriesThereIsNoRepsPanel()
        {
            // La regla de LT-61 (la parte montada es SPV): el tracker del legacy (:12-22) sin grupo, con una sola lectura de 300 m a
            // los 60 s: se ve parciales, no repeticiones. Con grupo, los dos.
            var loose = new SeriesTracker();
            loose.Begin(0, 0);
            loose.Update(0, RoutineSeeds.Lt59LooseWork, new StepPosition(null, 1, 1), 300, 60);
            var content = SeriesPanels.For(loose.State, new[] { RoutineSeeds.Lt59LooseWork }, new[] { StepPosition.Loose });
            Assert.That(content.Visible, Is.True, "parciales: con rutina, siempre");
            Assert.That(content.InSeries, Is.False, "sin serie no hay panel de repeticiones");
            Assert.That(content.CurrentLapMeters, Is.EqualTo(300));
            Assert.That(content.CurrentLapSeconds, Is.EqualTo(60));

            var grouped = new SeriesTracker();
            grouped.Begin(0, 0);
            grouped.Update(0, RoutineSeeds.Lt59Work, new StepPosition("g", 1, 3), 300, 60);
            content = SeriesPanels.For(grouped.State, new[] { RoutineSeeds.Lt59Work }, new[] { new StepPosition("g", 1, 3) });
            Assert.That(content.InSeries, Is.True);
            Assert.That(content.Rep, Is.EqualTo(1));
            Assert.That(content.RepCount, Is.EqualTo(3));
            Assert.That(content.RepMeters, Is.EqualTo(300), "la lectura única suma: 300 ≤ 10 × max(60; 1)");
        }

        [Test, Category("AC-WKT-18-01"), Category("AC-WKT-07-01")]
        public void OnlyWithARoutine()
        {
            // En el libre no hay paneles ni lugar reservado.
            var content = SeriesPanels.For(SeriesState.Empty, null, null);
            Assert.That(content.Visible, Is.False);
            Assert.That(content.InSeries, Is.False);
            Assert.That(content.PreviousReps, Is.Empty);
            using (var h = new WorkoutHarness())
            {
                h.Free();
                h.At(10.25);
                Assert.That(SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions).Visible, Is.False);
            }
        }

        [Test, Category("AC-WKT-18-01")]
        public void FromTheFirstFrameTheFirstStep()
        {
            // DV-48: desde el primer cuadro, el primer paso. Series 600 empieza con el calentamiento (sin serie: el chip 500 m
            // solo); Cinco reps empieza con la serie: la vuelta en curso en 0:00.0 0 m, Rep 1/5 · 0 m y el panel de repeticiones.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                var content = SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions);
                Assert.That(h.State.Series!.StepIndex, Is.Null, "el tracker todavía no recibió una lectura");
                Assert.That(content.Visible, Is.True);
                Assert.That(content.StepIndex, Is.EqualTo(0));
                Assert.That(content.IsWork, Is.False);
                Assert.That(content.InSeries, Is.False);
            }

            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.CincoReps());
                var content = SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions);
                Assert.That(content.IsWork, Is.True);
                Assert.That(content.CurrentLapMeters, Is.EqualTo(0));
                Assert.That(content.CurrentLapSeconds, Is.EqualTo(0));
                Assert.That(content.LapCount, Is.EqualTo(0));
                Assert.That(content.InSeries, Is.True);
                Assert.That(content.Rep, Is.EqualTo(1));
                Assert.That(content.RepCount, Is.EqualTo(5));
                Assert.That(content.RepMeters, Is.EqualTo(0));
                Assert.That(content.PreviousReps, Is.Empty);
                Assert.That(SeriesPanels.For(null, h.Engine.Steps, h.Engine.Positions).InSeries, Is.True, "sin foto, lo mismo");
            }
        }

        [Test, Category("AC-WKT-18-01")]
        public void LeavingTheSeriesHidesTheRepsPanel()
        {
            // Series 600: el panel de repeticiones se va a los 541 s (el enfriamiento, suelto).
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(31.25);
                Assert.That(SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions).InSeries, Is.True);
                h.At(540.25);
                Assert.That(SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions).InSeries, Is.True);
                h.At(541.25);
                var content = SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions);
                Assert.That(content.InSeries, Is.False);
                Assert.That(content.Visible, Is.True, "parciales sigue");
                Assert.That(content.IsWork, Is.False);
            }
        }

        [Test, Category("AC-WKT-18-04")]
        public void TheLastFourLapsMostRecentFirst()
        {
            // Fondo 3000 con R-4A a los 600 s: 0:50.0 250 m; #5 1:40.0, #4 1:40.0, #3 1:40.0 y #2 2:05.0; #1 no se ve.
            using (var h = new WorkoutHarness(k => RowerScripts.R4A(k)))
            {
                h.Routine(RoutineSeeds.FondoTresMil());
                h.At(600.25);
                var content = SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions);
                Assert.That(content.LapCount, Is.EqualTo(5));
                Assert.That(content.VisibleLaps, Is.EqualTo(4));
                Assert.That(content.Lap0.Number, Is.EqualTo(5));
                Assert.That(content.Lap1.Number, Is.EqualTo(4));
                Assert.That(content.Lap2.Number, Is.EqualTo(3));
                Assert.That(content.Lap3.Number, Is.EqualTo(2));
                Assert.That(WorkoutFormats.LapTime(content.Lap0.Seconds), Is.EqualTo("1:40.0"));
                Assert.That(WorkoutFormats.LapTime(content.Lap2.Seconds), Is.EqualTo("1:40.0"));
                Assert.That(WorkoutFormats.LapTime(content.Lap3.Seconds), Is.EqualTo("2:05.0"));
                Assert.That(WorkoutFormats.LapTime(content.CurrentLapSeconds), Is.EqualTo("0:50.0"));
                Assert.That(content.CurrentLapMeters, Is.EqualTo(250));
                Assert.That(content.InSeries, Is.False, "sin serie: sin línea Rep");
            }

            using (var h = new WorkoutHarness(k => RowerScripts.R3(k)))
            {
                // Trabajo 1000 con R-3, a los 170 s: 0:03.3 10 m y #1 2:46.7.
                h.Routine(RoutineSeeds.TrabajoMil());
                h.At(170.25);
                var content = SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions);
                Assert.That(content.VisibleLaps, Is.EqualTo(1));
                Assert.That(WorkoutFormats.LapTime(content.Lap0.Seconds), Is.EqualTo("2:46.7"));
                Assert.That(WorkoutFormats.LapTime(content.CurrentLapSeconds), Is.EqualTo("0:03.3"));
                Assert.That(content.CurrentLapMeters, Is.EqualTo(10));
            }
        }

        [Test, Category("AC-WKT-18-05"), Category("AC-WKT-18-06"), Category("AC-WKT-18-07"), Category("AC-WKT-18-08")]
        public void WhatTheyShowSecondBySecondInSeriesSeiscientos()
        {
            // AC-WKT-18-08: a los 180 s la barra ya está en el descanso y los paneles todavía con el trabajo (0:25.0 100 m, #1 2:05.0
            // y Rep 1/3 · 600 m); a los 181, fuera de un trabajo. AC-WKT-18-06: Rep 1/3 · 560 m a los 170, · 640 m a los 190 y
            // Rep 2/3 · 200 m a los 250. AC-WKT-18-07: a los 380 s, Rep 2 y Rep 1 con 2:05 680 m.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.SeriesSeiscientos());
                h.At(170.25);
                var c = Content(h);
                Assert.That(c.RepMeters, Is.EqualTo(560));
                h.At(180.25);
                c = Content(h);
                Assert.That(h.State.Step!.Value.Step.Type, Is.EqualTo(StepType.Rest), "la barra ya muestra el descanso");
                Assert.That(c.StepIndex, Is.EqualTo(1), "los paneles todavía el trabajo (P-01)");
                Assert.That(c.IsWork, Is.True);
                Assert.That(c.CurrentLapMeters, Is.EqualTo(100));
                Assert.That(WorkoutFormats.LapTime(c.CurrentLapSeconds), Is.EqualTo("0:25.0"));
                Assert.That(c.LapCount, Is.EqualTo(1));
                Assert.That(c.RepMeters, Is.EqualTo(600));
                h.At(181.25);
                c = Content(h);
                Assert.That(c.IsWork, Is.False, "Sin parciales en este paso");
                Assert.That(c.VisibleLaps, Is.EqualTo(0));
                Assert.That(c.RepMeters, Is.EqualTo(604));
                Assert.That(c.PreviousReps, Is.Empty, "la repetición no terminó");
                h.At(190.25);
                Assert.That(Content(h).RepMeters, Is.EqualTo(640));
                h.At(250.25);
                c = Content(h);
                Assert.That(c.Rep, Is.EqualTo(2));
                Assert.That(c.RepMeters, Is.EqualTo(200));
                Assert.That(c.PreviousReps.Count, Is.EqualTo(1));
                h.At(380.25);
                c = Content(h);
                Assert.That(c.PreviousReps.Count, Is.EqualTo(2));
                Assert.That(c.PreviousReps[0].Rep, Is.EqualTo(2));
                Assert.That(c.PreviousReps[1].Rep, Is.EqualTo(1));
                Assert.That(WorkoutFormats.PanelMeters(c.PreviousReps[1].TotalMeters), Is.EqualTo("680 m"));
            }
        }

        [Test, Category("AC-WKT-18-07")]
        public void RepsRowsOfTheAppCases()
        {
            // Pirámide 500 a los 880 s: Rep 1 2:05 1.112 m; Corta con R-4P a los 16 s: Rep 1 — 0 m.
            using (var h = new WorkoutHarness())
            {
                h.Routine(RoutineSeeds.PiramideQuinientos());
                h.At(880.25);
                var rep = Content(h).PreviousReps[0];
                Assert.That(WorkoutFormats.RepSplit(rep.WorkSplitSeconds), Is.EqualTo("2:05"));
                Assert.That(WorkoutFormats.PanelMeters(rep.TotalMeters), Is.EqualTo("1.112 m"));
            }

            using (var h = new WorkoutHarness(k => RowerScripts.R4P(k)))
            {
                h.Routine(RoutineSeeds.Corta());
                h.At(16.25);
                var rep = Content(h).PreviousReps[0];
                Assert.That(WorkoutFormats.RepSplit(rep.WorkSplitSeconds), Is.EqualTo("—"));
                Assert.That(WorkoutFormats.PanelMeters(rep.TotalMeters), Is.EqualTo("0 m"));
            }
        }

        private static SeriesPanelsContent Content(WorkoutHarness h)
        {
            return SeriesPanels.For(h.State.Series, h.Engine.Steps, h.Engine.Positions);
        }
    }
}
