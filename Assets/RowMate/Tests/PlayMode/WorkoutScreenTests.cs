using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Workout;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Placeholder;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Workout;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // WSC · Workout en reposo y el diálogo de inicio (F-WKT-12, F-BLE-10; DV-21, DV-22, DV-23, DV-36; 04a-motor-hud-funcional/
    // 02-tech-design §6.1, §6.2; 04-design-notes §4.1). El remo de prueba (R-4) conectado salvo que se diga otra cosa.
    public sealed class WorkoutScreenTests : WorkoutUiTestBase
    {
        private static readonly string[] ListaNames = { "Escalera SPM", "Pirámide 500", "Solo calentamiento", "Intervalos 4×" };

        [UnityTest, Category("AC-APP-02-08"), Category("AC-WKT-12-01")]
        public IEnumerator WorkoutIsNotAPlaceholder()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            var screen = H.Find(WorkoutScreenView.ScreenName)!;
            Assert.That(screen, Is.InstanceOf<WorkoutScreenView>());
            Assert.That(screen.Q(className: "rm-placeholder__icon"), Is.Null, "sin el ícono provisorio");
            Assert.That(H.Find("screen-history"), Is.InstanceOf<PlaceholderScreenView>(), "Historial sigue provisoria");
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(1), "destino 1");
            Assert.That(H.IsTextVisible("Entrenamiento"), Is.True, "workoutTitle");
            Assert.That(Screen.AppBar.Actions.childCount, Is.EqualTo(0), "sin acciones");
        }

        [UnityTest, Category("AC-WKT-12-02"), Category("AC-WKT-12-03")]
        public IEnumerator ContentInOrder()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            var window = H.Root.worldBound;
            var free = Bounds("workout-free");
            var label = Bounds("workout-routines-label");
            var first = Bounds("workout-routine-0");
            Assert.That(free.xMin - window.xMin, Is.EqualTo(16).Within(0.6f), "de 16");
            Assert.That(window.xMax - free.xMax, Is.EqualTo(16).Within(0.6f), "a 16");
            Assert.That(free.height, Is.GreaterThanOrEqualTo(55.5f), "56 de alto");
            Assert.That(label.yMin - free.yMax, Is.EqualTo(24).Within(0.6f), "24 entre el botón y la etiqueta");
            Assert.That(first.yMin, Is.GreaterThan(label.yMax - 0.5f), "la lista debajo de la etiqueta");
            Assert.That(H.IsTextVisible("Escenario"), Is.False, "sin workoutScene en la 4a (DV-34)");
            Assert.That(H.TextOf("workout-free"), Is.EqualTo("Entrenamiento libre"));
            Assert.That(Screen.FreeButton.Icon!.Codepoint, Is.EqualTo(RowMate.Core.Design.Icons.PlayArrow));

            // Sin remo, el aviso va arriba de todo.
            H.ScriptedRower!.Disconnect();
            yield return Settle();
            Assert.That(Bounds("workout-banner").yMax, Is.LessThanOrEqualTo(Bounds("workout-free").yMin + 0.5f), "el aviso arriba del botón");
        }

        [UnityTest, Category("AC-WKT-12-04"), Category("AC-RUT-10-11")]
        public IEnumerator RoutineListLikeRoutines()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            Assert.That(H.TextOf("workout-routines-label"), Is.EqualTo("  O elegí una rutina:"), "con los dos espacios del legacy");
            Assert.That(Screen.List.Count, Is.EqualTo(4));
            var segments = new[] { 8, 14, 1, 7 };
            for (var i = 0; i < 4; i++)
            {
                var card = Screen.List.CardAt(i)!;
                Assert.That(card.NameLabel.text, Is.EqualTo(ListaNames[i]), "orden de la colección");
                Assert.That(card.Bar.SegmentCount, Is.EqualTo(segments[i]), "la misma mini-barra que en Rutinas");
                Assert.That(card.Chevron, Is.Not.Null, "chevron en lugar del ⋮");
                Assert.That(card.Q<IconButton>(), Is.Null, "sin ⋮");
                Assert.That(card.name, Is.EqualTo("workout-routine-" + i));
            }

            // Prueba 4a: 6 tramos (amarillo, rosa, verde, rosa, verde, azul).
            CreateWorkout(RoutineSeeds.F4A);
            yield return GoToWorkout();
            Assert.That(Screen.List.CardAt(0)!.NameLabel.text, Is.EqualTo("Prueba 4a"));
            Assert.That(Screen.List.CardAt(0)!.Bar.SegmentCount, Is.EqualTo(6));
        }

        [UnityTest, Category("AC-WKT-12-05")]
        public IEnumerator SharedCollection()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return Tap("tab-routines");
            yield return Tap("routine-menu-1");
            yield return Tap("routine-menu-delete");
            yield return Tap("dialog-confirm");
            yield return Settle();
            yield return GoToWorkout();
            Assert.That(Screen.List.Count, Is.EqualTo(3), "Pirámide 500 ya no está");
            Assert.That(H.IsShown("workout-loading"), Is.False, "sin indicador: la misma colección, sin volver a leer");
            for (var i = 0; i < 3; i++)
            {
                Assert.That(Screen.List.CardAt(i)!.NameLabel.text, Is.Not.EqualTo("Pirámide 500"));
            }
        }

        [UnityTest, Category("AC-WKT-12-06")]
        public IEnumerator LoadingErrorAndEmpty()
        {
            CreateWorkout(null, o => o.Routines = HostRoutines.Scripted(r => r.LoadDelay = ScriptedRoutineRepository.Never));
            yield return GoToWorkout();
            yield return Advance(0.3);
            Assert.That(H.IsShown("workout-loading"), Is.True, "cargando: el indicador (después de 120 ms, con fundido)");
            Assert.That(H.IsShown("workout-empty"), Is.False);
            Assert.That(H.WhyNotTappable("workout-free"), Is.Null, "el botón sigue");

            CreateWorkout(null, o => o.Routines = HostRoutines.Rota());
            yield return GoToWorkout();
            Assert.That(H.IsShown("workout-error"), Is.True);
            Assert.That(H.IsTextVisible("No se pudieron cargar las rutinas"), Is.True);
            Assert.That(H.TextOf("workout-retry"), Is.EqualTo("Reintentar"));
            Assert.That(Bounds("workout-retry").yMin, Is.GreaterThan(Bounds("workout-free").yMax));

            CreateWorkout(RoutineSeeds.Empty);
            yield return GoToWorkout();
            Assert.That(H.TextOf("workout-empty"), Is.EqualTo("Creá una rutina en la pestaña \"Rutinas\""));
            Assert.That(H.IsShown("workout-routines-label"), Is.False, "sin etiqueta");
        }

        // Hallazgo de la corrección de QA-V4a-02 (02-tech-design, "Cambios por verificación"): los indicadores de carga de Workout
        // y de Rutinas (Spinner, una animación continua) giraban en el MotionDriver desde que se armaba la pantalla aunque su
        // estado estuviera oculto, así que la app nunca bajaba a 30 fps en reposo (ARCHITECTURE §13.1). Giran solo a la vista.
        [UnityTest, Category("AC-WKT-12-06")]
        public IEnumerator LoadingSpinnersSpinOnlyWhileShown()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            Assert.That(H.Handle.Motion.Contains(Screen.Spinner), Is.False, "con la lista cargada, el indicador de Workout no gira");
            Assert.That(H.Handle.Motion.Contains(H.RoutinesScreen!.Spinner), Is.False, "el de Rutinas tampoco");
            Assert.That(H.Handle.Motion.ActiveCount, Is.EqualTo(0), "en reposo, ninguna animación");
            Assert.That(H.Handle.Pacing.Demands, Is.EqualTo(0), "en reposo, ninguna demanda de 60 fps");

            // Una carga de 5 s: el indicador gira mientras se ve y deja de girar cuando la lista llega.
            CreateWorkout(RoutineSeeds.Lista, o => o.Routines = HostRoutines.Scripted(r => r.LoadDelay = TimeSpan.FromSeconds(5)));
            yield return GoToWorkout();
            yield return Advance(0.3);
            Assert.That(H.IsShown("workout-loading"), Is.True, "cargando");
            Assert.That(H.Handle.Motion.Contains(Screen.Spinner), Is.True, "a la vista, gira");
            yield return Advance(5);
            yield return Settle();
            Assert.That(H.IsShown("workout-loading"), Is.False, "cargó");
            Assert.That(H.Handle.Motion.Contains(Screen.Spinner), Is.False, "y deja de girar");
            Assert.That(H.Handle.Motion.Contains(H.RoutinesScreen!.Spinner), Is.False, "el de Rutinas también");
        }

        [UnityTest, Category("AC-WKT-12-07"), Category("AC-APP-03-10")]
        public IEnumerator StartDialog()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return OpenStartDialog("Escalera SPM");
            Assert.That(H.TopDialog, Is.InstanceOf<StartWorkoutDialog>());
            Assert.That(H.TextOf("dialog-title"), Is.EqualTo("Escalera SPM"), "el nombre de la rutina como título");
            Assert.That(H.TextOf("dialog-message"), Is.EqualTo("1 serie · 1 descanso\n\n¿Iniciar el entrenamiento?"));
            Assert.That(H.TextOf("dialog-cancel"), Is.EqualTo("Cancelar"));
            Assert.That(H.TextOf("dialog-confirm"), Is.EqualTo("Iniciar"));
            yield return Tap("dialog-cancel");
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), "Cancelar no inicia nada");

            // Tocar afuera es cancelar (AC-APP-03-10).
            yield return OpenStartDialog("Escalera SPM");
            H.TapScrim();
            yield return Settle();
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));

            yield return StartRoutine("Escalera SPM");
            Assert.That(H.Immersive, Is.Not.Null, "Iniciar abre el inmersivo");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
            Assert.That(Immersive.Hud.State.Value.IsRoutine, Is.True);
        }

        [UnityTest, Category("AC-WKT-12-08"), Category("AC-RUT-06-08")]
        public IEnumerator RoutineThatCannotStart()
        {
            foreach (var name in new[] { "Serie enorme", "Vatios imposibles", "Serie ×0" })
            {
                CreateWorkout(RoutineSeeds.Rara4a(name));
                yield return GoToWorkout();
                Assert.That(Screen.List.Count, Is.EqualTo(1), name + ": la lista carga igual");
                H.TapRoutineNamed(name);
                yield return Settle();
                Assert.That(H.TopDialog, Is.Null, name + ": sin diálogo de inicio");
                Assert.That(H.Shell.Toasts.CurrentText, Is.EqualTo("Esta rutina no se puede iniciar: revisá sus pasos en Rutinas"), name);
                Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));
                Assert.That(SessionCount(), Is.EqualTo(0), name + ": ninguna sesión");
            }
        }

        [UnityTest, Category("AC-WKT-12-09")]
        public IEnumerator ScrollsAsOne()
        {
            CreateWorkout(RoutineSeeds.ListaPlus(26), o =>
            {
                o.Profile = DeviceProfile.L640;
                o.TextScale = 1.3;
            });
            H.ScriptedRower!.Disconnect();
            yield return GoToWorkout();
            Assert.That(H.IsShown("workout-banner"), Is.True);
            Assert.That(UiQuery.ScrollViewOf(H.Find("workout-banner")!), Is.SameAs(Screen.Scroll), "el aviso se desplaza con todo");
            Assert.That(UiQuery.ScrollViewOf(H.Find("workout-free")!), Is.SameAs(Screen.Scroll), "el botón también");
            H.ScrollToEnd("workout-scroll");
            yield return Frames(3);
            yield return Settle();
            var last = Bounds("workout-routine-29");
            Assert.That(Inside("workout-routine-29"), Is.True, "se llega a la última");
            Assert.That(last.yMax, Is.LessThanOrEqualTo(Bounds("tab-bar").yMin + 0.5f), "por encima de la barra");
            AssertClean(H);
            var offset = H.ScrollOffsetOf("workout-scroll");
            yield return Tap("tab-device");
            yield return Tap("tab-workout");
            Assert.That(H.ScrollOffsetOf("workout-scroll"), Is.EqualTo(offset).Within(0.5f), "la posición se conserva");
        }

        [UnityTest, Category("AC-BLE-10-03")]
        public IEnumerator NoRowerBannerInEveryState()
        {
            foreach (var status in new[] { BleStatus.Off, BleStatus.Scanning, BleStatus.Connecting, BleStatus.Disconnected })
            {
                CreateWorkout(RoutineSeeds.Lista);
                yield return GoToWorkout();
                Assert.That(H.IsShown("workout-banner"), Is.False, "conectado: sin aviso");
                H.ScriptedRower!.SetStatus(status);
                yield return Settle();
                Assert.That(H.IsShown("workout-banner"), Is.True, status + ": el aviso");
                Assert.That(H.TextOf("workout-banner-text"), Is.EqualTo("Conectá el rower para iniciar un entrenamiento"), status.ToString());
                H.ScriptedRower.SetStatus(BleStatus.Connected);
                yield return Settle();
                Assert.That(H.IsShown("workout-banner"), Is.False, status + " → conectado: se va");
            }
        }

        [UnityTest, Category("AC-BLE-10-04"), Category("AC-RUT-10-11")]
        public IEnumerator StartDisabledWithoutRower()
        {
            CreateWorkout(RoutineSeeds.Lista);
            H.ScriptedRower!.Disconnect();
            yield return GoToWorkout();
            Assert.That(H.Find("workout-free")!.resolvedStyle.opacity, Is.EqualTo(0.38f).Within(0.01f), "al 38 %");
            Assert.That(Responds("workout-free"), Is.False, "no responde");
            Assert.That(H.Find("workout-routines")!.resolvedStyle.opacity, Is.EqualTo(0.5f).Within(0.01f), "las tarjetas al 50 %");
            Assert.That(Responds("workout-routine-0"), Is.False, "y no responden");
            H.TapUnchecked("workout-free");
            H.TapUnchecked("workout-routine-0");
            yield return Settle();
            Assert.That(H.Immersive, Is.Null);
            Assert.That(H.TopDialog, Is.Null);
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));
        }

        [UnityTest, Category("AC-BLE-10-06")]
        public IEnumerator ConnectionCheckedOnlyOnTap()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return OpenStartDialog("Escalera SPM");
            H.ScriptedRower!.Disconnect();
            yield return Settle();
            Assert.That(H.TopDialog, Is.Not.Null, "el diálogo sigue abierto");
            yield return Tap("dialog-confirm");
            Assert.That(H.Immersive, Is.Not.Null, "la rutina se inicia igual (paridad)");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active));
        }

        [UnityTest, Category("AC-WKT-01-09")]
        public IEnumerator TabShowsOnlyInProgressUnderTheImmersive()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return StartFree();
            Assert.That(Screen.InProgress.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "workoutInProgress debajo");
            Assert.That(Screen.InProgress.text, Is.EqualTo("Entrenamiento en curso..."));
            Assert.That(SameColor(Screen.InProgress.resolvedStyle.color, RowMate.Core.Design.Tokens.TextTertiary), Is.True, "en text.tertiary (DV-36)");
            Assert.That(Screen.AppBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "sin la barra superior fuera de reposo");
            Assert.That(H.Find("workout-rest")!.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "sin la vista de reposo");
            yield return Tap("hud-pause");
            Assert.That(Screen.AppBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "en pausa, tampoco");
        }

        [UnityTest, Category("AC-WKT-01-10")]
        public IEnumerator OneTapOneSession()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            yield return Frames();
            H.Tap("workout-free");
            H.TapUnchecked("workout-free");
            yield return Settle();
            Assert.That(H.Immersive, Is.Not.Null);
            yield return Advance(1);
            Assert.That(SessionCount(), Is.EqualTo(1), "una sesión");
            Assert.That(H.Find("page-stack")!.Query<VisualElement>("immersive").ToList().Count, Is.EqualTo(1), "un inmersivo");

            CreateWorkout(RoutineSeeds.Lista);
            yield return OpenStartDialog("Escalera SPM");
            yield return Frames();
            var confirmAt = H.BoundsOf("dialog-confirm").center;
            H.Tap("dialog-confirm");
            H.TapAt(confirmAt);
            yield return Settle();
            yield return Advance(1);
            Assert.That(SessionCount(), Is.EqualTo(1), "dos toques en Iniciar: una sesión");
        }

        // QA-V4a-01 = QA-V4a-ROB-01 (05-qa-lens-robustez §4.1): "Entrenamiento libre" tocado durante los 200 ms en que sale el
        // inmersivo de la sesión anterior (120 con reducir movimiento). ExitLock ya dejó inerte la página que sale y la vista de
        // reposo responde debajo; la sesión nueva tiene que tener SU inmersivo en el mismo toque, con Pausa, Terminar y "Atrás":
        // la página que sale no cuenta como el inmersivo de la pestaña. Legacy: cada inicio apila su propia ruta en el toque
        // (lib/features/workout/workout_screen.dart:88-89, :164-169).
        [UnityTest, Category("AC-WKT-13-01"), Category("AC-WKT-13-04"), Category("AC-WKT-13-10"), Category("AC-WKT-01-09")]
        public IEnumerator StartDuringThePreviousExitHasItsOwnImmersive()
        {
            foreach (var reduceMotion in new[] { false, true })
            {
                var label = reduceMotion ? "con reducir movimiento: " : "";
                CreateWorkout(RoutineSeeds.F4A, o =>
                {
                    o.ReduceMotion = reduceMotion;
                    if (reduceMotion)
                    {
                        o.Sessions = HostSessions.Scripted(r => r.FinishDelay = TimeSpan.FromSeconds(1));
                    }
                });
                yield return StartRoutine("Corta");
                var old = Immersive;
                if (!reduceMotion)
                {
                    yield return AdvanceTo(30.02); // fin automático a los 30 s; con la base en línea el cierre vuelve en el acto
                    // It. 4b (DT4a-15): terminada sola, espera al diálogo de completado (a los 300 ms); su Terminar la vuelve a reposo.
                    Assert.That(Phase, Is.EqualTo(WorkoutPhase.Finished), label + "la Corta terminó sola y espera el diálogo");
                    yield return AdvanceTo(30.4);
                    Assert.That(H.Completion, Is.Not.Null, label + "el diálogo de completado");
                    H.Tap("completion-finish");
                    Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), label + "Terminar: a reposo en el acto (el cierre ya volvió)");
                    // El velo del diálogo que sale se traga los toques sus 150 ms (DT-21), y el inmersivo que sale, los de su negro
                    // hasta que empieza a verse Workout (QA-V4b-MOT-02: 400 ms). Desde ahí vale el toque; la página todavía sale (640).
                    H.Advance(0.2);
                    yield return Frames();
                    Assert.That(H.WhyNotTappable("workout-free"), Is.Not.Null, label + "en el negro, Workout no recibe toques");
                    H.Advance(0.25);
                    yield return Frames();
                    Assert.That(old.HasRevealed, Is.True, label + "ya se empieza a ver Workout");
                }
                else
                {
                    // Con reducir movimiento la salida del inmersivo dura 120, menos que los 150 en que el velo de un diálogo que se
                    // cierra se traga los toques (DT-21): la salida que se toca es la que sigue a un cierre lento (Terminar →
                    // Terminar, y la base vuelve 1 s después), con el diálogo ya ido.
                    yield return AdvanceTo(10.2);
                    yield return Tap("hud-finish");
                    H.Tap("dialog-confirm");
                    H.Advance(0.5);
                    yield return Frames();
                    Assert.That(Phase, Is.EqualTo(WorkoutPhase.Finished), label + "terminando: la base todavía no volvió");
                    Assert.That(H.TopDialog, Is.Null, label + "el diálogo de terminar ya se fue");
                    H.Advance(0.55); // el cierre vuelve al segundo: a reposo, y el inmersivo empieza a salir (120)
                    yield return Frames();
                    Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle), label + "a reposo cuando vuelve el cierre");
                }
                Assert.That(old.IsExiting && H.Shell.Pages.IsExiting(old), Is.True, label + "su inmersivo está saliendo");
                Assert.That(Screen.Immersive, Is.Null, label + "la página que sale ya no es el inmersivo de la pestaña");
                H.Tap("workout-free"); // la vista de reposo responde debajo (ExitLock)
                Assert.That(Phase, Is.EqualTo(WorkoutPhase.Active), label + "la sesión nueva arranca en el toque");
                var page = H.Immersive;
                Assert.That(page, Is.Not.Null, label + "la sesión nueva tiene su inmersivo en el mismo toque (AC-WKT-13-01)");
                Assert.That(page, Is.Not.SameAs(old), label + "uno nuevo, no el que sale");
                Assert.That(Screen.Immersive, Is.SameAs(page), label);
                yield return Settle();
                Assert.That(H.Shell.Pages.HasPage(old), Is.False, label + "la página vieja terminó de salir");
                Assert.That(H.Immersive, Is.SameAs(page), label + "y la nueva sigue arriba");
                Assert.That(H.Find("page-stack")!.Query<VisualElement>("immersive").ToList().Count, Is.EqualTo(1), label + "un solo inmersivo");
                Assert.That(Hud.Free, Is.Not.Null, label + "el HUD del libre");
                Assert.That(H.Shell.Pages.CoversShell, Is.True, label + "tapa las pestañas");
                Assert.That(Screen.InProgress.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), label + "debajo, solo workoutInProgress (AC-WKT-01-09)");
                Assert.That(Responds("hud-pause"), Is.True, label + "Pausa responde");
                Assert.That(Responds("hud-finish"), Is.True, label + "Terminar responde");
                H.PressBack(BackSource.SystemBack);
                yield return Settle();
                Assert.That(H.TopDialog, Is.InstanceOf<FinishWorkoutDialog>(), label + "Atrás abre la confirmación de terminar");
                yield return Tap("dialog-cancel");
                yield return Tap("hud-pause");
                Assert.That(Phase, Is.EqualTo(WorkoutPhase.Paused), label + "Pausa pausa la sesión nueva");
            }
        }

        [UnityTest, Category("AC-APP-01-09")]
        public IEnumerator RoutinesLoadedAtBoot()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return Settle();
            yield return Tap("tab-workout");
            Assert.That(H.IsShown("workout-loading"), Is.False, "sin indicador");
            Assert.That(Screen.List.Count, Is.EqualTo(4), "la lista está, sin haber visitado Rutinas");
        }

        [UnityTest, Category("AC-APP-05-05")]
        public IEnumerator TextScale130()
        {
            CreateWorkout(RoutineSeeds.Lista, o => o.TextScale = 1.3);
            yield return GoToWorkout();
            AssertClean(H);
            yield return OpenStartDialog("Escalera SPM");
            AssertClean(H);
            yield return Tap("dialog-confirm");
            var size = Hud.Spm.Number.resolvedStyle.fontSize;
            Assert.That(size, Is.EqualTo(Hud.Spm.ValueSizeDp * Hud.Spm.Fit).Within(0.5f), "el HUD no cambia con la letra del sistema");
            yield return Tap("hud-finish");
            AssertClean(H);
        }

        [UnityTest, Category("AC-APP-05-07")]
        public IEnumerator TabOrder()
        {
            CreateWorkout(RoutineSeeds.Lista);
            yield return GoToWorkout();
            H.Find("workout-free")!.Focus();
            yield return Frames();
            Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo("workout-free"));
            H.PressTab();
            yield return Frames();
            Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo("workout-routine-0"), "Tab: de Entrenamiento libre a la primera tarjeta");
            H.PressTab();
            yield return Frames();
            Assert.That((H.Focused as VisualElement)?.name, Is.EqualTo("workout-routine-1"));
            H.PressEnter();
            yield return Settle();
            Assert.That(H.TopDialog, Is.InstanceOf<StartWorkoutDialog>(), "Enter activa la tarjeta");
            H.Find("dialog-cancel")!.Focus();
            yield return Frames();
            for (var i = 0; i < 4; i++)
            {
                H.PressTab();
                yield return Frames();
                var focused = H.Focused as VisualElement;
                Assert.That(focused != null && H.TopDialog!.Host.Contains(focused), Is.True, "Tab recorre solo el diálogo");
            }

            H.PressBack(BackSource.Escape);
            yield return Settle();
            Assert.That(H.TopDialog, Is.Null, "Escape cancela el diálogo");
            Assert.That(Phase, Is.EqualTo(WorkoutPhase.Idle));
        }
    }
}
