using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;

namespace RowMate.Tests.PlayMode
{
    // Base de las pruebas de los paneles de series (05-series-parciales/02-tech-design §6.3): F-5 con R-4, una MemoryPreferences por
    // prueba sembrada con lo que pide (nunca un archivo del usuario: AC-HIS-20-04) y los accesos a la columna, los paneles y sus
    // ranuras. "A los N s" = N s desde el toque que empezó la sesión.
    public abstract class SeriesUiTestBase : WorkoutUiTestBase
    {
        // ± 0,5 dp (DD5-25) contra lo que da la geometría con la ventana real; los números de 04-design-notes §9.2 usan s = 1,054 y la
        // app 1,0549 (411,4 / 390): en el borde de abajo de la columna eso suma hasta 0,6 dp, así que contra esos números va ± 1.
        protected const float RectTolerance = 1.0f;

        protected SeriesColumn Column => H.SeriesColumn ?? throw new InvalidOperationException("sin paneles de series");

        protected SeriesPanelView Laps => Column.Laps;

        protected SeriesPanelView Reps => Column.Reps;

        protected static MemoryPreferences Prefs(bool laps, bool reps)
        {
            var prefs = new MemoryPreferences();
            if (laps)
            {
                prefs.SetBool(SeriesPanelPrefs.LapsKey, true);
            }

            if (reps)
            {
                prefs.SetBool(SeriesPanelPrefs.RepsKey, true);
            }

            return prefs;
        }

        protected UiTestHost CreateSeries(DeviceProfile profile, bool laps = false, bool reps = false, Func<int, RowingData>? reading = null,
                                          Action<UiHostOptions>? configure = null, RoutineSeed? seed = null)
        {
            return CreateWorkout(seed ?? RoutineSeeds.F5, o =>
            {
                o.Profile = profile;
                o.IsDesktop = profile.Code == DeviceProfile.W1280.Code;
                o.Preferences = Prefs(laps, reps);
                configure?.Invoke(o);
            }, reading);
        }

        protected IEnumerator StartAt(string routine, double seconds)
        {
            yield return StartRoutine(routine);
            yield return AdvanceTo(seconds);
            yield return Settle();
        }

        protected DrawnSlot SlotNamed(string name)
        {
            var slot = H.SlotOf(name);
            Assert.That(slot.HasValue, Is.True, "no hay ranura " + name);
            return slot!.Value.Slot;
        }

        protected static void AssertSpan(string what, float actualFrom, float actualTo, double expectedFrom, double expectedTo, float tolerance = RectTolerance)
        {
            Assert.That(actualFrom, Is.EqualTo((float)expectedFrom).Within(tolerance), what + ": desde (" + actualFrom.ToString("0.00") + " a " + actualTo.ToString("0.00") + ")");
            Assert.That(actualTo, Is.EqualTo((float)expectedTo).Within(tolerance), what + ": hasta (" + actualFrom.ToString("0.00") + " a " + actualTo.ToString("0.00") + ")");
        }

        // El rectángulo visual de un panel en dp de ventana: su caja con el translate de la columna y el alto visual G.
        protected static Rect VisualRect(SeriesPanelView panel)
        {
            var box = panel.worldBound;
            return new Rect(box.x, box.y, box.width, (float)(panel.VisualHeightBase * panel.Scale));
        }

        protected static string Describe(SeriesPanelView panel)
        {
            return panel.name + " layout " + panel.layout + " world " + panel.worldBound + " G " + panel.VisualHeightBase.ToString("0.000") + " final "
                   + panel.FinalHeightBase.ToString("0.000") + " translate " + panel.resolvedStyle.translate.y.ToString("0.000") + " sp " + panel.Scale.ToString("0.0000");
        }

        protected IEnumerator SettlePanels()
        {
            // Un panel recién abierto o cerrado: 0,82 s (SeriesSettleTapMs) y lo que quede.
            H.Advance(0.84);
            yield return Frames();
            yield return Settle();
        }
    }
}
