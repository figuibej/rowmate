using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Presentation.Surface
{
    // Presentador alternativo de GammaRt (02-tech-design §3.1, S-B1, R2-16): en RenderPipelineManager.endContextRendering,
    // CommandBuffer.Blit de la RT UNorm al CameraTarget con un material cuyo shader decodifica sRGB → lineal antes de escribir
    // (el backbuffer sRGB vuelve a codificar). Funciona igual en todos los backends; suma un .shader y un material al build
    // (Settings/UI/GammaPresent.mat, solo si S-B1 elige este presentador). Sin material no presenta nada.
    public sealed class BlitPresenter : IUiPresenter
    {
        private readonly Material? _material;
        private readonly CommandBuffer _commands = new CommandBuffer { name = "RowMate.UiPresent" };
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private RenderTexture? _source;
        private bool _disposed;

        public BlitPresenter(Material? material)
        {
            _material = material;
            RenderPipelineManager.endContextRendering += OnEndContextRendering;
        }

        public bool IsAvailable => _material != null;

        public double LastCostMs { get; private set; }

        public void SetSource(RenderTexture gammaUi)
        {
            _source = gammaUi;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            RenderPipelineManager.endContextRendering -= OnEndContextRendering;
            _commands.Release();
        }

        private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (_source == null || _material == null)
            {
                return;
            }

            _stopwatch.Restart();
            _commands.Clear();
            _commands.Blit(_source, BuiltinRenderTextureType.CameraTarget, _material);
            context.ExecuteCommandBuffer(_commands);
            context.Submit();
            _stopwatch.Stop();
            LastCostMs = _stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}
