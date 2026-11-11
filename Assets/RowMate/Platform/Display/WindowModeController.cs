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
    // It. 15 (#74, AC-D15-19, DV-145; DT15-08, gate m-09): Capture arma lo que se guarda al cerrar (la ventana normal en dp, o la de antes
    // de la pantalla completa) y RestoreSaved la vuelve a poner. Unity aplica antes su propio rectángulo guardado (Screenmanager*): el
    // nuestro se aplica DESPUÉS, en el primer cuadro (SetWindowPos sobre el HWND del player), así gana siempre. No lee ni escribe el
    // registro.
    public sealed class PlayerWindowFrame : IWindowFrame
    {
        private readonly WindowsWndProcApi _api;
        private readonly IntPtr _window;
        private WindowRect? _pendingPlacement;
        private WindowRect? _pendingExterior;
        private bool _pendingMaximize;
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
            _pendingExterior = null;
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

        // Lo guardado al cerrar (dp lógicos con la escala del monitor de la ventana).
        public SavedWindow Capture(WindowModeState state)
        {
            var dpi = _api.DpiFor(_window);
            var scale = WindowGeometry.ScaleFor(dpi);
            var fullscreen = (state != null && state.IsFullscreen) || Screen.fullScreenMode != FullScreenMode.Windowed;
            if (!WindowsWndProcApi.NormalPlacement(_window, out var exterior, out var maximized))
            {
                exterior = WindowsWndProcApi.ExteriorRect(_window);
            }

            var restore = default(WindowRect);
            if (fullscreen && state != null)
            {
                var edges = _api.FrameEdges(_window, dpi, true);
                var content = state.Restore;
                restore = new WindowRect(content.X - edges.Left, content.Y - edges.Top, content.Width + edges.Width, content.Height + edges.Height);
            }

            return WindowPlacement.Capture(exterior, scale, maximized, fullscreen, restore);
        }

        // La ventana guardada, si se ve al menos en un 50 % en algún monitor (true); si no, false y el llamador usa PlaceInitial.
        public bool RestoreSaved(SavedWindow? saved)
        {
            var dpi = _api.DpiFor(_window);
            var choice = WindowPlacement.Choose(saved, WindowsWndProcApi.Monitors(), _api.FrameEdges(_window, dpi, true));
            if (choice == null)
            {
                return false;
            }

            if (Screen.fullScreenMode != FullScreenMode.Windowed)
            {
                Screen.SetResolution(Math.Max(1, choice.Rect.Width), Math.Max(1, choice.Rect.Height), FullScreenMode.Windowed);
            }

            // En el primer cuadro (después de lo que Unity restaura por su cuenta).
            _pendingPlacement = null;
            _pendingExterior = choice.Rect;
            _pendingMaximize = choice.Maximized;
            _waitFrames = 1;
            return true;
        }

        public void Tick()
        {
            if (_pendingExterior != null)
            {
                if (_waitFrames > 0)
                {
                    _waitFrames--;
                    return;
                }

                WindowsWndProcApi.PlaceExterior(_window, _pendingExterior.Value, _pendingMaximize);
                _pendingExterior = null;
                return;
            }

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
