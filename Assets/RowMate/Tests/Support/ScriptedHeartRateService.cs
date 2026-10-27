using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Qué hace ScriptedHeartRateService.ConnectAsync.
    public enum ScriptedConnect
    {
        // Pasa a conectado en el acto, con ConnectedName.
        Connect,

        // Queda en "conectando" (conexión manual en curso: CAP-14a).
        StayConnecting,

        // Falla con HrmIncompatibleException (CAP-14c).
        ThrowIncompatible,

        // Falla con InvalidOperationException(ErrorText) (CAP-14d).
        ThrowError,

        // Devuelve una tarea que el test completa o hace fallar después (acción lenta, AC-HRM-12-06).
        Pending,
    }

    // "Pulsómetro de prueba" (01-po-spec §0.4): estado, pulso, "reintentando", sensor recordado, resultados de búsqueda y
    // conducta de ConnectAsync, todo a pedido. Es _ThrowingHrm (rowmate-legacy test/heart_rate_provider_test.dart:11-51)
    // ampliado. Solo para tests: nunca entra en un build.
    public sealed class ScriptedHeartRateService : IHeartRateService
    {
        // El sensor recordado de las capturas (CAP-14a/b).
        public const string RememberedSample = "Polar H10 7E1A2B3C";

        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private readonly ObservableValue<HrmStatus> _status;
        private readonly EventStream<int> _bpm;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly List<string> _connectCalls = new List<string>();
        private TaskCompletionSource<bool>? _pending;

        public ScriptedHeartRateService(IAppLogger? logger = null, HrmStatus initial = HrmStatus.Disconnected)
        {
            var log = logger ?? NullAppLogger.Instance;
            _status = new ObservableValue<HrmStatus>(initial, log);
            Status = new ReadOnlyObservable<HrmStatus>(_status);
            _bpm = new EventStream<int>(log);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(log);
        }

        public IReadOnlyObservable<HrmStatus> Status { get; }

        public IEventSource<int> Bpm => _bpm;

        public int CurrentBpm { get; private set; }

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        // Nombre que informa conectado (por defecto, el recordado de muestra).
        public string ConnectedName { get; set; } = RememberedSample;

        public string? ConnectedDeviceName => _status.Value == HrmStatus.Connected ? ConnectedName : null;

        public string? RememberedDeviceName { get; set; }

        public bool IsRetrying { get; set; }

        // Lo que devuelve una búsqueda (por defecto, nada).
        public IReadOnlyList<BleScanResult> ScanResults { get; set; } = NoResults;

        public ScriptedConnect ConnectBehavior { get; set; } = ScriptedConnect.Connect;

        public string ErrorText { get; set; } = "PlatformException(connect_failed, GATT error 133, android.bluetooth.BluetoothGatt, null)";

        // Fallas a pedido de StartScanAsync y AutoConnectAsync.
        public Exception? ScanError { get; set; }

        public Exception? AutoConnectError { get; set; }

        public int StartScanCalls { get; private set; }

        public int StopScanCalls { get; private set; }

        public int AutoConnectCalls { get; private set; }

        public int DisconnectCalls { get; private set; }

        public int ForgetCalls { get; private set; }

        public IReadOnlyList<string> ConnectCalls => _connectCalls;

        public bool Disposed { get; private set; }

        // Fija el estado y lo avisa (aunque se repita).
        public void SetStatus(HrmStatus status)
        {
            if (_status.Value == status)
            {
                _status.ForceNotify();
                return;
            }

            _status.Value = status;
        }

        public void EmitBpm(int bpm)
        {
            CurrentBpm = bpm;
            _bpm.Emit(bpm);
        }

        public void EmitDevices(IReadOnlyList<BleScanResult> devices)
        {
            _devices.Emit(devices);
        }

        // "Polar H10 7E1A2B3C · reconectando..." (retrying = true) o "· no encontrado" (false), desconectado.
        public void Remember(string name, bool retrying)
        {
            RememberedDeviceName = name;
            IsRetrying = retrying;
            SetStatus(HrmStatus.Disconnected);
        }

        // Completa (o hace fallar) la conexión pendiente de ScriptedConnect.Pending.
        public void CompletePending()
        {
            var pending = _pending ?? throw new InvalidOperationException("ScriptedHeartRateService: no hay conexión pendiente.");
            _pending = null;
            pending.SetResult(true);
        }

        public void FailPending(Exception error)
        {
            var pending = _pending ?? throw new InvalidOperationException("ScriptedHeartRateService: no hay conexión pendiente.");
            _pending = null;
            pending.SetException(error);
        }

        public Task StartScanAsync(TimeSpan? timeout = null)
        {
            StartScanCalls++;
            if (ScanError != null)
            {
                return Failed(ScanError);
            }

            SetStatus(HrmStatus.Scanning);
            _devices.Emit(ScanResults);
            return Task.CompletedTask;
        }

        public Task StopScanAsync()
        {
            StopScanCalls++;
            if (_status.Value == HrmStatus.Scanning)
            {
                SetStatus(HrmStatus.Disconnected);
            }

            return Task.CompletedTask;
        }

        public Task ConnectAsync(string deviceId)
        {
            _connectCalls.Add(deviceId);
            switch (ConnectBehavior)
            {
                case ScriptedConnect.StayConnecting:
                    SetStatus(HrmStatus.Connecting);
                    return Task.CompletedTask;
                case ScriptedConnect.ThrowIncompatible:
                    return Failed(new HrmIncompatibleException());
                case ScriptedConnect.ThrowError:
                    return Failed(new InvalidOperationException(ErrorText));
                case ScriptedConnect.Pending:
                    _pending = new TaskCompletionSource<bool>();
                    return _pending.Task;
                default:
                    IsRetrying = false;
                    SetStatus(HrmStatus.Connected);
                    return Task.CompletedTask;
            }
        }

        public Task AutoConnectAsync()
        {
            AutoConnectCalls++;
            return AutoConnectError == null ? Task.CompletedTask : Failed(AutoConnectError);
        }

        public Task DisconnectAsync()
        {
            DisconnectCalls++;
            IsRetrying = false;
            if (CurrentBpm != 0)
            {
                EmitBpm(0);
            }

            SetStatus(HrmStatus.Disconnected);
            return Task.CompletedTask;
        }

        // Olvida el recordado y corta los reintentos; reavisa el estado (como el simulado).
        public Task ForgetAsync()
        {
            ForgetCalls++;
            RememberedDeviceName = null;
            IsRetrying = false;
            _status.ForceNotify();
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
