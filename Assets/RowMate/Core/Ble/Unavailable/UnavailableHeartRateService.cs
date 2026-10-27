using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;

namespace RowMate.Core.Ble.Unavailable
{
    // Pulsómetro del build normal de la It. 2 (P-04, AC-BLE-13-05): como el legacy sin Bluetooth LE. Desconectado, sin
    // sensor recordado ni reintentos (la tarjeta queda en "Sin sensor"), ninguna fuente emite y "Buscar" no hace nada.
    // Lo reemplaza el pulsómetro real en la It. 11.
    // legacy: lib/core/bluetooth/heart_rate_service.dart:108-113 (startScan: if (!await FlutterBluePlus.isSupported) return;)
    public sealed class UnavailableHeartRateService : IHeartRateService
    {
        private readonly EventStream<int> _bpm;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;

        public UnavailableHeartRateService(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Status = new ReadOnlyObservable<HrmStatus>(new ObservableValue<HrmStatus>(HrmStatus.Disconnected, logger));
            _bpm = new EventStream<int>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
        }

        public IReadOnlyObservable<HrmStatus> Status { get; }

        public IEventSource<int> Bpm => _bpm;

        public int CurrentBpm => 0;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public string? ConnectedDeviceName => null;

        public string? RememberedDeviceName => null;

        public bool IsRetrying => false;

        public Task StartScanAsync(TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }

        public Task StopScanAsync()
        {
            return Task.CompletedTask;
        }

        public Task ConnectAsync(string deviceId)
        {
            return Task.CompletedTask;
        }

        public Task AutoConnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task ForgetAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
