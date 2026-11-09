using RowMate.App.Composition;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Core.Ports;
#if ROWMATE_SIMULATOR
using RowMate.Simulator;
#endif

namespace RowMate.Bootstrap
{
    // El ÚNICO lugar que elige la variante de servicios de la app (= main.dart:51-54; AC-APP-01-02, AC-BLE-13-03, R-06).
    // Con ROWMATE_SIMULATOR: remo y pulsómetro simulados sobre un solo RowingSimulator (con ruido, como la app del legacy); nunca
    // crea el adaptador nativo ni carga la DLL (AC-APP-13-03). Sin el define: HardwareBackend (It. 10a: el remo real sobre el plugin
    // nativo, con su bomba de eventos; sin plugin, el equipo sin BLE de la It. 2). Las pantallas no se enteran.
    internal static class DeviceBackendFactory
    {
        // It. 11 (AC-HRM-15-13): las preferencias solo llegan al pulsómetro real; la rama del simulador no las ve ni escribe hrm.*.
        public static DeviceBackend Create(IScheduler scheduler, IAppLifecycle lifecycle, IAppLogger logger, MainThreadQueue mainThread,
                                           IPreferences preferences, out IBleEventPump? pump)
        {
#if ROWMATE_SIMULATOR
            pump = null;
            return SimulatorBackend.Create(scheduler, logger, noise: true);
#else
            return HardwareBackend.Create(scheduler, lifecycle, logger, mainThread, preferences, out pump);
#endif
        }

        // It. 6a ([PERF] de la escena, solo desarrollo): el preset del simulador (Fuerte = 280 W y 30 spm, SimulatorPanelStore). Sin el
        // simulador no hace nada.
        public static void SetPerfPreset(DeviceBackend backend, int watts, int spm)
        {
#if ROWMATE_SIMULATOR
            if (backend is SimulatorDeviceBackend simulator)
            {
                simulator.Simulator.TargetWatts = watts;
                simulator.Simulator.TargetSpm = spm;
            }
#endif
        }
    }
}
