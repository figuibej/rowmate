using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Workout;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // HCT · Los controles del inmersivo, el diálogo de terminar y el cierre (F-WKT-20, F-WKT-13; QA-G4aT-04; DN4a-20, DT4a-01;
    // 04a-motor-hud-funcional/02-tech-design §6.3, §6.8). Corta (F-4A) dura 30 s: serie ×2 [trabajo 10 s; descanso 5 s].
    public sealed class HudControlsTests : WorkoutUiTestBase
    {
        [UnityTest, Category("AC-WKT-20-01")]
        public IEnumerator ControlsRow()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            var layout = Immersive.Layout!;
            var s = (float)layout.Scale;
            var finish = Bounds("hud-finish");
            var pause = Bounds("hud-pause");
            var window = H.Root.worldBound;
            Assert.That(H.TextOf("hud-finish"), Is.EqualTo("Terminar"));
            Assert.That(H.TextOf("hud-pause"), Is.EqualTo("Pausa"));
            Assert.That(pause.xMin - finish.xMax, Is.EqualTo(16 * s).Within(0.6f), "16 de separación (× escala)");
            Assert.That(finish.width, Is.GreaterThanOrEqualTo(128 * s - 0.6f), "mínimo 128 × 56");
            Assert.That(finish.height, Is.GreaterThanOrEqualTo(56 * s - 0.6f));
            Assert.That(window.yMax - finish.yMax, Is.EqualTo(16 * s).Within(1f), "a margen seguro inferior + 16");
            Assert.That((finish.xMin + pause.xMax) / 2, Is.EqualTo(window.center.x).Within(1f), "abajo al centro");
            Assert.That(Hud.Controls.Finish.ClassListContains("rm-button--hud-danger"), Is.True);
            Assert.That(Hud.Controls.Pause.ClassListContains("rm-button--hud-outline"), Is.True);
        }

        [UnityTest, Category("AC-WKT-20-02")]
        public IEnumerator PauseAndResumeAtOnce()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return Frames();
            H.Tap("hud-pause");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Paused), "en el acto, sin confirmación");
            Assert.That(Hud.Controls.ShowsResume, Is.True, "el botón cambia en el mismo cuadro");
            yield return Frames();
            Assert.That(H.IsShown("hud-resume"), Is.True);
            Assert.That(H.IsShown("hud-pause"), Is.False);
            Assert.That(H.TextOf("hud-resume"), Is.EqualTo("Reanudar"));
            H.Tap("hud-resume");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
            Assert.That(Hud.Controls.ShowsResume, Is.False);
        }

        [UnityTest, Category("AC-WKT-20-03"), Category("AC-APP-03-10")]
        public IEnumerator FinishConfirmation()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return Tap("hud-finish");
            Assert.That(H.TopDialog, Is.InstanceOf<FinishWorkoutDialog>());
            Assert.That(Immersive.FinishDialog, Is.Not.Null);
            Assert.That(H.TextOf("dialog-title"), Is.EqualTo("Terminar entrenamiento"));
            Assert.That(H.TextOf("dialog-message"), Is.EqualTo("Se guardará la sesión. ¿Continuar?"));
            Assert.That(H.TextOf("dialog-cancel"), Is.EqualTo("Cancelar"));
            Assert.That(H.TextOf("dialog-confirm"), Is.EqualTo("Terminar"));
            Assert.That(H.Root.Q(className: "rm-dialog--hud"), Is.Null, "el diálogo de la app: opaco, sin variante del HUD");
            Assert.That(H.TopDialog!.Box.resolvedStyle.backgroundColor.a, Is.EqualTo(1).Within(0.001f), "opaco");
            Assert.That(H.TopDialog.Box.worldBound.width, Is.EqualTo(331).Within(1f), "de 331 en TV");
            H.TapScrim();
            yield return Settle();
            Assert.That(H.TopDialog, Is.Null, "tocar afuera es cancelar");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "sigue en marcha");
        }

        [UnityTest, Category("AC-WKT-20-04"), Category("AC-WKT-01-06"), Category("AC-WKT-13-04")]
        public IEnumerator ConfirmEndsWithoutSummary()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return Advance(12);
            yield return Tap("hud-finish");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "se cierra solo al volver a reposo");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));
            Assert.That(Screen.IsOnScreen, Is.True, "Workout en reposo con la barra de pestañas");
            Assert.That(H.IsShown("tab-bar"), Is.True);
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "sin resumen ni aviso de completado (Q-53)");
            var session = LastSession();
            Assert.That(session, Is.Not.Null);
            Assert.That(session!.IsCut, Is.False, "la sesión quedó cerrada");
            Assert.That(H.IsTextVisible("Strava"), Is.False);
        }

        [UnityTest, Category("AC-WKT-20-05")]
        public IEnumerator FinishFromPause()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return Tap("hud-pause");
            yield return Tap("hud-finish");
            Assert.That(H.TopDialog, Is.InstanceOf<FinishWorkoutDialog>(), "en pausa también");
            yield return Tap("dialog-confirm");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));
        }

        [UnityTest, Category("AC-WKT-20-06")]
        public IEnumerator NoDoubles()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Sessions = HostSessions.Scripted(r => r.FinishDelay = TimeSpan.FromSeconds(1)));
            yield return StartFree();
            yield return Advance(3);
            yield return Tap("hud-finish");
            yield return Frames();
            var confirmAt = H.BoundsOf("dialog-confirm").center;
            H.Tap("dialog-confirm");
            H.TapAt(confirmAt);
            yield return Frames();
            Assert.That(Phase, Is.Not.EqualTo(WorkoutPhase.Active), "terminando");
            Assert.That(Responds("hud-pause"), Is.False, "los controles no responden hasta que sale");
            H.TapAt(H.BoundsOf("hud-finish").center);
            yield return Frames();
            Assert.That(H.TopDialog, Is.Null, "no se abre otro diálogo");
            yield return Advance(1.5);
            yield return Settle();
            var finishes = 0;
            foreach (var call in H.ScriptedSessions!.Calls)
            {
                if (call.StartsWith("finish", StringComparison.Ordinal))
                {
                    finishes++;
                }
            }

            Assert.That(finishes, Is.EqualTo(1), "dos toques en Terminar: un solo cierre");
            Assert.That(H.Immersive, Is.Null);
        }

        [UnityTest, Category("AC-WKT-20-06")]
        public IEnumerator FinishShowsTheSpinnerAfter300Ms()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Sessions = HostSessions.Scripted(r => r.FinishDelay = TimeSpan.FromSeconds(2)));
            yield return StartFree();
            yield return Advance(3);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            H.Advance(0.2);
            yield return Frames();
            Assert.That(Hud.Controls.ShowsSpinner, Is.False, "antes de 300 ms, sin spinner (DN4a-20)");
            H.Advance(0.2);
            yield return Frames();
            Assert.That(Hud.Controls.ShowsSpinner, Is.True, "un cierre de más de 300 ms muestra el spinner en Terminar");
            yield return Advance(2);
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
        }

        [UnityTest, Category("AC-WKT-20-07"), Category("AC-WKT-03-05"), Category("AC-APP-03-11")]
        public IEnumerator RoutineEndsWithTheFinishDialogOpen()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(28);
            yield return Tap("hud-finish");
            var dialog = H.TopDialog;
            Assert.That(dialog, Is.InstanceOf<FinishWorkoutDialog>());
            yield return AdvanceTo(31);
            Assert.That(dialog!.WasConfirmed, Is.False, "se cerró sin efecto, como Cancelar");
            Assert.That(dialog.IsOpen, Is.False);
            yield return Settle();
            Assert.That(H.TopDialog, Is.Null, "ningún diálogo del inmersivo sobre Workout en reposo");
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("¡Entrenamiento completado!"), "sigue el fin automático");
            Assert.That(Inside("toast"), Is.True);
        }

        [UnityTest, Category("AC-WKT-20-07")]
        public IEnumerator FinishTapAfterTheEndDoesNothing()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(29);
            yield return Tap("hud-finish");
            var dialog = H.TopDialog!;
            var confirmAt = H.BoundsOf("dialog-confirm").center;
            // El cuadro del fin (a los 30 s): el diálogo ya está saliendo y un toque en Terminar no hace nada.
            yield return AdvanceTo(30.02);
            Assert.That(dialog.IsOpen, Is.False, "el diálogo se cerró en el cuadro del fin");
            H.TapAt(confirmAt);
            yield return Settle();
            Assert.That(dialog.WasConfirmed, Is.False, "un Terminar después del fin no hace nada");
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("¡Entrenamiento completado!"), "y queda el fin automático, no uno a mano");
            Assert.That(H.Immersive, Is.Null);
        }

        [UnityTest, Category("AC-WKT-20-07"), Category("AC-WKT-02-06")]
        public IEnumerator DialogClosesWhenTheRoutineEndsInBackground()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(25);
            yield return Tap("hud-finish");
            var dialog = H.TopDialog!;
            H.SendToBackground();
            H.BringToForeground(15);
            yield return Settle();
            Assert.That(dialog.WasConfirmed, Is.False);
            Assert.That(H.TopDialog, Is.Null, "el diálogo se cerró antes de que la página salga");
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("¡Entrenamiento completado!"), "el aviso del fin, al volver");
        }

        [UnityTest, Category("AC-WKT-13-03")]
        public IEnumerator BackOpensTheFinishDialogAndEscapeDoesNothing()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That(H.Immersive, Is.Not.Null, "Escape sin diálogo no hace nada");
            Assert.That(H.TopDialog, Is.Null);
            H.PressBack(BackSource.SystemBack);
            yield return Settle();
            Assert.That(H.TopDialog, Is.InstanceOf<FinishWorkoutDialog>(), "Atrás abre la confirmación de terminar");
            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That(H.TopDialog, Is.Null, "con el diálogo abierto, Escape lo cancela");
            Assert.That(H.Immersive, Is.Not.Null);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
        }

        [UnityTest, Category("AC-APP-03-09"), Category("AC-WKT-13-04")]
        public IEnumerator ImmersiveIsStackedLikeTheEditor()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            Assert.That(H.Shell.Pages.CoversShell, Is.True);
            Assert.That(H.WhyNotTappable("tab-routines"), Is.Not.Null, "la barra de pestañas no se toca");
            Assert.That(H.Find("editor-back"), Is.Null, "sin flecha de volver");
            Assert.That(Immersive.Root.resolvedStyle.opacity, Is.EqualTo(1).Within(0.001f), "entró con su fundido");
            var page = Immersive;
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            yield return Frames();
            Assert.That(page.IsExiting, Is.True, "sale");
            Assert.That(Responds("hud-pause"), Is.False, "lo que se está cerrando no responde");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.Shell.Pages.CoversShell, Is.False);
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator TabOrder()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            Assert.That(H.Focused as VisualElement == null || !Immersive.Root.Contains((VisualElement)H.Focused!), Is.True, "abre sin foco");
            foreach (var name in new[] { "hud-finish", "hud-pause", "hud-finish" })
            {
                H.PressTab();
                yield return Frames();
                Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo(name), "Tab → " + name);
            }

            H.PressTab(true);
            yield return Frames();
            Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo("hud-pause"), "Mayús+Tab al revés");
            H.PressEnter();
            yield return Frames();
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Paused), "Enter activa el control enfocado");
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator FocusReturnsOnlyWithTheKeyboard()
        {
            // DT4a-01: al cerrar un diálogo, el foco vuelve a quien lo abrió solo si se venía usando el teclado.
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return Tap("hud-finish");
            yield return Tap("dialog-cancel");
            Assert.That((H.Focused as VisualElement)?.name, Is.Not.EqualTo("hud-finish"), "con el puntero, el foco no vuelve a Terminar");

            H.PressTab();
            yield return Frames();
            Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo("hud-finish"));
            H.PressEnter();
            yield return Settle();
            Assert.That(H.TopDialog, Is.InstanceOf<FinishWorkoutDialog>());
            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo("hud-finish"), "con el teclado, vuelve a Terminar");
        }
    }
}
