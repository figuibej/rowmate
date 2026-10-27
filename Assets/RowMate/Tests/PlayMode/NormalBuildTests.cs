using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.UiSupport;
using UnityEngine.TestTools;

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
    }
}
