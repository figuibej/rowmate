using UnityEngine;

namespace RowMate.Presentation.Controls
{
    // El fondo provisorio del inmersivo en UN solo elemento (AC-SCN-01-01; DN4a-03, DN4a-18 con la variante de presupuesto de
    // 04-design-notes §7; 02-tech-design §6.3, §6.13): una textura de 1 × 100 generada por código, cielo arriba y agua abajo,
    // con el corte en el texel del horizonte (40 en vertical, 42 en horizontal: HudGeometry.Horizon). Como VerticalFadeTexture:
    // RGBA32 en sRGB (linear: false), sin mipmaps y en Clamp, y además con filtro Point, así el borde queda recto (con bilineal
    // sería un degradé de un texel, 9,1 dp en TV) y los dos tonos salen con el hex exacto. Los colores los pone quien la arma
    // (de Tokens: backdrop.sky y backdrop.water; nunca literales). 400 B; se rehace al rotar (100 píxeles).
    public static class BackdropTexture
    {
        public const int Height = 100;

        public static Texture2D Create(Color sky, Color water, double horizon)
        {
            var texture = new Texture2D(1, Height, TextureFormat.RGBA32, false, false)
            {
                name = "RowMate.HudBackdrop",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            Fill(texture, sky, water, horizon);
            return texture;
        }

        // El texel del corte, desde ARRIBA: round(horizonte × 100).
        public static int CutTexel(double horizon)
        {
            var cut = (int)System.Math.Round(horizon * Height);
            return cut < 0 ? 0 : cut > Height ? Height : cut;
        }

        public static void Fill(Texture2D texture, Color sky, Color water, double horizon)
        {
            var cut = CutTexel(horizon);
            var pixels = new Color32[Height];
            for (var row = 0; row < Height; row++)
            {
                // La fila 0 de una textura es la de abajo.
                var fromTop = Height - 1 - row;
                pixels[row] = fromTop < cut ? sky : water;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }
    }
}
