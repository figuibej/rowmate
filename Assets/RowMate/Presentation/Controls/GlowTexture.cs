using UnityEngine;

namespace RowMate.Presentation.Controls
{
    // El halo de una tarjeta del HUD con objetivo (AC-WKT-10-05, Q-60; ART_BIBLE §3.2, respaldo sin desenfoque; 02-tech-design
    // §6.5): un anillo 9-slice generado al arrancar, blanco con alfa, TRANSPARENTE POR DENTRO (con un BoxShadow debajo de un
    // vidrio translúcido el interior se teñiría y el fuera de objetivo bajaría a 3,3:1). El halo va solo por fuera de la
    // tarjeta: del borde (radio 14) hacia afuera 12 dp, con el alfa que cae de 1 a 0. El tinte (color base @20 %) lo pone la
    // tarjeta (-unity-background-image-tint-color). Se dibuja a 3× (Scale) con -unity-slice-scale 1/3: queda nítido en W1280
    // (escala del HUD 1,6). Blanco = Color.white (no es un color literal, ColorSourceTests).
    public static class GlowTexture
    {
        public const int Scale = 3;
        public const float GlowDp = 12;
        public const float RadiusDp = 14;

        // Lado de la textura y de cada slice, en píxeles de la textura.
        public static int SlicePx => (int)((GlowDp + RadiusDp) * Scale);

        public static int SizePx => 2 * SlicePx + 2;

        public static Texture2D Create()
        {
            var size = SizePx;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "RowMate.HudGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var glow = GlowDp * Scale;
            var radius = RadiusDp * Scale;
            var pixels = new Color32[size * size];
            // La tarjeta: un rectángulo redondeado inset "glow" desde cada borde de la textura.
            var minX = glow;
            var minY = glow;
            var maxX = size - glow;
            var maxY = size - glow;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = x + 0.5f;
                    var py = y + 0.5f;
                    var distance = OutsideDistance(px, py, minX, minY, maxX, maxY, radius);
                    var alpha = 0f;
                    if (distance > 0 && distance < glow)
                    {
                        var t = 1f - distance / glow;
                        alpha = t * t;
                    }

                    Color color = Color.white;
                    color.a = alpha;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // Distancia de un punto al rectángulo redondeado (0 adentro).
        private static float OutsideDistance(float x, float y, float minX, float minY, float maxX, float maxY, float radius)
        {
            var cx = Mathf.Clamp(x, minX + radius, maxX - radius);
            var cy = Mathf.Clamp(y, minY + radius, maxY - radius);
            var dx = x - cx;
            var dy = y - cy;
            var d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
            return d > 0 ? d : 0;
        }
    }
}
