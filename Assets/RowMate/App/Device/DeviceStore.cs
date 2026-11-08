using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;

namespace RowMate.App.Device
{
    // Estado del remo para Dispositivo (F-BLE-07, F-BLE-11; It. 10a F-BLE-08, -09, -12). Puerto de DeviceProvider; el vigilante de
    // 5 s (:69-86) vive en RowerService (DT10-01).
    // - Avisa en cada aviso del servicio, aunque el estado quede igual (el legacy llama a notifyListeners en cada evento:
    //   una lectura por segundo con el simulador, aunque repita los valores, AC-BLE-11-09).
    // - Al pasar a conectado reemplaza la lectura por RowingData.Empty (_resetSession, :38-42, :64-67): la grilla muestra
    //   ceros hasta la primera lectura de esa conexión; y borra la hora del último paquete del diagnóstico (el hex y el contador
    //   quedan).
    // - Las acciones capturan la excepción del servicio y la dejan en Error, con su ErrorKind (:104-127). Después de Dispose no
    //   avisa nada.
    // - It. 10a (02-tech-design §6.2): el diagnóstico (:56-61, :98-102), el enlace (RowerLink), la caja del enlace (Reconnecting o
    //   MonitorStopped con RetryDeviceId, DT10-06; se borra al reintentar, buscar o conectar, DV-98), los avisos LinkNotices para el
    //   HUD (Reconnecting, Reconnected solo si hubo reconexión, MonitorStopped, Cleared) y la vibración al quedar "Listo" por una
    //   conexión que eligió el usuario (A10-06).
    // legacy: lib/features/device/device_provider.dart:10-141
    public sealed class DeviceStore : IDisposable
    {
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private readonly IRowerService _rower;
        private readonly IClock? _clock;
        private readonly IHaptics? _haptics;
        private readonly ObservableValue<DeviceState> _state;
        private readonly EventStream<LinkNotice> _linkNotices;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private RowerLink _lastLink;
        private bool _noticeShown;
        private bool _disposed;

        public DeviceStore(IRowerService rower, IAppLogger logger)
            : this(rower, logger, null, null)
        {
        }

        // clock: la hora local del último paquete del diagnóstico (el programador de la app); null = sin hora (pruebas viejas).
        // haptics: la vibración de "Listo" (null = ninguna).
        public DeviceStore(IRowerService rower, IAppLogger logger, IClock? clock, IHaptics? haptics)
        {
            _rower = rower ?? throw new ArgumentNullException(nameof(rower));
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _clock = clock;
            _haptics = haptics;
            _linkNotices = new EventStream<LinkNotice>(logger);
            _lastLink = rower.Link.Value;
            // El legacy arranca en disconnected / unknown, que es lo que valen los servicios de la It. 2 al crearse.
            _state = new ObservableValue<DeviceState>(
                new DeviceState(rower.Status.Value, rower.AdapterState.Value, RowingData.Empty, NoResults, null, rower.ConnectedDeviceName)
                {
                    Link = _lastLink,
                },
                logger);
            State = new ReadOnlyObservable<DeviceState>(_state);

            // Mismo orden de suscripción que el legacy (:34-61); el enlace, al final.
            _subscriptions.Add(rower.AdapterState.Subscribe(OnAdapterState, false));
            _subscriptions.Add(rower.Status.Subscribe(OnStatus, false));
            _subscriptions.Add(rower.Devices.Subscribe(OnDevices));
            _subscriptions.Add(rower.Data.Subscribe(OnData));
            _subscriptions.Add(rower.RawBytes.Subscribe(OnRawBytes));
            _subscriptions.Add(rower.Link.Subscribe(OnLink, false));
        }

        public IReadOnlyObservable<DeviceState> State { get; }

        // Los avisos del enlace para el HUD (02-tech-design §6.3).
        public IEventSource<LinkNotice> LinkNotices => _linkNotices;

        // :104-114: borra el error y la lista (y el aviso de monitor apagado, DV-98), avisa y busca; una falla queda en Error.
        public async Task StartScanAsync()
        {
            if (_disposed)
            {
                return;
            }

            Publish(ClearMonitorStopped(_state.Value with { Error = null, ErrorKind = DeviceErrorKind.None, ScanResults = NoResults }));
            try
            {
                await _rower.StartScanAsync();
            }
            catch (Exception error)
            {
                Publish(_state.Value with { Error = error, ErrorKind = DeviceErrorKind.Scan });
            }
        }

        // :116
        public Task StopScanAsync()
        {
            return _rower.StopScanAsync();
        }

        // :118-127: borra el error (y el aviso de monitor apagado), avisa y conecta; una falla queda en Error.
        public async Task ConnectAsync(string deviceId)
        {
            if (_disposed)
            {
                return;
            }

            Publish(ClearMonitorStopped(_state.Value with { Error = null, ErrorKind = DeviceErrorKind.None }));
            try
            {
                await _rower.ConnectAsync(deviceId);
            }
            catch (Exception error)
            {
                Publish(_state.Value with { Error = error, ErrorKind = DeviceErrorKind.Connect });
            }
        }

        // AC-BLE-09-02: "Reintentar" del aviso de monitor apagado: vuelve a conectar el mismo remo.
        public Task RetryAsync()
        {
            var id = _state.Value.RetryDeviceId;
            return id == null || _disposed ? Task.CompletedTask : ConnectAsync(id);
        }

        // :129, sin confirmación (Q-33, AC-BLE-07-02). También corta los reintentos (AC-BLE-08-05).
        public Task DisconnectAsync()
        {
            return _rower.DisconnectAsync();
        }

        // AC-BLE-04-04: "Activar Bluetooth".
        public Task RequestEnableAsync()
        {
            return _disposed ? Task.CompletedTask : _rower.RequestEnableAsync();
        }

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

        private void OnAdapterState(BleAdapterState adapterState)
        {
            Publish(_state.Value with { AdapterState = adapterState });
        }

        private void OnStatus(BleStatus status)
        {
            var current = _state.Value;
            var connected = status == BleStatus.Connected;
            Publish(current with
            {
                Status = status,
                Data = connected ? RowingData.Empty : current.Data,
                Diagnostics = connected && current.Diagnostics.LastReceived.HasValue
                    ? current.Diagnostics with { LastReceived = null }
                    : current.Diagnostics,
            });
        }

        private void OnDevices(IReadOnlyList<BleScanResult> results)
        {
            Publish(_state.Value with { ScanResults = results ?? NoResults });
        }

        private void OnData(RowingData data)
        {
            Publish(_state.Value with { Data = data });
        }

        // :56-61: el último paquete, el contador de toda la vida de la app y la hora local.
        private void OnRawBytes(byte[] bytes)
        {
            var current = _state.Value.Diagnostics;
            var next = new BleDiagnostics(current.Count + 1, BleHex.ForDiagnostics(bytes), _clock?.LocalNow);
            Publish(_state.Value with { Diagnostics = next });
        }

        private void OnLink(RowerLink link)
        {
            var previous = _lastLink;
            _lastLink = link;
            var next = _state.Value with { Link = link };
            var name = link.DeviceName;

            if (link.IsReconnecting)
            {
                next = next with { LinkBanner = LinkBannerKind.Reconnecting, RetryDeviceId = null };
                if (!_noticeShown)
                {
                    _noticeShown = true;
                    Publish(next);
                    _linkNotices.Emit(new LinkNotice(LinkNoticeKind.Reconnecting, name));
                    return;
                }

                Publish(next);
                return;
            }

            if (previous.IsReconnecting && next.LinkBanner == LinkBannerKind.Reconnecting)
            {
                next = next with { LinkBanner = LinkBannerKind.None };
            }

            if (link.LastDisconnect == DisconnectReason.Watchdog && previous.LastDisconnect != DisconnectReason.Watchdog)
            {
                // AC-BLE-09-02, DT10-06: el servicio olvida el remo (bs:308); el aviso lo recuerda para "Reintentar".
                next = next with { LinkBanner = LinkBannerKind.MonitorStopped, RetryDeviceId = link.DeviceId };
                _noticeShown = false;
                Publish(next);
                _linkNotices.Emit(new LinkNotice(LinkNoticeKind.MonitorStopped, name));
                return;
            }

            var reconnected = previous.Reconnect == ReconnectPhase.Attempting && link.Phase == ConnectPhase.Ready;
            var userReady = link.Phase == ConnectPhase.Ready && previous.Reconnect == ReconnectPhase.None
                                                               && (previous.Phase == ConnectPhase.Connecting || previous.Phase == ConnectPhase.TakingControl);
            Publish(next);
            if (_noticeShown)
            {
                _noticeShown = false;
                _linkNotices.Emit(new LinkNotice(reconnected ? LinkNoticeKind.Reconnected : LinkNoticeKind.Cleared, name));
            }

            if (userReady)
            {
                // A10-06, AC-BLE-06-07: una vez, al quedar "Listo" por una conexión que eligió el usuario (no en la reconexión).
                _haptics?.Confirm();
            }
        }

        private static DeviceState ClearMonitorStopped(DeviceState state)
        {
            return state.LinkBanner == LinkBannerKind.MonitorStopped ? state with { LinkBanner = LinkBannerKind.None, RetryDeviceId = null } : state;
        }

        // Fotografía el nombre del servicio y avisa siempre (si el estado quedó igual, ForceNotify). Muerto, no avisa.
        private void Publish(DeviceState next)
        {
            if (_disposed)
            {
                return;
            }

            var before = _state.Value;
            _state.Value = next with { ConnectedDeviceName = _rower.ConnectedDeviceName };
            if (ReferenceEquals(before, _state.Value))
            {
                _state.ForceNotify();
            }
        }
    }
}
