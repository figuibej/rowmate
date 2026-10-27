using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Models;

namespace RowMate.Tests.Support
{
    // "Remo de prueba" (01-po-spec §0.4): el test fija su estado (desconectado, buscando, conectando, conectado) y manda las
    // lecturas que quiere, incluido un pulso por FTMS. No se conecta solo: buscar, conectar y desconectar solo se registran.
    // Equivale a _ControllableBleService (rowmate-legacy test/heart_rate_card_test.dart:25-48), con lecturas a pedido.
    // Solo para tests: nunca entra en un build (el asmdef de Tests/Support exige UNITY_INCLUDE_TESTS).
    public sealed class ControllableRowerService : IRowerService
    {
        private readonly ObservableValue<BleStatus> _status;
        private readonly ObservableValue<BleAdapterState> _adapterState;
        private readonly EventStream<RowingData> _data;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly EventStream<byte[]> _rawBytes;
        private readonly List<string> _connectCalls = new List<string>();

        public ControllableRowerService(IAppLogger? logger = null, BleStatus initial = BleStatus.Disconnected,
                                        BleAdapterState adapterState = BleAdapterState.On)
        {
            var log = logger ?? NullAppLogger.Instance;
            _status = new ObservableValue<BleStatus>(initial, log);
            _adapterState = new ObservableValue<BleAdapterState>(adapterState, log);
            Status = new ReadOnlyObservable<BleStatus>(_status);
            AdapterState = new ReadOnlyObservable<BleAdapterState>(_adapterState);
            _data = new EventStream<RowingData>(log);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(log);
            _rawBytes = new EventStream<byte[]>(log);
        }

        public IReadOnlyObservable<BleStatus> Status { get; }

        public IReadOnlyObservable<BleAdapterState> AdapterState { get; }

        public IEventSource<RowingData> Data => _data;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public IEventSource<byte[]> RawBytes => _rawBytes;

        // El nombre que informa con el remo conectado (null por defecto: la cabecera muestra "Rower", como el legacy).
        public string? ConnectedName { get; set; }

        public string? ConnectedDeviceName => _status.Value == BleStatus.Connected ? ConnectedName : null;

        // Fallas a pedido de StartScanAsync y ConnectAsync (para los errores de DeviceStore).
        public Exception? ScanError { get; set; }

        public Exception? ConnectError { get; set; }

        public int StartCalls { get; private set; }

        public int StartScanCalls { get; private set; }

        public int StopScanCalls { get; private set; }

        public int DisconnectCalls { get; private set; }

        public IReadOnlyList<string> ConnectCalls => _connectCalls;

        public bool Disposed { get; private set; }

        // = _ControllableBleService.emit: fija el estado y lo avisa (aunque se repita, como un StreamController).
        public void Emit(BleStatus status)
        {
            if (_status.Value == status)
            {
                _status.ForceNotify();
                return;
            }

            _status.Value = status;
        }

        public void EmitAdapterState(BleAdapterState state)
        {
            _adapterState.Value = state;
        }

        // Una lectura FTMS (con el pulso que quiera el test).
        public void EmitData(RowingData data)
        {
            _data.Emit(data);
        }

        public void EmitDevices(IReadOnlyList<BleScanResult> devices)
        {
            _devices.Emit(devices);
        }

        public void EmitRawBytes(byte[] bytes)
        {
            _rawBytes.Emit(bytes);
        }

        public void Start()
        {
            StartCalls++;
        }

        public Task StartScanAsync(TimeSpan? timeout = null)
        {
            StartScanCalls++;
            return ScanError == null ? Task.CompletedTask : Failed(ScanError);
        }

        public Task StopScanAsync()
        {
            StopScanCalls++;
            return Task.CompletedTask;
        }

        public Task ConnectAsync(string deviceId)
        {
            _connectCalls.Add(deviceId);
            return ConnectError == null ? Task.CompletedTask : Failed(ConnectError);
        }

        public Task DisconnectAsync()
        {
            DisconnectCalls++;
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
