using RowMate.Core.Ports;

namespace RowMate.Tests.Support
{
    // Pantalla encendida de prueba (02-tech-design §4.4 de la It. 4a): cuenta pedidos y liberaciones y simula el rechazo del
    // sistema (Accepts = false). Nunca toca el sistema.
    public sealed class FakeWakeLock : IWakeLock
    {
        public bool Accepts { get; set; } = true;

        public int EnableCalls { get; private set; }

        public int DisableCalls { get; private set; }

        // Pedida y aceptada, sin liberar.
        public bool IsHeld { get; private set; }

        public bool Enable()
        {
            EnableCalls++;
            IsHeld = Accepts;
            return Accepts;
        }

        public void Disable()
        {
            DisableCalls++;
            IsHeld = false;
        }
    }
}
