using System;
using RowMate.Core.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RowMate.Platform.Display
{
    // Lo que WindowModeController le pide a la ventana (It. 14, DT14-15): en el player de Windows, PlayerWindowFrame; en PlayMode, un
    // falso que registra (WindowModeTests).
    public interface IWindowFrame
    {
        // El área de contenido actual en la pantalla (x, y, ancho, alto), en píxeles físicos.
        WindowRect Current { get; }

        // Pantalla completa sin bordes (FullScreenWindow) en el monitor donde está la ventana.
        void EnterFullscreen();

        // A ventana, con el área de contenido de antes (tamaño en el acto; posición en el cuadro siguiente).
        void ExitFullscreen(WindowRect restore);

        // Una vez por cuadro (lo pendiente del cuadro anterior).
        void Tick();
    }

    // F11 o Alt+Enter: ventana ↔ pantalla completa (It. 14, AC-APP-15-07, DV-129; DT14-13, DT14-15). La lógica es WindowModeState (Core);
    // acá solo se leen las teclas del Input System y se aplica la acción. Esc no se toca (sigue siendo "Atrás"). No se recuerda entre
    // arranques. Solo lo crea AppRoot en el player de Windows; allowFullscreenSwitch está en 0, así que Unity no tiene otro camino.
    public sealed class WindowModeController
    {
        private readonly IWindowFrame _frame;

        public WindowModeController(IWindowFrame frame)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public WindowModeState State { get; } = new WindowModeState();

        // Una vez por cuadro (AppRoot.Update): lo pendiente de la ventana y las teclas.
        public void Update()
        {
            _frame.Tick();
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f11Key.wasPressedThisFrame)
            {
                OnKey(WindowKey.F11, false);
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                OnKey(WindowKey.Enter, keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            }
        }

        public WindowModeAction OnKey(WindowKey key, bool alt)
        {
            var action = State.OnKey(key, alt, _frame.Current);
            switch (action)
            {
                case WindowModeAction.EnterFullscreen:
                    _frame.EnterFullscreen();
                    break;
                case WindowModeAction.ExitFullscreen:
                    _frame.ExitFullscreen(State.Restore);
                    break;
            }

            return action;
        }
    }

    // La ventana del player de Windows para WindowModeController y la ubicación inicial (It. 14, DT14-15, DT14-16).
    public sealed class PlayerWindowFrame : IWindowFrame
    {
        private readonly WindowsWndProcApi _api;
        private readonly IntPtr _window;
        private WindowRect? _pendingPlacement;
        private int _waitFrames;

        public PlayerWindowFrame(WindowsWndProcApi api, IntPtr window)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _window = window;
        }

        public WindowRect Current => WindowsWndProcApi.ContentRect(_window);

        public void EnterFullscreen()
        {
            _pendingPlacement = null;
            var display = Screen.mainWindowDisplayInfo;
            Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
        }

        public void ExitFullscreen(WindowRect restore)
        {
            Screen.SetResolution(restore.Width, restore.Height, FullScreenMode.Windowed);
            _pendingPlacement = restore;
            _waitFrames = 1;
        }

        // Ventana inicial (AC-APP-15-05): en ventana, con el tamaño y la posición de WindowGeometry.Initial. Si Unity abrió en pantalla
        // completa (lo recuerda de la vez anterior), primero vuelve a ventana y ubica en el cuadro siguiente; la capa de arranque tapa el
        // cuadro intermedio.
        public WindowRect PlaceInitial()
        {
            var dpi = _api.DpiFor(_window);
            // QA-V14-02: el exterior (contenido + marco con el DPI de la ventana) es el que entra en el área de trabajo.
            var initial = WindowGeometry.Initial(WindowsWndProcApi.PrimaryWorkArea(), WindowGeometry.ScaleFor(dpi), _api.FrameEdges(_window, dpi, Screen.fullScreenMode != FullScreenMode.Windowed));
            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                Screen.SetResolution(initial.Width, initial.Height, FullScreenMode.Windowed);
                _pendingPlacement = initial;
                _waitFrames = 1;
                return initial;
            }

            _api.PlaceContent(_window, initial);
            return initial;
        }

        public void Tick()
        {
            if (_pendingPlacement == null)
            {
                return;
            }

            if (_waitFrames > 0)
            {
                _waitFrames--;
                return;
            }

            _api.PlaceContent(_window, _pendingPlacement.Value);
            _pendingPlacement = null;
        }
    }
}
