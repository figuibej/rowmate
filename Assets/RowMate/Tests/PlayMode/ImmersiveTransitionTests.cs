using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Shell;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // ITT · La entrada "sumergirse" y la salida con la última foto (AC-WKT-13-01, -04, -06 a -10; AC-APP-03-09;
    // 04b-hud-exr/02-tech-design §3.2, §3.3, §3.12): la página negra en 220, el fondo en 220-400, la cascada desde los 400, los
    // controles desde los 840 (al toque y al teclado), todo quieto a los 1120; la salida en 640 sin datos nuevos.
    public sealed class ImmersiveTransitionTests : WorkoutUiTestBase
    {
        private IEnumerator TapFreeWithoutSettling()
        {
            yield return GoToWorkout();
            yield return Frames();
            H.Tap("workout-free");
            yield return Frames();
        }

        [UnityTest, Category("AC-WKT-13-01"), Category("AC-WKT-13-06"), Category("AC-APP-03-09")]
        public IEnumerator EntryCascadeStates()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return TapFreeWithoutSettling();
            Assert.That(H.Immersive, Is.Not.Null, "se abre en el acto (AC-WKT-13-01)");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "la sesión corre desde el toque");
            var page = Immersive;
            H.Advance(0.1);
            yield return Frames();
            Assert.That(page.Root.resolvedStyle.opacity, Is.LessThan(1f), "la página negra todavía entra (220)");
            // It. 5 (QA-V4b-06): el HUD se arma cuando el fondo empieza a aparecer; mientras Workout funde a negro, no existe.
            Assert.That(page.HasView, Is.False, "el HUD todavía no se armó (QA-V4b-06)");
            Assert.That(page.Cascade.BackdropOpacity, Is.LessThan(0.01), "el fondo, después de los 220");
            H.Advance(0.4);
            yield return Frames();
            Assert.That(page.HasView, Is.True, "a los 500 ms el HUD ya está");
            Assert.That(page.Root.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.001f), "la página entera");
            Assert.That(Hud.Controls.resolvedStyle.opacity, Is.LessThan(0.01f), "los controles todavía no (840)");
            Assert.That(Hud.Controls.IsInteractive, Is.False);
            H.Advance(0.4);
            yield return Frames();
            Assert.That(Hud.Controls.IsInteractive, Is.True, "responden desde su tramo (840)");
            yield return Settle();
            Assert.That(page.Cascade.IsRunning, Is.False, "todo quieto a los 1120");
            Assert.That(Hud.TopBar.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.001f));
            Assert.That(Hud.Backdrop.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.001f));
        }

        // It. 5 (QA-V4b-06, AC-WKT-13-15 [Editor-PM]: el orden de la entrada; 05-series-parciales/00-deuda-4b §2): el HUD se arma en
        // el primer cuadro en que el fondo deja el 0 (después de los 220, con la página entera y negra), no en el del toque; nace
        // con las opacidades de la cascada (todo en 0, nada se ve de golpe) y con el estado del motor; los tiempos de la entrada
        // no cambian. Con reducir movimiento, en el acto.
        [UnityTest, Category("AC-WKT-13-15"), Category("AC-WKT-13-06"), Category("QA-V4b-06")]
        public IEnumerator TheHudIsBuiltInTheBlack()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return TapFreeWithoutSettling();
            var page = Immersive;
            var tapAt = H.Scheduler.MonotonicSeconds;
            Assert.That(page.HasView, Is.False, "no en el cuadro del toque");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), "la sesión corre desde el toque igual (AC-WKT-13-01)");
            H.Advance(0.2);
            yield return Frames();
            Assert.That(page.HasView, Is.False, "ni mientras Workout funde a negro");
            H.Advance(0.03);
            Assert.That(page.HasView, Is.True, "en el primer cuadro con el fondo fuera del 0");
            Assert.That(page.Cascade.BackdropOpacity, Is.GreaterThan(0).And.LessThan(0.3), "con la pantalla casi negra");
            yield return Frames();
            Assert.That(page.Root.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.001f), "la página ya entera y negra");
            foreach (var piece in new[] { Hud.TopBar, Hud.Spm, Hud.RightZone, Hud.Band, Hud.VitalsZone, Hud.Controls })
            {
                Assert.That(piece.resolvedStyle.opacity, Is.LessThan(0.01f), piece.name + " nace con la opacidad de la cascada");
            }

            Assert.That(Hud.Controls.IsInteractive, Is.False, "los controles, desde los 840");
            yield return Settle();
            Assert.That(page.Cascade.IsRunning, Is.False);
            Assert.That(Hud.Band.Time.Text, Is.EqualTo(H.HudState!.Clock), "con el estado del motor");
            Assert.That(Hud.Controls.IsInteractive, Is.True);
            // Lo continuo nace con la hora del toque (MotionDriver.StartingAt): el punto en vivo late con la fase de la 4b.
            H.Advance(0.37);
            yield return Frames();
            var since = RowMate.Core.Design.HudTimelines.Ms(H.Scheduler.MonotonicSeconds - tapAt);
            Assert.That(Hud.Free!.Chip.DotMotion.Opacity, Is.EqualTo(RowMate.Core.Design.LiveDot.At(since, false)).Within(0.01), "la fase desde el toque");
        }

        [UnityTest, Category("AC-WKT-13-15"), Category("AC-APP-05-08"), Category("QA-V4b-06")]
        public IEnumerator WithReducedMotionTheHudIsThereAtOnce()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.ReduceMotion = true);
            yield return TapFreeWithoutSettling();
            Assert.That(Immersive.HasView, Is.True, "sin negro: el fondo está en 1 desde el cuadro 0");
            Assert.That(Hud.Controls.IsInteractive, Is.True, "y los controles responden desde el cuadro 0");
        }

        [UnityTest, Category("AC-WKT-13-07"), Category("AC-APP-05-07")]
        public IEnumerator KeyboardDuringTheCascade()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return TapFreeWithoutSettling();
            H.Advance(0.5);
            yield return Frames();
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.Null, "un Tab a los 500 ms no enfoca nada");
            H.PressEnter();
            yield return Frames();
            Assert.That(H.TopDialog, Is.Null, "un Enter no activa nada que no se ve");
            H.Advance(0.4);
            yield return Frames();
            H.PressTab();
            yield return Frames();
            Assert.That(H.Focused, Is.SameAs(Hud.Controls.Finish), "a los 900 ms, el primer Tab enfoca Terminar");
        }

        [UnityTest, Category("AC-WKT-13-07")]
        public IEnumerator BackFromTheFirstFrame()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return TapFreeWithoutSettling();
            H.PressBack(BackSource.SystemBack);
            yield return Frames();
            Assert.That(H.TopDialog, Is.InstanceOf<RowMate.Presentation.Workout.FinishWorkoutDialog>(), "Atrás abre la confirmación desde el cuadro 0");
        }

        [UnityTest, Category("AC-WKT-13-08"), Category("AC-WKT-13-04")]
        public IEnumerator ExitKeepsTheLastPhoto()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            yield return AdvanceTo(12.2);
            yield return Tap("hud-finish");
            yield return Frames();
            var hud = Hud;
            var page = Immersive;
            H.Tap("dialog-confirm");
            yield return Frames();
            // Con la base en línea el cierre vuelve en el acto: a reposo, y la página sale con la última foto.
            for (var i = 0; i < 30; i++)
            {
                H.Step();
                Assert.That(hud.Band.Time.Text, Is.Not.EqualTo("00:00"), "nunca 00:00 en la salida");
                Assert.That(hud.Free!.Chip.Label.text, Is.Not.EqualTo("—"), "nunca el chip —");
            }

            Assert.That(page.IsExiting, Is.True);
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "la página salió");
            Assert.That(H.Shell.Pages.HasPage(page), Is.False);
        }

        // §3.12: después de la salida, el MotionDriver y las demandas de 60 fps vuelven a los de reposo, y ninguna pieza que mide
        // texto pide la escala del panel fuera de él (0 avisos de DPI en el registro, QA-V4a-05). QA-V4b-MOT-05: a mano también con
        // las piezas nuevas a la vista en el cuadro de Terminar (el chip y el punto en el libre; el sello y PAUSA; la cuenta
        // grande, el halo y Próximo). La vuelta del segundo plano ya en reposo la prueba IHT.LeavingTheImmersiveStopsItsContinuousMotions.
        [UnityTest, Category("AC-WKT-13-09")]
        public IEnumerator NothingAnimatesAfterTheExit()
        {
            var warnings = new System.Collections.Generic.List<string>();
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (message.IndexOf("DPI", StringComparison.Ordinal) >= 0)
                {
                    warnings.Add(message);
                }
            };
            Application.logMessageReceived += onLog;
            try
            {
                foreach (var exit in new[] { "a mano", "a mano con el sello y PAUSA", "a mano con la cuenta grande", "Terminar del diálogo", "Terminar durante el conteo" })
                {
                    yield return ExitAndCheck(exit);
                }

                yield return Advance(0.5);
                Assert.That(warnings, Is.Empty, "ningún aviso de DPI: " + string.Join(" | ", warnings));
            }
            finally
            {
                Application.logMessageReceived -= onLog;
            }
        }

        private IEnumerator ExitAndCheck(string exit)
        {
            var manual = exit.StartsWith("a mano", StringComparison.Ordinal);
            CreateWorkout(exit == "a mano" ? RoutineSeeds.Lista : manual ? RoutineSeeds.F4B : RoutineSeeds.F4A);
            yield return GoToWorkout();
            var restCount = H.Handle.Motion.ActiveCount;
            var restDemands = H.Handle.Pacing.Demands;
            if (exit == "a mano")
            {
                yield return StartFree();
                yield return AdvanceTo(20.2);
                Assert.That(H.IsShown("hud-phase-chip") && H.IsShown("hud-phase-dot"), Is.True, exit + ": el chip y el punto a la vista");
                yield return Tap("hud-finish");
                yield return Frames();
                H.Tap("dialog-confirm");
            }
            else if (manual)
            {
                var seal = exit == "a mano con el sello y PAUSA";
                yield return StartRoutine("Estados");
                yield return AdvanceTo(seal ? 10.6 : 7.6);
                if (seal)
                {
                    Assert.That(Hud.Steps!.Seal.IsShowing, Is.True, exit + ": el sello a la vista");
                    yield return Tap("hud-pause");
                    Assert.That(H.IsShown("hud-paused-label"), Is.True, exit + ": PAUSA a la vista");
                    Assert.That(Hud.Steps!.Seal.IsShowing, Is.True, exit + ": y el sello");
                }
                else
                {
                    Assert.That(H.IsShown("hud-big-countdown"), Is.True, exit + ": la cuenta grande a la vista");
                    Assert.That(H.IsShown("hud-up-next"), Is.True, exit + ": y Próximo");
                }

                yield return Tap("hud-finish");
                yield return Frames();
                H.Tap("dialog-confirm");
            }
            else
            {
                yield return StartRoutine("Corta");
                yield return AdvanceTo(exit == "Terminar del diálogo" ? 32 : 30.6);
                Assert.That(H.Completion, Is.Not.Null, exit + ": el diálogo");
                if (exit == "Terminar durante el conteo")
                {
                    Assert.That(H.Completion!.Motion.IsRunning, Is.True, exit + ": cuenta");
                }

                H.Tap("completion-finish");
            }

            yield return Settle();
            yield return Advance(1);
            Assert.That(H.Immersive, Is.Null, exit + ": salió");
            Assert.That(H.Handle.Motion.ActiveCount, Is.EqualTo(restCount), exit + ": el MotionDriver vuelve a lo de reposo");
            Assert.That(H.Handle.Pacing.Demands, Is.EqualTo(restDemands), exit + ": ninguna demanda de 60 fps de más");
        }

        // It. 5 (05-series-parciales/02-tech-design §3.3, §4.4; AC-WKT-13-06, -13-09, -18-01; VID-06): los paneles nacen con el HUD, en su
        // estado guardado y con el primer paso (DV-48), y entran con el tramo de SPM (520-800, opacidad y +8·sp): nada aparece de golpe
        // después de la cascada.
        [UnityTest, Category("AC-WKT-13-06"), Category("AC-WKT-18-01")]
        public IEnumerator ThePanelsComeWithSpmInTheCascade()
        {
            CreateWorkout(RoutineSeeds.F5, o =>
            {
                var prefs = new RowMate.Core.Common.MemoryPreferences();
                prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.LapsKey, true);
                prefs.SetBool(RowMate.App.Workout.SeriesPanelPrefs.RepsKey, true);
                o.Preferences = prefs;
            });
            yield return OpenStartDialog("Cinco reps");
            yield return Frames();
            H.Tap("dialog-confirm");
            var start = H.Scheduler.MonotonicSeconds;
            var built = false;
            while (H.Scheduler.MonotonicSeconds - start < 1.0)
            {
                H.Step();
                yield return null;
                if (!Immersive.HasView)
                {
                    continue;
                }

                var column = Hud.Series!;
                if (!built)
                {
                    built = true;
                    Assert.That(column.Laps.Motion.Expanded && column.Reps.Motion.Expanded, Is.True, "los dos abiertos desde que nacen");
                    Assert.That(column.RepsShown, Is.True, "Cinco reps empieza con la serie (DV-48)");
                    Assert.That(H.TextOf("hud-laps-current"), Is.EqualTo("0:00.0"), "el primer paso ya en el primer cuadro (DV-48)");
                    Assert.That(H.TextOf("hud-rep-progress"), Is.EqualTo("Rep 1/5 · 0 m"));
                }

                var spm = Hud.Spm.resolvedStyle.opacity;
                Assert.That(column.Laps.resolvedStyle.opacity, Is.EqualTo(spm).Within(0.002f), "parciales con el tramo de SPM");
                Assert.That(column.Reps.resolvedStyle.opacity, Is.EqualTo(spm).Within(0.002f), "repeticiones con el tramo de SPM");
                Assert.That(column.Laps.VisualHeightBase, Is.EqualTo(column.Laps.FinalHeightBase).Within(1e-6), "sin abrirse ni cerrarse");
            }

            Assert.That(built, Is.True);
            Assert.That(Hud.Series!.Laps.resolvedStyle.opacity, Is.EqualTo(1).Within(1e-4));
        }

        // It. 5 (AC-WKT-13-08, -13-09; ROB-02; §4.4): la salida se lleva la última foto de los paneles y nada sigue animando: con el
        // resorte de un panel en curso (el toque 200 ms antes de Terminar) y con la marca de una vuelta encendida.
        [UnityTest, Category("AC-WKT-13-09"), Category("AC-WKT-13-08"), Category("AC-WKT-18-20")]
        public IEnumerator NothingAnimatesAfterTheExitWithThePanels()
        {
            foreach (var marker in new[] { false, true })
            {
                CreateWorkout(RoutineSeeds.F5, o => o.Preferences = new RowMate.Core.Common.MemoryPreferences());
                yield return GoToWorkout();
                var restCount = H.Handle.Motion.ActiveCount;
                var restDemands = H.Handle.Pacing.Demands;
                // LM5-05: el State de la preferencia es uno por app; un HUD que quedara suscripto quedaría retenido entero.
                var restSubscribers = H.App.SeriesPanels.SubscriberCount;
                yield return StartRoutine("Series 600");
                Assert.That(H.App.SeriesPanels.SubscriberCount, Is.GreaterThan(restSubscribers), "el HUD sigue la preferencia mientras está");
                if (marker)
                {
                    H.TapText("500 m");
                    yield return Settle();
                    yield return AdvanceTo(155.3);
                    Assert.That(Hud.Series!.Laps.Visual.MarkerAlpha0, Is.GreaterThan(0.1), "la marca de #1 encendida");
                }
                else
                {
                    yield return AdvanceTo(60.2);
                }

                if (!marker)
                {
                    // El toque 200 ms antes de Terminar (el resorte sigue en curso).
                    H.TapText("500 m");
                    H.Advance(0.2);
                    Assert.That(Hud.Series!.Laps.Motion.IsRunning, Is.True, "el resorte en curso");
                }

                yield return Frames();
                H.Tap("hud-finish");
                yield return Frames(3);

                var current = H.TextOf("hud-laps-chip");
                H.Tap("dialog-confirm");
                for (var i = 0; i < 12; i++)
                {
                    H.Step();
                    yield return null;
                    if (H.Immersive != null && H.Immersive.HasView)
                    {
                        Assert.That(H.TextOf("hud-laps-chip"), Is.EqualTo(current), "la última foto");
                    }
                }

                yield return Settle();
                yield return Advance(1);
                Assert.That(H.Immersive, Is.Null, "salió");
                Assert.That(H.Handle.Motion.ActiveCount, Is.EqualTo(restCount), (marker ? "con la marca" : "con el resorte") + ": el MotionDriver vuelve a lo de reposo");
                Assert.That(H.Handle.Pacing.Demands, Is.EqualTo(restDemands), "ninguna demanda de 60 fps de más");
                Assert.That(H.App.SeriesPanels.SubscriberCount, Is.EqualTo(restSubscribers), "y el HUD que salió ya no está suscripto (LM5-05)");
            }
        }

        // QA-V4b-MOT-02: en los 400 ms en negro de la salida (el HUD se va en 0-200 y el fondo en 200-400) Workout todavía no se ve,
        // así que no recibe toques: el inmersivo que sale los absorbe sin actuar (AC-WKT-13-08: lo que sale no responde) y los
        // suelta cuando empieza a verse Workout (RevealStarted). Desde ahí vale el toque de ROB-01 (AC-WKT-01-09, el OK del PO).
        // Antes, el segundo toque de un doble toque en Terminar del diálogo (después de los 150 ms del velo) caía en la lista de
        // rutinas de abajo y abría ¿Empezar…?, o empezaba un libre.
        [UnityTest, Category("AC-WKT-13-08"), Category("AC-WKT-01-09")]
        public IEnumerator TouchesDuringTheBlackExitDoNotReachWorkout()
        {
            CreateWorkout(RoutineSeeds.F4A);
            yield return StartRoutine("Corta");
            yield return AdvanceTo(30.4);
            Assert.That(H.Completion, Is.Not.Null, "el diálogo de completado");
            var page = Immersive;
            H.Tap("completion-finish");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), "Terminar: a reposo en el acto");
            // Pasaron los 150 ms del velo del diálogo que se cierra (DT-21): la pantalla está negra.
            H.Advance(0.2);
            yield return Frames();
            Assert.That(page.IsExiting && !page.HasRevealed, Is.True, "en el negro de la salida");
            Assert.That(H.TopDialog, Is.Null, "el diálogo ya se fue");
            var picked = H.TapThrough("workout-free");
            Assert.That(picked != null && (ReferenceEquals(picked, page.Root) || page.Root.Contains(picked)), Is.True,
                "el toque lo recibe el inmersivo que sale: " + (picked?.name ?? "nadie"));
            var card = Screen.List.CardAt(0)!;
            H.TapAt(card.worldBound.center);
            yield return Frames();
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), "no empieza un libre");
            Assert.That(H.TopDialog, Is.Null, "ni ¿Empezar…?");
            Assert.That(H.WhyNotTappable("workout-free"), Is.Not.Null, "tapado por el negro");

            // A los 400 empieza a verse Workout: el toque ya vale (ROB-01).
            H.Advance(0.22);
            yield return Frames();
            Assert.That(page.HasRevealed && H.Shell.Pages.IsExiting(page), Is.True, "se empieza a ver Workout y la página todavía sale");
            Assert.That(H.WhyNotTappable("workout-free"), Is.Null, "desde RevealStarted, Workout responde");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null, "la página salió");
            Assert.That(H.TopDialog, Is.Null, "ningún diálogo sobre Workout en reposo");
            Assert.That(SessionCount(), Is.EqualTo(1), "una sola sesión, la de Corta");
        }

        [UnityTest, Category("AC-WKT-13-08"), Category("AC-APP-03-11")]
        public IEnumerator EndToastsAtTheReveal()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.Sessions = HostSessions.Scripted(r => r.FailOnFinish = true));
            yield return StartFree();
            yield return AdvanceTo(10.2);
            yield return Tap("hud-finish");
            yield return Frames();
            H.Tap("dialog-confirm");
            yield return Frames();
            H.Advance(0.2);
            yield return Frames();
            Assert.That(H.Shell.Toasts.IsVisible, Is.False, "sobre el negro, todavía nada");
            yield return Settle();
            Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("No se pudo guardar la sesión"), "el aviso cuando empieza a verse Workout");
        }
    }
}
