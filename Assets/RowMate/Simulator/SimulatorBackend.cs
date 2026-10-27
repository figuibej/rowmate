using System;
using RowMate.App.Composition;
using RowMate.Core.Common;

namespace RowMate.Simulator
{
    // Backend del modo simulador: el remo y el pulsómetro simulados sobre UN SOLO RowingSimulator, así el pulso del sensor
    // sale de la física del remo (AC-APP-01-02, AC-BLE-13-03, AC-HRM-15-09). No existe "remo simulado + pulsómetro real" ni
    // al revés (Q-40). La capa del simulador (panel) lo reconoce por el tipo.
    // legacy: lib/main.dart:51-54
    public sealed class SimulatorDeviceBackend : DeviceBackend
    {
        public SimulatorDeviceBackend(RowingSimulator simulator, SimulatedRowerService rower, SimulatedHeartRateService heartRate)
            : base(rower, heartRate, null)
        {
            Simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            SimRower = rower;
            SimHeartRate = heartRate;
            if (!ReferenceEquals(rower.Simulator, simulator) || !ReferenceEquals(heartRate.Simulator, simulator))
            {
                throw new ArgumentException("SimulatorDeviceBackend: el remo y el pulsómetro tienen que compartir el mismo RowingSimulator.");
            }
        }

        public RowingSimulator Simulator { get; }

        public SimulatedRowerService SimRower { get; }

        public SimulatedHeartRateService SimHeartRate { get; }
    }

    public static class SimulatorBackend
    {
        // noise = false: el modo de prueba de la It. 1 (AC-BLE-14-06), para tests y capturas; la app usa noise = true.
        // random: el azar del ruido (AC-BASE-07); sin él, el del sistema.
        public static SimulatorDeviceBackend Create(IScheduler scheduler, IAppLogger logger, bool noise, IRandom? random = null)
        {
            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var simulator = new RowingSimulator(random, noise);
            var rower = new SimulatedRowerService(simulator, scheduler, logger);
            var heartRate = new SimulatedHeartRateService(simulator, scheduler, logger);
            return new SimulatorDeviceBackend(simulator, rower, heartRate);
        }
    }
}
