using System;
using System.Runtime.InteropServices;
using System.Text;
using RowMate.Core.Common;

namespace RowMate.Platform.Display
{
    // Barra de título de la ventana de Windows en el tema del sistema (QA-V2-08 de la It. 2, AC-APP-05-10 de la It. 3).
    // legacy: windows/runner/win32_window.cpp:275-287 (UpdateTheme): lee HKCU\Software\Microsoft\Windows\CurrentVersion\
    // Themes\Personalize\AppsUseLightTheme (DWORD) y, si lo pudo leer, pone DWMWA_USE_IMMERSIVE_DARK_MODE (20) en
    // (valor == 0). Si el valor no está, no toca nada. El legacy lo aplica al crear la ventana (:147) y con
    // WM_DWMCOLORIZATIONCOLORCHANGED (:216-217). Unity no expone el WndProc del player, así que acá se aplica al arrancar
    // (AppRoot.Awake), al recuperar el foco (PlatformServices.Refresh) y, si el valor del registro cambió, en el sondeo de
    // cada PollSeconds (AppRoot.Update): el cambio con la app enfocada (modo automático) llega en ≤ 2 s. El sondeo es una
    // lectura del registro sin asignaciones; DwmSetWindowAttribute solo corre cuando el valor cambia.
    // Solo en el player de Windows: en el Editor la ventana es la del Editor y no se toca.
    public sealed class WindowsTitleBar
    {
        public const string Tag = "PLATFORM";
        public const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        public const string LightThemeValue = "AppsUseLightTheme";
        public const int DwmUseImmersiveDarkMode = 20;
        public const string UnityWindowClass = "UnityWndClass";
        public const double PollSeconds = 2;

        private readonly IAppLogger _logger;
        private double _nextPoll;

        public WindowsTitleBar(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Lo último que se aplicó (null: nada todavía, o el sistema no dice su tema).
        public bool? AppliedDark { get; private set; }

        // La regla del legacy: con el valor leído, oscura si es 0; sin valor, no se toca (null).
        public static bool? DarkFor(uint? appsUseLightTheme)
        {
            if (appsUseLightTheme == null)
            {
                return null;
            }

            return appsUseLightTheme.Value == 0;
        }

        // Cada PollSeconds: si el tema del sistema cambió desde lo aplicado, lo vuelve a aplicar.
        public void Poll(double monotonicSeconds)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (monotonicSeconds < _nextPoll)
            {
                return;
            }

            _nextPoll = monotonicSeconds + PollSeconds;
            if (DarkFor(ReadAppsUseLightTheme()) != AppliedDark)
            {
                Apply();
            }
#else
            _nextPoll = monotonicSeconds;
#endif
        }

        public void Apply()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                var dark = DarkFor(ReadAppsUseLightTheme());
                if (dark == null)
                {
                    return;
                }

                var window = PlayerWindow();
                if (window == IntPtr.Zero)
                {
                    return;
                }

                var value = dark.Value ? 1 : 0;
                var status = DwmSetWindowAttribute(window, DwmUseImmersiveDarkMode, ref value, sizeof(int));
                if (status != 0)
                {
                    _logger.Log(LogLevel.Warning, Tag, "DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE) devolvió 0x" + status.ToString("X8"));
                    return;
                }

                if (AppliedDark != dark)
                {
                    _logger.Log(LogLevel.Info, Tag, "barra de título " + (dark.Value ? "oscura" : "clara") + " (AppsUseLightTheme)");
                }

                AppliedDark = dark;
            }
            catch (Exception e)
            {
                // Windows sin DWM o sin el atributo (anterior a 10 20H1): la barra queda como la dibuja el sistema.
                _logger.Log(LogLevel.Warning, Tag, "no se pudo aplicar el tema a la barra de título: " + e.GetType().Name);
            }
#else
            _ = _logger;
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const uint RrfRtRegDword = 0x00000010;
        private static readonly UIntPtr HkeyCurrentUser = new UIntPtr(0x80000001u);
        private static IntPtr _found;
        private IntPtr _window;

        private static uint? ReadAppsUseLightTheme()
        {
            uint value = 0;
            uint size = 4;
            uint type;
            var status = RegGetValue(HkeyCurrentUser, PersonalizeKey, LightThemeValue, RrfRtRegDword, out type, ref value, ref size);
            return status == 0 ? value : (uint?)null;
        }

        // La ventana del player: la de clase UnityWndClass del hilo principal (el que corre los scripts y creó la ventana).
        // Se busca una vez; si todavía no se encuentra, se usa la ventana activa del hilo y se vuelve a buscar la próxima vez.
        private IntPtr PlayerWindow()
        {
            if (_window != IntPtr.Zero)
            {
                return _window;
            }

            _found = IntPtr.Zero;
            EnumThreadWindows(GetCurrentThreadId(), OnWindow, IntPtr.Zero);
            _window = _found;
            return _window != IntPtr.Zero ? _window : GetActiveWindow();
        }

        [AOT.MonoPInvokeCallback(typeof(EnumWindowsProc))]
        private static bool OnWindow(IntPtr window, IntPtr parameter)
        {
            var name = new StringBuilder(64);
            if (GetClassName(window, name, name.Capacity) > 0 && name.ToString() == UnityWindowClass)
            {
                _found = window;
                return false;
            }

            return true;
        }

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegGetValue(UIntPtr key, string subKey, string valueName, uint flags, out uint type, ref uint data,
            ref uint dataSize);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
#endif
    }
}
