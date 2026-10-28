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
