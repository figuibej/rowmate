using System;
using RowMate.Core.Design;
using RowMate.Core.Ports;
using RowMate.Presentation.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

namespace RowMate.Presentation.Surface
{
    // Dónde se dibuja el panel de la app (02-tech-design §3.1, D-01, QA-G2-13). Con GammaRt: RT R8G8B8A8_UNorm del tamaño
    // de la pantalla + forceGammaRendering, presentada por un IUiPresenter (S-B1). Con Overlay: el panel va directo a la
    // pantalla (composición lineal). En las dos, la escala es la densidad (PanelScaleController).
    // - Las RT se limpian a bg.app al crearlas y al recrearlas (una RT recién creada tiene contenido indefinido; AC-APP-01-07).
    // - Update() una vez por cuadro (AppRoot.Update): tamaño, escala, área segura; recrea la RT si cambió el tamaño.
    // runtimePanel es una COPIA del asset (AppRoot hace Instantiate): el asset no se toca en tiempo de ejecución (R2-05).
    public sealed class UiSurface : IDisposable
    {
        private readonly PanelScaleController _scale;
        private readonly IUiPresenter? _presenter;
        private RenderTexture? _target;
        private bool _disposed;

        public UiSurface(PanelSettings runtimePanel, IDisplayMetrics display, UiColorPipeline pipeline, IUiPresenter? presenter, Action? refreshDisplay)
        {
            Panel = runtimePanel ?? throw new ArgumentNullException(nameof(runtimePanel));
            Pipeline = pipeline;
            _presenter = pipeline == UiColorPipeline.GammaRt ? presenter : null;
            _scale = new PanelScaleController(runtimePanel, display, refreshDisplay);
            Panel.clearColor = true;
            Panel.colorClearValue = ClearValue(pipeline);
            Panel.forceGammaRendering = pipeline == UiColorPipeline.GammaRt;
            if (pipeline == UiColorPipeline.Overlay)
            {
                Panel.targetTexture = null;
            }

            Update();
        }

        public PanelSettings Panel { get; }

        public UiColorPipeline Pipeline { get; }

        public RenderTexture? Target => _target;

        public IUiPresenter? Presenter => _presenter;

        public void Update()
        {
            if (_disposed)
            {
                return;
            }

            var changed = _scale.Update();
            if (Pipeline != UiColorPipeline.GammaRt)
            {
                return;
            }

            if (_target != null && !changed && _target.width == _scale.Width && _target.height == _scale.Height)
            {
                return;
            }

            if (_target != null && _target.width == _scale.Width && _target.height == _scale.Height)
            {
                return;
            }

            var previous = _target;
            _target = CreateTarget(Math.Max(1, _scale.Width), Math.Max(1, _scale.Height), GraphicsFormat.R8G8B8A8_UNorm, "RowMate.UiGamma");
            Panel.targetTexture = _target;
            _presenter?.SetSource(_target);
            if (previous != null)
            {
                previous.Release();
                UnityEngine.Object.Destroy(previous);
            }
        }

        // El color de limpieza del panel para que se vea bg.app. Con Overlay el panel limpia un destino sRGB (el backbuffer o la
        // RT sRGB del arnés) y el valor se codifica: va en lineal, como en Clear (medido en S-B1: el primer cuadro salía #405B71).
        public static Color ClearValue(UiColorPipeline pipeline)
        {
            var color = TokenColors.ToColor(Tokens.BgApp);
            return pipeline == UiColorPipeline.Overlay && QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        }

        // RT limpia a bg.app (QA-G2-13; sonda §8.3: la primera lectura después del GL.Clear da #0D1B2A).
        public static RenderTexture CreateTarget(int width, int height, GraphicsFormat format, string name)
        {
            // Con stencil: UI Toolkit enmascara con stencil los overflow: hidden con esquinas redondeadas (el panel del
            // simulador, el cuerpo de la tarjeta mientras anima). Sin él la máscara sale como un rectángulo blanco (arnés, CAP-16).
            var descriptor = new RenderTextureDescriptor(width, height, format, GraphicsFormat.D24_UNorm_S8_UInt, 0)
            {
                useMipMap = false,
                autoGenerateMips = false
            };
            var target = new RenderTexture(descriptor)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave
            };
            target.Create();
            Clear(target);
            return target;
        }

        // GL.Clear escribe el valor tal cual: en una RT UNorm (GammaRt) los bytes son los del token; en una sRGB (Overlay, la
        // copia del presentador) el valor se codifica, así que se pasa el color en lineal (medido: sin esto salía #405B71).
        public static void Clear(RenderTexture target)
        {
            var active = RenderTexture.active;
            RenderTexture.active = target;
            var color = TokenColors.ToColor(Tokens.BgApp);
            GL.Clear(true, true, target.sRGB && QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
            RenderTexture.active = active;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _presenter?.Dispose();
            if (Panel.targetTexture == _target)
            {
                Panel.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                _target = null;
            }
        }
    }
}
