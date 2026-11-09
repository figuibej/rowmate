using System;
using RowMate.App.Composition;
using RowMate.Bootstrap;
using RowMate.Core.Ble;
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

    // It. 10a: el backend de HostBackends.FakeBle, con el adaptador falso a mano (UiTestHost.FakeBle).
    public sealed class FakeBleBackend : DeviceBackend
    {
        public FakeBleBackend(FakeBleAdapter adapter, RowerService rower, FakeAppLifecycle lifecycle, IHeartRateService heartRate)
            : base(rower, heartRate, adapter)
        {
            Adapter = adapter;
            RealRower = rower;
            Lifecycle = lifecycle;
        }

        public FakeBleAdapter Adapter { get; }

        public RowerService RealRower { get; }

        // It. 11: el pulsómetro real (null con el de prueba).
        public HeartRateService? RealHeartRate => HeartRate as HeartRateService;

        public FakeAppLifecycle Lifecycle { get; }
    }

    public static class HostBackends
    {
        // "Prueba": ControllableRowerService + ScriptedHeartRateService (spec §0.4).
        public static readonly HostBackendFactory TestDevices = (scheduler, logger) =>
            new DeviceBackend(new ControllableRowerService(logger), new ScriptedHeartRateService(logger), null);

        // Build normal de la It. 2 (equipo sin BLE): HardwareBackend (CAP-22).
        public static readonly HostBackendFactory Unavailable = (scheduler, logger) => HardwareBackend.Unavailable(logger);

        // It. 10a (02-tech-design §8.2, §8.4): el remo REAL (RowerService, Core) sobre el adaptador falso (FakeBleAdapter) con los equipos
        // ficticios K-10 y el reloj del host; el pulsómetro, el de prueba. setup prepara el adaptador antes de crear el servicio (estado
        // inicial, permiso, conducta de cada equipo). Ninguna toma toca la radio (ROWMATE_BLE=off).
        // It. 11 (02-tech-design §1.2, §7): con hrmPreferences, también el pulsómetro REAL (HeartRateService, Core) sobre el MISMO adaptador
        // falso (K-11) y esas preferencias (MemoryPreferences sembradas por el guion; la prueba pasa la misma instancia en
        // UiHostOptions.Preferences, así la app y el servicio ven lo mismo). Sin ellas, el pulsómetro de prueba de la 10a.
        public static HostBackendFactory FakeBle(Action<FakeBleAdapter>? setup = null, BleAdapterState initial = BleAdapterState.On,
                                                 RowMate.Core.Ports.IPreferences? hrmPreferences = null)
        {
            return (scheduler, logger) =>
            {
                var adapter = new FakeBleAdapter(scheduler, logger, false, false, initial);
                setup?.Invoke(adapter);
                var lifecycle = new FakeAppLifecycle(logger);
                var rower = new RowerService(adapter, scheduler, lifecycle, logger);
                IHeartRateService heartRate = hrmPreferences != null
                    ? new HeartRateService(adapter, hrmPreferences, scheduler, logger)
                    : new ScriptedHeartRateService(logger);
                return new FakeBleBackend(adapter, rower, lifecycle, heartRate);
            };
        }

        // It. 11: el remo y el pulsómetro reales sobre el adaptador falso; options.Preferences = preferences (las mismas del backend).
        public static UiHostOptions WithRealHrm(this UiHostOptions options, RowMate.Core.Common.MemoryPreferences preferences,
                                                Action<FakeBleAdapter>? setup = null, BleAdapterState initial = BleAdapterState.On)
        {
            options.Preferences = preferences;
            options.Backend = FakeBle(setup, initial, preferences);
            return options;
        }

        // El remo de prueba de la It. 4a (spec §0.4; 02-tech-design §4.5, §7.1): L(k) cada segundo por un timer del reloj del
        // host (ScriptedRower), conectado, y el pulsómetro de prueba (bpm y estados a pedido).
        public static HostBackendFactory TestRower(Func<int, RowMate.Core.Models.RowingData> reading)
        {
            if (reading == null)
            {
                throw new ArgumentNullException(nameof(reading));
            }

            // Start() en la creación del host (instante 0): L(k) llega en el instante k, como las lecturas del simulador.
            return (scheduler, logger) =>
            {
                var rower = new ScriptedRower(scheduler, reading, logger);
                rower.Start();
                return new DeviceBackend(rower, new ScriptedHeartRateService(logger), null);
            };
        }

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
