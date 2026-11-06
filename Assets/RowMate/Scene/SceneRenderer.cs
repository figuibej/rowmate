using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RowMate.Scene
{
    // El dibujo a pedido de la escena (06a-escena-lago/02-tech-design §3.2; DT6a-05; ADR 022; sondas S6a-2 y S6a-3): la cámara de la
    // escena NUNCA se dibuja sola (enabled = false); SceneHost.Tick llama Render una vez por cuadro dibujado y URP la dibuja adentro
    // de la llamada (RenderPipeline.SubmitRenderRequest con un SingleCameraRequest reutilizado: sin asignar). Si el pipeline no
    // admite el pedido (otro pipeline, una versión de URP sin él), el respaldo de S6a-2: Camera.Render() con la RT de destino.
    // BeginRender/EndRender envuelven el dibujo para la medida de memoria (cuenta "URP" de FrameAllocations.WatchScene, §4.4).
    // El pedido de render NO actualiza los Volume (URP solo lo hace en RenderCameraStack: ProcessRenderRequests va directo a
    // RenderSingleCameraInternal) y el post lee VolumeManager.instance.stack: antes de cada pedido se actualiza la pila principal con
    // la máscara de la cámara de la escena, como hace URP para cada cámara del juego. Sin eso el post quedaba con la última pila que
    // armó otra cámara (en el arnés, la de un Tick sintético con el velo: la hora y el velo de pausa no llegaban a la imagen; WP-D2).
    public sealed class SceneRenderer
    {
        private readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest();
        private readonly UniversalAdditionalCameraData? _data;
        private bool? _submitSupported;

        public SceneRenderer(Camera camera)
        {
            Camera = camera != null ? camera : throw new ArgumentNullException(nameof(camera));
            Camera.enabled = false;
            Camera.TryGetComponent(out _data);
        }

        public Camera Camera { get; }

        // Cuántos dibujos pidió (DEMO-01, SLC: 0 después de Ready fuera del inmersivo).
        public long Renders { get; private set; }

        // El respaldo de S6a-2 forzado (pruebas y sondas): Camera.Render() en lugar del pedido.
        public bool ForceCameraRender { get; set; }

        // Qué camino usó el último dibujo (para el informe de las sondas).
        public bool LastUsedSubmit { get; private set; }

        public Action? BeginRender { get; set; }

        public Action? EndRender { get; set; }

        public void Render(RenderTexture destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            BeginRender?.Invoke();
            try
            {
                Renders++;
                _request.destination = destination;
                if (!ForceCameraRender && SubmitSupported())
                {
                    LastUsedSubmit = true;
                    var volumes = VolumeManager.instance;
                    if (volumes.isInitialized)
                    {
                        volumes.ResetMainStack();
                        volumes.Update(null!, _data != null ? _data.volumeLayerMask : (LayerMask)Camera.cullingMask);
                    }

                    RenderPipeline.SubmitRenderRequest(Camera, _request);
                }
                else
                {
                    LastUsedSubmit = false;
                    var previous = Camera.targetTexture;
                    Camera.targetTexture = destination;
                    Camera.Render();
                    Camera.targetTexture = previous;
                }

                _request.destination = null!;
            }
            finally
            {
                EndRender?.Invoke();
            }
        }

        private bool SubmitSupported()
        {
            if (!_submitSupported.HasValue)
            {
                _request.destination = null!;
                _submitSupported = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset
                                   && RenderPipeline.SupportsRenderRequest(Camera, _request);
            }

            return _submitSupported.Value;
        }
    }
}
