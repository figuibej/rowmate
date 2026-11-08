using System;
using System.Threading.Tasks;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // El remo real sobre el adaptador falso con el reloj de prueba (10a 02-tech-design §8.2): FakeScheduler (Advance corre todo en
    // orden y las continuaciones de los await van en línea), FakeAppLifecycle, FakeBleAdapter con los equipos K-10 y RowerService
    // con los tiempos del legacy. At(t) avanza hasta el segundo t desde el inicio. Opcional: el DeviceStore de la app.
    internal sealed class BleRig : IDisposable
    {
        private readonly InlineContinuations _inline = new InlineContinuations();

        public BleRig(bool filtersScanInOs = false, bool nativeKeepAlive = false, BleAdapterState initial = BleAdapterState.On)
        {
            Logger = new RecordingLogger();
            Scheduler = new FakeScheduler();
            Lifecycle = new FakeAppLifecycle(Logger);
            Adapter = new FakeBleAdapter(Scheduler, Logger, filtersScanInOs, nativeKeepAlive, initial);
            Rower = new RowerService(Adapter, Scheduler, Lifecycle, Logger);
            Statuses = new StateRecorder<BleStatus>(Rower.Status);
            Links = new StateRecorder<RowerLink>(Rower.Link);
            Rower.Start();
        }

        public RecordingLogger Logger { get; }

        public FakeScheduler Scheduler { get; }

        public FakeAppLifecycle Lifecycle { get; }

        public FakeBleAdapter Adapter { get; }

        public RowerService Rower { get; }

        public StateRecorder<BleStatus> Statuses { get; }

        public StateRecorder<RowerLink> Links { get; }

        public BleStatus Status => Rower.Status.Value;

        public RowerLink Link => Rower.Link.Value;

        public double Now => Scheduler.MonotonicSeconds;

        public void At(double seconds)
        {
            var target = (long)(seconds * TimeSpan.TicksPerSecond + 0.5);
            var delta = target - Scheduler.Elapsed.Ticks;
            if (delta < 0)
            {
                throw new InvalidOperationException("BleRig: el tiempo no retrocede (" + seconds + ").");
            }

            Scheduler.Advance(TimeSpan.FromTicks(delta));
        }

        public void Wait(double seconds)
        {
            Scheduler.Advance(TimeSpan.FromTicks((long)(seconds * TimeSpan.TicksPerSecond + 0.5)));
        }

        // Busca y anuncia los equipos en el acto; la búsqueda sigue hasta sus 10 s.
        public Task Scan(params FakeBleDevice[] devices)
        {
            var scan = Rower.StartScanAsync();
            foreach (var device in devices)
            {
                Adapter.Advertise(device);
            }

            return scan;
        }

        // Busca, ve el equipo y lo conecta (los 10 s de la búsqueda siguen corriendo, como en el legacy).
        public Task Connect(FakeBleDevice device)
        {
            Scan(device);
            return Rower.ConnectAsync(device.Id);
        }

        // Conecta K-ROW y deja pasar la toma de control (300 ms).
        public void Connected(FakeBleDevice? device = null)
        {
            var d = device ?? FakeBleDevices.Row;
            Connect(d);
            Wait(0.3);
        }

        public DeviceStore Store(IClock? clock = null, FakeHaptics? haptics = null)
        {
            return new DeviceStore(Rower, Logger, clock ?? Scheduler, haptics);
        }

        public int WritesOf(byte opcode)
        {
            var count = 0;
            foreach (var write in Adapter.Writes)
            {
                if (write.Bytes.Length == 1 && write.Bytes[0] == opcode)
                {
                    count++;
                }
            }

            return count;
        }

        public void Dispose()
        {
            Statuses.Dispose();
            Links.Dispose();
            Rower.Dispose();
            _inline.Dispose();
        }
    }
}
