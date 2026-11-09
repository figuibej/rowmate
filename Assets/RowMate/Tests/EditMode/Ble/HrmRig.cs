using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // El pulsómetro real sobre el adaptador falso con el reloj de prueba (11-ble-pulsometro/02-tech-design §6.2): FakeScheduler,
    // FakeBleAdapter con los equipos K-11, MemoryPreferences (sembrable) y HeartRateService con los tiempos del legacy. Graba los
    // estados, los pulsos, el enlace y una línea de tiempo común ("bpm:0", "status:Disconnected") para comprobar el orden. Opcional:
    // el remo real sobre el MISMO adaptador (HSX) y el HeartRateStore de la app. At(t) avanza hasta el segundo t desde el inicio.
    internal sealed class HrmRig : IDisposable
    {
        private readonly InlineContinuations _inline = new InlineContinuations();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private RowerService? _rower;

        public HrmRig(bool filtersScanInOs = false, BleAdapterState initial = BleAdapterState.On, IEnumerable<KeyValuePair<string, object>>? seed = null,
                      MemoryPreferences? preferences = null)
        {
            Logger = new RecordingLogger();
            Scheduler = new FakeScheduler();
            Lifecycle = new FakeAppLifecycle(Logger);
            Adapter = new FakeBleAdapter(Scheduler, Logger, filtersScanInOs, false, initial, FakeBleDevices.K11);
            Preferences = preferences ?? (seed != null ? new MemoryPreferences(seed) : new MemoryPreferences());
            Hrm = new HeartRateService(Adapter, Preferences, Scheduler, Logger);
            Statuses = new StateRecorder<HrmStatus>(Hrm.Status);
            Bpms = new StateRecorder<int>(Hrm.Bpm);
            Links = new StateRecorder<HrmLink>(Hrm.Link);
            Lists = new StateRecorder<IReadOnlyList<BleScanResult>>(Hrm.Devices);
            _subscriptions.Add(Hrm.Status.Subscribe(s => Timeline.Add("status:" + s), false));
            _subscriptions.Add(Hrm.Bpm.Subscribe(b => Timeline.Add("bpm:" + b)));
        }

        public RecordingLogger Logger { get; }

        public FakeScheduler Scheduler { get; }

        public FakeAppLifecycle Lifecycle { get; }

        public FakeBleAdapter Adapter { get; }

        public MemoryPreferences Preferences { get; }

        public HeartRateService Hrm { get; }

        public StateRecorder<HrmStatus> Statuses { get; }

        public StateRecorder<int> Bpms { get; }

        public StateRecorder<HrmLink> Links { get; }

        public StateRecorder<IReadOnlyList<BleScanResult>> Lists { get; }

        public List<string> Timeline { get; } = new List<string>();

        public HrmStatus Status => Hrm.Status.Value;

        public HrmLink Link => Hrm.Link.Value;

        public double Now => Scheduler.MonotonicSeconds;

        // El remo real sobre el mismo adaptador (se crea la primera vez).
        public RowerService Rower
        {
            get
            {
                if (_rower == null)
                {
                    _rower = new RowerService(Adapter, Scheduler, Lifecycle, Logger);
                    _rower.Start();
                }

                return _rower;
            }
        }

        // Memoria sembrada como la dejaría una ejecución anterior.
        public static KeyValuePair<string, object>[] Remembered(FakeBleDevice device, string? name = null)
        {
            var pairs = new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>(HeartRateService.PrefDeviceId, device.Id) };
            var stored = name ?? device.Name;
            if (stored != null)
            {
                pairs.Add(new KeyValuePair<string, object>(HeartRateService.PrefDeviceName, stored));
            }

            return pairs.ToArray();
        }

        public string? StoredId => Preferences.TryGetString(HeartRateService.PrefDeviceId, out var id) ? id : null;

        public string? StoredName => Preferences.TryGetString(HeartRateService.PrefDeviceName, out var name) ? name : null;

        public void At(double seconds)
        {
            var target = (long)(seconds * TimeSpan.TicksPerSecond + 0.5);
            var delta = target - Scheduler.Elapsed.Ticks;
            if (delta < 0)
            {
                throw new InvalidOperationException("HrmRig: el tiempo no retrocede (" + seconds + ").");
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
            var scan = Hrm.StartScanAsync();
            foreach (var device in devices)
            {
                Adapter.Advertise(device);
            }

            return scan;
        }

        // Busca, ve el equipo y lo toca (conexión manual).
        public Task Connect(FakeBleDevice? device = null)
        {
            var d = device ?? FakeBleDevices.Hrm;
            Scan(d);
            return Hrm.ConnectAsync(d.Id);
        }

        // Conecta K-HRM y manda una lectura de 142.
        public void Connected142(FakeBleDevice? device = null)
        {
            var d = device ?? FakeBleDevices.Hrm;
            Connect(d);
            Adapter.Notify(d.Id, HeartRateParser.Measurement, HrPackets.Contact142);
        }

        public void Measure(byte[] packet, FakeBleDevice? device = null)
        {
            Adapter.Notify((device ?? FakeBleDevices.Hrm).Id, HeartRateParser.Measurement, packet);
        }

        public HeartRateStore Store()
        {
            return new HeartRateStore(Hrm, Logger);
        }

        public int HrmLines(string text)
        {
            var count = 0;
            foreach (var line in Logger.Lines)
            {
                if (line.StartsWith("[HRM]", StringComparison.Ordinal) && line.Contains(text))
                {
                    count++;
                }
            }

            return count;
        }

        // Los estados sin repeticiones seguidas (el servicio reemite como el legacy).
        public IReadOnlyList<HrmStatus> DistinctStatuses()
        {
            var list = new List<HrmStatus>();
            foreach (var s in Statuses.Values)
            {
                if (list.Count == 0 || list[list.Count - 1] != s)
                {
                    list.Add(s);
                }
            }

            return list;
        }

        public void Dispose()
        {
            foreach (var s in _subscriptions)
            {
                s.Dispose();
            }

            Statuses.Dispose();
            Bpms.Dispose();
            Links.Dispose();
            Lists.Dispose();
            Hrm.Dispose();
            _rower?.Dispose();
            _inline.Dispose();
        }
    }
}
