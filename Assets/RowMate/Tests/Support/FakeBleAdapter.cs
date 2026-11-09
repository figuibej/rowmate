using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Ble;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // Cómo responde el connect de un equipo del adaptador falso.
    public enum FakeConnectBehavior
    {
        Respond,
        NeverRespond,
        Fail,
        RespondAfter
    }

    // Una escritura GATT registrada.
    public sealed class FakeBleWrite
    {
        public FakeBleWrite(string deviceId, BleUuid characteristic, byte[] bytes, bool withResponse, double time)
        {
            DeviceId = deviceId;
            Characteristic = characteristic;
            Bytes = bytes;
            WithResponse = withResponse;
            Time = time;
        }

        public string DeviceId { get; }

        public BleUuid Characteristic { get; }

        public byte[] Bytes { get; }

        public bool WithResponse { get; }

        public double Time { get; }
    }

    // El adaptador Bluetooth falso (10a 02-tech-design §8.2): los equipos K-10 (FakeBleDevices), el estado, la búsqueda, el permiso
    // de Android (opcional: PermissionNotAsked / DenyPermission / DenyPermissionForever / AnswerPermission), el connect de cada
    // equipo (Respond, NeverRespond, Fail o RespondAfter), la caída del enlace, las notificaciones y el registro de todo lo que
    // pidió la app. Determinista con el programador de la prueba (FakeScheduler o el FrameScheduler de LifecycleHarness). Nunca
    // toca una radio.
    // It. 11 (02-tech-design §6.2): ConnectRequests (id y si pidió MTU), Steps (connect, discover, notify:<chr>, disconnect, en orden),
    // FailDiscover / FailSetNotify / HoldNextDiscover, el connect que falla con el adaptador sin On y LiveSessions. Como
    // NativeBleAdapter (:243-299, m-04): la cancelación de un connect pendiente actúa por pedido (reqId), no por id, y un connect nuevo
    // al mismo id reemplaza la sesión (cierra el periférico abierto, sin caída, y cancela el connect que seguía esperando).
    public sealed class FakeBleAdapter : IBleAdapter
    {
        private readonly IScheduler _scheduler;
        private readonly ObservableValue<BleAdapterState> _state;
        private readonly EventStream<BleScanResult> _scanResults;
        private readonly EventStream<BleException> _scanFailed;
        private readonly Dictionary<string, FakeBleDevice> _devices = new Dictionary<string, FakeBleDevice>(StringComparer.Ordinal);
        private readonly Dictionary<string, FakeConnectBehavior> _behaviors = new Dictionary<string, FakeConnectBehavior>(StringComparer.Ordinal);
        private readonly Dictionary<string, Exception> _failures = new Dictionary<string, Exception>(StringComparer.Ordinal);
        private readonly Dictionary<string, TimeSpan> _delays = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _connectCalls = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _connectCancels = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, FakeBlePeripheral> _peripherals = new Dictionary<string, FakeBlePeripheral>(StringComparer.Ordinal);
        private readonly List<FakeBlePeripheral> _opened = new List<FakeBlePeripheral>();
        private readonly Dictionary<int, PendingConnect> _pending = new Dictionary<int, PendingConnect>();
        private readonly Dictionary<string, int> _pendingById = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, Exception> _discoverFailures = new Dictionary<string, Exception>(StringComparer.Ordinal);
        private readonly Dictionary<string, Exception> _notifyFailures = new Dictionary<string, Exception>(StringComparer.Ordinal);
        private readonly Dictionary<string, TaskCompletionSource<IReadOnlyList<BleGattService>>> _heldDiscovers =
            new Dictionary<string, TaskCompletionSource<IReadOnlyList<BleGattService>>>(StringComparer.Ordinal);
        private readonly Dictionary<string, TaskCompletionSource<bool>> _heldDisconnects = new Dictionary<string, TaskCompletionSource<bool>>(StringComparer.Ordinal);
        private int _nextReqId;
        private Exception? _failNextScan;
        private bool _permissionModel;
        private BlePermissionState _permission = BlePermissionState.Granted;
        private bool? _answer;
        private BleAdapterState _radio;
        private IReadOnlyList<BleUuid> _scanServices = new BleUuid[0];

        public FakeBleAdapter(IScheduler scheduler, IAppLogger? logger = null, bool filtersScanInOs = false, bool needsNativeKeepAlive = false,
                              BleAdapterState initial = BleAdapterState.On, IEnumerable<FakeBleDevice>? devices = null)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Logger = logger ?? new RecordingLogger();
            FiltersScanInOs = filtersScanInOs;
            NeedsNativeKeepAlive = needsNativeKeepAlive;
            _radio = initial;
            _state = new ObservableValue<BleAdapterState>(initial, Logger);
            State = new ReadOnlyObservable<BleAdapterState>(_state);
            _scanResults = new EventStream<BleScanResult>(Logger);
            _scanFailed = new EventStream<BleException>(Logger);
            foreach (var device in devices ?? FakeBleDevices.K10)
            {
                _devices[device.Id] = device;
            }
        }

        public IAppLogger Logger { get; }

        public IReadOnlyObservable<BleAdapterState> State { get; }

        public bool FiltersScanInOs { get; }

        public bool NeedsNativeKeepAlive { get; }

        public IEventSource<BleScanResult> ScanResults => _scanResults;

        public IEventSource<BleException> ScanFailed => _scanFailed;

        public IScheduler Scheduler => _scheduler;

        public bool Scanning { get; private set; }

        public IReadOnlyList<BleUuid> LastScanServices => _scanServices;

        public int ScanStarts { get; private set; }

        public int ScanStops { get; private set; }

        public int PermissionRequests { get; private set; }

        public int RequestEnableCalls { get; private set; }

        public int AppSettingsOpens { get; private set; }

        public bool Disposed { get; private set; }

        public List<FakeBleWrite> Writes { get; } = new List<FakeBleWrite>();

        public List<(string DeviceId, BleUuid Characteristic, bool Enable)> SetNotifyCalls { get; } = new List<(string, BleUuid, bool)>();

        public int NativeKeepAliveStarts { get; internal set; }

        public int NativeKeepAliveStops { get; internal set; }

        public int NativeKeepAliveWrites { get; internal set; }

        // Hace fallar una escritura (devuelve la excepción) o la deja pasar (null).
        public Func<FakeBleWrite, Exception?>? WriteFailure { get; set; }

        // ── Estado y permiso ──

        public void SetState(BleAdapterState state)
        {
            _radio = state;
            Recompute();
        }

        public BlePermissionState Permission => _permission;

        // El permiso de Android, todavía sin pedir (la primera instalación).
        public void PermissionNotAsked()
        {
            _permissionModel = true;
            _permission = BlePermissionState.NotAsked;
            Recompute();
        }

        public void DenyPermission()
        {
            _permissionModel = true;
            _permission = BlePermissionState.Denied;
            Recompute();
        }

        public void DenyPermissionForever()
        {
            _permissionModel = true;
            _permission = BlePermissionState.DeniedForever;
            Recompute();
        }

        public void GrantPermission()
        {
            _permissionModel = true;
            _permission = BlePermissionState.Granted;
            Recompute();
        }

        // La respuesta del próximo diálogo (sin respuesta guionada, concede).
        public void AnswerPermission(bool granted)
        {
            _answer = granted;
        }

        public Task<bool> EnsureScanPermissionAsync()
        {
            if (!_permissionModel || _permission == BlePermissionState.Granted)
            {
                return Task.FromResult(true);
            }

            if (_permission == BlePermissionState.DeniedForever)
            {
                return Task.FromResult(false);
            }

            PermissionRequests++;
            var granted = _answer ?? true;
            _answer = null;
            _permission = granted ? BlePermissionState.Granted : BlePermissionState.Denied;
            Recompute();
            return Task.FromResult(granted);
        }

        public Task RequestEnableAsync()
        {
            RequestEnableCalls++;
            if (_permissionModel && _permission == BlePermissionState.DeniedForever)
            {
                AppSettingsOpens++;
            }

            return Task.CompletedTask;
        }

        // ── Búsqueda ──

        public void FailNextScan(Exception error)
        {
            _failNextScan = error;
        }

        public Task StartScanAsync(IReadOnlyList<BleUuid> services)
        {
            ScanStarts++;
            _scanServices = services;
            var failure = _failNextScan;
            if (failure != null)
            {
                _failNextScan = null;
                return Task.FromException(failure);
            }

            Scanning = true;
            return Task.CompletedTask;
        }

        public Task StopScanAsync()
        {
            ScanStops++;
            Scanning = false;
            return Task.CompletedTask;
        }

        // Un anuncio ahora (solo lo ve el escaneo en curso; con el filtro del sistema, solo si anuncia un servicio pedido).
        public void Advertise(FakeBleDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            _devices[device.Id] = device;
            if (!Scanning)
            {
                return;
            }

            if (FiltersScanInOs && !AdvertisesAny(device))
            {
                return;
            }

            _scanResults.Emit(device.ScanResult);
        }

        public IDisposable AdvertiseAt(TimeSpan delay, FakeBleDevice device)
        {
            return _scheduler.Schedule(delay, () => Advertise(device));
        }

        // El fallo asíncrono del escaneo (onScanFailed).
        public void EmitScanFailure(BleException error)
        {
            Scanning = false;
            _scanFailed.Emit(error);
        }

        // ── Conexión ──

        public void Respond(string id)
        {
            _behaviors[id] = FakeConnectBehavior.Respond;
        }

        public void NeverRespond(string id)
        {
            _behaviors[id] = FakeConnectBehavior.NeverRespond;
        }

        public void Fail(string id, Exception error)
        {
            _behaviors[id] = FakeConnectBehavior.Fail;
            _failures[id] = error;
        }

        public void RespondAfter(string id, TimeSpan delay)
        {
            _behaviors[id] = FakeConnectBehavior.RespondAfter;
            _delays[id] = delay;
        }

        public int ConnectCalls(string id)
        {
            return _connectCalls.TryGetValue(id, out var n) ? n : 0;
        }

        public int ConnectCancels(string id)
        {
            return _connectCancels.TryGetValue(id, out var n) ? n : 0;
        }

        public int TotalConnectCalls
        {
            get
            {
                var total = 0;
                foreach (var entry in _connectCalls)
                {
                    total += entry.Value;
                }

                return total;
            }
        }

        // El periférico vigente del equipo (o null).
        public FakeBlePeripheral? Peripheral(string id)
        {
            return _peripherals.TryGetValue(id, out var p) && !p.Closed ? p : null;
        }

        // It. 11: cada connect pedido, en orden (id y si pidió MTU alto).
        public List<(string DeviceId, bool RequestHighMtu)> ConnectRequests { get; } = new List<(string, bool)>();

        // It. 11: los pasos GATT en orden ("connect", "discover", "notify:2a37", "disconnect"), con el id.
        public List<(string DeviceId, string Step)> Steps { get; } = new List<(string, string)>();

        // Los pasos de un equipo, en orden.
        public IReadOnlyList<string> StepsOf(string id)
        {
            var steps = new List<string>();
            foreach (var step in Steps)
            {
                if (step.DeviceId == id)
                {
                    steps.Add(step.Step);
                }
            }

            return steps;
        }

        // Connects que reemplazó otro connect al mismo id (no cuentan en ConnectCancels).
        public int ReplacedConnects { get; private set; }

        // El descubrimiento de servicios del equipo falla (hasta ClearStepFailures).
        public void FailDiscover(string id, Exception error)
        {
            _discoverFailures[id] = error;
        }

        // Activar las notificaciones del equipo falla (hasta ClearStepFailures).
        public void FailSetNotify(string id, Exception error)
        {
            _notifyFailures[id] = error;
        }

        public void ClearStepFailures(string id)
        {
            _discoverFailures.Remove(id);
            _notifyFailures.Remove(id);
        }

        // El próximo descubrimiento del equipo queda pendiente hasta que se llame a la acción devuelta (responde con su GATT).
        public Action HoldNextDiscover(string id)
        {
            var held = new TaskCompletionSource<IReadOnlyList<BleGattService>>();
            _heldDiscovers[id] = held;
            return () =>
            {
                if (_heldDiscovers.TryGetValue(id, out var current) && ReferenceEquals(current, held))
                {
                    _heldDiscovers.Remove(id);
                }

                held.TrySetResult(_devices[id].Services);
            };
        }

        // La próxima desconexión pedida del equipo queda pendiente (el periférico ya se cerró) hasta que se llame a la acción devuelta.
        public Action HoldNextDisconnect(string id)
        {
            var held = new TaskCompletionSource<bool>();
            _heldDisconnects[id] = held;
            return () =>
            {
                if (_heldDisconnects.TryGetValue(id, out var current) && ReferenceEquals(current, held))
                {
                    _heldDisconnects.Remove(id);
                }

                held.TrySetResult(true);
            };
        }

        // Sesiones vivas del equipo: periféricos abiertos más connects pendientes (R-03: nunca más de una por id).
        public int LiveSessions(string id)
        {
            var live = 0;
            foreach (var peripheral in _opened)
            {
                if (peripheral.DeviceId == id && !peripheral.Closed)
                {
                    live++;
                }
            }

            foreach (var pending in _pending.Values)
            {
                if (pending.DeviceId == id)
                {
                    live++;
                }
            }

            return live;
        }

        public Task<IBlePeripheral> ConnectAsync(string deviceId, bool requestHighMtu, CancellationToken ct)
        {
            _connectCalls[deviceId] = ConnectCalls(deviceId) + 1;
            ConnectRequests.Add((deviceId, requestHighMtu));
            Steps.Add((deviceId, "connect"));
            if (_state.Value != BleAdapterState.On)
            {
                return Task.FromException<IBlePeripheral>(new BleException("connect", "adapter off"));
            }

            if (!_devices.TryGetValue(deviceId, out var device))
            {
                return Task.FromException<IBlePeripheral>(new BleException("connect", "device not found"));
            }

            ReplaceSession(deviceId);
            _behaviors.TryGetValue(deviceId, out var behavior);
            switch (behavior)
            {
                case FakeConnectBehavior.Fail:
                    return Task.FromException<IBlePeripheral>(_failures[deviceId]);
                case FakeConnectBehavior.Respond:
                    return Task.FromResult<IBlePeripheral>(Open(device));
            }

            var reqId = ++_nextReqId;
            var pending = new PendingConnect(deviceId, new TaskCompletionSource<IBlePeripheral>());
            _pending[reqId] = pending;
            _pendingById[deviceId] = reqId;
            if (behavior == FakeConnectBehavior.RespondAfter)
            {
                pending.Timer = _scheduler.Schedule(_delays[deviceId], () =>
                {
                    if (_pending.Remove(reqId))
                    {
                        ForgetPendingId(deviceId, reqId);
                        pending.Source.TrySetResult(Open(device));
                    }
                });
            }

            if (ct.CanBeCanceled)
            {
                ct.Register(() => CancelRequest(reqId));
            }

            return pending.Source.Task;
        }

        // La caída no pedida del enlace (sale del rango, se apaga el remo).
        public void DropLink(string id, int code = 8)
        {
            Peripheral(id)?.Drop(new BleException("connection", "link lost (code " + code + ")", code));
        }

        public void Notify(string id, BleUuid characteristic, byte[] bytes)
        {
            Peripheral(id)?.Emit(characteristic, bytes);
        }

        // Una notificación cada período (la k-ésima con bytes(k), k = 1, 2, …). Disponer el resultado la corta.
        public IDisposable NotifyEvery(string id, TimeSpan period, Func<int, byte[]> bytes, BleUuid? characteristic = null)
        {
            var k = 0;
            var chr = characteristic ?? FtmsUuids.RowerData;
            return _scheduler.SchedulePeriodic(period, () => Notify(id, chr, bytes(++k)));
        }

        public void Dispose()
        {
            Disposed = true;
        }

        internal Exception? RecordWrite(FakeBleWrite write)
        {
            Writes.Add(write);
            return WriteFailure?.Invoke(write);
        }

        internal Exception? DiscoverFailure(string id)
        {
            return _discoverFailures.TryGetValue(id, out var error) ? error : null;
        }

        internal Exception? NotifyFailure(string id)
        {
            return _notifyFailures.TryGetValue(id, out var error) ? error : null;
        }

        // Una sola vez: el descubrimiento siguiente responde en el acto.
        internal Task<IReadOnlyList<BleGattService>>? TakeHeldDiscover(string id)
        {
            if (!_heldDiscovers.TryGetValue(id, out var held))
            {
                return null;
            }

            _heldDiscovers.Remove(id);
            return held.Task;
        }

        internal Task? TakeHeldDisconnect(string id)
        {
            if (!_heldDisconnects.TryGetValue(id, out var held))
            {
                return null;
            }

            _heldDisconnects.Remove(id);
            return held.Task;
        }

        private FakeBlePeripheral Open(FakeBleDevice device)
        {
            var peripheral = new FakeBlePeripheral(this, device, _scheduler, Logger);
            _peripherals[device.Id] = peripheral;
            _opened.Add(peripheral);
            return peripheral;
        }

        // Como NativeBleAdapter (:243-260): un connect sobre un id con sesión la reemplaza.
        private void ReplaceSession(string id)
        {
            if (_peripherals.TryGetValue(id, out var open) && !open.Closed)
            {
                open.Dispose();
            }

            if (_pendingById.TryGetValue(id, out var reqId) && _pending.TryGetValue(reqId, out var previous))
            {
                _pending.Remove(reqId);
                _pendingById.Remove(id);
                previous.Timer?.Dispose();
                ReplacedConnects++;
                previous.Source.TrySetCanceled();
            }
        }

        // DT10-17 por pedido (m-04): solo cancela ESE connect si sigue pendiente.
        private void CancelRequest(int reqId)
        {
            if (!_pending.TryGetValue(reqId, out var pending))
            {
                return;
            }

            _pending.Remove(reqId);
            ForgetPendingId(pending.DeviceId, reqId);
            pending.Timer?.Dispose();
            _connectCancels[pending.DeviceId] = ConnectCancels(pending.DeviceId) + 1;
            pending.Source.TrySetCanceled();
        }

        private void ForgetPendingId(string id, int reqId)
        {
            if (_pendingById.TryGetValue(id, out var current) && current == reqId)
            {
                _pendingById.Remove(id);
            }
        }

        private sealed class PendingConnect
        {
            public PendingConnect(string deviceId, TaskCompletionSource<IBlePeripheral> source)
            {
                DeviceId = deviceId;
                Source = source;
            }

            public string DeviceId { get; }

            public TaskCompletionSource<IBlePeripheral> Source { get; }

            public IDisposable? Timer { get; set; }
        }

        private bool AdvertisesAny(FakeBleDevice device)
        {
            for (var i = 0; i < _scanServices.Count; i++)
            {
                for (var j = 0; j < device.Advertised.Count; j++)
                {
                    if (device.Advertised[j] == _scanServices[i])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Recompute()
        {
            var denied = _permissionModel && (_permission == BlePermissionState.Denied || _permission == BlePermissionState.DeniedForever);
            _state.Value = denied ? BleAdapterState.Unauthorized : _radio;
        }
    }

    // El periférico del adaptador falso: el GATT del equipo, las escrituras (al registro del adaptador, con la hora del
    // programador), las notificaciones con esa hora y el keepalive nativo (un periódico en el mismo programador: en el ciclo de
    // vida de prueba sus disparos llegan en el alcance, en su instante).
    public sealed class FakeBlePeripheral : IBlePeripheral
    {
        private readonly FakeBleAdapter _adapter;
        private readonly FakeBleDevice _device;
        private readonly IScheduler _scheduler;
        private readonly EventStream<BleException> _linkLost;
        private readonly EventStream<BleNotification> _notifications;
        private IDisposable? _keepAlive;

        internal FakeBlePeripheral(FakeBleAdapter adapter, FakeBleDevice device, IScheduler scheduler, IAppLogger logger)
        {
            _adapter = adapter;
            _device = device;
            _scheduler = scheduler;
            _linkLost = new EventStream<BleException>(logger);
            _notifications = new EventStream<BleNotification>(logger);
        }

        public string DeviceId => _device.Id;

        public string? Name => _device.Name;

        public IEventSource<BleException> LinkLost => _linkLost;

        public IEventSource<BleNotification> Notifications => _notifications;

        public bool Closed { get; private set; }

        public int DisconnectCalls { get; private set; }

        public bool NativeKeepAliveRunning => _keepAlive != null;

        public Task<IReadOnlyList<BleGattService>> DiscoverServicesAsync()
        {
            _adapter.Steps.Add((DeviceId, "discover"));
            if (Closed)
            {
                return Task.FromException<IReadOnlyList<BleGattService>>(new BleException("discover", "not connected"));
            }

            var failure = _adapter.DiscoverFailure(DeviceId);
            if (failure != null)
            {
                return Task.FromException<IReadOnlyList<BleGattService>>(failure);
            }

            return _adapter.TakeHeldDiscover(DeviceId) ?? Task.FromResult(_device.Services);
        }

        public Task SetNotifyAsync(BleUuid service, BleUuid characteristic, bool enable)
        {
            _adapter.SetNotifyCalls.Add((DeviceId, characteristic, enable));
            _adapter.Steps.Add((DeviceId, "notify:" + characteristic.ToShortString()));
            if (Closed)
            {
                return Task.FromException(new BleException("setNotify", "not connected"));
            }

            var failure = _adapter.NotifyFailure(DeviceId);
            return failure != null ? Task.FromException(failure) : Task.CompletedTask;
        }

        public Task WriteAsync(BleUuid service, BleUuid characteristic, byte[] data, bool withResponse)
        {
            if (Closed)
            {
                return Task.FromException(new BleException("write", "not connected"));
            }

            var failure = _adapter.RecordWrite(new FakeBleWrite(DeviceId, characteristic, (byte[])data.Clone(), withResponse, _scheduler.MonotonicSeconds));
            return failure == null ? Task.CompletedTask : Task.FromException(failure);
        }

        public Task DisconnectAsync()
        {
            DisconnectCalls++;
            _adapter.Steps.Add((DeviceId, "disconnect"));
            Close();
            return _adapter.TakeHeldDisconnect(DeviceId) ?? Task.CompletedTask;
        }

        public void StartNativeKeepAlive(BleUuid service, BleUuid characteristic, byte[] data, TimeSpan period)
        {
            _adapter.NativeKeepAliveStarts++;
            _keepAlive?.Dispose();
            _keepAlive = _scheduler.SchedulePeriodic(period, () => _adapter.NativeKeepAliveWrites++);
        }

        public void StopNativeKeepAlive()
        {
            _adapter.NativeKeepAliveStops++;
            _keepAlive?.Dispose();
            _keepAlive = null;
        }

        public void Dispose()
        {
            Close();
        }

        internal void Drop(BleException error)
        {
            if (Closed)
            {
                return;
            }

            Close();
            _linkLost.Emit(error);
        }

        internal void Emit(BleUuid characteristic, byte[] bytes)
        {
            if (!Closed)
            {
                _notifications.Emit(new BleNotification(characteristic, bytes, _scheduler.MonotonicSeconds));
            }
        }

        private void Close()
        {
            Closed = true;
            _keepAlive?.Dispose();
            _keepAlive = null;
        }
    }
}
