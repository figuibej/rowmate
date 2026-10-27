using System.Collections;
using NUnit.Framework;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Idioma del sistema en pantalla (F-APP-09; AC-APP-09-04, -05): sin selector ni memoria, cambio en vivo sin reiniciar.
    // Los textos del simulador siguen en español (Q-03).
    public sealed class LanguageOnScreenTests : SimulatorUiTestBase
    {
        [UnityTest, Category("AC-APP-09-04")]
        public IEnumerator NoLanguageSelectorAndNoMemory()
        {
            CreateSim(DeviceProfile.TV, SimulatorLayerMode.Full, "en-US");
            yield return Settle();
            Assert.That(H.IsTextVisible("Device"), Is.True);
            var selectors = H.Root.Query<VisualElement>().Where(e => e.name != null && (e.name.Contains("language") || e.name.Contains("idioma"))).ToList();
            Assert.That(selectors, Is.Empty, "sin selector de idioma");
            Assert.That(H.Root.Query<DropdownField>().ToList(), Is.Empty);
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            Assert.That(H.IsTextVisible("Dispositivo"), Is.True, "nada se recuerda: manda el sistema");
        }

        [UnityTest, Category("AC-APP-09-05")]
        public IEnumerator LiveLanguageChangeKeepsEverythingElse()
        {
            CreateSim(DeviceProfile.L640);
            yield return Advance(5);
            H.ScrollToEnd("device-connected-scroll");
            yield return Frames(3);
            var offset = H.ScrollOffsetOf("device-connected-scroll");
            var screen = H.DeviceScreen;
            H.SetLanguage("en-US");
            yield return Settle();
            Assert.That(H.IsTextVisible("Device"), Is.True, "cambia en vivo");
            Assert.That(H.HasText("HEART RATE SENSOR"), Is.True);
            Assert.That(H.HasText("SIMULADOR"), Is.True, "la cinta sigue en español");
            Assert.That(H.TextOf("device-name"), Is.EqualTo("Remo simulado"), "el nombre del simulado es fijo");
            Assert.That(H.DeviceScreen, Is.SameAs(screen), "no se rearma nada");
            Assert.That(H.App.Device.State.Value.IsConnected, Is.True);
            Assert.That(H.ScrollOffsetOf("device-connected-scroll"), Is.EqualTo(offset).Within(1f), "el desplazamiento se conserva");
        }
    }
}
