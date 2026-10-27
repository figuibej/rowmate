using System;
using RowMate.Core.Ports;
using UnityEngine;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Surface
{
    // Escala del panel por densidad (D-03, AC-APP-05-09): un solo modo en todas las plataformas, ConstantPixelSize con
    // scale = IDisplayMetrics.DpScale (1 dp = 1 px × densidad; Android DisplayMetrics.density, Windows dpi / 96). Recalcula
    // cada cuadro si cambiaron el tamaño de la pantalla, los dpi o Screen.safeArea (rotar o redimensionar no recrea nada,
    // AC-APP-02-07). refresh vuelve a leer la densidad y el área segura del sistema (DisplayMetricsProvider.Refresh).
    public sealed class PanelScaleController
    {
        private readonly PanelSettings _panel;
        private readonly IDisplayMetrics _display;
        private readonly Action? _refresh;
        private int _width = -1;
        private int _height = -1;
        private float _dpi = -1;
        private Rect _safe;

        public PanelScaleController(PanelSettings panel, IDisplayMetrics display, Action? refresh)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _refresh = refresh;
            _panel.scaleMode = PanelScaleMode.ConstantPixelSize;
        }

        public int Width => _width;

        public int Height => _height;

        // true si algo cambió (y la escala quedó actualizada).
        public bool Update()
        {
            var width = Screen.width;
            var height = Screen.height;
            var dpi = Screen.dpi;
            var safe = Screen.safeArea;
            if (width == _width && height == _height && Mathf.Approximately(dpi, _dpi) && safe == _safe)
            {
                return false;
            }

            _width = width;
            _height = height;
            _dpi = dpi;
            _safe = safe;
            _refresh?.Invoke();
            var scale = (float)(_display.DpScale > 0 ? _display.DpScale : 1.0);
            if (!Mathf.Approximately(_panel.scale, scale))
            {
                _panel.scale = scale;
            }

            return true;
        }
    }
}
