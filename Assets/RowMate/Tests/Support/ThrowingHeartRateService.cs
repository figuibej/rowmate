using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Servicio que falla en toda acción, para probar el manejo de errores (LT-33). 1 a 1 con _ThrowingHrm:
    // startScan → StateError('scan'), connect → HrmIncompatibleException, disconnect → StateError('disconnect');
    // stopScan, autoConnect y forget no hacen nada. StateError de Dart = InvalidOperationException.
    // legacy: rowmate-legacy test/heart_rate_provider_test.dart:11-51
    public sealed class ThrowingHeartRateService : IHeartRateService
    {
        private readonly EventStream<int> _bpm;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;

        public ThrowingHeartRateService(IAppLogger? logger = null)
        {
            var log = logger ?? NullAppLogger.Instance;
            Status = new ReadOnlyObservable<HrmStatus>(new ObservableValue<HrmStatus>(HrmStatus.Disconnected, log)); // :22
            _bpm = new EventStream<int>(log);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(log);
        }

        public IReadOnlyObservable<HrmStatus> Status { get; }

        public IEventSource<int> Bpm => _bpm;

        public int CurrentBpm => 0; // :24

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public string? ConnectedDeviceName => null;

        public string? RememberedDeviceName => null;

        public bool IsRetrying => false;

        public bool Disposed { get; private set; }

        // :32-33
        public Task StartScanAsync(TimeSpan? timeout = null)
        {
            return Failed(new InvalidOperationException("scan"));
        }

        // :35
        public Task StopScanAsync()
        {
            return Task.CompletedTask;
        }

        // :37-38
        public Task ConnectAsync(string deviceId)
        {
            return Failed(new HrmIncompatibleException());
        }

        // :40
        public Task AutoConnectAsync()
        {
            return Task.CompletedTask;
        }

        // :42
        public Task DisconnectAsync()
        {
            return Failed(new InvalidOperationException("disconnect"));
        }

        // :44
        public Task ForgetAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Disposed = true;
        }

        private static Task Failed(Exception error)
        {
            var source = new TaskCompletionSource<bool>();
            source.SetException(error);
            return source.Task;
        }
    }
}
