using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Core.Text;

namespace RowMate.Core.Ble.Native
{
    // IBleAdapter sobre las funciones planas del plugin (INativeBleApi) y su búfer de eventos (BleWire v1), igual para Windows y
    // Android (10a 02-tech-design §2.6, §5.2). Es también la bomba (IBleEventPump): AppRoot llama Drain en cada cuadro, antes del
    // Tick, y al volver del segundo plano, antes del alcance. Ningún hilo nativo entra a C# (ADR 009, modelo pull). Hilo principal.
    // - Pedidos: cada operación GATT devuelve un reqId y termina con un RequestDone; acá se resuelve su TaskCompletionSource.
    // - Sesiones por id (DT10-17, QA-G10a-03): un connect cancelado (venció el plazo de Core) cierra la sesión nativa; un connect
    //   bien tardío de un pedido abandonado también; una caída no pedida cierra la sesión ANTES de avisar LinkLost; Dispose y
    //   DisconnectAsync del periférico la cierran si sigue abierta. Como mucho una sesión viva por id.
    // - Reloj (BleEventClock, QA-G10a-04): en primer plano se calibra en cada drenaje y los eventos se despachan en el acto con su
    //   hora en el reloj de la app; al volver, el desfase queda congelado y cada evento se agenda en su instante virtual, así el
    //   alcance los intercala con el keepalive y el watchdog (ADR 017, AC-BLE-91-01). Woke del sistema → Reset.
    // - Permiso de Android (DT10-15, DT10-16, QA-G10a-02): Unauthorized solo después de una negativa; NotAsked muestra la búsqueda.
    // - Versión: rm_ble_api_version distinta de BleWire.ApiVersion, o Init fallido → Unsupported y registro.
    public sealed class NativeBleAdapter : IBleAdapter, IBleEventPump
    {
        public const string Tag = "BLE";

        private readonly INativeBleApi _api;
        private readonly IBlePermissions? _permissions;
        private readonly IScheduler _scheduler;
        private readonly IAppLogger _logger;
        private readonly ObservableValue<BleAdapterState> _state;
        private readonly EventStream<BleScanResult> _scanResults;
        private readonly EventStream<BleException> _scanFailed;
        private readonly BleEventClock _clock = new BleEventClock();
        private readonly byte[] _buffer = new byte[BleWire.MaxRecordBytes];
        private readonly Dictionary<int, Pending> _pending = new Dictionary<int, Pending>();
        private readonly Dictionary<int, string> _abandoned = new Dictionary<int, string>();
        private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _names = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Sink _sink;
        private readonly IDisposable _wokeSubscription;
        private readonly bool _usable;
        private BleAdapterState _nativeState = BleAdapterState.Unknown;
        private bool _scanning;
        private bool _resuming;
        private bool _disposed;

        public NativeBleAdapter(INativeBleApi api, IBlePermissions? permissions, IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _permissions = permissions;
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            if (lifecycle == null)
            {
                throw new ArgumentNullException(nameof(lifecycle));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _state = new ObservableValue<BleAdapterState>(BleAdapterState.Unknown, logger);
            State = new ReadOnlyObservable<BleAdapterState>(_state);
            _scanResults = new EventStream<BleScanResult>(logger);
            _scanFailed = new EventStream<BleException>(logger);
            _sink = new Sink(this);
            var init = _api.Init();
            var version = init == 0 ? _api.ApiVersion : 0;
            if (init != 0 || version != BleWire.ApiVersion)
            {
                _logger.Log(LogLevel.Error, Tag, "Plugin nativo no disponible (init " + Fmt.Integer(init) + ", versión " + Fmt.Integer(version)
                                                 + ", se esperaba " + Fmt.Integer(BleWire.ApiVersion) + ")");
            }
            else
            {
                _usable = true;
                _nativeState = ToState(_api.AdapterState());
            }

            Recompute();
            _wokeSubscription = lifecycle.Woke.Subscribe(_ => _clock.Reset());
        }

        public IReadOnlyObservable<BleAdapterState> State { get; }

        public bool FiltersScanInOs => _api.FiltersScanInOs;

        public bool NeedsNativeKeepAlive => _api.NeedsNativeKeepAlive;

        public IEventSource<BleScanResult> ScanResults => _scanResults;

        public IEventSource<BleException> ScanFailed => _scanFailed;

        // Pedidos esperando su RequestDone (pruebas y diagnóstico).
        public int PendingRequests => _pending.Count;

        public bool IsClockCalibrated => _clock.IsCalibrated;

        // El desfase vigente app − nativo (pruebas del alcance).
        internal double ClockOffset => _clock.Offset;

        // ── Permiso (§2.6) ──

        public async Task<bool> EnsureScanPermissionAsync()
        {
            var permissions = _permissions;
            if (permissions == null)
            {
                return true;
            }

            switch (permissions.State)
            {
                case BlePermissionState.Granted:
                    return true;
                case BlePermissionState.DeniedForever:
                    Recompute();
                    return false;
                default:
                    var result = await permissions.RequestAsync();
                    Recompute();
                    return result == BlePermissionState.Granted;
            }
        }

        // AC-BLE-04-04 (Q-32): sin permiso, primero el permiso (o los ajustes de la app si el sistema ya no muestra el diálogo);
        // con permiso y el adaptador sin encender, el pedido del sistema (Android) o los ajustes de Bluetooth (Windows). Un código
        // de error (SecurityException en Kotlin) abre los ajustes de la app: nunca es un error para el usuario.
        public async Task RequestEnableAsync()
        {
            if (!_usable || _disposed)
            {
                return;
            }

            var permissions = _permissions;
            if (permissions != null && permissions.State != BlePermissionState.Granted)
            {
                if (permissions.State == BlePermissionState.DeniedForever)
                {
                    permissions.OpenAppSettings();
                    return;
                }

                var result = await permissions.RequestAsync();
                Recompute();
                if (result == BlePermissionState.DeniedForever)
                {
                    permissions.OpenAppSettings();
                    return;
                }

                if (result != BlePermissionState.Granted)
                {
                    return;
                }
            }

            if (_nativeState == BleAdapterState.On)
            {
                return;
            }

            var code = _api.RequestEnable();
            if (code != 0)
            {
                _logger.Log(LogLevel.Warning, Tag, "Activar Bluetooth: código " + Fmt.Integer(code));
                permissions?.OpenAppSettings();
            }
        }

        // ── Búsqueda ──

        public Task StartScanAsync(IReadOnlyList<BleUuid> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (!_usable || _disposed)
            {
                return Task.FromException(new BleException("scan", "Bluetooth LE unavailable", BleWire.ErrorUnsupported));
            }

            var csv = new StringBuilder();
            for (var i = 0; i < services.Count; i++)
            {
                if (i > 0)
                {
                    csv.Append(',');
                }

                csv.Append(services[i].ToString());
            }

            var code = _api.StartScan(csv.ToString());
            if (code == BleWire.ErrorPermission)
            {
                _permissions?.Refresh();
                Recompute();
                return Task.FromException(new BlePermissionDeniedException("scan"));
            }

            if (code != 0)
            {
                return Task.FromException(new BleException("scan", "error " + Fmt.Integer(code), code));
            }

            _scanning = true;
            return Task.CompletedTask;
        }

        public Task StopScanAsync()
        {
            if (!_usable || _disposed)
            {
                return Task.CompletedTask;
            }

            _scanning = false;
            _api.StopScan();
            return Task.CompletedTask;
        }

        // ── Conexión y sesiones (DT10-17) ──

        public Task<IBlePeripheral> ConnectAsync(string deviceId, bool requestHighMtu, CancellationToken ct)
        {
            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (!_usable || _disposed)
            {
                return Task.FromException<IBlePeripheral>(new BleException("connect", "Bluetooth LE unavailable", BleWire.ErrorUnsupported));
            }

            if (ct.IsCancellationRequested)
            {
                return Task.FromCanceled<IBlePeripheral>(ct);
            }

            var session = SessionFor(deviceId);
            session.Generation++;
            // Un connect sobre un id con sesión la reemplaza: el plugin la cierra primero; el periférico viejo queda cerrado y un
            // connect anterior que seguía esperando termina cancelado.
            var old = session.Peripheral;
            session.Peripheral = null;
            if (old != null)
            {
                old.MarkClosed();
                FailPendingFor(old, new BleException("connection", "replaced"));
            }

            if (session.ConnectReqId.HasValue && _pending.TryGetValue(session.ConnectReqId.Value, out var previous))
            {
                _pending.Remove(session.ConnectReqId.Value);
                _abandoned[session.ConnectReqId.Value] = deviceId;
                previous.Cancel();
            }

            var reqId = _api.Connect(deviceId, requestHighMtu);
            if (reqId <= 0)
            {
                session.ConnectReqId = null;
                session.Open = false;
                return Task.FromException<IBlePeripheral>(new BleException("connect", "error " + Fmt.Integer(reqId), reqId));
            }

            session.Open = true;
            session.ConnectReqId = reqId;
            _names.TryGetValue(deviceId, out var name);
            var pending = new ConnectPending(deviceId, session.Generation, name);
            _pending[reqId] = pending;
            if (ct.CanBeCanceled)
            {
                pending.Registration = ct.Register(() => CancelConnect(reqId));
            }

            return pending.Source.Task;
        }

        // El plazo de Core venció (DT10-17): se cierra la sesión del id y el connect termina cancelado.
        private void CancelConnect(int reqId)
        {
            if (!_pending.TryGetValue(reqId, out var pending) || !(pending is ConnectPending connect))
            {
                return;
            }

            _pending.Remove(reqId);
            _abandoned[reqId] = connect.DeviceId;
            if (_sessions.TryGetValue(connect.DeviceId, out var session) && session.ConnectReqId == reqId)
            {
                session.Generation++;
                session.ConnectReqId = null;
                if (session.Open)
                {
                    session.Open = false;
                    _api.Disconnect(connect.DeviceId);
                }
            }

            _logger.Log(LogLevel.Info, Tag, "Conexión cancelada: " + BleIds.Mask(connect.DeviceId));
            connect.Cancel();
        }

        internal void Close(NativePeripheral peripheral)
        {
            if (peripheral.IsClosed)
            {
                return;
            }

            peripheral.MarkClosed();
            if (_sessions.TryGetValue(peripheral.DeviceId, out var session) && ReferenceEquals(session.Peripheral, peripheral))
            {
                session.Peripheral = null;
                if (session.Open && !_disposed)
                {
                    session.Open = false;
                    _api.Disconnect(peripheral.DeviceId);
                }
            }

            FailPendingFor(peripheral, new BleException("connection", "closed"));
        }

        internal Task<IReadOnlyList<BleGattService>> DiscoverAsync(NativePeripheral peripheral)
        {
            if (peripheral.IsClosed || _disposed)
            {
                return Task.FromException<IReadOnlyList<BleGattService>>(new BleException("discover", "not connected", BleWire.ErrorNotConnected));
            }

            var reqId = _api.Discover(peripheral.DeviceId);
            if (reqId <= 0)
            {
                return Task.FromException<IReadOnlyList<BleGattService>>(new BleException("discover", "error " + Fmt.Integer(reqId), reqId));
            }

            var pending = new DiscoverPending(peripheral);
            _pending[reqId] = pending;
            return pending.Source.Task;
        }

        internal Task SetNotifyAsync(NativePeripheral peripheral, BleUuid service, BleUuid characteristic, bool enable)
        {
            if (peripheral.IsClosed || _disposed)
            {
                return Task.FromException(new BleException("setNotify", "not connected", BleWire.ErrorNotConnected));
            }

            return Track(_api.SetNotify(peripheral.DeviceId, service.ToString(), characteristic.ToString(), enable), "setNotify", peripheral);
        }

        internal Task WriteAsync(NativePeripheral peripheral, BleUuid service, BleUuid characteristic, byte[] data, bool withResponse)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (peripheral.IsClosed || _disposed)
            {
                return Task.FromException(new BleException("write", "not connected", BleWire.ErrorNotConnected));
            }

            return Track(_api.Write(peripheral.DeviceId, service.ToString(), characteristic.ToString(), data, data.Length, withResponse), "write", peripheral);
        }

        internal void StartKeepAlive(NativePeripheral peripheral, BleUuid service, BleUuid characteristic, byte[] data, TimeSpan period)
        {
            if (peripheral.IsClosed || _disposed)
            {
                return;
            }

            var code = _api.KeepAliveStart(peripheral.DeviceId, service.ToString(), characteristic.ToString(), data, data.Length,
                (int)period.TotalMilliseconds);
            if (code != 0)
            {
                _logger.Log(LogLevel.Warning, Tag, "Keepalive nativo: código " + Fmt.Integer(code));
            }
        }

        internal void StopKeepAlive(NativePeripheral peripheral)
        {
            if (_disposed)
            {
                return;
            }

            _api.KeepAliveStop(peripheral.DeviceId);
        }

        // ── La bomba (§5.2) ──

        public void Drain(bool resuming)
        {
            if (!_usable || _disposed)
            {
                return;
            }

            if (resuming)
            {
                OnResumed();
                if (!_clock.IsCalibrated)
                {
                    // Sin ningún drenaje de primer plano antes (no debería pasar): la mejor estimación posible.
                    _clock.Calibrate(_scheduler.MonotonicSeconds, _api.MonotonicSeconds());
                }
            }
            else
            {
                _clock.Calibrate(_scheduler.MonotonicSeconds, _api.MonotonicSeconds());
            }

            var guard = 0;
            while (_api.Pending() > 0 && guard++ < 1024)
            {
                var length = _api.Drain(_buffer, _buffer.Length);
                if (length <= 0)
                {
                    break;
                }

                _resuming = resuming;
                try
                {
                    BleEventReader.Read(_buffer, length, _sink);
                }
                catch (FormatException error)
                {
                    _logger.Log(LogLevel.Error, Tag, error.Message, error);
                    break;
                }
                finally
                {
                    _resuming = false;
                }
            }
        }

        // Al volver al frente (antes de los eventos de la pausa): el permiso pudo cambiar en los ajustes (AC-APP-13-02) y un aviso
        // del adaptador pudo perderse.
        private void OnResumed()
        {
            _permissions?.Refresh();
            _nativeState = ToState(_api.AdapterState());
            Recompute();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _wokeSubscription.Dispose();
            if (!_usable)
            {
                return;
            }

            if (_scanning)
            {
                _scanning = false;
                _api.StopScan();
            }

            foreach (var session in _sessions)
            {
                session.Value.Peripheral?.MarkClosed();
                if (session.Value.Open)
                {
                    session.Value.Open = false;
                    _api.Disconnect(session.Key);
                }
            }

            var pending = new List<Pending>(_pending.Values);
            _pending.Clear();
            for (var i = 0; i < pending.Count; i++)
            {
                pending[i].Fail(new BleException("ble", "shut down"));
            }

            _api.Shutdown();
        }

        // ── Eventos (en el reloj de la app) ──

        private void HandleAdapterState(int state)
        {
            _nativeState = ToState(state);
            Recompute();
        }

        private void HandleScanResult(string id, string? name, int rssi, BleUuid[] services)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _names[id] = name!;
            }

            if (!_scanning)
            {
                return;
            }

            _scanResults.Emit(new BleScanResult(id, name, name, rssi, services));
        }

        private void HandleScanFailed(int code, string? message)
        {
            _scanning = false;
            if (code == BleWire.ErrorPermission)
            {
                // Sin permiso: no es un banner (DV-100); el estado pasa a Unauthorized si no quedó concedido.
                _permissions?.Refresh();
                Recompute();
                _scanFailed.Emit(new BlePermissionDeniedException("scan", message ?? "permission denied"));
                return;
            }

            _scanFailed.Emit(new BleException("scan", message ?? "error " + Fmt.Integer(code), code));
        }

        private void HandleConnection(string id, bool connected, int code)
        {
            if (!_sessions.TryGetValue(id, out var session))
            {
                return;
            }

            if (connected)
            {
                return; // el connect termina con su RequestDone
            }

            var peripheral = session.Peripheral;
            if (peripheral == null || peripheral.IsClosed)
            {
                return;
            }

            // Caída no pedida (DT10-17): la sesión se cierra ANTES de avisar (en Windows, MaintainConnection = false); los
            // reintentos de Core abren una nueva.
            session.Peripheral = null;
            peripheral.MarkClosed();
            if (session.Open)
            {
                session.Open = false;
                _api.Disconnect(id);
            }

            var error = new BleException("connection", "link lost (code " + Fmt.Integer(code) + ")", code);
            FailPendingFor(peripheral, error);
            peripheral.RaiseLinkLost(error);
        }

        private void HandleRequestDone(int reqId, int status, ReadOnlySpan<byte> rest)
        {
            if (!_pending.TryGetValue(reqId, out var pending))
            {
                // Un connect abandonado (venció el plazo o lo reemplazó otro) que igual terminó bien: se cierra, salvo que ya haya
                // otra conexión del mismo id (el plugin la reemplazó al recibir el connect nuevo).
                if (_abandoned.TryGetValue(reqId, out var id))
                {
                    _abandoned.Remove(reqId);
                    if (status == 0 && _sessions.TryGetValue(id, out var session) && session.Peripheral == null && !session.ConnectReqId.HasValue)
                    {
                        _logger.Log(LogLevel.Info, Tag, "Conexión tardía cerrada: " + BleIds.Mask(id));
                        session.Open = false;
                        _api.Disconnect(id);
                    }
                }

                return;
            }

            _pending.Remove(reqId);
            pending.Complete(this, reqId, status, rest);
        }

        private void HandleNotify(double appSeconds, string id, BleUuid characteristic, byte[] value)
        {
            if (!_sessions.TryGetValue(id, out var session))
            {
                return;
            }

            var peripheral = session.Peripheral;
            if (peripheral == null || peripheral.IsClosed)
            {
                return;
            }

            peripheral.RaiseNotification(new BleNotification(characteristic, value, appSeconds));
        }

        private void HandleDropped(int count)
        {
            _logger.Log(LogLevel.Warning, Tag, "El búfer nativo descartó " + Fmt.Integer(count) + " eventos");
        }

        // ── Ayudas ──

        private void Recompute()
        {
            BleAdapterState state;
            if (!_usable)
            {
                state = BleAdapterState.Unsupported;
            }
            else if (_nativeState == BleAdapterState.Unsupported)
            {
                state = BleAdapterState.Unsupported;
            }
            else if (_permissions != null
                     && (_permissions.State == BlePermissionState.Denied || _permissions.State == BlePermissionState.DeniedForever))
            {
                state = BleAdapterState.Unauthorized;
            }
            else
            {
                state = _nativeState == BleAdapterState.Unauthorized ? BleAdapterState.Unknown : _nativeState;
            }

            _state.Value = state;
        }

        private static BleAdapterState ToState(int value)
        {
            return value >= (int)BleAdapterState.Unknown && value <= (int)BleAdapterState.TurningOff ? (BleAdapterState)value : BleAdapterState.Unknown;
        }

        private Session SessionFor(string id)
        {
            if (!_sessions.TryGetValue(id, out var session))
            {
                session = new Session();
                _sessions[id] = session;
            }

            return session;
        }

        private Task Track(int reqId, string operation, NativePeripheral peripheral)
        {
            if (reqId <= 0)
            {
                return Task.FromException(new BleException(operation, "error " + Fmt.Integer(reqId), reqId));
            }

            var pending = new SimplePending(operation, peripheral);
            _pending[reqId] = pending;
            return pending.Source.Task;
        }

        private void FailPendingFor(NativePeripheral peripheral, BleException error)
        {
            List<int>? doomed = null;
            foreach (var entry in _pending)
            {
                if (ReferenceEquals(entry.Value.Peripheral, peripheral))
                {
                    (doomed ??= new List<int>()).Add(entry.Key);
                }
            }

            if (doomed == null)
            {
                return;
            }

            for (var i = 0; i < doomed.Count; i++)
            {
                var pending = _pending[doomed[i]];
                _pending.Remove(doomed[i]);
                pending.Fail(error);
            }
        }

        private void Defer(double nativeSeconds, Action<double> handler)
        {
            var app = _clock.ToApp(nativeSeconds);
            // ADR 017: al volver, cada evento en su instante virtual del alcance (en un empate, en orden de llegada).
            var delay = app - _scheduler.MonotonicSeconds;
            _scheduler.Schedule(TimeSpan.FromTicks(delay > 0 ? (long)(delay * TimeSpan.TicksPerSecond) : 0), () => handler(app));
        }

        private sealed class Session
        {
            public int Generation;
            public int? ConnectReqId;
            public bool Open;
            public NativePeripheral? Peripheral;
        }

        private abstract class Pending
        {
            public NativePeripheral? Peripheral { get; protected set; }

            public abstract void Complete(NativeBleAdapter adapter, int reqId, int status, ReadOnlySpan<byte> rest);

            public abstract void Fail(Exception error);

            public virtual void Cancel()
            {
                Fail(new OperationCanceledException("cancelled"));
            }

            protected static string Message(ReadOnlySpan<byte> rest, int status)
            {
                return BleEventReader.ReadErrorMessage(rest) ?? "error " + Fmt.Integer(status);
            }
        }

        private sealed class ConnectPending : Pending
        {
            public ConnectPending(string deviceId, int generation, string? name)
            {
                DeviceId = deviceId;
                Generation = generation;
                Name = name;
            }

            public string DeviceId { get; }

            public int Generation { get; }

            public string? Name { get; }

            public CancellationTokenRegistration Registration { get; set; }

            public TaskCompletionSource<IBlePeripheral> Source { get; } = new TaskCompletionSource<IBlePeripheral>();

            public override void Complete(NativeBleAdapter adapter, int reqId, int status, ReadOnlySpan<byte> rest)
            {
                Registration.Dispose();
                var session = adapter.SessionFor(DeviceId);
                if (session.ConnectReqId == reqId)
                {
                    session.ConnectReqId = null;
                }

                if (status != 0)
                {
                    session.Open = false;
                    Source.TrySetException(new BleException("connect", Message(rest, status), status));
                    return;
                }

                var peripheral = new NativePeripheral(adapter, DeviceId, Name, Generation, adapter._logger);
                session.Peripheral = peripheral;
                session.Open = true;
                Source.TrySetResult(peripheral);
            }

            public override void Fail(Exception error)
            {
                Registration.Dispose();
                Source.TrySetException(error);
            }

            public override void Cancel()
            {
                Registration.Dispose();
                Source.TrySetCanceled();
            }
        }

        private sealed class DiscoverPending : Pending
        {
            public DiscoverPending(NativePeripheral peripheral)
            {
                Peripheral = peripheral;
            }

            public TaskCompletionSource<IReadOnlyList<BleGattService>> Source { get; } = new TaskCompletionSource<IReadOnlyList<BleGattService>>();

            public override void Complete(NativeBleAdapter adapter, int reqId, int status, ReadOnlySpan<byte> rest)
            {
                if (status != 0)
                {
                    Source.TrySetException(new BleException("discover", Message(rest, status), status));
                    return;
                }

                IReadOnlyList<BleGattService> services;
                try
                {
                    services = BleEventReader.ReadServices(rest);
                }
                catch (IndexOutOfRangeException)
                {
                    Source.TrySetException(new BleException("discover", "malformed services"));
                    return;
                }

                Source.TrySetResult(services);
            }

            public override void Fail(Exception error)
            {
                Source.TrySetException(error);
            }
        }

        private sealed class SimplePending : Pending
        {
            private readonly string _operation;

            public SimplePending(string operation, NativePeripheral peripheral)
            {
                _operation = operation;
                Peripheral = peripheral;
            }

            public TaskCompletionSource<bool> Source { get; } = new TaskCompletionSource<bool>();

            public override void Complete(NativeBleAdapter adapter, int reqId, int status, ReadOnlySpan<byte> rest)
            {
                if (status != 0)
                {
                    Source.TrySetException(new BleException(_operation, Message(rest, status), status));
                    return;
                }

                Source.TrySetResult(true);
            }

            public override void Fail(Exception error)
            {
                Source.TrySetException(error);
            }
        }

        // Traduce los registros (hora nativa) a llamadas en el reloj de la app. En primer plano, directo (sin cierres: solo asignan
        // los byte[] y string del evento); al volver, cada uno agendado en su instante virtual.
        private sealed class Sink : IBleEventSink
        {
            private readonly NativeBleAdapter _owner;

            public Sink(NativeBleAdapter owner)
            {
                _owner = owner;
            }

            public void OnAdapterState(double t, int state)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleAdapterState(state);
                    return;
                }

                LaterAdapterState(t, state);
            }

            public void OnScanResult(double t, string id, string? name, int rssi, BleUuid[] services)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleScanResult(id, name, rssi, services);
                    return;
                }

                LaterScanResult(t, id, name, rssi, services);
            }

            public void OnScanFailed(double t, int code, string? message)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleScanFailed(code, message);
                    return;
                }

                LaterScanFailed(t, code, message);
            }

            public void OnConnection(double t, string id, bool connected, int code)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleConnection(id, connected, code);
                    return;
                }

                LaterConnection(t, id, connected, code);
            }

            public void OnRequestDone(double t, int reqId, int status, ReadOnlySpan<byte> rest)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleRequestDone(reqId, status, rest);
                    return;
                }

                LaterRequestDone(t, reqId, status, rest.ToArray());
            }

            public void OnNotify(double t, string id, BleUuid characteristic, byte[] value)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleNotify(_owner._clock.ToApp(t), id, characteristic, value);
                    return;
                }

                LaterNotify(t, id, characteristic, value);
            }

            public void OnDropped(double t, int count)
            {
                if (!_owner._resuming)
                {
                    _owner.HandleDropped(count);
                    return;
                }

                LaterDropped(t, count);
            }

            private void LaterDropped(double t, int count)
            {
                _owner.Defer(t, _ => _owner.HandleDropped(count));
            }

            // Los diferidos, aparte: un cierre que captura parámetros se reserva al entrar al método, así el camino de primer plano
            // no lo paga.
            private void LaterAdapterState(double t, int state)
            {
                _owner.Defer(t, _ => _owner.HandleAdapterState(state));
            }

            private void LaterScanResult(double t, string id, string? name, int rssi, BleUuid[] services)
            {
                _owner.Defer(t, _ => _owner.HandleScanResult(id, name, rssi, services));
            }

            private void LaterScanFailed(double t, int code, string? message)
            {
                _owner.Defer(t, _ => _owner.HandleScanFailed(code, message));
            }

            private void LaterConnection(double t, string id, bool connected, int code)
            {
                _owner.Defer(t, _ => _owner.HandleConnection(id, connected, code));
            }

            private void LaterRequestDone(double t, int reqId, int status, byte[] rest)
            {
                _owner.Defer(t, _ => _owner.HandleRequestDone(reqId, status, rest));
            }

            private void LaterNotify(double t, string id, BleUuid characteristic, byte[] value)
            {
                _owner.Defer(t, app => _owner.HandleNotify(app, id, characteristic, value));
            }
        }
    }
}
