using System;

namespace RowMate.Core.Ble
{
    // Decodificador del Heart Rate Profile del Bluetooth SIG (F-HRM-01), port 1 a 1 del legacy.
    // Byte 0 = banderas:
    //   bit 0    formato del bpm: 0 = uint8 (byte 1), 1 = uint16 little-endian (bytes 1-2)
    //   bits 1-2 contacto: 0b0x = no informa, 0b10 = sin contacto, 0b11 = con contacto
    //   bit 3    Energy Expended (uint16), no se lee ni se valida
    //   bit 4    intervalos RR (uint16 cada uno), no se leen ni se validan
    // legacy: lib/core/bluetooth/heart_rate_parser.dart:20-40
    public static class HeartRateParser
    {
        public const string ServiceUuid = "0000180d-0000-1000-8000-00805f9b34fb";
        public const string MeasurementUuid = "00002a37-0000-1000-8000-00805f9b34fb";

        public static readonly BleUuid Service = BleUuid.Parse(ServiceUuid);
        public static readonly BleUuid Measurement = BleUuid.Parse(MeasurementUuid);

        public static HeartRateReading? Parse(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            return Parse(new ReadOnlySpan<byte>(bytes));
        }

        // null si está vacío o si le faltan bytes para su formato: el de 16 bits con 2 bytes NO cae a 8 bits (:25, :28).
        public static HeartRateReading? Parse(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length == 0)
            {
                return null;
            }

            var flags = bytes[0];
            var is16 = (flags & 0x01) != 0;
            if (bytes.Length < (is16 ? 3 : 2))
            {
                return null;
            }

            var bpm = is16 ? bytes[1] | (bytes[2] << 8) : bytes[1];

            // switch (contactBits) { 2 => false, 3 => true, _ => null } (:33-38).
            var contactBits = (flags >> 1) & 0x03;
            bool? contact;
            switch (contactBits)
            {
                case 2:
                    contact = false;
                    break;
                case 3:
                    contact = true;
                    break;
                default:
                    contact = null;
                    break;
            }

            return new HeartRateReading(bpm, contact);
        }
    }
}
