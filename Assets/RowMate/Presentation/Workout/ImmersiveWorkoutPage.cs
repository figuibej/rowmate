using System;
using RowMate.App.Workout;
using RowMate.Core.Common;
using RowMate.Core.Design;
using RowMate.Core.Workout;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Shell;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Workout
{
    // El entrenamiento a pantalla completa, funcional (F-WKT-13, -14, -16, -17, -20; legacy immersive_workout_screen.dart;
    // 02-tech-design §6.3 de la It. 4a; 04-design-notes §4.3, §5.1, §5.7, §5.10):
    //   #immersive .rm-page .rm-immersive     pantalla completa, recibe eventos; tapa la barra de pestañas hasta que sale
    //   └── #hud (HudView)                     fondo, barra superior, tarjetas, banda y controles, por zonas (HudZones)
    // - Se abre en el acto (AC-WKT-13-01): la apila Workout con un fundido (PageTransitionKind.Fade, 240 / 200; 120 con reducir
    //   movimiento) y el motor ya está en marcha desde el toque. Abre sin foco.
    // - Se cierra sola (AC-WKT-13-04): cuando la fase vuelve a Idle hace Pop una vez. Si el primer estado después del segundo
    //   plano ya está en reposo (Instant), sale SIN transición (DN4a-17): no se ve fundirse el HUD de una sesión que terminó
    //   antes.
    // - Nada queda huérfano (AC-WKT-20-07; QA-G4a-P02, QA-G4aT-04): en el primer estado con la fase fuera de Active y Paused
    //   (Finished o, después de un alcance, ya Idle), ANTES del Pop, cancela su FinishWorkoutDialog abierto (como Cancelar).
    // - "Atrás" de Android abre la confirmación de terminar si los controles responden (AC-WKT-13-03); Escape sin diálogo no
    //   hace nada (BackNavigation, P-08).
    // - Mientras está arriba pide 60 fps, registra sus controles como obstáculo inferior (el aviso sube 8 dp sobre ellos,
    //   §6.9) y declara su superficie para la llave del simulador con el lugar que calcula HudGeometry (DN4a-14, §6.10). Al
    //   empezar su salida retira las dos cosas en el cuadro 0 (la barra de pestañas vuelve y el aviso del fin entra sobre
    //   ella). Dispone su WorkoutHudStore en OnPopped, después de cancelar sus diálogos.
    // - Tab (DN4a-10): Terminar → Pausa / Reanudar → la llave (con el simulador) → Terminar, y Mayús+Tab al revés.
    public sealed class ImmersiveWorkoutPage : IPage
    {
        private readonly UiContext _ui;
        private readonly ShellLayout _layout;
        private readonly HudZones _zones;
        private FinishWorkoutDialog? _finishDialog;
        private VisualElement? _wrench;
        private bool _pacing;
        private bool _exiting;
        private bool _popped;
        private bool _seenState;

        public ImmersiveWorkoutPage(UiContext ui, ShellLayout layout)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Hud = ui.App.CreateHud();

            Root = new VisualElement { name = "immersive" };
            Root.AddToClassList("rm-immersive");
            View = new HudView(ui, Hud.State.Value.IsRoutine, OnFinishTapped, () => Hud.Pause(), () => Hud.Resume());
            Root.Add(View);
            _zones = new HudZones(View, () => _layout.Insets, ui.ReserveWrench);
            _zones.Changed += OnZonesChanged;
            Root.RegisterCallback<GeometryChangedEvent>(_ => _zones.Update());
            View.NaturalChanged += OnNaturalChanged;
            Root.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
            // Sin foco (abre así), el primer Tab llega a la raíz del panel: también entra al anillo (Terminar primero).
            Root.RegisterCallback<AttachToPanelEvent>(_ => HookPanelRoot());
            Root.Bind(Hud.State, OnState);

            // La superficie de la llave desde el cuadro 0 de la entrada (sale en 90 y entra en su lugar al terminar), con el lugar
            // que calcula HudGeometry por ventana (no depende de los valores).
            DeclareInitialSurface();
            _layout.Changed += OnShellLayoutChanged;

            // Abre sin foco (DN4a-10): el toque que la abrió no deja el foco en Workout.
            Blur();
        }

        public VisualElement Root { get; }

        public WorkoutHudStore Hud { get; }

        public HudView View { get; }

        public HudZones Zones => _zones;

        public HudLayout? Layout => _zones.Layout;

        public FinishWorkoutDialog? FinishDialog => _finishDialog != null && _finishDialog.IsOpen ? _finishDialog : null;

        public bool IsExiting => _exiting;

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
            CancelDialogs();
            ReleaseShell();
            _layout.Changed -= OnShellLayoutChanged;
            _zones.Changed -= OnZonesChanged;
            UnhookWrench();
            UnhookPanelRoot();
            View.Dispose();
            Hud.Dispose();
        }

        // "Atrás" de Android: la confirmación de terminar si los controles responden (AC-WKT-13-03, AC-WKT-20-03).
        public void OnBackRequested()
        {
            OnFinishTapped();
        }

        // Toca Terminar: el diálogo de confirmación (uno solo).
        private void OnFinishTapped()
        {
            var state = Hud.State.Value;
            if (_exiting || !state.ControlsEnabled || FinishDialog != null)
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
            var engineStep = _ui.App.Workout.State.Value.Step;
            View.Apply(state, engineStep?.StepIndex);
            _zones.Update();

            if (state.Phase != WorkoutPhase.Active && state.Phase != WorkoutPhase.Paused)
            {
                // AC-WKT-20-07: el diálogo de terminar se cierra sin efecto antes de que la página salga.
                CancelDialogs();
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

        private void BeginExit(bool instantly)
        {
            if (_exiting)
            {
                return;
            }

            _exiting = true;
            ReleaseShell();
            if (instantly)
            {
                _ui.Pages.PopImmediately(this);
            }
            else
            {
                _ui.Pages.Pop(this);
            }
        }

        private void CancelDialogs()
        {
            var dialog = _finishDialog;
            if (dialog != null && dialog.IsOpen)
            {
                dialog.Cancel();
            }
        }

        // La superficie y el obstáculo, fuera en el cuadro 0 de la salida.
        private void ReleaseShell()
        {
            _layout.SetSurface(this, null);
            _layout.SetBottomObstacle(this, null);
            if (_pacing)
            {
                _pacing = false;
                _ui.Motion.Pacing.Release();
            }
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

            var input = new HudLayoutInput(size.x, size.y, _layout.Insets, _ui.ReserveWrench, false, false, false, false, default);
            Publish(HudGeometry.Layout(in input));
        }

        private void OnZonesChanged(HudLayout layout)
        {
            if (!_exiting && !_popped)
            {
                Publish(layout);
            }
        }

        // Los controles como obstáculo inferior (dp desde abajo) y, con el simulador, el lugar de la llave y el techo del panel.
        private void Publish(HudLayout layout)
        {
            var controls = layout.Controls;
            var height = layout.WindowHeight;
            _layout.SetBottomObstacle(this, new BottomObstacle(controls.Left, controls.Right, height - controls.Bottom, controls.Height));
            if (layout.Wrench.HasValue)
            {
                _layout.SetSurface(this, OverlaySurface.Workout(layout.Wrench.Value, layout.PanelCeiling));
            }

            HookWrench();
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

            MoveFocus(evt.target as VisualElement, evt.direction == NavigationMoveEvent.Direction.Next);
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

        // Un Tab que no nace en la página ni en la llave (nada con foco, o el foco en otra capa): entra al anillo.
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

            MoveFocus(null, evt.direction == NavigationMoveEvent.Direction.Next);
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
