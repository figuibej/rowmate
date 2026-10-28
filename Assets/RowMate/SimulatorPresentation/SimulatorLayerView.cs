using System;
using System.Collections.Generic;
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
    //   ├── #sim-open .rm-sim-fab (llave)             40 × 40, a 16 de la derecha y FabBottom(alto real de la barra)
    //   ├── #sim-panel .rm-sim-panel                  280 de ancho, alto máximo MaxHeight(alto de la ventana, márgenes)
    //   └── #sim-ribbon .rm-sim-ribbon                último hijo, sin eventos
    // Con el panel cerrado (como arranca) se ve la llave; al tocarla el panel abre en el mismo lugar y la llave se oculta;
    // #sim-close lo cierra y vuelve la llave (AC-BLE-16-02). Abrir: PopTransition (opacidad 160, escala 0,92 → 1 en 240);
    // cerrar: el panel se oculta en el acto y la llave vuelve con 0,85 → 1 en 160 (DN-14). Con store == null (UiTestHost,
    // capturas "Prueba", QA-G2-05) la capa tiene cinta y llave, sin panel: tocar la llave no hace nada.
    // It. 3 (DV-13, DN3-14; AC-BLE-16-02 con el texto nuevo): la llave se aparta de los obstáculos inferiores (el + de
    // Rutinas, mientras Rutinas es la pestaña visible y ninguna página la tapa) con SimulatorPanelLayout.FabBottom: barra + 16,
    // o 16 dp sobre el + (barra + 88), con la x fija; va de un lugar al otro solo en y con un resorte ω 22 (en el acto con
    // reducir movimiento) y se puede tocar mientras se mueve. La barra usa su alto CONSERVADO (con el editor encima no salta).
    // La llave también es un obstáculo (para el aviso y el relleno del contenido) mientras se ve. El panel se abre SIEMPRE en
    // el mismo lugar: PanelBottom(barra) = barra + 16.
    public sealed class SimulatorLayerView : VisualElement, IDisposable
    {
        private readonly SimulatorPanelStore? _store;
        private readonly ShellLayout _layout;
        private readonly VisualElement _tabBar;
        private readonly PopTransition _panelIn;
        private readonly PopTransition _fabIn;
        private readonly SpringMotion _fabBottom;
        private readonly List<BottomObstacle> _obstacles = new List<BottomObstacle>();
        private bool? _open;
        private bool _disposed;

        public SimulatorLayerView(UiContext ui, SimulatorPanelStore? store, ShellLayout layout, VisualElement tabBar)
        {
            _store = store;
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _tabBar = tabBar ?? throw new ArgumentNullException(nameof(tabBar));
            name = "simulator-layer";
            AddToClassList("rm-layer");
            AddToClassList("rm-sim-layer");
            pickingMode = PickingMode.Ignore;

            Fab = new Button(() => _store?.Open()) { name = "sim-open", text = "" };
            RmButton.StripUnityChrome(Fab);
            Fab.AddToClassList("rm-sim-fab");
            Fab.focusable = true;
            Fab.Add(RmButton.StateLayer());
            Fab.Add(new IconView(Icons.Build, false, 24));
            Fab.Add(RmButton.FocusRing());
            Add(Fab);
            _fabIn = new PopTransition(ui.Motion, Fab, MotionSpecs.FabInScaleFrom, Tokens.DurFast.Milliseconds, Tokens.DurFast.Milliseconds, 120);

            if (store != null)
            {
                Panel = new SimulatorPanelView(ui, store);
                Panel.style.display = DisplayStyle.None;
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
            RegisterCallback<GeometryChangedEvent>(_ => Place());
            _layout.Changed += Place;
            _layout.ObstaclesChanged += Place;
            if (store != null)
            {
                this.Bind(store.State, OnState);
            }

            Place();
        }

        public Button Fab { get; }

        public SimulatorPanelView? Panel { get; }

        public SimulatorRibbon Ribbon { get; }

        public SimulatorPanelStore? Store => _store;

        public bool IsOpen => _open == true;

        // La llave se está moviendo (regla de toma: ningún resorte en curso).
        public bool IsMoving => _fabBottom.IsRunning;

        // Distancia actual de la llave al borde inferior, en dp.
        public double FabBottom => _fabBottom.HasValue ? _fabBottom.Value : SimulatorPanelLayout.FabBottom(_layout.CurrentTabBarHeight);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _layout.Changed -= Place;
            _layout.ObstaclesChanged -= Place;
            _fabBottom.Stop();
            _layout.SetBottomObstacle(this, null);
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
                Panel.style.display = DisplayStyle.Flex;
                if (!first)
                {
                    _panelIn.Play();
                }
            }
            else
            {
                _panelIn.Finish();
                Panel.style.display = DisplayStyle.None;
                Fab.style.display = DisplayStyle.Flex;
                UpdateObstacle();
                if (!first)
                {
                    _fabIn.Play();
                }
            }
        }

        // Llave en FabBottom(barra, obstáculos) (DV-13) y panel en PanelBottom(barra) (siempre el mismo lugar); alto máximo del
        // panel con la regla de D-26.
        private void Place()
        {
            if (_disposed)
            {
                return;
            }

            var bar = _layout.CurrentTabBarHeight;
            var width = _layout.WindowSize.x > 0 ? _layout.WindowSize.x : layout.width;
            _layout.CopyObstacles(_obstacles, this);
            var target = width > 0 ? SimulatorPanelLayout.FabBottom(width, bar, _obstacles) : SimulatorPanelLayout.FabBottom(bar);
            Fab.style.right = (float)SimulatorPanelLayout.FabRight;
            _fabBottom.MoveTo(target, MotionSpecs.KeyOmega);
            if (Panel == null)
            {
                return;
            }

            Panel.style.bottom = (float)SimulatorPanelLayout.PanelBottom(bar);
            Panel.style.right = (float)SimulatorPanelLayout.FabRight;
            var height = layout.height > 0 ? layout.height : _tabBar.panel?.visualTree.layout.height ?? 0;
            if (height > 0)
            {
                Panel.style.maxHeight = (float)SimulatorPanelLayout.MaxHeight(height, _layout.Insets);
            }
        }

        private void OnFabBottom(double bottom)
        {
            Fab.style.bottom = (float)bottom;
            UpdateObstacle();
        }

        // La llave como obstáculo inferior mientras se ve (el aviso no la tapa ni ella al aviso, DN3-13).
        private void UpdateObstacle()
        {
            var width = _layout.WindowSize.x;
            if (_disposed || IsOpen || !(width > 0))
            {
                _layout.SetBottomObstacle(this, null);
                return;
            }

            _layout.SetBottomObstacle(this, SimulatorPanelLayout.FabObstacle(width, FabBottom));
        }
    }
}
