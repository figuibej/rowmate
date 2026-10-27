using System;
using System.IO;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace RowMate.Snapshots
{
    // Videos del arnés (02-tech-design §4.1 punto 5): UnityEditor.Media.MediaEncoder (H.264, 30 fps), cuadro a cuadro con el
    // reloj de prueba (S-0: MP4 escrito en batchmode). Si el cuadro es más alto que 2160 px se reduce a la mitad (el codificador
    // H.264 de Windows no pasa de 4096 × 2304). Si MediaEncoder falla, guarda los cuadros como PNG numerados en una carpeta
    // con el nombre del video y lo avisa (R2-12).
    public sealed class VideoRecorder : IDisposable
    {
        private const int MaxHeight = 2160;

        private readonly string _path;
        private readonly int _width;
        private readonly int _height;
        private readonly RenderTexture? _scaled;
        private MediaEncoder? _encoder;
        private string? _fallbackFolder;
        private int _frames;

        public VideoRecorder(string path, int sourceWidth, int sourceHeight, int fps, GraphicsFormat sourceFormat)
        {
            _path = path;
            var divisor = sourceHeight > MaxHeight ? 2 : 1;
            _width = (sourceWidth / divisor) & ~1;
            _height = (sourceHeight / divisor) & ~1;
            if (divisor > 1 || _width != sourceWidth || _height != sourceHeight)
            {
                _scaled = new RenderTexture(_width, _height, 0, sourceFormat) { hideFlags = HideFlags.HideAndDontSave };
                _scaled.Create();
            }

            try
            {
                var attributes = new VideoTrackAttributes
                {
                    frameRate = new MediaRational(fps),
                    width = (uint)_width,
                    height = (uint)_height,
                    includeAlpha = false,
                    bitRateMode = UnityEditor.VideoBitrateMode.High
                };
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                _encoder = new MediaEncoder(path, attributes);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[SNAPSHOT] MediaEncoder no disponible (" + error.Message + "): se guardan los cuadros como PNG.");
                StartFallback();
            }
        }

        public int Frames => _frames;

        public string? FallbackFolder => _fallbackFolder;

        public int Width => _width;

        public int Height => _height;

        public void AddFrame(RenderTexture source)
        {
            var target = source;
            if (_scaled != null)
            {
                Graphics.Blit(source, _scaled);
                target = _scaled;
            }

            var texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false, true);
            var active = RenderTexture.active;
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
            texture.Apply(false);
            RenderTexture.active = active;
            try
            {
                if (_encoder != null)
                {
                    _encoder.AddFrame(texture);
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(_fallbackFolder!, "frame-" + _frames.ToString("D4") + ".png"), texture.EncodeToPNG());
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("[SNAPSHOT] MediaEncoder.AddFrame falló (" + error.Message + "): se siguen guardando PNG.");
                _encoder?.Dispose();
                _encoder = null;
                StartFallback();
                File.WriteAllBytes(Path.Combine(_fallbackFolder!, "frame-" + _frames.ToString("D4") + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            _frames++;
        }

        public void Dispose()
        {
            _encoder?.Dispose();
            _encoder = null;
            if (_scaled != null)
            {
                _scaled.Release();
                UnityEngine.Object.DestroyImmediate(_scaled);
            }
        }

        private void StartFallback()
        {
            _fallbackFolder = Path.Combine(Path.GetDirectoryName(_path)!, Path.GetFileNameWithoutExtension(_path));
            Directory.CreateDirectory(_fallbackFolder);
        }
    }
}
