using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Models;

namespace RowMate.Tests.Support
{
    // El remo de prueba de la spec §0.4 (02-tech-design §4.5 de la It. 4a): manda L(k) cada segundo por un timer del
    // programador (también en pausa y en segundo plano: en segundo plano queda pendiente y llega en el alcance, como el
    // buffer nativo de la 10a). L(0) sale en Start(); L(k) k segundos después. Con el reloj de las pruebas (LifecycleHarness y
    // WorkoutHarness) Start() corre en el instante −0,5, así L(k) llega en el instante k − 0,5. Arranca Connected.
    // Disconnect (AC-WKT-03-10): estado Disconnected y no manda lecturas (la desconexión simulada no manda ceros, M-12);
    // Connect: Connected y sigue con la lectura que toque.
    public sealed class ScriptedRower : IRowerService
    {
        private readonly IScheduler _scheduler;
        private readonly Func<int, RowingData> _reading;
        private readonly ObservableValue<BleStatus> _status;
        private readonly ObservableValue<BleAdapterState> _adapter;
        private readonly EventStream<RowingData> _data;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly EventStream<byte[]> _rawBytes;
        private IDisposable? _timer;
        private bool _connected = true;
        private bool _disposed;

        public ScriptedRower(IScheduler scheduler, Func<int, RowingData> reading, IAppLogger logger)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _reading = reading ?? throw new ArgumentNullException(nameof(reading));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _status = new ObservableValue<BleStatus>(BleStatus.Connected, logger);
            _adapter = new ObservableValue<BleAdapterState>(BleAdapterState.On, logger);
            _data = new EventStream<RowingData>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
            _rawBytes = new EventStream<byte[]>(logger);
            Status = new ReadOnlyObservable<BleStatus>(_status);
            AdapterState = new ReadOnlyObservable<BleAdapterState>(_adapter);
        }

        public IReadOnlyObservable<BleStatus> Status { get; }

        public IReadOnlyObservable<BleAdapterState> AdapterState { get; }

        public IEventSource<RowingData> Data => _data;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public IEventSource<byte[]> RawBytes => _rawBytes;

        public string? ConnectedDeviceName => _status.Value == BleStatus.Connected ? "Remo de prueba" : null;

        // El índice k de la próxima lectura. It. 4b: el arnés lo fija al empezar la sesión (L(n) en los n − 0,5 de la sesión, 04b §5.2).
        public int Next { get; set; }

        // La última lectura mandada.
        public RowingData? Last { get; private set; }

        // L(0) en el acto y L(k) cada segundo. Idempotente.
        public void Start()
        {
            if (_disposed || _timer != null)
            {
                return;
            }

            EmitNext();
            _timer = _scheduler.SchedulePeriodic(TimeSpan.FromSeconds(1), EmitNext);
        }

        public void Disconnect()
        {
            _connected = false;
            _status.Value = BleStatus.Disconnected;
        }

        public void Connect()
        {
            _connected = true;
            _status.Value = BleStatus.Connected;
        }

        public void SetStatus(BleStatus status)
        {
            _connected = status == BleStatus.Connected;
            _status.Value = status;
        }

        // Una lectura a mano (además del guion).
        public void Emit(RowingData reading)
        {
            Last = reading;
            _data.Emit(reading);
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
            Connect();
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            Disconnect();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }

        private void EmitNext()
        {
            var k = Next++;
            if (!_connected || _disposed)
            {
                return;
            }

            Emit(_reading(k));
        }
    }
}
