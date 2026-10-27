using System;
using System.Collections;
using System.Collections.Generic;
using RowMate.App.Composition;
using RowMate.App.Shell;
using RowMate.Bootstrap;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Localization;
using RowMate.Core.Ports;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Device;
using RowMate.Presentation.Diagnostics;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;
using RowMate.Tests.Support;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using AppContext = RowMate.App.Composition.AppContext;
using Object = UnityEngine.Object;
#if ROWMATE_SIMULATOR
using RowMate.Simulator;
#endif

namespace RowMate.Tests.UiSupport
{
    // Host de UI determinista sobre una RT (02-tech-design §4.2, §5.1). Lo usan las pruebas PlayMode (con yield) y el arnés
    // de capturas en modo edición (con EditorApplication.update): misma UI, mismo PanelSettings y misma tubería de color que
    // la app. Arma la app con AppComposer + ShellHost, igual que AppRoot.Start, con FakeScheduler (hora local fija), idioma,
    // accesibilidad y densidad de prueba, y la fábrica de backend que se le pase.
    // El documento se crea con el patrón "GameObject inactivo → UIDocument → copia del panel → activo" (sonda §8.3): la raíz
    // existe al volver, en modo edición y en Play. Nunca modifica el asset del panel (R2-05).
    public sealed class UiTestHost : IDisposable
    {
        public const double FrameSeconds = 1.0 / 60.0;

        private readonly GameObject _host;
        private readonly PanelSettings _panel;
        private readonly HostLogger _logger;
        private RenderTexture _target;
        private bool _disposed;

        private UiTestHost(UiHostOptions options, GameObject host, PanelSettings panel, RenderTexture target, UIDocument document,
                           HostLogger logger, FakeScheduler scheduler, FakeDisplayMetrics display, FakeSystemLocale locale,
                           FakeAccessibilitySettings accessibility, DeviceBackend backend, AppContext app, ShellHandle handle,
                           IBackButtonPolicy backButton)
        {
            Options = options;
            _host = host;
            _panel = panel;
            _target = target;
            Document = document;
            _logger = logger;
            Scheduler = scheduler;
            Display = display;
            Locale = locale;
            Accessibility = accessibility;
            Backend = backend;
            App = app;
            Handle = handle;
            BackButton = backButton;
            Profile = options.Profile;
        }

        public UiHostOptions Options { get; }

        public DeviceProfile Profile { get; private set; }

        public FakeScheduler Scheduler { get; }

        public FakeDisplayMetrics Display { get; }

        public FakeSystemLocale Locale { get; }

        public FakeAccessibilitySettings Accessibility { get; }

        public AppContext App { get; }

        public DeviceBackend Backend { get; }

        public ShellHandle Handle { get; }

        public IBackButtonPolicy BackButton { get; }

        public UIDocument Document { get; }

        public PanelSettings Panel => _panel;

        public RenderTexture Target => _target;

        public RecordingLogger Logger => _logger.Recording;

        // La raíz del documento (la "ventana").
        public VisualElement Root => Document.rootVisualElement;

        public AppShellView Shell => Handle.Shell;

        public DeviceScreenView? DeviceScreen => Handle.Shell.DeviceScreen;

        public HeartRateCardView? HeartRateCard => DeviceScreen?.Card;

        public ControllableRowerService? TestRower => Backend.Rower as ControllableRowerService;

        public ScriptedHeartRateService? TestHeartRate => Backend.HeartRate as ScriptedHeartRateService;

#if ROWMATE_SIMULATOR
        public SimulatorDeviceBackend? Simulator => Backend as SimulatorDeviceBackend;

        public RowMate.Simulator.Presentation.SimulatorLayerView? SimulatorLayer =>
            Handle.Shell.TopLayer as RowMate.Simulator.Presentation.SimulatorLayerView;
#endif

        public static UiTestHost Create(UiHostOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logger = new HostLogger();
            var scheduler = new FakeScheduler(options.StartUtc, options.LocalOffset);
            var backend = options.Backend(scheduler, logger);
            var display = new FakeDisplayMetrics(options.Profile.Density, options.Insets);
            var tags = new string[options.LanguageTags.Count];
            for (var i = 0; i < tags.Length; i++)
            {
                tags[i] = options.LanguageTags[i];
            }

            var locale = new FakeSystemLocale(tags);
            var accessibility = new FakeAccessibilitySettings(options.TextScale, options.ReduceMotion, logger);
            var texts = LocalizationSet.Load(UiAssetsLocator.StringsEs(), UiAssetsLocator.StringsEn());
            var app = AppComposer.Compose(new AppServices(backend, scheduler, logger, texts, locale, accessibility,
                new ShellOptions(options.ProfileTab)));

            var target = CreateTarget(options.Profile, options.Pipeline);
            var panel = Object.Instantiate(UiAssetsLocator.Panel());
            panel.name = "RowMate.TestPanel";
            panel.hideFlags = HideFlags.HideAndDontSave;
            Configure(panel, target, options.Profile, options.Pipeline);

            var host = new GameObject("RowMate.UiTestHost") { hideFlags = HideFlags.DontSave };
            host.SetActive(false);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panel;
            host.SetActive(true);
            var root = document.rootVisualElement;
            if (root == null)
            {
                throw new InvalidOperationException("UiTestHost: el UIDocument no tiene raíz después de activarlo.");
            }

            var backButton = options.BackButton ?? new CountingBackButtonPolicy();
            var handle = ShellHost.Build(app, root, new ShellHostOptions(scheduler, options.IsDesktop, backButton, display,
                TopLayerFor(options, backend, app, scheduler)) { Pipeline = options.Pipeline });
            var result = new UiTestHost(options, host, panel, target, document, logger, scheduler, display, locale, accessibility, backend, app,
                handle, backButton);
            if (options.StartApp)
            {
                app.Start();
            }

            handle.Tick();
            return result;
        }

        // ── Tiempo ────────────────────────────────────────────────────────────

        // Avanza el reloj (timers del backend incluidos) y corre un Tick del shell (animaciones y área segura).
        public void Advance(TimeSpan by)
        {
            Scheduler.Advance(by);
            Handle.Tick();
        }

        public void Advance(double seconds)
        {
            Advance(TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond)));
        }

        // Un cuadro de 1/60 s.
        public void Step()
        {
            Advance(FrameSeconds);
        }

        // = pumpAndSettle: de a 1/60 s hasta que no haya animaciones finitas (latido y spinner no cuentan). Tope 2 s.
        public void Settle(TimeSpan? max = null)
        {
            var limit = (max ?? TimeSpan.FromSeconds(2)).TotalSeconds;
            var elapsed = 0.0;
            Handle.Tick();
            while (Handle.Motion.HasFiniteMotions && elapsed < limit)
            {
                Step();
                elapsed += FrameSeconds;
            }
        }

        // Cuadros reales de Unity (PlayMode: yield de un cuadro; arnés: un EditorApplication.update) para layout y dibujo.
        public IEnumerator Frames(int count = 2)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        // Settle con cuadros reales entre pasos (cuando una animación depende del layout del cuadro siguiente).
        public IEnumerator SettleFrames(double maxSeconds = 2)
        {
            var elapsed = 0.0;
            yield return null;
            while (Handle.Motion.HasFiniteMotions && elapsed < maxSeconds)
            {
                Step();
                elapsed += FrameSeconds;
                yield return null;
            }

            yield return null;
        }

        public bool HasFiniteMotions => Handle.Motion.HasFiniteMotions;

        public bool IsHeartAtRest()
        {
            var card = HeartRateCard;
            return card == null || card.Heartbeat.IsAtRest(Scheduler.MonotonicSeconds);
        }

        // ── Toques (QA-G2-07) ─────────────────────────────────────────────────

        // = tester.tap con hit-test: el centro de worldBound tiene que estar dentro de la ventana y panel.Pick(centro) tiene
        // que ser el elemento o un descendiente; si no, falla con "fuera de la ventana" o "tapado por #<nombre>". Recién entonces
        // manda NavigationSubmitEvent (un elemento deshabilitado no responde).
        public void Tap(string elementName)
        {
            TapElement(FindRequired(elementName), elementName);
        }

        public void TapText(string text)
        {
            var element = UiQuery.FindByText(Root, text) ?? throw new InvalidOperationException("Tap: no hay ningún elemento con el texto \"" + text + "\".");
            var target = ActionableOf(element);
            TapElement(target, "\"" + text + "\"");
        }

        // Sin hit-test: solo para lo que el legacy hace por API (hoy, nada).
        public void TapUnchecked(string elementName)
        {
            Submit(FindRequired(elementName));
        }

        // Motivo por el que un toque fallaría (null = se puede tocar).
        public string? WhyNotTappable(string elementName)
        {
            var element = Find(elementName);
            return element == null ? "no existe #" + elementName : HitTestProblem(element);
        }

        // = ensureVisible: desplaza el ScrollView ancestro hasta que el elemento quede entero en la zona visible NO tapada
        // (viewport ∩ por encima de #tab-bar). ScrollView.ScrollTo no alcanza: deja el elemento al pie del viewport, debajo de
        // la barra absoluta (sonda §8.3).
        public void ScrollTo(string elementName)
        {
            var element = FindRequired(elementName);
            var scroll = UiQuery.ScrollViewOf(element) ?? throw new InvalidOperationException("ScrollTo: #" + elementName + " no está en un ScrollView.");
            var viewport = scroll.contentViewport.worldBound;
            var top = viewport.yMin;
            var bottom = viewport.yMax;
            var tabBar = Root.Q("tab-bar");
            if (tabBar != null && tabBar.worldBound.yMin < bottom && tabBar.worldBound.yMax > top)
            {
                bottom = Mathf.Min(bottom, tabBar.worldBound.yMin);
            }

            var bounds = element.worldBound;
            var offset = scroll.scrollOffset;
            const float margin = 8f;
            if (bounds.yMax > bottom)
            {
                offset.y += bounds.yMax - bottom + margin;
            }
            else if (bounds.yMin < top)
            {
                offset.y -= top - bounds.yMin + margin;
            }

            if (offset.y < 0)
            {
                offset.y = 0;
            }

            scroll.scrollOffset = offset;
        }

        public void ScrollToEnd(string scrollViewName)
        {
            var scroll = Root.Q<ScrollView>(scrollViewName) ?? throw new InvalidOperationException("ScrollToEnd: no existe #" + scrollViewName);
            scroll.scrollOffset = new Vector2(0, float.MaxValue);
        }

        public float ScrollOffsetOf(string scrollViewName)
        {
            var scroll = Root.Q<ScrollView>(scrollViewName);
            return scroll == null ? 0 : scroll.scrollOffset.y;
        }

        // ── Pantalla y sistema ───────────────────────────────────────────────

        // Recrea la RT con otro tamaño sin rearmar la UI (AC-APP-02-07).
        public void Resize(DeviceProfile profile)
        {
            var previous = _target;
            _target = CreateTarget(profile, Options.Pipeline);
            Configure(_panel, _target, profile, Options.Pipeline);
            Display.DpScale = profile.Density;
            Profile = profile;
            DestroyTarget(previous);
            Handle.Tick();
        }

        public void SetInsets(SafeInsets insets)
        {
            Display.SafeArea = insets;
            Handle.Tick();
        }

        public void SetLanguage(params string[] tags)
        {
            Locale.Set(tags);
            App.RefreshSystemSettings(tags);
            Handle.Tick();
        }

        public void SetAccessibility(double textScale, bool reduceMotion)
        {
            Accessibility.Set(textScale, reduceMotion);
            Handle.Tick();
        }

        // ── Consultas ─────────────────────────────────────────────────────────

        public VisualElement? Find(string elementName)
        {
            return Root.Q(elementName);
        }

        public T? Find<T>(string elementName)
            where T : VisualElement
        {
            return Root.Q<T>(elementName);
        }

        public string? TextOf(string elementName)
        {
            return UiQuery.TextOf(Find(elementName));
        }

        public bool IsTextVisible(string text)
        {
            return UiQuery.IsTextVisible(Root, text);
        }

        public bool HasText(string text)
        {
            return UiQuery.HasText(Root, text);
        }

        public bool IsShown(string elementName)
        {
            var element = Find(elementName);
            return element != null && UiQuery.IsShown(element) && UiQuery.IsInside(element, Root.worldBound);
        }

        public Rect BoundsOf(string elementName)
        {
            return FindRequired(elementName).worldBound;
        }

        public Texture2D ReadPixels(Rect? cropPx = null)
        {
            RectInt? crop = null;
            if (cropPx.HasValue)
            {
                var r = cropPx.Value;
                crop = new RectInt(Mathf.FloorToInt(r.x), Mathf.FloorToInt(r.y), Mathf.CeilToInt(r.width), Mathf.CeilToInt(r.height));
            }

            return RtReader.Read(_target, crop);
        }

        public LayoutAuditResult Audit()
        {
            return LayoutAudit.Run(Root);
        }

        public bool HasErrors()
        {
            for (var i = 0; i < Logger.Levels.Count; i++)
            {
                if (Logger.Levels[i] == LogLevel.Error)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Handle.Dispose();
            App.Dispose();
            Backend.Dispose();
            if (_host != null)
            {
                DestroyObject(_host);
            }

            if (_panel != null)
            {
                _panel.targetTexture = null;
                DestroyObject(_panel);
            }

            DestroyTarget(_target);
        }

        // ── Internos ──────────────────────────────────────────────────────────

        private static TopLayerFactory? TopLayerFor(UiHostOptions options, DeviceBackend backend, AppContext app, IClock clock)
        {
            switch (options.SimulatorLayer)
            {
                case SimulatorLayerMode.Full:
                {
                    var factory = SimulatorLayerFactory.CreateOrNull(backend, app, clock);
                    if (factory == null)
                    {
                        throw new InvalidOperationException("UiTestHost: SimulatorLayerMode.Full exige el backend del simulador (y ROWMATE_SIMULATOR).");
                    }

                    return factory;
                }

                case SimulatorLayerMode.ChromeOnly:
#if ROWMATE_SIMULATOR
                    return SimulatorLayerFactory.CreateChromeOnly();
#else
                    throw new InvalidOperationException("UiTestHost: la capa del simulador necesita ROWMATE_SIMULATOR.");
#endif
                default:
                    return null;
            }
        }

        private static RenderTexture CreateTarget(DeviceProfile profile, UiColorPipeline pipeline)
        {
            var format = pipeline == UiColorPipeline.GammaRt ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB;
            return UiSurface.CreateTarget(profile.WidthPx, profile.HeightPx, format, "RowMate.UiTestHost." + profile.Code);
        }

        private static void Configure(PanelSettings panel, RenderTexture target, DeviceProfile profile, UiColorPipeline pipeline)
        {
            panel.targetTexture = target;
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.scale = (float)profile.Density;
            panel.forceGammaRendering = pipeline == UiColorPipeline.GammaRt;
            panel.clearColor = true;
            panel.colorClearValue = UiSurface.ClearValue(pipeline);
        }

        private static void DestroyTarget(RenderTexture? target)
        {
            if (target == null)
            {
                return;
            }

            target.Release();
            DestroyObject(target);
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        private VisualElement FindRequired(string elementName)
        {
            return Find(elementName) ?? throw new InvalidOperationException("no existe #" + elementName);
        }

        private void TapElement(VisualElement element, string what)
        {
            var problem = HitTestProblem(element);
            if (problem != null)
            {
                throw new InvalidOperationException("Tap " + what + ": " + problem);
            }

            Submit(element);
        }

        private string? HitTestProblem(VisualElement element)
        {
            var window = Root.worldBound;
            var center = element.worldBound.center;
            if (!(element.worldBound.width > 0) || !window.Contains(center))
            {
                return "fuera de la ventana (centro " + center + " en " + window + ")";
            }

            var picked = Root.panel.Pick(center);
            if (picked == null)
            {
                return "tapado o recortado (Pick devolvió nada en " + center + ")";
            }

            if (picked != element && !element.Contains(picked))
            {
                return "tapado por " + UiQuery.Describe(picked);
            }

            return null;
        }

        // El accionable (Button o Toggle) que contiene a un texto.
        private static VisualElement ActionableOf(VisualElement element)
        {
            for (var e = element; e != null; e = e.hierarchy.parent)
            {
                if (e is Button || e is Toggle)
                {
                    return e;
                }
            }

            return element;
        }

        private static void Submit(VisualElement element)
        {
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = element;
                element.SendEvent(submit);
            }
        }

        // Registra todo (los tests miran Logger) y repite errores y advertencias en el registro de Unity: un Error no esperado
        // hace fallar la prueba en el Test Framework.
        private sealed class HostLogger : IAppLogger
        {
            public RecordingLogger Recording { get; } = new RecordingLogger();

            public void Log(LogLevel level, string tag, string message, Exception? error = null)
            {
                Recording.Log(level, tag, message, error);
                if (level == LogLevel.Error)
                {
                    Debug.LogError("[" + tag + "] " + message + (error != null ? " (" + error.GetType().Name + ": " + error.Message + ")" : ""));
                }
                else if (level == LogLevel.Warning)
                {
                    Debug.LogWarning("[" + tag + "] " + message);
                }
            }
        }
    }
}
