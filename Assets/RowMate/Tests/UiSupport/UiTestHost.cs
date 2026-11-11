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
using RowMate.Presentation.Routines;
using RowMate.Presentation.Shell;
using RowMate.Presentation.Surface;
using RowMate.Presentation.Workout;
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
    // Host de UI determinista sobre una RT (02-tech-design §4.2, §5.1 de la It. 2; 03-rutinas/02-tech-design §5.2). Lo usan las
    // pruebas PlayMode (con yield) y el arnés de capturas en modo edición (con EditorApplication.update): misma UI, mismo
    // PanelSettings (copia hecha por RuntimePanelFactory con los respaldos de fuente del MISMO UiAssets.asset que la app,
    // QA-G3-12) y misma tubería de color que la app. Arma la app con AppComposer + ShellHost, igual que AppRoot.Start, con
    // FakeScheduler (hora local fija), idioma, accesibilidad y densidad de prueba, la fábrica de backend y la de rutinas que se
    // le pasen (SQLite real en una carpeta temporal, de guion o en memoria), un SequenceClock para las fechas de creación,
    // un FakeClipboard y un FakeSoftKeyboard. Nunca usa la carpeta de datos del usuario (AC-HIS-20-04).
    // El documento se crea con el patrón "GameObject inactivo → UIDocument → copia del panel → activo" (sonda §8.3): la raíz
    // existe al volver, en modo edición y en Play. Nunca modifica el asset del panel (R2-05).
    // It. 4a (02-tech-design §7.1): las sesiones del motor (por defecto en el mismo archivo que las rutinas), un ciclo de vida
    // de prueba por host (FakeAppLifecycle: sin pedido, adelante y con foco, también sin ventana: spec §0.4), el segundo plano
    // con alcance (SendToBackground / BringToForeground, en el orden de AppRoot: IsCatchingUp mientras dura y InBackground =
    // false recién al final, QA-G4aT-07), el foco (Unfocus / Focus) y los accesos a Workout y al inmersivo.
    public sealed class UiTestHost : IDisposable
    {
        public const double FrameSeconds = 1.0 / 60.0;

        private readonly GameObject _host;
        private readonly PanelSettings _panel;
        private readonly HostLogger _logger;
        private readonly IBackButtonPolicy _backButton;
        private RenderTexture _target;
        private bool _disposed;

        private UiTestHost(UiHostOptions options, GameObject host, PanelSettings panel, RenderTexture target, UIDocument document,
                           HostLogger logger, FakeScheduler scheduler, FakeDisplayMetrics display, FakeSystemLocale locale,
                           FakeAccessibilitySettings accessibility, IBackButtonPolicy backButton, HostRoutineSet routines, IClock creationClock,
                           IClipboard clipboard, FakeSoftKeyboard keyboard)
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
            _backButton = backButton;
            Routines = routines;
            CreationClock = creationClock;
            Clipboard = clipboard;
            Keyboard = keyboard;
            Lifecycle = new FakeAppLifecycle(logger);
            SystemUi = new FakeSystemUi(scheduler);
            Profile = options.Profile;
            Backend = null!;
            App = null!;
            Handle = null!;
            Sessions = null!;
        }

        public UiHostOptions Options { get; }

        public DeviceProfile Profile { get; private set; }

        public FakeScheduler Scheduler { get; }

        public FakeDisplayMetrics Display { get; }

        public FakeSystemLocale Locale { get; }

        public FakeAccessibilitySettings Accessibility { get; }

        public AppContext App { get; private set; }

        public DeviceBackend Backend { get; private set; }

        public ShellHandle Handle { get; private set; }

        public IBackButtonPolicy BackButton => _backButton;

        // Las rutinas del host (la base temporal, el repositorio de guion o el de memoria).
        public HostRoutineSet Routines { get; }

        public IClock CreationClock { get; }

        public IClipboard Clipboard { get; }

        public FakeClipboard? TestClipboard => Clipboard as FakeClipboard;

        public FakeSoftKeyboard Keyboard { get; }

        // Veces que se armó la app sobre la misma base (Reopen).
        public int Generation { get; private set; }

        public UIDocument Document { get; }

        public PanelSettings Panel => _panel;

        public RenderTexture Target => _target;

        public RecordingLogger Logger => _logger.Recording;

        // La raíz del documento (la "ventana").
        public VisualElement Root => Document.rootVisualElement;

        public AppShellView Shell => Handle.Shell;

        public DeviceScreenView? DeviceScreen => Handle.Shell.DeviceScreen;

        public HeartRateCardView? HeartRateCard => DeviceScreen?.Card;

        public RoutinesScreenView? RoutinesScreen => Handle.Shell.RoutinesScreen;

        // El editor de arriba (la página que no está saliendo), si hay.
        public RoutineEditorPage? Editor => Handle.Shell.Pages.Top as RoutineEditorPage;

        public ModalDialog? TopDialog => Handle.Shell.Dialogs.Top;

        public ControllableRowerService? TestRower => Backend.Rower as ControllableRowerService;

        public ScriptedHeartRateService? TestHeartRate => Backend.HeartRate as ScriptedHeartRateService;

        public ScriptedRoutineRepository? ScriptedRoutines => Routines.Scripted;

        // ── It. 4a ──

        // Las sesiones del motor (el LocalStore de las rutinas, en memoria o de guion).
        public ISessionRepository Sessions { get; private set; }

        public InMemorySessionRepository? MemorySessions => Sessions as InMemorySessionRepository;

        public ScriptedSessionRepository? ScriptedSessions => Sessions as ScriptedSessionRepository;

        // El ciclo de vida de prueba del host (spec §0.4).
        public FakeAppLifecycle Lifecycle { get; }

        // It. 4b (F-APP-15; 04b-hud-exr/02-tech-design §5.2 punto 6): la interfaz del sistema de prueba del host (anota cada pedido
        // con su instante; ninguna prueba ni toma llama al sistema).
        public FakeSystemUi SystemUi { get; }

        // It. 4b: el diálogo de rutina completada abierto (null si no hay).
        public CompletionDialog? Completion => Immersive?.Completion;

        // It. 4b (QA-G4bT-11 (d)): abre el diálogo de completado con los totales de un caso, sin correr la rutina.
        public CompletionDialog OpenCompletion(RowMate.App.Workout.HudCompletion completion)
        {
            var page = Immersive ?? throw new InvalidOperationException("OpenCompletion: no hay inmersivo.");
            var dialog = page.OpenCompletionForTest(completion);
            TickAll();
            return dialog;
        }

        // It. 4b (§2.8, §5.2): la zona horaria y la hora del equipo.
        public void SetLocalOffset(TimeSpan offset)
        {
            Scheduler.SetLocalOffset(offset);
            TickAll();
        }

        public void ShiftWallClock(TimeSpan by)
        {
            Scheduler.ShiftUtc(by);
            TickAll();
        }

        // El remo de prueba de la It. 4a (HostBackends.TestRower), si es el del host.
        public ScriptedRower? ScriptedRower => Backend.Rower as ScriptedRower;

        // It. 10a: el backend con el remo real sobre el adaptador falso (HostBackends.FakeBle), o null.
        public FakeBleBackend? FakeBle => Backend as FakeBleBackend;

        public WorkoutScreenView? WorkoutScreen => Handle.Shell.WorkoutScreen;

        // El inmersivo de arriba (la página que no está saliendo), si hay.
        public ImmersiveWorkoutPage? Immersive => Handle.Shell.Pages.Top as ImmersiveWorkoutPage;

        // El estado del HUD del inmersivo de arriba (null sin inmersivo).
        public RowMate.App.Workout.WorkoutHudState? HudState => Immersive?.Hud.State.Value;

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
            var display = new FakeDisplayMetrics(options.Profile.Density, options.Insets);
            var tags = new string[options.LanguageTags.Count];
            for (var i = 0; i < tags.Length; i++)
            {
                tags[i] = options.LanguageTags[i];
            }

            var locale = new FakeSystemLocale(tags);
            var accessibility = new FakeAccessibilitySettings(options.TextScale, options.ReduceMotion, logger);
            var routines = options.Routines(scheduler, logger);
            var creationClock = options.CreationClock ?? new SequenceClock();
            var clipboard = options.Clipboard ?? new FakeClipboard();
            var keyboard = new FakeSoftKeyboard();

            var target = CreateTarget(options.Profile, options.Pipeline);
            var panel = RuntimePanelFactory.Create(UiAssetsLocator.Panel(), UiAssetsLocator.IconsOutlineFallback(), UiAssetsLocator.EmojiFallback(),
                "RowMate.TestPanel");
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
                routines.Dispose();
                throw new InvalidOperationException("UiTestHost: el UIDocument no tiene raíz después de activarlo.");
            }

            var backButton = options.BackButton ?? new CountingBackButtonPolicy();
            var result = new UiTestHost(options, host, panel, target, document, logger, scheduler, display, locale, accessibility, backButton,
                routines, creationClock, clipboard, keyboard);
            if (options.SceneUrpTier.HasValue)
            {
                // QA-V6a-REN-16: también lo del asset de URP del nivel (sombras, MSAA), restaurado en Dispose.
                result._tierScope = new TierScope(options.SceneUrpTier.Value);
            }

            result.Compose(routines.Repository);
            return result;
        }

        // Reabrir la app sobre la MISMA base (DEMO-10, AC-RUT-08-09, RPS.ReopenShowsTheSame): dispone la UI, la app y el
        // backend, cierra la base y la vuelve a abrir sobre el mismo archivo, y arma todo de nuevo (sin tocar el reloj).
        public void Reopen()
        {
            Handle.Dispose();
            App.Dispose();
            Backend.Dispose();
            var repository = Routines.Reopen(_logger);
            Generation++;
            Compose(repository);
        }

        private void Compose(IRoutineRepository repository)
        {
            var options = Options;
            Backend = options.Backend(Scheduler, _logger);
            Sessions = options.Sessions(Scheduler, _logger, Routines);
            var texts = LocalizationSet.Load(UiAssetsLocator.StringsEs(), UiAssetsLocator.StringsEn());
            App = AppComposer.Compose(new AppServices(Backend, repository, Sessions, Scheduler, _logger, texts, Locale, Accessibility,
                new ShellOptions(options.ProfileTab))
            {
                CreationClock = CreationClock, Lifecycle = Lifecycle, SystemUi = SystemUi, Preferences = PreferencesFor(options),
                Exporter = ExporterFor(options), ExportZone = options.ExportZone ?? (() => options.LocalZone), SimulatorBuild = options.SimulatorBuild,
                Haptics = options.Haptics,
            });
            _sceneBridge?.Dispose();
            _sceneBridge = options.Scene == SceneMode.Real ? CreateScene(options) : null;
            Handle = ShellHost.Build(App, Root, new ShellHostOptions(Scheduler, options.IsDesktop, _backButton, Display,
                TopLayerFor(options, Backend, App, Scheduler), Clipboard, Keyboard)
            {
                Pipeline = options.Pipeline, BoatZoneOutline = options.BoatZoneOutline, Scene = options.FakeScene ?? _sceneBridge,
                Thumbnails = UiAssetsLocator.Thumbnails(),
                BootSplash = options.BootSplash, BootMark = options.BootSplash ? UiAssetsLocator.BootMark() : null,
            });
            Handle.Shell.Ui.TextMenuTouchMode = options.TextMenuTouchMode;
            // It. 14 (DT14-22): el lector de pantalla forzado encendido mientras dura el anfitrión, con el puente y el anunciador falso.
            ScreenReader?.Dispose();
            ScreenReader = null;
            if (options.ScreenReader)
            {
                if (!_screenReaderForced)
                {
                    _screenReaderForced = true;
                    UnityEngine.Accessibility.AssistiveSupport.screenReaderStatusOverride = UnityEngine.Accessibility.AssistiveSupport.ScreenReaderStatusOverride.ForceEnabled;
                }

                ScreenReader = new RowMate.Presentation.Accessibility.ScreenReaderBridge(Handle.Shell, Announcer);
            }

            if (options.StartApp)
            {
                App.Start();
            }

            TickAll();
        }

        // ── It. 12: el exportador de "Exportar TCX" (12-exportar-tcx/02-tech-design §3.5, §6.2) ──

        // El exportador de la app de este anfitrión (el de UiHostOptions o el falso propio).
        public IFileExporter Exporter { get; private set; } = null!;

        // El falso propio (o el de UiHostOptions si es uno): Hold, FailNext, Requests, Reveals y Files.
        public FakeFileExporter? FakeExporter => Exporter as FakeFileExporter;

        // La carpeta temporal del falso propio (null si las opciones traen otro exportador).
        public string? ExportFolder { get; private set; }

        private IFileExporter ExporterFor(UiHostOptions options)
        {
            if (options.Exporter != null)
            {
                Exporter = options.Exporter;
                return Exporter;
            }

            // Reopen = el mismo falso (y su carpeta): los archivos ya exportados siguen ahí.
            if (Exporter is FakeFileExporter && ExportFolder != null)
            {
                return Exporter;
            }

            ExportFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RowMate.Tests", "exports-" + Guid.NewGuid().ToString("N"));
            Exporter = new FakeFileExporter(ExportFolder, null, options.FakeExportDelivery);
            return Exporter;
        }

        // ── It. 5: las preferencias (05-series-parciales/02-tech-design §2.7, §6.2; AC-HIS-20-04) ──

        // Las preferencias de la app de este anfitrión (las de UiHostOptions, una MemoryPreferences propia o el archivo temporal).
        public IPreferences Preferences { get; private set; } = null!;

        // El archivo temporal de las preferencias (con UiHostOptions.PreferencesFile), o null.
        public string? PreferencesPath { get; private set; }

        private MemoryPreferences? _memoryPreferences;
        private RowMate.Infrastructure.Preferences.JsonFilePreferences? _filePreferences;
        private string? _preferencesFolder;

        private IPreferences PreferencesFor(UiHostOptions options)
        {
            if (options.Preferences != null)
            {
                Preferences = options.Preferences;
                return Preferences;
            }

            if (options.PreferencesFile)
            {
                // Reopen = el reinicio: lo pendiente se termina de guardar y el archivo se vuelve a leer.
                ClosePreferencesFile();
                _preferencesFolder ??= System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RowMate.Tests", "prefs-" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(_preferencesFolder);
                PreferencesPath = System.IO.Path.Combine(_preferencesFolder, RowMate.Infrastructure.Preferences.PreferencesFiles.Name);
                _filePreferences = new RowMate.Infrastructure.Preferences.JsonFilePreferences(PreferencesPath, _logger);
                Preferences = _filePreferences;
                return Preferences;
            }

            _memoryPreferences ??= new MemoryPreferences();
            Preferences = _memoryPreferences;
            return Preferences;
        }

        // Espera lo pendiente del archivo temporal (no hace nada en memoria).
        public void FlushPreferences()
        {
            _filePreferences?.FlushAsync().Wait(TimeSpan.FromSeconds(5));
        }

        private void ClosePreferencesFile()
        {
            if (_filePreferences == null)
            {
                return;
            }

            _filePreferences.FlushAsync().Wait(TimeSpan.FromSeconds(5));
            _filePreferences.Dispose();
            _filePreferences = null;
        }

        // It. 5 (S-LARGO, LT-59 a LT-61; 05 §5.1 punto 9, §6.4): el contenido de los paneles armado a mano o con ComposeSeries, en el
        // acto, hasta OverrideSeries(null) (como la fila de 1998 píldoras de la 4b). instant = false: con la animación de un dato
        // del motor (el empuje, la fila nueva; LM5-01 de la verificación de la 5).
        public void OverrideSeries(RowMate.App.Workout.HudSeries? series, bool instant = true)
        {
            var column = SeriesColumn ?? throw new InvalidOperationException("OverrideSeries: no hay paneles de series (sin inmersivo con rutina).");
            column.OverrideSeries(series, instant);
            TickAll();
        }

        // La columna de los paneles del inmersivo de arriba (null sin rutina o sin HUD).
        public SeriesColumn? SeriesColumn => Immersive != null && Immersive.HasView ? Immersive.View.Series : null;

        // ── Tiempo ────────────────────────────────────────────────────────────

        // Avanza el reloj (timers del backend incluidos) y corre un Tick del shell (animaciones y área segura).
        public void Advance(TimeSpan by)
        {
            Scheduler.Advance(by);
            TickAll();
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

        // = pumpAndSettle: de a 1/60 s hasta que no haya animaciones finitas (latido y spinner no cuentan). Tope 2 s. Si alguna
        // espera el layout del cuadro siguiente (lo nuevo que crece), corta sin gastar reloj: el que llama deja pasar un cuadro
        // real y vuelve a llamar (UiTestBase.Settle, ScenarioScript.Settle). Devuelve el reloj que avanzó.
        public double Settle(TimeSpan? max = null)
        {
            var limit = (max ?? TimeSpan.FromSeconds(2)).TotalSeconds;
            var elapsed = 0.0;
            TickAll();
            while (Handle.Motion.HasFiniteMotions && elapsed < limit && !Handle.Motion.WaitingForLayout)
            {
                Step();
                elapsed += FrameSeconds;
            }

            return elapsed;
        }

        // Alguna animación espera el layout del cuadro siguiente.
        public bool WaitingForLayout => Handle.Motion.WaitingForLayout;

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

        // ── Ciclo de vida (It. 4a, §7.1) ─────────────────────────────────────

        // La app pasa a segundo plano (Android e iOS): el motor manda lo pendiente; ningún cuadro hasta BringToForeground.
        public void SendToBackground()
        {
            Lifecycle.SetBackground(true);
        }

        // Vuelve después de `seconds` en segundo plano, en el orden de AppRoot (§4.2): el alcance (con FakeScheduler, Advance ya
        // dispara todo en orden) con IsCatchingUp mientras dura, y recién al final InBackground = false: el motor publica UNA
        // foto con Instant y Workout muestra el aviso retenido. Después, un cuadro.
        public void BringToForeground(double seconds)
        {
            Scheduler.IsCatchingUp = true;
            try
            {
                Scheduler.Advance(TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond)));
            }
            finally
            {
                Scheduler.IsCatchingUp = false;
            }

            Lifecycle.SetBackground(false);
            TickAll();
        }

        // La ventana sin foco o minimizada (Windows: el loop sigue y el reloj también).
        public void Unfocus()
        {
            Lifecycle.SetFocus(false);
            TickAll();
        }

        public void Focus()
        {
            Lifecycle.SetFocus(true);
            TickAll();
        }

        // Toca la tarjeta de Workout de la rutina con ese nombre (con hit-test).
        public void TapRoutineNamed(string routineName)
        {
            var list = WorkoutScreen?.List ?? throw new InvalidOperationException("TapRoutineNamed: no hay pantalla Workout.");
            for (var i = 0; i < list.Count; i++)
            {
                var card = list.CardAt(i);
                if (card != null && card.Routine.Name == routineName)
                {
                    // It. 8: el selector de escenario empuja la lista hacia abajo; la tarjeta se trae arriba del visor (como el dedo del
                    // usuario) antes del toque con hit-test.
                    var scroll = WorkoutScreen!.Scroll;
                    var y = card.ChangeCoordinatesTo(scroll.contentContainer, Vector2.zero).y;
                    var target = Mathf.Max(0, y - 16);
                    if (Mathf.Abs(scroll.scrollOffset.y - target) > 0.5f)
                    {
                        scroll.scrollOffset = new Vector2(scroll.scrollOffset.x, target);
                        TickAll();
                        TickAll();
                    }

                    TapElement(card, "la tarjeta de \"" + routineName + "\"");
                    return;
                }
            }

            throw new InvalidOperationException("TapRoutineNamed: no hay una tarjeta de \"" + routineName + "\" en Workout.");
        }

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
            var element = UiQuery.FindByText(Root, text);
            if (element == null)
            {
                // It. 5 (05-series-parciales/02-tech-design §3.1; QA-G5T-04): una ranura de texto dibujado (los paneles de series): el
                // centro de la ranura, por la selección real; tiene que llegar al elemento que la dibuja.
                var slot = UiQuery.FindSlotByText(Root, text) ?? throw new InvalidOperationException("Tap: no hay ningún elemento ni ranura con el texto \"" + text + "\".");
                var center = slot.Slot.WorldRect.center;
                var picked = Root.panel.Pick(center);
                if (picked == null || (picked != slot.Owner && !slot.Owner.Contains(picked)))
                {
                    throw new InvalidOperationException("Tap \"" + text + "\": la ranura de " + UiQuery.Describe(slot.Owner) + " está tapada por " + UiQuery.Describe(picked));
                }

                Submit(picked);
                return;
            }

            var target = ActionableOf(element);
            TapElement(target, "\"" + text + "\"");
        }

        // It. 5 (QA-G5T-04): un arrastre por la selección real: PointerDown en from al elemento que da Pick, PointerMove en frames
        // pasos (con un cuadro del reloj de prueba cada uno) y PointerUp en to, al mismo elemento (como la captura del puntero).
        // Devuelve el elemento que lo recibió.
        public VisualElement? DragAt(Vector2 from, Vector2 to, int frames = 6)
        {
            var target = Root.panel.Pick(from);
            if (target == null)
            {
                return null;
            }

            SendPointer(PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = from, clickCount = 1 }), target);
            var steps = Math.Max(1, frames);
            for (var i = 1; i <= steps; i++)
            {
                var point = Vector2.Lerp(from, to, i / (float)steps);
                SendPointer(PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = point }), target);
                Step();
            }

            SendPointer(PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = to, clickCount = 1 }), target);
            TickAll();
            return target;
        }

        // Un toque con el puntero (PointerDown y PointerUp en el mismo punto) por la selección real.
        public VisualElement? PointerTapAt(Vector2 point)
        {
            return DragAt(point, point, 1);
        }

        // La rueda del mouse en un punto (delta.y > 0: hacia abajo), al elemento que da Pick.
        public VisualElement? Wheel(Vector2 point, float deltaY)
        {
            var target = Root.panel.Pick(point);
            if (target == null)
            {
                return null;
            }

            using (var wheel = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, delta = new Vector2(0, deltaY), mousePosition = point }))
            {
                wheel.target = target;
                target.SendEvent(wheel);
            }

            TickAll();
            return target;
        }

        private static void SendPointer(EventBase evt, VisualElement target)
        {
            using (evt)
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        // Sin hit-test: solo para lo que el legacy hace por API (hoy, nada).
        public void TapUnchecked(string elementName)
        {
            Submit(FindRequired(elementName));
        }

        // Un toque "real" en el centro del elemento: lo recibe lo que haya ahí (Pick), aunque ya no sea el elemento (lo que sale
        // no responde y el toque pasa a lo de atrás: RED.TapDuringTheExitCreatesNothing). Devuelve lo que lo recibió.
        public VisualElement? TapThrough(string elementName)
        {
            var element = FindRequired(elementName);
            return TapAt(element.worldBound.center);
        }

        // Un toque en un punto del panel: el elemento que da Pick recibe un NavigationSubmitEvent (que burbujea).
        public VisualElement? TapAt(Vector2 point)
        {
            var picked = Root.panel.Pick(point);
            if (picked != null)
            {
                Submit(picked);
            }

            return picked;
        }

        // Tocar el velo del diálogo de arriba, lejos del diálogo (= cancelar).
        public void TapScrim()
        {
            var window = Root.worldBound;
            var box = TopDialog?.Box.worldBound ?? Rect.zero;
            var point = new Vector2(window.center.x, Mathf.Max(window.yMin + 4, box.yMin - 12));
            if (box.Contains(point))
            {
                point = new Vector2(window.center.x, Mathf.Min(window.yMax - 4, box.yMax + 12));
            }

            TapAt(point);
        }

        // Motivo por el que un toque fallaría (null = se puede tocar).
        public string? WhyNotTappable(string elementName)
        {
            var element = Find(elementName);
            return element == null ? "no existe #" + elementName : HitTestProblem(element);
        }

        // Clic derecho en el centro del elemento (el menú del texto, AC-RUT-11-09): PointerDownEvent con el botón 1.
        public void RightClick(string elementName)
        {
            var element = FindRequired(elementName);
            var center = element.worldBound.center;
            var target = Root.panel.Pick(center) ?? element;
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 1, mousePosition = center, clickCount = 1 }))
            {
                down.target = target;
                target.SendEvent(down);
            }
        }

        // Puntero que entra y sale (ayudas en escritorio, AC-APP-03-06).
        public void PointerEnter(string elementName)
        {
            var element = FindRequired(elementName);
            using (var enter = PointerEnterEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = element.worldBound.center }))
            {
                enter.target = element;
                element.SendEvent(enter);
            }
        }

        // It. 12 (CAP-12): el puntero se mueve sobre el centro del elemento (a lo que da Pick ahí, o al elemento).
        public void PointerMove(string elementName)
        {
            var element = FindRequired(elementName);
            var center = element.worldBound.center;
            var target = Root.panel.Pick(center) ?? element;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = center }))
            {
                move.target = target;
                target.SendEvent(move);
            }
        }

        public void PointerLeave(string elementName)
        {
            var element = FindRequired(elementName);
            using (var leave = PointerLeaveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = element.worldBound.center }))
            {
                leave.target = element;
                element.SendEvent(leave);
            }
        }

        // Presionar y soltar con el dedo (mantener presionado = presionar, avanzar el reloj, soltar).
        public void PointerDown(string elementName, int button = 0)
        {
            var element = FindRequired(elementName);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = button, mousePosition = element.worldBound.center }))
            {
                down.target = element;
                element.SendEvent(down);
            }
        }

        public void PointerUp(string elementName, int button = 0)
        {
            var element = FindRequired(elementName);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = button, mousePosition = element.worldBound.center }))
            {
                up.target = element;
                element.SendEvent(up);
            }
        }

        // ── Teclado ───────────────────────────────────────────────────────────

        // El elemento con foco (null = nada).
        public Focusable? Focused => Root.panel?.focusController?.focusedElement;

        // El RmTextField con foco (null si el foco no está en un campo).
        public RmTextField? FocusedField
        {
            get
            {
                for (var e = Focused as VisualElement; e != null; e = e.hierarchy.parent)
                {
                    if (e is RmTextField field)
                    {
                        return field;
                    }
                }

                return null;
            }
        }

        // Escribe en un campo como el usuario: le da el foco y cambia el valor (el campo filtra y avisa al store). El cursor queda
        // al final, como después de teclear (QA-V3-04: antes quedaba en 0, "|500" en CAP-16).
        public void Type(string fieldName, string text)
        {
            var field = Find<RmTextField>(fieldName) ?? throw new InvalidOperationException("Type: no existe el campo #" + fieldName);
            if (!field.IsFocused)
            {
                field.FocusInput(false);
            }

            field.Input.value = text;
            var end = field.Value.Length;
            field.Select(end, end);
        }

        // "Atrás" (BackSource.SystemBack = la tecla de Android; Escape = la de la PC, que no cierra páginas: P-08).
        public void PressBack(BackSource source)
        {
            Handle.HandleBack(source);
            TickAll();
        }

        // Una tecla a la hoja con foco (o a la raíz), como la manda UI Toolkit: en un campo, el TextElement interno (S-B3a:
        // focusedElement devuelve el TextField, la raíz del compuesto, y un evento dirigido a él no llega al editor de texto).
        public void PressKey(KeyCode key, char character = '\0', EventModifiers modifiers = EventModifiers.None)
        {
            var target = KeyTarget();
            using (var down = KeyDownEvent.GetPooled(character, key, modifiers))
            {
                down.target = target;
                target.SendEvent(down);
            }
        }

        // Tab (y Mayús+Tab): la tecla y la navegación que genera UI Toolkit.
        public void PressTab(bool shift = false)
        {
            PressKey(KeyCode.Tab, '\t', shift ? EventModifiers.Shift : EventModifiers.None);
            var target = KeyTarget();
            using (var move = NavigationMoveEvent.GetPooled(shift ? NavigationMoveEvent.Direction.Previous : NavigationMoveEvent.Direction.Next))
            {
                move.target = target;
                target.SendEvent(move);
            }
        }

        // Enter: la tecla y el NavigationSubmitEvent que genera UI Toolkit sobre el elemento con foco.
        public void PressEnter()
        {
            PressKey(KeyCode.Return, '\n');
            if (Focused == null)
            {
                return;
            }

            Submit(KeyTarget());
        }

        // La hoja que recibe el teclado: el TextElement interno de un campo con foco, el elemento con foco o la raíz.
        public VisualElement KeyTarget()
        {
            var focused = Focused as VisualElement;
            if (focused is TextField field)
            {
                return (VisualElement?)field.Q(TextField.textInputUssName)?.Q<TextElement>() ?? field;
            }

            return focused ?? Root;
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
            if (tabBar != null && tabBar.resolvedStyle.display != DisplayStyle.None && tabBar.worldBound.yMin < bottom && tabBar.worldBound.yMax > top)
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
            var scroll = Root.Q<ScrollView>(scrollViewName) ?? (Root.Q(scrollViewName) as ScrollView)
                ?? throw new InvalidOperationException("ScrollToEnd: no existe #" + scrollViewName);
            scroll.scrollOffset = new Vector2(0, float.MaxValue);
        }

        // El contenido desplazable del diálogo de arriba, hasta el final.
        public void ScrollDialogToEnd()
        {
            var dialog = TopDialog ?? throw new InvalidOperationException("ScrollDialogToEnd: no hay un diálogo abierto.");
            dialog.ContentScroll.scrollOffset = new Vector2(0, float.MaxValue);
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
            TickAll();
        }

        public void SetInsets(SafeInsets insets)
        {
            Display.SafeArea = insets;
            TickAll();
        }

        public void SetLanguage(params string[] tags)
        {
            Locale.Set(tags);
            App.RefreshSystemSettings(tags);
            TickAll();
        }

        public void SetAccessibility(double textScale, bool reduceMotion)
        {
            Accessibility.Set(textScale, reduceMotion);
            TickAll();
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

        // It. 5 (§3.6, §6.2): por nombre, primero un elemento y, si no hay, una ranura de texto dibujado (los paneles de series).
        public string? TextOf(string elementName)
        {
            var element = Find(elementName);
            if (element != null)
            {
                return UiQuery.TextOf(element);
            }

            return UiQuery.FindSlot(Root, elementName)?.Slot.Text;
        }

        // La ranura de ese nombre (o null), con el elemento que la dibuja.
        public (VisualElement Owner, DrawnSlot Slot)? SlotOf(string slotName)
        {
            return UiQuery.FindSlot(Root, slotName);
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
            if (element == null)
            {
                var slot = UiQuery.FindSlot(Root, elementName);
                return slot.HasValue && slot.Value.Slot.IsVisible && UiQuery.IsShown(slot.Value.Owner);
            }

            return UiQuery.IsShown(element) && UiQuery.IsInside(element, Root.worldBound);
        }

        public Rect BoundsOf(string elementName)
        {
            var element = Find(elementName);
            if (element == null)
            {
                var slot = UiQuery.FindSlot(Root, elementName) ?? throw new InvalidOperationException("no existe #" + elementName + " (ni como ranura)");
                return slot.Slot.WorldRect;
            }

            return element.worldBound;
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
            ScreenReader?.Dispose();
            ScreenReader = null;
            if (_screenReaderForced)
            {
                _screenReaderForced = false;
                UnityEngine.Accessibility.AssistiveSupport.screenReaderStatusOverride = UnityEngine.Accessibility.AssistiveSupport.ScreenReaderStatusOverride.OSDriven;
            }

            Handle.Dispose();
            _sceneBridge?.Dispose();
            _sceneBridge = null;
            _tierScope?.Dispose();
            _tierScope = null;
            RestoreShaderCompilation();
            App.Dispose();
            Backend.Dispose();
            Routines.Dispose();
            ClosePreferencesFile();
            if (ExportFolder != null && System.IO.Directory.Exists(ExportFolder))
            {
                try
                {
                    System.IO.Directory.Delete(ExportFolder, true);
                }
                catch (System.IO.IOException)
                {
                }
            }

            if (_preferencesFolder != null)
            {
                try
                {
                    System.IO.Directory.Delete(_preferencesFolder, true);
                }
                catch (System.IO.IOException)
                {
                }
            }

            if (_host != null)
            {
                DestroyObject(_host);
            }

            if (_panel != null)
            {
                _panel.targetTexture = null;
                RuntimePanelFactory.Destroy(_panel);
            }

            DestroyTarget(_target);
        }

        // ── It. 6a: la escena real (06a-escena-lago/02-tech-design §1.1 B1, §5.1) ──

        private SceneBridge? _sceneBridge;
        private TierScope? _tierScope;
        private bool _shaderCompilationTouched;
        private bool _asyncShadersBefore;

        // La escena (SceneMode.Real) y su puente; null sin escena.
        public RowMate.Scene.SceneHost? Scene => _sceneBridge?.Host;

        public SceneBridge? SceneBridge => _sceneBridge;

        // El Tick del shell y, con escena, el de la escena (el armado en el acto, la orilla en el acto y la sesión en pasos de 1/60 s).
        private void TickAll()
        {
            Handle.Tick();
            ScreenReader?.Tick(Scheduler.MonotonicSeconds);
            SceneTick();
        }

        // ── It. 14: el lector de pantalla (DT14-22) ──

        // El puente del lector (solo con UiHostOptions.ScreenReader) y lo que anunció.
        public RowMate.Presentation.Accessibility.ScreenReaderBridge? ScreenReader { get; private set; }

        public FakeA11yAnnouncer Announcer { get; } = new FakeA11yAnnouncer();

        private bool _screenReaderForced;

        // Reconcilia ya (sin esperar los 250 ms) y devuelve el volcado del árbol.
        public string A11yDump()
        {
            var bridge = ScreenReader ?? throw new InvalidOperationException("A11yDump: el anfitrión no tiene el lector (UiHostOptions.ScreenReader).");
            bridge.RebuildNow();
            return bridge.Dump();
        }

        public void SceneTick()
        {
            var bridge = _sceneBridge;
            if (bridge == null || _disposed)
            {
                return;
            }

            bridge.Host.SetViewport(Profile.WidthPx, Profile.HeightPx, Profile.Density);
            bridge.Tick(Scheduler.MonotonicSeconds, Handle.Motion, App.Workout.State.Value.Phase, !Options.ScenePlayerWarmup);
        }

        private SceneBridge CreateScene(UiHostOptions options)
        {
            var world = options.SceneWorld ?? LoadSceneWorld();
            if (!_shaderCompilationTouched)
            {
                // §5.1 (QA-G6aT-12): la compilación de shaders sincrónica mientras dura el anfitrión (el primer dibujo sale con los shaders
                // de verdad, no con el de "compilando"). ShaderUtil.allowAsyncCompilation no se guarda en ProjectSettings.
#if UNITY_EDITOR
                _asyncShadersBefore = UnityEditor.ShaderUtil.allowAsyncCompilation;
                UnityEditor.ShaderUtil.allowAsyncCompilation = false;
#endif
                _shaderCompilationTouched = true;
            }

            var host = new RowMate.Scene.SceneHost(world, App, new RowMate.Scene.SceneHostOptions
            {
                Tier = options.SceneTier,
                RegionCode = options.SceneRegion,
                SynchronousStreaming = !options.ScenePlayerStreaming,
                FixedSteps = true,
                Character = options.SceneCharacter,
            });
            return new SceneBridge(host, _logger);
        }

        private void RestoreShaderCompilation()
        {
            if (!_shaderCompilationTouched)
            {
                return;
            }

            _shaderCompilationTouched = false;
#if UNITY_EDITOR
            UnityEditor.ShaderUtil.allowAsyncCompilation = _asyncShadersBefore;
#endif
        }

        public static RowMate.Scene.Slots.SceneWorldDefinition LoadSceneWorld()
        {
#if UNITY_EDITOR
            var world = UnityEditor.AssetDatabase.LoadAssetAtPath<RowMate.Scene.Slots.SceneWorldDefinition>(RowMate.Scene.Slots.SceneWorldDefinition.AssetPath);
            if (world == null)
            {
                throw new InvalidOperationException("falta " + RowMate.Scene.Slots.SceneWorldDefinition.AssetPath + ": correr RowMate/Bootstrap/Reaplicar");
            }

            return world;
#else
            throw new InvalidOperationException("UiTestHost solo corre en el Editor.");
#endif
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

            FocusLikeAClick(element);
            Submit(element);
        }

        // Un clic de verdad le da el foco a lo que toca si es enfocable (el foco de UI Toolkit al presionar). Sin esto, un Tap
        // dejaba el foco donde estaba: al cerrar el diálogo de + Paso, ExitLock se lo devolvía al nombre y selectAllOnFocus lo
        // seleccionaba entero (DEMO-04 y DEMO-06; QA-V3-04). Sin teclado de por medio no prende el anillo (.rm-keyboard-nav).
        private static void FocusLikeAClick(VisualElement element)
        {
            if (element.focusable && element.canGrabFocus && element.panel?.focusController?.focusedElement != element)
            {
                element.Focus();
            }
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
