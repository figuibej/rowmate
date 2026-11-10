using System;
using System.Runtime.InteropServices;
using RowMate.Core.Common;

namespace RowMate.Platform.Display
{
    // Lo que WindowsWindowFrame necesita de Windows (It. 14, DT14-14): en el player, user32 (WindowsWndProcApi); en EditMode, un falso
    // (WindowsWindowFrameTests). Todo en píxeles físicos (el player de Unity es per-monitor DPI aware).
    public interface IWndProcApi
    {
        IntPtr GetWndProc(IntPtr window);

        // Devuelve el procedimiento anterior.
        IntPtr SetWndProc(IntPtr window, IntPtr procedure);

        IntPtr CallOriginal(IntPtr original, IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        int DpiFor(IntPtr window);

        // Lo que suma el marco (bordes y barra de título) al área de contenido, con el DPI dado.
        void FrameInsets(IntPtr window, int dpi, out int width, out int height);

        // Encola un mensaje en la ventana (PostMessageW): el WM_CLOSE que vuelve a llegar directo al procedimiento de Unity (QA-V14-01).
        bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }

    // Qué pasó la última vez que se quiso sacar la subclase (QA-V14-01: queda en el registro del player al salir).
    public enum WndProcRestore
    {
        None,
        Restored,
        NotOurs,
    }

    // La ventana del player de Windows (It. 14, AC-APP-15-05/-06; DT14-13, DT14-14, QA-G14-04, QA-G14-14): una subclase del WndProc hecha
    // desde C#, sin DLL nueva (SetWindowLongPtrW(GWLP_WNDPROC) + CallWindowProcW):
    // - WM_GETMINMAXINFO: primero el original (así queda el máximo que calcula Unity) y después solo ptMinTrackSize = 640 × 360 lógicos
    //   más el marco (WindowGeometry.MinTrack), con Marshal.WriteInt32 en los desplazamientos 24 y 28, sin asignar.
    // - WM_SYSCHAR con VK_RETURN: devuelve 0 (Alt+Enter sin el pitido de Windows; lo maneja WindowModeController).
    // - WM_CLOSE (la X, Alt+F4, el menú del sistema; QA-V14-01): Unity apaga el player mientras atiende WM_CLOSE, y con nuestro thunk
    //   en la cadena (o en la pila, porque el original se llamaba desde acá) terminaba en 0xC0000005 después del apagado del dominio.
    //   Ahora se restaura el original y se encola el MISMO WM_CLOSE (PostMessageW), que llega directo al procedimiento de Unity sin nada
    //   administrado en el medio; el nuestro devuelve 0. Si otra subclase quedó encima (no se puede restaurar), se delega como antes y
    //   queda NotOurs en LastUninstall para el registro.
    // - WM_DESTROY / WM_NCDESTROY: se restaura antes de delegar (por si la ventana se destruye sin pasar por WM_CLOSE).
    // - El resto, al original.
    // El delegado vive en un campo estático (con [MonoPInvokeCallback] por si algún día se usa IL2CPP). Uninstall() vuelve a poner el
    // original solo si el procedimiento actual sigue siendo el nuestro; es idempotente y lo llaman Application.quitting (suscrito al
    // instalar) y AppRoot.OnDestroy, siempre antes de que se apague el dominio. Después el manejador solo delega. El manejador no llama a
    // Unity (R14-02). Solo se instala en el player de Windows (UNITY_STANDALONE_WIN && !UNITY_EDITOR); en el Editor no hace nada.
    public sealed class WindowsWindowFrame : IDisposable
    {
        public const uint WmDestroy = 0x0002;
        public const uint WmClose = 0x0010;
        public const uint WmNcDestroy = 0x0082;
        public const uint WmGetMinMaxInfo = 0x0024;
        public const uint WmSysChar = 0x0106;
        public const int VkReturn = 0x0D;

        // MINMAXINFO: ptReserved (8), ptMaxSize (8), ptMaxPosition (8), ptMinTrackSize (8: x en 24, y en 28), ptMaxTrackSize (8).
        public const int MinTrackXOffset = 24;
        public const int MinTrackYOffset = 28;

        private static readonly WndProc Procedure = OnMessage;
        private static WindowsWindowFrame? _current;
        private static IntPtr _fallbackOriginal;

        private readonly IWndProcApi _api;
        private readonly IntPtr _window;
        private IntPtr _original;
        private IntPtr _ours;

        public WindowsWindowFrame(IWndProcApi api, IntPtr window)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _window = window;
        }

        public delegate IntPtr WndProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        public bool IsInstalled { get; private set; }

        public IntPtr Window => _window;

        public IntPtr Original => _original;

        // Lo que pasó en la última Uninstall que hizo algo (None si nunca se sacó).
        public WndProcRestore LastUninstall { get; private set; }

        public static string Describe(WndProcRestore restore)
        {
            switch (restore)
            {
                case WndProcRestore.Restored:
                    return "subclase de la ventana retirada: WndProc original restaurado";
                case WndProcRestore.NotOurs:
                    return "subclase de la ventana NO retirada: otra subclase quedó encima (el thunk sigue en la cadena)";
                default:
                    return "subclase de la ventana sin retirar";
            }
        }

        // El puntero a nuestro procedimiento (el mismo en todo el proceso).
        public static IntPtr OurProcedure => Marshal.GetFunctionPointerForDelegate(Procedure);

        // quitting: la suscripción a Application.quitting (la pone quien llama: Uninstall se engancha ahí). Devuelve false si no se pudo.
        public bool Install(Action<Action>? subscribeQuitting)
        {
            if (IsInstalled || _window == IntPtr.Zero)
            {
                return IsInstalled;
            }

            _ours = OurProcedure;
            _current = this;
            _original = _api.SetWndProc(_window, _ours);
            if (_original == IntPtr.Zero)
            {
                _current = null;
                return false;
            }

            _fallbackOriginal = _original;
            IsInstalled = true;
            subscribeQuitting?.Invoke(Uninstall);
            return true;
        }

        // Restaura el procedimiento original si el actual sigue siendo el nuestro. Idempotente.
        public void Uninstall()
        {
            if (!IsInstalled)
            {
                return;
            }

            IsInstalled = false;
            if (_api.GetWndProc(_window) == _ours)
            {
                _api.SetWndProc(_window, _original);
                LastUninstall = WndProcRestore.Restored;
            }
            else
            {
                LastUninstall = WndProcRestore.NotOurs;
            }

            if (ReferenceEquals(_current, this))
            {
                _current = null;
            }
        }

        public void Dispose()
        {
            Uninstall();
        }

        // El manejador (lo llama Windows con el delegado estático; las pruebas lo llaman directo).
        public IntPtr Handle(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (message == WmGetMinMaxInfo && lParam != IntPtr.Zero)
            {
                var result = _api.CallOriginal(_original, window, message, wParam, lParam);
                var dpi = _api.DpiFor(window);
                _api.FrameInsets(window, dpi, out var insetWidth, out var insetHeight);
                WindowGeometry.MinTrack(dpi, insetWidth, insetHeight, out var width, out var height);
                Marshal.WriteInt32(lParam, MinTrackXOffset, width);
                Marshal.WriteInt32(lParam, MinTrackYOffset, height);
                return result;
            }

            if (message == WmSysChar && wParam.ToInt64() == VkReturn)
            {
                return IntPtr.Zero;
            }

            if (message == WmClose)
            {
                Uninstall();
                if (LastUninstall == WndProcRestore.Restored && _api.PostMessage(window, message, wParam, lParam))
                {
                    return IntPtr.Zero;
                }

                return _api.CallOriginal(_original, window, message, wParam, lParam);
            }

            if (message == WmDestroy || message == WmNcDestroy)
            {
                Uninstall();
            }

            return _api.CallOriginal(_original, window, message, wParam, lParam);
        }

        [AOT.MonoPInvokeCallback(typeof(WndProc))]
        private static IntPtr OnMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            var current = _current;
            if (current != null && current.IsInstalled)
            {
                return current.Handle(window, message, wParam, lParam);
            }

            // Después de Uninstall (o si algo quedó colgado): solo delega.
            return WindowsWndProcApi.CallWindowProc(_fallbackOriginal, window, message, wParam, lParam);
        }
    }

    // user32 para WindowsWindowFrame y la ubicación de la ventana (It. 14). Las llamadas existen en todas las plataformas como
    // declaraciones; solo se usan en el player de Windows.
    public sealed class WindowsWndProcApi : IWndProcApi
    {
        private const int GwlpWndProc = -4;
        private const uint WsOverlappedWindow = 0x00CF0000;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SpiGetWorkArea = 0x0030;
        public const string UnityWindowClass = "UnityWndClass";

        private static IntPtr _found;

        public IntPtr GetWndProc(IntPtr window)
        {
            return GetWindowLongPtrW(window, GwlpWndProc);
        }

        public IntPtr SetWndProc(IntPtr window, IntPtr procedure)
        {
            return SetWindowLongPtrW(window, GwlpWndProc, procedure);
        }

        public IntPtr CallOriginal(IntPtr original, IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            return CallWindowProc(original, window, message, wParam, lParam);
        }

        public static IntPtr CallWindowProc(IntPtr original, IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            return original == IntPtr.Zero ? DefWindowProcW(window, message, wParam, lParam) : CallWindowProcW(original, window, message, wParam, lParam);
        }

        public int DpiFor(IntPtr window)
        {
            var dpi = (int)GetDpiForWindow(window);
            return dpi > 0 ? dpi : WindowGeometry.BaseDpi;
        }

        public bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            return PostMessageW(window, message, wParam, lParam);
        }

        public void FrameInsets(IntPtr window, int dpi, out int width, out int height)
        {
            var style = (uint)GetWindowLongPtrW(window, -16).ToInt64();
            var exStyle = (uint)GetWindowLongPtrW(window, -20).ToInt64();
            var rect = new Rect32 { Left = 0, Top = 0, Right = 0, Bottom = 0 };
            if (!AdjustWindowRectExForDpi(ref rect, style != 0 ? style : WsOverlappedWindow, false, exStyle, (uint)dpi))
            {
                width = 0;
                height = 0;
                return;
            }

            width = rect.Right - rect.Left;
            height = rect.Bottom - rect.Top;
        }

        // Lo que suma el marco a cada lado del contenido con el DPI dado (QA-V14-02: la ventana inicial recorta el exterior).
        // windowedStyle: con el estilo de una ventana normal (si todavía está en pantalla completa, sin bordes).
        public WindowFrameEdges FrameEdges(IntPtr window, int dpi, bool windowedStyle = false)
        {
            var style = windowedStyle ? 0u : (uint)GetWindowLongPtrW(window, -16).ToInt64();
            var exStyle = windowedStyle ? 0u : (uint)GetWindowLongPtrW(window, -20).ToInt64();
            var rect = new Rect32 { Left = 0, Top = 0, Right = 0, Bottom = 0 };
            if (!AdjustWindowRectExForDpi(ref rect, style != 0 ? style : WsOverlappedWindow, false, exStyle, (uint)dpi))
            {
                return WindowFrameEdges.None;
            }

            return new WindowFrameEdges(-rect.Left, -rect.Top, rect.Right, rect.Bottom);
        }

        // El área de trabajo del monitor principal (sin la barra de tareas), en píxeles físicos.
        public static WindowRect PrimaryWorkArea()
        {
            var rect = new Rect32();
            if (!SystemParametersInfoW(SpiGetWorkArea, 0, ref rect, 0))
            {
                return new WindowRect(0, 0, 1920, 1080);
            }

            return new WindowRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        // El área de contenido de la ventana en la pantalla (x, y, ancho, alto).
        public static WindowRect ContentRect(IntPtr window)
        {
            var client = new Rect32();
            if (!GetClientRect(window, ref client))
            {
                return default;
            }

            var origin = new Point32 { X = 0, Y = 0 };
            ClientToScreen(window, ref origin);
            return new WindowRect(origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top);
        }

        // Pone el área de contenido en content (el marco se suma con el DPI de la ventana).
        public void PlaceContent(IntPtr window, WindowRect content)
        {
            var dpi = DpiFor(window);
            FrameInsets(window, dpi, out var insetWidth, out var insetHeight);
            var style = (uint)GetWindowLongPtrW(window, -16).ToInt64();
            var exStyle = (uint)GetWindowLongPtrW(window, -20).ToInt64();
            var rect = new Rect32 { Left = content.X, Top = content.Y, Right = content.X + content.Width, Bottom = content.Y + content.Height };
            if (!AdjustWindowRectExForDpi(ref rect, style != 0 ? style : WsOverlappedWindow, false, exStyle, (uint)dpi))
            {
                rect = new Rect32 { Left = content.X, Top = content.Y, Right = content.X + content.Width + insetWidth, Bottom = content.Y + content.Height + insetHeight };
            }

            SetWindowPos(window, IntPtr.Zero, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top, SwpNoZOrder | SwpNoActivate);
        }

        // La ventana del player: la de clase UnityWndClass del hilo que corre los scripts (el que creó la ventana).
        public static IntPtr FindPlayerWindow()
        {
            _found = IntPtr.Zero;
            EnumThreadWindows(GetCurrentThreadId(), OnWindow, IntPtr.Zero);
            return _found;
        }

        [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
        private static bool OnWindow(IntPtr window, IntPtr parameter)
        {
            var name = new System.Text.StringBuilder(64);
            if (GetClassNameW(window, name, name.Capacity) > 0 && name.ToString() == UnityWindowClass)
            {
                _found = window;
                return false;
            }

            return true;
        }

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect32
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point32
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtrW(IntPtr window, int index);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtrW(IntPtr window, int index, IntPtr value);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProcW(IntPtr previous, IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessageW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProcW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustWindowRectExForDpi(ref Rect32 rect, uint style, [MarshalAs(UnmanagedType.Bool)] bool menu, uint exStyle, uint dpi);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfoW(uint action, uint param, ref Rect32 value, uint winIni);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClientRect(IntPtr window, ref Rect32 rect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr window, ref Point32 point);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassNameW(IntPtr window, System.Text.StringBuilder name, int capacity);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}
