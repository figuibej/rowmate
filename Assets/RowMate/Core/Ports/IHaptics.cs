namespace RowMate.Core.Ports
{
    // Respuesta táctil (A10-06, D10-11 de la It. 10a): una vibración corta y sutil al quedar "Listo" con el remo. Android:
    // performHapticFeedback(CONFIRM) en API 30+ y CONTEXT_CLICK en 26-29, sin el permiso VIBRATE; Windows y el Editor: nada
    // (NoHaptics). Reducir movimiento no la apaga. Hilo principal.
    public interface IHaptics
    {
        void Confirm();
    }
}
