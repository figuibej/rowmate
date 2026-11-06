using System;
using RowMate.Core.Scene.Art;
using UnityEngine;

namespace RowMate.Scene
{
    // Las texturas procedurales de la escena, horneadas por filas al precargar (06a-escena-lago/02-tech-design §3.4 paso 3; DN6a-18;
    // 04-design-notes, "Cambios por implementación de WP-D1" §4): las recetas son de Core (SkyTextures) y acá solo se escriben los
    // texeles. Cada unidad del armado escribe unas filas (costo acotado: SceneWarmupPolicy). Sin mipmaps (cielo y nubes se ven a una
    // distancia fija) salvo el ruido del terreno, que se repite en el piso y lleva mipmaps.
    public sealed class ProceduralTexture
    {
        public enum Kind
        {
            CloudAtlas,
            Moon,
            Noise,
            CloudLayer,
        }

        private readonly Kind _kind;
        private byte[] _pixels;
        private readonly int _channels;
        private int _row;

        public ProceduralTexture(Kind kind, string name)
        {
            _kind = kind;
            int width, height;
            TextureFormat format;
            var mips = false;
            switch (kind)
            {
                case Kind.CloudAtlas:
                    width = SkyTextures.CloudAtlasWidth;
                    height = SkyTextures.CloudAtlasHeight;
                    format = TextureFormat.RG16;
                    _channels = 2;
                    break;
                case Kind.Moon:
                    width = SkyTextures.MoonSize;
                    height = SkyTextures.MoonSize;
                    format = TextureFormat.R8;
                    _channels = 1;
                    break;
                case Kind.Noise:
                    width = SkyTextures.NoiseSize;
                    height = SkyTextures.NoiseSize;
                    format = TextureFormat.RGBA32;
                    _channels = 4;
                    mips = true;
                    break;
                default:
                    width = SkyTextures.CloudLayerWidth;
                    height = SkyTextures.CloudLayerHeight;
                    format = TextureFormat.R8;
                    _channels = 1;
                    break;
            }

            // Datos, no colores: lineales (linear: true), así el shader lee el valor tal cual.
            Texture = new Texture2D(width, height, format, mips, true)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = kind == Kind.Noise ? TextureWrapMode.Repeat : (kind == Kind.CloudLayer ? TextureWrapMode.Repeat : TextureWrapMode.Clamp),
                filterMode = FilterMode.Bilinear,
                anisoLevel = kind == Kind.Noise ? 4 : 1,
            };
            if (kind == Kind.CloudLayer)
            {
                Texture.wrapModeV = TextureWrapMode.Clamp;
            }

            _pixels = new byte[width * height * _channels];
        }

        public Texture2D Texture { get; }

        public bool Done => _row >= Texture.height;

        // Escribe hasta `rows` filas más (desde la de arriba de la imagen). true al terminar (sube la textura).
        public bool BuildRows(int rows)
        {
            var width = Texture.width;
            var height = Texture.height;
            var end = Math.Min(height, _row + rows);
            for (var y = _row; y < end; y++)
            {
                // y: la fila de la Texture2D (0 abajo); top = la fila desde arriba.
                var top = height - 1 - y;
                for (var x = 0; x < width; x++)
                {
                    var i = (y * width + x) * _channels;
                    switch (_kind)
                    {
                        case Kind.CloudAtlas:
                        {
                            // La variante k en la celda (k mod 2; k / 2) contada desde arriba; v = 0 arriba de la celda.
                            var cellX = x / SkyTextures.CloudCellWidth;
                            var cellY = top / SkyTextures.CloudCellHeight;
                            var variant = cellX + 2 * cellY;
                            var u = (x - cellX * SkyTextures.CloudCellWidth + 0.5) / SkyTextures.CloudCellWidth;
                            var v = (top - cellY * SkyTextures.CloudCellHeight + 0.5) / SkyTextures.CloudCellHeight;
                            SkyTextures.CloudTexel(variant, u, v, out var body, out var shade);
                            _pixels[i] = ToByte(body);
                            _pixels[i + 1] = ToByte(shade);
                            break;
                        }

                        case Kind.Moon:
                        {
                            // (u; v) del disco de −1 a 1, v hacia arriba (la fila 0 de la textura es la de abajo).
                            var u = (x + 0.5) / width * 2 - 1;
                            var v = (y + 0.5) / height * 2 - 1;
                            _pixels[i] = ToByte(SkyTextures.MoonAlbedo(u, v));
                            break;
                        }

                        case Kind.Noise:
                            for (var c = 0; c < 4; c++)
                            {
                                _pixels[i + c] = ToByte(SkyTextures.Noise(c, x, y));
                            }

                            break;
                        default:
                            _pixels[i] = ToByte(SkyTextures.CloudLayer(x, y));
                            break;
                    }
                }
            }

            _row = end;
            if (!Done)
            {
                return false;
            }

            if (_pixels.Length == 0)
            {
                // Ya subida.
                return true;
            }

            Texture.SetPixelData(_pixels, 0);
            // Ronda 1 de la verificación (QA-V6a-REN-07): sin la copia de la CPU (la textura deja de ser legible) y sin el búfer de
            // armado: el contenido de la escena queda en lo de la tabla de §4.1 (≈ 0,6 MB; con la copia legible era el doble).
            Texture.Apply(Texture.mipmapCount > 1, true);
            _pixels = System.Array.Empty<byte>();
            return true;
        }

        private static byte ToByte(double value)
        {
            var v = value < 0 ? 0 : value > 1 ? 1 : value;
            return (byte)Math.Round(v * 255);
        }
    }
}
