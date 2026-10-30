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
    public sealed class ImmersiveWorkoutPage : IPage
    {
        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly HudZones _zones;
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

        public ImmersiveWorkoutPage(UiContext ui, ShellLayout layout)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Hud = ui.App.CreateHud();
            Session = Hud.State.Value.Session;

            Root = new VisualElement { name = "immersive" };
            Root.AddToClassList("rm-immersive");
            View = new HudView(ui, Hud.State.Value.IsRoutine, Hud.WallClock, OnFinishTapped, () => Hud.Pause(), () => Hud.Resume());
            Root.Add(View);
            _zones = new HudZones(View, () => _layout.ImmersiveInsets, ui.ReserveWrench);
            _zones.Changed += OnZonesChanged;
            Root.RegisterCallback<GeometryChangedEvent>(_ => _zones.Update());
            View.NaturalChanged += OnNaturalChanged;
            Root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            // Sin foco (abre así), el primer Tab llega a la raíz del panel: también entra al anillo (Terminar primero).
            Root.RegisterCallback<AttachToPanelEvent>(_ => HookPanelRoot());

            _cascade = new ImmersiveCascadeMotion(ui.Motion, ApplyPiece, o => SetOpacity(View.Backdrop, o), OnPieceInteractive, OnCascadeDone);
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

            // La entrada (§3.2): los controles responden desde su tramo (con reducir movimiento, desde el cuadro 0).
            View.Controls.SetInteractive(false);
            _cascade.Play();

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

        public HudView View { get; }

        public HudZones Zones => _zones;

        public HudLayout? Layout => _zones.Layout;

        public ImmersiveCascadeMotion Cascade => _cascade;

        public ImmersiveExitMotion Exit => _exit;

        public FinishWorkoutDialog? FinishDialog => _finishDialog != null && _finishDialog.IsOpen ? _finishDialog : null;

        public CompletionDialog? Completion => _completion != null && _completion.IsOpen ? _completion : null;

        // El diálogo de completado está pedido (los 300 ms corren) o abierto.
        public bool CompletionScheduled => _completionTimer != null;

        public bool IsExiting => _exiting;

        public bool HasRevealed => _revealed;

        public void OnPushed()
        {
            _zones.Update();
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
            CancelCompletionTimer();
            CancelDialogs(true);
            ReleaseShell();
            _layout.Changed -= OnShellLayoutChanged;
            _zones.Changed -= OnZonesChanged;
            UnhookWrench();
            UnhookPanelRoot();
            View.Dispose();
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
                View.Freeze();
            }
            else if (!_exiting)
            {
                var engineStep = _ui.App.Workout.State.Value.Step;
                View.Apply(state, engineStep?.StepIndex);
                _zones.Update();
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

        private void OpenCompletion(WorkoutHudState state, bool instantly)
        {
            if (_exiting || _popped || state.Completion == null || _completion != null)
            {
                return;
            }

            CancelFinishDialog();
            var dialog = new CompletionDialog(_ui, state.Completion, () => Hud.AcknowledgeCompletionAsync());
            _completion = dialog;
            dialog.Box.RegisterCallback<GeometryChangedEvent>(_ => RepublishSurface());
            _ui.Dialogs.Open(dialog, instantly);
            RepublishSurface();
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
            View.Freeze();
            CancelCompletionTimer();
            CancelDialogs(false);
            ReleaseShell();
            View.Controls.SetInteractive(false);
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

        // ── La entrada y la salida (§3.2, §3.3) ──

        private void ApplyPiece(HudPiece piece, PieceVisual visual)
        {
            var element = View.PieceOf(piece);
            if (element == null || _exiting)
            {
                return;
            }

            SetOpacity(element, visual.Opacity);
            var s = _zones.Layout?.Scale ?? 1;
            var shift = visual.ShiftDp * s;
            element.style.translate = Math.Abs(shift) < 1e-4 ? (StyleTranslate)StyleKeyword.Null : new Translate(0, (float)shift);
        }

        private void OnPieceInteractive(HudPiece piece)
        {
            if (piece == HudPiece.Controls && !_exiting)
            {
                View.Controls.SetInteractive(true);
            }
        }

        // Todo quieto (1120; con reducir movimiento, 120): la llave entra en su lugar.
        private void OnCascadeDone()
        {
            if (!_exiting)
            {
                View.Controls.SetInteractive(true);
            }

            HoldWrench(false);
        }

        private void ApplyExit(ExitVisual visual)
        {
            if (_popped)
            {
                return;
            }

            for (var i = 0; i < Pieces.Length; i++)
            {
                var element = View.PieceOf(Pieces[i]);
                if (element != null)
                {
                    SetOpacity(element, _exitFrom[i] * visual.Hud);
                }
            }

            SetOpacity(View.Callouts.Count, visual.Hud);
            SetOpacity(View.Callouts.Paused, visual.Hud);
            SetOpacity(View.Backdrop, visual.Backdrop);
        }

        private void OnRevealStarted()
        {
            if (_revealed)
            {
                return;
            }

            _revealed = true;
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
            var layout = _zones.Layout ?? _lastLayout;
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
            if (layout.Wrench.HasValue)
            {
                var dialog = DialogRect();
                _layout.SetSurface(this, dialog.HasValue
                    ? OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling, layout.LeftLaneObstacles, dialog.Value)
                    : OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling));
            }

            HookWrench();
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
            if (!_exiting && !_popped)
            {
                _zones.Invalidate();
                _zones.Update();
            }
        }

        private void OnNaturalChanged()
        {
            // Un ancho natural cambió (un número más largo, otro idioma): HudZones decide si hay que rehacer la disposición.
            if (!_exiting && !_popped)
            {
                _zones.Update();
            }
        }

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

            if (View.Controls.IsInteractive)
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
            var controls = View.Controls;
            var pause = controls.ShowsResume ? (VisualElement)controls.Resume : controls.Pause;
            var wrench = _layout.Wrench;
            var ring = wrench != null && wrench.resolvedStyle.display != DisplayStyle.None
                ? new VisualElement[] { controls.Finish, pause, wrench }
                : new VisualElement[] { controls.Finish, pause };
            var index = -1;
            for (var i = 0; i < ring.Length; i++)
            {
                if (from != null && (ReferenceEquals(ring[i], from) || ring[i].Contains(from)))
                {
                    index = i;
                    break;
                }
            }

            var next = index < 0 ? (forward ? 0 : ring.Length - 1) : (index + (forward ? 1 : ring.Length - 1)) % ring.Length;
            for (var tries = 0; tries < ring.Length; tries++)
            {
                var candidate = ring[next];
                if (candidate.canGrabFocus && candidate.enabledInHierarchy)
                {
                    candidate.Focus();
                    return;
                }

                next = (next + (forward ? 1 : ring.Length - 1)) % ring.Length;
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

            if (View.Controls.IsInteractive)
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
