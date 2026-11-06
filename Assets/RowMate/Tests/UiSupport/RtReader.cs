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
            if (UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsHalfFormat(rt.graphicsFormat)
                || UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsIEEE754Format(rt.graphicsFormat)
                || rt.graphicsFormat == UnityEngine.Experimental.Rendering.GraphicsFormat.B10G11R11_UFloatPack32)
            {
                return ReadFloat(rt, crop);
            }

            var texture = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false, true);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(crop.x, rt.height - crop.y - crop.height, crop.width, crop.height), 0, 0, false);
            texture.Apply(false);
            RenderTexture.active = active;
            return texture;
        }

        // It. 6a (la RT de la escena de media precisión en los niveles con HDR): los valores lineales, codificados en sRGB y en bytes,
        // como los de una RT R8G8B8A8_SRGB leída con Read (lo mismo que se ve en el panel).
        private static Texture2D ReadFloat(RenderTexture rt, RectInt crop)
        {
            var raw = new Texture2D(crop.width, crop.height, TextureFormat.RGBAFloat, false, true);
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            raw.ReadPixels(new Rect(crop.x, rt.height - crop.y - crop.height, crop.width, crop.height), 0, 0, false);
            raw.Apply(false);
            RenderTexture.active = active;
            var pixels = raw.GetPixels();
            var bytes = new Color32[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                bytes[i] = new Color(Mathf.LinearToGammaSpace(Mathf.Clamp01(c.r)), Mathf.LinearToGammaSpace(Mathf.Clamp01(c.g)),
                    Mathf.LinearToGammaSpace(Mathf.Clamp01(c.b)), Mathf.Clamp01(c.a));
            }

            Object.DestroyImmediate(raw);
            var texture = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(bytes);
            texture.Apply(false);
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
