using System.Collections;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode
{
    // La composición del build normal de la It. 2 (P-04, AC-BLE-13-05): equipo sin Bluetooth LE (HardwareBackend), sin capa
    // del simulador.
    public sealed class NormalBuildTests : UiTestBase
    {
        private IEnumerator Normal()
        {
            Create(new UiHostOptions { Backend = HostBackends.Unavailable, Profile = DeviceProfile.W1280 });
            yield return Settle();
        }

        [UnityTest, Category("AC-APP-01-03")]
        public IEnumerator NormalCompositionOpensOnSearchView()
        {
            yield return Normal();
            Assert.That(H.DeviceScreen!.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(H.IsTextVisible("Sin sensor"), Is.True);
        }

        [UnityTest, Category("AC-BLE-05-02")]
        public IEnumerator EmptyStateWithoutEverSearching()
        {
            yield return Normal();
            Assert.That(H.IsTextVisible("No se encontraron dispositivos"), Is.True, "estado vacío desde el primer cuadro");
            Assert.That(H.Find("device-search")!.enabledSelf, Is.True);
        }

        [UnityTest, Category("AC-BLE-13-02")]
        public IEnumerator NormalCompositionHasNoSimulatorLayer()
        {
            yield return Normal();
            Assert.That(H.Find("simulator-layer"), Is.Null);
            Assert.That(H.Find("sim-ribbon"), Is.Null);
            Assert.That(H.Find("sim-open"), Is.Null);
        }

        [UnityTest, Category("AC-BLE-13-05")]
        public IEnumerator NormalBuildSearchDoesNothing()
        {
            yield return Normal();
            yield return Tap("device-search");
            yield return Tap("hrm-search");
            Assert.That(H.App.Device.State.Value.Status, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(H.HasErrors(), Is.False, "sin errores");
        }

        // QA-V3-01 (c): el clic de tools/unity/capture-cap27.ps1 (-ClickRelative 'x,y', fracciones del área cliente de la
        // ventana) cae en la pestaña Rutinas de la composición del build normal (4 pestañas, sin Perfil) y la elige: en la
        // ventana por defecto del player (1280 × 720), en ventanas más chicas, con otra escala y en una del alto de un monitor
        // 4K al 100 % (2100 dp, donde 0,96 ya caía arriba de la barra de 64 dp). Si la barra cambia (otra pestaña, otro alto),
        // esta prueba avisa antes de que la toma salga mal.
        [UnityTest, Category("QA-V3-01")]
        public IEnumerator Cap27ClickSelectsTheRoutinesTab()
        {
            var click = Cap27Click();
            var profiles = new[]
            {
                DeviceProfile.W1280, DeviceProfile.L640, DeviceProfile.P800, new DeviceProfile("W1280@1.5", 1280, 720, 1.5),
                DeviceProfile.Dp(1920, 1080), DeviceProfile.Dp(2560, 2100),
            };
            foreach (var profile in profiles)
            {
                Create(new UiHostOptions { Backend = HostBackends.Unavailable, Profile = profile });
                yield return Settle();
                var window = H.Root.worldBound;
                var point = new Vector2(window.xMin + click.x * window.width, window.yMin + click.y * window.height);
                var routines = H.Find("tab-routines")!;
                var picked = H.Root.panel.Pick(point);
                Assert.That(IsWithin(picked, routines), Is.True, profile + ": el punto " + point + " cae en #" + (picked == null ? "nada" : picked.name));
                Assert.That(H.Find("tab-routines")!.ClassListContains("rm-tab--selected"), Is.False, profile + ": arranca en otra pestaña");
                H.TapAt(point);
                yield return Settle();
                Assert.That(routines.ClassListContains("rm-tab--selected"), Is.True, profile + ": Rutinas queda elegida");
            }
        }

        // (x, y) de -ClickRelative en capture-cap27.ps1 (una sola llamada a capture-window.ps1).
        private static Vector2 Cap27Click()
        {
            var script = File.ReadAllText(RepoPaths.Combine("tools/unity/capture-cap27.ps1"));
            var matches = Regex.Matches(script, @"-ClickRelative '([0-9.]+),([0-9.]+)'");
            Assert.That(matches.Count, Is.EqualTo(1), "capture-cap27.ps1: una llamada con -ClickRelative");
            var groups = matches[0].Groups;
            return new Vector2(float.Parse(groups[1].Value, CultureInfo.InvariantCulture), float.Parse(groups[2].Value, CultureInfo.InvariantCulture));
        }

        private static bool IsWithin(VisualElement? element, VisualElement ancestor)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current == ancestor)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
