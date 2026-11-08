using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Models;
using RowMate.Core.Ports;
using RowMate.Core.Text;

namespace RowMate.Core.Ble
{
    // El remo FTMS real (F-BLE-04 a -09; 10a 02-tech-design §2.2-§2.5): puerto de BleService sobre IBleAdapter, con los tiempos
    // del legacy (RowerTimings.Legacy) en el IScheduler inyectado. Hilo principal. Diferencias con el legacy, todas de la spec o
    // del diseño:
    // - Reconexión (Q-29, DV-96, DV-97, DT10-02): un intento 3 s después de CADA fallo, sin límite; con el adaptador sin On se
    //   saltea y se reagenda. La cancelan DisconnectAsync, StartScanAsync y ConnectAsync; el watchdog y la manual nunca la arman.
    // - Watchdog (DT10-01, dp:69-86): vive acá y no en DeviceStore; mide con la hora de cada paquete (BleNotification).
    // - Permiso (DT10-16): se pide ANTES de la espera de 5 s del adaptador; negado no es un error (DV-100).
    // - Conexión solo a ids que vinieron como resultado FTMS en esta sesión de la app o al último remo (DT10-04, AC-BLE-SEC-01).
    // - El plazo de 15 s (10 s en la reconexión) cancela el connect del adaptador, que cierra la sesión nativa (DT10-17).
    // - Apagar el Bluetooth con el remo conectado se trata como una caída (QA-G10a-11).
    // - El fallo asíncrono del escaneo (ScanFailed) termina la búsqueda con error, salvo el de permiso (QA-G10a-08).
    // Todas las continuaciones comparan la generación de búsqueda o de conexión: una operación superada no toca el estado.
    // legacy: lib/core/bluetooth/ble_service.dart:11-345; lib/features/device/device_provider.dart:8, :64-86
    public sealed class RowerService : IRowerService
    {
        public const string Tag = "BLE";

        private static readonly IReadOnlyList<BleUuid> FtmsOnly = new[] { FtmsUuids.Service };
        private static readonly byte[] RequestControl = { 0x00 };
        private static readonly byte[] StartOrResume = { 0x07 };

        private readonly IBleAdapter _adapter;
        private readonly IScheduler _scheduler;
        private readonly IAppLifecycle _lifecycle;
        private readonly IAppLogger _logger;
        private readonly RowerTimings _timings;
        private readonly ObservableValue<BleStatus> _status;
        private readonly ObservableValue<BleAdapterState> _adapterState;
        private readonly ObservableValue<RowerLink> _link;
        private readonly EventStream<RowingData> _data;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly EventStream<byte[]> _rawBytes;
        private readonly HashSet<string> _known = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _names = new Dictionary<string, string?>(StringComparer.Ordinal);
        private readonly BleCadence _cadence = new BleCadence();
        private readonly IDisposable _adapterSubscription;
        private readonly IDisposable _backgroundSubscription;

        private int _scanGeneration;
        private int _connectGeneration;
        private string? _last;
        private string? _deviceName;
        private IBlePeripheral? _peripheral;
        private IDisposable? _notifySubscription;
        private IDisposable? _linkLostSubscription;
        private CancellationTokenSource? _connectCts;
        private CancellationTokenSource? _scanAbort;
        private (BleUuid Service, BleUuid Characteristic)? _controlPoint;
        private IDisposable? _keepAliveTimer;
        private bool _nativeKeepAlive;
        private IDisposable? _reconnectTimer;
        private IDisposable? _watchdogTimer;
        private double? _lastPacketAt;
        private double _lastCadenceReport;
        private bool _disposed;

        public RowerService(IBleAdapter adapter, IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger, RowerTimings? timings = null)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timings = timings ?? RowerTimings.Legacy;
            _status = new ObservableValue<BleStatus>(BleStatus.Disconnected, logger); // bs:43
            Status = new ReadOnlyObservable<BleStatus>(_status);
            _adapterState = new ObservableValue<BleAdapterState>(BleAdapterState.Unknown, logger); // bs:40
            AdapterState = new ReadOnlyObservable<BleAdapterState>(_adapterState);
            _link = new ObservableValue<RowerLink>(RowerLink.Idle, logger);
            Link = new ReadOnlyObservable<RowerLink>(_link);
            _data = new EventStream<RowingData>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
            _rawBytes = new EventStream<byte[]>(logger);
            // bs:46-55: el estado del adaptador desde el constructor (flutter_blue_plus emite el actual al suscribirse).
            _adapterSubscription = adapter.State.Subscribe(OnAdapterState);
            _backgroundSubscription = lifecycle.InBackground.Subscribe(OnBackground, false);
        }

        public IReadOnlyObservable<BleStatus> Status { get; }

        public IReadOnlyObservable<BleAdapterState> AdapterState { get; }

        public IReadOnlyObservable<RowerLink> Link { get; }

        public IEventSource<RowingData> Data => _data;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        public IEventSource<byte[]> RawBytes => _rawBytes;

        // bs:57: el nombre del equipo en curso (desde que empieza a conectar hasta la limpieza).
        public string? ConnectedDeviceName => _deviceName;

        // Hay un intento de reconexión agendado o en curso.
        public bool IsReconnecting => _link.Value.Reconnect != ReconnectPhase.None;

        public void Start()
        {
        }

        // bs:64-124 (AC-BLE-04-05, -06, AC-BLE-05-04 a -08, AC-APP-13-02).
        public async Task StartScanAsync(TimeSpan? timeout = null)
        {
            if (_disposed)
            {
                return;
            }

            // bs:66-67: sin Bluetooth LE no hace nada (la vista lo muestra, Q-47).
            if (_adapter.State.Value == BleAdapterState.Unsupported)
            {
                return;
            }

            // DT10-16: el permiso antes de esperar el adaptador. Negado no es un error (DV-100): el adaptador ya pasó a Unauthorized.
            var generation = ++_scanGeneration;
            if (!await _adapter.EnsureScanPermissionAsync())
            {
                _logger.Log(LogLevel.Info, Tag, "Permiso de Bluetooth denegado");
                if (generation == _scanGeneration)
                {
                    SetIdleStatus(BleStatus.Disconnected);
                }

                return;
            }

            if (_disposed || generation != _scanGeneration)
            {
                return;
            }

            // bs:69-84: espera hasta 5 s a que el adaptador esté encendido.
            if (_adapter.State.Value != BleAdapterState.On)
            {
                _logger.Log(LogLevel.Info, Tag, "Adaptador no listo (" + _adapter.State.Value + "), esperando...");
                if (!await WaitForAdapterOnAsync())
                {
                    _logger.Log(LogLevel.Info, Tag, "Timeout esperando adaptador");
                    if (generation == _scanGeneration && !_disposed)
                    {
                        SetIdleStatus(BleStatus.Off);
                    }

                    return;
                }

                _logger.Log(LogLevel.Info, Tag, "Adaptador listo");
                if (_disposed || generation != _scanGeneration)
                {
                    return;
                }
            }

            // Q-29, DV-96: buscar cancela la reconexión (en el legacy, no).
            CancelReconnect();
            _status.Value = BleStatus.Scanning; // bs:86
            var found = new List<BleScanResult>(); // bs:87: el orden del primer descubrimiento, reemplazo en el lugar (:107)
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            var clientSide = !_adapter.FiltersScanInOs;
            BleException? failure = null;
            var failed = new CancellationTokenSource();
            _scanAbort = failed;
            var results = _adapter.ScanResults.Subscribe(r =>
            {
                if (generation != _scanGeneration || r == null || !ScanFilter.Accept(r, FtmsUuids.Service, clientSide))
                {
                    return;
                }

                _known.Add(r.DeviceId);
                _names[r.DeviceId] = r.PlatformName;
                if (index.TryGetValue(r.DeviceId, out var at))
                {
                    found[at] = r;
                }
                else
                {
                    index[r.DeviceId] = found.Count;
                    found.Add(r);
                }

                _devices.Emit(found.ToArray());
            });
            // QA-G10a-08: el fallo asíncrono (onScanFailed) termina esta búsqueda; el de permiso, sin banner.
            var failures = _adapter.ScanFailed.Subscribe(e =>
            {
                if (generation != _scanGeneration || failure != null)
                {
                    return;
                }

                _logger.Log(LogLevel.Warning, Tag, "Error en el escaneo: " + e.Message);
                failure = e;
                failed.Cancel();
            });
            try
            {
                await _adapter.StartScanAsync(FtmsOnly);
                if (failure == null)
                {
                    try
                    {
                        await _scheduler.Delay(timeout ?? _timings.Scan, failed.Token); // bs:113
                    }
                    catch (OperationCanceledException) when (failure != null || _disposed)
                    {
                    }
                }
            }
            catch (BlePermissionDeniedException denied)
            {
                failure = denied;
            }
            finally
            {
                results.Dispose();
                failures.Dispose();
                if (ReferenceEquals(_scanAbort, failed))
                {
                    _scanAbort = null;
                }

                failed.Dispose();
                if (generation == _scanGeneration && !_disposed)
                {
                    await StopAdapterScanQuietly();
                    // bs:118-121: vuelve a desconectado solo si sigue buscando (también si startScan lanzó).
                    if (_status.Value == BleStatus.Scanning)
                    {
                        _status.Value = BleStatus.Disconnected;
                    }
                }
            }

            if (failure != null && !(failure is BlePermissionDeniedException))
            {
                throw failure;
            }
        }

        // bs:126-132. Desvío menor: no baja a desconectado si el remo está conectando o conectado (el legacy no lo llama así).
        public async Task StopScanAsync()
        {
            if (_disposed)
            {
                return;
            }

            var wasScanning = _status.Value == BleStatus.Scanning;
            _scanGeneration++;
            if (wasScanning)
            {
                await StopAdapterScanQuietly();
            }

            SetIdleStatus(BleStatus.Disconnected);
        }

        // bs:135-160 (AC-BLE-06-01 a -06, AC-BLE-SEC-01).
        public async Task ConnectAsync(string deviceId)
        {
            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (_disposed)
            {
                return;
            }

            // DT10-04: solo un remo que el usuario vio en la lista (FTMS) o el último que tuvo conectado.
            if (!_known.Contains(deviceId) && deviceId != _last)
            {
                throw new ArgumentException("RowerService: " + BleIds.Mask(deviceId) + " no vino de una búsqueda FTMS.", nameof(deviceId));
            }

            CancelReconnect(); // bs:136-137
            var generation = BeginAttempt(deviceId);
            _last = deviceId; // bs:140
            PublishLink(deviceId, ConnectPhase.Connecting, ReconnectPhase.None, null, DisconnectReason.None);
            try
            {
                await ConnectAndPrepareAsync(deviceId, _timings.Connect, generation);
            }
            catch (Exception error)
            {
                if (generation != _connectGeneration || _disposed)
                {
                    return;
                }

                // bs:156-159
                _logger.Log(LogLevel.Warning, Tag, "Error al conectar " + BleIds.Mask(deviceId) + ": " + error.Message);
                Cleanup();
                _status.Value = BleStatus.Disconnected;
                PublishLink(deviceId, ConnectPhase.None, ReconnectPhase.None, null, DisconnectReason.ConnectFailed);
                throw;
            }
        }

        // bs:304-313 (AC-BLE-07-05): corta, olvida el remo y no reconecta.
        public Task DisconnectAsync()
        {
            return DisconnectCoreAsync(DisconnectReason.Manual);
        }

        // AC-BLE-04-04 (Q-32): nunca lanza.
        public async Task RequestEnableAsync()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                await _adapter.RequestEnableAsync();
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "Activar Bluetooth: " + error.Message, error);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _scanGeneration++;
            _connectGeneration++;
            CancelReconnect();
            _connectCts?.Cancel();
            _scanAbort?.Cancel();
            Cleanup();
            _adapterSubscription.Dispose();
            _backgroundSubscription.Dispose();
        }

        // ── Adaptador y segundo plano ──

        private void OnAdapterState(BleAdapterState state)
        {
            _logger.Log(LogLevel.Info, Tag, "Adapter state: " + state); // bs:48
            _adapterState.Value = state;
            if (state != BleAdapterState.Off)
            {
                return;
            }

            // QA-G10a-11: con el remo conectado (o conectando), apagar el Bluetooth es una caída: se limpia y la reconexión espera
            // el On (AC-BLE-04-01, AC-BLE-08-04, DV-97). No depende del aviso nativo, que puede tardar (R-03).
            if (_peripheral != null || _status.Value == BleStatus.Connecting)
            {
                var wasAttempt = _status.Value == BleStatus.Connecting;
                _connectGeneration++;
                _connectCts?.Cancel();
                Cleanup();
                ScheduleReconnect(wasAttempt ? DisconnectReason.ConnectFailed : DisconnectReason.Lost);
            }

            _status.Value = BleStatus.Off; // bs:51-53
        }

        private void OnBackground(bool inBackground)
        {
            if (_disposed || _peripheral == null || _controlPoint == null || !_adapter.NeedsNativeKeepAlive)
            {
                return;
            }

            var cp = _controlPoint.Value;
            if (inBackground && !_nativeKeepAlive && _keepAliveTimer != null)
            {
                // ADR 017, DT10-09: Unity pausa el loop; el plugin sigue escribiendo 0x07 cada 2 s.
                _keepAliveTimer.Dispose();
                _keepAliveTimer = null;
                _peripheral.StartNativeKeepAlive(cp.Service, cp.Characteristic, StartOrResume, _timings.KeepAlive);
                _nativeKeepAlive = true;
            }
            else if (!inBackground && _nativeKeepAlive)
            {
                _peripheral.StopNativeKeepAlive();
                _nativeKeepAlive = false;
                StartKeepAlive();
            }
        }

        private async Task<bool> WaitForAdapterOnAsync()
        {
            var on = new TaskCompletionSource<bool>();
            using (var timeout = new CancellationTokenSource())
            using (_adapter.State.Subscribe(s =>
                   {
                       if (s == BleAdapterState.On)
                       {
                           on.TrySetResult(true);
                       }
                   }))
            {
                if (on.Task.IsCompleted)
                {
                    return true;
                }

                var delay = _scheduler.Delay(_timings.AdapterWait, timeout.Token);
                var first = await Task.WhenAny(on.Task, delay);
                if (first == on.Task)
                {
                    timeout.Cancel();
                    return true;
                }

                return false;
            }
        }

        private async Task StopAdapterScanQuietly()
        {
            try
            {
                await _adapter.StopScanAsync();
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "Error al detener el escaneo: " + error.Message, error);
            }
        }

        // Desconectado / apagado sin pisar una conexión en curso o hecha.
        private void SetIdleStatus(BleStatus status)
        {
            if (_status.Value != BleStatus.Connected && _status.Value != BleStatus.Connecting)
            {
                _status.Value = status;
            }
        }

        // ── Conexión (bs:135-160, :163-199, :204-289) ──

        private int BeginAttempt(string deviceId)
        {
            var generation = ++_connectGeneration;
            _connectCts?.Cancel();
            _deviceName = _names.TryGetValue(deviceId, out var name) ? name : _deviceName;
            _status.Value = BleStatus.Connecting; // bs:138
            return generation;
        }

        // connect con tope, descubrimiento, toma de control y keepalive; después, el listener de caída y Connected.
        private async Task ConnectAndPrepareAsync(string deviceId, TimeSpan limit, int generation)
        {
            var cts = new CancellationTokenSource();
            _connectCts = cts;
            var timedOut = false;
            IBlePeripheral peripheral;
            var timer = _scheduler.Schedule(limit, () =>
            {
                timedOut = true;
                cts.Cancel();
            });
            try
            {
                peripheral = await _adapter.ConnectAsync(deviceId, true, cts.Token);
            }
            catch (OperationCanceledException) when (timedOut)
            {
                throw new BleException("connect", "timed out after " + Fmt.Integer((long)limit.TotalSeconds) + " s");
            }
            finally
            {
                timer.Dispose();
                if (ReferenceEquals(_connectCts, cts))
                {
                    _connectCts = null;
                }

                cts.Dispose();
            }

            if (generation != _connectGeneration || _disposed)
            {
                peripheral.Dispose();
                throw new OperationCanceledException("RowerService: conexión superada");
            }

            _peripheral = peripheral;
            _deviceName = peripheral.Name ?? (_names.TryGetValue(deviceId, out var listed) ? listed : null);
            _notifySubscription = peripheral.Notifications.Subscribe(OnNotification);
            await DiscoverAsync(peripheral, generation);
            if (generation != _connectGeneration || _disposed)
            {
                throw new OperationCanceledException("RowerService: conexión superada");
            }

            // bs:146-154: el listener de caída DESPUÉS de conectar y descubrir.
            _linkLostSubscription = peripheral.LinkLost.Subscribe(e => OnLinkLost(generation, e));
            // QA-G10a-10 (dp:64-67): los paquetes de la toma de control no cuentan para el watchdog.
            _lastPacketAt = null;
            _lastCadenceReport = _scheduler.MonotonicSeconds;
            StartWatchdog();
            _status.Value = BleStatus.Connected; // bs:155
            PublishLink(deviceId, ConnectPhase.Ready, ReconnectPhase.None, null, DisconnectReason.None);
        }

        // bs:204-257 (AC-BLE-06-02).
        private async Task DiscoverAsync(IBlePeripheral peripheral, int generation)
        {
            var services = await peripheral.DiscoverServicesAsync();
            if (generation != _connectGeneration)
            {
                return;
            }

            _logger.Log(LogLevel.Info, Tag, "===== SERVICIOS DESCUBIERTOS =====");
            var hasRowerData = false;
            (BleUuid, BleUuid)? controlPoint = null;
            for (var s = 0; s < services.Count; s++)
            {
                var service = services[s];
                _logger.Log(LogLevel.Info, Tag, "Servicio: " + service.Uuid);
                for (var c = 0; c < service.Characteristics.Count; c++)
                {
                    var chr = service.Characteristics[c];
                    _logger.Log(LogLevel.Info, Tag, "  Char: " + chr.Uuid + " [" + PropertyNames(chr.Properties) + "]");
                    if (chr.CanNotify)
                    {
                        try
                        {
                            await peripheral.SetNotifyAsync(service.Uuid, chr.Uuid, true);
                            _logger.Log(LogLevel.Info, Tag, "  → Suscrito a " + chr.Uuid);
                        }
                        catch (Exception error)
                        {
                            _logger.Log(LogLevel.Warning, Tag, "  → Error suscribiendo " + chr.Uuid + ": " + error.Message);
                        }

                        if (generation != _connectGeneration)
                        {
                            return;
                        }
                    }

                    if (chr.Uuid == FtmsUuids.ControlPoint)
                    {
                        controlPoint = (service.Uuid, chr.Uuid);
                    }

                    if (chr.Uuid == FtmsUuids.RowerData)
                    {
                        hasRowerData = true;
                    }
                }
            }

            _logger.Log(LogLevel.Info, Tag, "===================================");
            if (!hasRowerData)
            {
                // bs:251-253 (Q-35): K-BIKE queda conectado con las métricas en 0 y sin watchdog.
                _logger.Log(LogLevel.Warning, Tag, "AVISO: No se encontró 0x2AD1 – continuando de todas formas");
            }

            _controlPoint = controlPoint;
            await RequestFtmsControlAsync(peripheral, generation);
        }

        // bs:259-289 (AC-BLE-06-03, -04; QA-G10a-07: si falla 0x00 no espera ni escribe 0x07, y el keepalive se arma igual).
        private async Task RequestFtmsControlAsync(IBlePeripheral peripheral, int generation)
        {
            if (_controlPoint == null)
            {
                _logger.Log(LogLevel.Info, Tag, "Control Point no encontrado, omitiendo handshake");
                return;
            }

            var cp = _controlPoint.Value;
            if (_last != null)
            {
                PublishLink(_last, ConnectPhase.TakingControl, _link.Value.Reconnect, null, _link.Value.LastDisconnect);
            }

            try
            {
                _logger.Log(LogLevel.Info, Tag, "Control Point: solicitando control (0x00)");
                await peripheral.WriteAsync(cp.Service, cp.Characteristic, RequestControl, true);
                await _scheduler.Delay(_timings.ControlGap);
                if (generation != _connectGeneration)
                {
                    return;
                }

                _logger.Log(LogLevel.Info, Tag, "Control Point: iniciando sesión (0x07)");
                await peripheral.WriteAsync(cp.Service, cp.Characteristic, StartOrResume, true);
                _logger.Log(LogLevel.Info, Tag, "Handshake FTMS completado");
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "Control Point error (puede ser ignorado): " + error.Message);
            }

            if (generation == _connectGeneration)
            {
                StartKeepAlive();
            }
        }

        // bs:278-288: 0x07 cada 2 s; en segundo plano con keepalive nativo, lo hace el plugin (ADR 017).
        private void StartKeepAlive()
        {
            _keepAliveTimer?.Dispose();
            _keepAliveTimer = null;
            var peripheral = _peripheral;
            if (peripheral == null || _controlPoint == null)
            {
                return;
            }

            if (_lifecycle.InBackground.Value && _adapter.NeedsNativeKeepAlive)
            {
                var cp = _controlPoint.Value;
                peripheral.StartNativeKeepAlive(cp.Service, cp.Characteristic, StartOrResume, _timings.KeepAlive);
                _nativeKeepAlive = true;
                return;
            }

            _keepAliveTimer = _scheduler.SchedulePeriodic(_timings.KeepAlive, () => KeepAliveTick(peripheral));
        }

        private void KeepAliveTick(IBlePeripheral peripheral)
        {
            // ADR 017: quien escribe al hardware no escribe "en el pasado" durante el alcance.
            if (_scheduler.IsCatchingUp || _controlPoint == null || !ReferenceEquals(peripheral, _peripheral))
            {
                return;
            }

            KeepAliveWriteAsync(peripheral, _controlPoint.Value).Forget(_logger, Tag);
        }

        private async Task KeepAliveWriteAsync(IBlePeripheral peripheral, (BleUuid Service, BleUuid Characteristic) cp)
        {
            try
            {
                await peripheral.WriteAsync(cp.Service, cp.Characteristic, StartOrResume, true);
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "Keepalive error: " + error.Message);
            }
        }

        // ── Datos (bs:291-302) y watchdog (dp:69-86) ──

        private void OnNotification(BleNotification notification)
        {
            var bytes = notification.Value;
            if (bytes.Length == 0)
            {
                return; // bs:224, :292
            }

            if (notification.Characteristic != FtmsUuids.RowerData)
            {
                // bs:222-227: el resto de las características, solo al registro.
                _logger.Log(LogLevel.Debug, "BLE-CHAR:" + notification.Characteristic.ToString().ToUpperInvariant(),
                    "len=" + Fmt.Integer(bytes.Length) + " → " + BleHex.Format(bytes));
                return;
            }

            _logger.Log(LogLevel.Debug, Tag, "Raw bytes (" + Fmt.Integer(bytes.Length) + "): " + BleHex.Format(bytes));
            _lastPacketAt = notification.MonotonicSeconds;
            _rawBytes.Emit((byte[])bytes.Clone());
            var parsed = FtmsParser.ParseRowerData(bytes);
            if (parsed == null)
            {
                _logger.Log(LogLevel.Debug, Tag, "Parser returned null");
                return;
            }

            _logger.Log(LogLevel.Debug, Tag, "Parsed → SPM:" + Fmt.DartToString(parsed.StrokeRate) + " W:" + Fmt.Integer(parsed.PowerWatts)
                                             + " dist:" + Fmt.Integer(parsed.DistanceMeters) + "m elapsed:" + Fmt.Integer(parsed.ElapsedSeconds) + "s");
            _cadence.Add(notification.MonotonicSeconds, parsed);
            if (notification.MonotonicSeconds - _lastCadenceReport >= 60)
            {
                _lastCadenceReport = notification.MonotonicSeconds;
                _logger.Log(LogLevel.Info, Tag, _cadence.Report());
            }

            _data.Emit(parsed);
        }

        private void StartWatchdog()
        {
            _watchdogTimer?.Dispose();
            _watchdogTimer = _scheduler.SchedulePeriodic(_timings.WatchdogPeriod, WatchdogTick);
        }

        // dp:71-80 (AC-BLE-09-01): solo después del primer paquete de esta conexión; ≥ 5 s enteros (inSeconds) → manual.
        private void WatchdogTick()
        {
            if (_status.Value != BleStatus.Connected || !_lastPacketAt.HasValue)
            {
                return;
            }

            var silence = _scheduler.MonotonicSeconds - _lastPacketAt.Value + 1e-9;
            if (Math.Floor(silence) >= _timings.WatchdogThreshold.TotalSeconds)
            {
                _logger.Log(LogLevel.Warning, Tag, "Watchdog: sin datos por " + Fmt.Integer((long)Math.Floor(silence)) + "s → desconectando");
                StopWatchdog();
                DisconnectCoreAsync(DisconnectReason.Watchdog).Forget(_logger, Tag);
            }
        }

        private void StopWatchdog()
        {
            _watchdogTimer?.Dispose();
            _watchdogTimer = null;
        }

        // ── Caída, reconexión y desconexión ──

        // bs:148-153 (AC-BLE-08-02).
        private void OnLinkLost(int generation, BleException error)
        {
            if (generation != _connectGeneration || _disposed)
            {
                return;
            }

            _logger.Log(LogLevel.Warning, Tag, "Enlace caído: " + error.Message);
            _status.Value = BleStatus.Disconnected;
            Cleanup();
            ScheduleReconnect(DisconnectReason.Lost);
        }

        // Q-29, DV-96, DT10-02 (AC-BLE-08-02, -03): un intento a los 3 s de la caída y 3 s después de cada fallo, sin límite.
        // El enlace pasa directo a Waiting (sin un estado intermedio sin reconexión: el aviso fijo del HUD no parpadea).
        private void ScheduleReconnect(DisconnectReason reason)
        {
            var id = _last;
            if (_disposed)
            {
                return;
            }

            if (id == null)
            {
                _link.Value = _link.Value with { Phase = ConnectPhase.None, Reconnect = ReconnectPhase.None, NextAttemptAt = null, LastDisconnect = reason };
                return;
            }

            _logger.Log(LogLevel.Info, Tag, "Reconexión automática programada...");
            _reconnectTimer?.Dispose();
            var due = _scheduler.MonotonicSeconds + _timings.ReconnectDelay.TotalSeconds;
            PublishLink(id, ConnectPhase.None, ReconnectPhase.Waiting, due, reason);
            _reconnectTimer = _scheduler.Schedule(_timings.ReconnectDelay, OnReconnectDue);
        }

        private void OnReconnectDue()
        {
            _reconnectTimer = null;
            var id = _last;
            if (id == null || _disposed)
            {
                return;
            }

            // DV-97, DT10-02: sin el adaptador encendido el intento se saltea y se reagenda; al encenderlo, reconecta solo.
            if (_adapter.State.Value != BleAdapterState.On)
            {
                ScheduleReconnect(_link.Value.LastDisconnect);
                return;
            }

            ReconnectAttemptAsync(id).Forget(_logger, Tag);
        }

        // bs:173-197 con el tope de 10 s.
        private async Task ReconnectAttemptAsync(string id)
        {
            _logger.Log(LogLevel.Info, Tag, "Intentando reconectar a " + (_names.TryGetValue(id, out var name) && name != null ? name : BleIds.Mask(id)) + "...");
            var generation = BeginAttempt(id);
            PublishLink(id, ConnectPhase.Connecting, ReconnectPhase.Attempting, null, _link.Value.LastDisconnect);
            try
            {
                await ConnectAndPrepareAsync(id, _timings.ReconnectConnect, generation);
                _logger.Log(LogLevel.Info, Tag, "Reconexión exitosa");
            }
            catch (Exception error)
            {
                if (generation != _connectGeneration || _disposed)
                {
                    return;
                }

                _logger.Log(LogLevel.Warning, Tag, "Reconexión fallida: " + error.Message);
                _status.Value = BleStatus.Disconnected;
                Cleanup();
                ScheduleReconnect(DisconnectReason.ConnectFailed);
            }
        }

        private void CancelReconnect()
        {
            _reconnectTimer?.Dispose();
            _reconnectTimer = null;
            var link = _link.Value;
            if (link.Reconnect != ReconnectPhase.None)
            {
                if (link.Reconnect == ReconnectPhase.Attempting)
                {
                    _connectGeneration++;
                    _connectCts?.Cancel();
                    Cleanup();
                    if (_status.Value == BleStatus.Connecting)
                    {
                        _status.Value = BleStatus.Disconnected;
                    }
                }

                _link.Value = link with { Phase = ConnectPhase.None, Reconnect = ReconnectPhase.None, NextAttemptAt = null };
            }
        }

        private async Task DisconnectCoreAsync(DisconnectReason reason)
        {
            if (_disposed)
            {
                return;
            }

            _logger.Log(LogLevel.Info, Tag, reason == DisconnectReason.Watchdog ? "Desconexión del watchdog" : "Desconexión manual");
            CancelReconnect(); // bs:306-307
            var id = _last;
            _last = null; // bs:308: no reconectar
            _connectGeneration++;
            _connectCts?.Cancel();
            var peripheral = _peripheral;
            _linkLostSubscription?.Dispose();
            _linkLostSubscription = null;
            if (peripheral != null)
            {
                try
                {
                    await peripheral.DisconnectAsync(); // bs:309
                }
                catch (Exception error)
                {
                    _logger.Log(LogLevel.Warning, Tag, "Error al desconectar: " + error.Message, error);
                }
            }

            if (ReferenceEquals(_peripheral, peripheral))
            {
                Cleanup(); // bs:310
            }

            if (_disposed)
            {
                return;
            }

            _status.Value = BleStatus.Disconnected; // bs:312
            _link.Value = new RowerLink(id, _link.Value.DeviceName, ConnectPhase.None, ReconnectPhase.None, null, reason);
        }

        // bs:315-333: corta keepalive, watchdog y suscripciones y suelta el equipo (cierra la sesión nativa si sigue abierta).
        // La reconexión NO se cancela acá (Q-29: el legacy sí, por eso hacía un solo intento).
        private void Cleanup()
        {
            _keepAliveTimer?.Dispose();
            _keepAliveTimer = null;
            StopWatchdog();
            _notifySubscription?.Dispose();
            _notifySubscription = null;
            _linkLostSubscription?.Dispose();
            _linkLostSubscription = null;
            var peripheral = _peripheral;
            _peripheral = null;
            if (peripheral != null)
            {
                if (_nativeKeepAlive)
                {
                    peripheral.StopNativeKeepAlive();
                }

                peripheral.Dispose();
            }

            _nativeKeepAlive = false;
            _controlPoint = null;
            _deviceName = null;
            _lastPacketAt = null;
            if (_cadence.Packets > 0)
            {
                _logger.Log(LogLevel.Info, Tag, _cadence.Report());
                _cadence.Reset();
            }
        }

        private void PublishLink(string? id, ConnectPhase phase, ReconnectPhase reconnect, double? nextAttemptAt, DisconnectReason reason)
        {
            string? name = null;
            if (id != null && _names.TryGetValue(id, out var listed))
            {
                name = listed;
            }

            if (id != null && _deviceName != null)
            {
                name = _deviceName;
            }

            _link.Value = new RowerLink(id, name, phase, reconnect, nextAttemptAt, reason);
        }

        private static string PropertyNames(BleCharacteristicProperties properties)
        {
            // bs:211-215: READ, WRITE, NOTIFY, INDICATE en ese orden (WRITE también con "sin respuesta").
            var parts = new List<string>(4);
            if ((properties & BleCharacteristicProperties.Read) != 0)
            {
                parts.Add("READ");
            }

            if ((properties & (BleCharacteristicProperties.Write | BleCharacteristicProperties.WriteNoResponse)) != 0)
            {
                parts.Add("WRITE");
            }

            if ((properties & BleCharacteristicProperties.Notify) != 0)
            {
                parts.Add("NOTIFY");
            }

            if ((properties & BleCharacteristicProperties.Indicate) != 0)
            {
                parts.Add("INDICATE");
            }

            return string.Join(",", parts);
        }
    }
}
