using System;
using System.Runtime.InteropServices;
using RowMate.Core.Design;

namespace RowMate.Platform.Display
{
    // El fondo de la ventana del player de Windows antes del primer cuadro de Unity (It. 14, QA-V14-03, AC-APP-16-10): entre que la
    // ventana aparece y el primer Present de D3D pasan ≈ 0,7 s (AppRoot.Awake arma la base, el shell y el HUD precalentado) y Windows
    // muestra el área de contenido sin pintar, en blanco. Lo antes posible (RuntimeInitializeLoadType.SubsystemRegistration, antes de
    // cargar la primera escena) se cambia el pincel de fondo de la clase de la ventana a bg.app (#0D1B2A) y se pinta el área de contenido
    // con GDI (FillRect): DWM muestra esa superficie hasta el primer Present. AppRoot vuelve a pintar después de ubicar la ventana inicial
    // (el área crece). Nada de esto corre en el Editor ni fuera del player de Windows. Sin asignaciones salvo el pincel (uno por
    // proceso, que vive lo que la ventana).
    public static class WindowsBootBackground
    {
        public const int GclpHbrBackground = -10;

        private static IntPtr _brush;

        // El COLORREF de Win32 (0x00BBGGRR) de un token.
        public static uint ColorRef(ColorToken token)
        {
            var rgb = token.Rgb;
            return (uint)(rgb.R | (rgb.G << 8) | (rgb.B << 16));
        }

        // Milisegundos desde que arrancó el proceso, para el registro (−1 si no se pudo leer).
        public static double SinceProcessStartMs()
        {
            try
            {
                return (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds;
            }
            catch (Exception)
            {
                return -1;
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // Cuándo se pintó por primera vez (ms desde el arranque del proceso; NaN si no se pudo). AppRoot lo deja en el registro.
        public static double FirstPaintMs { get; private set; } = double.NaN;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnSubsystemRegistration()
        {
            try
            {
                var window = WindowsWndProcApi.FindPlayerWindow();
                if (window != IntPtr.Zero && Paint(window))
                {
                    FirstPaintMs = SinceProcessStartMs();
                }
            }
            catch (Exception)
            {
                // Sin fondo propio: queda el de Windows hasta el primer cuadro (nunca impide arrancar).
            }
        }

        // Pone el pincel de bg.app en la clase de la ventana y pinta el área de contenido ya. Devuelve false si no pudo.
        public static bool Paint(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return false;
            }

            if (_brush == IntPtr.Zero)
            {
                _brush = CreateSolidBrush(ColorRef(Tokens.BgApp));
                if (_brush == IntPtr.Zero)
                {
                    return false;
                }

                SetClassLongPtrW(window, GclpHbrBackground, _brush);
            }

            var rect = new Rect32();
            if (!GetClientRect(window, ref rect))
            {
                return false;
            }

            var dc = GetDC(window);
            if (dc == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                FillRect(dc, ref rect, _brush);
            }
            finally
            {
                ReleaseDC(window, dc);
            }

            GdiFlush();
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect32
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint color);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GdiFlush();

        [DllImport("user32.dll")]
        private static extern IntPtr SetClassLongPtrW(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr window, ref Rect32 rect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr window);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr window, IntPtr dc);

        [DllImport("user32.dll")]
        private static extern int FillRect(IntPtr dc, ref Rect32 rect, IntPtr brush);
#endif
    }
}
