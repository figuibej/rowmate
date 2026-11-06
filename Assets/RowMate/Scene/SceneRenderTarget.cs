using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace RowMate.Scene
{
    // La RT de la escena (06a-escena-lago/02-tech-design §3.2; DT6a-05, DT6a-16; ADR 022): del tamaño de la ventana en píxeles por la
    // resolución del nivel (Alto 1; Medio 0,85; Bajo 0,75; y la que baje la adaptación). La resolución es el tamaño de
    // ESTA RT: los assets de URP quedan con renderScale 1 y nunca se tocan (QA-G6aT-05). Filtro de punto si mide lo mismo que la
    // ventana; bilineal si está escalada (no se ve a bloques al estirarla en el panel, QA-G6aT-09). Con su profundidad (URP dibuja
    // directo en ella cuando no necesita una intermedia). Se rehace solo al cambiar la ventana o la resolución (la escena sigue).
    // El formato (cambio por implementación de WP-B, B6a-14; ronda 1 de la verificación, QA-V6a-REN-07: FormatFor): con HDR, de media
    // precisión en Alto y B10G11R11 en Medio; R8G8B8A8_SRGB en Bajo. URP arma la intermedia de una cámara que dibuja en una RT con el formato de ESA RT: con una de 8 bits no hay HDR (el
    // disco del sol en 3 + 5e, DN6a-21, quedaba recortado en 1 antes del tonemapping y salía gris). El panel la lee igual (los dos
    // formatos dan el valor lineal al muestrear).
    public sealed class SceneRenderTarget : IDisposable
    {
        private RenderTexture? _texture;

        public RenderTexture? Texture => _texture;

        public int WindowWidthPx { get; private set; }

        public int WindowHeightPx { get; private set; }

        public double Resolution { get; private set; } = 1;

        public int Width => _texture != null ? _texture.width : 0;

        public int Height => _texture != null ? _texture.height : 0;

        // Cuántas veces se rehízo (las pruebas: no se rehace en cada cuadro).
        public int Generation { get; private set; }

        // El tamaño de la RT para una ventana y una resolución (al menos 1 px; redondeo al entero más cercano).
        public static void SizeFor(int windowWidthPx, int windowHeightPx, double resolution, out int width, out int height)
        {
            var r = resolution > 0 && resolution <= 1 ? resolution : 1;
            width = Math.Max(1, (int)Math.Round(windowWidthPx * r));
            height = Math.Max(1, (int)Math.Round(windowHeightPx * r));
        }

        // El formato de la RT de un nivel (ronda 1 de la verificación, QA-V6a-REN-07): Bajo, R8G8B8A8_SRGB; Medio (el teléfono),
        // B10G11R11_UFloatPack32 (4 B/px: la memoria y el ancho de banda de la tabla de §4.1, y URP arma con él su intermedia y el MSAA;
        // la escena es opaca y el panel no lee el alfa), con R16G16B16A16_SFloat si la plataforma no lo dibuja; Alto (el escritorio),
        // R16G16B16A16_SFloat (8 B/px: la precisión de las sondas de color ± 1 y de las tomas; en el escritorio la memoria sobra).
        public static GraphicsFormat FormatFor(bool hdr, RowMate.Core.Scene.QualityTier tier)
        {
            if (!hdr)
            {
                return GraphicsFormat.R8G8B8A8_SRGB;
            }

            if (tier != RowMate.Core.Scene.QualityTier.High && CompactHdrSupported())
            {
                return GraphicsFormat.B10G11R11_UFloatPack32;
            }

            return GraphicsFormat.R16G16B16A16_SFloat;
        }

        private static bool? _compactHdr;

        private static bool CompactHdrSupported()
        {
            if (!_compactHdr.HasValue)
            {
                // La misma condición que URP usa para su intermedia HDR de 32 bits (UniversalRenderPipeline.MakeRenderTextureGraphicsFormat).
                // (Sample da false en Direct3D 11 aunque el formato se muestree: URP tampoco lo mira.)
                _compactHdr = SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormatUsage.Blend);
            }

            return _compactHdr.Value;
        }

        // Rehace la RT si cambió la ventana, la resolución o el formato (hdr: el del nivel). true = es otra RT (quien la muestra la vuelve a tomar).
        public bool Ensure(int windowWidthPx, int windowHeightPx, double resolution, bool hdr = false,
                           RowMate.Core.Scene.QualityTier tier = RowMate.Core.Scene.QualityTier.High)
        {
            if (windowWidthPx <= 0 || windowHeightPx <= 0)
            {
                return false;
            }

            SizeFor(windowWidthPx, windowHeightPx, resolution, out var width, out var height);
            var format = FormatFor(hdr, tier);
            if (_texture != null && _texture.IsCreated() && _texture.width == width && _texture.height == height && _texture.graphicsFormat == format)
            {
                WindowWidthPx = windowWidthPx;
                WindowHeightPx = windowHeightPx;
                Resolution = resolution;
                return false;
            }

            Release();
            var descriptor = new RenderTextureDescriptor(width, height, format, GraphicsFormat.D24_UNorm_S8_UInt)
            {
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false,
            };
            _texture = new RenderTexture(descriptor)
            {
                name = "RowMate.SceneRT",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = width == windowWidthPx && height == windowHeightPx ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _texture.Create();
            WindowWidthPx = windowWidthPx;
            WindowHeightPx = windowHeightPx;
            Resolution = resolution;
            Generation++;
            return true;
        }

        public void Release()
        {
            if (_texture == null)
            {
                return;
            }

            _texture.Release();
            if (Application.isPlaying)
            {
                Object.Destroy(_texture);
            }
            else
            {
                Object.DestroyImmediate(_texture);
            }

            _texture = null;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
