using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Tests.UiSupport;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace RowMate.Tests.PlayMode.Simulator
{
    // Dispositivo con el remo y el pulsómetro simulados (F-BLE-07, -08, -11, -15; F-HRM-15; 02-tech-design §5.2).
    public sealed class DeviceSimulatorTests : SimulatorUiTestBase
    {
        private string? Tile(MetricKind kind)
        {
            return H.DeviceScreen!.Connected.Grid.Find(kind)?.Value.Text;
        }

        private IEnumerator ConnectHrm()
        {
            _ = H.App.HeartRate.StartScanAsync();
            _ = H.App.HeartRate.ConnectAsync("SIM:HRM", "Pulsómetro simulado");
            yield return Settle();
        }

        [UnityTest, Category("AC-APP-01-03"), Category("AC-BLE-15-10")]
        public IEnumerator SimulatorOpensConnectedWithoutTouching()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            Assert.That(H.DeviceScreen!.View, Is.EqualTo(DeviceViewKind.Connected));
            Assert.That(H.TextOf("device-name"), Is.EqualTo("Remo simulado"));
        }

        [UnityTest, Category("AC-APP-01-04"), Category("AC-HRM-15-01")]
        public IEnumerator CardOpensInNoSensor()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            Assert.That(H.HeartRateCard!.Body, Is.EqualTo(HrmCardBody.NoSensor));
            Assert.That(H.HasErrors(), Is.False, "autoConnect del simulado no hace nada y no deja errores");
        }

        [UnityTest, Category("AC-APP-01-05")]
        public IEnumerator DeviceTabKeepsUpdatingWhileHidden()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(5);
            yield return Tap("tab-routines");
            var before = H.App.Device.State.Value.Data.DistanceMeters;
            yield return Advance(10);
            var after = H.App.Device.State.Value.Data.DistanceMeters;
            Assert.That(after - before, Is.InRange(30, 45), "≈ 38 m en 10 s");
            Assert.That(Tile(MetricKind.Distance), Is.EqualTo(RowingFormats.Meters(after)), "la grilla oculta sigue al día");
            yield return Tap("tab-device");
            Assert.That(Tile(MetricKind.Distance), Is.EqualTo(RowingFormats.Meters(H.App.Device.State.Value.Data.DistanceMeters)));
        }

        [UnityTest, Category("AC-APP-02-04")]
        public IEnumerator HrmSearchSurvivesTabSwitchAndExpiresAt10s()
        {
            CreateSim(DeviceProfile.TV);
            yield return Settle();
            yield return ScrollAndTap("hrm-search");
            yield return Tap("tab-history");
            yield return Advance(5);
            yield return Tap("tab-device");
            Assert.That(H.App.HeartRate.State.Value.IsScanning, Is.True, "la búsqueda sigue al cambiar de pestaña");
            yield return Advance(5.1);
            Assert.That(H.App.HeartRate.State.Value.IsScanning, Is.False, "vence a los 10 s");
        }

        [UnityTest, Category("AC-BLE-07-02")]
        public IEnumerator DisconnectShowsEmptySearch()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(3);
            yield return Tap("device-disconnect");
            Assert.That(H.DeviceScreen!.View, Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(H.IsTextVisible("No se encontraron dispositivos"), Is.True);
        }

        [UnityTest, Category("AC-BLE-07-03")]
        public IEnumerator NoAutoReconnectAfterDisconnect()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(3);
            yield return Tap("device-disconnect");
            yield return Advance(10);
            Assert.That(H.DeviceScreen!.View, Is.EqualTo(DeviceViewKind.Scan));
            yield return Tap("device-search");
            Assert.That(H.DeviceScreen.View, Is.EqualTo(DeviceViewKind.Connected), "buscar reconecta");
        }

        [UnityTest, Category("AC-BLE-08-01"), Category("AC-BLE-15-10")]
        public IEnumerator DropShowsSearchForThreeSeconds()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(5);
            Sim.SimRower.SimulateDisconnect();
            yield return Settle();
            Assert.That(H.DeviceScreen!.View, Is.EqualTo(DeviceViewKind.Scan));
            yield return Advance(2.5);
            Assert.That(H.DeviceScreen.View, Is.EqualTo(DeviceViewKind.Scan));
            yield return Advance(0.6);
            yield return Settle();
            Assert.That(H.DeviceScreen.View, Is.EqualTo(DeviceViewKind.Connected), "vuelve a los 3 s");
        }

        [UnityTest, Category("AC-BLE-11-09")]
        public IEnumerator ReconnectShowsZerosThenValues()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(10.5);
            var distance = H.App.Device.State.Value.Data.DistanceMeters;
            Sim.SimRower.SimulateDisconnect();
            yield return Advance(3.05);
            yield return Settle();
            Assert.That(Tile(MetricKind.Watts), Is.EqualTo("0"), "al conectar: ceros");
            Assert.That(Tile(MetricKind.Split), Is.EqualTo("--:--"));
            yield return Advance(1);
            Assert.That(Tile(MetricKind.Watts), Is.EqualTo("150"));
            Assert.That(H.App.Device.State.Value.Data.DistanceMeters, Is.GreaterThan(distance), "la distancia sigue");
        }

        [UnityTest, Category("AC-BLE-11-10")]
        public IEnumerator GridShowsReferenceValuesAt60s()
        {
            CreateSim(DeviceProfile.TV);
            yield return Advance(60);
            Assert.That(Tile(MetricKind.Split), Is.EqualTo("02:13"));
            Assert.That(Tile(MetricKind.Spm), Is.EqualTo("24.0"));
            Assert.That(Tile(MetricKind.Watts), Is.EqualTo("150"));
            Assert.That(Tile(MetricKind.Distance), Is.EqualTo("226m"));
        }

        [UnityTest, Category("AC-BLE-13-04")]
        public IEnumerator SameTreeWithSimulatedAndTestDevices()
        {
            CreateSim(DeviceProfile.TV, SimulatorLayerMode.None);
            yield return Settle();
            var simulated = Names(H.Find("screen-device")!);
            Create(new UiHostOptions());
            H.TestRower!.Emit(BleStatus.Connected);
            yield return Settle();
            var test = Names(H.Find("screen-device")!);
            Assert.That(test, Is.EquivalentTo(simulated), "las pantallas no saben si los servicios son simulados");
        }

        [UnityTest, Category("AC-HRM-13-12")]
        public IEnumerator HeartBeatsAtTheRealRate()
        {
            CreateSim(DeviceProfile.TV);
            yield return ConnectHrm();
            yield return Advance(60);
            Assert.That(H.TextOf("hrm-bpm"), Is.EqualTo("142"));
            var clock = H.HeartRateCard!.Heartbeat.Clock;
            var before = clock.BeatsStarted;
            for (var i = 0; i < 600; i++)
            {
                H.Step();
            }

            var beats = clock.BeatsStarted - before;
            Assert.That(beats, Is.InRange(23, 24), "142 por minuto: ≈ 23-24 latidos en 10 s");
            yield return Frames();
        }

        [UnityTest, Category("AC-HRM-04-05")]
        public IEnumerator PulseKeepsComingWithTheRowerDisconnected()
        {
            CreateSim(DeviceProfile.TV);
            yield return ConnectHrm();
            yield return Advance(5);
            _ = H.App.Device.DisconnectAsync();
            yield return Advance(3);
            Assert.That(H.App.HeartRate.State.Value.Bpm, Is.GreaterThan(0));
            Assert.That(H.TextOf("hrm-bpm"), Is.Not.EqualTo("--"));
        }

        [UnityTest, Category("AC-HRM-15-08")]
        public IEnumerator HrmDropShowsReconnecting()
        {
            CreateSim(DeviceProfile.TV);
            yield return ConnectHrm();
            yield return Advance(2);
            Sim.SimHeartRate.SimulateDisconnect();
            yield return Settle();
            Assert.That(H.TextOf("hrm-remembered-text"), Is.EqualTo("Pulsómetro simulado · reconectando..."));
            Assert.That(H.Find("hrm-retry")!.enabledSelf, Is.False);
            yield return Advance(3.1);
            yield return Settle();
            Assert.That(H.HeartRateCard!.Body, Is.EqualTo(HrmCardBody.Connected));
        }

        [UnityTest, Category("AC-HRM-15-07")]
        public IEnumerator ForgetDuringReconnecting()
        {
            CreateSim(DeviceProfile.TV);
            yield return ConnectHrm();
            yield return Advance(2);
            Sim.SimHeartRate.SimulateDisconnect();
            yield return Settle();
            yield return ScrollAndTap("hrm-forget");
            yield return Advance(4);
            Assert.That(H.HeartRateCard!.Body, Is.EqualTo(HrmCardBody.NoSensor), "olvidar cancela la vuelta");
        }

        [UnityTest, Category("AC-HRM-15-12")]
        public IEnumerator SearchOtherDuringTheDrop()
        {
            CreateSim(DeviceProfile.TV);
            yield return ConnectHrm();
            yield return Advance(2);
            Sim.SimHeartRate.SimulateDisconnect();
            yield return Settle();
            yield return ScrollAndTap("hrm-search");
            Assert.That(H.HeartRateCard!.Body, Is.EqualTo(HrmCardBody.Scanning));
            yield return Advance(4);
            Assert.That(H.App.HeartRate.State.Value.IsConnected, Is.False, "buscar otro cancela la vuelta");
        }

        [UnityTest, Category("AC-APP-02-07")]
        public IEnumerator ResizeKeepsConnectionCardAndTab()
        {
            CreateSim(DeviceProfile.TV);
            yield return ConnectHrm();
            yield return Advance(3);
            H.Resize(DeviceProfile.TH);
            yield return Frames(3);
            Assert.That(H.App.Device.State.Value.IsConnected, Is.True);
            Assert.That(H.App.HeartRate.State.Value.IsConnected, Is.True);
            Assert.That(H.DeviceScreen!.Connected.Grid.Spec!.Columns, Is.EqualTo(3));
            Assert.That(H.App.Shell.SelectedIndex.Value, Is.EqualTo(0));
        }

        [UnityTest, Category("AC-APP-01-03"), Category("QA-G2-01")]
        public IEnumerator BootSceneWithSimulatorOpensConnected()
        {
            yield return SimulatorBootSceneProbe.Run(DeviceViewKind.Connected);
        }

        // Condición del PO para D-24 (R2-09): el árbol y la geometría de CAP-01 en PlayMode coinciden con la captura del arnés
        // (modo edición). Sin capturas, se ignora.
        [UnityTest, Category("AC-APP-05-02"), Category("R2-09")]
        public IEnumerator EditModeCaptureMatchesPlayModeTree()
        {
            const string manifest = "docs/migration/iterations/02-dispositivo-simulador/snapshots/manifest.json";
            if (!File.Exists(manifest))
            {
                Assert.Ignore("sin " + manifest + ": la escribe tools/unity/capture-snapshots.ps1");
            }

            JsonNode? shot = null;
            foreach (var candidate in JsonParser.Parse(File.ReadAllText(manifest))["shots"].Items)
            {
                if (candidate["id"].AsString() == "CAP-01")
                {
                    shot = candidate;
                }
            }

            if (shot == null)
            {
                Assert.Ignore("el manifest no tiene CAP-01");
            }

            CreateSim(DeviceProfile.TV);
            yield return Advance(60.55);
            yield return Settle();
            yield return Frames(3);
            var compared = 0;
            foreach (var element in shot!["elements"].Items)
            {
                var name = element["name"].AsString();
                var live = H.Find(name);
                if (live == null || name == "sim-ribbon")
                {
                    continue;
                }

                var bounds = live.worldBound;
                Assert.That(bounds.x, Is.EqualTo((float)element["x"].AsDouble()).Within(0.5f), name + " x");
                Assert.That(bounds.y, Is.EqualTo((float)element["y"].AsDouble()).Within(0.5f), name + " y");
                Assert.That(bounds.width, Is.EqualTo((float)element["w"].AsDouble()).Within(0.5f), name + " w");
                Assert.That(bounds.height, Is.EqualTo((float)element["h"].AsDouble()).Within(0.5f), name + " h");
                compared++;
            }

            Assert.That(compared, Is.GreaterThan(20), "elementos comparados");
        }

        private static List<string> Names(VisualElement root)
        {
            var names = new List<string>();
            root.Query<VisualElement>().ForEach(e =>
            {
                if (!string.IsNullOrEmpty(e.name) && !e.name.StartsWith("unity-") && e.name != "device-name")
                {
                    names.Add(e.name + "|" + string.Join(" ", e.GetClasses()));
                }
            });
            names.Sort();
            return names;
        }
    }
}
