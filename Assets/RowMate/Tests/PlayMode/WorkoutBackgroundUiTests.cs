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

        [UnityTest, Category("AC-WKT-02-06"), Category("AC-WKT-03-05")]
        public IEnumerator CompletedToastOnceOnReturn()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(5.05);
            H.SendToBackground();
            H.BringToForeground(35);
            var back = H.Scheduler.MonotonicSeconds;
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "al volver, Workout en reposo");
            Assert.That(Screen.IsOnScreen, Is.True);
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("¡Entrenamiento completado!"), "el aviso del fin, retenido");
            var shown = H.Shell.Toasts.ShowCount;
            var session = LastSession();
            Assert.That(session, Is.Not.Null);
            Assert.That(session!.IsCut, Is.False, "cerrada como en primer plano");
            Assert.That(session.PointCount, Is.EqualTo(7), "con sus 7 puntos");
            H.Advance(back + 3.8 - H.Scheduler.MonotonicSeconds);
            yield return Frames();
            Assert.That(H.Shell.Toasts.IsVisible, Is.True, "con sus 4 s desde la vuelta");
            yield return Advance(1);
            yield return Settle();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False);
            Assert.That(H.Shell.Toasts.ShowCount, Is.EqualTo(shown), "una sola vez");
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
