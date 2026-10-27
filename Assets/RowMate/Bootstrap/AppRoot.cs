using System;
using System.Globalization;
using RowMate.App.Composition;
using RowMate.App.Shell;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Platform;
using RowMate.Platform.Logging;
using RowMate.Platform.Time;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Surface;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Bootstrap
{
    // Composition root (ARCHITECTURE §4.1, ADR 003; 02-tech-design §1.4). Único MonoBehaviour de Boot.unity.
    // Orden (QA-G2-01, medido en 6000.3.27f1): UIDocument tiene [DefaultExecutionOrder(-100)]; este componente corre antes
    // (−10000), así que en su Awake y su OnEnable rootVisualElement todavía es null. Awake configura (y le da al documento la
    // copia del panel, antes del OnEnable del documento); Start arma la UI, cuando la raíz ya existe.
    // Sin wakelock en la It. 2 (D-08): el punto de enganche es el paso 8 (F-APP-14, It. 4a). Ningún estático salvo BuildFlags.
    [DefaultExecutionOrder(-10000)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class AppRoot : MonoBehaviour
    {
        public const string Tag = "APP";
        public const string PipelineArgument = "-rowmate-ui-pipeline";
        public const string ColorProbeArgument = "-rowmate-color-probe";

        [SerializeField] private UiAssets? _assets;

        private IAppLogger? _logger;
        private UnityScheduler? _scheduler;
        private PlatformServices? _platform;
        private DeviceBackend? _backend;
        private AppContext? _app;
        private ShellHandle? _shell;
        private UiSurface? _surface;
        private UIDocument? _document;
        private FramePacer? _pacer;

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

            // 3. Reloj y sistema (idioma, accesibilidad, densidad, "Atrás").
            _scheduler = new UnityScheduler(logger);
            _platform = PlatformFactory.Create(logger);

            // 4. Remo y pulsómetro: el único #if ROWMATE_SIMULATOR (DeviceBackendFactory).
            _backend = DeviceBackendFactory.Create(_scheduler, logger);

            // 5. La app pura. Perfil apagado en la It. 2 (su regla real es de la It. 12, D-20).
            var texts = LocalizationSet.Load(_assets.StringsEs!.text, _assets.StringsEn!.text);
            _app = AppComposer.Compose(new AppServices(_backend, _scheduler, logger, texts, _platform.Locale, _platform.Accessibility,
                new ShellOptions(false)));

            // 6. Superficie: copia del panel (el asset no se toca, R2-05), RT limpia a bg.app y presentador (S-B1). El
            //    documento de la escena no tiene panel: se engancha a la copia en su OnEnable, que corre después.
            var pipeline = PipelineFromCommandLine(UiColorPipeline.GammaRt);
            var panel = Instantiate(_assets.Panel!);
            panel.name = _assets.Panel!.name + " (runtime)";
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

            if (HasArgument(ColorProbeArgument))
            {
                var probe = gameObject.AddComponent<ColorProbe>();
                probe.Run(root, _surface!, ArgumentValue(ColorProbeArgument) ?? "rowmate-color-probe.json", _logger!);
                return;
            }

            // 7. La UI (ShellHost). En un build normal SimulatorLayerFactory devuelve null (sin capa).
            _shell = ShellHost.Build(_app, root, new ShellHostOptions(_scheduler, _platform.IsDesktop, _platform.BackButton, _platform.Display,
                SimulatorLayerFactory.CreateOrNull(_backend, _app, _scheduler)) { Pipeline = _surface!.Pipeline });
            _pacer = new FramePacer(_shell.Pacing);

            // 8. El remo empieza a escuchar (el simulado se conecta) y el pulsómetro intenta su reconexión sin esperar
            //    (main.dart:68-79). Corre antes del primer Update: el scheduler no avanza antes de que la UI esté suscripta.
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

            // "Atrás" de Android llega como Escape del Input System (AC-APP-02-06).
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                _shell.HandleBack();
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

        // Orden inverso de lo que se haya creado (Start puede no haber corrido): sin avisarle nada al usuario (AC-APP-01-08).
        private void OnDestroy()
        {
            _shell?.Dispose();
            _shell = null;
            _app?.Dispose();
            _app = null;
            _backend?.Dispose();
            _backend = null;
            _surface?.Dispose();
            _surface = null;
        }

        private static UiColorPipeline PipelineFromCommandLine(UiColorPipeline fallback)
        {
            var value = ArgumentValue(PipelineArgument);
            if (value == null)
            {
                return fallback;
            }

            return string.Equals(value, "Overlay", StringComparison.OrdinalIgnoreCase) ? UiColorPipeline.Overlay : UiColorPipeline.GammaRt;
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
