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
    // - _run (:91-101): toda falla de una acción queda en el error y avisa; el error se borra al EMPEZAR una acción y al llegar a
    //   conectado, nunca al pasar a desconectado (AC-HRM-12-03). Después de Dispose no avisa nada (AC-HRM-12-06).
    // It. 11 (02-tech-design §3.1):
    // - El error lleva el tipo según la acción (DT11-15): Buscar → Scan; conectar, Conectar, Desconectar y Olvidar → Connect;
    //   HrmIncompatibleException → Incompatible siempre. Más el nombre al empezar (ErrorName) y el Message sin traducir.
    // - Link: el enlace del servicio (fase de reconexión, lectura vencida); reavisa cuando cambia.
    // - Notices (DT11-12): Connected al pasar a conectado con Link.ConnectedBy ∈ {Startup, Drop}; Lost en cada corrida nueva de la
    //   cadencia de caída. Nada después de Dispose.
    // Entrega sincrónica (D-04): el primer estado que se avisa después de ConnectAsync ya trae el nombre elegido.
    // legacy: lib/features/device/heart_rate_provider.dart:7-117
    public sealed class HeartRateStore : IDisposable
    {
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private readonly IHeartRateService _hrm;
        private readonly ObservableValue<HeartRateState> _state;
        private readonly EventStream<HrmNotice> _notices;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private HrmStatus _status;
        private int _bpm;
        private IReadOnlyList<BleScanResult> _scanResults = NoResults;
        private HrmErrorKind _errorKind;
        private string? _errorName;
        private string? _errorDetail;
        private string? _chosenName;
        private HrmLink _link;
        private int _lastLostRun;
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
            _link = hrm.Link.Value ?? HrmLink.Idle;
            _lastLostRun = _link.Reason == HrmRetryReason.Drop ? _link.Run : 0;
            _state = new ObservableValue<HeartRateState>(Snapshot(), logger);
            State = new ReadOnlyObservable<HeartRateState>(_state);
            _notices = new EventStream<HrmNotice>(logger);

            _subscriptions.Add(hrm.Status.Subscribe(OnStatus, false));
            _subscriptions.Add(hrm.Bpm.Subscribe(OnBpm));
            _subscriptions.Add(hrm.Devices.Subscribe(OnDevices));
            _subscriptions.Add(hrm.Link.Subscribe(OnLink, false));
        }

        public IReadOnlyObservable<HeartRateState> State { get; }

        // It. 11 (DT11-12): los avisos breves del pulsómetro (los muestra HrmNoticePresenter).
        public IEventSource<HrmNotice> Notices => _notices;

        // :56
        public static bool IsIncompatible(Exception error)
        {
            return error is HrmIncompatibleException;
        }

        // :58-63: borra el error y la lista, avisa y busca.
        public async Task StartScanAsync()
        {
            ClearError();
            _scanResults = NoResults;
            Notify();
            await Run(HrmErrorKind.Scan, null, () => _hrm.StartScanAsync());
        }

        // :65-70: borra el error, fija el nombre elegido, avisa y conecta.
        public async Task ConnectAsync(string deviceId, string? name)
        {
            ClearError();
            _chosenName = name;
            Notify();
            await Run(HrmErrorKind.Connect, name, () => _hrm.ConnectAsync(deviceId));
        }

        // :72-78: reintenta con el sensor recordado ("Conectar" de la tarjeta).
        public async Task RetryAsync()
        {
            ClearError();
            _chosenName = null;
            Notify();
            await Run(HrmErrorKind.Connect, _hrm.RememberedDeviceName, () => _hrm.AutoConnectAsync());
        }

        // :80-83 (sin aviso previo, como el legacy). m-07: si falla, "No se pudo conectar con {name}".
        public async Task DisconnectAsync()
        {
            ClearError();
            await Run(HrmErrorKind.Connect, _hrm.ConnectedDeviceName ?? _hrm.RememberedDeviceName, () => _hrm.DisconnectAsync());
        }

        // :85-89: + reaviso al terminar.
        public async Task ForgetAsync()
        {
            ClearError();
            await Run(HrmErrorKind.Connect, _hrm.RememberedDeviceName, () => _hrm.ForgetAsync());
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
        private async Task Run(HrmErrorKind kind, string? name, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception error)
            {
                _errorKind = IsIncompatible(error) ? HrmErrorKind.Incompatible : kind;
                _errorName = name;
                _errorDetail = error.Message ?? "";
                Notify();
            }
        }

        private void ClearError()
        {
            _errorKind = HrmErrorKind.None;
            _errorName = null;
            _errorDetail = null;
        }

        private void OnStatus(HrmStatus status)
        {
            var wasConnected = _status == HrmStatus.Connected;
            _status = status;
            _link = _hrm.Link.Value ?? HrmLink.Idle;
            if (status == HrmStatus.Connected)
            {
                ClearError();
            }

            // El nombre elegido en la lista solo vale para ese intento; los reintentos muestran el recordado (:26-28).
            if (status != HrmStatus.Connecting)
            {
                _chosenName = null;
            }

            Notify();

            // AC-HRM-07-05: solo las reconexiones automáticas (al abrir, Conectar o tras una caída) avisan.
            if (!wasConnected && status == HrmStatus.Connected
                && (_link.ConnectedBy == HrmConnectOrigin.Startup || _link.ConnectedBy == HrmConnectOrigin.Drop))
            {
                EmitNotice(new HrmNotice(HrmNoticeKind.Connected, _hrm.ConnectedDeviceName));
            }
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

        private void OnLink(HrmLink link)
        {
            _link = link ?? HrmLink.Idle;
            Notify();

            // AC-HRM-08-03: una vez por corrida de la cadencia de caída.
            if (_link.Reason == HrmRetryReason.Drop && _link.Run != _lastLostRun)
            {
                _lastLostRun = _link.Run;
                EmitNotice(new HrmNotice(HrmNoticeKind.Lost, _hrm.RememberedDeviceName));
            }
        }

        private void EmitNotice(HrmNotice notice)
        {
            if (!_disposed)
            {
                _notices.Emit(notice);
            }
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
            return new HeartRateState(_status, _bpm, _scanResults, _errorKind, _errorName, _errorDetail, _chosenName, _hrm.IsRetrying,
                _hrm.ConnectedDeviceName, _hrm.RememberedDeviceName, _link);
        }
    }
}
