using RowMate.App.Composition;
using RowMate.Bootstrap;
using RowMate.Core.Common;
using RowMate.Tests.Support;
#if ROWMATE_SIMULATOR
using RowMate.Simulator;
#endif

namespace RowMate.Tests.UiSupport
{
    // QA-G2-05: el host no elige entre variantes fijas; recibe una fábrica. Cada fábrica crea servicios nuevos sobre el reloj
    // del host y su dueño (DeviceBackend.Dispose los libera).
    public delegate DeviceBackend HostBackendFactory(IScheduler scheduler, IAppLogger logger);

    public static class HostBackends
    {
        // "Prueba": ControllableRowerService + ScriptedHeartRateService (spec §0.4).
        public static readonly HostBackendFactory TestDevices = (scheduler, logger) =>
            new DeviceBackend(new ControllableRowerService(logger), new ScriptedHeartRateService(logger), null);

        // Build normal de la It. 2 (equipo sin BLE): HardwareBackend (CAP-22).
        public static readonly HostBackendFactory Unavailable = (scheduler, logger) => HardwareBackend.Create(logger);

#if ROWMATE_SIMULATOR
        // "Sim": remo y pulsómetro simulados sin ruido sobre un solo RowingSimulator.
        public static HostBackendFactory Simulator(bool noise = false)
        {
            return (scheduler, logger) => SimulatorBackend.Create(scheduler, logger, noise);
        }

        // LT-31 (= _pump con bleFactory, heart_rate_card_test.dart:51-56, :161-165): un RowingSimulator sin ruido compartido por
        // un ControllableRowerService (el remo de prueba) y un SimulatedHeartRateService.
        public static HostBackendFactory TestRowerWithSimulatedHeartRate()
        {
            return (scheduler, logger) =>
            {
                var simulator = new RowingSimulator(null, false);
                return new DeviceBackend(new ControllableRowerService(logger), new SimulatedHeartRateService(simulator, scheduler, logger), null);
            };
        }
#endif
    }
}
