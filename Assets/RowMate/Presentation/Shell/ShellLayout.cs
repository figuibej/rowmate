using System;
using System.Collections.Generic;
using RowMate.Core.Ports;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Shell
{
    // La regla ÚNICA de área segura (02-tech-design §3.1, QA-G2-06): cada margen lo consume una sola pieza, como en el legacy
    // (AppBar = SafeArea(bottom: false), NavigationBar = SafeArea, cuerpo sin relleno):
    // - #app-root: nada (sin padding, ocupa todo el panel).
    // - .rm-appbar: superior y laterales (padding-top = Top, padding-left/right = Left/Right; alto 56 + Top).
    // - #tab-bar: inferior y laterales (padding-bottom = Bottom, padding-left/right; alto 64 + Bottom).
    // - El contenido de cada pantalla: nada lateral; abajo, el alto REAL de #tab-bar como padding dentro de lo desplazable
    //   (DN-23), leído de #tab-bar.layout.height en GeometryChangedEvent.
    // - #simulator-layer: nada (la cinta va en la esquina de la pantalla).
    // Poll() vuelve a mirar el área segura (una vez por cuadro, desde ShellHandle.Tick): rotar o redimensionar no recrea nada.
    public sealed class ShellLayout
    {
        public const float AppBarHeight = 56;
        public const float TabBarHeight = 64;

        private readonly IDisplayMetrics _display;
        private readonly List<VisualElement> _appBars = new List<VisualElement>();
        private readonly List<(VisualElement Element, float Base)> _bottomPaddings = new List<(VisualElement, float)>();
        private VisualElement? _tabBar;
        private SafeInsets _insets;
        private float _tabBarHeight = TabBarHeight;

        public ShellLayout(IDisplayMetrics display)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _insets = display.SafeArea;
        }

        public SafeInsets Insets => _insets;

        // Alto real de la barra de pestañas (64 + margen inferior), del último layout.
        public float CurrentTabBarHeight => _tabBarHeight;

        // Cambió el área segura o el alto real de la barra.
        public event Action? Changed;

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

        // padding-bottom = basePadding + alto real de la barra, dentro del contenido desplazable de una pantalla.
        public void RegisterBottomPadding(VisualElement content, float basePadding)
        {
            _bottomPaddings.Add((content, basePadding));
            content.style.paddingBottom = basePadding + _tabBarHeight;
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
            Changed?.Invoke();
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

            Changed?.Invoke();
        }

        private static bool Same(SafeInsets a, SafeInsets b)
        {
            return Math.Abs(a.Left - b.Left) < 0.01 && Math.Abs(a.Top - b.Top) < 0.01 && Math.Abs(a.Right - b.Right) < 0.01
                   && Math.Abs(a.Bottom - b.Bottom) < 0.01;
        }
    }
}
