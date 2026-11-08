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

        // El contorno de depuración de la zona del bote en el inmersivo (#hud-boat-zone; It. 4a, AC-WKT-17-09): solo lo pide el
        // anfitrión de pruebas y del arnés.
        public bool BoatZoneOutline { get; set; }

        // Hay capa del simulador (con la llave): el HUD le reserva su lugar (HudGeometry, DN4a-14). Presentation no sabe del
        // simulador: solo que arriba hay una capa con la llave.
        public bool ReserveWrench { get; set; }

        // It. 6a (06a-escena-lago/02-tech-design §3.3; DT6a-03): la escena 3D del inmersivo (Bootstrap/SceneBridge sobre Scene/SceneHost).
        // null = sin escena: el fondo provisorio de siempre (las pruebas y las tomas de las It. 2 a 5).
        public RowMate.Presentation.Workout.IImmersiveScene? Scene { get; set; }

        // It. 8 (08-entornos-selector/02-tech-design §3.4; AC-SCN-22-04): las miniaturas horneadas del selector, en el orden de
        // EnvironmentId (UiAssets las trae; las pone AppRoot o el anfitrión de pruebas). Una que falta: el degradé de la tarjeta y UN
        // aviso [SLOT] ui.thumbnail.<escenario>.
        public Texture2D?[] Thumbnails { get; } = new Texture2D?[4];

        private readonly bool[] _thumbnailWarned = new bool[4];

        public Texture2D? Thumbnail(RowMate.Core.Scene.EnvironmentId id)
        {
            var i = (int)id;
            var texture = i >= 0 && i < Thumbnails.Length ? Thumbnails[i] : null;
            if (texture == null && i >= 0 && i < _thumbnailWarned.Length && !_thumbnailWarned[i])
            {
                _thumbnailWarned[i] = true;
                Logger.Log(LogLevel.Warning, "SLOT", RowMate.Core.Scene.SlotIds.ForThumbnail(id) + ": falta la miniatura horneada; se usa el degradé");
            }

            return texture;
        }

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
