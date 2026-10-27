using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;

namespace RowMate.Core.Ble.Unavailable
{
    // Remo del build normal de la It. 2 (P-04, AC-BLE-13-05): se comporta como el legacy en un equipo sin Bluetooth LE.
    // Desconectado, adaptador Unknown (así Dispositivo muestra la vista de búsqueda con el botón y el estado vacío; solo Off
    // y Unauthorized cambian de vista, device_screen.dart:346-347), ninguna fuente emite y todos los métodos terminan en el
    // acto sin cambiar nada ni registrar errores. Es código de producción, no un doble de prueba. Lo reemplaza el remo real
    // en la It. 10a.
    // legacy: lib/core/bluetooth/ble_service.dart:65-67 (startScan: if (!supported) return;)
    public sealed class UnavailableRowerService : IRowerService
    {
        private readonly EventStream<RowingData> _data;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly EventStream<byte[]> _rawBytes;

        public UnavailableRowerService(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Status = new ReadOnlyObservable<BleStatus>(new ObservableValue<BleStatus>(BleStatus.Disconnected, logger));
            AdapterState = new ReadOnlyObservable<BleAdapterState>(new ObservableValue<BleAdapterState>(BleAdapterState.Unknown, logger));
            _data = new EventStream<RowingData>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
            _rawBytes = new EventStream<byte[]>(logger);
        }

        public IReadOnlyObservable<BleStatus> Status { get; }

        public IReadOnlyObservable<BleAdapterState> AdapterState { get; }

        public IEventSource<RowingData> Data => _data;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public IEventSource<byte[]> RawBytes => _rawBytes;

        public string? ConnectedDeviceName => null;

        public void Start()
        {
        }

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

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
