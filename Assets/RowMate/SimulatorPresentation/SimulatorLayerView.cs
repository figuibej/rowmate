using System;
using RowMate.Core.Design;
using RowMate.Presentation;
using RowMate.Presentation.Binding;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using RowMate.Simulator.Panel;
using UnityEngine.UIElements;

namespace RowMate.Simulator.Presentation
{
    // Capa del simulador (F-BLE-16; 02-tech-design §3.5): el equivalente del Overlay del legacy, montado en MaterialApp.builder
    // por encima del Navigator (main.dart:108-110). Es el ÚLTIMO hijo de #app-root, así queda por encima de las pestañas y,
    // desde la It. 3, de las páginas, diálogos y avisos, sin depender de ninguna pantalla (spec §12.5).
    //   #simulator-layer .rm-layer .rm-sim-layer     ocupa todo el panel, sin padding, pickingMode Ignore
    //   ├── #sim-open .rm-sim-fab (llave)             40 × 40, donde dice WrenchPlacement
    //   ├── #sim-panel .rm-sim-panel                  280 de ancho, anclado en la llave (WrenchPlacement.PanelFor)
    //   └── #sim-ribbon .rm-sim-ribbon                último hijo, sin eventos
    // Con el panel cerrado (como arranca) se ve la llave; al tocarla el panel se abre donde está y la llave se oculta;
    // #sim-close lo cierra y vuelve la llave (AC-BLE-16-02). Abrir: PopTransition (opacidad 160, escala 0,92 → 1 en 240) con
    // el origen en la esquina de la llave; cerrar: el panel se oculta en el acto y la llave vuelve con 0,85 → 1 en 160 (DN-14).
    // Con store == null (UiTestHost, capturas "Prueba", QA-G2-05) la capa tiene cinta y llave, sin panel.
    // It. 4a, D-ART-10 (DV-39 en lugar de DV-13; DD4a-01 a DD4a-03; 02-tech-design §6.10; 04-design-notes §3.9, §5.9):
    // - El lugar lo decide WrenchPlacement con la superficie que declara la pantalla de arriba (ShellLayout.Surface): carril
    //   izquierdo (16 + margen) en pestañas (barra + 16) y pantallas apiladas (16 + margen inferior); en el entrenamiento, el
    //   lugar que calcula el HUD, tal cual.
    // - Movimiento: entre pestañas no se mueve; pestaña ↔ pantalla apilada, resorte ω 22 en y; al cambiar de carril (entrar o
    //   salir del entrenamiento) sale en el cuadro 0 (opacidad 1 → 0 en 90, sin toques mientras sale) y, cuando termina la
    //   transición de la página, entra en el lugar nuevo (0,85 → 1 en 160); dentro del entrenamiento (rotar, otra ventana), en
    //   el acto. Reducir movimiento: en el acto, o solo opacidad (90 y 120) al cambiar de carril. Se puede tocar mientras se
    //   mueve.
    // - El panel se abre donde está la llave y crece desde su esquina (abajo a la izquierda fuera del entrenamiento, abajo a la
    //   derecha en el entrenamiento), con el alto máximo de PanelFor (en el entrenamiento vertical, hasta 8 dp debajo de Watts
    //   con su objetivo: AC-BLE-16-03). Si la llave cambia de lugar con el panel abierto, el panel SIGUE abierto y la sigue con
    //   el mismo movimiento (también cuando el inmersivo se cierra solo): nunca queda en el lugar de una pantalla que ya salió.
    // - La llave es un obstáculo inferior (para el aviso y el relleno de lo que se desplaza) en su lugar de destino, mientras
    //   no está abierto el panel; ShellLayout la expone (RegisterWrench) para los menús y el anillo de Tab del inmersivo.
    public sealed class SimulatorLayerView : VisualElement, IDisposable
    {
        public const double LaneOutMs = 90;

        private readonly UiContext _ui;
        private readonly SimulatorPanelStore? _store;
        private readonly ShellLayout _layout;
        private readonly PopTransition _panelIn;
        private readonly PopTransition _fabIn;
        private readonly SpringMotion _fabBottom;
        private readonly LaneSwap _laneSwap;
        private WrenchSpot? _spot;
        private OverlayContextKind _kind;
        private PanelAnchor _anchor;
        private bool? _open;
        private bool _disposed;

        public SimulatorLayerView(UiContext ui, SimulatorPanelStore? store, ShellLayout layout, VisualElement tabBar)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _store = store;
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (tabBar == null)
            {
                throw new ArgumentNullException(nameof(tabBar));
            }

            name = "simulator-layer";
            AddToClassList("rm-layer");
            AddToClassList("rm-sim-layer");
            pickingMode = PickingMode.Ignore;

            Fab = new Button(() => _store?.Open()) { name = "sim-open", text = "" };
            RmButton.StripUnityChrome(Fab);
            Fab.AddToClassList("rm-sim-fab");
            Fab.focusable = true;
            Fab.style.right = StyleKeyword.Auto;
            Fab.Add(RmButton.StateLayer());
            Fab.Add(new IconView(Icons.Build, false, 24));
            Fab.Add(RmButton.FocusRing());
            Add(Fab);
            _fabIn = new PopTransition(ui.Motion, Fab, MotionSpecs.FabInScaleFrom, Tokens.DurFast.Milliseconds, Tokens.DurFast.Milliseconds, 120);

            if (store != null)
            {
                Panel = new SimulatorPanelView(ui, store);
                Panel.style.display = DisplayStyle.None;
                Panel.style.right = StyleKeyword.Auto;
                Add(Panel);
                _panelIn = new PopTransition(ui.Motion, Panel, MotionSpecs.PanelInScaleFrom, Tokens.DurFast.Milliseconds, Tokens.DurBase.Milliseconds,
                    Tokens.DurFast.Milliseconds);
            }
            else
            {
                _panelIn = new PopTransition(ui.Motion, Fab, 1, 0, 0, 0);
            }

            Ribbon = new SimulatorRibbon();
            Add(Ribbon);

            _fabBottom = new SpringMotion(ui.Motion, OnFabBottom);
            _laneSwap = new LaneSwap(this);
            RegisterCallback<GeometryChangedEvent>(_ => Place());
            _layout.Changed += Place;
            _layout.SurfaceChanged += Place;
            _layout.RegisterWrench(Fab);
            if (store != null)
            {
                this.Bind(store.State, OnState);
            }

            Place();
            // It. 4b (§3.13): el resumen del tiempo de CPU del hilo principal en el registro del build mientras el inmersivo está arriba.
            Probe = new FrameTimeProbe(ui, this);
        }

        public FrameTimeProbe Probe { get; }

        public Button Fab { get; }

        public SimulatorPanelView? Panel { get; }

        public SimulatorRibbon Ribbon { get; }

        public SimulatorPanelStore? Store => _store;

        public bool IsOpen => _open == true;

        // La llave (o el panel) se está moviendo o cambiando de carril (regla de toma: ningún movimiento en curso).
        public bool IsMoving => _fabBottom.IsRunning || _laneSwap.IsRunning;

        // Cambiando de carril (sale y vuelve a entrar).
        public bool IsSwappingLane => _laneSwap.IsRunning;

        // El lugar de destino de la llave (null antes del primer layout).
        public WrenchSpot? Spot => _spot;

        // Dónde se abre el panel (con la llave de destino).
        public PanelAnchor PanelAnchor => _anchor;

        // Distancia actual de la llave al borde inferior, en dp.
        public double FabBottom => _fabBottom.HasValue ? _fabBottom.Value : _spot?.Bottom ?? 0;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _layout.Changed -= Place;
            _layout.SurfaceChanged -= Place;
            _fabBottom.Stop();
            _laneSwap.Stop();
            _layout.SetBottomObstacle(this, null);
            if (ReferenceEquals(_layout.Wrench, Fab))
            {
                _layout.RegisterWrench(null);
            }

            _store?.Dispose();
        }

        private void OnState(SimulatorPanelState state)
        {
            if (_open == state.IsOpen)
            {
                return;
            }

            var first = _open == null;
            _open = state.IsOpen;
            if (Panel == null)
            {
                return;
            }

            if (state.IsOpen)
            {
                Fab.style.display = DisplayStyle.None;
                _layout.SetBottomObstacle(this, null);
                ApplyPanel(FabBottom);
                Panel.style.display = DisplayStyle.Flex;
                Panel.style.opacity = StyleKeyword.Null;
                Panel.pickingMode = PickingMode.Position;
                if (!first && !_laneSwap.IsRunning)
                {
                    _panelIn.OriginLeft = _anchor.GrowsRight;
                    _panelIn.Play();
                }
            }
            else
            {
                _panelIn.Finish();
                Panel.style.display = DisplayStyle.None;
                Fab.style.display = DisplayStyle.Flex;
                UpdateObstacle();
                if (!first && !_laneSwap.IsRunning)
                {
                    _fabIn.Play();
                }
            }
        }

        // El lugar de la llave y del panel para la superficie vigente (02-tech-design §6.10).
        private void Place()
        {
            if (_disposed)
            {
                return;
            }

            var size = _layout.WindowSize;
            var width = size.x > 0 ? size.x : layout.width;
            var height = size.y > 0 ? size.y : layout.height;
            if (!(width > 0) || !(height > 0))
            {
                return;
            }

            var insets = _layout.Insets;
            var surface = _layout.Surface;
            var spot = WrenchPlacement.For(width, height, insets, surface);
            var anchor = WrenchPlacement.PanelFor(width, height, insets, spot, surface);
            var previous = _spot;
            var previousKind = _kind;
            _spot = spot;
            _kind = surface.Kind;
            _anchor = anchor;
            Fab.style.right = StyleKeyword.Auto;
            if (Panel != null)
            {
                Panel.style.maxHeight = (float)anchor.MaxHeight;
            }

            if (previous == null || !_ui.Motion.Armed)
            {
                _laneSwap.Stop();
                Fab.style.left = (float)spot.Left;
                _fabBottom.Snap(spot.Bottom);
                UpdateObstacle();
                return;
            }

            var workoutChange = (previousKind == OverlayContextKind.Workout) != (surface.Kind == OverlayContextKind.Workout);
            if (previous.Value.RightLane != spot.RightLane || workoutChange)
            {
                // Cambio de carril (entrar o salir del entrenamiento): sale en el cuadro 0, en su lugar, y entra en el nuevo (x e y)
                // al terminar la transición: nunca se ve desplazarse. It. 4b (QA-V4b-01, H-D2-02; ANIMATION_BIBLE §2.11): salir del
                // entrenamiento es siempre un cambio de carril, también desde el diálogo de completado, donde la llave ya está en el
                // carril izquierdo (R10): se va en el cuadro 0 y no queda a la vista sobre el negro de la salida.
                _laneSwap.Start();
                UpdateObstacle();
                return;
            }

            if (_laneSwap.IsRunning)
            {
                // Sigue saliendo o esperando: entra directo en el lugar nuevo.
                UpdateObstacle();
                return;
            }

            Fab.style.left = (float)spot.Left;

            var stackChange = previousKind != surface.Kind && previousKind != OverlayContextKind.Workout && surface.Kind != OverlayContextKind.Workout;
            if (stackChange)
            {
                // Pestaña ↔ pantalla apilada: resorte ω 22 en y (con reducir movimiento, en el acto).
                _fabBottom.MoveTo(spot.Bottom, MotionSpecs.KeyOmega);
            }
            else
            {
                // Entre pestañas no se mueve; dentro del entrenamiento (rotar, otra ventana), en el acto, con el HUD.
                _fabBottom.Snap(spot.Bottom);
            }

            UpdateObstacle();
        }

        private void OnFabBottom(double bottom)
        {
            Fab.style.bottom = (float)bottom;
            if (IsOpen && !_laneSwap.IsRunning)
            {
                ApplyPanel(bottom);
            }
        }

        // El panel en la esquina de la llave: el borde de abajo, el de la llave (que puede estar moviéndose); el borde izquierdo,
        // el del lugar de destino (PanelFor). It. 4b (DD4b-04; 04b/00-deuda-diseno §3.2): el alto máximo se calcula EN CADA CUADRO
        // con el borde de abajo de ese cuadro (SimulatorPanelLayout.MaxHeight), no con el del destino: si el contenido no entra,
        // el borde de arriba queda quieto y el panel se estira o se encoge con la llave; si entra, se traslada entero. Nunca salta
        // entre una pestaña y una pantalla apilada.
        private void ApplyPanel(double bottom)
        {
            if (Panel == null)
            {
                return;
            }

            Panel.style.left = (float)_anchor.Left;
            Panel.style.right = StyleKeyword.Auto;
            Panel.style.bottom = (float)bottom;
            Panel.style.maxHeight = (float)FrameMaxHeight(bottom);
        }

        private double FrameMaxHeight(double bottom)
        {
            var height = _layout.WindowSize.y > 0 ? _layout.WindowSize.y : layout.height;
            if (!(height > 0))
            {
                return _anchor.MaxHeight;
            }

            var surface = _layout.Surface;
            var workout = surface.Kind == OverlayContextKind.Workout;
            return SimulatorPanelLayout.MaxHeight(height, _layout.Insets, bottom, workout ? SimulatorPanelLayout.WorkoutTopReserve : SimulatorPanelLayout.AppTopReserve,
                workout ? surface.PanelCeiling : null);
        }

        // La llave como obstáculo inferior mientras se ve (el aviso no la tapa ni ella al aviso, DN3-13), en su lugar de destino.
        private void UpdateObstacle()
        {
            if (_disposed || IsOpen || _spot == null)
            {
                _layout.SetBottomObstacle(this, null);
                return;
            }

            _layout.SetBottomObstacle(this, _spot.Value.Obstacle);
        }

        // El cambio de carril de la llave (y del panel abierto, que la sigue): sale en 90 (ease.exit) sin recibir toques, espera a
        // que termine la transición de la página (PageStack) y entra en el lugar nuevo con su transición (la llave 0,85 → 1 en
        // 160; el panel, la de la It. 2 desde su esquina nueva). Finita: cuenta para Settle y para la regla de toma.
        private sealed class LaneSwap : IMotion
        {
            private readonly SimulatorLayerView _owner;
            private double _start;
            private bool _fadingOut;
            private bool _running;

            public LaneSwap(SimulatorLayerView owner)
            {
                _owner = owner;
            }

            public bool IsFinite => true;

            public bool IsRunning => _running;

            public void Start()
            {
                var motion = _owner._ui.Motion;
                _owner._fabIn.Finish();
                _owner._panelIn.Finish();
                _start = motion.Now;
                _fadingOut = true;
                if (!_running)
                {
                    _running = true;
                    motion.Add(this);
                }

                Target().pickingMode = PickingMode.Ignore;
            }

            public void Stop()
            {
                if (!_running)
                {
                    return;
                }

                _running = false;
                _owner._ui.Motion.Remove(this);
                var spot = _owner._spot;
                if (spot != null)
                {
                    _owner.Fab.style.left = (float)spot.Value.Left;
                }

                Restore();
            }

            public bool Tick(double nowSeconds)
            {
                if (!_running)
                {
                    return false;
                }

                var target = Target();
                if (_fadingOut)
                {
                    var elapsed = (nowSeconds - _start) * 1000.0;
                    var u = elapsed >= LaneOutMs ? 1 : elapsed <= 0 ? 0 : elapsed / LaneOutMs;
                    target.style.opacity = (float)(1 - Easing.Evaluate(Tokens.EaseExit, u));
                    if (u < 1)
                    {
                        return true;
                    }

                    _fadingOut = false;
                    target.style.opacity = 0;
                }

                // It. 4b (§5.7): también espera la cascada del inmersivo (la página retiene la llave hasta ImmersiveEntryEndMs) y la
                // entrada o la salida de un diálogo (R10: con el de rutina completada cambia de carril cuando termina su entrada).
                if (_owner._ui.Pages.IsTransitioning || _owner._layout.WrenchHeld || _owner._ui.Dialogs.IsTransitioning)
                {
                    return true;
                }

                // Terminó la transición: el lugar nuevo, en el acto, y la entrada.
                _running = false;
                var spot = _owner._spot;
                if (spot != null)
                {
                    _owner.Fab.style.left = (float)spot.Value.Left;
                    _owner._fabBottom.Snap(spot.Value.Bottom);
                }

                Restore();
                if (_owner.IsOpen && _owner.Panel != null)
                {
                    _owner.ApplyPanel(_owner.FabBottom);
                    _owner._panelIn.OriginLeft = _owner._anchor.GrowsRight;
                    _owner._panelIn.Play();
                }
                else
                {
                    _owner._fabIn.Play();
                }

                _owner.UpdateObstacle();
                return false;
            }

            private VisualElement Target()
            {
                return _owner.IsOpen && _owner.Panel != null ? _owner.Panel : _owner.Fab;
            }

            private void Restore()
            {
                _owner.Fab.style.opacity = StyleKeyword.Null;
                _owner.Fab.pickingMode = PickingMode.Position;
                if (_owner.Panel != null)
                {
                    _owner.Panel.style.opacity = StyleKeyword.Null;
                    _owner.Panel.pickingMode = PickingMode.Position;
                }
            }
        }
    }
}
