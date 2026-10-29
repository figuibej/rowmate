using RowMate.Core.Ports;

namespace RowMate.Platform.Power
{
    // Sin pantalla encendida (macOS hasta la It. 10b, con IOPMAssertion en el bundle Swift; 02-tech-design §4.4 de la It. 4a):
    // rechaza el pedido, así ScreenKeepAwake deja su aviso en el registro una vez (AC-APP-14-04) y no se le muestra nada al
    // usuario (paridad).
    public sealed class NoWakeLock : IWakeLock
    {
        public static readonly NoWakeLock Instance = new NoWakeLock();

        private NoWakeLock()
        {
        }

        public bool Enable()
        {
            return false;
        }

        public void Disable()
        {
        }
    }
}
