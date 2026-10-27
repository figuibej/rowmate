using RowMate.App.Composition;
using RowMate.Core.Common;
#if ROWMATE_SIMULATOR
using RowMate.Simulator;
#endif

namespace RowMate.Bootstrap
{
    // El ÚNICO lugar que elige la variante de servicios de la app (= main.dart:51-54; AC-APP-01-02, AC-BLE-13-03, R-06).
    // Con ROWMATE_SIMULATOR: remo y pulsómetro simulados sobre un solo RowingSimulator (con ruido, como la app del legacy).
    // Sin el define: HardwareBackend (It. 2: equipo sin BLE; 10a/11: servicios reales). Las pantallas no se enteran.
    internal static class DeviceBackendFactory
    {
        public static DeviceBackend Create(IScheduler scheduler, IAppLogger logger)
        {
#if ROWMATE_SIMULATOR
            return SimulatorBackend.Create(scheduler, logger, noise: true);
#else
            return HardwareBackend.Create(logger);
#endif
        }
    }
}
