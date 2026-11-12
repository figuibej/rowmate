using System;
using RowMate.Core.Design;
using UnityEngine;
using UnityEngine.Rendering;

namespace RowMate.Scene
{
    // El vidrio desenfocado del HUD (F-WKT-17; 06b-agua-avanzada/02-tech-design §4.4; DT6b-08 a -11; AC-6B-23 a -27): NO es un renderer
    // feature de URP. La escena ya se dibuja a pedido en su RT (ADR 022): después de dibujarla, un CommandBuffer armado una vez por tamaño
    // baja a ½ por eje (y a ¼ en teléfono, densidad ≥ 2: el radio en dp es parecido, DT6b-11), corre 3 pasos de Kawase (1,5, 2,5 y 3,5
    // texeles) y compone _RmHudGlass, opaco, con el tinte glass.fallback y α(L) por píxel (GlassTint, en el espacio del panel). Las RT son
    // del formato de la escena (el panel la muestrea igual que scene-surface) y se reservan al cambiar de tamaño, nunca por cuadro. Se
    // ejecuta con Graphics.ExecuteCommandBuffer, con BeginSample/EndSample("RowMate.HudBlur") ADENTRO del búfer (G6b-11: así lo ve el
    // GpuRecorder). 3 SetPass (bajada, Kawase y composición), aparte del tope de la escena.
    public sealed class HudBlurPass : IDisposable
    {
        public const string SampleName = "RowMate.HudBlur";
        public static readonly float[] KawaseOffsets = { 1.5f, 2.5f, 3.5f };

        private static readonly int BlurTexel = Shader.PropertyToID("_RmBlurTexel");
        private static readonly int BlurOffset = SceneShaderIds.BlurOffset;
        private readonly Material _material;
        private readonly CommandBuffer _buffer = new CommandBuffer { name = SampleName };
        private RenderTexture? _a;
        private RenderTexture? _b;
        private RenderTexture? _glass;
        private RenderTexture? _source;
        private bool _quarter;

        public HudBlurPass(Material material)
        {
            _material = material ?? throw new ArgumentNullException(nameof(material));
            var tint = Tokens.GlassFallback.Rgb;
            _material.SetVector(SceneShaderIds.GlassAlpha,
                new Vector4((float)GlassTint.AlphaMin, (float)GlassTint.AlphaMax, (float)GlassTint.LumaFrom, (float)GlassTint.LumaTo));
            // SetVector, no SetColor: el tinte va en sRGB, sin convertir (la mezcla es en el espacio del panel, IA6b-01).
            _material.SetVector(SceneShaderIds.GlassTint, new Vector4(tint.R / 255f, tint.G / 255f, tint.B / 255f, 1));
        }

        // _RmHudGlass: el compuesto (null hasta la primera corrida).
        public RenderTexture? Output => _glass;

        public long Runs { get; private set; }

        // Las veces que se rehicieron las RT (otro tamaño): el panel vuelve a tomar la textura.
        public int Generation { get; private set; }

        // Arma las RT y el búfer para esta RT de la escena (true = son otras).
        public bool Ensure(RenderTexture scene, double density)
        {
            var quarter = density >= 2;
            if (ReferenceEquals(scene, _source) && quarter == _quarter && _glass != null && _glass.IsCreated())
            {
                return false;
            }

            Release();
            _source = scene;
            _quarter = quarter;
            var w = Math.Max(8, scene.width / (quarter ? 4 : 2));
            var h = Math.Max(8, scene.height / (quarter ? 4 : 2));
            _a = Create("RowMate.HudBlurA", w, h, scene);
            _b = Create("RowMate.HudBlurB", w, h, scene);
            _glass = Create("RowMate.HudGlass", w, h, scene);
            Build(scene, w, h);
            Generation++;
            return true;
        }

        private static RenderTexture Create(string name, int width, int height, RenderTexture like)
        {
            var rt = new RenderTexture(new RenderTextureDescriptor(width, height, like.graphicsFormat, 0) { msaaSamples = 1, useMipMap = false })
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            rt.Create();
            return rt;
        }

        private void Build(RenderTexture scene, int w, int h)
        {
            _buffer.Clear();
            _buffer.BeginSample(SampleName);
            _buffer.SetGlobalVector(BlurTexel, new Vector4(1f / scene.width, 1f / scene.height, 0, 0));
            RenderTexture from = _a!;
            RenderTexture to = _b!;
            if (_quarter)
            {
                // Teléfono: ½ y después otra ½ (las dos con la misma pasada de bajada).
                var half = new RenderTargetIdentifier(_b!);
                _buffer.Blit(scene, half, _material, 0);
                _buffer.SetGlobalVector(BlurTexel, new Vector4(2f / scene.width, 2f / scene.height, 0, 0));
                _buffer.Blit(half, _a!, _material, 0);
            }
            else
            {
                _buffer.Blit(scene, _a!, _material, 0);
            }

            _buffer.SetGlobalVector(BlurTexel, new Vector4(1f / w, 1f / h, 0, 0));
            foreach (var offset in KawaseOffsets)
            {
                _buffer.SetGlobalFloat(BlurOffset, offset);
                _buffer.Blit(from, to, _material, 1);
                var swap = from;
                from = to;
                to = swap;
            }

            _buffer.Blit(from, _glass!, _material, 2);
            _buffer.EndSample(SampleName);
        }

        public void Execute()
        {
            if (_glass == null)
            {
                return;
            }

            Graphics.ExecuteCommandBuffer(_buffer);
            Runs++;
        }

        public void Release()
        {
            foreach (var rt in new[] { _a, _b, _glass })
            {
                if (rt != null)
                {
                    rt.Release();
                    MeshUpload.Destroy(rt);
                }
            }

            _a = null;
            _b = null;
            _glass = null;
            _source = null;
        }

        public void Dispose()
        {
            Release();
            _buffer.Release();
        }
    }
}
