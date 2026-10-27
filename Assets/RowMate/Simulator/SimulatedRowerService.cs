using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Text;

namespace RowMate.Simulator
{
    // Remo FTMS simulado y "conectado" (F-BLE-15): reemplazo de BleService en modo simulador. Port línea a línea, con los
    // timers en el IScheduler inyectado (reloj falso en los tests, ADR 005). Los métodos ...Async completan en el acto, como
    // los async del legacy que no esperan nada. Después de Dispose ningún método hace nada (AC-APP-01-08).
    // Diferencias que no se ven: los avisos llegan en el acto y no en un microtask (D-04), y la conexión inicial la dispara
    // Start() en lugar del primer oyente del estado (D-05).
    // legacy: lib/core/bluetooth/simulated_ble_service.dart:8-120
    public sealed class SimulatedRowerService : IRowerService
    {
        public const string Tag = "SIM";

        // :11, fijo en los dos idiomas (Q-03).
        public const string DeviceName = "Remo simulado";

        // :93
        public static readonly TimeSpan TickPeriod = TimeSpan.FromSeconds(1);

        // :81
        public static readonly TimeSpan DefaultReconnectAfter = TimeSpan.FromSeconds(3);

        private readonly IScheduler _scheduler;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<BleStatus> _status;
        private readonly EventStream<RowingData> _data;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly EventStream<byte[]> _rawBytes;

        private IDisposable? _dataTimer;
        private IDisposable? _reconnectTimer;
        private bool _started;
        private bool _disposed;

        public SimulatedRowerService(RowingSimulator simulator, IScheduler scheduler, IAppLogger logger)
        {
            Simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _status = new ObservableValue<BleStatus>(BleStatus.Disconnected, logger); // :37
            Status = new ReadOnlyObservable<BleStatus>(_status);
            // El adaptador figura siempre encendido (:22-26, :55): con el simulador nunca se ve "Bluetooth apagado".
            AdapterState = new ReadOnlyObservable<BleAdapterState>(new ObservableValue<BleAdapterState>(BleAdapterState.On, logger));
            _data = new EventStream<RowingData>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
            _rawBytes = new EventStream<byte[]>(logger);
        }

        // La física compartida con el pulsómetro simulado (:29).
        public RowingSimulator Simulator { get; }

        public IReadOnlyObservable<BleStatus> Status { get; }

        public IReadOnlyObservable<BleAdapterState> AdapterState { get; }

        public IEventSource<RowingData> Data => _data;

        // Nunca emiten (:34-35, AC-BLE-15-08): sin lista de equipos ni paquetes crudos.
        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public IEventSource<byte[]> RawBytes => _rawBytes;

        // :59-60 (AC-BLE-15-01): el nombre existe solo mientras está conectado.
        public string? ConnectedDeviceName => _status.Value == BleStatus.Connected ? DeviceName : null;

        public bool IsReconnectPending => _reconnectTimer != null;

        // :17-21: se conecta una sola vez en la vida del servicio, cuando la app empieza a escuchar (AC-BLE-15-02).
        public void Start()
        {
            if (_disposed || _started)
            {
                return;
            }

            _started = true;
            Connect();
        }

        // :62-65: no hay equipos que buscar: "Buscar" reconecta al remo simulado en el acto (AC-BLE-15-04).
        public Task StartScanAsync(TimeSpan? timeout = null)
        {
            Connect();
            return Task.CompletedTask;
        }

        // :67-68: no hace nada.
        public Task StopScanAsync()
        {
            return Task.CompletedTask;
        }

        // :70-71
        public Task ConnectAsync(string deviceId)
        {
            Connect();
            return Task.CompletedTask;
        }

        // :73-78: cancela una vuelta pendiente y corta (AC-BLE-07-02 a -04, AC-BLE-15-05). No se reconecta solo.
        public Task DisconnectAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            CancelReconnect();
            Stop();
            return Task.CompletedTask;
        }

        // :80-86 (AC-BLE-15-06): solo conectado; corta y vuelve a los 3 s. La física no se toca: distancia, paladas y tiempo
        // siguen desde donde iban (AC-BLE-15-09).
        public void SimulateDisconnect(TimeSpan? reconnectAfter = null)
        {
            if (_disposed || _status.Value != BleStatus.Connected)
            {
                return;
            }

            var after = reconnectAfter ?? DefaultReconnectAfter;
            _logger.Log(LogLevel.Info, Tag, "Desconexión simulada, reconectando en " + Fmt.Integer((long)after.TotalSeconds) + "s");
            Stop();
            _reconnectTimer = _scheduler.Schedule(after, Connect);
        }

        // :110-119: cancela todos los timers.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dataTimer?.Dispose();
            _dataTimer = null;
            CancelReconnect();
        }

        // :88-97: cancela la vuelta pendiente; si ya está conectado, nada (AC-BLE-15-07); si no, conectado y una lectura por
        // segundo con el pulso en 0, como un monitor real: el pulso llega solo por el pulsómetro (AC-BLE-15-03).
        private void Connect()
        {
            if (_disposed)
            {
                return;
            }

            CancelReconnect();
            if (_status.Value == BleStatus.Connected)
            {
                return;
            }

            // El timer se agenda ANTES de avisar: con la entrega sincrónica (D-04), un oyente que desconecta al enterarse no deja
            // un timer de lecturas vivo. Las lecturas empiezan al segundo, igual que en el legacy.
            _dataTimer?.Dispose();
            _dataTimer = _scheduler.SchedulePeriodic(TickPeriod, EmitReading);
            _status.Value = BleStatus.Connected;
        }

        // :99-103
        private void Stop()
        {
            _dataTimer?.Dispose();
            _dataTimer = null;
            if (_status.Value != BleStatus.Disconnected)
            {
                _status.Value = BleStatus.Disconnected;
            }
        }

        private void CancelReconnect()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }

        // :95: simulator.tick().copyWith(heartRate: 0)
        private void EmitReading()
        {
            _data.Emit(Simulator.Tick().WithHeartRate(0));
        }
    }
}
