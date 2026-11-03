using System.Collections;
using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // WBU · El segundo plano en pantalla (Q-91; AC-WKT-02-06, -08; QA-G4aT-07, QA-G4a-P08; 04a-motor-hud-funcional/02-tech-design
    // §4.1-§4.2): al volver, el alcance corre con IsCatchingUp (sin fotos intermedias) y el primer estado llega con Instant: la
    // vista lo pone sin animar (sin rodillos, sin empujón, los pulsos con origen en el primer cuadro real).
    public sealed class WorkoutBackgroundUiTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-02-08")]
        public IEnumerator FirstFrameShowsThePresent()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Prueba 4a");
            yield return AdvanceTo(10.05);
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:10"));
            var published = 0;
            using (Immersive.Hud.State.Subscribe(_ => published++, false))
            {
                H.SendToBackground();
                H.BringToForeground(70);
            }

            var first = Immersive.Hud.State.Value;
            Assert.That(first.Instant, Is.True, "el primer estado después del alcance, con Instant");
            Assert.That(published, Is.EqualTo(1), "una sola foto por la vuelta (sin las intermedias)");
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("01:20"), "el primer cuadro ya muestra el presente");
            Assert.That(H.TextOf("hud-step-type"), Is.EqualTo("Trabajo"), "y el paso actual");
            Assert.That(Hud.Band.Time.IsRolling, Is.False, "sin rodillos: del valor viejo al nuevo de una vez");
            Assert.That(Hud.Watts.OffTarget!.IsRunning, Is.False, "sin empujón ni rampa");
            var pulse = Hud.Steps!.Pulse;
            Assert.That(pulse.IsPulsing, Is.True, "Próximo a 10 s del final");
            Assert.That(pulse.Origin, Is.EqualTo(H.Scheduler.MonotonicSeconds).Within(1e-6), "el pulso con origen en el primer cuadro de la vuelta");
            yield return Frames();
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
        }

        // It. 5 (05-series-parciales/02-tech-design §4.2; AC-WKT-02-08, AC-WKT-18-19): las vueltas y las repeticiones del segundo plano
        // están en el primer cuadro, sin empuje, sin marca y sin rodillos.
        [UnityTest, Category("AC-WKT-02-08"), Category("AC-WKT-18-19")]
        public IEnumerator ThePanelsShowThePresentInTheFirstFrame()
        {
            var prefs = new RowMate.Core.Common.MemoryPreferences();
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.LapsKey, true);
            prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.RepsKey, true);
            CreateWorkout(RoutineSeeds.F5, o => o.Preferences = prefs);
            yield return StartRoutine("Series 600");
            yield return AdvanceTo(150.2);
            H.SendToBackground();
            H.BringToForeground(220);
            var column = Hud.Series!;
            Assert.That(H.TextOf("hud-reps-row-0"), Is.EqualTo("Rep 1 2:05 680 m"), "la repetición del segundo plano, ya en el primer cuadro");
            Assert.That(H.TextOf("hud-reps-row-1"), Is.Null.Or.Empty, "una sola");
            Assert.That(column.Laps.Motion.IsRunning || column.Reps.Motion.IsRunning, Is.False, "sin empuje, sin marca, sin piezas");
            Assert.That(column.Laps.RollProgress, Is.EqualTo(1), "sin rodillos");
            Assert.That(column.Reps.Visual.MarkerAlpha0, Is.EqualTo(0), "sin marca");
            yield return Frames();
            Assert.That(column.Laps.Motion.IsRunning || column.Reps.Motion.IsRunning, Is.False, "nada anima lo que ya pasó");
        }

        // It. 4b (AC-WKT-02-06, AC-WKT-21-09; DT4a-15): Corta termina sola en segundo plano; al volver, el diálogo de completado ya
        // está, SIN su entrada y con los totales finales, la sesión cerrada con sus 7 puntos y sin el aviso workoutCompleted
        // (AC-WKT-03-05: es el título del diálogo). Su Terminar vuelve a reposo y la página sale.
        [UnityTest, Category("AC-WKT-02-06"), Category("AC-WKT-03-05"), Category("AC-WKT-21-09")]
        public IEnumerator CortaEndsInBackgroundDialogAlreadyOpen()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(5.05);
            H.SendToBackground();
            H.BringToForeground(35);
            yield return Frames();
            var dialog = H.Completion;
            Assert.That(dialog, Is.Not.Null, "al volver, el diálogo de completado ya está");
            Assert.That(dialog!.OpenedInstantly, Is.True, "sin su entrada");
            Assert.That(dialog.Motion.IsRunning, Is.False, "con el check dibujado y los totales finales");
            Assert.That(dialog.TimeValue.Text, Is.EqualTo("00:30"));
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "sin aviso de completado (AC-WKT-03-05)");
            var session = LastSession();
            Assert.That(session, Is.Not.Null);
            Assert.That(session!.IsCut, Is.False, "cerrada como en primer plano");
            Assert.That(session.PointCount, Is.EqualTo(7), "con sus 7 puntos");
            yield return Tap("completion-finish");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "Terminar: Workout en reposo");
            Assert.That(Screen.IsOnScreen, Is.True);
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "sin aviso");
        }

        [UnityTest, Category("AC-WKT-02-08")]
        public IEnumerator WithoutFocusTheClockKeepsGoing()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            H.Unfocus();
            yield return AdvanceTo(20.05);
            Assert.That(H.TextOf("hud-time-value"), Is.EqualTo("00:20"), "la ventana sin foco: el reloj sigue");
            H.Focus();
            yield return Frames();
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
        }
    }
}
