using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Tests.UiSupport;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Los 4 casos legacy de la tarjeta del pulsómetro (01-po-spec §6; 02-tech-design §5.3), con los servicios simulados.
    // legacy: test/heart_rate_card_test.dart
    public sealed class HeartRateCardTests : SimulatorUiTestBase
    {
        public static IEnumerable<TestCaseData> Lt28()
        {
            yield return new TestCaseData().Returns(null).SetName("sin sensor → buscar → conectar → bpm → desconectar")
                .SetCategory("LT-28").SetCategory("AC-HRM-13-01").SetCategory("AC-HRM-13-09").SetCategory("AC-HRM-04-03").SetCategory("AC-BLE-11-07");
        }

        public static IEnumerable<TestCaseData> Lt29()
        {
            yield return new TestCaseData().Returns(null).SetName("los escaneos del remo y del pulsómetro se excluyen")
                .SetCategory("LT-29").SetCategory("AC-HRM-13-08").SetCategory("AC-BLE-05-03");
        }

        public static IEnumerable<TestCaseData> Lt30()
        {
            yield return new TestCaseData().Returns(null).SetName("en landscape la vista de escaneo no desborda")
                .SetCategory("LT-30").SetCategory("AC-HRM-13-10").SetCategory("AC-HRM-13-01");
        }

        public static IEnumerable<TestCaseData> Lt31()
        {
            yield return new TestCaseData().Returns(null).SetName("mientras el remo escanea no se puede buscar pulsómetro")
                .SetCategory("LT-31").SetCategory("AC-HRM-13-04").SetCategory("AC-HRM-13-08").SetCategory("AC-BLE-10-02").SetCategory("AC-BLE-05-03");
        }

        // legacy: test/heart_rate_card_test.dart:83 'sin sensor → buscar → conectar → bpm → desconectar'
        [TestCaseSource(nameof(Lt28))]
        public IEnumerator NoSensorSearchConnectBpmDisconnect()
        {
            CreateSim(DeviceProfile.Dp(800, 600), SimulatorLayerMode.None);
            yield return Settle();
            Assert.That(H.IsTextVisible("PULSÓMETRO") || H.HasText("PULSÓMETRO"), Is.True);
            Assert.That(H.HasText("Sin sensor"), Is.True);
            Assert.That(H.TextOf("hrm-watch-hint"), Does.Contain("Heart for Bluetooth"));
            Assert.That(H.WhyNotTappable("hrm-search"), Is.Not.Null, "800 × 600: hay que desplazarse hasta Buscar");
            H.ScrollTo("hrm-search");
            yield return Frames();
            yield return Tap("hrm-search");
            Assert.That(H.HasText("Buscando sensores..."), Is.True);
            Assert.That(H.HasText("Pulsómetro simulado"), Is.True);
            yield return ScrollAndTap(HrmRow);
            Assert.That(H.App.HeartRate.State.Value.IsConnected, Is.True);
            Assert.That(H.TextOf("hrm-connected-name"), Is.EqualTo("Pulsómetro simulado"));
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("--"));
            Sim.Simulator.Tick();
            yield return Advance(1);
            var bpm = Sim.SimHeartRate.CurrentBpm;
            Assert.That(bpm, Is.GreaterThan(0));
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo(RowMate.Core.Text.Fmt.Integer(bpm)));
            Assert.That(H.HasText("--"), Is.False);
            yield return ScrollAndTap("hrm-disconnect");
            Assert.That(H.HasText("Sin sensor"), Is.True);
        }

        // legacy: test/heart_rate_card_test.dart:121 'los escaneos del remo y del pulsómetro se excluyen'
        [TestCaseSource(nameof(Lt29))]
        public IEnumerator RowerAndHrmScansExcludeEachOther()
        {
            CreateSim(DeviceProfile.Dp(800, 600), SimulatorLayerMode.None);
            yield return Settle();
            _ = H.App.Device.DisconnectAsync();
            yield return Settle();
            Assert.That(H.Find("device-search")!.enabledSelf, Is.True);
            yield return ScrollAndTap("hrm-search");
            Assert.That(H.App.HeartRate.State.Value.IsScanning, Is.True);
            Assert.That(H.Find("device-search")!.enabledSelf, Is.False, "el pulsómetro busca: el remo no");
            _ = Sim.SimHeartRate.StopScanAsync();
            yield return Settle();
            Assert.That(H.Find("device-search")!.enabledSelf, Is.True);
        }

        // legacy: test/heart_rate_card_test.dart:142 'en landscape la vista de escaneo no desborda'
        [TestCaseSource(nameof(Lt30))]
        public IEnumerator LandscapeScanViewDoesNotOverflow()
        {
            CreateSim(DeviceProfile.L640, SimulatorLayerMode.None);
            yield return Settle();
            _ = H.App.Device.DisconnectAsync();
            yield return Settle();
            AssertClean(H);
            yield return ScrollAndTap("hrm-search");
            Assert.That(H.App.HeartRate.State.Value.Status, Is.EqualTo(HrmStatus.Scanning));
            var row = H.Find(HrmRow);
            Assert.That(row, Is.Not.Null);
            Assert.That(H.Find("hrm-card-slot-scan")!.Contains(row!), Is.True, "la fila está en el área de la tarjeta");
            AssertClean(H);
        }

        // legacy: test/heart_rate_card_test.dart:161 'mientras el remo escanea no se puede buscar pulsómetro'
        [TestCaseSource(nameof(Lt31))]
        public IEnumerator HrmSearchDisabledWhileRowerScans()
        {
            Create(new UiHostOptions { Profile = DeviceProfile.Dp(800, 600), Backend = HostBackends.TestRowerWithSimulatedHeartRate() });
            yield return Settle();
            Assert.That(H.Find("hrm-search")!.enabledSelf, Is.True);
            foreach (var (status, enabled) in new[] { (BleStatus.Scanning, false), (BleStatus.Connecting, false), (BleStatus.Disconnected, true) })
            {
                H.TestRower!.Emit(status);
                yield return Settle();
                Assert.That(H.Find("hrm-search")!.enabledSelf, Is.EqualTo(enabled), status.ToString());
            }
        }
    }
}
