using System;
using System.Collections;
using NUnit.Framework;
using RowMate.Simulator;
using RowMate.Tests.UiSupport;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Base de las pruebas de UI con los servicios simulados (02-tech-design §5.1, solo con ROWMATE_SIMULATOR).
    public abstract class SimulatorUiTestBase
    {
        // La fila del pulsómetro simulado (key del legacy: hrm-device-<id>, SimulatedHeartRateService.DeviceId).
        protected const string HrmRow = "hrm-device-SIM:HRM";

        protected UiTestHost? Host { get; private set; }

        protected UiTestHost H => Host ?? throw new InvalidOperationException("sin host");

        protected SimulatorDeviceBackend Sim => H.Simulator ?? throw new InvalidOperationException("el backend no es el del simulador");

        [TearDown]
        public void DisposeHost()
        {
            Host?.Dispose();
            Host = null;
        }

        // Simulador sin ruido; con la capa completa salvo que se pida otra cosa.
        protected UiTestHost CreateSim(DeviceProfile profile, SimulatorLayerMode layer = SimulatorLayerMode.Full, params string[] languages)
        {
            return Create(new UiHostOptions
            {
                Profile = profile,
                Backend = HostBackends.Simulator(false),
                SimulatorLayer = layer,
                LanguageTags = languages.Length > 0 ? languages : new[] { "es-AR" },
            });
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

        // ensureVisible + tap (lo que hace el test legacy cuando el elemento está fuera de la vista).
        protected IEnumerator ScrollAndTap(string elementName)
        {
            yield return Frames();
            if (H.WhyNotTappable(elementName) != null && RowMate.Tests.UiSupport.UiQuery.ScrollViewOf(H.Find(elementName)!) != null)
            {
                H.ScrollTo(elementName);
                yield return Frames();
            }

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
