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

        // El remo de prueba de la It. 4a (HostBackends.TestRower), si es el del host.
        public ScriptedRower? ScriptedRower => Backend.Rower as ScriptedRower;

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
                new ShellOptions(options.ProfileTab)) { CreationClock = CreationClock, Lifecycle = Lifecycle });
            Handle = ShellHost.Build(App, Root, new ShellHostOptions(Scheduler, options.IsDesktop, _backButton, Display,
                TopLayerFor(options, Backend, App, Scheduler), Clipboard, Keyboard) { Pipeline = options.Pipeline, BoatZoneOutline = options.BoatZoneOutline });
            Handle.Shell.Ui.TextMenuTouchMode = options.TextMenuTouchMode;
            if (options.StartApp)
            {
                App.Start();
            }

            Handle.Tick();
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

        // = pumpAndSettle: de a 1/60 s hasta que no haya animaciones finitas (latido y spinner no cuentan). Tope 2 s. Si alguna
        // espera el layout del cuadro siguiente (lo nuevo que crece), corta sin gastar reloj: el que llama deja pasar un cuadro
        // real y vuelve a llamar (UiTestBase.Settle, ScenarioScript.Settle). Devuelve el reloj que avanzó.
        public double Settle(TimeSpan? max = null)
        {
            var limit = (max ?? TimeSpan.FromSeconds(2)).TotalSeconds;
            var elapsed = 0.0;
            Handle.Tick();
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
            Handle.Tick();
        }

        // La ventana sin foco o minimizada (Windows: el loop sigue y el reloj también).
        public void Unfocus()
        {
            Lifecycle.SetFocus(false);
            Handle.Tick();
        }

        public void Focus()
        {
            Lifecycle.SetFocus(true);
            Handle.Tick();
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
            var element = UiQuery.FindByText(Root, text) ?? throw new InvalidOperationException("Tap: no hay ningún elemento con el texto \"" + text + "\".");
            var target = ActionableOf(element);
            TapElement(target, "\"" + text + "\"");
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
            Handle.Tick();
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
            Routines.Dispose();
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
