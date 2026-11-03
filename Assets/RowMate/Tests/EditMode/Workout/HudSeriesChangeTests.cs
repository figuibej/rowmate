using System;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Localization;
using RowMate.Core.Models;
using RowMate.Tests.EditMode.Localization;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Workout
{
    // HSC: qué cambió entre dos estados de los paneles, POR PIEZA (05-series-parciales/02-tech-design §2.5; 04-design-notes §6.2 a
    // §6.4; DD5-18): la tabla del diseño con R-4, con el motor de verdad y la composición del store (el segundo anterior y el
    // segundo del cambio).
    [TestFixture]
    public sealed class HudSeriesChangeTests
    {
        [Test, Category("AC-WKT-18-19")]
        public void ANewLap()
        {
            // Series 600 · 325 (VID-02): NewLaps 1, nada más.
            var c = Change(RoutineSeeds.SeriesSeiscientos(), 325);
            Assert.That(c.NewLaps, Is.EqualTo(1));
            Assert.That(c.StepChanged, Is.False);
            Assert.That(c.LapDropped, Is.False);
            Assert.That(c.NewRep, Is.False);
            Assert.That(c.IsNone, Is.False);
        }

        [Test, Category("AC-WKT-18-08"), Category("AC-WKT-18-19")]
        public void WorkToRest()
        {
            // Series 600 · 181 (VID-03): StepChanged, LapsBody ToEmpty, ClearedLaps 1.
            var c = Change(RoutineSeeds.SeriesSeiscientos(), 181);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.ToEmpty));
            Assert.That(c.ClearedLaps, Is.EqualTo(1));
            Assert.That(c.NewRep, Is.False);
            Assert.That(c.RepsBody, Is.EqualTo(SeriesBodyChange.None));
        }

        [Test, Category("AC-WKT-18-08"), Category("AC-WKT-18-19")]
        public void RestToTheNextRep()
        {
            // Series 600 · 201 (VID-04): StepChanged, LapsBody FromEmpty, NewRep, RepsBody FromEmpty.
            var c = Change(RoutineSeeds.SeriesSeiscientos(), 201);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            Assert.That(c.NewRep, Is.True);
            Assert.That(c.RepsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            Assert.That(c.RepDropped, Is.False);
            Assert.That(c.SeriesStarted, Is.False);
        }

        [Test, Category("AC-WKT-18-19")]
        public void ASecondRepJoinsTheList()
        {
            // Series 600 · 371 (CAP-04): StepChanged, LapsBody FromEmpty, NewRep (de 1 a 2 filas); la lista no cambia de clase.
            var c = Change(RoutineSeeds.SeriesSeiscientos(), 371);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            Assert.That(c.NewRep, Is.True);
            Assert.That(c.RepsBody, Is.EqualTo(SeriesBodyChange.None));
        }

        [Test, Category("AC-WKT-18-19"), Category("AC-WKT-18-01")]
        public void LeavingTheSeriesTheEmptyDoesNotMove()
        {
            // Series 600 · 541 (descanso → enfriamiento; CAP-07): StepChanged, LapsBody None, SeriesEnded; nada más (QA-G5P-03).
            var c = Change(RoutineSeeds.SeriesSeiscientos(), 541);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.None), "el vacío no se mueve");
            Assert.That(c.SeriesEnded, Is.True);
            Assert.That(c.SeriesStarted, Is.False);
            Assert.That(c.NewLaps, Is.EqualTo(0));
            Assert.That(c.ClearedLaps, Is.EqualTo(0));
            Assert.That(c.NewRep, Is.False);
            Assert.That(c.RepDropped, Is.False);
            Assert.That(c.RepsBody, Is.EqualTo(SeriesBodyChange.None), "el panel entero se va");
        }

        [Test, Category("AC-WKT-18-01"), Category("AC-WKT-18-19")]
        public void TheSeriesStarts()
        {
            // Series 600 · 31 (DEMO-11): StepChanged, LapsBody FromEmpty, SeriesStarted.
            var c = Change(RoutineSeeds.SeriesSeiscientos(), 31);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            Assert.That(c.SeriesStarted, Is.True);
            Assert.That(c.RepsBody, Is.EqualTo(SeriesBodyChange.None));
        }

        [Test, Category("AC-WKT-18-19")]
        public void WorkToWorkRolls()
        {
            // Dos series · 61 (trabajo → trabajo): StepChanged, LapsBody Roll (la vuelta en curso no funde), NewRep, RepsBody
            // FromEmpty. Dos series · 121 (serie A → serie B): Roll, RepsBody ToEmpty, ni SeriesStarted ni SeriesEnded. Dos series ·
            // 241: ToEmpty, SeriesEnded.
            var a = Change(RoutineSeeds.DosSeries(), 61);
            Assert.That(a.StepChanged, Is.True);
            Assert.That(a.LapsBody, Is.EqualTo(SeriesBodyChange.Roll));
            Assert.That(a.NewRep, Is.True);
            Assert.That(a.RepsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            var b = Change(RoutineSeeds.DosSeries(), 121);
            Assert.That(b.LapsBody, Is.EqualTo(SeriesBodyChange.Roll));
            Assert.That(b.RepsBody, Is.EqualTo(SeriesBodyChange.ToEmpty));
            Assert.That(b.SeriesStarted, Is.False);
            Assert.That(b.SeriesEnded, Is.False);
            Assert.That(b.NewRep, Is.False);
            var end = Change(RoutineSeeds.DosSeries(), 241);
            Assert.That(end.StepChanged, Is.True);
            Assert.That(end.LapsBody, Is.EqualTo(SeriesBodyChange.ToEmpty));
            Assert.That(end.SeriesEnded, Is.True);
            Assert.That(end.RepsBody, Is.EqualTo(SeriesBodyChange.None));
        }

        [Test, Category("AC-WKT-18-19")]
        public void WorkToWorkClearsTheLaps()
        {
            // Pirámide 500 · 726 (el trabajo de 500 m → el de 250 m): Roll y ClearedLaps 1.
            var c = Change(RoutineSeeds.PiramideQuinientos(), 726);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.Roll));
            Assert.That(c.ClearedLaps, Is.EqualTo(1));
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheOldestRepGoes()
        {
            // Cinco reps · 541 (CAP-10): StepChanged, LapsBody FromEmpty, NewRep, RepDropped.
            var c = Change(RoutineSeeds.CincoReps(), 541);
            Assert.That(c.StepChanged, Is.True);
            Assert.That(c.LapsBody, Is.EqualTo(SeriesBodyChange.FromEmpty));
            Assert.That(c.NewRep, Is.True);
            Assert.That(c.RepDropped, Is.True);
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheOldestLapGoes()
        {
            // Fondo 3000 con R-4A, la vuelta 5 (a los 550 s; CAP-08): NewLaps 1, LapDropped.
            var c = Change(RoutineSeeds.FondoTresMil(), 550, k => RowerScripts.R4A(k));
            Assert.That(c.NewLaps, Is.EqualTo(1));
            Assert.That(c.LapDropped, Is.True);
            Assert.That(c.StepChanged, Is.False);
        }

        [Test, Category("AC-WKT-18-19")]
        public void TwoLapsInOneSecond()
        {
            // Armado a mano: con el tope de 10 m/s solo pasa con Δt > 100 s (AC-WKT-07-04: una lectura puede cerrar varias).
            var l10n = LocalizationFixture.Es;
            var before = Series(l10n, 2);
            var after = Series(l10n, 4);
            var c = HudSeriesChange.Between(before, after);
            Assert.That(c.NewLaps, Is.EqualTo(2));
            Assert.That(c.LapDropped, Is.False, "había 2 a la vista: entran las 2 sin que se vaya ninguna");
            var five = HudSeriesChange.Between(after, Series(l10n, 6));
            Assert.That(five.NewLaps, Is.EqualTo(2));
            Assert.That(five.LapDropped, Is.True);
        }

        [Test, Category("AC-WKT-18-19")]
        public void TheFirstStateOrAnotherSessionIsNone()
        {
            var l10n = LocalizationFixture.Es;
            var state = Series(l10n, 2);
            Assert.That(HudSeriesChange.Between(null, state).IsNone, Is.True, "el primer estado se aplica en su estado final");
            Assert.That(HudSeriesChange.Between(state with { Session = 7 }, state).IsNone, Is.True, "otra sesión");
            Assert.That(HudSeriesChange.Between(state, state).IsNone, Is.True, "el mismo estado");
            Assert.That(default(HudSeriesChange).IsNone, Is.True);
        }

        // El cambio entre el segundo second − 1 y el segundo second, con la composición pura del store.
        private static HudSeriesChange Change(Routine routine, int second, Func<int, RowingData?>? rower = null)
        {
            var l10n = LocalizationFixture.Es;
            using (var h = new WorkoutHarness(rower))
            {
                h.Routine(routine);
                h.At(second - 1 + 0.25);
                var before = WorkoutHudStore.ComposeSeries(h.State.Series, h.Engine.Steps, h.Engine.Positions, l10n, h.State.Session);
                h.At(second + 0.25);
                var after = WorkoutHudStore.ComposeSeries(h.State.Series, h.Engine.Steps, h.Engine.Positions, l10n, h.State.Session);
                Assert.That(before, Is.Not.Null);
                Assert.That(after, Is.Not.Null);
                return HudSeriesChange.Between(before, after!);
            }
        }

        // Un trabajo con count vueltas (las 4 últimas a la vista).
        private static HudSeries Series(Localizer l10n, int count)
        {
            var visible = Math.Min(count, 4);
            var laps = new HudLapRow[visible];
            for (var i = 0; i < visible; i++)
            {
                laps[i] = SeriesSeeds.Lap(l10n, count - i, 125);
            }

            return new HudSeries(1, 1, true, "0:10.0", 10, "40 m", 40, laps, count, false, 1, null, 0, Array.Empty<HudRepRow>());
        }
    }
}
