using System;
using RowMate.Core.Design;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace RowMate.Bootstrap
{
    // La UI armada por ShellHost (02-tech-design §1.4): Tick() una vez por cuadro (área segura, teclado y animaciones),
    // HandleBack(BackSource) para "Atrás" del sistema y Escape (menú → diálogo → página → raíz; Escape no cierra páginas,
    // P-08) y Dispose() al cerrar (cierra las capas, desuscribe las vistas al sacarlas del panel, libera la capa del
    // simulador y la textura de degradé).
    public sealed class ShellHandle : IDisposable
    {
        private readonly BackNavigation _back;
        private Texture2D? _fade;
        private bool _disposed;

        internal ShellHandle(AppShellView shell, MotionDriver motion, FramePacingPolicy pacing, BackNavigation back, Texture2D fade)
        {
            Shell = shell;
            Motion = motion;
            Pacing = pacing;
            _back = back;
            _fade = fade;
        }

        public AppShellView Shell { get; }

        public MotionDriver Motion { get; }

        public FramePacingPolicy Pacing { get; }

        public VisualElement Root => Shell;

        public void Tick()
        {
            if (_disposed)
            {
                return;
            }

            Shell.Layout.Poll();
            Shell.Tick();
            Motion.Tick();
        }

        public BackNavigation Back => _back;

        // "Atrás" de Android (AC-APP-02-06, AC-APP-03-02): lo de arriba, o la raíz → IBackButtonPolicy.OnBackAtRoot(), una vez.
        public void HandleBack()
        {
            HandleBack(BackSource.SystemBack);
        }

        public void HandleBack(BackSource source)
        {
            if (!_disposed)
            {
                _back.HandleBack(source);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Shell.Dispose();
            Shell.RemoveFromHierarchy();
            if (Shell.TopLayer is IDisposable layer)
            {
                layer.Dispose();
            }

            if (_fade != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_fade);
                }
                else
                {
                    Object.DestroyImmediate(_fade);
                }

                _fade = null;
            }
        }
    }
}
