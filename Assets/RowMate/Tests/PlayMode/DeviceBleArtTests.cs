using System;
using System.Collections;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Design;
using RowMate.Presentation.Device;
using RowMate.Tests.Support;
using RowMate.Tests.UiSupport;
using UnityEngine.UIElements;
using UnityEngine.TestTools;

namespace RowMate.Tests.PlayMode
{
    // DBA · El arte y el movimiento del Bluetooth real (WP-D; 10a 00-arte-bluetooth §1-§5; A10-03, -05, -07, -11, -12) con el adaptador
    // falso K-10: el sonar (banda, alto compacto), el check de "Listo", "se despierta" y el diagnóstico en mono.
    public sealed class DeviceBleArtTests : UiTestBase
    {
        private FakeBleAdapter Adapter => H.FakeBle!.Adapter;

        private DeviceScreenView Screen => H.DeviceScreen!;

        private IEnumerator CreateBle(BleAdapterState initial = BleAdapterState.On, Action<FakeBleAdapter>? setup = null,
                                      DeviceProfile? profile = null, bool reduceMotion = false)
        {
            Create(new UiHostOptions
            {
                Profile = profile ?? DeviceProfile.TV,
                Backend = HostBackends.FakeBle(setup, initial),
                ReduceMotion = reduceMotion
            });
            yield return Settle();
        }

        [UnityTest, Category("A10-03"), Category("AC-BLE-05-04")]
        public IEnumerator TheSonarBandShowsWhileSearchingAndLeavesAfterTenSeconds()
        {
            yield return CreateBle();
            Assert.That(Screen.Scan.Sonar.IsShown, Is.False, "sin búsqueda no hay banda");
            yield return Tap("device-search");
            yield return Settle();
            var sonar = Screen.Scan.Sonar;
            Assert.That(sonar.IsShown && sonar.IsSearching, Is.True);
            Assert.That(sonar.resolvedStyle.height, Is.EqualTo((float)BleMotionSpecs.SonarBandDp).Within(0.5f), "152 dp");
            yield return Advance(10.1);
            yield return Settle();
            Assert.That(sonar.IsShown, Is.False, "se fue con la búsqueda");
            Assert.That(sonar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest, Category("A10-03"), Category("AC-BLE-05-09")]
        public IEnumerator CompactHeightHasNoSonarBand()
        {
            yield return CreateBle(profile: DeviceProfile.L640);
            yield return Tap("device-search");
            yield return Settle();
            Assert.That(Screen.Scan.Sonar.IsCompact, Is.True, "L640: menos de 560 dp de contenido");
            Assert.That(Screen.Scan.Sonar.IsShown, Is.False, "queda el spinner del botón, como el legacy (D10-02)");
            Assert.That(Screen.Scan.Search.IsBusy, Is.True);
        }

        [UnityTest, Category("A10-05"), Category("AC-BLE-06-07")]
        public IEnumerator ReadyDrawsTheCheckOnTheTappedCard()
        {
            yield return CreateBle(setup: a => a.RespondAfter(FakeBleDevices.RowId, TimeSpan.FromSeconds(1)));
            yield return Tap("device-search");
            Adapter.Advertise(FakeBleDevices.Row);
            Adapter.Advertise(FakeBleDevices.Anon);
            yield return Settle();
            yield return Frames();
            H.Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            yield return Frames();
            var row = Screen.Scan.Results.CardFor(FakeBleDevices.RowId)!;
            Assert.That(row.ShowsCheck, Is.False);
            H.Advance(1.3);
            yield return Frames();
            Assert.That(Screen.HoldingReady, Is.True);
            Assert.That(row.ShowsCheck, Is.True, "el spinner pasa al check en t₀");
            Assert.That(row.ShowsSpinner, Is.False);
            Assert.That(row.Line2.ClassListContains("rm-scan-card__line2--ready"), Is.True, "Listo en state.connected");
        }

        [UnityTest, Category("A10-07"), Category("AC-BLE-04-03")]
        public IEnumerator WakingUpMorphsIntoTheScanInSevenHundredTwentyMs()
        {
            yield return CreateBle(BleAdapterState.Off);
            yield return Advance(1);
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.BluetoothState));
            Assert.That(Screen.State.Art.Breathes, Is.True, "apagado: respira");
            Adapter.SetState(BleAdapterState.On);
            yield return Frames();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(Screen.State.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "la ilustración se despierta antes de irse");
            H.Advance(BleMotionSpecs.WakeOutEndMs / 1000.0 + 0.02);
            yield return Frames();
            Assert.That(Screen.State.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "a los 480 ms ya cambió");
            Assert.That(Screen.Scan.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            yield return Settle();
            Assert.That(Screen.Scan.resolvedStyle.opacity, Is.EqualTo(1f));
            Assert.That(Adapter.ScanStarts, Is.EqualTo(0), "no busca solo");
        }

        [UnityTest, Category("A10-12"), Category("AC-BLE-04-03")]
        public IEnumerator WakingUpWithReducedMotionEndsInTheSameState()
        {
            yield return CreateBle(BleAdapterState.Off, reduceMotion: true);
            yield return Advance(1);
            Adapter.SetState(BleAdapterState.On);
            yield return Settle();
            Assert.That(Screen.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(Screen.State.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(Screen.Scan.resolvedStyle.opacity, Is.EqualTo(1f));
            Assert.That(Screen.State.Art.WakeIcon.resolvedStyle.opacity, Is.EqualTo(0f), "la ilustración vuelve a reposo");
        }

        [UnityTest, Category("A10-10"), Category("AC-BLE-12-02")]
        public IEnumerator TheDiagnosticsValuesUseTheMonoToken()
        {
            yield return CreateBle();
            yield return Tap("device-search");
            Adapter.Advertise(FakeBleDevices.Row);
            yield return Settle();
            yield return Tap(ScanResultCard.NamePrefix + FakeBleDevices.RowId);
            yield return Advance(1);
            yield return Settle();
            Screen.Connected.OpenDebug();
            Adapter.Notify(FakeBleDevices.RowId, FtmsUuids.RowerData, FakeBleDevices.RowerData);
            yield return Settle();
            var panel = Screen.Connected.DebugPanel!;
            Assert.That(panel.Hex.ClassListContains("rm-type-mono"), Is.True);
            Assert.That(panel.Count.ClassListContains("rm-type-mono"), Is.True);
            Assert.That(panel.Hex.resolvedStyle.unityFontDefinition.fontAsset?.name, Does.StartWith("JetBrainsMono"), "FontAsset de Mono (A10-10)");
            Assert.That(panel.Dot.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "RECEIVING con el punto");
            Assert.That(panel.Hex.resolvedStyle.minHeight.value, Is.EqualTo(36f).Within(0.5f), "2 líneas de alto mínimo");
        }
    }
}
