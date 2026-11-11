using System;
using UnityEngine;

namespace RowMate.Presentation.Controls
{
    // La sombra del tooltip del gráfico (It. 15, #37, AC-D15-06; arte A15-03 con los valores de la spec): UI Toolkit no tiene box-shadow,
    // así que es un hermano 9-slice DETRÁS del tooltip, con esta textura generada al arrancar (como GlowTexture) y cacheada: un
    // rectángulo redondeado de radio 8 dp con el borde gaussiano de 8 dp de desenfoque (σ = 4 dp), blanco con alfa (lleno por dentro).
    // El color (negro al 35 %) lo pone el elemento con -unity-background-image-tint-color; el corrimiento de +2 dp en y, su posición.
    // Se dibuja a 2× (Scale) con -unity-slice-scale 1/2. Blanco = Color.white (no es un color literal, ColorSourceTests).
    public static class ShadowTexture
    {
        public const int Scale = 2;
        public const float BlurDp = 8;
        public const float RadiusDp = 8;
        public const float OffsetYDp = 2;
        public const float Alpha = 0.35f;

        private static Texture2D? _cached;

        // Lado de cada slice y de la textura, en píxeles de la textura.
        public static int SlicePx => (int)((BlurDp + RadiusDp) * Scale);

        public static int SizePx => 2 * SlicePx + 2;

        // La textura compartida (una para toda la app).
        public static Texture2D Shared
        {
            get
            {
                if (_cached == null)
                {
                    _cached = Create();
                }

                return _cached;
            }
        }

        public static Texture2D Create()
        {
            var size = SizePx;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "RowMate.TooltipShadow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var blur = BlurDp * Scale;
            var radius = RadiusDp * Scale;
            var sigma = blur / 2f;
            var pixels = new Color32[size * size];
            // El rectángulo de la caja: a "blur" de cada borde de la textura.
            var minX = blur;
            var minY = blur;
            var maxX = size - blur;
            var maxY = size - blur;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var d = SignedDistance(x + 0.5f, y + 0.5f, minX, minY, maxX, maxY, radius);
                    // Cobertura de un borde desenfocado con una gaussiana: 0,5 · erfc(d / (σ√2)).
                    var alpha = 0.5f * (1f - Erf(d / (sigma * 1.41421356f)));
                    Color color = Color.white;
                    color.a = Mathf.Clamp01(alpha);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // Distancia con signo al rectángulo redondeado (negativa adentro).
        private static float SignedDistance(float x, float y, float minX, float minY, float maxX, float maxY, float radius)
        {
            var cx = Mathf.Clamp(x, minX + radius, maxX - radius);
            var cy = Mathf.Clamp(y, minY + radius, maxY - radius);
            var dx = x - cx;
            var dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - radius;
        }

        // erf de Abramowitz y Stegun 7.1.26 (error < 1,5e-7).
        private static float Erf(float x)
        {
            var sign = x < 0 ? -1f : 1f;
            var ax = Math.Abs(x);
            var t = 1f / (1f + 0.3275911f * ax);
            var y = 1f - ((((1.061405429f * t - 1.453152027f) * t + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-ax * ax);
            return sign * y;
        }
    }
}
