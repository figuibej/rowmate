using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;

namespace RowMate.App.Device
{
    // Estado del pulsómetro para su tarjeta (F-HRM-12). Puerto 1 a 1 de HeartRateProvider.
    // - Suscripciones (:23-39): estado (si conectado borra el error; si no es "conectando" descarta el nombre elegido), pulso y
    //   lista. Avisa en cada aviso del servicio, aunque repita el valor (AC-HRM-04-01: cada pulso se muestra).
    // - En cada aviso fotografía IsRetrying, ConnectedDeviceName y RememberedDeviceName del servicio (el legacy los lee en vivo).
    // - _run (:91-101): toda falla de una acción queda en Error y avisa; el error se borra al EMPEZAR una acción y al llegar a
    //   conectado, nunca al pasar a desconectado (AC-HRM-12-03). Después de Dispose no avisa nada (AC-HRM-12-06).
    // Entrega sincrónica (D-04): el primer estado que se avisa después de ConnectAsync ya trae el nombre elegido.
    // legacy: lib/features/device/heart_rate_provider.dart:7-117
    public sealed class HeartRateStore : IDisposable
    {
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private readonly IHeartRateService _hrm;
        private readonly ObservableValue<HeartRateState> _state;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private HrmStatus _status;
        private int _bpm;
        private IReadOnlyList<BleScanResult> _scanResults = NoResults;
        private Exception? _error;
        private string? _chosenName;
        private bool _disposed;

        public HeartRateStore(IHeartRateService hrm, IAppLogger logger)
        {
            _hrm = hrm ?? throw new ArgumentNullException(nameof(hrm));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            // :20-22: arranca con el estado y el pulso del servicio.
            _status = hrm.Status.Value;
            _bpm = hrm.CurrentBpm;
            _state = new ObservableValue<HeartRateState>(Snapshot(), logger);
            State = new ReadOnlyObservable<HeartRateState>(_state);

            _subscriptions.Add(hrm.Status.Subscribe(OnStatus, false));
            _subscriptions.Add(hrm.Bpm.Subscribe(OnBpm));
            _subscriptions.Add(hrm.Devices.Subscribe(OnDevices));
        }

        public IReadOnlyObservable<HeartRateState> State { get; }

        // :56
        public static bool IsIncompatible(Exception error)
        {
            return error is HrmIncompatibleException;
        }

        // :58-63: borra el error y la lista, avisa y busca.
        public async Task StartScanAsync()
        {
            _error = null;
            _scanResults = NoResults;
            Notify();
            await Run(() => _hrm.StartScanAsync());
        }

        // :65-70: borra el error, fija el nombre elegido, avisa y conecta.
        public async Task ConnectAsync(string deviceId, string? name)
        {
            _error = null;
            _chosenName = name;
            Notify();
            await Run(() => _hrm.ConnectAsync(deviceId));
        }

        // :72-78: reintenta con el sensor recordado ("Conectar" de la tarjeta).
        public async Task RetryAsync()
        {
            _error = null;
            _chosenName = null;
            Notify();
            await Run(() => _hrm.AutoConnectAsync());
        }

        // :80-83 (sin aviso previo, como el legacy).
        public async Task DisconnectAsync()
        {
            _error = null;
            await Run(() => _hrm.DisconnectAsync());
        }

        // :85-89: + reaviso al terminar.
        public async Task ForgetAsync()
        {
            _error = null;
            await Run(() => _hrm.ForgetAsync());
            Notify();
        }

        // :109-116
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i].Dispose();
            }

            _subscriptions.Clear();
        }

        // :91-101. Una conexión real puede tardar 15 s: si el store ya se descartó, no avisa (Notify lo controla).
        private async Task Run(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception error)
            {
                _error = error;
                Notify();
            }
        }

        private void OnStatus(HrmStatus status)
        {
            _status = status;
            if (status == HrmStatus.Connected)
            {
                _error = null;
            }

            // El nombre elegido en la lista solo vale para ese intento; los reintentos muestran el recordado (:26-28).
            if (status != HrmStatus.Connecting)
            {
                _chosenName = null;
            }

            Notify();
        }

        private void OnBpm(int bpm)
        {
            _bpm = bpm;
            Notify();
        }

        private void OnDevices(IReadOnlyList<BleScanResult> results)
        {
            _scanResults = results ?? NoResults;
            Notify();
        }

        // = _notifyIfAlive(): un estado nuevo por aviso; si quedó igual, ForceNotify. Muerto, no avisa.
        private void Notify()
        {
            if (_disposed)
            {
                return;
            }

            var before = _state.Value;
            _state.Value = Snapshot();
            if (ReferenceEquals(before, _state.Value))
            {
                _state.ForceNotify();
            }
        }

        private HeartRateState Snapshot()
        {
            return new HeartRateState(_status, _bpm, _scanResults, _error, _chosenName, _hrm.IsRetrying, _hrm.ConnectedDeviceName,
                _hrm.RememberedDeviceName);
        }
    }
}
