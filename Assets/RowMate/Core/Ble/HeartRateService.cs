using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Core.Text;

namespace RowMate.Core.Ble
{
    // El pulsómetro real (F-HRM-02, -03, -05 a -11, -16; It. 11, 02-tech-design §2.2-§2.6): puerto de HeartRateService sobre el MISMO
    // IBleAdapter del remo, con ReconnectLoop y StaleWatchdog en el IScheduler inyectado y la memoria en las preferencias de la app.
    // Hilo principal: los eventos del plugin llegan por la bomba de la 10a. Diferencias con el legacy, todas de la spec o del diseño:
    // - DT11-02: el watchdog por plazo (un solo temporizador). DT11-04: un sensor sin nombre se publica como "" (nunca la dirección).
    // - DT11-05: un sensor nuevo sin nombre borra hrm.deviceName (Q-41); el mismo id sin nombre conserva el guardado.
    // - DT11-06: solo conecta a un id que llegó en una búsqueda del pulsómetro de esta ejecución o al recordado.
    // - DT11-07: un intento reemplazado cierra lo suyo, salvo que la acción nueva apunte al mismo id (hrs:286-291).
    // - DT11-08: buscar pide el permiso primero (el mismo que el remo); negado = desconectado y sin error.
    // - DT11-09: apagar el Bluetooth con el sensor conectado es una caída; las cadencias NO se saltean con el adaptador apagado.
    // - DT11-10: el registro enmascara las direcciones (BleIds.Mask).
    // - QA-G11-02: EndScanWait sube la generación de búsqueda ANTES de cancelar la espera (FakeScheduler cancela en línea).
    // Todas las continuaciones comparan la generación de conexión (_connectGen) o de búsqueda (_scanGen): una operación superada no
    // toca el estado compartido.
    // legacy: lib/core/bluetooth/heart_rate_service.dart:30-448
    public sealed class HeartRateService : IHeartRateService
    {
        public const string Tag = "HRM";

        // hrs:34-35
        public const string PrefDeviceId = "hrm.deviceId";
        public const string PrefDeviceName = "hrm.deviceName";

        private static readonly IReadOnlyList<BleUuid> HeartRateOnly = new[] { HeartRateParser.Service };
        private static readonly IReadOnlyList<BleScanResult> NoResults = new BleScanResult[0];

        private readonly IBleAdapter _adapter;
        private readonly IPreferences _preferences;
        private readonly IScheduler _scheduler;
        private readonly IAppLogger _logger;
        private readonly HrmTimings _timings;
        private readonly ObservableValue<HrmStatus> _status;
        private readonly ObservableValue<HrmLink> _link;
        private readonly EventStream<int> _bpm;
        private readonly EventStream<IReadOnlyList<BleScanResult>> _devices;
        private readonly HashSet<string> _known = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string?> _advNames = new Dictionary<string, string?>(StringComparer.Ordinal);
        private readonly ReconnectLoop _startupLoop;
        private readonly ReconnectLoop _dropLoop;
        private readonly StaleWatchdog _watchdog;
        private readonly IDisposable _adapterSubscription;
        private readonly Action<BleNotification> _onNotification;

        // hrs:66-69: connect, startScan, disconnect, forget y dispose la suben; un ConnectToAsync de una generación anterior se
        // aborta en su próximo await sin tocar el estado compartido.
        private int _connectGen;

        // QA-G11-02: la búsqueda en curso es dueña de _scanDone y del estado solo mientras su generación sea la vigente.
        private int _scanGen;
        private CancellationTokenSource? _scanDone;

        // = _device + _notifySub + _connSub del legacy: el intento (o la conexión) vigente.
        private HrmAttempt? _attempt;
        private string? _rememberedId;
        private string? _rememberedName;
        private bool _isDisconnecting;
        private bool _isStale;
        private HrmConnectOrigin _connectedBy;
        private int _currentBpm;
        private bool _disposed;

        public HeartRateService(IBleAdapter adapter, IPreferences preferences, IScheduler scheduler, IAppLogger logger, HrmTimings? timings = null)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timings = timings ?? HrmTimings.Legacy;
            _status = new ObservableValue<HrmStatus>(HrmStatus.Disconnected, logger); // hrs:55
            Status = new ReadOnlyObservable<HrmStatus>(_status);
            _link = new ObservableValue<HrmLink>(HrmLink.Idle, logger);
            Link = new ReadOnlyObservable<HrmLink>(_link);
            _bpm = new EventStream<int>(logger);
            _devices = new EventStream<IReadOnlyList<BleScanResult>>(logger);
            _onNotification = OnNotification;

            // hrs:76-89
            _startupLoop = new ReconnectLoop(scheduler, _timings.StartupInterval, () => TryRememberedAsync(HrmConnectOrigin.Startup), logger,
                _timings.StartupMaxAttempts, ReemitStatus);
            _dropLoop = new ReconnectLoop(scheduler, _timings.DropInterval, () => TryRememberedAsync(HrmConnectOrigin.Drop), logger);
            _startupLoop.Changed += PublishLink;
            _dropLoop.Changed += PublishLink;
            _watchdog = new StaleWatchdog(scheduler, _timings.Stale, OnStale);
            _adapterSubscription = adapter.State.Subscribe(OnAdapterState, false);
        }

        public IReadOnlyObservable<HrmStatus> Status { get; }

        public IReadOnlyObservable<HrmLink> Link { get; }

        public IEventSource<int> Bpm => _bpm;

        public int CurrentBpm => _currentBpm;

        public IEventSource<IReadOnlyList<BleScanResult>> Devices => _devices;

        // hrs:91-95 con DT11-04: el del sistema, si no el recordado, si no "" (la tarjeta muestra hrmUnknownSensor); null = no conectado.
        public string? ConnectedDeviceName
        {
            get
            {
                if (_status.Value != HrmStatus.Connected)
                {
                    return null;
                }

                var name = _attempt?.Peripheral?.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }

                return _rememberedName ?? "";
            }
        }

        // hrs:97-98 con DT11-04: null = no hay sensor recordado; "" = recordado sin nombre.
        public string? RememberedDeviceName => _rememberedId == null ? null : _rememberedName ?? "";

        // hrs:100
        public bool IsRetrying => _startupLoop.IsRunning || _dropLoop.IsRunning;

        // ── Búsqueda (hrs:108-183) ──

        // AC-HRM-02-01 a -05, AC-HRM-16-02.
        public async Task StartScanAsync(TimeSpan? timeout = null)
        {
            if (_disposed)
            {
                return;
            }

            // hrs:109-112: antes del primer await, que ningún reintento se cuele en el hueco.
            _connectGen++;
            CancelLoops();
            AbortPendingConnect();
            if (_adapter.State.Value == BleAdapterState.Unsupported)
            {
                return; // hrs:113
            }

            // QA-G11-02: la búsqueda anterior termina ya y deja de ser dueña de nada (todavía sin await).
            var previousWasScanning = _scanDone != null;
            EndScanWait();
            var generation = _scanGen;
            var done = new CancellationTokenSource();
            _scanDone = done;

            // DT11-08: el permiso primero (el mismo que el remo). Negado no es un error (DV-100).
            bool permitted;
            try
            {
                permitted = await _adapter.EnsureScanPermissionAsync();
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "Permiso de Bluetooth: " + error.Message);
                permitted = false;
            }

            if (!permitted)
            {
                if (generation == _scanGen && !_disposed)
                {
                    _logger.Log(LogLevel.Info, Tag, "Permiso de Bluetooth denegado");
                    _scanDone = null;
                    if (previousWasScanning)
                    {
                        await StopAdapterScanQuietly();
                    }

                    if (_status.Value == HrmStatus.Scanning)
                    {
                        SetStatus(HrmStatus.Disconnected);
                    }
                }

                done.Dispose();
                return;
            }

            if (generation != _scanGen || _disposed)
            {
                done.Dispose();
                return;
            }

            SetStatus(HrmStatus.Scanning); // hrs:117
            _devices.Emit(NoResults); // hrs:118
            var found = new List<BleScanResult>();
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            var clientSide = !_adapter.FiltersScanInOs;
            var results = _adapter.ScanResults.Subscribe(r =>
            {
                // R-05: solo los de esta búsqueda; en Windows filtra la app (hrs:135-136, sf:12-20).
                if (generation != _scanGen || r == null || !ScanFilter.Accept(r, HeartRateParser.Service, clientSide))
                {
                    return;
                }

                _known.Add(r.DeviceId);
                _advNames[r.DeviceId] = r.AdvName;
                if (index.TryGetValue(r.DeviceId, out var at))
                {
                    found[at] = r; // hrs:143: reemplazo en el lugar
                }
                else
                {
                    // hrs:138-142 con DT11-10: la dirección enmascarada; el nombre, solo al Player.log local.
                    _logger.Log(LogLevel.Info, Tag, "Encontrado " + BleIds.Mask(r.DeviceId) + " \"" + (r.AdvName ?? "") + "\" rssi="
                                                    + Fmt.Integer(r.Rssi) + " uuids=[" + Uuids(r.ServiceUuids) + "]");
                    index[r.DeviceId] = found.Count;
                    found.Add(r);
                }

                _devices.Emit(found.ToArray()); // hrs:145
            });
            // hrs:147-151 (m-02): un fallo nativo termina la búsqueda en silencio, con la línea en el registro.
            var failures = _adapter.ScanFailed.Subscribe(e =>
            {
                if (generation != _scanGen || done.IsCancellationRequested)
                {
                    return;
                }

                _logger.Log(LogLevel.Warning, Tag, "Error en el escaneo: " + e.Message);
                done.Cancel();
            });
            try
            {
                // DT11-08: sin esperar a On (el legacy no espera); con el Bluetooth claramente apagado, error de búsqueda.
                var adapterState = _adapter.State.Value;
                if (adapterState == BleAdapterState.Off || adapterState == BleAdapterState.TurningOff || adapterState == BleAdapterState.Unauthorized)
                {
                    throw new BleException("scan", "adapter off");
                }

                await _adapter.StartScanAsync(HeartRateOnly);
                if (generation == _scanGen && !_disposed)
                {
                    _logger.Log(LogLevel.Info, Tag, "Escaneando sensores 0x180D durante "
                                                    + Fmt.Integer((long)(timeout ?? _timings.Scan).TotalSeconds) + "s"); // hrs:130
                    try
                    {
                        await _scheduler.Delay(timeout ?? _timings.Scan, done.Token); // hrs:154
                    }
                    catch (OperationCanceledException)
                    {
                        // la terminó StopScan, una acción nueva, el fallo nativo o Dispose.
                    }
                }
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                // hrs:155-157: arrancar falló → desconectado y relanza (la tarjeta muestra el error de búsqueda).
                _logger.Log(LogLevel.Warning, Tag, "Error escaneando: " + error.Message);
                if (generation != _scanGen || _disposed)
                {
                    return;
                }

                SetStatus(HrmStatus.Disconnected);
                throw;
            }
            finally
            {
                results.Dispose();
                failures.Dispose();
                // hrs:160-166: solo si nadie la terminó desde afuera sigue siendo dueña. NO reanuda la reconexión (Q-43).
                if (generation == _scanGen && !_disposed)
                {
                    _scanDone = null;
                    await StopAdapterScanQuietly();
                    if (_status.Value == HrmStatus.Scanning)
                    {
                        SetStatus(HrmStatus.Disconnected);
                    }
                }

                done.Dispose();
            }
        }

        // hrs:170-177: solo si el escaneo es nuestro (el activo podría ser el del remo).
        public async Task StopScanAsync()
        {
            if (_disposed || _status.Value != HrmStatus.Scanning)
            {
                return;
            }

            EndScanWait();
            await StopAdapterScanQuietly();
            if (_status.Value == HrmStatus.Scanning)
            {
                SetStatus(HrmStatus.Disconnected);
            }
        }

        // ── Conexión (hrs:188-319) ──

        // AC-HRM-03-01 a -04, AC-HRM-SEC-01.
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

            // DT11-06: solo un sensor que vino en una búsqueda del pulsómetro o el recordado.
            if (!_known.Contains(deviceId) && deviceId != _rememberedId)
            {
                throw new ArgumentException("HeartRateService: " + BleIds.Mask(deviceId) + " no vino de una búsqueda 0x180D.", nameof(deviceId));
            }

            // hrs:189-192
            if (_status.Value == HrmStatus.Connecting)
            {
                _logger.Log(LogLevel.Info, Tag, "connect ignorado: ya hay una conexión en curso");
                return;
            }

            _connectGen++;
            CancelLoops();
            // QA-G11-02: antes de cualquier await; Status pasa directo de Scanning a Connecting.
            var wasScanning = _status.Value == HrmStatus.Scanning;
            EndScanWait();
            if (wasScanning)
            {
                await StopAdapterScanQuietly(); // hrs:195
            }

            try
            {
                await ConnectToAsync(deviceId, HrmConnectOrigin.Manual);
            }
            catch (HrmSupersededException)
            {
                // hrs:199-202: el propio usuario la reemplazó (otro connect, búsqueda, desconectar...).
                _logger.Log(LogLevel.Info, Tag, "Conexión manual reemplazada por otra acción");
            }
        }

        // hrs:207-215 (AC-HRM-07-01, AC-APP-01-10).
        public Task AutoConnectAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            LoadRemembered();
            if (_rememberedId == null || _status.Value != HrmStatus.Disconnected || _scanDone != null)
            {
                return Task.CompletedTask; // :209-211: una búsqueda del usuario tiene prioridad
            }

            _dropLoop.Cancel();
            _startupLoop.Start();
            ReemitStatus(); // isRetrying cambió
            return Task.CompletedTask;
        }

        // hrs:337-349 (AC-HRM-10-01): corta reintentos, desconecta y olvida el sensor.
        public async Task DisconnectAsync()
        {
            if (_disposed)
            {
                return;
            }

            _connectGen++;
            CancelLoops();
            if (_status.Value == HrmStatus.Scanning)
            {
                EndScanWait();
                await StopAdapterScanQuietly();
            }

            _isDisconnecting = true;
            var attempt = Cleanup();
            if (attempt != null)
            {
                // Sin cola: una conexión pendiente se cancela ya (no espera los 15 s).
                await CloseQuietlyAsync(attempt);
            }

            _isDisconnecting = false; // m-10
            await ForgetAsync();
            if (!_disposed)
            {
                SetStatus(HrmStatus.Disconnected);
            }
        }

        // hrs:353-363 (AC-HRM-10-02): olvida el sensor y corta reintentos, sin tocar una conexión activa (un intento en curso se aborta).
        public async Task ForgetAsync()
        {
            if (_disposed)
            {
                return;
            }

            _connectGen++;
            CancelLoops();
            if (_status.Value == HrmStatus.Scanning)
            {
                // Valor por defecto (02-tech-design §12): cortan la búsqueda en vez de dejarla huérfana.
                EndScanWait();
                await StopAdapterScanQuietly();
                if (_status.Value == HrmStatus.Scanning)
                {
                    SetStatus(HrmStatus.Disconnected);
                }
            }

            AbortPendingConnect();
            _rememberedId = null;
            _rememberedName = null;
            _preferences.Remove(PrefDeviceId);
            _preferences.Remove(PrefDeviceName);
            ReemitStatus();
        }

        // hrs:435-447 (AC-HRM-11-03): nada queda vivo (temporizadores, suscripciones, sesiones).
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _connectGen++;
            CancelLoops();
            AbortPendingConnect();
            var wasScanning = _status.Value == HrmStatus.Scanning;
            _disposed = true;
            EndScanWait();
            if (wasScanning)
            {
                StopAdapterScanQuietly().Forget(_logger, Tag);
            }

            _watchdog.Dispose();
            _startupLoop.Dispose();
            _dropLoop.Dispose();
            var attempt = Detach();
            if (attempt != null)
            {
                Close(attempt);
            }

            _adapterSubscription.Dispose();
        }

        // ── Conexión ──

        // hrs:217-227: los fallos solo van al registro (AC-HRM-07-02).
        private async Task<bool> TryRememberedAsync(HrmConnectOrigin origin)
        {
            var id = _rememberedId;
            if (id == null)
            {
                return true; // nada que reintentar
            }

            try
            {
                await ConnectToAsync(id, origin);
                return true;
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Info, Tag, "Reintento fallido: " + error.Message);
                return false;
            }
        }

        // hrs:229-306 (AC-HRM-03-01 a -03, AC-HRM-06-03, AC-HRM-11-01, -02).
        private async Task ConnectToAsync(string id, HrmConnectOrigin origin)
        {
            var generation = _connectGen;
            _isDisconnecting = false;
            var attempt = new HrmAttempt(id);
            _attempt = attempt;
            SetStatus(HrmStatus.Connecting); // hrs:237
            try
            {
                // hrs:241-246: conexión directa sin MTU con tope de 15 s (DT10-17: el plazo cancela el connect del adaptador).
                var timedOut = false;
                var deadline = _scheduler.Schedule(_timings.Connect, () =>
                {
                    timedOut = true;
                    attempt.Cancel();
                });
                IBlePeripheral peripheral;
                try
                {
                    peripheral = await _adapter.ConnectAsync(id, false, attempt.Token);
                }
                catch (OperationCanceledException) when (timedOut)
                {
                    throw new BleException("connect", "timed out after " + Fmt.Integer((long)_timings.Connect.TotalSeconds) + " s");
                }
                finally
                {
                    deadline.Dispose(); // m-05: conectado o no, el plazo ya no queda pendiente
                }

                attempt.Peripheral = peripheral;
                EnsureCurrent(generation);

                // hrs:248-253, :308-319
                var services = await peripheral.DiscoverServicesAsync();
                EnsureCurrent(generation);
                if (!HasMeasurement(services))
                {
                    // hrs:250-252: el catch de abajo cierra el enlace en silencio (el legacy lo cerraba dos veces).
                    throw new HrmIncompatibleException();
                }

                // hrs:254-258
                await peripheral.SetNotifyAsync(HeartRateParser.Service, HeartRateParser.Measurement, true);
                EnsureCurrent(generation);
                attempt.NotifySubscription = peripheral.Notifications.Subscribe(_onNotification);

                // hrs:259 (DT11-05)
                Remember(id, peripheral.Name);

                // hrs:262-272: la vigilancia de caída DESPUÉS de conectar.
                attempt.LinkLostSubscription = peripheral.LinkLost.Subscribe(e => OnLinkLost(attempt, e));

                // hrs:274-276
                _isStale = false;
                _watchdog.Touch();
                _connectedBy = origin;
                PublishLink(); // antes del estado: quien escucha Connected ya ve ConnectedBy
                SetStatus(HrmStatus.Connected);
                _logger.Log(LogLevel.Info, Tag, "Conectado a " + (ConnectedDeviceName is { Length: > 0 } name ? name : BleIds.Mask(id)));
            }
            catch (Exception error)
            {
                if (error is HrmSupersededException || generation != _connectGen)
                {
                    // hrs:280-293: otra acción es dueña del estado; solo soltar lo propio y cerrarlo en silencio, salvo que el nuevo
                    // dueño apunte al mismo sensor (DT11-07: no cortarle el intento).
                    _logger.Log(LogLevel.Info, Tag, "Intento de conexión reemplazado: " + BleIds.Mask(id));
                    attempt.ReleaseSubscriptions();
                    var owner = _attempt;
                    if (ReferenceEquals(owner, attempt))
                    {
                        _attempt = null;
                    }

                    if (owner == null || ReferenceEquals(owner, attempt) || owner.Id != attempt.Id)
                    {
                        Close(attempt);
                    }

                    throw new HrmSupersededException();
                }

                // hrs:294-304: el enlace pudo quedar abierto (falló discover o notify): soltarlo para no dejar un GATT colgado.
                _logger.Log(LogLevel.Warning, Tag, "Error al conectar: " + error.Message);
                await CloseQuietlyAsync(attempt);
                if (generation != _connectGen)
                {
                    throw new HrmSupersededException(); // :299-301: mientras cerrábamos arrancó otra acción
                }

                Cleanup();
                SetStatus(HrmStatus.Disconnected);
                throw;
            }
        }

        private void EnsureCurrent(int generation)
        {
            if (generation != _connectGen || _disposed)
            {
                throw new HrmSupersededException();
            }
        }

        private static bool HasMeasurement(IReadOnlyList<BleGattService> services)
        {
            for (var s = 0; s < services.Count; s++)
            {
                if (services[s].Uuid != HeartRateParser.Service)
                {
                    continue;
                }

                var characteristics = services[s].Characteristics;
                for (var c = 0; c < characteristics.Count; c++)
                {
                    if (characteristics[c].Uuid == HeartRateParser.Measurement)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // hrs:321-326 (AC-HRM-04-06): cada lectura válida cuenta (sin contacto → 0); la inválida se descarta sin rearmar el watchdog.
        // Sin reservar memoria: TryParse sobre el arreglo; el enlace solo se rearma al cambiar IsStale.
        private void OnNotification(BleNotification notification)
        {
            if (notification.Characteristic != HeartRateParser.Measurement)
            {
                return;
            }

            if (!HeartRateParser.TryParse(notification.Value, out var bpm, out var contact))
            {
                return;
            }

            _watchdog.Touch();
            if (_isStale)
            {
                _isStale = false;
                PublishLink();
            }

            EmitBpm(contact == false ? 0 : bpm);
        }

        // hrs:86-89 (AC-HRM-05-02): bpm 0 una vez; la conexión no se corta ni se reintenta.
        private void OnStale()
        {
            if (_disposed || _status.Value != HrmStatus.Connected)
            {
                return;
            }

            _isStale = true;
            PublishLink();
            EmitBpm(0);
        }

        private void EmitBpm(int value)
        {
            if (_disposed)
            {
                return;
            }

            _currentBpm = value;
            _bpm.Emit(value);
        }

        // ── Caída (hrs:263-272) ──

        private void OnLinkLost(HrmAttempt attempt, BleException error)
        {
            if (!ReferenceEquals(_attempt, attempt) || _isDisconnecting || _disposed || _status.Value != HrmStatus.Connected)
            {
                return;
            }

            _logger.Log(LogLevel.Info, Tag, "Enlace caído: " + error.Message);
            HandleDrop();
        }

        // DT11-09: apagar el Bluetooth con el sensor conectado es una caída (no espera el aviso nativo).
        private void OnAdapterState(BleAdapterState state)
        {
            if (_disposed || _isDisconnecting || _status.Value != HrmStatus.Connected)
            {
                return;
            }

            if (state == BleAdapterState.Off || state == BleAdapterState.TurningOff || state == BleAdapterState.Unauthorized)
            {
                _logger.Log(LogLevel.Info, Tag, "Bluetooth no disponible (" + state + ")");
                HandleDrop();
            }
        }

        // AC-HRM-08-01: primero el pulso a 0, después desconectado, y la cadencia de 3 s sin límite.
        private void HandleDrop()
        {
            _logger.Log(LogLevel.Info, Tag, "Conexión perdida, reintentando cada " + Fmt.Integer((long)_timings.DropInterval.TotalSeconds) + "s");
            var attempt = Cleanup();
            if (attempt != null)
            {
                Close(attempt);
            }

            SetStatus(HrmStatus.Disconnected);
            _dropLoop.Start();
        }

        // ── Desconexión y limpieza ──

        private void CancelLoops()
        {
            _startupLoop.Cancel();
            _dropLoop.Cancel();
        }

        // hrs:372-383: si hay un intento en vuelo (ya reemplazado por _connectGen), lo cierra y deja el estado en desconectado.
        private void AbortPendingConnect()
        {
            if (_status.Value != HrmStatus.Connecting)
            {
                return;
            }

            var pending = Detach();
            _watchdog.Stop();
            SetStatus(HrmStatus.Disconnected);
            if (pending != null)
            {
                Close(pending);
            }
        }

        // hrs:393-401: suelta suscripciones y watchdog; el pulso a 0 si no lo era. Devuelve el intento soltado (lo cierra quien llama).
        private HrmAttempt? Cleanup()
        {
            var attempt = Detach();
            _watchdog.Stop();
            var linkChanged = _isStale || _connectedBy != HrmConnectOrigin.None;
            _isStale = false;
            _connectedBy = HrmConnectOrigin.None;
            if (linkChanged)
            {
                PublishLink();
            }

            if (_currentBpm != 0)
            {
                EmitBpm(0);
            }

            return attempt;
        }

        private HrmAttempt? Detach()
        {
            var attempt = _attempt;
            _attempt = null;
            attempt?.ReleaseSubscriptions();
            return attempt;
        }

        // Cierra lo del intento sin esperar: el periférico (cierra la sesión nativa) o, si todavía no conectó, su connect pendiente.
        private static void Close(HrmAttempt attempt)
        {
            var peripheral = attempt.Peripheral;
            if (peripheral != null)
            {
                peripheral.Dispose();
            }
            else
            {
                attempt.Cancel();
            }
        }

        // hrs:385-391 (_quietDisconnect)
        private async Task CloseQuietlyAsync(HrmAttempt attempt)
        {
            var peripheral = attempt.Peripheral;
            if (peripheral == null)
            {
                attempt.Cancel();
                return;
            }

            await DisconnectQuietlyAsync(peripheral);
        }

        private async Task DisconnectQuietlyAsync(IBlePeripheral peripheral)
        {
            try
            {
                await peripheral.DisconnectAsync();
            }
            catch (Exception error)
            {
                _logger.Log(LogLevel.Warning, Tag, "Error al desconectar: " + error.Message);
            }
            finally
            {
                peripheral.Dispose();
            }
        }

        // QA-G11-02: la generación sube y _scanDone se suelta ANTES de cancelar la espera; el finally de la búsqueda vieja (que con
        // FakeScheduler corre dentro de Cancel) ve otra generación y no toca nada: lo que haga falta lo hace quien llamó.
        private void EndScanWait()
        {
            _scanGen++;
            var done = _scanDone;
            _scanDone = null;
            if (done == null)
            {
                return;
            }

            try
            {
                done.Cancel();
            }
            catch (ObjectDisposedException)
            {
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
                _logger.Log(LogLevel.Warning, Tag, "Error al detener el escaneo: " + error.Message);
            }
        }

        // ── Memoria (hrs:405-421) ──

        // DT11-05: nombre = el del sistema, o el anunciado en esta búsqueda, o ninguno. Otro id sin nombre borra el nombre guardado
        // (Q-41); el mismo id sin nombre (reconectar sin buscar) lo conserva.
        private void Remember(string id, string? systemName)
        {
            string? name = !string.IsNullOrEmpty(systemName) ? systemName : null;
            if (name == null && _advNames.TryGetValue(id, out var advertised) && !string.IsNullOrEmpty(advertised))
            {
                name = advertised;
            }

            var sameId = _preferences.TryGetString(PrefDeviceId, out var storedId) && storedId == id;
            _rememberedId = id;
            _preferences.SetString(PrefDeviceId, id);
            if (name != null)
            {
                _rememberedName = name;
                _preferences.SetString(PrefDeviceName, name);
            }
            else if (sameId)
            {
                if (_rememberedName == null && _preferences.TryGetString(PrefDeviceName, out var stored) && stored.Length > 0)
                {
                    _rememberedName = stored;
                }
            }
            else
            {
                _rememberedName = null;
                _preferences.Remove(PrefDeviceName);
            }
        }

        // hrs:414-421: sin id no hay sensor (un nombre huérfano no se hereda).
        private void LoadRemembered()
        {
            _rememberedId = _preferences.TryGetString(PrefDeviceId, out var id) && id.Length > 0 ? id : null;
            _rememberedName = _rememberedId != null && _preferences.TryGetString(PrefDeviceName, out var name) && name.Length > 0 ? name : null;
        }

        // ── Estado ──

        // hrs:425-428: el legacy agrega al stream aunque repita el estado.
        private void SetStatus(HrmStatus status)
        {
            if (_status.Value == status)
            {
                _status.ForceNotify();
            }
            else
            {
                _status.Value = status;
            }
        }

        // hrs:431-433: para que la UI relea IsRetrying y RememberedDeviceName.
        private void ReemitStatus()
        {
            if (!_disposed)
            {
                _status.ForceNotify();
            }
        }

        private void PublishLink()
        {
            if (_disposed)
            {
                return;
            }

            ReconnectLoop? loop = _startupLoop.IsRunning ? _startupLoop : _dropLoop.IsRunning ? _dropLoop : null;
            HrmLink link;
            if (loop == null)
            {
                link = new HrmLink(ReconnectPhase.None, HrmRetryReason.None, 0, null, TimeSpan.Zero, _isStale, _connectedBy);
            }
            else
            {
                var waiting = !loop.IsAttempting && loop.NextAttemptAt.HasValue;
                link = new HrmLink(waiting ? ReconnectPhase.Waiting : ReconnectPhase.Attempting,
                    ReferenceEquals(loop, _startupLoop) ? HrmRetryReason.Startup : HrmRetryReason.Drop,
                    loop.Run, waiting ? loop.NextAttemptAt : null, loop.Interval, _isStale, _connectedBy);
            }

            _link.Value = link;
        }

        private static string Uuids(IReadOnlyList<BleUuid>? uuids)
        {
            if (uuids == null || uuids.Count == 0)
            {
                return "";
            }

            var parts = new string[uuids.Count];
            for (var i = 0; i < uuids.Count; i++)
            {
                parts[i] = uuids[i].ToShortString();
            }

            return string.Join(", ", parts);
        }

        // El intento de conexión vigente (= _device del legacy) con lo suyo: el connect cancelable, el periférico y las suscripciones.
        private sealed class HrmAttempt
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();

            public HrmAttempt(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public CancellationToken Token => _cts.Token;

            public IBlePeripheral? Peripheral { get; set; }

            public IDisposable? NotifySubscription { get; set; }

            public IDisposable? LinkLostSubscription { get; set; }

            public void Cancel()
            {
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
            }

            public void ReleaseSubscriptions()
            {
                NotifySubscription?.Dispose();
                NotifySubscription = null;
                LinkLostSubscription?.Dispose();
                LinkLostSubscription = null;
            }
        }

        // hrs:13-17: un intento de conexión fue reemplazado por otra acción (interno; nunca llega a la tarjeta).
        private sealed class HrmSupersededException : Exception
        {
            public HrmSupersededException()
                : base("Intento de conexión reemplazado")
            {
            }
        }
    }
}
