using RowMate.App.Composition;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;
using RowMate.Core.Ble.Unavailable;
using RowMate.Core.Common;
using RowMate.Core.Ports;
using RowMate.Platform.Ble;

namespace RowMate.Bootstrap
{
    // Backend del build normal (02-tech-design §1.4 de la It. 2; 10a 02-tech-design §5.1). Es código de producción, no un doble de
    // prueba.
    // - Create: el remo real (RowerService, Core) sobre el plugin nativo (NativeBleAdapter + WindowsBleApi o el de Android), si
    //   BleAdapterFactory da uno; el adaptador es también la bomba de eventos que AppRoot drena en cada cuadro. Sin plugin (otro
    //   sistema, ROWMATE_BLE=off, la DLL que no carga): el equipo sin Bluetooth LE de la It. 2. El pulsómetro real es de la It. 11.
    // - Unavailable: el equipo sin Bluetooth LE (P-04, AC-BLE-13-05: remo y pulsómetro "no disponibles", buscar no hace nada; legacy
    //   ble_service.dart:65-67, heart_rate_service.dart:108-113). Público: lo usa también UiTestHost (HostBackends.Unavailable, CAP-22).
    public static class HardwareBackend
    {
        public const string Tag = "BLE";

        public static DeviceBackend Create(IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger, MainThreadQueue mainThread,
                                           out IBleEventPump? pump)
        {
            pump = null;
            var api = BleAdapterFactory.Create(logger, mainThread, out var permissions);
            if (api == null)
            {
                return Unavailable(logger);
            }

            var adapter = new NativeBleAdapter(api, permissions, scheduler, lifecycle, logger);
            pump = adapter;
            var rower = new RowerService(adapter, scheduler, lifecycle, logger);
            logger.Log(LogLevel.Info, Tag, "Remo real (plugin nativo, adaptador " + adapter.State.Value + ")");
            // Dispose: pulsómetro, remo y después el adaptador (cierra las sesiones y el plugin).
            return new DeviceBackend(rower, new UnavailableHeartRateService(logger), adapter);
        }

        public static DeviceBackend Unavailable(IAppLogger logger)
        {
            return new DeviceBackend(new UnavailableRowerService(logger), new UnavailableHeartRateService(logger), null);
        }
    }
}
