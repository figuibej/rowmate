using System;
using System.Collections.Generic;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // La regla ÚNICA de área segura (02-tech-design §3.1 de la It. 2, QA-G2-06): cada margen lo consume una sola pieza, como
    // en el legacy (AppBar = SafeArea(bottom: false), NavigationBar = SafeArea, cuerpo sin relleno):
    // - #app-root: nada (sin padding, ocupa todo el panel).
    // - .rm-appbar: superior y laterales (padding-top = Top, padding-left/right = Left/Right; alto 56 + Top).
    // - #tab-bar: inferior y laterales (padding-bottom = Bottom, padding-left/right; alto 64 + Bottom).
    // - El contenido de cada pantalla: nada lateral; abajo, el alto REAL de #tab-bar como padding dentro de lo desplazable
    //   (DN-23), leído de #tab-bar.layout.height en GeometryChangedEvent.
    // - #simulator-layer: nada (la cinta va en la esquina de la pantalla).
    // Poll() vuelve a mirar el área segura (una vez por cuadro, desde ShellHandle.Tick): rotar o redimensionar no recrea nada.
    // It. 3 (03-rutinas/02-tech-design §4.8, DN3-13, DN3-14):
    // - El alto de la barra se CONSERVA mientras está oculta (display: none con el editor encima): el último alto visible.
    // - Obstáculos inferiores (el + de Rutinas, la llave del simulador): cada dueño registra su rectángulo en dp de la
    //   ventana (x desde la izquierda, y desde ABAJO). Los usan el aviso (BottomObstacles.Lift, gap 8), la llave (gap 16) y
    //   el relleno inferior de los contenidos desplazables (RegisterClearance: tope del obstáculo más alto que se cruza + 16).
    // It. 4a (D-ART-10, DD4a-01; 02-tech-design §6.10):
    // - La SUPERFICIE para la llave del simulador (OverlaySurface, de Core): cada pantalla declara la suya (SetSurface) y manda
    //   la de arriba de la pila (la última que se declaró; volver a declarar no cambia su lugar). Rutinas, una pestaña; el
    //   editor, una pantalla apilada; el inmersivo, el entrenamiento con el lugar que calcula HudGeometry. Sin ninguna, una
    //   pestaña. Una pestaña siempre usa el alto VIGENTE de la barra. El + de Rutinas ya no es obstáculo de la llave (sigue
    //   siéndolo del aviso y del relleno).
    // - La llave misma (Wrench): la registra la capa del simulador (Presentation no la conoce de otro modo); la usan los
    //   menús, que la esquivan (DD4a-02), y el anillo de Tab del inmersivo (DN4a-10).
    public sealed class ShellLayout
    {
        public const float AppBarHeight = 56;
        public const float TabBarHeight = 64;

        // Aire entre el contenido desplazable y el obstáculo más alto que se cruza con él (DN3-14).
        public const double ClearanceGapDp = 16;

        private readonly IDisplayMetrics _display;
        private readonly List<VisualElement> _appBars = new List<VisualElement>();
        private readonly List<(VisualElement Element, float Base)> _bottomPaddings = new List<(VisualElement, float)>();
        private readonly List<(object Owner, BottomObstacle Obstacle)> _obstacles = new List<(object, BottomObstacle)>();
        private readonly List<Clearance> _clearances = new List<Clearance>();
        private readonly List<BottomObstacle> _scratch = new List<BottomObstacle>();
        private readonly List<(object Owner, OverlaySurface Surface)> _surfaces = new List<(object, OverlaySurface)>();
        private VisualElement? _tabBar;
        private VisualElement? _window;
        private SafeInsets _insets;
        private float _tabBarHeight = TabBarHeight;
        private Vector2 _windowSize;

        public ShellLayout(IDisplayMetrics display)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _insets = display.SafeArea;
        }

        public SafeInsets Insets => _insets;

        // Alto real de la barra de pestañas (64 + margen inferior), del último layout VISIBLE (se conserva con la barra oculta).
        public float CurrentTabBarHeight => _tabBarHeight;

        // Tamaño de la ventana en dp (el de #app-root), del último layout.
        public Vector2 WindowSize => _windowSize;

        // La barra se ve (Rutinas, Dispositivo) o la tapa una página (el editor).
        public bool TabBarVisible => _tabBar != null && _tabBar.resolvedStyle.display != DisplayStyle.None;

        // Cambió el área segura, el alto real de la barra o el tamaño de la ventana.
        public event Action? Changed;

        // Cambió algún obstáculo inferior.
        public event Action? ObstaclesChanged;

        // Cambió la superficie de arriba para la llave (D-ART-10).
        public event Action? SurfaceChanged;

        // La llave del simulador (null sin la capa o antes de armarla).
        public VisualElement? Wrench { get; private set; }

        // La superficie vigente: la última declarada (una pestaña siempre con el alto vigente de la barra); sin ninguna, una
        // pestaña.
        public OverlaySurface Surface
        {
            get
            {
                if (_surfaces.Count == 0)
                {
                    return OverlaySurface.Tab(_tabBarHeight);
                }

                var top = _surfaces[_surfaces.Count - 1].Surface;
                return top.Kind == OverlayContextKind.Tab && Math.Abs(top.TabBarHeight - _tabBarHeight) > 0.01
                    ? OverlaySurface.Tab(_tabBarHeight, top.Obstacles)
                    : top;
            }
        }

        // Declara (o con null retira) la superficie de owner. Volver a declarar la reemplaza en su lugar de la pila.
        public void SetSurface(object owner, OverlaySurface? surface)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            for (var i = 0; i < _surfaces.Count; i++)
            {
                if (!ReferenceEquals(_surfaces[i].Owner, owner))
                {
                    continue;
                }

                if (surface == null)
                {
                    _surfaces.RemoveAt(i);
                    SurfaceChanged?.Invoke();
                    return;
                }

                if (_surfaces[i].Surface.Equals(surface))
                {
                    return;
                }

                _surfaces[i] = (owner, surface);
                if (i == _surfaces.Count - 1)
                {
                    SurfaceChanged?.Invoke();
                }

                return;
            }

            if (surface != null)
            {
                _surfaces.Add((owner, surface));
                SurfaceChanged?.Invoke();
            }
        }

        public bool HasSurface(object owner)
        {
            for (var i = 0; i < _surfaces.Count; i++)
            {
                if (ReferenceEquals(_surfaces[i].Owner, owner))
                {
                    return true;
                }
            }

            return false;
        }

        // La capa del simulador registra su llave (o null al irse).
        public void RegisterWrench(VisualElement? wrench)
        {
            Wrench = wrench;
        }

        // La caja de la llave en coordenadas del panel si se ve y se puede tocar (para los menús, DD4a-02); null si no.
        public Rect? VisibleWrenchBounds()
        {
            var wrench = Wrench;
            if (wrench == null || wrench.panel == null || wrench.resolvedStyle.display == DisplayStyle.None || wrench.resolvedStyle.opacity < 0.01f)
            {
                return null;
            }

            var bounds = wrench.worldBound;
            return bounds.width > 0 ? bounds : (Rect?)null;
        }

        public void AttachWindow(VisualElement window)
        {
            _window = window;
            window.RegisterCallback<GeometryChangedEvent>(OnWindowGeometry);
        }

        public void AttachTabBar(VisualElement tabBar)
        {
            _tabBar = tabBar;
            tabBar.RegisterCallback<GeometryChangedEvent>(OnTabBarGeometry);
            ApplyTabBar();
        }

        public void RegisterAppBar(VisualElement appBar)
        {
            _appBars.Add(appBar);
            ApplyAppBar(appBar);
        }

        // Una página que se va (el editor) deja de recibir el área segura.
        public void UnregisterAppBar(VisualElement appBar)
        {
            _appBars.Remove(appBar);
        }

        // padding-bottom = basePadding + alto real de la barra, dentro del contenido desplazable de una pantalla.
        public void RegisterBottomPadding(VisualElement content, float basePadding)
        {
            _bottomPaddings.Add((content, basePadding));
            content.style.paddingBottom = basePadding + _tabBarHeight;
        }

        // padding-bottom del contenido de un desplazable que llega al borde inferior de la ventana: el tope del obstáculo más
        // alto que se cruza con su ancho + 16, como mínimo minimum() (dp desde abajo); más extra() (teclado). DN3-14:
        // Rutinas en TV, barra + 88 = 152 con el +, 208 con la llave encima; el editor, 24 sin obstáculos y 136 con la llave.
        public void RegisterClearance(VisualElement scrollView, VisualElement content, Func<double> minimum, Func<double>? extra = null)
        {
            var entry = new Clearance(scrollView, content, minimum, extra);
            _clearances.Add(entry);
            scrollView.RegisterCallback<GeometryChangedEvent>(_ => ApplyClearance(entry));
            ApplyClearance(entry);
        }

        public void UnregisterClearance(VisualElement scrollView)
        {
            for (var i = _clearances.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_clearances[i].ScrollView, scrollView))
                {
                    _clearances.RemoveAt(i);
                }
            }
        }

        // Vuelve a calcular los rellenos inferiores (el teclado cambió, el contenido se movió).
        public void RefreshClearances()
        {
            for (var i = 0; i < _clearances.Count; i++)
            {
                ApplyClearance(_clearances[i]);
            }
        }

        // Pone (o con null saca) el obstáculo de owner. Los dueños: RoutinesScreenView (el +) y SimulatorLayerView (la llave).
        public void SetBottomObstacle(object owner, BottomObstacle? obstacle)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            for (var i = 0; i < _obstacles.Count; i++)
            {
                if (!ReferenceEquals(_obstacles[i].Owner, owner))
                {
                    continue;
                }

                if (obstacle == null)
                {
                    _obstacles.RemoveAt(i);
                    NotifyObstacles();
                    return;
                }

                var old = _obstacles[i].Obstacle;
                var value = obstacle.Value;
                if (Math.Abs(old.Left - value.Left) < 0.01 && Math.Abs(old.Right - value.Right) < 0.01 && Math.Abs(old.Bottom - value.Bottom) < 0.01
                    && Math.Abs(old.Height - value.Height) < 0.01)
                {
                    return;
                }

                _obstacles[i] = (owner, value);
                NotifyObstacles();
                return;
            }

            if (obstacle != null)
            {
                _obstacles.Add((owner, obstacle.Value));
                NotifyObstacles();
            }
        }

        // Los obstáculos actuales (sin el de except), en la lista dada.
        public void CopyObstacles(List<BottomObstacle> into, object? except = null)
        {
            into.Clear();
            for (var i = 0; i < _obstacles.Count; i++)
            {
                if (!ReferenceEquals(_obstacles[i].Owner, except))
                {
                    into.Add(_obstacles[i].Obstacle);
                }
            }
        }

        public bool HasObstacle(object owner)
        {
            for (var i = 0; i < _obstacles.Count; i++)
            {
                if (ReferenceEquals(_obstacles[i].Owner, owner))
                {
                    return true;
                }
            }

            return false;
        }

        public void Poll()
        {
            var insets = _display.SafeArea;
            if (Same(insets, _insets))
            {
                return;
            }

            _insets = insets;
            for (var i = 0; i < _appBars.Count; i++)
            {
                ApplyAppBar(_appBars[i]);
            }

            ApplyTabBar();
            RefreshClearances();
            Changed?.Invoke();
        }

        private void NotifyObstacles()
        {
            RefreshClearances();
            ObstaclesChanged?.Invoke();
        }

        private void ApplyClearance(Clearance entry)
        {
            var bounds = entry.ScrollView.worldBound;
            var origin = _window != null ? _window.worldBound.xMin : 0;
            var left = bounds.width > 0 ? bounds.xMin - origin : 0;
            var right = bounds.width > 0 ? bounds.xMax - origin : _windowSize.x;
            CopyObstacles(_scratch);
            var value = BottomObstacles.ClearanceAbove(left, right, _scratch, ClearanceGapDp, entry.Minimum());
            if (entry.Extra != null)
            {
                value += entry.Extra();
            }

            var padding = (float)value;
            var current = entry.Content.style.paddingBottom;
            if (current.keyword != StyleKeyword.Undefined || Math.Abs(current.value.value - padding) > 0.01f)
            {
                entry.Content.style.paddingBottom = padding;
            }
        }

        private void ApplyAppBar(VisualElement appBar)
        {
            appBar.style.paddingTop = (float)_insets.Top;
            appBar.style.paddingLeft = (float)_insets.Left;
            appBar.style.paddingRight = (float)_insets.Right;
            appBar.style.height = AppBarHeight + (float)_insets.Top;
        }

        private void ApplyTabBar()
        {
            if (_tabBar == null)
            {
                return;
            }

            _tabBar.style.paddingBottom = (float)_insets.Bottom;
            _tabBar.style.paddingLeft = (float)_insets.Left;
            _tabBar.style.paddingRight = (float)_insets.Right;
            _tabBar.style.height = TabBarHeight + (float)_insets.Bottom;
        }

        private void OnTabBarGeometry(GeometryChangedEvent evt)
        {
            // Con la barra oculta (display: none) llega 0: se conserva el último alto visible (la llave no salta, §4.8).
            var height = evt.newRect.height;
            if (!(height > 0) || Mathf.Abs(height - _tabBarHeight) < 0.01f)
            {
                return;
            }

            _tabBarHeight = height;
            for (var i = 0; i < _bottomPaddings.Count; i++)
            {
                var entry = _bottomPaddings[i];
                entry.Element.style.paddingBottom = entry.Base + height;
            }

            RefreshClearances();
            Changed?.Invoke();
        }

        private void OnWindowGeometry(GeometryChangedEvent evt)
        {
            var size = evt.newRect.size;
            if (!(size.x > 0) || (Mathf.Abs(size.x - _windowSize.x) < 0.01f && Mathf.Abs(size.y - _windowSize.y) < 0.01f))
            {
                return;
            }

            _windowSize = size;
            RefreshClearances();
            Changed?.Invoke();
        }

        private static bool Same(SafeInsets a, SafeInsets b)
        {
            return Math.Abs(a.Left - b.Left) < 0.01 && Math.Abs(a.Top - b.Top) < 0.01 && Math.Abs(a.Right - b.Right) < 0.01
                   && Math.Abs(a.Bottom - b.Bottom) < 0.01;
        }

        private sealed class Clearance
        {
            public Clearance(VisualElement scrollView, VisualElement content, Func<double> minimum, Func<double>? extra)
            {
                ScrollView = scrollView;
                Content = content;
                Minimum = minimum;
                Extra = extra;
            }

            public VisualElement ScrollView { get; }

            public VisualElement Content { get; }

            public Func<double> Minimum { get; }

            public Func<double>? Extra { get; }
        }
    }
}
