using System.Threading.Tasks;

namespace RowMate.Core.Ble
{
    // El permiso de Bluetooth de Android (DT10-15, DT10-16; 02-tech-design §2.1, §2.6). Lo implementa AndroidBlePermissions
    // (Platform, delgado: checkSelfPermission, shouldShowRequestPermissionRationale y la negativa recordada en el plugin); la
    // regla (cuándo pedir, cuándo Unauthorized, cuándo abrir los ajustes) es de NativeBleAdapter. Windows y el simulador no
    // tienen (null). Hilo principal: la respuesta del diálogo llega por MainThreadQueue (DT10-10).
    public interface IBlePermissions
    {
        BlePermissionState State { get; }

        // Muestra el diálogo del sistema si puede; termina con el estado resultante.
        Task<BlePermissionState> RequestAsync();

        // Relee el permiso (al volver al frente: el usuario pudo darlo en los ajustes).
        void Refresh();

        // Abre los ajustes de la app (ACTION_APPLICATION_DETAILS_SETTINGS).
        void OpenAppSettings();
    }
}
