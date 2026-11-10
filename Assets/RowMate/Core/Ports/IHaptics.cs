namespace RowMate.Core.Ports
{
    // Respuesta táctil (A10-06, D10-11 de la It. 10a; It. 14, DT14-23). Android: View.performHapticFeedback, sin el permiso
    // VIBRATE (respeta el ajuste "respuesta táctil" del sistema); Windows y el Editor: nada (NoHaptics). Reducir movimiento no
    // la apaga. Hilo principal.
    // Confirm() queda como atajo de la It. 10a. Play(kind) tiene una implementación por defecto que solo atiende Confirm, así las
    // plataformas de la 10a compilan sin cambios hasta que WP-B1 implemente los demás tipos (AndroidHaptics con RmHaptics).
    public interface IHaptics
    {
        void Confirm();

        void Play(HapticKind kind)
        {
            if (kind == HapticKind.Confirm)
            {
                Confirm();
            }
        }
    }
}
