using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Tests.UiSupport;

namespace RowMate.Tests.PlayMode
{
    // Base de las pruebas de UI en PlayMode (02-tech-design §5.1): un UiTestHost por prueba (la UI real en una RT del tamaño
    // pedido, con el reloj de prueba) que se libera al terminar. Frames() deja pasar cuadros reales para el layout y el dibujo;
    // Settle() = pumpAndSettle.
    public abstract class UiTestBase
    {
        protected UiTestHost? Host { get; private set; }

        protected UiTestHost H => Host ?? throw new InvalidOperationException("sin host");

        [TearDown]
        public void DisposeHost()
        {
            Host?.Dispose();
            Host = null;
        }

        protected UiTestHost Create(UiHostOptions options)
        {
            Host?.Dispose();
            Host = UiTestHost.Create(options);
            return Host;
        }

        protected static IEnumerator Frames(int count = 2)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        // Avanza el reloj de prueba, asienta las transiciones y deja dos cuadros de layout.
        protected IEnumerator Advance(double seconds)
        {
            H.Advance(seconds);
            yield return Frames();
        }

        // = pumpAndSettle con cuadros reales cuando una animación espera el layout (lo nuevo que crece). Tope: 2 s de reloj.
        protected IEnumerator Settle()
        {
            yield return Frames();
            var spent = 0.0;
            var stalled = 0;
            for (var i = 0; i < 60; i++)
            {
                var advanced = H.Settle(TimeSpan.FromSeconds(Math.Max(0, 2 - spent)));
                spent += advanced;
                yield return Frames();
                if (!H.HasFiniteMotions || spent >= 2)
                {
                    break;
                }

                // Si algo sigue esperando un layout que no llega, el reloj avanza igual (como antes: tope de 2 s).
                stalled = advanced > 0 ? 0 : stalled + 1;
                if (stalled >= 3)
                {
                    H.Step();
                    spent += UiTestHost.FrameSeconds;
                }
            }
        }

        protected IEnumerator Tap(string elementName)
        {
            yield return Frames();
            H.Tap(elementName);
            yield return Settle();
        }

        protected static void AssertClean(UiTestHost host)
        {
            var audit = host.Audit();
            Assert.That(audit.IsClean, Is.True, audit.ToString());
        }
    }
}
