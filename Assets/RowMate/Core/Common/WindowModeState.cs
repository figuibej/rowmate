namespace RowMate.Core.Common
{
    // Las teclas que mira el control de pantalla completa.
    public enum WindowKey
    {
        Other,
        F11,
        Enter,
        Escape,
    }

    public enum WindowModeAction
    {
        None,

        // A pantalla completa sin bordes en el monitor donde está la ventana (FullScreenWindow).
        EnterFullscreen,

        // A ventana, al rectángulo guardado (Restore).
        ExitFullscreen,
    }

    // Ventana ↔ pantalla completa en Windows (It. 14, AC-APP-15-07, DV-129; DT14-13, DT14-15): F11 o Alt+Enter alternan; la misma
    // tecla vuelve al tamaño y la posición de antes; Esc no hace nada (sigue siendo "Atrás" / cerrar diálogo); Enter sin Alt
    // tampoco. No se recuerda entre arranques (cada arranque abre en ventana, AC-APP-15-05). Entrar o salir del inmersivo no
    // pasa por acá (AC-APP-15-02).
    public sealed class WindowModeState
    {
        public bool IsFullscreen { get; private set; }

        // El rectángulo de la ventana antes de pasar a pantalla completa (válido mientras IsFullscreen).
        public WindowRect Restore { get; private set; }

        public WindowModeAction OnKey(WindowKey key, bool alt, WindowRect current)
        {
            if (key == WindowKey.F11 || (key == WindowKey.Enter && alt))
            {
                return Toggle(current);
            }

            return WindowModeAction.None;
        }

        public WindowModeAction Toggle(WindowRect current)
        {
            if (IsFullscreen)
            {
                IsFullscreen = false;
                return WindowModeAction.ExitFullscreen;
            }

            Restore = current;
            IsFullscreen = true;
            return WindowModeAction.EnterFullscreen;
        }
    }
}
