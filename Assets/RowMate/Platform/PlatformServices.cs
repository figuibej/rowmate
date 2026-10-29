using System;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Accessibility;
using RowMate.Platform.Display;
using RowMate.Platform.Input;
using RowMate.Platform.Locale;
using RowMate.Platform.Storage;

namespace RowMate.Platform
{
    // Lo que la app necesita del sistema, ya elegido por plataforma (PlatformFactory): idioma, accesibilidad, densidad y
    // área segura, "Atrás", la barra de título de Windows y, desde la It. 3, la carpeta privada de la base, el portapapeles y
    // el teclado del teléfono (03-rutinas/02-tech-design §1.1); desde la It. 4a, la pantalla siempre encendida (WakeLock,
    // 02-tech-design §4.4). Refresh() vuelve a leer idioma, accesibilidad y pantalla y vuelve a aplicar el tema de la barra de
    // título (al recuperar el foco, AC-APP-09-05, QA-V2-08).
    public sealed class PlatformServices
    {
        public PlatformServices(SystemLocaleProvider locale, AccessibilitySettingsProvider accessibility, DisplayMetricsProvider display,
                                BackButtonPolicy backButton, PlatformKind kind, bool isDesktop, WindowsTitleBar titleBar,
                                AppDataLocation storage, IClipboard clipboard, ISoftKeyboard keyboard, IWakeLock wakeLock)
        {
            WakeLock = wakeLock ?? throw new ArgumentNullException(nameof(wakeLock));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            Keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            Accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            Display = display ?? throw new ArgumentNullException(nameof(display));
            BackButton = backButton ?? throw new ArgumentNullException(nameof(backButton));
            TitleBar = titleBar ?? throw new ArgumentNullException(nameof(titleBar));
            Kind = kind;
            IsDesktop = isDesktop;
        }

        public SystemLocaleProvider Locale { get; }

        public AccessibilitySettingsProvider Accessibility { get; }

        public DisplayMetricsProvider Display { get; }

        public BackButtonPolicy BackButton { get; }

        // Solo hace algo en el player de Windows (legacy win32_window.cpp:275, QA-V2-08).
        public WindowsTitleBar TitleBar { get; }

        public PlatformKind Kind { get; }

        // Carpeta privada de la base (AC-HIS-17-01); Bootstrap/DataLocation la puede reemplazar en desarrollo.
        public AppDataLocation Storage { get; }

        // GUIUtility.systemCopyBuffer (menú del clic derecho, AC-RUT-11-09).
        public IClipboard Clipboard { get; }

        // TouchScreenKeyboard en el teléfono; en escritorio, siempre oculto (KeyboardAvoider, AC-RUT-11-10).
        public ISoftKeyboard Keyboard { get; }

        // Windows y macOS (y el Editor): foco de teclado y hover (FocusHighlight, .rm-desktop).
        public bool IsDesktop { get; }

        // Pantalla siempre encendida de la plataforma (WakeLockFactory); cuándo pedirla lo decide ScreenKeepAwake (Core).
        public IWakeLock WakeLock { get; }

        public void Refresh()
        {
            Locale.Refresh();
            Accessibility.Refresh();
            Display.Refresh();
            TitleBar.Apply();
        }
    }
}
