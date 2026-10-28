using System;
using System.Globalization;
using System.IO;
using RowMate.App.Composition;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Infrastructure.Database;
using RowMate.Infrastructure.Routines;
using RowMate.Infrastructure.Threading;
using RowMate.Platform;
using RowMate.Platform.Logging;
using RowMate.Platform.Time;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;
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
    // Sin wakelock todavía (D-08): el punto de enganche es el paso 8 (F-APP-14, It. 4a). Ningún estático salvo BuildFlags.
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
        public const string RoundTripName = "Ida y vuelta";
#endif

        [SerializeField] private UiAssets? _assets;

        private IAppLogger? _logger;
        private UnityScheduler? _scheduler;
        private PlatformServices? _platform;
        private DeviceBackend? _backend;
        private SqliteRoutineRepository? _routines;
        private AppContext? _app;
        private ShellHandle? _shell;
        private UiSurface? _surface;
        private UIDocument? _document;
        private FramePacer? _pacer;
        private BackSource _backSource;

        public UiAssets? Assets => _assets;

        public UiSurface? Surface => _surface;

        public ShellHandle? Shell => _shell;

        public AppContext? App => _app;

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
            _scheduler = new UnityScheduler(logger);
            _platform = PlatformFactory.Create(logger);
            _backSource = Application.platform == RuntimePlatform.Android ? BackSource.SystemBack : BackSource.Escape;

            // Barra de título de Windows en el tema del sistema desde el primer cuadro (legacy win32_window.cpp:147, QA-V2-08);
            // al recuperar el foco la vuelve a aplicar PlatformServices.Refresh.
            _platform.TitleBar.Apply();

            // 4. Remo y pulsómetro: el único #if ROWMATE_SIMULATOR (DeviceBackendFactory).
            _backend = DeviceBackendFactory.Create(_scheduler, logger);

            // 4b. Base local (F-HIS-17, F-HIS-20): una por variante (rower_app.db o rower_app_dev.db), en la carpeta privada de
            //     la app (03-rutinas/02-tech-design §2.2, §2.4). El ÚNICO lugar del código que elige el archivo. Se abre en el
            //     primer uso (la carga del arranque), en el hilo de la base (SerialExecutor); ninguna ruta va al registro.
            var file = DatabaseFiles.NameFor(BuildFlags.IsSimulator);
            var dbPath = Path.Combine(DataLocation.DatabaseDirectory(_platform), file);
            _routines = new SqliteRoutineRepository(new RowMateDatabase(dbPath, new SerialExecutor("RowMate.Db", logger), logger), logger);
            logger.Log(LogLevel.Info, Tag, "base " + file + (DataLocation.IsOverridden ? " (carpeta de desarrollo)" : ""));

            // 5. La app pura. Perfil apagado (su regla real es de la It. 12, D-20).
            var texts = LocalizationSet.Load(_assets.StringsEs!.text, _assets.StringsEn!.text);
            _app = AppComposer.Compose(new AppServices(_backend, _routines, _scheduler, logger, texts, _platform.Locale, _platform.Accessibility,
                new ShellOptions(false)));

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

            // 7. La UI (ShellHost). En un build normal SimulatorLayerFactory devuelve null (sin capa).
            _shell = ShellHost.Build(_app, root, new ShellHostOptions(_scheduler, _platform.IsDesktop, _platform.BackButton, _platform.Display,
                SimulatorLayerFactory.CreateOrNull(_backend, _app, _scheduler), _platform.Clipboard, _platform.Keyboard) { Pipeline = _surface!.Pipeline });
            _pacer = new FramePacer(_shell.Pacing);

            // 8. El remo empieza a escuchar (el simulado se conecta), las rutinas se cargan sin esperar (AC-APP-01-09) y el
            //    pulsómetro intenta su reconexión (main.dart:68-83). Corre antes del primer Update: el scheduler no avanza antes
            //    de que la UI esté suscripta.
            _app.Start();
        }

        private void Update()
        {
            _scheduler?.Tick();
            _surface?.Update();
            if (_shell == null)
            {
                return;
            }

            _shell.Tick();
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

        // Al recuperar el foco: idioma, letra y movimiento del sistema (AC-APP-09-05).
        private void OnApplicationFocus(bool focus)
        {
            if (!focus || _platform == null || _app == null)
            {
                return;
            }

            _platform.Refresh();
            _app.RefreshSystemSettings(_platform.Locale.PreferredLanguageTags);
        }

        // Orden inverso de lo que se haya creado (Start puede no haber corrido), sin avisarle nada al usuario (AC-APP-01-08):
        // shell → app (stores) → backend → repositorio y ejecutor de la base → superficie. El ejecutor termina lo encolado: un
        // guardado que ya cerró el editor ya hizo commit (el editor espera el commit, AC-RUT-08-07).
        private void OnDestroy()
        {
            _shell?.Dispose();
            _shell = null;
            _app?.Dispose();
            _app = null;
            _backend?.Dispose();
            _backend = null;
            _routines?.Dispose();
            _routines = null;
            _surface?.Dispose();
            _surface = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
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
