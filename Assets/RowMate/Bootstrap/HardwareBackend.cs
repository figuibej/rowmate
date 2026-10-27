using RowMate.App.Composition;
using RowMate.Core.Ble.Unavailable;
using RowMate.Core.Common;

namespace RowMate.Bootstrap
{
    // Backend del build normal (02-tech-design §1.4). En la It. 2 es un equipo sin Bluetooth LE (P-04, AC-BLE-13-05): el remo
    // y el pulsómetro "no disponibles" quedan desconectados, ninguna fuente emite y buscar no hace nada, como el legacy en
    // un equipo sin BLE (ble_service.dart:65-67, heart_rate_service.dart:108-113). Es código de producción, no un doble de
    // prueba. En la 10a/11 se reemplazan por RowerService / HeartRateService reales. Público: lo usa también UiTestHost
    // (HostBackends.Unavailable, CAP-22).
    public static class HardwareBackend
    {
        public static DeviceBackend Create(IAppLogger logger)
        {
            return new DeviceBackend(new UnavailableRowerService(logger), new UnavailableHeartRateService(logger), null);
        }
    }
}
