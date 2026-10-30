using UnityEngine;

namespace RowMate.Presentation.Workout
{
    // La textura del halo de la píldora en curso (04b-hud-exr/02-tech-design §3.4; 04-design-notes §6, DN4b-17): como
    // GlowTexture (las tarjetas) pero con el radio de la píldora (4) y 10 de halo, a 3× (86 × 86 px, ≈ 29,6 KB), generada al
    // arrancar. TRANSPARENTE POR DENTRO (la pista no se tiñe) y, por fuera, el perfil (1 − d/E)²: en el borde 1, en E 0. El
    // 0,65 del borde (PillGlowEdgeFactor, lo que deja el BoxShadow del legacy) y el α del pulso van en el tinte de los vértices.
    // PillRowView la dibuja en 9 partes con cada lado estirado a su largo (10·s a los costados, 8·s arriba y 4·s abajo): las
    // esquinas se parten en el borde de la píldora, así el perfil queda (1 − d/E)² con E distinto por lado y empieza justo en el
    // borde, sin hueco. Blanco = Color.white (no es un color literal, ColorSourceTests).
    public static class PillGlowTexture
    {
        public const int Scale = 3;
        public const float GlowDp = 10;
        public const float RadiusDp = 4;

        // Lado de cada esquina (E + r) y de la textura, en píxeles.
        public static int SlicePx => (int)((GlowDp + RadiusDp) * Scale);

        public static int SizePx => 2 * SlicePx + 2;

        // Dónde está el borde de la píldora dentro de la esquina, en fracción de la textura (E·3 / tamaño).
        public static float EdgeU => GlowDp * Scale / SizePx;

        public static float SliceU => (float)SlicePx / SizePx;

        public static Texture2D Create()
        {
            var size = SizePx;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "RowMate.PillGlow",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var glow = GlowDp * Scale;
            var radius = RadiusDp * Scale;
            var pixels = new Color32[size * size];
            var minX = glow;
            var minY = glow;
            var maxX = size - glow;
            var maxY = size - glow;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = OutsideDistance(x + 0.5f, y + 0.5f, minX, minY, maxX, maxY, radius);
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

        public static void Destroy(Texture2D? texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }
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
