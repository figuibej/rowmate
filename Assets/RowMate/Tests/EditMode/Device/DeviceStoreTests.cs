using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Device
{
    // DeviceStore = lib/features/device/device_provider.dart sin el vigilante ni el diagnóstico (It. 10a), con el remo de prueba.
    [TestFixture]
    public sealed class DeviceStoreTests
    {
        private static readonly RowingData Image01 = new RowingData
        {
            StrokeRate = 23.5, PowerWatts = 149, DistanceMeters = 652, Pace500mSeconds = 133, StrokeCount = 80, ElapsedSeconds = 170,
            TotalCalories = 12,
        };

        [Test]
        public void InitialStateComesFromTheService()
        {
            var rower = new ControllableRowerService(adapterState: BleAdapterState.Unknown);
            var store = new DeviceStore(rower, new RecordingLogger());
            var state = store.State.Value;
            Assert.That(state.Status, Is.EqualTo(BleStatus.Disconnected));
            Assert.That(state.AdapterState, Is.EqualTo(BleAdapterState.Unknown));
            Assert.That(state.Data, Is.EqualTo(RowingData.Empty));
            Assert.That(state.ScanResults, Is.Empty);
            Assert.That(state.Error, Is.Null);
            Assert.That(state.IsConnected, Is.False);
            store.Dispose();
        }

        [Test, Category("AC-BLE-11-09")]
        public void ZerosOnEveryConnect()
        {
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, new RecordingLogger());
            rower.Emit(BleStatus.Connected);
            Assert.That(store.State.Value.Data, Is.EqualTo(RowingData.Empty), "ceros hasta la primera lectura");
            rower.EmitData(Image01);
            Assert.That(store.State.Value.Data, Is.EqualTo(Image01));

            rower.Emit(BleStatus.Disconnected);
            Assert.That(store.State.Value.Data, Is.EqualTo(Image01), "desconectar no borra la última lectura");
            rower.Emit(BleStatus.Connected);
            Assert.That(store.State.Value.Data, Is.EqualTo(RowingData.Empty), "cada vez que pasa a conectado, ceros otra vez");
            store.Dispose();
        }

        [Test, Category("AC-BLE-11-09")]
        public void EveryReadingNotifiesEvenIfItRepeats()
        {
            // El legacy llama a notifyListeners en cada lectura (una por segundo con el simulador), aunque repita los valores.
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, new RecordingLogger());
            rower.Emit(BleStatus.Connected);
            var recorder = new StateRecorder<DeviceState>(store.State);
            rower.EmitData(Image01);
            rower.EmitData(Image01);
            rower.EmitData(Image01 with { DistanceMeters = 653 });
            Assert.That(recorder.Count, Is.EqualTo(3));
            Assert.That(recorder.Last.Data.DistanceMeters, Is.EqualTo(653));
            store.Dispose();
        }

        [Test]
        public void EveryServiceEventNotifies()
        {
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, new RecordingLogger());
            var recorder = new StateRecorder<DeviceState>(store.State);
            rower.EmitAdapterState(BleAdapterState.Off);
            rower.Emit(BleStatus.Scanning);
            var found = new[] { new BleScanResult("AA:BB", "AMS-670B", null, -60, new BleUuid[0]) };
            rower.EmitDevices(found);
            Assert.That(recorder.Count, Is.EqualTo(3));
            Assert.That(store.State.Value.AdapterState, Is.EqualTo(BleAdapterState.Off));
            Assert.That(store.State.Value.Status, Is.EqualTo(BleStatus.Scanning));
            Assert.That(store.State.Value.ScanResults, Is.SameAs(found));
            store.Dispose();
        }

        [Test]
        public void SnapshotsTheConnectedNameOfTheService()
        {
            var rower = new ControllableRowerService { ConnectedName = "AMS-670B" };
            var store = new DeviceStore(rower, new RecordingLogger());
            Assert.That(store.State.Value.ConnectedDeviceName, Is.Null);
            rower.Emit(BleStatus.Connected);
            Assert.That(store.State.Value.ConnectedDeviceName, Is.EqualTo("AMS-670B"));
            Assert.That(DeviceScreenRules.DeviceName(store.State.Value.ConnectedDeviceName), Is.EqualTo("AMS-670B"));
            rower.Emit(BleStatus.Disconnected);
            Assert.That(store.State.Value.ConnectedDeviceName, Is.Null);
            store.Dispose();
        }

        [Test]
        public void StartScanClearsErrorAndListThenKeepsTheFailure()
        {
            // :104-114: borra el error y la lista, avisa y busca; una falla queda en el estado (el banner es de la It. 10a).
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, new RecordingLogger());
            rower.EmitDevices(new[] { new BleScanResult("AA:BB", "X", null, -60, new BleUuid[0]) });
            rower.ScanError = new InvalidOperationException("scan");
            var recorder = new StateRecorder<DeviceState>(store.State);
            var task = store.StartScanAsync();
            Assert.That(task.IsCompleted && !task.IsFaulted, Is.True, "la excepción no sube");
            Assert.That(recorder.Values[0].ScanResults, Is.Empty, "antes de buscar se borra la lista");
            Assert.That(recorder.Values[0].Error, Is.Null);
            Assert.That(store.State.Value.Error, Is.InstanceOf<InvalidOperationException>());

            rower.ScanError = null;
            store.StartScanAsync();
            Assert.That(store.State.Value.Error, Is.Null, "buscar de nuevo empieza limpio");
            Assert.That(rower.StartScanCalls, Is.EqualTo(2));
            store.Dispose();
        }

        [Test]
        public void ConnectFailureIsKept()
        {
            var rower = new ControllableRowerService { ConnectError = new InvalidOperationException("gatt 133") };
            var store = new DeviceStore(rower, new RecordingLogger());
            store.ConnectAsync("AA:BB");
            Assert.That(rower.ConnectCalls, Is.EqualTo(new[] { "AA:BB" }));
            Assert.That(store.State.Value.Error!.Message, Is.EqualTo("gatt 133"));
            rower.ConnectError = null;
            store.ConnectAsync("AA:BB");
            Assert.That(store.State.Value.Error, Is.Null);
            store.Dispose();
        }

        [Test]
        public void DisconnectAndStopScanGoToTheService()
        {
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, new RecordingLogger());
            store.DisconnectAsync();
            store.StopScanAsync();
            Assert.That(rower.DisconnectCalls, Is.EqualTo(1), "sin confirmación (Q-33)");
            Assert.That(rower.StopScanCalls, Is.EqualTo(1));
            store.Dispose();
        }

        [Test]
        public void NoNotificationAfterDispose()
        {
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, new RecordingLogger());
            var recorder = new StateRecorder<DeviceState>(store.State);
            store.Dispose();
            rower.Emit(BleStatus.Connected);
            rower.EmitData(Image01);
            store.StartScanAsync();
            store.ConnectAsync("x");
            Assert.That(recorder.Count, Is.EqualTo(0));
            Assert.That(rower.StartScanCalls, Is.EqualTo(0), "muerto, no busca");
            Assert.DoesNotThrow(() => store.Dispose());
        }

        [Test]
        public void SubscriberFailureDoesNotStopTheOthers()
        {
            // QA-V-01: una vista que falla al recibir un estado no congela a las otras (01-po-spec §12.6).
            var logger = new RecordingLogger();
            var rower = new ControllableRowerService();
            var store = new DeviceStore(rower, logger);
            var seen = new List<BleStatus>();
            store.State.Subscribe(_ => throw new InvalidOperationException("vista rota"), false);
            store.State.Subscribe(s => seen.Add(s.Status), false);
            rower.Emit(BleStatus.Connected);
            Assert.That(seen, Is.EqualTo(new[] { BleStatus.Connected }));
            Assert.That(logger.Lines.Count, Is.EqualTo(1));
            store.Dispose();
        }
    }
}
