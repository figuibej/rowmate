using System;
using System.Globalization;
using System.IO;
using RowMate.App.Composition;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Ports;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Threading;
using RowMate.Platform;
using RowMate.Platform.Lifecycle;
using RowMate.Platform.Logging;
using RowMate.Platform.Time;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;
using RowMate.Presentation.Workout;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using AppContext = RowMate.App.Composition.AppContext;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using RowMate.Infrastructure.Diagnostics;
#endif

namespace RowMate.Bootstrap
{
    // Composition root (ARCHITECTURE §4.1, ADR 003; 02-tech-design §1.4 de la It. 2; 03-rutinas/02-tech-design §3.7). Único
    // MonoBehaviour de Boot.unity.
    // Orden (QA-G2-01, medido en 6000.3.27f1): UIDocument tiene [DefaultExecutionOrder(-100)]; este componente corre antes
    // (−10000), así que en su Awake y su OnEnable rootVisualElement todavía es null. Awake configura (y le da al documento la
    // copia del panel, armada por RuntimePanelFactory con sus respaldos de fuente, antes del OnEnable del documento); Start
    // arma la UI, cuando la raíz ya existe.
    // It. 4a (02-tech-design §3.5, §4.2, §4.4): la base local es un LocalStore (rutinas y sesiones sobre la misma conexión y
    // el mismo ejecutor); el ciclo de vida (AppLifecycle) lo mueven OnApplicationPause y OnApplicationFocus; la pantalla
    // siempre encendida (ScreenKeepAwake) se pide en Awake, antes de la primera pantalla (AC-APP-14-01), y se libera al cerrar
    // (AC-APP-14-03; termina DV-08 de la It. 2). Ningún estático salvo BuildFlags.
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppRoot : MonoBehaviour
    {
        public const string Tag = "APP";

        // Diagnósticos por línea de comandos (QA-V2-09, decisión del PO al cerrar la It. 2): solo en el Editor y en los builds
        // de desarrollo. En un build normal ni se leen ni quedan sus nombres en el ensamblado (BuildOutputInspector los busca).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public const string PipelineArgument = "-rowmate-ui-pipeline";
        public const string ColorProbeArgument = "-rowmate-color-probe";
        public const string DbSmokeArgument = "-rowmate-db-smoke";
        public const string DbRoundTripArgument = "-rowmate-db-roundtrip";

        // It. 10a (02-tech-design §5.3; AC-BLE-04-07, -05-10, -91-02): el humo del Bluetooth real (build.ps1 -BleSmoke): 3 búsquedas de
        // 10 s sin conectar, la segunda con la ventana minimizada; solo contadores en el registro.
        public const string BleSmokeArgument = BleSmoke.Argument;
        public const string RoundTripName = "Ida y vuelta";

        // It. 5 (QA-V4b-06; 05-series-parciales/00-deuda-4b §2): la medida de la entrada al inmersivo en el build. Sin
        // precalentado del HUD (para comparar en el mismo build) y N entradas automáticas (libre y rutina alternados, 12 s cada
        // una) con FrameTimeProbe, solo con la carpeta de datos de desarrollo (ROWMATE_DATA_DIR): nunca en la base del usuario.
        public const string NoHudWarmupArgument = "-rowmate-no-hud-warmup";
        public const string PerfEntriesArgument = "-rowmate-perf-entries";
        public const string PerfRoutineName = "Medición de la entrada";

        // It. 5 (05-series-parciales/02-tech-design §3.7; AC-WKT-18-21, AC-WKT-13-15): las rutinas de las entradas (por nombre,
        // separadas por ";": la primera entrada es libre y las siguientes, estas en orden) y la medida de los paneles de series
        // (Fondo 3000 y después Series 600 con el simulador, abriendo y cerrando los paneles cada 2 s por la API de las
        // preferencias; FrameTimeProbe escribe "[PERF] paneles: …"). Las rutinas las siembra SnapshotCli.SeedDatabase -seed F5.
        public const string PerfRoutinesArgument = "-rowmate-perf-routines";
        public const string PerfSeriesArgument = "-rowmate-perf-series";

        // It. 6a (06a-escena-lago/02-tech-design §4.5; AC-SCN-01-10, AC-WKT-13-15, AC-WKT-14-19): la medida de la escena en el build de
        // desarrollo, por tiempo. -rowmate-perf-scene <s>: libre con Fuerte; -rowmate-scene-delay <s>: la escena demorada (sin precarga);
        // -rowmate-quality <nivel>: el nivel forzado (con medium, la línea de base de GPU para la It. 14); -rowmate-perf-hours: la
        // secuencia de VID-05; -rowmate-perf-completion: Corta con el diálogo y la toma aérea.
        public const string PerfSceneArgument = "-rowmate-perf-scene";
        public const string SceneDelayArgument = "-rowmate-scene-delay";
        public const string QualityArgument = "-rowmate-quality";
        public const string PerfHoursArgument = "-rowmate-perf-hours";
        public const string PerfCompletionArgument = "-rowmate-perf-completion";

        // It. 8 (08-entornos-selector/02-tech-design §5; AC-SCN-01-19, DA8-16): con -rowmate-perf-scene, el escenario (lake, river, coast,
        // regatta) y la hora forzada (Hora escena). Con un escenario que no es el lago: el lago listo en reposo, se elige el escenario y la
        // entrada va 1 s después (el cambio de kit en reposo; la escena puede quedar demorada).
        public const string PerfEnvArgument = "-rowmate-perf-env";
        public const string PerfHourArgument = "-rowmate-perf-hour";

        // It. 6a, ronda 1 de la verificación (QA-V6a-13, QA-V6a-REN-03; AC-WKT-14-19): -rowmate-perf-long-series <s>: "Serie larga" (la
        // siembra SnapshotCli.SeedDatabase -seed F4B) con la escena durante <s> s, con sus cambios de paso cada 30 s; FrameTimeProbe
        // escribe sus ventanas de 10 s (los últimos 5 s de cada paso caen adentro de una).
        public const string PerfLongSeriesArgument = "-rowmate-perf-long-series";

        // It. 6a, ronda 2 de la verificación (QA-V6a-R2-01): -rowmate-perf-profile <archivo.raw> con -rowmate-perf-long-series: el
        // Profiler (registro binario con la pila de cada asignación) de los 25,5 a los 27,5 s de Serie larga (el halo y la cuenta grande
        // de los últimos 5 s del primer paso). El archivo lo lee el Editor (RowMate.EditorTools.ProfileAllocations).
        public const string PerfProfileArgument = "-rowmate-perf-profile";

        // It. 12 (12-exportar-tcx/02-tech-design DT12-20; AC-STR-09-26): -rowmate-export-smoke <n>: exporta la sesión más nueva n veces
        // sin UI (con el exportador de la app: en desarrollo, ROWMATE_DATA_DIR\exports) y registra los nombres; sale con 0 o 1. Solo con
        // la carpeta de datos de desarrollo: nunca Documentos.
        public const string ExportSmokeArgument = "-rowmate-export-smoke";

        // It. 14 (14-pulido-plataformas/02-tech-design DT14-12; AC-APP-16-10): -rowmate-boot-splash off: sin la capa de arranque (para
        // medir la diferencia en el MISMO build); -rowmate-perf-boot: escribe "[PERF] arranque: primer cuadro del shell {ms}" (desde que
        // arrancó el proceso hasta "shell listo") y sale con 0 un segundo después. Los dos solo en desarrollo (build.ps1 -BootSmoke).
        public const string BootSplashArgument = "-rowmate-boot-splash";
        public const string PerfBootArgument = "-rowmate-perf-boot";

        // It. 15 (15-deuda-pc/02-tech-design §2.1, DT15-19; AC-D15-03): -rowmate-perf-history: con F-9P sembrada en ROWMATE_DATA_DIR, abre
        // el Historial, recarga 5 veces, abre el Detalle n.º 1 y desplaza la lista 10 s; escribe "[PERF] history: …" y sale con 0.
        public const string PerfHistoryArgument = "-rowmate-perf-history";
#endif

        [SerializeField] private UiAssets? _assets;

        private IAppLogger? _logger;
        private UnityScheduler? _scheduler;
        private PlatformServices? _platform;
        private AppLifecycle? _lifecycle;
        private ScreenKeepAwake? _keepAwake;
        private DeviceBackend? _backend;
        // It. 10a (§5.2): lo que llega de otros hilos (permisos de Android) y la bomba de eventos del plugin BLE (null = sin plugin).
        private RowMate.Core.Common.MainThreadQueue? _mainThread;
        private RowMate.Core.Ble.IBleEventPump? _blePump;
        private LocalStore? _store;
        private AppContext? _app;
        private ShellHandle? _shell;
        private UiSurface? _surface;
        private UIDocument? _document;
        private FramePacer? _pacer;
        private HudWarmup? _warmup;
        private RowMate.Infrastructure.Preferences.JsonFilePreferences? _preferences;
        private BackSource _backSource;
        private SceneBridge? _sceneBridge;
        private RowMate.Platform.Display.ThermalAdapter? _thermal;
        private RowMate.Core.Scene.IRenderAssetQuality? _assetQuality;
        // It. 14 (DT14-13 a DT14-16): la ventana de Windows (mínimo, Alt+Enter sin pitido) y F11 / Alt+Enter; null fuera del player de Windows.
        private RowMate.Platform.Display.WindowsWindowFrame? _windowFrame;
        private RowMate.Platform.Display.WindowModeController? _windowMode;
        // It. 15 (#74, AC-D15-19): la ventana del player para guardarla al cerrar y restaurarla al abrir.
        private RowMate.Platform.Display.PlayerWindowFrame? _playerFrame;
        private bool _windowSaved;
        // It. 14 (DT14-17 a DT14-22): el puente del lector de pantalla (solo trabaja con el lector encendido).
        private RowMate.Presentation.Accessibility.ScreenReaderBridge? _screenReader;

        public UiAssets? Assets => _assets;

        public UiSurface? Surface => _surface;

        public ShellHandle? Shell => _shell;

        public AppContext? App => _app;

        // El ciclo de vida como lo ve el resto de la app (Core.Ports): lo lee BootSceneTests sin depender de Platform.
        public IAppLifecycle? Lifecycle => _lifecycle;

        public ScreenKeepAwake? KeepAwake => _keepAwake;

        // La base local de la app (rutinas y sesiones en un archivo, It. 4a; BootSceneTests).
        public LocalStore? Store => _store;

        // It. 5 (QA-V4b-06): el HUD ya se precalentó y se desarmó (BootSceneTests).
        public bool HudWarmedUp { get; private set; }

        // Pasos 1 a 6 (sin tocar rootVisualElement).
        private void Awake()
        {
            // 1. Ningún texto depende de la cultura del equipo (F-APP-12, ADR 012).
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            // 2. Registro: primera línea propia (P-06: si Windows no muestra la versión en el ejecutable, queda acá).
            var logger = new UnityAppLogger();
            _logger = logger;
            logger.Log(LogLevel.Info, Tag, AppIdentity.ProductName + " " + AppIdentity.Version + " (" + AppIdentity.BuildNumber.ToString(CultureInfo.InvariantCulture)
                + ") · " + Application.platform + " · simulador=" + (BuildFlags.IsSimulator ? "sí" : "no"));

            if (_assets == null || !_assets.IsComplete)
            {
                logger.Log(LogLevel.Error, Tag, "AppRoot sin UiAssets completo (correr RowMate/Bootstrap/Reaplicar)");
                enabled = false;
                return;
            }

            // 3. Reloj y sistema (idioma, accesibilidad, densidad, "Atrás", base, portapapeles y teclado).
            // It. 4b (P-17; 04b-hud-exr/02-tech-design §2.9): el programador con la fuente de sueño de la plataforma (el reloj de
            // la app es el despierto) y el offset de la zona sin caché.
            _platform = PlatformFactory.Create(logger);
            _scheduler = new UnityScheduler(logger, _platform.SleepSource);
            _backSource = Application.platform == RuntimePlatform.Android ? BackSource.SystemBack : BackSource.Escape;

            // Barra de título de Windows en el tema del sistema desde el primer cuadro (legacy win32_window.cpp:147, QA-V2-08);
            // al recuperar el foco la vuelve a aplicar PlatformServices.Refresh.
            _platform.TitleBar.Apply();

            // 3a. It. 14 (AC-APP-15-05 a -07, -09): la ventana inicial, el mínimo, F11 / Alt+Enter y las orientaciones de iPhone.
            SetUpWindow(logger);

            // 3b. Ciclo de vida y pantalla siempre encendida (It. 4a §4.2, §4.4): el primer pedido, antes de armar la UI
            //     (AC-APP-14-01: desde el arranque, en toda la app; lib/main.dart:42). ScreenKeepAwake vuelve a pedir al volver
            //     del segundo plano y al recuperar el foco (AC-APP-14-02); un rechazo solo va al registro (AC-APP-14-04).
            _lifecycle = new AppLifecycle(logger);
            var lifecycle = _lifecycle;
            _scheduler.Woke += wake => lifecycle.SignalWake(wake);
            _keepAwake = new ScreenKeepAwake(_platform.WakeLock, _lifecycle, logger);
            _keepAwake.Start();

            // 4. Remo y pulsómetro: el único #if ROWMATE_SIMULATOR (DeviceBackendFactory). It. 10a: en el build normal, el remo real sobre
            //    el plugin nativo y su bomba de eventos (que Update drena antes del Tick).
            //    It. 11 (02-tech-design §5; R-07): las preferencias (paso 4c) se leen ANTES, porque el pulsómetro real recuerda su sensor en
            //    ellas (hrm.deviceId / hrm.deviceName) y AppContext.Start lo reconecta al abrir. Ninguna ruta va al registro.
            _preferences = new RowMate.Infrastructure.Preferences.JsonFilePreferences(DataLocation.PreferencesFile(_platform), logger);
            PlaceWindow(logger);
            _mainThread = new RowMate.Core.Common.MainThreadQueue(logger);
            _backend = DeviceBackendFactory.Create(_scheduler, _lifecycle, logger, _mainThread, _preferences, out _blePump);

            // 4b. Base local (F-HIS-17, F-HIS-20): una por variante (rower_app.db o rower_app_dev.db), en la carpeta privada de
            //     la app (03-rutinas/02-tech-design §2.2, §2.4). El ÚNICO lugar del código que elige el archivo. Se abre en el
            //     primer uso (la carga del arranque), en el hilo de la base (SerialExecutor); ninguna ruta va al registro. Desde
            //     la It. 4a es un LocalStore: rutinas y sesiones sobre la misma conexión y el mismo ejecutor (§3.5).
            var file = DatabaseFiles.NameFor(BuildFlags.IsSimulator);
            var dbPath = Path.Combine(DataLocation.DatabaseDirectory(_platform), file);
            _store = new LocalStore(new RowMateDatabase(dbPath, new SerialExecutor("RowMate.Db", logger), logger), logger);
            logger.Log(LogLevel.Info, Tag, "base " + file + (DataLocation.IsOverridden ? " (carpeta de desarrollo)" : ""));

            // 4c. It. 5 (05-series-parciales/02-tech-design §2.7, §2.8; ADR 021): las preferencias (prefs.json, en la carpeta de la
            //     base; en desarrollo, ROWMATE_DATA_DIR), leídas en el arranque (desde la It. 11, en el paso 4, antes del backend); se
            //     guardan fuera del hilo principal. Ninguna ruta va al registro.

            // 5. La app pura. Perfil apagado (su regla real es de la It. 12, D-20).
            var texts = LocalizationSet.Load(_assets.StringsEs!.text, _assets.StringsEn!.text);
            //    It. 12 (12-exportar-tcx/02-tech-design §3.5; DT12-03, DT12-10, DT12-16): el exportador de la plataforma (Windows:
            //    Documentos\RowMate\TCX, o ROWMATE_DATA_DIR\exports en desarrollo; Android: la caché y la hoja de compartir), la zona del
            //    equipo releída en cada exportación y la marca del simulador. Ninguna ruta va al registro.
            var exporter = ExportLocations.CreateExporter(_platform.Kind, ExportLocations.ForApp(_platform.Kind), logger);
            _app = AppComposer.Compose(new AppServices(_backend, _store.Routines, _store.Sessions, _scheduler, logger, texts, _platform.Locale,
                _platform.Accessibility, new ShellOptions(false))
            {
                Lifecycle = _lifecycle, SystemUi = _platform.SystemUi, Preferences = _preferences, Haptics = RowMate.Platform.Haptics.HapticsFactory.Create(logger),
                Exporter = exporter, ExportZone = ExportZoneNow, SimulatorBuild = BuildFlags.IsSimulator, AppVersion = Application.version,
            });

            // 6. Superficie: copia del panel con sus respaldos de fuente (RuntimePanelFactory: el asset no se toca, R2-05;
            //    QA-G3-12), RT limpia a bg.app y presentador (S-B1). El documento de la escena no tiene panel: se engancha a la
            //    copia en su OnEnable, que corre después.
            var pipeline = PipelineFromCommandLine(UiColorPipeline.GammaRt);
            var panel = RuntimePanelFactory.Create(_assets.Panel!, _assets.IconsOutlineFallback, _assets.EmojiFallback, _assets.Panel!.name + " (runtime)");
            IUiPresenter? presenter = pipeline == UiColorPipeline.GammaRt ? new OverlayCopyPresenter(panel.themeStyleSheet) : null;
            var display = _platform.Display;
            _surface = new UiSurface(panel, display, pipeline, presenter, display.Refresh);
            _document = GetComponent<UIDocument>();
            _document.panelSettings = _surface.Panel;
        }

        // Pasos 7 y 8: la UI y el arranque.
        private void Start()
        {
            if (_app == null || _document == null || _platform == null || _scheduler == null || _backend == null)
            {
                return;
            }

            var root = _document.rootVisualElement;
            if (root == null)
            {
                _logger?.Log(LogLevel.Error, Tag, "UIDocument sin raíz en Start");
                enabled = false;
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (HasArgument(ColorProbeArgument))
            {
                var probe = gameObject.AddComponent<ColorProbe>();
                probe.Run(root, _surface!, ArgumentValue(ColorProbeArgument) ?? "rowmate-color-probe.json", _logger!);
                return;
            }

            // Humo de la base en un player (R3-01): crea, guarda, relee y borra en una subcarpeta temporal de la carpeta dada;
            // escribe "[DB] humo: ok" o "[DB] humo: falla (…)" SIN rutas (QA-G3-20) y sale (0 bien, 1 falla).
            var smokeFolder = ArgumentValue(DbSmokeArgument);
            if (smokeFolder != null)
            {
                _ = RunDatabaseSmoke(smokeFolder);
                return;
            }

            // Ida y vuelta por la base de la app (It. 3, R3-01): con la composición real (RoutinesStore → repositorio →
            // SerialExecutor → SQLite nativo), en la carpeta de siempre (en desarrollo, ROWMATE_DATA_DIR). Primera corrida:
            // guarda "Ida y vuelta" y sale; la segunda la encuentra después de reabrir el proceso y sale con 0.
            if (HasArgument(DbRoundTripArgument))
            {
                _ = RunDatabaseRoundTrip();
                return;
            }
#endif

            // 6b. It. 6a (06a-escena-lago/02-tech-design §3.1 a §3.4, §4.2): la escena 3D del inmersivo (SceneHost con su puente), si
            //     SceneWorld.asset está completo; si no, el inmersivo sigue con el fondo provisorio (un aviso).
            _sceneBridge = CreateScene();

            // 7. La UI (ShellHost). En un build normal SimulatorLayerFactory devuelve null (sin capa).
            _shell = ShellHost.Build(_app, root, new ShellHostOptions(_scheduler, _platform.IsDesktop, _platform.BackButton, _platform.Display,
                SimulatorLayerFactory.CreateOrNull(_backend, _app, _scheduler), _platform.Clipboard, _platform.Keyboard)
            {
                Pipeline = _surface!.Pipeline, Scene = _sceneBridge, Thumbnails = _assets!.Thumbnails,
                // It. 14 (DT14-10, AC-APP-16-10): la capa de arranque desde el primer cuadro.
                BootSplash = BootSplashEnabled(), BootMark = _assets.BootMark,
            });
            _pacer = new FramePacer(_shell.Pacing);
            // It. 14 (DT14-17): el lector de pantalla (con el lector apagado, Tick solo lee un bool).
            _screenReader = new RowMate.Presentation.Accessibility.ScreenReaderBridge(_shell.Shell, new RowMate.Presentation.Accessibility.UnityA11yAnnouncer());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (HasArgument(PerfBootArgument))
            {
                WatchBootForPerf();
            }
#endif

            // 8. El remo empieza a escuchar (el simulado se conecta), las rutinas se cargan sin esperar (AC-APP-01-09) y el
            //    pulsómetro intenta su reconexión (main.dart:68-83). Corre antes del primer Update: el scheduler no avanza antes
            //    de que la UI esté suscripta.
            _app.Start();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (HasArgument(BleSmokeArgument))
            {
                _ = BleSmoke.RunAsync(_backend.Rower, _backend.HeartRate, _scheduler, _logger!);
            }

            var exportSmoke = ArgumentValue(ExportSmokeArgument);
            if (exportSmoke != null)
            {
                _ = RunExportSmoke(exportSmoke);
            }
#endif

            // 9. It. 5 (QA-V4b-06; 05-series-parciales/00-deuda-4b §2): la primera vez del HUD (el JIT, los estilos y el texto) en
            //    este cuadro, el del arranque, y no en el del toque que abre el primer inmersivo. Lo desarma Update en el cuadro
            //    siguiente; no se ve ni toca el motor.
            var warmUp = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            warmUp = !HasArgument(NoHudWarmupArgument);
            var perfEntries = ArgumentValue(PerfEntriesArgument);
            if (perfEntries != null)
            {
                _ = RunPerfEntries(perfEntries, warmUp, ArgumentValue(PerfRoutinesArgument));
            }

            var perfSeries = ArgumentValue(PerfSeriesArgument);
            if (perfSeries != null && perfEntries == null)
            {
                _ = RunPerfSeries(perfSeries);
            }

            // It. 6a (§4.5): la escena (libre con Fuerte), la secuencia de las horas (VID-05) y el diálogo de completado con la toma aérea.
            var perfScene = ArgumentValue(PerfSceneArgument);
            if (perfScene != null && perfEntries == null && perfSeries == null)
            {
                _ = RunPerfScene(perfScene);
            }
            else if (HasArgument(PerfHoursArgument) && perfEntries == null && perfSeries == null)
            {
                _ = RunPerfHours();
            }
            else if (HasArgument(PerfCompletionArgument) && perfEntries == null && perfSeries == null)
            {
                _ = RunPerfCompletion();
            }
            else if (ArgumentValue(PerfLongSeriesArgument) != null && perfEntries == null && perfSeries == null)
            {
                _ = RunPerfLongSeries(ArgumentValue(PerfLongSeriesArgument)!);
            }
            else if (HasArgument(PerfHistoryArgument) && perfEntries == null && perfSeries == null)
            {
                _ = RunPerfHistory();
            }
#endif
            if (warmUp)
            {
                _warmup = new HudWarmup(_shell.Shell.Ui, root);
                _warmup.Start();
            }
        }

        private void Update()
        {
            // It. 10a (§5.2, QA-G10a-17): primero lo que llegó de otros hilos, después los eventos del plugin BLE (sin asignar si no hay) y
            // recién ahí el programador.
            _mainThread?.Drain();
            _blePump?.Drain(false);
            _scheduler?.Tick();
            _surface?.Update();
            if (_shell == null)
            {
                return;
            }

            _shell.Tick();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            PerfHistoryFrame();
#endif
            _screenReader?.Tick(_scheduler!.MonotonicSeconds);
            _windowMode?.Update();
            if (_warmup != null && _warmup.Tick())
            {
                _warmup = null;
                HudWarmedUp = true;
            }

            _pacer?.Apply(_scheduler!.MonotonicSeconds);

            // Tema de la barra de título de Windows cambiado con la app enfocada (AC-APP-05-10): una lectura cada 2 s.
            _platform?.TitleBar.Poll(_scheduler!.MonotonicSeconds);

            // "Atrás" de Android llega como Escape del Input System (AC-APP-02-06): SystemBack en Android, Escape en el resto
            // (Escape solo cierra menús y diálogos, P-08). Es el ÚNICO camino de Escape (03-rutinas/02-tech-design §4.2).
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                _shell.HandleBack(_backSource);
            }
        }

        // It. 6a (06a-escena-lago/02-tech-design §3.2, §4.2, §4.4): la escena se avanza y se dibuja una vez por cuadro, DESPUÉS de los
        // Update (las animaciones del HUD de este cuadro ya corrieron) y antes de que la UI se componga (el panel muestrea la RT recién
        // dibujada: cero cuadros de atraso, S6a-7). La adaptación térmica mide solo los cuadros en que la escena se dibujó.
        private void LateUpdate()
        {
            var bridge = _sceneBridge;
            if (bridge == null || _shell == null || _scheduler == null || _app == null || _platform == null)
            {
                return;
            }

            var host = bridge.Host;
            host.SetViewport(Screen.width, Screen.height, _platform.Display.DpScale);
            var before = host.Draws;
            var now = _scheduler.MonotonicSeconds;
            bridge.Tick(now, _shell.Motion, _app.Workout.State.Value.Phase);
            if (_thermal != null)
            {
                ApplyQuality(_thermal.Sample(now, host.Draws != before));
            }
        }

        private SceneBridge? CreateScene()
        {
            var world = _assets?.SceneWorld;
            if (world == null || !world.IsComplete || _app == null || _platform == null || _logger == null)
            {
                _logger?.Log(LogLevel.Warning, RowMate.Scene.SceneHost.Tag, "sin SceneWorld completo (correr RowMate/Bootstrap/Reaplicar): el inmersivo sigue con el fondo provisorio");
                return null;
            }

            var tier = RowMate.Platform.Display.GraphicsTier.ChooseForThisDevice();
            var forcedTier = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (RowMate.Platform.Display.GraphicsTier.TryParse(ArgumentValue(QualityArgument), out var forced))
            {
                tier = forced;
                forcedTier = true;
            }
#endif
            _assetQuality = Application.isEditor
                ? (RowMate.Core.Scene.IRenderAssetQuality)new RowMate.Platform.Display.EditorRenderAssetQuality()
                : new RowMate.Platform.Display.PlayerUrpAssetQuality();
            _assetQuality.ApplyTier(tier);
            var host = new RowMate.Scene.SceneHost(world, _app, new RowMate.Scene.SceneHostOptions
            {
                Tier = tier,
                AssetQuality = _assetQuality,
                RegionCode = RegionOf(_platform.Locale.PreferredLanguageTags),
            });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var delay = ArgumentValue(SceneDelayArgument);
            if (delay != null && double.TryParse(delay, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0)
            {
                _sceneDelaySeconds = seconds;
            }
#endif
            // QA-V6a-REN-13: con el nivel forzado (-rowmate-quality, la línea de base de GPU para la It. 14) la adaptación queda apagada: la
            // resolución es la del nivel durante toda la corrida (antes subía de 0,85 a 0,95 y las ventanas mezclaban tres resoluciones).
            _thermal = forcedTier ? null : new RowMate.Platform.Display.ThermalAdapter(host.Resolution);
            _logger.Log(LogLevel.Info, RowMate.Scene.SceneHost.Tag, "escena: nivel " + tier + (forcedTier ? " (forzado: sin adaptación)" : ""));
            return new SceneBridge(host, _logger);
        }

        // La adaptación (ARCHITECTURE §20.3): la resolución y la densidad en la escena; la distancia de sombras y el nivel en el asset.
        private void ApplyQuality(RowMate.Core.Scene.QualityAction action)
        {
            var host = _sceneBridge?.Host;
            if (host == null)
            {
                return;
            }

            switch (action.Kind)
            {
                case RowMate.Core.Scene.QualityActionKind.LowerResolution:
                case RowMate.Core.Scene.QualityActionKind.RaiseResolution:
                    host.SetResolution(action.Resolution);
                    break;
                case RowMate.Core.Scene.QualityActionKind.LowerShadowDistance:
                    _assetQuality?.SetShadowDistance(host.Profile.ShadowDistance / 2);
                    break;
                case RowMate.Core.Scene.QualityActionKind.LowerVegetationDensity:
                    host.SetVegetationDensity(0.5);
                    break;
                case RowMate.Core.Scene.QualityActionKind.LowerTier:
                    // QA-V6a-REN-09: el nivel entero (el perfil de la escena y, por el IRenderAssetQuality de sus opciones, el asset de URP).
                    host.LowerTier();
                    break;
            }

            if (action.Kind != RowMate.Core.Scene.QualityActionKind.None)
            {
                _logger?.Log(LogLevel.Info, RowMate.Scene.SceneHost.Tag, "adaptación: " + action.Kind + " (resolución " + host.Resolution.ToString("0.00", CultureInfo.InvariantCulture) + ")");
            }
        }

        // La región del sistema para el hemisferio de la luna (DV-57): "es-AR" → "AR"; sin región, null (el norte).
        private static string? RegionOf(System.Collections.Generic.IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return null;
            }

            var parts = tags[0].Split('-', '_');
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 2)
                {
                    return parts[i].ToUpperInvariant();
                }
            }

            return null;
        }

        private double _sceneDelaySeconds;

        // Segundo plano (Android e iOS: la actividad se pausa y el loop se detiene; en escritorio Unity no lo llama con
        // runInBackground: minimizada NO es segundo plano). 02-tech-design §4.2:
        // - Al irse: primero el ciclo de vida (el motor manda lo pendiente y espera la base a lo sumo 1 s, AC-WKT-06-15: queda
        //   escrito ANTES de que el sistema congele la app), después el programador anota el segundo plano.
        // - Al volver: primero el alcance (FrameScheduler.ResumeFromBackground: el remo y el motor disparan en orden todo lo
        //   vencido, con la hora de su instante; sin un EnterBackground antes, como la llamada del arranque, no hace nada) y
        //   recién después el ciclo de vida vuelve a primer plano: el motor publica UNA foto con Instant, los topes de espera
        //   vuelven a correr, ScreenKeepAwake vuelve a pedir la pantalla y, con foco, Workout muestra el aviso retenido.
        private void OnApplicationPause(bool paused)
        {
            if (_scheduler == null || _lifecycle == null)
            {
                return;
            }

            if (paused)
            {
                _lifecycle.SetBackground(true);
                _scheduler.EnterBackground();
                return;
            }

            if (_scheduler.InBackground)
            {
                // It. 10a (§5.2): los eventos BLE de la pausa se agendan en su instante virtual ANTES del alcance (desfase congelado).
                _mainThread?.Drain();
                _blePump?.Drain(true);
                var fired = _scheduler.ResumeFromBackground();
                _logger?.Log(LogLevel.Debug, Tag, "vuelta del segundo plano: " + fired.ToString(CultureInfo.InvariantCulture) + " disparos en el alcance");
            }

            _lifecycle.SetBackground(false);
        }

        // El foco de la ventana (en Windows, minimizar también lo saca): el ciclo de vida (los avisos esperan a que el usuario
        // esté delante, AC-WKT-02-09; ScreenKeepAwake vuelve a pedir, AC-APP-14-02) y, al recuperarlo, idioma, letra y
        // movimiento del sistema (AC-APP-09-05).
        private void OnApplicationFocus(bool focus)
        {
            if (_platform == null || _app == null)
            {
                return;
            }

            if (focus)
            {
                _platform.Refresh();
                _app.RefreshSystemSettings(_platform.Locale.PreferredLanguageTags);
            }

            _lifecycle?.SetFocus(focus);
        }

        // Orden inverso de lo que se haya creado (Start puede no haber corrido), sin avisarle nada al usuario (AC-APP-01-08):
        // shell → app (stores; el motor deja cortada una sesión en curso con todo lo que mandó, P-03) → backend → LocalStore
        // (el ejecutor termina lo encolado y cierra la conexión en su hilo) → superficie → pantalla encendida liberada
        // (AC-APP-14-03). Un guardado que ya cerró el editor ya hizo commit (el editor espera el commit, AC-RUT-08-07).
        private void OnDestroy()
        {
            // It. 14 (DT14-14, QA-G14-04): el WndProc original vuelve ANTES de que se apague el dominio (también lo hace Application.quitting).
            _windowFrame?.Uninstall();
            _windowFrame = null;
            _screenReader?.Dispose();
            _screenReader = null;
            _warmup?.Dispose();
            _warmup = null;
            _shell?.Dispose();
            _shell = null;
            _sceneBridge?.Dispose();
            _sceneBridge = null;
            _app?.Dispose();
            _app = null;
            // It. 5: lo último que se tocó se termina de guardar (hasta 1 s, como la base).
            if (_preferences != null)
            {
                try
                {
                    _preferences.FlushAsync().Wait(TimeSpan.FromSeconds(1));
                }
                catch (Exception error)
                {
                    _logger?.Log(LogLevel.Warning, Tag, "no se terminaron de guardar las preferencias", error);
                }

                _preferences.Dispose();
                _preferences = null;
            }

            _backend?.Dispose();
            _backend = null;
            _store?.Dispose();
            _store = null;
            // It. 6a (lo encontró SceneBudgetTests: los contadores de dibujo son de todo el cuadro): la copia del panel también se
            // destruye. UiSurface.Dispose la deja sin RT (y una copia viva sin RT dibuja en la pantalla): en las pruebas que cargan y
            // descargan Boot.unity quedaban paneles de más dibujando en cada cuadro.
            var runtimePanel = _surface?.Panel;
            _surface?.Dispose();
            _surface = null;
            if (runtimePanel != null)
            {
                RowMate.Presentation.Surface.RuntimePanelFactory.Destroy(runtimePanel);
            }

            _keepAwake?.Dispose();
            _keepAwake = null;
            _scheduler?.Dispose();
            _platform?.SleepSource.Dispose();
        }

        // It. 14 (AC-APP-15-05 a -07, -09; DT14-13 a DT14-16, QA-G14-10): Windows: la subclase del WndProc (mínimo de 640 × 360 lógicos,
        // Alt+Enter sin pitido), la ventana inicial de 1280 × 720 lógicos centrada en el área de trabajo (salvo que la línea de comandos
        // traiga -screen-* o -popupwindow) y F11 / Alt+Enter. macOS: 800 × 600 lógicos (el mínimo queda para la 10b, D-04). iPhone: sin
        // vertical invertida (UnityAppController manda sobre Info.plist). No se lee ni se escribe el registro.
        private void SetUpWindow(IAppLogger logger)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                var window = RowMate.Platform.Display.WindowsWndProcApi.FindPlayerWindow();
                if (window == IntPtr.Zero)
                {
                    logger.Log(LogLevel.Warning, "PLATFORM", "no se encontró la ventana del player: sin mínimo ni pantalla completa");
                    return;
                }

                var api = new RowMate.Platform.Display.WindowsWndProcApi();
                _windowFrame = new RowMate.Platform.Display.WindowsWindowFrame(api, window);
                var installed = _windowFrame;
                if (!installed.Install(uninstall => Application.quitting += uninstall))
                {
                    logger.Log(LogLevel.Warning, "PLATFORM", "no se pudo subclasificar la ventana: sin mínimo de 640 × 360");
                }
                else
                {
                    // QA-V14-01: al salir, el registro dice si el WndProc original volvió (WM_CLOSE lo restaura antes de que Unity apague).
                    Application.quitting += () => logger.Log(LogLevel.Info, "PLATFORM",
                        RowMate.Platform.Display.WindowsWindowFrame.Describe(installed.LastUninstall));
                }

                var frame = new RowMate.Platform.Display.PlayerWindowFrame(api, window);
                // It. 15 (#74): la ubicación (la guardada o la inicial) va en PlaceWindow, cuando ya se leyó prefs.json.
                _playerFrame = frame;
                installed.Closing += SaveWindow;
                Application.quitting += SaveWindow;

                // QA-V14-03: el área de contenido en bg.app hasta el primer cuadro (otra vez después de ubicarla: el área creció).
                RowMate.Platform.Display.WindowsBootBackground.Paint(window);
                var firstPaint = RowMate.Platform.Display.WindowsBootBackground.FirstPaintMs;
                logger.Log(LogLevel.Info, "PLATFORM", double.IsNaN(firstPaint)
                    ? "fondo de la ventana en bg.app desde AppRoot (sin la pintura temprana)"
                    : "fondo de la ventana en bg.app a los " + firstPaint.ToString("0", CultureInfo.InvariantCulture) + " ms del arranque");
                _windowMode = new RowMate.Platform.Display.WindowModeController(frame);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Warning, "PLATFORM", "ventana sin configurar: " + error.GetType().Name);
            }
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            if (WindowGeometry.ShouldPlaceInitial(Environment.GetCommandLineArgs()))
            {
                Screen.SetResolution(WindowGeometry.MacInitialWidth, WindowGeometry.MacInitialHeight, FullScreenMode.Windowed);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (UnityEngine.iOS.Device.generation.ToString().StartsWith("iPhone", StringComparison.Ordinal))
            {
                Screen.autorotateToPortraitUpsideDown = false;
            }
#else
            _ = logger;
#endif
        }

        // It. 15 (#74, AC-D15-19, DV-145, DV-130; DT15-08): al abrir, la ventana guardada en prefs.json si se ve al menos en un 50 % en
        // algún monitor; si no (o si falta), centrada en 1280 × 720 como en la It. 14. Con -screen-* o -popupwindow, nada.
        private void PlaceWindow(IAppLogger logger)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var frame = _playerFrame;
            if (frame == null || !WindowGeometry.ShouldPlaceInitial(Environment.GetCommandLineArgs()))
            {
                return;
            }

            try
            {
                var saved = _preferences != null ? WindowPrefs.Read(_preferences) : null;
                if (frame.RestoreSaved(saved))
                {
                    logger.Log(LogLevel.Info, "PLATFORM", "ventana restaurada de prefs.json" + (saved != null && saved.Maximized ? " (maximizada)" : ""));
                }
                else
                {
                    frame.PlaceInitial();
                    logger.Log(LogLevel.Info, "PLATFORM", saved == null ? "ventana inicial (sin ventana guardada)" : "ventana inicial (la guardada no se ve en ningún monitor)");
                }

                var window = RowMate.Platform.Display.WindowsWndProcApi.FindPlayerWindow();
                if (window != IntPtr.Zero)
                {
                    RowMate.Platform.Display.WindowsBootBackground.Paint(window);
                }
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Warning, "PLATFORM", "ventana sin ubicar: " + error.GetType().Name);
            }
#else
            _ = logger;
#endif
        }

        // Al cerrar (WM_CLOSE y Application.quitting; lo primero que llegue): la ventana en prefs.json (dp lógicos; la pantalla completa
        // no se recuerda, DV-129). La escritura la espera OnDestroy (FlushAsync).
        private void SaveWindow()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (_windowSaved || _playerFrame == null || _preferences == null)
            {
                return;
            }

            _windowSaved = true;
            try
            {
                var saved = _playerFrame.Capture(_windowMode?.State ?? new WindowModeState());
                WindowPrefs.Write(_preferences, saved);
                _logger?.Log(LogLevel.Info, "PLATFORM", "ventana guardada: " + saved.Width.ToString(CultureInfo.InvariantCulture) + " x "
                    + saved.Height.ToString(CultureInfo.InvariantCulture) + " dp" + (saved.Maximized ? " (maximizada)" : ""));
            }
            catch (Exception error)
            {
                _logger?.Log(LogLevel.Warning, "PLATFORM", "ventana sin guardar: " + error.GetType().Name);
            }
#endif
        }

        // La capa de arranque: siempre, salvo -rowmate-boot-splash off en desarrollo (la medición de DT14-12).
        private static bool BootSplashEnabled()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return !string.Equals(ArgumentValue(BootSplashArgument), "off", StringComparison.OrdinalIgnoreCase);
#else
            return true;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // -rowmate-perf-boot (DT14-12): "[PERF] arranque: primer cuadro del shell {ms}" desde que arrancó el proceso, y sale con 0 un
        // segundo después (build.ps1 -BootSmoke toma la mediana de 5 con y sin la capa).
        private void WatchBootForPerf()
        {
            var shell = _shell!;
            var logger = _logger!;
            var scheduler = _scheduler!;
            shell.Ready.Ready += () =>
            {
                double ms;
                try
                {
                    ms = (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds;
                }
                catch (Exception)
                {
                    ms = Time.realtimeSinceStartupAsDouble * 1000.0;
                }

                logger.Log(LogLevel.Info, "PERF", "arranque: primer cuadro del shell " + ms.ToString("0", CultureInfo.InvariantCulture) + " ms (capa de arranque "
                    + (BootSplashEnabled() ? "sí" : "no") + ")");
                _ = QuitAfter(scheduler, TimeSpan.FromSeconds(1));
            };
        }

        private static async System.Threading.Tasks.Task QuitAfter(UnityScheduler scheduler, TimeSpan delay)
        {
            await scheduler.Delay(delay);
            Application.Quit(0);
        }
#endif

        // It. 12 (DT12-03): la zona del equipo AL EXPORTAR, sin caché (el equipo pudo cambiar de zona u horario de verano). En el hilo
        // principal, una vez por exportación (TcxExportService).
        private static TimeZoneInfo ExportZoneNow()
        {
            TimeZoneInfo.ClearCachedData();
            return TimeZoneInfo.Local;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // It. 12 (DT12-20; AC-STR-09-26): la sesión más nueva, exportada n veces seguidas con el exportador de la app; registra
        // "[EXPORT] humo: k/n <archivo>" (solo el nombre: nunca la ruta) y "[EXPORT] humo: ok" o "falla (…)". Solo con ROWMATE_DATA_DIR.
        private async System.Threading.Tasks.Task RunExportSmoke(string countText)
        {
            var logger = _logger!;
            var app = _app!;
            if (!DataLocation.IsOverridden || !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 1)
            {
                logger.Log(LogLevel.Error, RowMate.Core.Export.ExportLog.Tag, "humo: falla (hace falta ROWMATE_DATA_DIR y un número de exportaciones)");
                Application.Quit(1);
                return;
            }

            var saved = new System.Collections.Generic.List<string>();
            var failed = 0;
            using (app.Export.Notices.Subscribe(notice =>
            {
                if (notice.Kind == RowMate.App.Export.TcxExportNoticeKind.Saved && notice.File != null)
                {
                    saved.Add(notice.File.FileName);
                }
                else
                {
                    failed++;
                }
            }))
            {
                try
                {
                    var history = await _store!.Sessions.GetHistoryAsync();
                    if (history.Count == 0)
                    {
                        logger.Log(LogLevel.Error, RowMate.Core.Export.ExportLog.Tag, "humo: falla (la base no tiene sesiones)");
                        Application.Quit(1);
                        return;
                    }

                    var newest = history[0].Session;
                    for (var i = 0; i < count; i++)
                    {
                        await app.Export.ExportAsync(newest);
                        var name = saved.Count > i ? saved[i] : "(sin archivo)";
                        logger.Log(LogLevel.Info, RowMate.Core.Export.ExportLog.Tag, "humo: " + (i + 1).ToString(CultureInfo.InvariantCulture) + "/"
                            + count.ToString(CultureInfo.InvariantCulture) + " " + name);
                    }
                }
                catch (Exception error)
                {
                    logger.Log(LogLevel.Error, RowMate.Core.Export.ExportLog.Tag, "humo: falla (" + error.GetType().Name + ")");
                    Application.Quit(1);
                    return;
                }
            }

            var ok = saved.Count == count && failed == 0;
            logger.Log(ok ? LogLevel.Info : LogLevel.Error, RowMate.Core.Export.ExportLog.Tag, ok ? "humo: ok" : "humo: falla (" + failed.ToString(CultureInfo.InvariantCulture) + " fallas)");
            Application.Quit(ok ? 0 : 1);
        }

        private async System.Threading.Tasks.Task RunDatabaseSmoke(string folder)
        {
            var ok = await DatabaseSmoke.RunAsync(folder, _logger!);
            Application.Quit(ok ? 0 : 1);
        }

        private async System.Threading.Tasks.Task RunDatabaseRoundTrip()
        {
            var store = _app!.Routines;
            await store.LoadAsync();
            var state = store.State.Value;
            if (state.Phase != RowMate.App.Routines.RoutinesPhase.Ready)
            {
                _logger!.Log(LogLevel.Error, "DB", "ida y vuelta: falla (no cargó)");
                Application.Quit(1);
                return;
            }

            foreach (var routine in state.Routines)
            {
                if (routine.Name == RoundTripName && routine.Steps.Count == 2)
                {
                    _logger!.Log(LogLevel.Info, "DB", "ida y vuelta: sigue después de reabrir (" + routine.Steps.Count.ToString(CultureInfo.InvariantCulture)
                        + " pasos, " + state.Routines.Count.ToString(CultureInfo.InvariantCulture) + " rutinas)");
                    Application.Quit(0);
                    return;
                }
            }

            var steps = new[]
            {
                new RowMate.Core.Models.IntervalStep(0, 0, RowMate.Core.Models.StepType.Work) { DurationSeconds = 60 },
                new RowMate.Core.Models.IntervalStep(0, 1, RowMate.Core.Models.StepType.Rest) { DurationSeconds = 30 },
            };
            var created = await store.CreateAsync(RoundTripName, "", steps);
            _logger!.Log(created != null ? LogLevel.Info : LogLevel.Error, "DB", created != null ? "ida y vuelta: guardada" : "ida y vuelta: falla (no guardó)");
            Application.Quit(created != null ? 0 : 1);
        }

        // QA-V4b-06 (05-series-parciales/00-deuda-4b §2): N entradas al inmersivo, una libre y una rutina alternadas, de 12 s cada
        // una (FrameTimeProbe escribe su ventana [PERF] de 10 s y la línea de la entrada), terminadas como con Terminar → Terminar;
        // después sale con 0. Solo en la carpeta de desarrollo (ROWMATE_DATA_DIR): crea ahí una rutina de prueba si no hay ninguna.
        private async System.Threading.Tasks.Task RunPerfEntries(string countText, bool warmUp, string? routineNames)
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            if (!DataLocation.IsOverridden || !int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 1)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de la entrada: hace falta ROWMATE_DATA_DIR y un número de entradas");
                Application.Quit(1);
                return;
            }

            try
            {
                logger.Log(LogLevel.Info, "PERF", "medición de la entrada (QA-V4b-06): " + count.ToString(CultureInfo.InvariantCulture) + " entradas, "
                    + (warmUp ? "con" : "sin") + " precalentado del HUD");
                app.Shell.Select(1);
                await app.Routines.LoadAsync();
                RowMate.Core.Models.Routine? routine = null;
                foreach (var candidate in app.Routines.State.Value.Routines)
                {
                    if (candidate.Name == PerfRoutineName)
                    {
                        routine = candidate;
                    }
                }

                if (routine == null)
                {
                    var steps = new[]
                    {
                        new RowMate.Core.Models.IntervalStep(0, 0, RowMate.Core.Models.StepType.Warmup) { DurationSeconds = 30 },
                        new RowMate.Core.Models.IntervalStep(0, 1, RowMate.Core.Models.StepType.Work) { DurationSeconds = 60, TargetWattsMin = 160, TargetSpm = 22 },
                        new RowMate.Core.Models.IntervalStep(0, 2, RowMate.Core.Models.StepType.Rest) { DurationSeconds = 30 },
                    };
                    routine = await app.Routines.CreateAsync(PerfRoutineName, "", steps);
                }

                // It. 5: con -rowmate-perf-routines, la primera libre y después esas rutinas en orden (las siembra SnapshotCli -seed F5).
                var named = NamedRoutines(app, routineNames);
                if (named == null)
                {
                    logger.Log(LogLevel.Error, "PERF", "medición de la entrada: falta una de las rutinas de " + PerfRoutinesArgument);
                    Application.Quit(1);
                    return;
                }

                for (var wait = 0; wait < 100 && !app.WorkoutRest.State.Value.CanStart; wait++)
                {
                    await scheduler.Delay(TimeSpan.FromMilliseconds(100));
                }

                await scheduler.Delay(TimeSpan.FromSeconds(2));
                for (var i = 0; i < count; i++)
                {
                    var entryRoutine = named.Count > 0 ? (i == 0 ? null : named[(i - 1) % named.Count]) : (i % 2 == 0 ? null : routine);
                    var free = entryRoutine == null;
                    logger.Log(LogLevel.Info, "PERF", "entrada " + (i + 1).ToString(CultureInfo.InvariantCulture) + ": " + (free ? "libre" : "rutina " + entryRoutine!.Name));
                    if (_sceneDelaySeconds > 0)
                    {
                        // -rowmate-scene-delay: la escena demorada (sin precarga), para medir su preparación por tramos y su primer dibujo.
                        _sceneBridge?.Host.DelayReadinessForPerf(_sceneDelaySeconds);
                    }

                    var started = free ? app.WorkoutRest.StartFree() : app.WorkoutRest.StartRoutine(entryRoutine!);
                    if (!started)
                    {
                        logger.Log(LogLevel.Error, "PERF", "no arrancó la entrada " + (i + 1).ToString(CultureInfo.InvariantCulture));
                        Application.Quit(1);
                        return;
                    }

                    await scheduler.Delay(TimeSpan.FromSeconds(12));
                    LogSceneWarmup(logger, i + 1);
                    await app.Workout.FinishAsync();
                    await scheduler.Delay(TimeSpan.FromSeconds(3));
                }

                logger.Log(LogLevel.Info, "PERF", "medición de la entrada: terminada");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de la entrada: falla", error);
                Application.Quit(1);
            }
        }

        // It. 6a: el peor cuadro de la preparación de la escena desde el último toque (la escena demorada: ≤ su tramo, AC-WKT-13-15).
        private void LogSceneWarmup(IAppLogger logger, int entry)
        {
            var host = _sceneBridge?.Host;
            if (host == null)
            {
                return;
            }

            // It. 8 (02-tech-design §5): el [PERF] por escenario dice cuál se midió (el del kit armado y el de la sesión, si hay).
            logger.Log(LogLevel.Info, "PERF", "escenario (n.º " + entry.ToString(CultureInfo.InvariantCulture) + "): kit " + host.KitEnvironment
                + ", sesión " + (host.Session != null ? host.Session.Environment.Id.ToString() : "ninguna") + ", cambios de kit " + host.KitSwitches.ToString(CultureInfo.InvariantCulture));
            logger.Log(LogLevel.Info, "PERF", "escena (n.º " + entry.ToString(CultureInfo.InvariantCulture) + "): " + host.Readiness.Value + ", peor cuadro de la preparación "
                + host.WorstWarmupMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms, dibujos " + host.Draws.ToString(CultureInfo.InvariantCulture)
                + ", nivel " + host.Tier + ", resolución " + host.Resolution.ToString("0.00", CultureInfo.InvariantCulture)
                + "; preparándose: " + _sceneBridge!.WarmupFrames.ToString(CultureInfo.InvariantCulture) + " cuadros con tramo y "
                + _sceneBridge.StarvedFrames.ToString(CultureInfo.InvariantCulture) + " sin tramo"
                + (_sceneBridge.LastStarvedBy != null ? " (lo último que lo retuvo: " + _sceneBridge.LastStarvedBy.Name + ")" : "")
                // Ronda 1 de la verificación: la unidad más cara del armado (QA-V6a-REN-06), la orilla armada entera a la vista (REN-04: 0) y
                // las veces que el cuadro del armado del HUD terminó la orilla (CatchUp).
                + "; peor unidad " + host.WorstUnitMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms (" + host.WorstUnitPhase
                + (host.WorstUnitStep.Length > 0 ? ": " + host.WorstUnitStep : "") + "; por fase: " + UnitsByPhase(host) + "); orilla entera a la vista: "
                + host.InViewShoreRebuilds.ToString(CultureInfo.InvariantCulture) + "; orilla terminada en el negro: "
                + host.CatchUps.ToString(CultureInfo.InvariantCulture) + "; CPU de la escena del último cuadro "
                + host.LastCpuMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms");
            // It. 7 (07-remero-palada/02-tech-design §4, §7.2 B2): el remero que se dibujó (el maniquí o el del slot) y sus triángulos.
            var rower = host.World?.Rower;
            if (rower != null)
            {
                logger.Log(LogLevel.Info, "PERF", "remero (n.º " + entry.ToString(CultureInfo.InvariantCulture) + "): " + (rower.FromSlot ? "slot " + rower.Source : "maniquí")
                    + ", " + rower.Triangles.ToString(CultureInfo.InvariantCulture) + " triángulos, " + rower.Spec.Height.ToString("0.000", CultureInfo.InvariantCulture) + " m");
            }
        }

        // La unidad más cara de cada fase del armado (QA-V6a-REN-06).
        private static string UnitsByPhase(RowMate.Scene.SceneHost host)
        {
            var text = new System.Text.StringBuilder();
            for (var phase = 0; phase < RowMate.Scene.SceneHost.PhaseCount; phase++)
            {
                if (text.Length > 0)
                {
                    text.Append(", ");
                }

                text.Append(RowMate.Scene.SceneHost.PhaseName(phase)).Append(' ').Append(host.WorstUnitMsOf(phase).ToString("0.00", CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        // Espera a que se pueda empezar (el remo conectado) y la escena esté lista (la precarga en reposo), como mucho 20 s.
        private async System.Threading.Tasks.Task<bool> WaitForStart(AppContext app, UnityScheduler scheduler)
        {
            app.Shell.Select(1);
            for (var wait = 0; wait < 200 && (!app.WorkoutRest.State.Value.CanStart
                                              || _sceneBridge != null && _sceneBridge.Host.Readiness.Value == RowMate.Scene.SceneReadiness.Loading); wait++)
            {
                await scheduler.Delay(TimeSpan.FromMilliseconds(100));
            }

            await scheduler.Delay(TimeSpan.FromSeconds(2));
            return app.WorkoutRest.State.Value.CanStart;
        }

        // -rowmate-perf-scene <s> (AC-SCN-01-10): libre con Fuerte durante <s> s; FrameTimeProbe escribe la ventana de 10 s (p99, máximo,
        // GPU, memoria) y la entrada; al final, la escena (nivel, resolución, peor armado). Solo con ROWMATE_DATA_DIR. Sale con 0.
        private async System.Threading.Tasks.Task RunPerfScene(string secondsText)
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            if (!DataLocation.IsOverridden || !int.TryParse(secondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds < 10)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de la escena: hace falta ROWMATE_DATA_DIR y los segundos (10 o más)");
                Application.Quit(1);
                return;
            }

            try
            {
                if (!await WaitForStart(app, scheduler))
                {
                    logger.Log(LogLevel.Error, "PERF", "medición de la escena: el remo no se conectó");
                    Application.Quit(1);
                    return;
                }

                DeviceBackendFactory.SetPerfPreset(_backend!, 280, 30);
                var envText = ArgumentValue(PerfEnvArgument);
                var environment = RowMate.Core.Scene.EnvironmentId.Lake;
                if (envText != null && !RowMate.Core.Scene.EnvironmentKeys.TryParse(envText, out environment))
                {
                    logger.Log(LogLevel.Error, "PERF", "medición de la escena: escenario desconocido " + envText);
                    Application.Quit(1);
                    return;
                }

                var hourText = ArgumentValue(PerfHourArgument);
                if (hourText != null && int.TryParse(hourText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour) && hour >= 0 && hour < 24)
                {
                    app.Scene.HourOverride.Value = hour;
                }

                if (envText != null)
                {
                    // DA8-16: el escenario se elige en reposo con el lago listo y la entrada va 1 s después.
                    app.SceneEnvironment.Select(environment);
                    await scheduler.Delay(TimeSpan.FromSeconds(1));
                }

                logger.Log(LogLevel.Info, "PERF", "medición de la escena: libre con Fuerte, " + seconds.ToString(CultureInfo.InvariantCulture) + " s; escenario "
                    + RowMate.Core.Scene.EnvironmentKeys.ToKey(app.SceneEnvironment.Selected.Value) + "; hora " + (app.Scene.HourOverride.Value?.ToString(CultureInfo.InvariantCulture) ?? "real"));
                app.WorkoutRest.StartFree();
                await scheduler.Delay(TimeSpan.FromSeconds(seconds));
                LogSceneWarmup(logger, 1);
                await app.Workout.FinishAsync();
                await scheduler.Delay(TimeSpan.FromSeconds(3));
                logger.Log(LogLevel.Info, "PERF", "medición de la escena: terminada");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de la escena: falla", error);
                Application.Quit(1);
            }
        }

        // ── -rowmate-perf-history (It. 15, AC-D15-03, DT15-19) ──
        // Los cuadros (ms) mientras se mide y, durante la rueda, la lista que se desplaza un poco por cuadro (como una rueda continua: se
        // mueve el desplazamiento de la ScrollView, el mismo trabajo de layout y repintado que la rueda real).
        private System.Collections.Generic.List<double>? _perfFrames;
        private ScrollView? _perfWheel;
        private float _perfWheelStep = 24f;

        private void PerfHistoryFrame()
        {
            _perfFrames?.Add(Time.unscaledDeltaTime * 1000.0);
            var scroll = _perfWheel;
            if (scroll == null)
            {
                return;
            }

            var max = Math.Max(0f, scroll.contentContainer.layout.height - scroll.contentViewport.layout.height);
            var y = scroll.scrollOffset.y + _perfWheelStep;
            if (y >= max || y <= 0)
            {
                _perfWheelStep = -_perfWheelStep;
                y = Mathf.Clamp(y, 0, max);
            }

            scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, y);
        }

        private System.Collections.Generic.List<double> TakeFrames()
        {
            var frames = _perfFrames ?? new System.Collections.Generic.List<double>();
            _perfFrames = null;
            return frames;
        }

        private static double MaxOf(System.Collections.Generic.List<double> values)
        {
            var max = 0.0;
            for (var i = 0; i < values.Count; i++)
            {
                max = Math.Max(max, values[i]);
            }

            return max;
        }

        private async System.Threading.Tasks.Task RunPerfHistory()
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            var screen = _shell?.Shell.HistoryScreen;
            if (!DataLocation.IsOverridden || screen == null)
            {
                logger.Log(LogLevel.Error, "PERF", "medición del Historial: hace falta ROWMATE_DATA_DIR y la pestaña Historial");
                Application.Quit(1);
                return;
            }

            try
            {
                await scheduler.Delay(TimeSpan.FromSeconds(2));
                app.Shell.Select(screen.TabIndex);
                await scheduler.Delay(TimeSpan.FromSeconds(2));
                screen.List.FinishBatches();
                await scheduler.Delay(TimeSpan.FromSeconds(1));
                var count = screen.List.Count;
                // 1. Cinco recargas: el peor cuadro desde el pedido hasta 0,5 s después; base = lo que tardó la carga (la lectura en el
                //    ejecutor y la vuelta al hilo principal); gc = recolecciones de la generación 0 en las 5.
                var worstReload = 0.0;
                var worstBase = 0.0;
                var gcBefore = GC.CollectionCount(0);
                for (var k = 0; k < 5; k++)
                {
                    _perfFrames = new System.Collections.Generic.List<double>(256);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    await app.History.LoadAsync();
                    worstBase = Math.Max(worstBase, watch.Elapsed.TotalMilliseconds);
                    await scheduler.Delay(TimeSpan.FromMilliseconds(500));
                    worstReload = Math.Max(worstReload, MaxOf(TakeFrames()));
                }

                var gc = GC.CollectionCount(0) - gcBefore;
                // 2. El Detalle n.º 1: el peor cuadro desde el toque hasta 3 s después (la entrada de la página y la del gráfico).
                _perfFrames = new System.Collections.Generic.List<double>(512);
                var opened = screen.OpenAt(0);
                await scheduler.Delay(TimeSpan.FromSeconds(3));
                var worstDetail = MaxOf(TakeFrames());
                var data = screen.Detail?.Chart.Data;
                var minutes = data != null && data.Count > 0 ? data.X[data.Count - 1] / 60.0 : 0;
                screen.Detail?.Close();
                await scheduler.Delay(TimeSpan.FromSeconds(1.5));
                // 3. La lista desplazándose 10 s.
                _perfWheel = screen.List.Scroll;
                await scheduler.Delay(TimeSpan.FromMilliseconds(300));
                _perfFrames = new System.Collections.Generic.List<double>(2048);
                await scheduler.Delay(TimeSpan.FromSeconds(10));
                var wheel = TakeFrames();
                _perfWheel = null;
                wheel.Sort();
                var p95 = wheel.Count > 0 ? wheel[Math.Max(0, Math.Min(wheel.Count - 1, (int)Math.Ceiling(wheel.Count * 0.95) - 1))] : 0;
                var slow = 0;
                for (var i = 0; i < wheel.Count; i++)
                {
                    if (wheel[i] > 33)
                    {
                        slow++;
                    }
                }

                var pct = wheel.Count > 0 ? 100.0 * slow / wheel.Count : 0;
                var c = CultureInfo.InvariantCulture;
                logger.Log(LogLevel.Info, "PERF", "history: recarga " + worstReload.ToString("0.0", c) + " ms · base " + worstBase.ToString("0.0", c)
                    + " ms · gc " + gc.ToString(c) + " · detalle " + worstDetail.ToString("0.0", c) + " ms · rueda p95 " + p95.ToString("0.0", c)
                    + " ms · >33 ms " + pct.ToString("0.0", c) + " % (" + count.ToString(c) + " sesiones, " + wheel.Count.ToString(c) + " cuadros, detalle "
                    + (opened ? minutes.ToString("0", c) + " min" : "sin abrir") + ")");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición del Historial: falla", error);
                Application.Quit(1);
            }
        }

        // -rowmate-perf-hours (VID-05): libre a las 17 h forzadas y + cada 2 s hasta las 23 h. Sale con 0.
        private async System.Threading.Tasks.Task RunPerfHours()
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            try
            {
                if (!DataLocation.IsOverridden || !await WaitForStart(app, scheduler))
                {
                    logger.Log(LogLevel.Error, "PERF", "medición de las horas: hace falta ROWMATE_DATA_DIR y el remo");
                    Application.Quit(1);
                    return;
                }

                // QA-V6a-06: la hora se fija en Workout, ANTES del toque (como la pone el usuario desde el panel), no en el cuadro del toque.
                app.Scene.HourOverride.Value = 17;
                await scheduler.Delay(TimeSpan.FromSeconds(2));
                app.WorkoutRest.StartFree();
                await scheduler.Delay(TimeSpan.FromSeconds(4));
                for (var hour = 18; hour <= 23; hour++)
                {
                    app.Scene.HourOverride.Value = hour;
                    logger.Log(LogLevel.Info, "PERF", "hora de la escena: " + hour.ToString(CultureInfo.InvariantCulture) + " h");
                    await scheduler.Delay(TimeSpan.FromSeconds(2));
                }

                await scheduler.Delay(TimeSpan.FromSeconds(8));
                LogSceneWarmup(logger, 1);
                await app.Workout.FinishAsync();
                await scheduler.Delay(TimeSpan.FromSeconds(3));
                app.Scene.HourOverride.Value = null;
                logger.Log(LogLevel.Info, "PERF", "medición de las horas: terminada");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de las horas: falla", error);
                Application.Quit(1);
            }
        }

        // -rowmate-perf-completion: "Corta" (sembrada con -seed F5) hasta el diálogo de completado y la toma aérea (60 fps mientras se
        // mueve; después puede bajar a 30 y la adaptación no dispara); después el Terminar del diálogo. Sale con 0.
        private async System.Threading.Tasks.Task RunPerfCompletion()
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            try
            {
                await app.Routines.LoadAsync();
                var routines = NamedRoutines(app, "Corta");
                if (!DataLocation.IsOverridden || routines == null || routines.Count != 1 || !await WaitForStart(app, scheduler))
                {
                    logger.Log(LogLevel.Error, "PERF", "medición del completado: hace falta ROWMATE_DATA_DIR, el remo y Corta (sembrar con -seed F5)");
                    Application.Quit(1);
                    return;
                }

                // QA-V6a-06: StartRoutine directo, sin el toque en la lista ni el diálogo de confirmación: su primera entrada paga código que
                // en la app real ya corrió al tocar la rutina (00-deuda-5 §1.2). Esa entrada no es la medida de AC-WKT-13-15 (lo es la de
                // -rowmate-perf-entries y la prueba a mano por la lista).
                logger.Log(LogLevel.Info, "PERF", "completado: Corta con StartRoutine directo (la primera entrada no es la del usuario)");
                app.WorkoutRest.StartRoutine(routines[0]);
                await scheduler.Delay(TimeSpan.FromSeconds(45));
                logger.Log(LogLevel.Info, "PERF", "completado: fotogramas a " + (OnDemandRenderingInterval() > 1 ? "30" : "60") + " fps con el diálogo quieto");
                LogSceneWarmup(logger, 1);
                await app.Workout.AcknowledgeCompletion();
                await scheduler.Delay(TimeSpan.FromSeconds(3));
                logger.Log(LogLevel.Info, "PERF", "medición del completado: terminada");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición del completado: falla", error);
                Application.Quit(1);
            }
        }

        private static int OnDemandRenderingInterval()
        {
            return UnityEngine.Rendering.OnDemandRendering.renderFrameInterval;
        }

        // -rowmate-perf-long-series <s> (QA-V6a-13, QA-V6a-REN-03; AC-WKT-14-19, AC-SCN-01-10): "Serie larga" (100 × 30 s de trabajo y 30 s de
        // descanso, la fila de píldoras más larga; sembrada con SnapshotCli -seed F4B) con la escena durante <s> s y Fuerte; FrameTimeProbe
        // escribe la ventana de 10 s (máximo, p99, GPU, memoria y asignaciones por cuadro) y la entrada. Arranca con StartRoutine directo:
        // su primera entrada no es la del usuario (QA-V6a-06). Solo con ROWMATE_DATA_DIR. Sale con 0.
        private async System.Threading.Tasks.Task RunPerfLongSeries(string secondsText)
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            try
            {
                await app.Routines.LoadAsync();
                var routines = NamedRoutines(app, "Serie larga");
                if (!DataLocation.IsOverridden || routines == null || routines.Count != 1
                    || !int.TryParse(secondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds < 10
                    || !await WaitForStart(app, scheduler))
                {
                    logger.Log(LogLevel.Error, "PERF", "medición de Serie larga: hace falta ROWMATE_DATA_DIR, el remo, los segundos (10 o más) y Serie larga (sembrar con -seed F4B)");
                    Application.Quit(1);
                    return;
                }

                DeviceBackendFactory.SetPerfPreset(_backend!, 280, 30);
                logger.Log(LogLevel.Info, "PERF", "medición de Serie larga con la escena: " + seconds.ToString(CultureInfo.InvariantCulture)
                    + " s, con StartRoutine directo (la primera entrada no es la del usuario)");
                app.WorkoutRest.StartRoutine(routines[0]);
                var profile = ArgumentValue(PerfProfileArgument);
                if (!string.IsNullOrEmpty(profile) && seconds >= 28)
                {
                    await scheduler.Delay(TimeSpan.FromSeconds(25.5));
                    UnityEngine.Profiling.Profiler.maxUsedMemory = 256 * 1024 * 1024;
                    UnityEngine.Profiling.Profiler.logFile = profile;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = true;
                    UnityEngine.Profiling.Profiler.enableAllocationCallstacks = true;
                    UnityEngine.Profiling.Profiler.enabled = true;
                    logger.Log(LogLevel.Info, "PERF", "Profiler: grabando de 25,5 a 27,5 s");
                    await scheduler.Delay(TimeSpan.FromSeconds(2));
                    UnityEngine.Profiling.Profiler.enabled = false;
                    UnityEngine.Profiling.Profiler.enableBinaryLog = false;
                    UnityEngine.Profiling.Profiler.logFile = "";
                    logger.Log(LogLevel.Info, "PERF", "Profiler: terminado");
                    await scheduler.Delay(TimeSpan.FromSeconds(seconds - 27.5));
                }
                else
                {
                    await scheduler.Delay(TimeSpan.FromSeconds(seconds));
                }

                LogSceneWarmup(logger, 1);
                await app.Workout.FinishAsync();
                await scheduler.Delay(TimeSpan.FromSeconds(3));
                logger.Log(LogLevel.Info, "PERF", "medición de Serie larga: terminada");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de Serie larga: falla", error);
                Application.Quit(1);
            }
        }

        // Las rutinas de -rowmate-perf-routines, por nombre y en orden (vacío sin el argumento; null si falta alguna).
        private static System.Collections.Generic.List<RowMate.Core.Models.Routine>? NamedRoutines(AppContext app, string? names)
        {
            var result = new System.Collections.Generic.List<RowMate.Core.Models.Routine>();
            if (string.IsNullOrWhiteSpace(names))
            {
                return result;
            }

            foreach (var raw in names!.Split(';'))
            {
                var name = raw.Trim();
                if (name.Length == 0)
                {
                    continue;
                }

                RowMate.Core.Models.Routine? found = null;
                foreach (var candidate in app.Routines.State.Value.Routines)
                {
                    if (candidate.Name == name)
                    {
                        found = candidate;
                    }
                }

                if (found == null)
                {
                    return null;
                }

                result.Add(found);
            }

            return result;
        }

        // It. 5 (05-series-parciales/02-tech-design §3.7; AC-WKT-18-21 [PERF]): Fondo 3000 y después Series 600, cada una la mitad
        // de los segundos pedidos, abriendo y cerrando los paneles cada 2 s por la API de las preferencias (sin el toque de verdad);
        // FrameTimeProbe escribe sus ventanas "[PERF] paneles: …" (objetivo: ningún cuadro de más de 33 ms y p99 ≤ 16,7 ms). Solo en
        // la carpeta de desarrollo (ROWMATE_DATA_DIR), con las rutinas de F-5 sembradas (SnapshotCli -seed F5). Sale con 0.
        private async System.Threading.Tasks.Task RunPerfSeries(string secondsText)
        {
            var app = _app!;
            var scheduler = _scheduler!;
            var logger = _logger!;
            if (!DataLocation.IsOverridden || !int.TryParse(secondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) || seconds < 4)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de los paneles: hace falta ROWMATE_DATA_DIR y los segundos (4 o más)");
                Application.Quit(1);
                return;
            }

            try
            {
                app.Shell.Select(1);
                await app.Routines.LoadAsync();
                var routines = NamedRoutines(app, "Fondo 3000;Series 600");
                if (routines == null || routines.Count != 2)
                {
                    logger.Log(LogLevel.Error, "PERF", "medición de los paneles: faltan Fondo 3000 o Series 600 (sembrar con -seed F5)");
                    Application.Quit(1);
                    return;
                }

                for (var wait = 0; wait < 100 && !app.WorkoutRest.State.Value.CanStart; wait++)
                {
                    await scheduler.Delay(TimeSpan.FromMilliseconds(100));
                }

                await scheduler.Delay(TimeSpan.FromSeconds(2));
                // QA-V6a-06: con StartRoutine directo (sin el toque en la lista): la primera entrada no es la del usuario.
                logger.Log(LogLevel.Info, "PERF", "medición de los paneles: " + seconds.ToString(CultureInfo.InvariantCulture)
                    + " s, con StartRoutine directo (la primera entrada no es la del usuario)");
                foreach (var routine in routines)
                {
                    if (!app.WorkoutRest.StartRoutine(routine))
                    {
                        logger.Log(LogLevel.Error, "PERF", "no arrancó " + routine.Name);
                        Application.Quit(1);
                        return;
                    }

                    var toggles = Math.Max(1, seconds / 2 / 2);
                    for (var t = 0; t < toggles; t++)
                    {
                        await scheduler.Delay(TimeSpan.FromSeconds(2));
                        app.SeriesPanels.Toggle(t % 2 == 0 ? RowMate.Core.Workout.SeriesPanelKind.Laps : RowMate.Core.Workout.SeriesPanelKind.Reps);
                    }

                    await app.Workout.FinishAsync();
                    await scheduler.Delay(TimeSpan.FromSeconds(3));
                }

                logger.Log(LogLevel.Info, "PERF", "medición de los paneles: terminada");
                Application.Quit(0);
            }
            catch (Exception error)
            {
                logger.Log(LogLevel.Error, "PERF", "medición de los paneles: falla", error);
                Application.Quit(1);
            }
        }
#endif

        // Respaldo Overlay de ADR 019 (-rowmate-ui-pipeline Overlay): solo en desarrollo (QA-V2-09); el build normal siempre
        // usa la tubería por defecto.
        private static UiColorPipeline PipelineFromCommandLine(UiColorPipeline fallback)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var value = ArgumentValue(PipelineArgument);
            if (value == null)
            {
                return fallback;
            }

            return string.Equals(value, "Overlay", StringComparison.OrdinalIgnoreCase) ? UiColorPipeline.Overlay : UiColorPipeline.GammaRt;
#else
            return fallback;
#endif
        }

        internal static bool HasArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string? ArgumentValue(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
