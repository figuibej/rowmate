using System;
using System.Collections;
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

        public IEnumerator Settle()
        {
            yield return Frames(2);
            Host.Settle();
            yield return Frames(2);
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
    }
}
