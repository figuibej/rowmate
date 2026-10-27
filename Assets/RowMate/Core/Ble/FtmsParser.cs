using System;
using RowMate.Core.Models;

namespace RowMate.Core.Ble
{
    // Decodificador del paquete FTMS Rower Data 0x2AD1 (F-BLE-02), port 1 a 1 del legacy.
    // Banderas (uint16 little-endian):
    //   bit 0  More Data (apagado = vienen ritmo y paladas)   bit 7  nivel de resistencia (2, se saltea)
    //   bit 1  ritmo promedio (1, se saltea)                  bit 8  energía total (2) + kcal/h (2) + kcal/min (1)
    //   bit 2  distancia total (uint24)                       bit 9  pulso (uint8)
    //   bit 3  split instantáneo (uint16)                     bit 10 equivalente metabólico (1, se saltea)
    //   bit 4  split promedio (uint16)                        bit 11 tiempo transcurrido (uint16)
    //   bit 5  potencia instantánea (sint16)                  bit 12 tiempo restante (se ignora: lo calcula la rutina)
    //   bit 6  potencia promedio (sint16)
    // Igual que el legacy: los bits 1, 7 y 10 suman al offset SIN verificar el largo; después, un campo que no entra
    // (offset + k > largo) se omite sin error. Un campo ausente vale 0 (no hay estado entre paquetes) y los valores
    // crudos no se filtran (65535, potencia negativa: Q-38). Ningún paquete produce una excepción.
    // legacy: lib/core/bluetooth/ftms_parser.dart:26-140
    public static class FtmsParser
    {
        // null si tiene menos de 2 bytes (también la notificación vacía, ble_service.dart:292). legacy: ftms_parser.dart:27
        public static RowingData? ParseRowerData(byte[] raw)
        {
            if (raw == null)
            {
                throw new ArgumentNullException(nameof(raw));
            }

            return ParseRowerData(new ReadOnlySpan<byte>(raw));
        }

        public static RowingData? ParseRowerData(ReadOnlySpan<byte> raw)
        {
            if (raw.Length < 2)
            {
                return null;
            }

            var length = raw.Length;
            var flags = raw[0] | (raw[1] << 8);
            var offset = 2;

            double strokeRate = 0;
            var strokeCount = 0;
            var distanceMeters = 0;
            var pace500mSeconds = 0;
            var powerWatts = 0;
            var totalCalories = 0;
            var heartRate = 0;
            var elapsedSeconds = 0;

            // Bit 0 = More Data: si está apagado vienen el ritmo y las paladas, cada uno solo si hay bytes (:43-53).
            var moreData = (flags & 0x01) != 0;
            if (!moreData)
            {
                if (offset + 1 <= length)
                {
                    strokeRate = raw[offset] * 0.5;
                    offset += 1;
                }

                if (offset + 2 <= length)
                {
                    strokeCount = Uint16(raw, offset);
                    offset += 2;
                }
            }

            // Bit 1: ritmo promedio (1 byte), se saltea sin verificar el largo (:56-58).
            if ((flags & 0x02) != 0)
            {
                offset += 1;
            }

            // Bit 2: distancia total, uint24 (:61-66).
            if ((flags & 0x04) != 0 && offset + 3 <= length)
            {
                distanceMeters = raw[offset] | (raw[offset + 1] << 8) | (raw[offset + 2] << 16);
                offset += 3;
            }

            // Bit 3: split instantáneo, uint16 (:69-72).
            if ((flags & 0x08) != 0 && offset + 2 <= length)
            {
                pace500mSeconds = Uint16(raw, offset);
                offset += 2;
            }

            // Bit 4: split promedio, uint16; se usa solo si el instantáneo quedó en 0 (:75-80).
            if ((flags & 0x10) != 0 && offset + 2 <= length)
            {
                if (pace500mSeconds == 0)
                {
                    pace500mSeconds = Uint16(raw, offset);
                }

                offset += 2;
            }

            // Bit 5: potencia instantánea, sint16 (:83-86).
            if ((flags & 0x20) != 0 && offset + 2 <= length)
            {
                powerWatts = Int16(raw, offset);
                offset += 2;
            }

            // Bit 6: potencia promedio, sint16; se usa solo si la instantánea quedó en 0 (:89-94).
            if ((flags & 0x40) != 0 && offset + 2 <= length)
            {
                if (powerWatts == 0)
                {
                    powerWatts = Int16(raw, offset);
                }

                offset += 2;
            }

            // Bit 7: nivel de resistencia (2 bytes), se saltea sin verificar el largo (:97-99).
            if ((flags & 0x80) != 0)
            {
                offset += 2;
            }

            // Bit 8: energía total uint16; después kcal/h (2) y kcal/min (1), salteados solo si entran (:102-109).
            if ((flags & 0x100) != 0 && offset + 2 <= length)
            {
                totalCalories = Uint16(raw, offset);
                offset += 2;
                if (offset + 2 <= length)
                {
                    offset += 2;
                }

                if (offset + 1 <= length)
                {
                    offset += 1;
                }
            }

            // Bit 9: pulso, uint8 (:112-115).
            if ((flags & 0x200) != 0 && offset + 1 <= length)
            {
                heartRate = raw[offset];
                offset += 1;
            }

            // Bit 10: equivalente metabólico (1 byte), se saltea sin verificar el largo (:118-120).
            if ((flags & 0x400) != 0)
            {
                offset += 1;
            }

            // Bit 11: tiempo transcurrido, uint16 (:123-126).
            if ((flags & 0x800) != 0 && offset + 2 <= length)
            {
                elapsedSeconds = Uint16(raw, offset);
                offset += 2;
            }

            // Bit 12: tiempo restante, ignorado: lo calcula la rutina (:128-129).
            return new RowingData
            {
                StrokeRate = strokeRate,
                StrokeCount = strokeCount,
                DistanceMeters = distanceMeters,
                Pace500mSeconds = pace500mSeconds,
                PowerWatts = powerWatts,
                TotalCalories = totalCalories,
                HeartRate = heartRate,
                ElapsedSeconds = elapsedSeconds,
            };
        }

        // = ByteData.getUint16(offset, Endian.little).
        private static int Uint16(ReadOnlySpan<byte> raw, int offset)
        {
            return raw[offset] | (raw[offset + 1] << 8);
        }

        // = ByteData.getInt16(offset, Endian.little).
        private static int Int16(ReadOnlySpan<byte> raw, int offset)
        {
            return (short)(raw[offset] | (raw[offset + 1] << 8));
        }
    }
}
