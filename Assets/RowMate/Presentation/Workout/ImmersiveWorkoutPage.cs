using System;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Motion.Hud;
using RowMate.Presentation.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El entrenamiento a pantalla completa (F-WKT-13, -14, -16, -17, -19 a -21, F-APP-15; legacy immersive_workout_screen.dart;
    // 02-tech-design §6.3 de la It. 4a; 04b-hud-exr/02-tech-design §2.6, §3.2, §3.3, §3.9, §4, §5.7):
    //   #immersive .rm-page .rm-immersive     pantalla completa NEGRA, recibe eventos; tapa la barra de pestañas hasta que sale
    //   └── #hud (HudView)                     fondo, barra superior, tarjetas, banda, el disco del centro y controles (HudZones)
    // - La sesión que muestra (Hud.State.Session). Se abre en el acto (AC-WKT-13-01) con la entrada "sumergirse": la página negra
    //   entra en 220 (PageTransitionKind.Immerse), el fondo en 220-400 y la cascada de las seis zonas desde los 400
    //   (ImmersiveCascadeMotion); los controles responden desde su tramo (840), al toque y al teclado; todo quieto a los 1120,
    //   cuando entra la llave. Con reducir movimiento, un fundido lineal de 120 sin cascada. Las barras del sistema se piden
    //   ocultas en el cuadro del toque (ImmersiveSystemBars, AC-APP-15-04).
    // - La rutina terminó SOLA (DT4a-15; AC-WKT-21-01): a los 300 ms del primer estado Finished + Completed se abre el diálogo de
    //   completado (CompletionDialogRule: una vez por sesión, nunca a mano), sin esperar al guardado; con Instant o si vence en el
    //   alcance (segundo plano), sin su entrada (AC-WKT-21-09). Su Terminar vuelve el motor a reposo.
    // - Se cierra sola (AC-WKT-13-04): en el primer estado de reposo el HUD deja de aplicar datos (la ÚLTIMA FOTO, O-01), la
    //   página cierra sus diálogos (el de completado con Cancel: es el dueño) y sale: el HUD se va en 0-200, el fondo funde a negro
    //   en 200-400 y la página en 400-640 (ImmersiveExitMotion + PageTransition); a los 400 avisa RevealStarted (los avisos del
    //   final, DN4b-24) y a los 640 sale del árbol y pide las barras del sistema. En los 400 en negro la raíz absorbe los toques
    //   sin actuar (Workout todavía no se ve, QA-V4b-MOT-02) y los suelta en RevealStarted. Si el primer estado después del segundo plano ya
    //   está en reposo (Instant), sale SIN transición (DN4a-17).
    // - Nada queda huérfano (AC-WKT-20-07; QA-G4a-P02, QA-G4aT-04): en el primer estado con la fase fuera de Active y Paused,
    //   ANTES del Pop, cancela su FinishWorkoutDialog abierto (como Cancelar).
    // - "Atrás" de Android abre la confirmación de terminar desde el cuadro 0 (AC-WKT-13-07); Escape sin diálogo no hace nada
    //   (BackNavigation, P-08).
    // - Mientras está arriba pide 60 fps, registra sus controles como obstáculo inferior y declara su superficie para la llave del
    //   simulador (la retiene durante la cascada: §5.7); con el diálogo de completado, las piezas del carril izquierdo y el
    //   rectángulo del diálogo (R10). Al empezar su salida retira las dos cosas en el cuadro 0.
    // - Tab (DN4a-10): Terminar → Pausa / Reanudar → la llave (con el simulador) → Terminar, y Mayús+Tab al revés; el primer Tab
    //   sin foco no entra al anillo hasta que los controles responden (840).
    // - It. 5 (05-series-parciales/02-tech-design §3.3, §3.5; AC-WKT-18-12, AC-WKT-13-06, -07): el anillo pasa a Terminar → Pausa /
    //   Reanudar → parciales → repeticiones (si se ve) → la llave → Terminar. La cascada y la salida de los paneles de series van por
    //   SeriesColumn (el único que escribe su translate y su opacidad, DT5-14); responden desde su tramo (520), nunca durante la salida
    //   ni con la rutina terminada; el de repeticiones que se va con el foco lo pierde antes (Blur).
    // - It. 5 (QA-V4b-06, AC-WKT-13-15; 05-series-parciales/00-deuda-4b §2): el HUD (HudView y HudZones) se arma en el cuadro en que
    //   el fondo empieza a aparecer, el primero después de los 220, con la pantalla entera negra y quieta; no en el del toque, en
    //   el que Workout empieza a fundirse. En ese cuadro recibe las opacidades que ya tiene la cascada (todo en 0) y el estado del
    //   store. Con reducir movimiento (el fondo está en 1 desde el cuadro 0) o sin animar, se arma en el acto. Si la sesión vuelve
    //   a reposo antes, no se arma: la salida funde la página negra. La página, el store, la cascada, las barras del sistema, la
    //   llave, la superficie y "Atrás" siguen desde el toque (AC-WKT-13-01, -07). HasView dice si ya está.
    public sealed class ImmersiveWorkoutPage : IPage
    {
        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private HudView? _view;
        private HudZones? _zones;
        private bool _controlsResponding;
        // La hora del toque en el reloj del motion: el HUD que se arma en el negro arranca lo suyo desde ahí (QA-V4b-06).
        private readonly double _openedAt;
        private readonly ImmersiveCascadeMotion _cascade;
        private readonly ImmersiveExitMotion _exit;
        private FinishWorkoutDialog? _finishDialog;
        private CompletionDialog? _completion;
        private IDisposable? _completionTimer;
        private long _completionSession = long.MinValue;
        private VisualElement? _wrench;
        private bool _pacing;
        private bool _exiting;
        private bool _popped;
        private bool _seenState;
        private bool _barsHidden;
        private bool _holdingWrench;
        private bool _revealed;
        private HudLayout? _lastLayout;
        // It. 10a (02-tech-design §6.3, plan B de R-08): la banda tiempo-distancia como obstáculo inferior mientras el aviso del enlace
        // está a la vista (en L640 el aviso centrado se cruzaba con ella; así sube por encima con 8 de aire).
        private readonly object _bandOwner = new object();
        private bool _bandObstacle;

        // It. 6a (06a-escena-lago/02-tech-design §3.9): la escena 3D (null sin escena: el fondo provisorio de siempre).
        private readonly IImmersiveScene? _scene;
        private readonly SceneFadeMotion? _sceneFade;
        private readonly HudReflowMotion _reflow;
        private IDisposable? _sceneReadiness;
        private IDisposable? _pacingRelease;
        private bool _flightPending;
        private bool _sceneShown;
        private bool _sceneClosed;
        private bool? _landscape;

        public ImmersiveWorkoutPage(UiContext ui, ShellLayout layout)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _openedAt = ui.Motion.Now;
            // La escena arranca en el cuadro del toque (en 0) y corre desde acá aunque todavía no se dibuje (AC-SCN-02-05).
            _scene = ui.Scene;
            _scene?.Open(_openedAt);
            if (_scene != null)
            {
                _sceneFade = new SceneFadeMotion(ui.Motion, ApplySceneFade, OnSceneFadeDone);
            }

            _reflow = new HudReflowMotion(ui.Motion, ApplyReflow);
            Hud = ui.App.CreateHud();
            Session = Hud.State.Value.Session;

            Root = new VisualElement { name = "immersive" };
            Root.AddToClassList("rm-immersive");
            Root.RegisterCallback<GeometryChangedEvent>(_ => _zones?.Update());
            Root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            // Sin foco (abre así), el primer Tab llega a la raíz del panel: también entra al anillo (Terminar primero).
            Root.RegisterCallback<AttachToPanelEvent>(_ => HookPanelRoot());

            _cascade = new ImmersiveCascadeMotion(ui.Motion, ApplyPiece, OnBackdrop, OnPieceInteractive, OnCascadeDone);
            _exit = new ImmersiveExitMotion(ui.Motion, ApplyExit, OnRevealStarted, () => { });

            // F-APP-15 (AC-APP-15-04): las barras del sistema, ocultas desde el cuadro del toque.
            _ui.App.SystemBars.EntryStarted(this);
            _barsHidden = true;

            Root.Bind(Hud.State, OnState);

            // La superficie de la llave desde el cuadro 0 de la entrada, con el lugar que calcula HudGeometry por ventana (no
            // depende de los valores); la llave sale en 90 y no entra hasta que termina la cascada (la página la retiene).
            HoldWrench(true);
            DeclareInitialSurface();
            _layout.Changed += OnShellLayoutChanged;
            // QA-V6a-02 (VID-09): la clase de la ventana desde el toque. Sin esto la PRIMERA rotación del inmersivo solo la anotaba (no
            // había una clase anterior con qué compararla) y el HUD aparecía entero, sin su entrada de 200 ms.
            var window = _layout.WindowSize;
            if (window.x > 0 && window.y > 0)
            {
                _landscape = window.x > window.y;
            }

            // La entrada (§3.2): los controles responden desde su tramo (con reducir movimiento, desde el cuadro 0). El HUD se arma
            // cuando el fondo empieza a aparecer (OnBackdrop): con reducir movimiento, ya en este Play.
            _cascade.Play();

            // La escena que llega tarde (AC-SCN-01-07): cuando queda lista, aparece en su lugar con su fundido de 0,6 s.
            _sceneReadiness = _scene?.Readiness.Subscribe(OnSceneReadiness, false);

            // Abre sin foco (DN4a-10): el toque que la abrió no deja el foco en Workout.
            Blur();
        }

        // Empezó a verse Workout en la salida (a los 400; con reducir movimiento o sin transición, en el acto): los avisos del
        // final entran ahí (DN4b-24).
        public event Action? RevealStarted;

        public VisualElement Root { get; }

        public WorkoutHudStore Hud { get; }

        // La sesión que muestra este inmersivo (la generación del motor al abrir).
        public long Session { get; private set; }

        // El HUD, desde el cuadro en que el fondo empieza a aparecer (QA-V4b-06): antes no existe (HasView).
        public HudView View => _view ?? throw new InvalidOperationException("el HUD todavía no se armó: se arma cuando el fondo empieza a aparecer (QA-V4b-06)");

        public bool HasView => _view != null;

        public HudZones Zones => _zones ?? throw new InvalidOperationException("el HUD todavía no se armó (QA-V4b-06)");

        public HudLayout? Layout => _zones?.Layout;

        public ImmersiveCascadeMotion Cascade => _cascade;

        public ImmersiveExitMotion Exit => _exit;

        public FinishWorkoutDialog? FinishDialog => _finishDialog != null && _finishDialog.IsOpen ? _finishDialog : null;

        public CompletionDialog? Completion => _completion != null && _completion.IsOpen ? _completion : null;

        // El diálogo de completado está pedido (los 300 ms corren) o abierto.
        public bool CompletionScheduled => _completionTimer != null;

        public bool IsExiting => _exiting;

        // La hora del toque en el reloj del motion (la sonda [PERF] mide la entrada desde acá).
        public double OpenedAt => _openedAt;

        public bool HasRevealed => _revealed;

        public void OnPushed()
        {
            _zones?.Update();
        }

        public void OnPopped()
        {
            if (_popped)
            {
                return;
            }

            _popped = true;
            _cascade.Stop();
            _exit.Stop();
            _sceneFade?.Stop();
            _reflow.Stop();
            _sceneReadiness?.Dispose();
            _sceneReadiness = null;
            _pacingRelease?.Dispose();
            _pacingRelease = null;
            CloseScene();
            CancelCompletionTimer();
            CancelDialogs(true);
            ReleaseShell();
            _layout.Changed -= OnShellLayoutChanged;
            if (_zones != null)
            {
                _zones.Changed -= OnZonesChanged;
            }

            UnhookWrench();
            UnhookPanelRoot();
            _view?.Dispose();
            Hud.Dispose();
            if (!_revealed)
            {
                OnRevealStarted();
            }

            // AC-APP-15-04: las barras vuelven al terminar la salida (si no queda otro inmersivo).
            if (_barsHidden)
            {
                _barsHidden = false;
                _ui.App.SystemBars.ExitFinished(this);
            }
        }

        // "Atrás" de Android: la confirmación de terminar si los controles responden (AC-WKT-13-03, AC-WKT-20-03), desde el
        // cuadro 0 (AC-WKT-13-07).
        public void OnBackRequested()
        {
            OnFinishTapped();
        }

        // Toca Terminar: el diálogo de confirmación (uno solo).
        private void OnFinishTapped()
        {
            var state = Hud.State.Value;
            if (_exiting || !state.ControlsEnabled || FinishDialog != null || Completion != null)
            {
                return;
            }

            var dialog = new FinishWorkoutDialog(_ui);
            _finishDialog = dialog;
            dialog.Closed += closed =>
            {
                if (closed.WasConfirmed)
                {
                    // Sin resumen ni aviso de completado (Q-53); un toque que llega con la fase ya fuera de marcha no hace nada.
                    Hud.FinishAsync().Forget(_ui.Logger, WorkoutEngine.Tag);
                }
            };
            _ui.Dialogs.Open(dialog);
        }

        private void OnState(WorkoutHudState state)
        {
            if (_popped)
            {
                return;
            }

            var firstState = !_seenState;
            _seenState = true;
            if (state.Phase == WorkoutPhase.Idle)
            {
                // La última foto (O-01): el HUD no aplica el reposo.
                _view?.Freeze();
            }
            else if (!_exiting)
            {
                // Sin HUD todavía (QA-V4b-06), el estado lo toma BuildView del store cuando se arma.
                if (_view != null)
                {
                    var engineStep = _ui.App.Workout.State.Value.Step;
                    _view.Apply(state, engineStep?.StepIndex);
                    _zones?.Update();
                }

                if (state.Session != 0)
                {
                    Session = state.Session;
                }
            }

            if (state.Phase != WorkoutPhase.Active && state.Phase != WorkoutPhase.Paused)
            {
                // AC-WKT-20-07: el diálogo de terminar se cierra sin efecto antes de que la página salga.
                CancelFinishDialog();
            }

            if (state.Phase == WorkoutPhase.Finished)
            {
                _view?.Series?.SetInteractive(false);
                MaybeScheduleCompletion(state);
                return;
            }

            if (state.Phase != WorkoutPhase.Idle)
            {
                return;
            }

            if (firstState)
            {
                // La sesión ya volvió a reposo antes de que la página se armara: sale en el cuadro siguiente, sin transición
                // (no a mitad del Push).
                Root.schedule.Execute(() => BeginExit(true));
                return;
            }

            BeginExit(state.Instant);
        }

        // AC-WKT-21-01, -07, -09; AC-WKT-13-11 (§2.6): a los 300 ms del primer estado Finished + Completed, una vez por sesión.
        private void MaybeScheduleCompletion(WorkoutHudState state)
        {
            if (_exiting || _completionTimer != null || _completion != null || !CompletionDialogRule.ShouldOpen(state, _completionSession))
            {
                return;
            }

            _completionSession = state.Session;
            if (CompletionDialogRule.OpensInstantly(state))
            {
                // Volvió del segundo plano o despertó con la rutina ya terminada: el diálogo ya está, con los totales finales.
                OpenCompletion(state, true);
                return;
            }

            var scheduler = _ui.App.Scheduler;
            _completionTimer = scheduler.Schedule(TimeSpan.FromMilliseconds(MotionSpecs.CompletedDialogDelayMs), () =>
            {
                _completionTimer = null;
                // Si vence dentro del alcance (la app pasó a segundo plano en esos 300 ms), sin entrada (QA-G4bT-08 (d)).
                OpenCompletion(Hud.State.Value, scheduler.IsCatchingUp || Hud.State.Value.Instant);
            });
        }

        // Para las pruebas (O-5): cómo se arma el diálogo de completado (null = el de verdad).
        internal Func<HudCompletion, CompletionDialog>? CompletionFactoryForTest { get; set; }

        private void OpenCompletion(WorkoutHudState state, bool instantly)
        {
            if (_exiting || _popped || state.Completion == null || _completion != null)
            {
                return;
            }

            CancelFinishDialog();
            try
            {
                var factory = CompletionFactoryForTest;
                var dialog = factory != null ? factory(state.Completion) : new CompletionDialog(_ui, state.Completion, () => Hud.AcknowledgeCompletionAsync());
                _completion = dialog;
                dialog.Box.RegisterCallback<GeometryChangedEvent>(_ => RepublishSurface());
                _ui.Dialogs.Open(dialog, instantly);
                RepublishSurface();
                OnCompletionOpened();
            }
            catch (Exception error)
            {
                // It. 5 (O-5 de la lente de motion de la 4b; 05-series-parciales/00-deuda-4b §4): solo el Terminar del diálogo saca
                // al motor de Finished (como el legacy, immersive_workout_screen.dart:261-289). Si el diálogo no se pudo armar, la
                // sesión no puede quedar terminada para siempre (la app, ocupada hasta reiniciarse): se anota y se hace lo que haría
                // su Terminar. El guardado ya empezó en el segundo del fin; la página sale como siempre, sin diálogo.
                _ui.Logger.Log(LogLevel.Error, WorkoutEngine.Tag, "no se pudo abrir el diálogo de completado: vuelve a reposo sin él (O-5)", error);
                var broken = _completion;
                _completion = null;
                if (broken != null && broken.IsOpen)
                {
                    broken.Cancel();
                }

                Hud.AcknowledgeCompletionAsync().Forget(_ui.Logger, WorkoutEngine.Tag);
            }
        }

        // It. 6a (AC-SCN-02-07; P-07): con el diálogo de completado, la toma aérea (0,4 m/s, 6 s, cabeceo de −4°; con reducir movimiento,
        // nada). Cuando la toma terminó y el diálogo está quieto, la página suelta su demanda de 60 fps (la vuelve a pedir nadie: la
        // salida la libera igual en ReleaseShell).
        private void OnCompletionOpened()
        {
            if (_scene == null || _exiting || _popped)
            {
                return;
            }

            _scene.BeginAerial(_ui.Motion.Now);
            _pacingRelease?.Dispose();
            var settle = MotionSpecs.AerialSeconds + MotionSpecs.CompletedDialogDelayMs / 1000.0;
            _pacingRelease = _ui.App.Scheduler.Schedule(TimeSpan.FromSeconds(settle), () =>
            {
                _pacingRelease = null;
                if (_pacing && !_exiting && !_popped && Completion != null)
                {
                    _pacing = false;
                    _ui.Motion.Pacing.Release();
                }
            });
        }

        // Para el arnés y las pruebas (QA-G4bT-11 (d)): el diálogo con los totales de un caso, sin correr la rutina.
        public CompletionDialog OpenCompletionForTest(HudCompletion completion)
        {
            CancelFinishDialog();
            var dialog = new CompletionDialog(_ui, completion, () => Hud.AcknowledgeCompletionAsync());
            _completion = dialog;
            dialog.Box.RegisterCallback<GeometryChangedEvent>(_ => RepublishSurface());
            _ui.Dialogs.Open(dialog, false);
            return dialog;
        }

        private void CancelCompletionTimer()
        {
            _completionTimer?.Dispose();
            _completionTimer = null;
        }

        private void BeginExit(bool instantly)
        {
            if (_exiting)
            {
                return;
            }

            _exiting = true;
            _view?.Freeze();
            // La cámara sube y retrocede desde la pose que tenga (AC-SCN-02-06); con reducir movimiento no se mueve.
            _scene?.BeginExit(_ui.Motion.Now);
            _sceneFade?.Stop();
            _pacingRelease?.Dispose();
            _pacingRelease = null;
            CancelCompletionTimer();
            CancelDialogs(false);
            ReleaseShell();
            _view?.Series?.SetInteractive(false);
            _view?.Controls.SetInteractive(false);
            // La salida arranca desde las opacidades que tenga la entrada (si se cortó a mitad: AC-WKT-13-07).
            var from = new ExitStart(_cascade.IsRunning ? _cascade.BackdropOpacity : 1, 1);
            for (var i = 0; i < Pieces.Length; i++)
            {
                _exitFrom[i] = _cascade.IsRunning ? _cascade.PieceOpacity(Pieces[i]) : 1;
            }

            _cascade.Stop();
            if (instantly)
            {
                _exit.Play(from, true);
                _ui.Pages.PopImmediately(this);
            }
            else
            {
                _exit.Play(from);
                _ui.Pages.Pop(this);
                // QA-V4b-MOT-02: en los 400 en negro Workout todavía no se ve. ExitLock dejó inerte todo el árbol (lo que sale no
                // responde, AC-WKT-13-08) y los toques pasaban a lo de abajo; la raíz negra los absorbe sin actuar (no tiene
                // manejadores de toque) hasta que empieza a verse Workout (OnRevealStarted). Con reducir movimiento Workout se ve
                // desde el cuadro 0 (ya avisó RevealStarted) y no absorbe nada.
                if (!_revealed && _ui.Pages.IsExiting(this))
                {
                    Root.pickingMode = PickingMode.Position;
                }
            }
        }

        private static readonly HudPiece[] Pieces =
        {
            HudPiece.TopBar, HudPiece.Spm, HudPiece.SplitWatts, HudPiece.Band, HudPiece.Vitals, HudPiece.Controls,
        };

        private readonly double[] _exitFrom = { 1, 1, 1, 1, 1, 1 };

        // It. 5: los paneles de series (la cascada y la salida van por SeriesColumn, DT5-14).
        private static readonly HudPiece[] SeriesPieces = { HudPiece.SeriesLaps, HudPiece.SeriesReps };

        private bool _seriesResponding;

        // Los paneles responden: desde su tramo de la cascada, no durante la salida ni con la rutina terminada (detrás del diálogo de
        // completado; AC-WKT-18-20). Con un diálogo abierto el panel mismo no responde (el velo ya captura el puntero).
        private bool SeriesInteractive => _seriesResponding && !_exiting && !_popped && Hud.State.Value.Phase != WorkoutPhase.Finished;

        // ── La entrada y la salida (§3.2, §3.3) ──

        private void ApplyPiece(HudPiece piece, PieceVisual visual)
        {
            if (piece == HudPiece.SeriesLaps || piece == HudPiece.SeriesReps)
            {
                // It. 5 (DT5-14): los paneles de series los compone SeriesColumn (el único que escribe sus estilos), con su escala sp.
                if (!_exiting)
                {
                    _view?.Series?.SetPiece(piece, visual.Opacity, visual.ShiftDp);
                }

                return;
            }

            var element = _view?.PieceOf(piece);
            if (element == null || _exiting)
            {
                return;
            }

            SetOpacity(element, visual.Opacity);
            var s = _zones?.Layout?.Scale ?? 1;
            var shift = visual.ShiftDp * s;
            element.style.translate = Math.Abs(shift) < 1e-4 ? (StyleTranslate)StyleKeyword.Null : new Translate(0, (float)shift);
        }

        // El fondo de la cascada (QA-V4b-06): la primera vez que deja el 0 (el primer cuadro después de los 220, con la pantalla
        // entera negra y quieta; con reducir movimiento o sin animar, en el Play del toque) se arma el HUD.
        private void OnBackdrop(double opacity)
        {
            if (_view == null && opacity > 0 && !_exiting && !_popped)
            {
                BuildView();
            }
            else if (_flightPending && !_exiting && !_popped)
            {
                // It. 6a (§3.9): el cuadro SIGUIENTE al del armado del HUD (el HUD paga su armado solo, 00-deuda-5 §5): la escena entra
                // con el vuelo anclado a los 220 ms del toque y la opacidad del fondo de la cascada.
                _flightPending = false;
                StartScene(false);
            }

            if (_view != null)
            {
                SetOpacity(_view.Backdrop, opacity);
                if (_sceneShown && _sceneFade?.IsRunning != true)
                {
                    _view.SetSceneOpacity(opacity);
                }
            }
        }

        // ── It. 6a: la escena (06a-escena-lago/02-tech-design §3.9) ──

        // La escena entra: con el vuelo (o en su lugar con reducir movimiento), si ya está lista; si no, el fondo provisorio sigue y la
        // escena aparece con su fundido cuando quede lista (OnSceneReadiness).
        private void StartScene(bool inPlace)
        {
            var scene = _scene;
            var view = _view;
            if (scene == null || view == null || _sceneShown || _exiting || _popped)
            {
                return;
            }

            if (scene.Readiness.Value != ImmersiveSceneReadiness.Ready)
            {
                return;
            }

            if (inPlace)
            {
                scene.ShowInPlace(_ui.Motion.Now);
            }
            else
            {
                scene.BeginFlight(SceneTimelines.FlightStartSeconds(_openedAt));
            }

            _sceneShown = true;
            view.SetSceneShown(true);
            view.SetSceneOpacity(_cascade.IsRunning ? _cascade.BackdropOpacity : 1);
        }

        private void OnSceneReadiness(ImmersiveSceneReadiness readiness)
        {
            if (readiness != ImmersiveSceneReadiness.Ready || _view == null || _sceneShown || _flightPending || _exiting || _popped)
            {
                return;
            }

            // La escena demorada (AC-SCN-01-07): aparece en su lugar (con la rampa de la respiración) y funde 0,6 s sobre el provisorio.
            _scene!.ShowInPlace(_ui.Motion.Now);
            _sceneShown = true;
            _view.SetSceneShown(true, true);
            _view.SetSceneOpacity(0);
            _sceneFade!.Play();
        }

        private void ApplySceneFade(double opacity)
        {
            if (!_exiting)
            {
                _view?.SetSceneOpacity(opacity);
            }
        }

        private void OnSceneFadeDone()
        {
            if (!_exiting && !_popped && _sceneShown)
            {
                _view?.SetSceneShown(true);
                _view?.SetSceneOpacity(1);
            }
        }

        private void CloseScene()
        {
            if (_scene == null || _sceneClosed)
            {
                return;
            }

            _sceneClosed = true;
            _scene.Close();
        }

        // DT6a-06: al cambiar de clase de ventana (vertical ↔ horizontal) las piezas del HUD entran en 200 en el layout nuevo.
        private void ApplyReflow(double opacity)
        {
            var view = _view;
            if (view == null || _exiting || _cascade.IsRunning)
            {
                return;
            }

            for (var i = 0; i < Pieces.Length; i++)
            {
                var element = view.PieceOf(Pieces[i]);
                if (element != null)
                {
                    SetOpacity(element, opacity);
                }
            }
        }

        // El HUD con lo que ya pasó: el estado del store (sin animar: es su primer estado), las opacidades y subidas de la cascada en
        // este instante y si los controles ya responden. Lo de la llave, el obstáculo y las barras ya estaba desde el toque.
        private void BuildView()
        {
            // Lo continuo que arranca con el primer estado (el punto en vivo, el pulso de Próximo, el corazón) toma la hora del
            // toque como comienzo, como cuando el HUD se armaba ahí: sus fases son las de la 4b (CAP-13: 0,444 a los 42,2 s).
            using (_ui.Motion.StartingAt(_openedAt))
            {
                BuildViewNow();
            }
        }

        private void BuildViewNow()
        {
            var state = Hud.State.Value;
            var view = new HudView(_ui, state.IsRoutine, Hud.WallClock, OnFinishTapped, () => Hud.Pause(), () => Hud.Resume());
            _view = view;
            Root.Add(view);
            // El fondo con su textura desde este cuadro, el primero en que se ve: el horizonte depende solo de la ventana (la
            // disposición inicial de DeclareInitialSurface); HudZones lo vuelve a poner igual cuando el HUD tenga layout.
            if (_lastLayout != null)
            {
                view.Backdrop.SetHorizon(_lastLayout.Horizon);
            }

            var zones = new HudZones(view, () => _layout.ImmersiveInsets, _ui.ReserveWrench);
            _zones = zones;
            zones.Changed += OnZonesChanged;
            view.NaturalChanged += OnNaturalChanged;
            view.Controls.SetInteractive(_controlsResponding && !_exiting);
            for (var i = 0; i < Pieces.Length; i++)
            {
                ApplyPiece(Pieces[i], new PieceVisual(_cascade.PieceOpacity(Pieces[i]), _cascade.PieceShiftDp(Pieces[i])));
            }

            foreach (var piece in SeriesPieces)
            {
                ApplyPiece(piece, new PieceVisual(_cascade.PieceOpacity(piece), _cascade.PieceShiftDp(piece)));
            }

            view.Series?.SetInteractive(SeriesInteractive);

            if (state.Phase != WorkoutPhase.Idle)
            {
                view.Apply(state, _ui.App.Workout.State.Value.Step?.StepIndex);
            }

            zones.Update();

            // It. 6a (§3.9): la escena NO se dibuja en el cuadro del armado del HUD; entra en el siguiente con el vuelo. Con reducir
            // movimiento, en su lugar desde el cuadro 0 (sin respiración). Este cuadro es negro y quieto: lo que la escena tenga pendiente
            // para su primer cuadro a la vista (la orilla de 0 m después de una sesión larga) se termina acá (QA-V6a-REN-04).
            if (_scene != null)
            {
                _scene.CatchUp();
                if (_ui.App.Accessibility.Value.ReduceMotion)
                {
                    StartScene(true);
                }
                else
                {
                    _flightPending = true;
                }
            }
        }

        private void OnPieceInteractive(HudPiece piece)
        {
            if (piece == HudPiece.SeriesLaps && !_exiting)
            {
                // It. 5 (AC-WKT-13-07; DT5-12): los paneles responden desde su tramo de la cascada (520), como los controles desde el suyo.
                _seriesResponding = true;
                _view?.Series?.SetInteractive(SeriesInteractive);
            }

            if (piece == HudPiece.Controls && !_exiting)
            {
                _controlsResponding = true;
                _view?.Controls.SetInteractive(true);
            }
        }

        // Todo quieto (1120; con reducir movimiento, 120): la llave entra en su lugar.
        private void OnCascadeDone()
        {
            if (!_exiting)
            {
                _controlsResponding = true;
                if (_view == null && !_popped)
                {
                    // Una entrada que terminó sin que el fondo se moviera (no debería pasar): el HUD igual.
                    BuildView();
                }

                _seriesResponding = true;
                _view?.Series?.SetInteractive(SeriesInteractive);
                _view?.Controls.SetInteractive(true);
            }

            HoldWrench(false);
        }

        private void ApplyExit(ExitVisual visual)
        {
            var view = _view;
            if (_popped || view == null)
            {
                return;
            }

            for (var i = 0; i < Pieces.Length; i++)
            {
                var element = view.PieceOf(Pieces[i]);
                if (element != null)
                {
                    SetOpacity(element, _exitFrom[i] * visual.Hud);
                }
            }

            view.Series?.SetExit(visual.Hud);
            SetOpacity(view.Callouts.Count, visual.Hud);
            SetOpacity(view.Callouts.Paused, visual.Hud);
            SetOpacity(view.Backdrop, visual.Backdrop);
            if (_sceneShown)
            {
                // It. 6a: scene-surface funde a negro con el fondo (200-400).
                view.SetSceneOpacity(visual.Backdrop);
            }
        }

        private void OnRevealStarted()
        {
            if (_revealed)
            {
                return;
            }

            _revealed = true;
            // It. 6a (§3.9): a los 400 de la salida el negro cubre la escena: se cierra (deja de integrar y de dibujar).
            CloseScene();
            // Desde acá vale el toque en Workout (ROB-01, AC-WKT-01-09; QA-V4b-MOT-02): la raíz que sale deja de absorber.
            Root.pickingMode = PickingMode.Ignore;
            RevealStarted?.Invoke();
        }

        private static void SetOpacity(VisualElement element, double opacity)
        {
            element.style.opacity = opacity >= 0.9995 ? (StyleFloat)StyleKeyword.Null : (float)Math.Max(0, opacity);
        }

        // ── Diálogos ──

        private void CancelFinishDialog()
        {
            var dialog = _finishDialog;
            if (dialog != null && dialog.IsOpen)
            {
                dialog.Cancel();
            }
        }

        // Al volver a reposo, antes del Pop: el de terminar y el de completado (el cierre del dueño, Cancel, que Dismissible =
        // false no bloquea: QA-G4bT-09 (c)). Nunca queda un diálogo del inmersivo sobre Workout en reposo (AC-WKT-13-04).
        private void CancelDialogs(bool popped)
        {
            CancelFinishDialog();
            var completion = _completion;
            if (completion != null && completion.IsOpen)
            {
                completion.Cancel();
            }
        }

        // ── La llave, el obstáculo y la superficie ──

        // La superficie y el obstáculo, fuera en el cuadro 0 de la salida.
        private void ReleaseShell()
        {
            _layout.SetSurface(this, null);
            _layout.SetBottomObstacle(this, null);
            _layout.SetBottomObstacle(_bandOwner, null);
            HoldWrench(false);
            if (_pacing)
            {
                _pacing = false;
                _ui.Motion.Pacing.Release();
            }
        }

        private void HoldWrench(bool hold)
        {
            if (_holdingWrench == hold)
            {
                return;
            }

            _holdingWrench = hold;
            _layout.HoldWrench(this, hold);
        }

        private void DeclareInitialSurface()
        {
            if (!_pacing)
            {
                _pacing = true;
                _ui.Motion.Pacing.Acquire();
            }

            var size = _layout.WindowSize;
            if (!(size.x > 0) || !(size.y > 0))
            {
                return;
            }

            var input = new HudLayoutInput(size.x, size.y, _layout.ImmersiveInsets, _ui.ReserveWrench, false, false, false, false, default);
            Publish(HudGeometry.Layout(in input));
        }

        private void OnZonesChanged(HudLayout layout)
        {
            if (!_exiting && !_popped)
            {
                Publish(layout);
            }
        }

        private void RepublishSurface()
        {
            var layout = _zones?.Layout ?? _lastLayout;
            if (layout != null && !_exiting && !_popped)
            {
                Publish(layout);
            }
        }

        // Los controles como obstáculo inferior (dp desde abajo) y, con el simulador, el lugar de la llave y el techo del panel;
        // con el diálogo de completado abierto, las piezas del carril izquierdo y el rectángulo del diálogo (R10, §5.7).
        private void Publish(HudLayout layout)
        {
            _lastLayout = layout;
            var controls = layout.Controls;
            var height = layout.WindowHeight;
            _layout.SetBottomObstacle(this, new BottomObstacle(controls.Left, controls.Right, height - controls.Bottom, controls.Height));
            PublishBand(layout);
            if (layout.Wrench.HasValue)
            {
                var dialog = DialogRect();
                // It. 5: con el diálogo, también lo que se ve de la columna de los paneles de series (AC-BLE-16-02).
                _layout.SetSurface(this, dialog.HasValue
                    ? OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling, layout.LeftLaneObstacles, dialog.Value, _view?.Series?.VisibleBounds())
                    : OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling));
            }

            HookWrench();
        }

        // It. 10a: la banda como obstáculo del aviso del enlace (lo pide WorkoutScreenView al mostrarlo; se saca al salir).
        public void SetLinkNoticeObstacle(bool on)
        {
            if (_bandObstacle == on)
            {
                return;
            }

            _bandObstacle = on;
            var layout = _zones?.Layout ?? _lastLayout;
            if (layout != null && !_exiting && !_popped)
            {
                PublishBand(layout);
            }
            else if (!on)
            {
                _layout.SetBottomObstacle(_bandOwner, null);
            }
        }

        private void PublishBand(HudLayout layout)
        {
            if (!_bandObstacle)
            {
                _layout.SetBottomObstacle(_bandOwner, null);
                return;
            }

            var band = layout.Band;
            _layout.SetBottomObstacle(_bandOwner, new BottomObstacle(band.Left, band.Left + band.Width, layout.WindowHeight - (band.Top + band.Height), band.Height));
        }

        // El rectángulo del diálogo de completado abierto, en dp de ventana (null sin diálogo o antes de su layout).
        private DpRect? DialogRect()
        {
            var dialog = Completion;
            if (dialog == null)
            {
                return null;
            }

            var box = dialog.Box.worldBound;
            var window = Root.worldBound;
            if (!(box.width > 0) || !(box.height > 0))
            {
                return null;
            }

            return new DpRect(box.x - window.x, box.y - window.y, box.width, box.height);
        }

        private void OnShellLayoutChanged()
        {
            if (!_exiting && !_popped && _zones != null)
            {
                _zones.Invalidate();
                _zones.Update();
            }

            // It. 6a (DT6a-06): un cambio de clase de ventana (vertical ↔ horizontal) hace entrar el HUD en 200 en el layout nuevo; un
            // cambio de tamaño dentro de la misma clase se reacomoda en el acto. La escena se adapta en el acto (otra RT, la misma sesión).
            var size = _layout.WindowSize;
            if (size.x > 0 && size.y > 0)
            {
                var landscape = size.x > size.y;
                if (_landscape.HasValue && _landscape.Value != landscape && !_exiting && !_popped && _view != null && !_cascade.IsRunning)
                {
                    _reflow.OnOrientationClassChanged(_ui.App.Accessibility.Value.ReduceMotion);
                }

                _landscape = landscape;
            }
        }

        private void OnNaturalChanged()
        {
            // Un ancho natural cambió (un número más largo, otro idioma): HudZones decide si hay que rehacer la disposición.
            if (!_exiting && !_popped)
            {
                _zones?.Update();
            }
        }

        // Los controles del HUD responden (sin HUD todavía, no: QA-V4b-06).
        private bool ControlsInteractive => _view != null && _view.Controls.IsInteractive;

        // ── El anillo de Tab ──

        // El anillo de Tab del inmersivo: Terminar → Pausa/Reanudar → la llave → Terminar (y al revés con Mayús).
        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            if (_ui.Dialogs.HasOpen)
            {
                return;
            }

            if (ControlsInteractive)
            {
                MoveFocus(evt.target as VisualElement, evt.direction == NavigationMoveEvent.Direction.Next);
            }

            // Sin la navegación por defecto de UI Toolkit (como FocusTrap, S3-b): el anillo es este.
            Root.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
        }

        private void OnWrenchNavigation(NavigationMoveEvent evt)
        {
            if (_exiting || _popped || _ui.Dialogs.HasOpen || !ReferenceEquals(_ui.Pages.Top, this))
            {
                return;
            }

            if (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            MoveFocus(_wrench, evt.direction == NavigationMoveEvent.Direction.Next);
            _wrench?.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
        }

        private void MoveFocus(VisualElement? from, bool forward)
        {
            if (_view == null)
            {
                return;
            }

            var controls = _view.Controls;
            var pause = controls.ShowsResume ? (VisualElement)controls.Resume : controls.Pause;
            var wrench = _layout.Wrench;
            // It. 5 (AC-WKT-18-12; P-06): Terminar → Pausa / Reanudar → parciales → repeticiones (si se ve) → la llave → Terminar.
            var ring = _ring;
            var count = 0;
            ring[count++] = controls.Finish;
            ring[count++] = pause;
            var series = _view.Series;
            if (series != null)
            {
                ring[count++] = series.Laps;
                if (series.RepsShown)
                {
                    ring[count++] = series.Reps;
                }
            }

            if (wrench != null && wrench.resolvedStyle.display != DisplayStyle.None)
            {
                ring[count++] = wrench;
            }

            MoveFocusIn(ring, count, from, forward);
        }

        private readonly VisualElement[] _ring = new VisualElement[5];

        private static void MoveFocusIn(VisualElement[] ring, int length, VisualElement? from, bool forward)
        {
            var index = -1;
            for (var i = 0; i < length; i++)
            {
                if (from != null && (ReferenceEquals(ring[i], from) || ring[i].Contains(from)))
                {
                    index = i;
                    break;
                }
            }

            var next = index < 0 ? (forward ? 0 : length - 1) : (index + (forward ? 1 : length - 1)) % length;
            for (var tries = 0; tries < length; tries++)
            {
                var candidate = ring[next];
                if (candidate.canGrabFocus && candidate.enabledInHierarchy)
                {
                    candidate.Focus();
                    return;
                }

                next = (next + (forward ? 1 : length - 1)) % length;
            }
        }

        // Un Tab que no nace en la página ni en la llave (nada con foco, o el foco en otra capa): entra al anillo, pero no antes
        // de que los controles respondan (QA-G4bT-09 (a)): un Tab a los 500 ms no enfoca nada.
        private void OnPanelNavigation(NavigationMoveEvent evt)
        {
            if (_exiting || _popped || _ui.Dialogs.HasOpen || !ReferenceEquals(_ui.Pages.Top, this))
            {
                return;
            }

            if (evt.direction != NavigationMoveEvent.Direction.Next && evt.direction != NavigationMoveEvent.Direction.Previous)
            {
                return;
            }

            if (evt.target is VisualElement target && (ReferenceEquals(target, Root) || Root.Contains(target)
                                                       || _wrench != null && (ReferenceEquals(target, _wrench) || _wrench.Contains(target))))
            {
                // Lo resuelven OnNavigationMove y OnWrenchNavigation.
                return;
            }

            if (ControlsInteractive)
            {
                MoveFocus(null, evt.direction == NavigationMoveEvent.Direction.Next);
            }

            _panelRoot?.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
        }

        private VisualElement? _panelRoot;

        private void HookPanelRoot()
        {
            var root = Root.panel?.visualTree;
            if (root == null || ReferenceEquals(root, _panelRoot) || _popped)
            {
                return;
            }

            UnhookPanelRoot();
            _panelRoot = root;
            _panelRoot.RegisterCallback<NavigationMoveEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
        }

        private void UnhookPanelRoot()
        {
            _panelRoot?.UnregisterCallback<NavigationMoveEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
            _panelRoot = null;
        }

        private void HookWrench()
        {
            var wrench = _layout.Wrench;
            if (ReferenceEquals(wrench, _wrench))
            {
                return;
            }

            UnhookWrench();
            _wrench = wrench;
            _wrench?.RegisterCallback<NavigationMoveEvent>(OnWrenchNavigation, TrickleDown.TrickleDown);
        }

        private void UnhookWrench()
        {
            _wrench?.UnregisterCallback<NavigationMoveEvent>(OnWrenchNavigation, TrickleDown.TrickleDown);
            _wrench = null;
        }

        private void Blur()
        {
            var focused = _ui.Pages.panel?.focusController?.focusedElement;
            if (focused is VisualElement element)
            {
                element.Blur();
            }
        }
    }
}
