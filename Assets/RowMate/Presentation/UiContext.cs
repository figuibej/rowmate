using System;
using RowMate.Core.Common;
using RowMate.Core.Localization;
using RowMate.Core.Ports;
using RowMate.Presentation.Controls;
using RowMate.Presentation.Motion;
using RowMate.Presentation.Shell;
using UnityEngine;
using AppContext = RowMate.App.Composition.AppContext;

namespace RowMate.Presentation
{
    // Lo que comparten las vistas de la app (02-tech-design §3 de la It. 2; 03-rutinas/02-tech-design §1.1): la app armada
    // (stores, textos, accesibilidad), el reloj de las animaciones, la textura de degradé de los brillos (DN-06), la densidad
    // y el área segura, si es escritorio, el portapapeles y el teclado del teléfono, y desde la It. 3 las capas del shell
    // (páginas, diálogos, avisos y menús), que AppShellView arma ANTES que las pantallas.
    // Lo arma ShellHost; ninguna vista sabe si los servicios son simulados, reales o de prueba (AC-BLE-13-04).
    public sealed class UiContext
    {
        private ShellLayout? _layout;
        private PageStack? _pages;
        private DialogLayer? _dialogs;
        private ToastLayer? _toasts;
        private PopupLayer? _popups;

        public UiContext(AppContext app, MotionDriver motion, Texture2D fade, IDisplayMetrics display, bool isDesktop, IClipboard clipboard,
                         ISoftKeyboard keyboard)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            Fade = fade;
            Display = display ?? throw new ArgumentNullException(nameof(display));
            IsDesktop = isDesktop;
            Clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
            Keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        }

        public AppContext App { get; }

        public MotionDriver Motion { get; }

        public Texture2D Fade { get; }

        public IDisplayMetrics Display { get; }

        public bool IsDesktop { get; }

        // El portapapeles del menú de texto (SystemClipboard en la app; FakeClipboard en pruebas y capturas, R3-10).
        public IClipboard Clipboard { get; }

        // El teclado del teléfono (KeyboardAvoider, AC-RUT-11-10).
        public ISoftKeyboard Keyboard { get; }

        // Menú propio del texto en el teléfono (R3-13): SystemMenu por defecto; AppMenu abre el de la PC al mantener presionado.
        public TextMenuTouchMode TextMenuTouchMode { get; set; } = TextMenuTouchMode.SystemMenu;

        // El panel dibuja con forceGammaRendering (GammaRt): ver MeshColor.
        public bool GammaPanel { get; set; }

        public IAppLogger Logger => App.Logger;

        public Localizer Texts => App.Texts.Value;

        public ShellLayout Layout => _layout ?? throw new InvalidOperationException("UiContext: el shell todavía no armó sus capas.");

        public PageStack Pages => _pages ?? throw new InvalidOperationException("UiContext: el shell todavía no armó sus capas.");

        public DialogLayer Dialogs => _dialogs ?? throw new InvalidOperationException("UiContext: el shell todavía no armó sus capas.");

        public ToastLayer Toasts => _toasts ?? throw new InvalidOperationException("UiContext: el shell todavía no armó sus capas.");

        public PopupLayer Popups => _popups ?? throw new InvalidOperationException("UiContext: el shell todavía no armó sus capas.");

        // Las pone AppShellView, una sola vez, antes de crear las pantallas (03-rutinas/02-tech-design §4.1).
        internal void AttachLayers(ShellLayout layout, PageStack pages, DialogLayer dialogs, ToastLayer toasts, PopupLayer popups)
        {
            if (_layout != null)
            {
                throw new InvalidOperationException("UiContext: las capas ya están puestas.");
            }

            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _pages = pages ?? throw new ArgumentNullException(nameof(pages));
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
        }
    }
}
