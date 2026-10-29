using System;
using System.Collections;
using System.Collections.Generic;
using RowMate.Presentation.Shell;
using RowMate.Tests.UiSupport;

namespace RowMate.Snapshots
{
    // Pasos de un escenario del arnés, con el reloj de prueba (02-tech-design §4.1). Cada paso deja pasar dos
    // EditorApplication.update para que UI Toolkit haga el layout (los toques miran worldBound y panel.Pick) y, después de
    // un toque, asienta las transiciones (= pumpAndSettle). Mismo patrón que las pruebas PlayMode.
    public sealed class ScenarioScript
    {
        public ScenarioScript(UiTestHost host)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public UiTestHost Host { get; }

        public IEnumerator Frames(int count = 2)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        public IEnumerator Advance(double seconds)
        {
            Host.Advance(seconds);
            yield return Frames(2);
        }

        // = pumpAndSettle con updates reales cuando una animación espera el layout (lo nuevo que crece). Tope: 2 s de reloj.
        public IEnumerator Settle()
        {
            yield return Frames(2);
            var spent = 0.0;
            var stalled = 0;
            for (var i = 0; i < 60; i++)
            {
                var advanced = Host.Settle(TimeSpan.FromSeconds(Math.Max(0, 2 - spent)));
                spent += advanced;
                yield return Frames(2);
                if (!Host.HasFiniteMotions || spent >= 2)
                {
                    break;
                }

                // Si algo sigue esperando un layout que no llega, el reloj avanza igual (como antes: tope de 2 s).
                stalled = advanced > 0 ? 0 : stalled + 1;
                if (stalled >= 3)
                {
                    Host.Step();
                    spent += UiTestHost.FrameSeconds;
                }
            }
        }

        public IEnumerator Tap(string elementName)
        {
            yield return Frames(2);
            Host.Tap(elementName);
            yield return Settle();
        }

        public IEnumerator TapTimes(string elementName, int times)
        {
            for (var i = 0; i < times; i++)
            {
                yield return Frames(1);
                Host.Tap(elementName);
            }

            yield return Settle();
        }

        public IEnumerator ScrollTo(string elementName)
        {
            yield return Frames(2);
            Host.ScrollTo(elementName);
            yield return Frames(2);
        }

        public IEnumerator ScrollToEnd(string scrollViewName)
        {
            yield return Frames(2);
            Host.ScrollToEnd(scrollViewName);
            yield return Frames(2);
        }

        public IEnumerator Do(Action<UiTestHost> action)
        {
            yield return Frames(1);
            action(Host);
            yield return Settle();
        }

        // ── It. 3 (03-rutinas/02-tech-design §5.1) ──

        // Toca, llevándolo a la vista primero si está fuera de su ScrollView (= ensureVisible + tap).
        public IEnumerator TapInView(string elementName)
        {
            yield return Frames(2);
            if (Host.WhyNotTappable(elementName) != null && Host.Find(elementName) is { } element && UiQuery.ScrollViewOf(element) != null)
            {
                Host.ScrollTo(elementName);
                yield return Frames(2);
            }

            Host.Tap(elementName);
            yield return Settle();
        }

        // Escribe en un campo como el usuario (foco y valor; el campo filtra y avisa al store; la vista previa se rearma).
        public IEnumerator Type(string fieldName, string text)
        {
            yield return Frames(2);
            Host.Type(fieldName, text);
            yield return Settle();
        }

        public IEnumerator RightClick(string elementName)
        {
            yield return Frames(2);
            Host.RightClick(elementName);
            yield return Settle();
        }

        public IEnumerator PressBack(BackSource source)
        {
            yield return Frames(2);
            Host.PressBack(source);
            yield return Settle();
        }

        // Cierra la app y la vuelve a abrir sobre la MISMA base (DEMO-10, AC-RUT-08-09).
        public IEnumerator Reopen()
        {
            yield return Frames(2);
            Host.Reopen();
            yield return Settle();
        }

        // ── It. 4a (04a-motor-hud-funcional/02-tech-design §7.1) ──

        // El instante del reloj de prueba (monotónico) del toque que empezó la sesión: lo anotan StartFree y StartRoutine, y
        // AdvanceTo cuenta desde ahí (el motor arranca en el toque, AC-WKT-13-01).
        public double SessionStart { get; private set; } = double.NaN;

        // Workout y "Entrenamiento libre" (el inmersivo entra con su fundido, que Settle deja terminado).
        public IEnumerator StartFree()
        {
            yield return GoToWorkout();
            Host.Tap("workout-free");
            SessionStart = Host.Scheduler.MonotonicSeconds;
            yield return Settle();
        }

        // Toca la tarjeta de la rutina en Workout (por su nombre): abre el diálogo de inicio, o el aviso si no se puede iniciar.
        public IEnumerator TapRoutine(string name)
        {
            yield return GoToWorkout();
            Host.TapRoutineNamed(name);
            yield return Settle();
        }

        // La tarjeta y "Iniciar" del diálogo de inicio.
        public IEnumerator StartRoutine(string name)
        {
            yield return TapRoutine(name);
            yield return ConfirmStart();
        }

        // "Iniciar" en el diálogo de inicio abierto: la sesión empieza en el toque.
        public IEnumerator ConfirmStart()
        {
            yield return Frames(2);
            Host.Tap("dialog-confirm");
            SessionStart = Host.Scheduler.MonotonicSeconds;
            yield return Settle();
        }

        // Avanza el reloj de prueba hasta N segundos desde el toque que empezó la sesión (nunca para atrás).
        public IEnumerator AdvanceTo(double secondsSinceStart)
        {
            if (double.IsNaN(SessionStart))
            {
                throw new InvalidOperationException("AdvanceTo sin una sesión empezada (StartFree o StartRoutine)");
            }

            var target = SessionStart + secondsSinceStart;
            var now = Host.Scheduler.MonotonicSeconds;
            if (target > now + 1e-9)
            {
                Host.Advance(target - now);
            }

            yield return Frames(2);
        }

        // "A los N s" para una foto fija: un instante del segundo N de la sesión, con los rodillos del cambio de segundo ya
        // quietos (N + 0,45 o más) y dentro de la ventana de la lectura [k + 0,56; k + 0,84] del reloj de prueba, así la regla
        // de toma (SnapshotRunner.TakeRule) no se corre al segundo siguiente. Si ningún instante de [N + 0,45; N + 0,95) cae en la
        // ventana, el más tardío de [N; N + 0,45) que caiga: la regla espera ahí que se aquieten.
        public IEnumerator AtSecond(double n)
        {
            if (double.IsNaN(SessionStart))
            {
                throw new InvalidOperationException("AtSecond sin una sesión empezada (StartFree o StartRoutine)");
            }

            var second = SessionStart + n;
            var chosen = double.NaN;
            for (var d = 0.45; d < 0.95 && double.IsNaN(chosen); d += 0.01)
            {
                if (InReadingWindow(second + d))
                {
                    chosen = d;
                }
            }

            for (var d = 0.44; d >= 0 && double.IsNaN(chosen); d -= 0.01)
            {
                if (InReadingWindow(second + d))
                {
                    chosen = d;
                }
            }

            yield return AdvanceTo(n + (double.IsNaN(chosen) ? 0 : chosen));
        }

        private static bool InReadingWindow(double monotonic)
        {
            var fraction = monotonic - Math.Floor(monotonic);
            return fraction >= 0.56 && fraction <= 0.84;
        }

        private IEnumerator GoToWorkout()
        {
            yield return Frames(2);
            if (Host.App.Shell.SelectedIndex.Value != 1 || Host.Shell.Pages.CoversShell)
            {
                Host.Tap("tab-workout");
                yield return Settle();
            }
        }

        // Una toma de una secuencia (Kind = Sequence): la regla de toma de una foto fija y el registro (la pone el arnés).
        public Func<string, Func<UiTestHost, IEnumerable<string>>?, IEnumerator>? OnShot { get; set; }

        public IEnumerator Shot(string id, Func<UiTestHost, IEnumerable<string>>? checks = null)
        {
            if (OnShot == null)
            {
                throw new InvalidOperationException("Shot fuera de una secuencia (" + id + ")");
            }

            yield return OnShot(id, checks);
        }
    }
}
