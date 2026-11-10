using System;
using System.Collections.Generic;
using RowMate.Core.Text;

namespace RowMate.Core.Common
{
    // Un rectángulo de ventana en px de pantalla (enteros, como los de Win32).
    public readonly struct WindowRect : IEquatable<WindowRect>
    {
        public WindowRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public bool Equals(WindowRect other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object? obj)
        {
            return obj is WindowRect other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((X * 397 ^ Y) * 397 ^ Width) * 397 ^ Height;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ") " + Width + " × " + Height;
        }
    }

    // Lo que suma el marco de la ventana a cada lado del área de contenido (AdjustWindowRectExForDpi con el DPI de la ventana), en px.
    public readonly struct WindowFrameEdges
    {
        public static readonly WindowFrameEdges None = new WindowFrameEdges(0, 0, 0, 0);

        public WindowFrameEdges(int left, int top, int right, int bottom)
        {
            Left = Math.Max(0, left);
            Top = Math.Max(0, top);
            Right = Math.Max(0, right);
            Bottom = Math.Max(0, bottom);
        }

        public int Left { get; }

        public int Top { get; }

        public int Right { get; }

        public int Bottom { get; }

        public int Width => Left + Right;

        public int Height => Top + Bottom;
    }

    // La geometría de la ventana de escritorio (It. 14, F-APP-15; DT14-13, DT14-16; 02-tech-design §3):
    // - Initial: 1280 × 720 lógicos × la escala del DPI. Con el marco (QA-V14-02), el que se recorta al área de trabajo del monitor
    //   principal y se centra es el rectángulo EXTERIOR (contenido + marco): el contenido se achica lo que haga falta para que la barra
    //   de título y los bordes queden adentro (AC-APP-15-05; legacy windows/runner/main.cpp:28-29; DV-130 centrada).
    // - MinTrack: el tamaño mínimo del MARCO para que el área de contenido no baje de 640 × 360 lógicos (AC-APP-15-06, DV-128):
    //   640 × 360 × escala + las inserciones del marco (las de AdjustWindowRectExForDpi).
    // - ShouldPlaceInitial: con -screen-width, -screen-height, -screen-fullscreen o -popupwindow en la línea de comandos no se
    //   reubica nada (los humos de build.ps1 y QA; QA-G14-07).
    // - macOS: 800 × 600 lógicos (MainMenu.xib:335).
    public static class WindowGeometry
    {
        public const int InitialWidth = 1280;
        public const int InitialHeight = 720;
        public const int MinWidth = 640;
        public const int MinHeight = 360;
        public const int MacInitialWidth = 800;
        public const int MacInitialHeight = 600;
        public const int BaseDpi = 96;

        private static readonly string[] PlacementOverrides = { "-screen-width", "-screen-height", "-screen-fullscreen", "-popupwindow" };

        public static double ScaleFor(int dpi)
        {
            return dpi > 0 ? dpi / (double)BaseDpi : 1;
        }

        // El rectángulo del área de contenido inicial (lógico × escala), centrado y recortado al área de trabajo (sin marco: macOS).
        public static WindowRect Initial(WindowRect workArea, double dpiScale, int logicalWidth = InitialWidth, int logicalHeight = InitialHeight)
        {
            return Initial(workArea, dpiScale, WindowFrameEdges.None, logicalWidth, logicalHeight);
        }

        // El área de contenido inicial con el marco dado: el exterior (contenido + marco) entra en el área de trabajo y va centrado.
        public static WindowRect Initial(WindowRect workArea, double dpiScale, WindowFrameEdges frame, int logicalWidth = InitialWidth, int logicalHeight = InitialHeight)
        {
            if (!(dpiScale > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(dpiScale), dpiScale, "WindowGeometry: la escala tiene que ser positiva.");
            }

            var width = Math.Max(1, Math.Min((int)DartMath.Round(logicalWidth * dpiScale), workArea.Width - frame.Width));
            var height = Math.Max(1, Math.Min((int)DartMath.Round(logicalHeight * dpiScale), workArea.Height - frame.Height));
            var outerX = workArea.X + (workArea.Width - (width + frame.Width)) / 2;
            var outerY = workArea.Y + (workArea.Height - (height + frame.Height)) / 2;
            return new WindowRect(outerX + frame.Left, outerY + frame.Top, width, height);
        }

        // El tamaño mínimo del marco (ptMinTrackSize de WM_GETMINMAXINFO): contenido de 640 × 360 lógicos más el marco.
        public static void MinTrack(int dpi, int frameInsetWidth, int frameInsetHeight, out int width, out int height)
        {
            var scale = ScaleFor(dpi);
            width = (int)DartMath.Round(MinWidth * scale) + Math.Max(0, frameInsetWidth);
            height = (int)DartMath.Round(MinHeight * scale) + Math.Max(0, frameInsetHeight);
        }

        public static bool ShouldPlaceInitial(IReadOnlyList<string> commandLine)
        {
            if (commandLine == null)
            {
                return true;
            }

            for (var i = 0; i < commandLine.Count; i++)
            {
                var arg = commandLine[i];
                if (arg == null)
                {
                    continue;
                }

                for (var j = 0; j < PlacementOverrides.Length; j++)
                {
                    if (string.Equals(arg, PlacementOverrides[j], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // ¿Hay que corregir la ventana al arrancar? Si no está en ventana o el tamaño no es el inicial (Unity recuerda el
        // último; no se lee ni se escribe el registro: se pisa, DT14-16).
        public static bool NeedsCorrection(bool windowed, int width, int height, double dpiScale, WindowRect workArea, WindowFrameEdges frame = default)
        {
            var initial = Initial(workArea, dpiScale, frame);
            return !windowed || width != initial.Width || height != initial.Height;
        }
    }
}
