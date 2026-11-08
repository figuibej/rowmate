using System;
using System.Collections.Generic;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;
using RowMate.Core.Common;

namespace RowMate.Tests.Support
{
    // El plugin nativo falso (10a 02-tech-design §8.2): escribe registros del formato v1 con el codificador de referencia
    // (BleWireWriter) en un búfer propio, con SU PROPIO reloj (nativeNow: el de la prueba más un desfase arbitrario), y lleva las
    // sesiones como el plugin: un Connect abre una (o reemplaza la del id), Disconnect la cierra y cancela un connect pendiente
    // (RequestDone con BleWire.StatusCancelled). Así se prueban el puente (NativeBleAdapter), las sesiones (LiveSessions ≤ 1) y el
    // reloj del alcance sin la DLL ni el teléfono. Nunca toca una radio.
    public sealed class FakeNativeBleApi : INativeBleApi
    {
        private readonly Func<double> _nativeNow;
        private readonly IScheduler _scheduler;
        private readonly BleWireWriter _writer = new BleWireWriter();
        private readonly List<byte[]> _ring = new List<byte[]>();
        private readonly Dictionary<string, FakeBleDevice> _devices = new Dictionary<string, FakeBleDevice>(StringComparer.Ordinal);
        private readonly Dictionary<string, FakeConnectBehavior> _behaviors = new Dictionary<string, FakeConnectBehavior>(StringComparer.Ordinal);
        private readonly Dictionary<string, TimeSpan> _delays = new Dictionary<string, TimeSpan>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _live = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _pendingConnect = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _disconnects = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _connects = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _stubborn = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, (double Start, double Period)> _keepAlive = new Dictionary<string, (double, double)>(StringComparer.Ordinal);
        private int _pendingBytes;
        private int _nextRequest;
        private int _keepAliveDone;

        public FakeNativeBleApi(Func<double> nativeNow, IScheduler scheduler, bool filtersScanInOs = false, bool needsNativeKeepAlive = false,
                                IEnumerable<FakeBleDevice>? devices = null)
        {
            _nativeNow = nativeNow ?? throw new ArgumentNullException(nameof(nativeNow));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            FiltersScanInOs = filtersScanInOs;
            NeedsNativeKeepAlive = needsNativeKeepAlive;
            foreach (var device in devices ?? FakeBleDevices.K10)
            {
                _devices[device.Id] = device;
            }
        }

        // El reloj del plugin (el de la prueba más el desfase).
        public double NativeNow => _nativeNow();

        public int ApiVersion { get; set; } = BleWire.ApiVersion;

        public int InitCode { get; set; }

        public int AdapterStateValue { get; set; } = (int)BleAdapterState.On;

        public int StartScanCode { get; set; }

        public int RequestEnableCode { get; set; }

        public bool FiltersScanInOs { get; }

        public bool NeedsNativeKeepAlive { get; }

        public int InitCalls { get; private set; }

        public int ShutdownCalls { get; private set; }

        public int ScanStarts { get; private set; }

        public int ScanStops { get; private set; }

        public string? LastScanCsv { get; private set; }

        public int RequestEnableCalls { get; private set; }

        public int PendingCalls { get; private set; }

        public int DrainCalls { get; private set; }

        public int Replaced { get; private set; }

        public List<(string Id, string Characteristic, byte[] Bytes, double Time)> Writes { get; } = new List<(string, string, byte[], double)>();

        public int KeepAliveStarts { get; private set; }

        public int KeepAliveStops { get; private set; }

        // Escrituras del keepalive nativo hasta ahora (las terminadas y la que sigue corriendo), en el reloj nativo.
        public int NativeKeepAliveWrites
        {
            get
            {
                var running = 0;
                foreach (var entry in _keepAlive)
                {
                    running += (int)Math.Floor((NativeNow - entry.Value.Start) / entry.Value.Period + 1e-9);
                }

                return _keepAliveDone + running;
            }
        }

        public int LiveSessions(string id)
        {
            return _live.TryGetValue(id, out var n) ? n : 0;
        }

        public int Disconnects(string id)
        {
            return _disconnects.TryGetValue(id, out var n) ? n : 0;
        }

        public int Connects(string id)
        {
            return _connects.TryGetValue(id, out var n) ? n : 0;
        }

        // ── Guion de conexiones ──

        public void Respond(string id)
        {
            _behaviors[id] = FakeConnectBehavior.Respond;
        }

        public void NeverRespond(string id)
        {
            _behaviors[id] = FakeConnectBehavior.NeverRespond;
        }

        public void RespondAfter(string id, TimeSpan delay, bool ignoreDisconnect = false)
        {
            _behaviors[id] = FakeConnectBehavior.RespondAfter;
            _delays[id] = delay;
            if (ignoreDisconnect)
            {
                _stubborn.Add(id);
            }
            else
            {
                _stubborn.Remove(id);
            }
        }

        // ── Eventos a mano (con la hora nativa que se quiera) ──

        public void EnqueueAtNative(double t, Action<BleWireWriter, double> record)
        {
            _writer.Clear();
            record(_writer, t);
            Push(_writer.ToArray());
        }

        public void AdapterStateNow(BleAdapterState state)
        {
            AdapterStateValue = (int)state;
            EnqueueAtNative(NativeNow, (w, t) => w.AdapterState(t, (int)state));
        }

        public void AdvertiseNow(FakeBleDevice device)
        {
            EnqueueAtNative(NativeNow, (w, t) => w.ScanResult(t, device.Id, device.Name, device.Rssi, device.Advertised));
        }

        public void ScanFailedNow(int code, string message)
        {
            EnqueueAtNative(NativeNow, (w, t) => w.ScanFailed(t, code, message));
        }

        public void NotifyAt(double t, string id, BleUuid characteristic, byte[] bytes)
        {
            EnqueueAtNative(t, (w, at) => w.Notify(at, id, characteristic, bytes));
        }

        public void NotifyNow(string id, BleUuid characteristic, byte[] bytes)
        {
            NotifyAt(NativeNow, id, characteristic, bytes);
        }

        // La caída no pedida del enlace (el plugin sigue con la sesión abierta hasta que C# la cierre).
        public void DropLinkNow(string id, int code = 8)
        {
            EnqueueAtNative(NativeNow, (w, t) => w.Connection(t, id, false, code));
        }

        // ── INativeBleApi ──

        public int Init()
        {
            InitCalls++;
            return InitCode;
        }

        public void Shutdown()
        {
            ShutdownCalls++;
        }

        public double MonotonicSeconds()
        {
            return NativeNow;
        }

        public int AdapterState()
        {
            return AdapterStateValue;
        }

        public int StartScan(string servicesCsv)
        {
            ScanStarts++;
            LastScanCsv = servicesCsv;
            return StartScanCode;
        }

        public int StopScan()
        {
            ScanStops++;
            return 0;
        }

        public int Connect(string id, bool highMtu)
        {
            var reqId = ++_nextRequest;
            _connects[id] = Connects(id) + 1;
            if (LiveSessions(id) > 0)
            {
                // DT10-17: un connect sobre un id con sesión primero la cierra.
                Replaced++;
                CancelPendingConnect(id);
            }

            _live[id] = 1;
            _pendingConnect[id] = reqId;
            if (!_devices.TryGetValue(id, out _))
            {
                Finish(id, reqId, -1, "device not found");
                return reqId;
            }

            _behaviors.TryGetValue(id, out var behavior);
            switch (behavior)
            {
                case FakeConnectBehavior.Respond:
                    Succeed(id, reqId);
                    break;
                case FakeConnectBehavior.RespondAfter:
                    _scheduler.Schedule(_delays[id], () =>
                    {
                        var stubborn = _stubborn.Contains(id);
                        if (!stubborn && (!_pendingConnect.TryGetValue(id, out var current) || current != reqId))
                        {
                            return; // cancelado: su RequestDone ya salió
                        }

                        _live[id] = 1;
                        Succeed(id, reqId);
                    });
                    break;
            }

            return reqId;
        }

        public int Disconnect(string id)
        {
            _disconnects[id] = Disconnects(id) + 1;
            CancelPendingConnect(id);
            _live[id] = 0;
            StopKeepAliveOf(id);
            return 0;
        }

        public int Discover(string id)
        {
            var reqId = ++_nextRequest;
            if (LiveSessions(id) == 0 || !_devices.TryGetValue(id, out var device))
            {
                Finish(id, reqId, BleWire.ErrorNotConnected, "not connected");
                return reqId;
            }

            EnqueueAtNative(NativeNow, (w, t) => w.DiscoverDone(t, reqId, device.Services));
            return reqId;
        }

        public int SetNotify(string id, string service, string characteristic, bool enable)
        {
            var reqId = ++_nextRequest;
            Finish(id, reqId, LiveSessions(id) > 0 ? 0 : BleWire.ErrorNotConnected, "not connected");
            return reqId;
        }

        public int Write(string id, string service, string characteristic, byte[] data, int length, bool withResponse)
        {
            var reqId = ++_nextRequest;
            var copy = new byte[length];
            Buffer.BlockCopy(data, 0, copy, 0, length);
            Writes.Add((id, characteristic, copy, NativeNow));
            Finish(id, reqId, LiveSessions(id) > 0 ? 0 : BleWire.ErrorNotConnected, "not connected");
            return reqId;
        }

        public int KeepAliveStart(string id, string service, string characteristic, byte[] data, int length, int periodMs)
        {
            KeepAliveStarts++;
            StopKeepAliveOf(id);
            _keepAlive[id] = (NativeNow, periodMs / 1000.0);
            return 0;
        }

        public int KeepAliveStop(string id)
        {
            KeepAliveStops++;
            StopKeepAliveOf(id);
            return 0;
        }

        public int Pending()
        {
            PendingCalls++;
            return _pendingBytes;
        }

        public int Drain(byte[] buffer, int capacity)
        {
            DrainCalls++;
            var written = 0;
            var taken = 0;
            while (taken < _ring.Count && written + _ring[taken].Length <= capacity)
            {
                Buffer.BlockCopy(_ring[taken], 0, buffer, written, _ring[taken].Length);
                written += _ring[taken].Length;
                taken++;
            }

            _ring.RemoveRange(0, taken);
            _pendingBytes -= written;
            return written;
        }

        public int RequestEnable()
        {
            RequestEnableCalls++;
            return RequestEnableCode;
        }

        private void Succeed(string id, int reqId)
        {
            _pendingConnect.Remove(id);
            EnqueueAtNative(NativeNow, (w, t) => w.Connection(t, id, true, 0));
            EnqueueAtNative(NativeNow, (w, t) => w.RequestDone(t, reqId, 0));
        }

        private void Finish(string id, int reqId, int status, string message)
        {
            if (status != 0 && _pendingConnect.TryGetValue(id, out var connect) && connect == reqId)
            {
                _pendingConnect.Remove(id);
                _live[id] = 0;
            }

            EnqueueAtNative(NativeNow, (w, t) => w.RequestDone(t, reqId, status, message));
        }

        private void CancelPendingConnect(string id)
        {
            if (_pendingConnect.TryGetValue(id, out var reqId))
            {
                _pendingConnect.Remove(id);
                if (!_stubborn.Contains(id))
                {
                    EnqueueAtNative(NativeNow, (w, t) => w.RequestDone(t, reqId, BleWire.StatusCancelled, "cancelled"));
                }
            }
        }

        private void StopKeepAliveOf(string id)
        {
            if (_keepAlive.TryGetValue(id, out var running))
            {
                _keepAliveDone += (int)Math.Floor((NativeNow - running.Start) / running.Period + 1e-9);
                _keepAlive.Remove(id);
            }
        }

        private void Push(byte[] record)
        {
            _ring.Add(record);
            _pendingBytes += record.Length;
        }
    }
}
