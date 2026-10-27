using System;
using System.Diagnostics;
using RowMate.Core.Design;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Presentation.Surface
{
    // Presentador preferido de GammaRt (02-tech-design §3.1, S-B1): en cada cuadro dibujado, Graphics.CopyTexture de la RT
    // UNorm (bytes sRGB compuestos como el legacy) a una RT sRGB del mismo tamaño (copia de bytes), y un segundo panel
    // Overlay (sortingOrder alto) con un único elemento a pantalla completa con background-image = la RT sRGB y
    // pickingMode Ignore. Al muestrear la sRGB la GPU decodifica a lineal y el backbuffer sRGB vuelve a codificar: quedan
    // los mismos bytes (P2 de QA, sameBytes=True en D3D11). La copia va en RenderPipelineManager.beginContextRendering, el
    // primer gancho de render posterior al repintado de los paneles con targetTexture (latencia 0, criterio 5 de S-B1).
    // Solo UI Toolkit, sin shader ni cambios de URP. Riesgo: CopyTexture UNorm → sRGB en GLES 3.0 (R2-02 → BlitPresenter).
    // El GameObject del panel va en la RAÍZ de la escena: un UIDocument hijo del GameObject de otro UIDocument se anida en el
    // panel del padre (su PanelSettings se ignora). Colgado de AppRoot, la copia se dibujaba dentro de la propia RT y la
    // pantalla quedaba en el color de la cámara (S-B1, build de Windows: RT y copia bien, backbuffer #0D1B2A).
    public sealed class OverlayCopyPresenter : IUiPresenter
    {
        public const int OverlaySortingOrder = 30000;

        private readonly GameObject _host;
        private readonly PanelSettings _panel;
        private readonly UIDocument _document;
        private readonly VisualElement _image;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private RenderTexture? _source;
        private RenderTexture? _copy;
        private bool _disposed;

        public OverlayCopyPresenter(ThemeStyleSheet? theme)
        {
            _panel = ScriptableObject.CreateInstance<PanelSettings>();
            _panel.name = "RowMate.UiPresenterPanel";
            _panel.hideFlags = HideFlags.HideAndDontSave;
            _panel.themeStyleSheet = theme;
            _panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            _panel.scale = 1;
            _panel.sortingOrder = OverlaySortingOrder;
            _panel.clearColor = false;
            _host = new GameObject("RowMate.UiPresenter");
            _host.SetActive(false);
            _document = _host.AddComponent<UIDocument>();
            _document.panelSettings = _panel;
            _host.SetActive(true);
            var root = _document.rootVisualElement;
            _image = new VisualElement { name = "ui-presenter-image" };
            _image.pickingMode = PickingMode.Ignore;
            _image.style.position = Position.Absolute;
            _image.style.left = 0;
            _image.style.top = 0;
            _image.style.right = 0;
            _image.style.bottom = 0;
            _image.style.backgroundColor = TokenColors.ToColor(Tokens.BgApp);
            if (root != null)
            {
                root.pickingMode = PickingMode.Ignore;
                root.style.position = Position.Absolute;
                root.style.left = 0;
                root.style.top = 0;
                root.style.right = 0;
                root.style.bottom = 0;
                root.Add(_image);
            }

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
        }

        public double LastCostMs { get; private set; }

        public int CopiesDone { get; private set; }

        // La RT sRGB que muestra el panel Overlay (diagnóstico de S-B1).
        public RenderTexture? Output => _copy;

        public void SetSource(RenderTexture gammaUi)
        {
            _source = gammaUi;
            var previous = _copy;
            _copy = UiSurface.CreateTarget(gammaUi.width, gammaUi.height, GraphicsFormat.R8G8B8A8_SRGB, "RowMate.UiPresent");
            _image.style.backgroundImage = Background.FromRenderTexture(_copy);
            if (previous != null)
            {
                previous.Release();
                Object.Destroy(previous);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            if (_host != null)
            {
                Object.Destroy(_host);
            }

            if (_panel != null)
            {
                Object.Destroy(_panel);
            }

            if (_copy != null)
            {
                _copy.Release();
                Object.Destroy(_copy);
                _copy = null;
            }
        }

        private void OnBeginContextRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            Copy();
        }

        public void Copy()
        {
            if (_source == null || _copy == null || !_source.IsCreated())
            {
                return;
            }

            _stopwatch.Restart();
            Graphics.CopyTexture(_source, _copy);
            _stopwatch.Stop();
            LastCostMs = _stopwatch.Elapsed.TotalMilliseconds;
            CopiesDone++;
        }
    }
}
