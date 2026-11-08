using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Ble.Native;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // BER y BEC: el formato binario v1 de los eventos nativos (10a 02-tech-design §2.6) contra su fijación dorada
    // (Tests/Golden/ble/events-v1.bin, que también escriben la DLL y Kotlin), el lector y el reloj de los eventos (§5.2).
    [TestFixture]
    public sealed class BleWireTests
    {
        private sealed class FixtureSink : IBleEventSink
        {
            public List<string> Lines { get; } = new List<string>();

            public IReadOnlyList<BleGattService>? Services { get; private set; }

            public string? Error { get; private set; }

            public void OnAdapterState(double t, int state)
            {
                Lines.Add("AdapterState t=" + T(t) + " state=" + state);
            }

            public void OnScanResult(double t, string id, string? name, int rssi, BleUuid[] services)
            {
                var uuids = new string[services.Length];
                for (var i = 0; i < services.Length; i++)
                {
                    uuids[i] = services[i].ToString();
                }

                Lines.Add("ScanResult t=" + T(t) + " id=" + id + " name=" + (name ?? "(null)") + " rssi=" + rssi + " services=" + string.Join(",", uuids));
            }

            public void OnScanFailed(double t, int code, string? message)
            {
                Lines.Add("ScanFailed t=" + T(t) + " code=" + code + " message=" + message);
            }

            public void OnConnection(double t, string id, bool connected, int code)
            {
                Lines.Add("Connection t=" + T(t) + " id=" + id + " connected=" + connected + " code=" + code);
            }

            public void OnRequestDone(double t, int reqId, int status, ReadOnlySpan<byte> rest)
            {
                Lines.Add("RequestDone t=" + T(t) + " reqId=" + reqId + " status=" + status + " rest=" + rest.Length);
                if (reqId == 2)
                {
                    Services = BleEventReader.ReadServices(rest);
                }

                if (status != 0)
                {
                    Error = BleEventReader.ReadErrorMessage(rest);
                }
            }

            public void OnNotify(double t, string id, BleUuid characteristic, byte[] value)
            {
                Lines.Add("Notify t=" + T(t) + " id=" + id + " chr=" + characteristic + " value=" + BleHex.Format(value));
            }

            public void OnDropped(double t, int count)
            {
                Lines.Add("Dropped t=" + T(t) + " count=" + count);
            }

            private static string T(double t)
            {
                return t.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string GoldenPath => RepoPaths.Combine(BleWireFixture.RelativePath);

        [Test]
        public void GoldenMatchesTheEncoder()
        {
            Assert.That(File.Exists(GoldenPath), Is.True, BleWireFixture.RelativePath + " (regenerar con RegenerateGolden)");
            Assert.That(File.ReadAllBytes(GoldenPath), Is.EqualTo(BleWireFixture.Encode()), "el formato v1 no cambia sin cambiar la versión");
        }

        // Solo a mano: dotnet test --filter Name=RegenerateGolden (escribe el archivo del repo).
        [Test, Explicit("escribe Tests/Golden/ble/events-v1.bin")]
        public void RegenerateGolden()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GoldenPath)!);
            File.WriteAllBytes(GoldenPath, BleWireFixture.Encode());
        }

        [Test]
        public void ReaderDecodesTheFixture()
        {
            var bytes = File.ReadAllBytes(GoldenPath);
            var sink = new FixtureSink();
            Assert.That(BleEventReader.Read(bytes, bytes.Length, sink), Is.EqualTo(11));
            Assert.That(sink.Lines, Is.EqualTo(BleWireFixture.Expected));
            Assert.That(sink.Services!.Count, Is.EqualTo(1));
            Assert.That(sink.Services[0].Characteristics[1].Uuid, Is.EqualTo(FtmsUuids.ControlPoint));
            Assert.That(sink.Services[0].Characteristics[1].Properties, Is.EqualTo(BleCharacteristicProperties.Write | BleCharacteristicProperties.Indicate));
            Assert.That(sink.Error, Is.EqualTo("GATT error 133"));
        }

        [Test]
        public void HeaderLayoutIsLittleEndian()
        {
            // u8 kind | u8 flags | u16 payloadLength | f64 t (1,5 = 0x3FF8000000000000) | i32 state (5).
            var bytes = BleWireFixture.Encode();
            Assert.That(new[] { bytes[0], bytes[1], bytes[2], bytes[3] }, Is.EqualTo(new byte[] { 1, 0, 4, 0 }));
            Assert.That(new[] { bytes[4], bytes[5], bytes[6], bytes[7], bytes[8], bytes[9], bytes[10], bytes[11] },
                Is.EqualTo(new byte[] { 0, 0, 0, 0, 0, 0, 0xF8, 0x3F }));
            Assert.That(new[] { bytes[12], bytes[13], bytes[14], bytes[15] }, Is.EqualTo(new byte[] { 5, 0, 0, 0 }));
            // El UUID en el orden del texto: 00001826-… → 00 00 18 26 00 00 10 00 80 00 00 80 5f 9b 34 fb.
            var uuid = new byte[16];
            BleWire.WriteUuid(FtmsUuids.Service, uuid, 0);
            Assert.That(BleHex.Format(uuid), Is.EqualTo("00 00 18 26 00 00 10 00 80 00 00 80 5f 9b 34 fb"));
            Assert.That(BleWire.ReadUuid(uuid), Is.EqualTo(FtmsUuids.Service));
        }

        [Test]
        public void UnknownKindIsSkippedAndTruncationFails()
        {
            var good = BleWireFixture.Encode();
            var withUnknown = new byte[good.Length + 15];
            withUnknown[0] = 99;
            withUnknown[2] = 3;
            Buffer.BlockCopy(good, 0, withUnknown, 15, good.Length);
            var sink = new FixtureSink();
            Assert.That(BleEventReader.Read(withUnknown, withUnknown.Length, sink), Is.EqualTo(12), "el desconocido cuenta pero no se entrega");
            Assert.That(sink.Lines.Count, Is.EqualTo(11));

            var truncated = new byte[good.Length - 1];
            Buffer.BlockCopy(good, 0, truncated, 0, truncated.Length);
            var partial = new FixtureSink();
            Assert.Throws<FormatException>(() => BleEventReader.Read(truncated, truncated.Length, partial));
            Assert.That(partial.Lines.Count, Is.EqualTo(10), "lo anterior ya se entregó");
        }

        [Test]
        public void WriterRejectsOversizedRecords()
        {
            var w = new BleWireWriter();
            Assert.Throws<ArgumentException>(() => w.Notify(0, "x", FtmsUuids.RowerData, new byte[BleWire.MaxRecordBytes]));
            Assert.That(w.Length, Is.EqualTo(0), "no queda un registro a medias");
            Assert.That(BleWire.NativeRingBytes, Is.EqualTo(4 * 1024 * 1024), "QA-G10a-13: alcanza para 6 h a 2 Hz");
        }

        // ── BEC: el reloj de los eventos (QA-G10a-04) ──

        [Test, Category("AC-BLE-91-01")]
        public void MaxOfSamplesConvergesToOffset()
        {
            // Cada muestra (hora del cuadro anterior − nativo ahora) queda por debajo del desfase real; el máximo converge.
            var clock = new BleEventClock();
            Assert.That(clock.IsCalibrated, Is.False);
            const double real = -1000.25;
            clock.Calibrate(10.0 - 0.016, 10.0 - real);
            Assert.That(clock.Offset, Is.EqualTo(real - 0.016).Within(1e-9));
            clock.Calibrate(10.5 - 0.004, 10.5 - real);
            clock.Calibrate(11.0 - 0.030, 11.0 - real);
            Assert.That(clock.Offset, Is.EqualTo(real - 0.004).Within(1e-9), "el máximo, no el último");
            Assert.That(clock.ToApp(2000.0), Is.EqualTo(2000.0 + real - 0.004).Within(1e-9));
        }

        [Test, Category("AC-BLE-91-01")]
        public void ResetOnWoke()
        {
            // En un sueño del sistema el reloj despierto de la app descuenta lo dormido y el nativo no: el desfase cambia.
            var logger = new RecordingLogger();
            var scheduler = new FakeScheduler();
            var lifecycle = new FakeAppLifecycle(logger);
            var api = new FakeNativeBleApi(() => scheduler.MonotonicSeconds + 5, scheduler);
            var adapter = new NativeBleAdapter(api, null, scheduler, lifecycle, logger);
            adapter.Drain(false);
            Assert.That(adapter.IsClockCalibrated, Is.True);
            lifecycle.SignalWake(new RowMate.Core.Common.SystemWake());
            Assert.That(adapter.IsClockCalibrated, Is.False);
            adapter.Dispose();
            Assert.That(api.ShutdownCalls, Is.EqualTo(1));
        }
    }
}
