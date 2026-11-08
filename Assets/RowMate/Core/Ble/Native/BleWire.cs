using System;
using System.Collections.Generic;
using System.Text;

namespace RowMate.Core.Ble.Native
{
    // Formato binario v1 de los eventos nativos (10a 02-tech-design §2.6; ADR 009): lo escriben IGUAL la DLL de Windows (C++) y
    // el plugin de Android (Kotlin), y lo lee BleEventReader. Fijado byte por byte en Tests/Golden/ble/events-v1.bin (+ .md).
    // Little endian. Cada registro:
    //   u8 kind | u8 flags (0) | u16 payloadLength | f64 monotonicSeconds | payload
    //   str  = u16 n + n bytes UTF-8 (n = 0 → null)
    //   uuid = 16 bytes en el orden canónico del texto (los 128 bits de izquierda a derecha)
    //   1 AdapterState  i32 state (orden de BleAdapterState)
    //   2 ScanResult    str id | str name | i16 rssi | u8 n | n × uuid
    //   3 ScanFailed    i32 code | str message
    //   4 Connection    str id | u8 connected | i32 code
    //   5 RequestDone   i32 reqId | i32 status (0 bien) | Discover bien: u8 nSvc {uuid | u8 nChr {uuid | u8 props}} ; error: str message
    //   6 Notify        str id | uuid characteristic | u16 n | n bytes
    //   7 Dropped       i32 count (el buffer nativo descartó los más viejos)
    public static class BleWire
    {
        public const int ApiVersion = 1;
        public const int HeaderSize = 12;

        // Tope de un registro (cabecera incluida) y del búfer de drenaje que reusa NativeBleAdapter.
        public const int MaxRecordBytes = 65536;

        // Búfer nativo: 4 MiB. Un Notify de Rower Data ocupa ≈ 70 B y el RequestDone de cada escritura ≈ 20 B: a 2 Hz con el
        // keepalive son ≈ 3 MB en 6 h (QA-G10a-13: los 2 MB del diseño alcanzaban para ≈ 3,5-4 h).
        public const int NativeRingBytes = 4 * 1024 * 1024;

        public const byte KindAdapterState = 1;
        public const byte KindScanResult = 2;
        public const byte KindScanFailed = 3;
        public const byte KindConnection = 4;
        public const byte KindRequestDone = 5;
        public const byte KindNotify = 6;
        public const byte KindDropped = 7;

        // Códigos comunes de las funciones de la API (rm_ble_* / RmBle.*): 0 bien; negativos, errores.
        public const int Ok = 0;
        public const int ErrorGeneric = -1;
        public const int ErrorPermission = BlePermissionDeniedException.PermissionCode;
        public const int ErrorUnsupported = -3;
        public const int ErrorNotConnected = -4;

        // Estado de RequestDone de un pedido cancelado (disconnect durante un connect pendiente, DT10-17).
        public const int StatusCancelled = -5;

        public static void WriteUuid(BleUuid uuid, byte[] destination, int offset)
        {
            for (var i = 0; i < 8; i++)
            {
                destination[offset + i] = (byte)(uuid.High >> (56 - 8 * i));
                destination[offset + 8 + i] = (byte)(uuid.Low >> (56 - 8 * i));
            }
        }

        public static BleUuid ReadUuid(ReadOnlySpan<byte> source)
        {
            ulong high = 0;
            ulong low = 0;
            for (var i = 0; i < 8; i++)
            {
                high = (high << 8) | source[i];
                low = (low << 8) | source[8 + i];
            }

            return new BleUuid(high, low);
        }
    }

    // El codificador de referencia del formato v1 (C#): lo usan FakeNativeBleApi y la fijación dorada. La DLL y Kotlin tienen el
    // suyo y escriben los mismos bytes (WindowsDllContractTests, native/android/check.ps1 -Golden).
    public sealed class BleWireWriter
    {
        private byte[] _buffer;
        private int _length;
        private int _recordStart;

        public BleWireWriter(int capacity = 1024)
        {
            _buffer = new byte[Math.Max(64, capacity)];
        }

        public int Length => _length;

        public byte[] ToArray()
        {
            var copy = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, copy, 0, _length);
            return copy;
        }

        public void Clear()
        {
            _length = 0;
        }

        public void AdapterState(double t, int state)
        {
            Begin(BleWire.KindAdapterState, t);
            I32(state);
            End();
        }

        public void ScanResult(double t, string id, string? name, int rssi, IReadOnlyList<BleUuid> services)
        {
            Begin(BleWire.KindScanResult, t);
            Str(id);
            Str(name);
            I16(rssi);
            U8(services.Count);
            for (var i = 0; i < services.Count; i++)
            {
                Uuid(services[i]);
            }

            End();
        }

        public void ScanFailed(double t, int code, string? message)
        {
            Begin(BleWire.KindScanFailed, t);
            I32(code);
            Str(message);
            End();
        }

        public void Connection(double t, string id, bool connected, int code)
        {
            Begin(BleWire.KindConnection, t);
            Str(id);
            U8(connected ? 1 : 0);
            I32(code);
            End();
        }

        // Un pedido terminado: status 0 sin más datos (connect, setNotify, write) o con error y su mensaje.
        public void RequestDone(double t, int reqId, int status, string? errorMessage = null)
        {
            Begin(BleWire.KindRequestDone, t);
            I32(reqId);
            I32(status);
            if (status != 0)
            {
                Str(errorMessage);
            }

            End();
        }

        // El descubrimiento bien: los servicios y sus características.
        public void DiscoverDone(double t, int reqId, IReadOnlyList<BleGattService> services)
        {
            Begin(BleWire.KindRequestDone, t);
            I32(reqId);
            I32(0);
            U8(services.Count);
            for (var s = 0; s < services.Count; s++)
            {
                Uuid(services[s].Uuid);
                var characteristics = services[s].Characteristics;
                U8(characteristics.Count);
                for (var c = 0; c < characteristics.Count; c++)
                {
                    Uuid(characteristics[c].Uuid);
                    U8((int)characteristics[c].Properties);
                }
            }

            End();
        }

        public void Notify(double t, string id, BleUuid characteristic, byte[] value)
        {
            Begin(BleWire.KindNotify, t);
            Str(id);
            Uuid(characteristic);
            U16(value.Length);
            Bytes(value, 0, value.Length);
            End();
        }

        public void Dropped(double t, int count)
        {
            Begin(BleWire.KindDropped, t);
            I32(count);
            End();
        }

        private void Begin(byte kind, double t)
        {
            _recordStart = _length;
            U8(kind);
            U8(0);
            U16(0);
            var bits = BitConverter.DoubleToInt64Bits(t);
            for (var i = 0; i < 8; i++)
            {
                U8((int)((bits >> (8 * i)) & 0xFF));
            }
        }

        private void End()
        {
            var payload = _length - _recordStart - BleWire.HeaderSize;
            if (payload + BleWire.HeaderSize > BleWire.MaxRecordBytes)
            {
                _length = _recordStart;
                throw new ArgumentException("BleWireWriter: registro de más de " + BleWire.MaxRecordBytes + " bytes.");
            }

            _buffer[_recordStart + 2] = (byte)(payload & 0xFF);
            _buffer[_recordStart + 3] = (byte)(payload >> 8);
        }

        private void Ensure(int more)
        {
            if (_length + more <= _buffer.Length)
            {
                return;
            }

            var next = new byte[Math.Max(_buffer.Length * 2, _length + more)];
            Buffer.BlockCopy(_buffer, 0, next, 0, _length);
            _buffer = next;
        }

        private void U8(int value)
        {
            Ensure(1);
            _buffer[_length++] = (byte)value;
        }

        private void U16(int value)
        {
            U8(value & 0xFF);
            U8((value >> 8) & 0xFF);
        }

        private void I16(int value)
        {
            U16(value & 0xFFFF);
        }

        private void I32(int value)
        {
            U8(value & 0xFF);
            U8((value >> 8) & 0xFF);
            U8((value >> 16) & 0xFF);
            U8((value >> 24) & 0xFF);
        }

        private void Str(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                U16(0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > ushort.MaxValue)
            {
                throw new ArgumentException("BleWireWriter: texto demasiado largo.");
            }

            U16(bytes.Length);
            Bytes(bytes, 0, bytes.Length);
        }

        private void Uuid(BleUuid uuid)
        {
            Ensure(16);
            BleWire.WriteUuid(uuid, _buffer, _length);
            _length += 16;
        }

        private void Bytes(byte[] source, int offset, int count)
        {
            Ensure(count);
            Buffer.BlockCopy(source, offset, _buffer, _length, count);
            _length += count;
        }
    }
}
