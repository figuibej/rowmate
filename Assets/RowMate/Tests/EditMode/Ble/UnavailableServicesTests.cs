using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Unavailable;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // Remo y pulsómetro del build normal de la It. 2 (P-04): como el legacy en un equipo sin Bluetooth LE
    // (ble_service.dart:65-67, heart_rate_service.dart:108-113): nada se conecta, nada se emite, nada falla.
    [TestFixture]
    public sealed class UnavailableServicesTests
    {
        [Test, Category("AC-APP-01-03")]
        public void RowerStartsAndStaysDisconnected()
        {
            var logger = new RecordingLogger();
            var rower = new UnavailableRowerService(logger);
            var statuses = new StateRecorder<BleStatus>(rower.Status);
            var data = new StateRecorder<RowingData>(rower.Data);
            rower.Start();
            Assert.That(rower.Status.Value, Is.EqualTo(BleStatus.Disconnected), "el remo real no se reconecta al abrir");
            Assert.That(rower.AdapterState.Value, Is.EqualTo(BleAdapterState.Unknown), "ni Off ni Unauthorized: vista de búsqueda");
            Assert.That(rower.ConnectedDeviceName, Is.Null);
            Assert.That(statuses.Count, Is.EqualTo(0));
            Assert.That(data.Count, Is.EqualTo(0));
            Assert.That(DeviceScreenRules.ViewFor(rower.Status.Value), Is.EqualTo(DeviceViewKind.Scan));
            Assert.That(logger.Lines, Is.Empty);
            rower.Dispose();
        }

        [Test, Category("AC-BLE-13-05")]
        public void ScanAndSearchDoNothing()
        {
            var logger = new RecordingLogger();
            var rower = new UnavailableRowerService(logger);
            var statuses = new StateRecorder<BleStatus>(rower.Status);
            var devices = new StateRecorder<IReadOnlyList<BleScanResult>>(rower.Devices);
            var raw = new StateRecorder<byte[]>(rower.RawBytes);
            var store = new DeviceStore(rower, logger);

            var scan = store.StartScanAsync();
            Assert.That(scan.IsCompleted && !scan.IsFaulted, Is.True);
            store.StopScanAsync();
            store.ConnectAsync("AA:BB");
            store.DisconnectAsync();

            Assert.That(statuses.Count, Is.EqualTo(0), "ni estado");
            Assert.That(devices.Count, Is.EqualTo(0));
            Assert.That(raw.Count, Is.EqualTo(0));
            Assert.That(store.State.Value.Error, Is.Null, "ni error");
            Assert.That(logger.Lines, Is.Empty, "ni mensaje");
            var spec = DeviceScreenRules.Scan(store.State.Value.Status, false, store.State.Value.ScanResults.Count);
            Assert.That(spec.SearchEnabled, Is.True, "Buscar dispositivos habilitado");
            Assert.That(spec.ShowEmptyState, Is.True, "y el estado vacío");
            store.Dispose();
            rower.Dispose();
        }

        [Test, Category("AC-BLE-13-05")]
        public void HeartRateStaysWithoutSensorAndSearchDoesNothing()
        {
            var logger = new RecordingLogger();
            var hrm = new UnavailableHeartRateService(logger);
            var store = new HeartRateStore(hrm, logger);
            var recorder = new StateRecorder<HeartRateState>(store.State);

            Assert.That(HrmCardRules.BodyFor(store.State.Value), Is.EqualTo(HrmCardBody.NoSensor), "la tarjeta en Sin sensor");
            store.StartScanAsync();
            store.RetryAsync();
            store.ConnectAsync("AA:BB", "X");
            store.DisconnectAsync();
            store.ForgetAsync();
            hrm.StopScanAsync();
            hrm.AutoConnectAsync();

            Assert.That(store.State.Value.Status, Is.EqualTo(HrmStatus.Disconnected));
            Assert.That(store.State.Value.Error, Is.Null);
            Assert.That(store.State.Value.Bpm, Is.EqualTo(0));
            Assert.That(store.State.Value.HasRemembered, Is.False);
            Assert.That(store.State.Value.IsRetrying, Is.False);
            Assert.That(HrmCardRules.BodyFor(store.State.Value), Is.EqualTo(HrmCardBody.NoSensor), "Buscar no hace nada");
            foreach (var state in recorder.Values)
            {
                Assert.That(state.Status, Is.EqualTo(HrmStatus.Disconnected));
            }

            Assert.That(logger.Lines, Is.Empty);
            store.Dispose();
            hrm.Dispose();
        }

        [Test, Category("AC-BLE-13-05")]
        public void EveryMethodCompletesAtOnce()
        {
            var logger = new RecordingLogger();
            var rower = new UnavailableRowerService(logger);
            var hrm = new UnavailableHeartRateService(logger);
            var tasks = new[]
            {
                rower.StartScanAsync(TimeSpan.FromSeconds(10)), rower.StopScanAsync(), rower.ConnectAsync("x"), rower.DisconnectAsync(),
                hrm.StartScanAsync(), hrm.StopScanAsync(), hrm.ConnectAsync("x"), hrm.AutoConnectAsync(), hrm.DisconnectAsync(),
                hrm.ForgetAsync(),
            };
            foreach (var task in tasks)
            {
                Assert.That(task.IsCompleted, Is.True);
                Assert.That(task.IsFaulted, Is.False);
            }

            Assert.That(hrm.CurrentBpm, Is.EqualTo(0));
            Assert.That(hrm.ConnectedDeviceName, Is.Null);
            Assert.That(hrm.RememberedDeviceName, Is.Null);
            Assert.DoesNotThrow(() => rower.Dispose());
            Assert.DoesNotThrow(() => hrm.Dispose());
        }

        [Test]
        public void LoggerIsRequired()
        {
            Assert.Throws<ArgumentNullException>(() => new UnavailableRowerService(null!));
            Assert.Throws<ArgumentNullException>(() => new UnavailableHeartRateService(null!));
        }
    }
}
