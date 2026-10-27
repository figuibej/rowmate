using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Text;

namespace RowMate.Simulator
{
    // Pulsómetro simulado (F-HRM-15): reemplazo de HeartRateService en modo simulador. Un único sensor sintético que publica
    // el pulso que calcula el RowingSimulator compartido con el remo (AC-HRM-15-09). No guarda nada: la tarjeta siempre abre
    // en "Sin sensor" (AC-HRM-15-01). Port línea a línea, con los timers en el IScheduler inyectado; los ...Async completan
    // en el acto. El estado se reemite (ForceNotify) cuando el legacy repite el aviso (búsqueda repetida, forget).
    // Después de Dispose ningún método hace nada (AC-HRM-15-11).
    // legacy: lib/core/bluetooth/simulated_heart_rate_service.dart:9-156
    public sealed class SimulatedHeartRateService : IHeartRateService
    {
        public const string Tag = "SIM";

        // :13, fijo en los dos idiomas (Q-03).
        public const string DeviceName = "Pulsómetro simulado";

        // :14
        public const string DeviceId = "SIM:HRM";

        // :61
        public const int Rssi = -50;

        // :65
        public static readonly TimeSpan DefaultScanTimeout = TimeSpan.FromSeconds(10);

        // :107
        public static readonly TimeSpan DefaultReconnectAfter = TimeSpan.FromSeconds(3);

        // :126
        public static readonly TimeSpan ReadingPeriod = TimeSpan.FromSeconds(1);

        private readonly IScheduler _scheduler;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<HrmStatus> _status;
        private readonly EventStream<int> _bpm;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;

        private int _currentBpm;
        private IDisposable? _dataTimer;
        private IDisposable? _scanTimer;
        private IDisposable? _reconnectTimer;
        private bool _disposed;

        public SimulatedHeartRateService(RowingSimulator simulator, IScheduler scheduler, IAppLogger logger)
        {
            Simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _status = new ObservableValue<HrmStatus>(HrmStatus.Disconnected, logger); // :24
            Status = new ReadOnlyObservable<HrmStatus>(_status);
            _bpm = new EventStream<int>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
        }

        public RowingSimulator Simulator { get; }

        public IReadOnlyObservable<HrmStatus> Status { get; }

        public IEventSource<int> Bpm => _bpm;

        public int CurrentBpm => _currentBpm;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        // :41-42 (AC-HRM-15-05): solo mientras está conectado.
        public string? ConnectedDeviceName => _status.Value == HrmStatus.Connected ? DeviceName : null;

        // :44-48: no se persiste nada; solo mientras vuelve de una caída simulada se expone el nombre, para que la tarjeta
        // muestre "reconectando...".
        public string? RememberedDeviceName => _reconnectTimer != null ? DeviceName : null;

        public bool IsRetrying => _reconnectTimer != null;

        // :50-63: SIM:HRM, sin nombre de plataforma, anunciado "Pulsómetro simulado", −50 dBm y el servicio 0x180D.
        public static BleScanResult FakeScanResult()
        {
            return new BleScanResult(DeviceId, null, DeviceName, Rssi, new[] { HeartRateParser.Service });
        }

        // :65-77 (AC-HRM-15-02, -12): un escaneo del usuario reemplaza a la vuelta automática; la lista llega en el acto con
        // un único sensor; si nadie lo elige, a los 10 s (o el plazo pedido) pasa a desconectado.
        public Task StartScanAsync(TimeSpan? timeout = null)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            CancelReconnect();
            _scanTimer?.Dispose();
            _scanTimer = null;
            SetStatus(HrmStatus.Scanning);
            _devices.Emit(new[] { FakeScanResult() });
            _scanTimer = _scheduler.Schedule(timeout ?? DefaultScanTimeout, OnScanTimeout);
            return Task.CompletedTask;
        }

        // :79-84 (AC-HRM-15-03): si está buscando, pasa a desconectado; si no, nada.
        public Task StopScanAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            _scanTimer?.Dispose();
            _scanTimer = null;
            if (_status.Value == HrmStatus.Scanning)
            {
                SetStatus(HrmStatus.Disconnected);
            }

            return Task.CompletedTask;
        }

        // :86-87 (AC-HRM-15-04): a cualquier dispositivo, directo a conectado.
        public Task ConnectAsync(string deviceId)
        {
            Connect();
            return Task.CompletedTask;
        }

        // :89-91: sin sensor recordado en modo simulador, no conecta solo (AC-APP-01-04, AC-HRM-15-01).
        public Task AutoConnectAsync()
        {
            return Task.CompletedTask;
        }

        // :93-98 (AC-HRM-15-06): cancela la vuelta, deja de publicar, publica 0 si hacía falta y pasa a desconectado.
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

        // :100-105 (AC-HRM-15-07): cancela la vuelta y vuelve a avisar el estado; una conexión activa no se corta.
        public Task ForgetAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            CancelReconnect();
            _status.ForceNotify();
            return Task.CompletedTask;
        }

        // :107-116 (AC-HRM-15-08): solo conectado. La vuelta se agenda ANTES de cortar, así "desconectado" llega con
        // IsRetrying = true y el recordado puesto (la tarjeta muestra "Pulsómetro simulado · reconectando...").
        public void SimulateDisconnect(TimeSpan? reconnectAfter = null)
        {
            if (_disposed || _status.Value != HrmStatus.Connected)
            {
                return;
            }

            var after = reconnectAfter ?? DefaultReconnectAfter;
            _logger.Log(LogLevel.Info, Tag, "Pulsómetro desconectado, reconectando en " + Fmt.Integer((long)after.TotalSeconds) + "s");
            _reconnectTimer = _scheduler.Schedule(after, OnReconnect);
            Stop();
        }

        // :147-155: cancela todos los timers.
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _dataTimer?.Dispose();
            _dataTimer = null;
            _scanTimer?.Dispose();
            _scanTimer = null;
            CancelReconnect();
        }

        private void OnScanTimeout()
        {
            _scanTimer = null;
            if (_status.Value == HrmStatus.Scanning)
            {
                SetStatus(HrmStatus.Disconnected);
            }
        }

        private void OnReconnect()
        {
            _reconnectTimer = null;
            Connect();
        }

        // :118-130: cancela la búsqueda y la vuelta; si ya está conectado, nada; si no, conectado y cada 1 s el pulso del
        // simulador (la primera lectura al segundo: hasta entonces la tarjeta muestra "--").
        private void Connect()
        {
            if (_disposed)
            {
                return;
            }

            _scanTimer?.Dispose();
            _scanTimer = null;
            CancelReconnect();
            if (_status.Value == HrmStatus.Connected)
            {
                return;
            }

            // Por si un escaneo dejó el timer anterior corriendo (:124). Se agenda ANTES de avisar (entrega sincrónica, D-04).
            _dataTimer?.Dispose();
            _dataTimer = _scheduler.SchedulePeriodic(ReadingPeriod, EmitReading);
            SetStatus(HrmStatus.Connected);
        }

        // :132-140: deja de publicar; si el último no era 0, publica 0 (una sola vez, AC-HRM-04-04); desconectado.
        private void Stop()
        {
            _dataTimer?.Dispose();
            _dataTimer = null;
            if (_currentBpm != 0)
            {
                _currentBpm = 0;
                _bpm.Emit(0);
            }

            if (_status.Value != HrmStatus.Disconnected)
            {
                SetStatus(HrmStatus.Disconnected);
            }
        }

        // :126-129: el último pulso calculado por el simulador, aunque repita el anterior (AC-HRM-04-01).
        private void EmitReading()
        {
            _currentBpm = Simulator.HeartRate;
            _bpm.Emit(_currentBpm);
        }

        private void CancelReconnect()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
        }

        // :142-145: el legacy emite siempre, aunque el estado se repita.
        private void SetStatus(HrmStatus status)
        {
            if (_status.Value == status)
            {
                _status.ForceNotify();
                return;
            }

            _status.Value = status;
        }
    }
}
