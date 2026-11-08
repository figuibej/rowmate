using System;
using System.Collections.Generic;
using System.Text;

namespace RowMate.Core.Ble.Native
{
    // Lo que sale de un registro del formato v1 (BleWire). Las horas son las del reloj NATIVO (QPC / System.nanoTime, DT10-18):
    // quien recibe las convierte con BleEventClock. Los byte[] y string son de cada evento real (las únicas asignaciones).
    public interface IBleEventSink
    {
        void OnAdapterState(double t, int state);

        void OnScanResult(double t, string id, string? name, int rssi, BleUuid[] services);

        void OnScanFailed(double t, int code, string? message);

        void OnConnection(double t, string id, bool connected, int code);

        // rest: lo que sigue a reqId y status (los servicios del descubrimiento o el mensaje de error; ver BleEventReader.Read*).
        void OnRequestDone(double t, int reqId, int status, ReadOnlySpan<byte> rest);

        void OnNotify(double t, string id, BleUuid characteristic, byte[] value);

        void OnDropped(double t, int count);
    }

    // Decodificador del formato v1 (10a 02-tech-design §2.6). Lee registros enteros; un tipo desconocido se saltea por su largo
    // (compatible hacia adelante dentro de la v1). Un registro truncado o con un campo fuera de su largo es un error del plugin:
    // FormatException con el índice, y lo leído antes ya se entregó.
    public static class BleEventReader
    {
        // Devuelve cuántos registros entregó.
        public static int Read(byte[] buffer, int length, IBleEventSink sink)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            if (length < 0 || length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var span = new ReadOnlySpan<byte>(buffer, 0, length);
            var offset = 0;
            var count = 0;
            while (offset < length)
            {
                if (length - offset < BleWire.HeaderSize)
                {
                    throw new FormatException("BleEventReader: cabecera truncada en el registro " + count + ".");
                }

                var kind = span[offset];
                var payloadLength = span[offset + 2] | (span[offset + 3] << 8);
                var t = BitConverter.Int64BitsToDouble(ReadI64(span, offset + 4));
                var start = offset + BleWire.HeaderSize;
                if (length - start < payloadLength)
                {
                    throw new FormatException("BleEventReader: registro " + count + " truncado.");
                }

                var payload = span.Slice(start, payloadLength);
                Dispatch(kind, t, payload, sink, count);
                offset = start + payloadLength;
                count++;
            }

            return count;
        }

        // El mensaje de un RequestDone con error (o null).
        public static string? ReadErrorMessage(ReadOnlySpan<byte> rest)
        {
            var at = 0;
            return rest.Length >= 2 ? ReadStr(rest, ref at) : null;
        }

        // Los servicios de un RequestDone de descubrimiento bien.
        public static IReadOnlyList<BleGattService> ReadServices(ReadOnlySpan<byte> rest)
        {
            var at = 0;
            var count = ReadU8(rest, ref at);
            var services = new List<BleGattService>(count);
            for (var s = 0; s < count; s++)
            {
                var uuid = ReadUuid(rest, ref at);
                var nChr = ReadU8(rest, ref at);
                var characteristics = new BleCharacteristic[nChr];
                for (var c = 0; c < nChr; c++)
                {
                    var chr = ReadUuid(rest, ref at);
                    characteristics[c] = new BleCharacteristic(chr, (BleCharacteristicProperties)ReadU8(rest, ref at));
                }

                services.Add(new BleGattService(uuid, characteristics));
            }

            return services;
        }

        private static void Dispatch(byte kind, double t, ReadOnlySpan<byte> p, IBleEventSink sink, int index)
        {
            var at = 0;
            try
            {
                switch (kind)
                {
                    case BleWire.KindAdapterState:
                        sink.OnAdapterState(t, ReadI32(p, ref at));
                        break;
                    case BleWire.KindScanResult:
                    {
                        var id = ReadStr(p, ref at) ?? "";
                        var name = ReadStr(p, ref at);
                        var rssi = (short)(p[at] | (p[at + 1] << 8));
                        at += 2;
                        var n = ReadU8(p, ref at);
                        var uuids = new BleUuid[n];
                        for (var i = 0; i < n; i++)
                        {
                            uuids[i] = ReadUuid(p, ref at);
                        }

                        sink.OnScanResult(t, id, name, rssi, uuids);
                        break;
                    }

                    case BleWire.KindScanFailed:
                    {
                        var code = ReadI32(p, ref at);
                        sink.OnScanFailed(t, code, ReadStr(p, ref at));
                        break;
                    }

                    case BleWire.KindConnection:
                    {
                        var id = ReadStr(p, ref at) ?? "";
                        var connected = ReadU8(p, ref at) != 0;
                        sink.OnConnection(t, id, connected, ReadI32(p, ref at));
                        break;
                    }

                    case BleWire.KindRequestDone:
                    {
                        var reqId = ReadI32(p, ref at);
                        var status = ReadI32(p, ref at);
                        sink.OnRequestDone(t, reqId, status, p.Slice(at));
                        break;
                    }

                    case BleWire.KindNotify:
                    {
                        var id = ReadStr(p, ref at) ?? "";
                        var chr = ReadUuid(p, ref at);
                        var n = p[at] | (p[at + 1] << 8);
                        at += 2;
                        if (p.Length - at < n)
                        {
                            throw new IndexOutOfRangeException();
                        }

                        sink.OnNotify(t, id, chr, p.Slice(at, n).ToArray());
                        break;
                    }

                    case BleWire.KindDropped:
                        sink.OnDropped(t, ReadI32(p, ref at));
                        break;
                    default:
                        // Tipo desconocido: se saltea por su largo.
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new FormatException("BleEventReader: registro " + index + " (tipo " + kind + ") con un campo fuera de su largo.");
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new FormatException("BleEventReader: registro " + index + " (tipo " + kind + ") con un campo fuera de su largo.");
            }
        }

        private static long ReadI64(ReadOnlySpan<byte> s, int at)
        {
            long value = 0;
            for (var i = 7; i >= 0; i--)
            {
                value = (value << 8) | s[at + i];
            }

            return value;
        }

        private static int ReadU8(ReadOnlySpan<byte> s, ref int at)
        {
            return s[at++];
        }

        private static int ReadI32(ReadOnlySpan<byte> s, ref int at)
        {
            var value = s[at] | (s[at + 1] << 8) | (s[at + 2] << 16) | (s[at + 3] << 24);
            at += 4;
            return value;
        }

        private static string? ReadStr(ReadOnlySpan<byte> s, ref int at)
        {
            var n = s[at] | (s[at + 1] << 8);
            at += 2;
            if (n == 0)
            {
                return null;
            }

            if (s.Length - at < n)
            {
                throw new IndexOutOfRangeException();
            }

            var text = Encoding.UTF8.GetString(s.Slice(at, n));
            at += n;
            return text;
        }

        private static BleUuid ReadUuid(ReadOnlySpan<byte> s, ref int at)
        {
            if (s.Length - at < 16)
            {
                throw new IndexOutOfRangeException();
            }

            var uuid = BleWire.ReadUuid(s.Slice(at, 16));
            at += 16;
            return uuid;
        }
    }
}
