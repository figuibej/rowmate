using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RowMate.Scene
{
    // El reflejo planar del agua en Alto (F-SCN-12; 06b-agua-avanzada/02-tech-design §4.2 (1); DA6b-01, PA6b-07; AC-6B-12; #13): una
    // cámara espejo deshabilitada con la matriz de reflexión en y = 0 y la proyección oblicua (recorta lo que está debajo del agua), dibujada
    // a pedido con RenderPipeline.SubmitRenderRequest y GL.invertCulling alrededor, a ½ × ½ de la RT de la escena con mips. Sin post, sin
    // sombras ni MSAA y con el plano lejano en 600 m. Quien llama apaga el agua, los degradés de pantalla y las partículas durante el pedido;
    // los dibujos instanciados son por cámara (la orilla instanciada no se encola para el espejo: el reflejo lleva el cielo, el horizonte,
    // el terreno, el bote y el remero). Se mide con el marcador RowMate.Scene.Reflection. Sin asignar en régimen.
    public sealed class PlanarReflection : IDisposable
    {
        public const float FarClip = 600f;
        public const float ClipOffset = 0.02f;
        public const string MarkerName = "RowMate.Scene.Reflection";

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker(MarkerName);
        private readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest();
        private readonly GameObject _object;
        private RenderTexture? _target;
        private bool? _submitSupported;

        public PlanarReflection(Transform parent, int layer)
        {
            _object = new GameObject("scene-reflection-camera") { hideFlags = HideFlags.DontSave, layer = layer };
            _object.transform.SetParent(parent, false);
            Camera = _object.AddComponent<Camera>();
            Camera.enabled = false;
            var data = _object.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            Camera.allowMSAA = false;
            Camera.allowHDR = false;
        }

        public Camera Camera { get; }

        public RenderTexture? Texture => _target;

        public long Renders { get; private set; }

        // La RT del reflejo del tamaño de la mitad de la de la escena (por eje) y su formato. false = no se pudo crear (ReportFailure).
        public bool Ensure(RenderTexture scene)
        {
            var width = Math.Max(16, scene.width / 2);
            var height = Math.Max(16, scene.height / 2);
            if (_target != null && _target.width == width && _target.height == height && _target.graphicsFormat == scene.graphicsFormat
                && _target.IsCreated())
            {
                return true;
            }

            Release();
            var descriptor = new RenderTextureDescriptor(width, height, scene.graphicsFormat, GraphicsFormat.D24_UNorm_S8_UInt)
            {
                useMipMap = true,
                autoGenerateMips = true,
                msaaSamples = 1,
            };
            _target = new RenderTexture(descriptor) { name = "RowMate.WaterReflection", hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            return _target.Create();
        }

        // ¿La RT se perdió (un cambio de dispositivo)? La falla en medio de la sesión (§5).
        public bool Lost => _target == null || !_target.IsCreated();

        public void Render(Camera main)
        {
            if (_target == null)
            {
                return;
            }

            using (Marker.Auto())
            {
                Camera.CopyFrom(main);
                Camera.enabled = false;
                Camera.targetTexture = null;
                Camera.allowMSAA = false;
                Camera.allowHDR = false;
                Camera.farClipPlane = Math.Min(main.farClipPlane, FarClip);

                // La matriz de reflexión en y = 0 (diag(1; −1; 1)) y la posición espejada (para el culling y los LOD).
                var reflection = Matrix4x4.Scale(new Vector3(1, -1, 1));
                var mainPosition = main.transform.position;
                _object.transform.SetPositionAndRotation(new Vector3(mainPosition.x, -mainPosition.y, mainPosition.z),
                    Quaternion.LookRotation(Vector3.Scale(main.transform.forward, new Vector3(1, -1, 1)), Vector3.Scale(main.transform.up, new Vector3(1, -1, 1))));
                Camera.worldToCameraMatrix = main.worldToCameraMatrix * reflection;

                // La proyección oblicua: el plano cercano es el agua (normal hacia arriba, un poco abajo para no cortar el contacto).
                var view = Camera.worldToCameraMatrix;
                var point = view.MultiplyPoint(new Vector3(0, -ClipOffset, 0));
                var normal = view.MultiplyVector(Vector3.up).normalized;
                var plane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal));
                Camera.projectionMatrix = main.CalculateObliqueMatrix(plane);

                _request.destination = _target;
                var invert = GL.invertCulling;
                GL.invertCulling = true;
                try
                {
                    if (SubmitSupported())
                    {
                        RenderPipeline.SubmitRenderRequest(Camera, _request);
                    }
                    else
                    {
                        Camera.targetTexture = _target;
                        Camera.Render();
                        Camera.targetTexture = null;
                    }

                    Renders++;
                }
                finally
                {
                    GL.invertCulling = invert;
                    _request.destination = null!;
                }
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

        public void Release()
        {
            if (_target != null)
            {
                _target.Release();
                MeshUpload.Destroy(_target);
                _target = null;
            }
        }

        public void Dispose()
        {
            Release();
            MeshUpload.Destroy(_object);
        }
    }
}
