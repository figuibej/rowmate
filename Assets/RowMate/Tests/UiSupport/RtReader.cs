using UnityEngine;

namespace RowMate.Tests.UiSupport
{
    // Lectura de la RT de la UI (02-tech-design §4.1 punto 5): RenderTexture.active = rt; ReadPixels en una Texture2D RGBA32
    // lineal (sin conversión: los bytes leídos son los de la pantalla con la misma tubería). Rectángulos en px con origen
    // ARRIBA a la izquierda (como worldBound × densidad); la Texture2D resultante tiene el origen abajo, como toda textura.
    public static class RtReader
    {
        public static Texture2D Read(RenderTexture rt, RectInt? cropTopLeft = null)
        {
            var crop = cropTopLeft ?? new RectInt(0, 0, rt.width, rt.height);
            crop = Clamp(crop, rt.width, rt.height);
            var texture = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false, true);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(crop.x, rt.height - crop.y - crop.height, crop.width, crop.height), 0, 0, false);
            texture.Apply(false);
            RenderTexture.active = active;
            return texture;
        }

        // Rectángulo en px (origen arriba) de un rectángulo en dp, con margen en dp.
        public static RectInt ToPixels(Rect dp, double density, float marginDp = 0)
        {
            var x = Mathf.FloorToInt((float)((dp.x - marginDp) * density));
            var y = Mathf.FloorToInt((float)((dp.y - marginDp) * density));
            var xMax = Mathf.CeilToInt((float)((dp.xMax + marginDp) * density));
            var yMax = Mathf.CeilToInt((float)((dp.yMax + marginDp) * density));
            return new RectInt(x, y, xMax - x, yMax - y);
        }

        public static RectInt Clamp(RectInt rect, int width, int height)
        {
            var x = Mathf.Clamp(rect.x, 0, width - 1);
            var y = Mathf.Clamp(rect.y, 0, height - 1);
            var xMax = Mathf.Clamp(rect.xMax, x + 1, width);
            var yMax = Mathf.Clamp(rect.yMax, y + 1, height);
            return new RectInt(x, y, xMax - x, yMax - y);
        }

        // Píxel (origen arriba) de una textura leída con Read sin recorte.
        public static Color32 PixelTopLeft(Texture2D frame, int x, int y)
        {
            x = Mathf.Clamp(x, 0, frame.width - 1);
            y = Mathf.Clamp(y, 0, frame.height - 1);
            return frame.GetPixel(x, frame.height - 1 - y);
        }
    }
}
