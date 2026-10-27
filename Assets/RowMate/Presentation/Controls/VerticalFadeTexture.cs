using UnityEngine;

namespace RowMate.Presentation.Controls
{
    // Textura de degradé vertical (DN-06; 04-design-notes §4.4): 1 × 64 RGBA32, blanca con alfa smoothstep, 1 arriba → 0
    // abajo, clamp y bilineal. Se crea por código al armar el shell (sin asset, 256 B) y se destruye al disponerlo. Es el
    // fondo de cada .rm-card__glow y de .rm-tab-bar__sheen; el tinte, el tamaño y el alto van en USS (WP-D).
    // El blanco sale de Color.white (no es un color literal, ColorSourceTests).
    public static class VerticalFadeTexture
    {
        public const int Height = 64;

        public static Texture2D Create()
        {
            var texture = new Texture2D(1, Height, TextureFormat.RGBA32, false, false)
            {
                name = "RowMate.VerticalFade",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[Height];
            for (var row = 0; row < Height; row++)
            {
                // La fila 0 de una textura es la de abajo: arriba (fila 63) alfa 1, abajo (fila 0) alfa 0.
                var fromTop = (Height - 1 - row) / (float)(Height - 1);
                var t = 1f - fromTop;
                var alpha = t * t * (3f - 2f * t);
                Color color = Color.white;
                color.a = alpha;
                pixels[row] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
