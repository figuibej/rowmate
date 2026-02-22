import 'dart:typed_data';
import '../models/rowing_data.dart';

/// Parser del protocolo FTMS (Fitness Machine Service)
/// Characteristic Rower Data: UUID 0x2AD2
///
/// Formato de flags (2 bytes, little-endian):
///   Bit 0  – More Data (0 = Stroke Rate + Stroke Count presentes)
///   Bit 1  – Average Stroke Rate presente
///   Bit 2  – Total Distance presente
///   Bit 3  – Instantaneous Pace presente
///   Bit 4  – Average Pace presente
///   Bit 5  – Instantaneous Power presente
///   Bit 6  – Average Power presente
///   Bit 7  – Resistance Level presente
///   Bit 8  – Total Energy presente (+ Energy/hr + Energy/min)
///   Bit 9  – Heart Rate presente
///   Bit 10 – Metabolic Equivalent presente
///   Bit 11 – Elapsed Time presente
///   Bit 12 – Remaining Time presente
class FtmsParser {
  static const String rowerDataUuid = '00002ad1-0000-1000-8000-00805f9b34fb';
  static const String ftmsServiceUuid = '00001826-0000-1000-8000-00805f9b34fb';
  static const String controlPointUuid = '00002ad9-0000-1000-8000-00805f9b34fb';

  static RowingData? parseRowerData(List<int> rawBytes) {
    if (rawBytes.length < 2) return null;

    final data = ByteData.sublistView(Uint8List.fromList(rawBytes));
    final flags = data.getUint16(0, Endian.little);
    int offset = 2;

    double strokeRate = 0;
    int strokeCount = 0;
    int distanceMeters = 0;
    int pace500mSeconds = 0;
    int powerWatts = 0;
    int totalCalories = 0;
    int heartRate = 0;
    int elapsedSeconds = 0;

    // Bit 0 = More Data (si es 0, stroke rate y stroke count están presentes)
    final moreData = (flags & 0x01) != 0;
    if (!moreData) {
      if (offset + 1 <= rawBytes.length) {
        strokeRate = data.getUint8(offset) * 0.5;
        offset += 1;
      }
      if (offset + 2 <= rawBytes.length) {
        strokeCount = data.getUint16(offset, Endian.little);
        offset += 2;
      }
    }

    // Bit 1: Average Stroke Rate (1 byte, skip)
    if ((flags & 0x02) != 0) {
      offset += 1;
    }

    // Bit 2: Total Distance (3 bytes uint24)
    if ((flags & 0x04) != 0 && offset + 3 <= rawBytes.length) {
      distanceMeters = rawBytes[offset] |
          (rawBytes[offset + 1] << 8) |
          (rawBytes[offset + 2] << 16);
      offset += 3;
    }

    // Bit 3: Instantaneous Pace (2 bytes, segundos por 500m)
    if ((flags & 0x08) != 0 && offset + 2 <= rawBytes.length) {
      pace500mSeconds = data.getUint16(offset, Endian.little);
      offset += 2;
    }

    // Bit 4: Average Pace (2 bytes) — usar si no hay pace instantáneo
    if ((flags & 0x10) != 0 && offset + 2 <= rawBytes.length) {
      if (pace500mSeconds == 0) {
        pace500mSeconds = data.getUint16(offset, Endian.little);
      }
      offset += 2;
    }

    // Bit 5: Instantaneous Power (2 bytes, sint16)
    if ((flags & 0x20) != 0 && offset + 2 <= rawBytes.length) {
      powerWatts = data.getInt16(offset, Endian.little);
      offset += 2;
    }

    // Bit 6: Average Power (2 bytes) — usar si no hay power instantáneo
    if ((flags & 0x40) != 0 && offset + 2 <= rawBytes.length) {
      if (powerWatts == 0) {
        powerWatts = data.getInt16(offset, Endian.little);
      }
      offset += 2;
    }

    // Bit 7: Resistance Level (2 bytes, skip)
    if ((flags & 0x80) != 0) {
      offset += 2;
    }

    // Bit 8: Total Energy (2 bytes) + Energy/hr (2 bytes) + Energy/min (1 byte)
    if ((flags & 0x100) != 0 && offset + 2 <= rawBytes.length) {
      totalCalories = data.getUint16(offset, Endian.little);
      offset += 2;
      // Energy per hour
      if (offset + 2 <= rawBytes.length) offset += 2;
      // Energy per minute
      if (offset + 1 <= rawBytes.length) offset += 1;
    }

    // Bit 9: Heart Rate (1 byte, uint8 bpm)
    if ((flags & 0x200) != 0 && offset + 1 <= rawBytes.length) {
      heartRate = data.getUint8(offset);
      offset += 1;
    }

    // Bit 10: Metabolic Equivalent (1 byte, skip)
    if ((flags & 0x400) != 0) {
      offset += 1;
    }

    // Bit 11: Elapsed Time (2 bytes, segundos)
    if ((flags & 0x800) != 0 && offset + 2 <= rawBytes.length) {
      elapsedSeconds = data.getUint16(offset, Endian.little);
      offset += 2;
    }

    // Bit 12: Remaining Time (2 bytes, skip)
    // ignorado porque lo calculamos desde la rutina

    return RowingData(
      strokeRate: strokeRate,
      strokeCount: strokeCount,
      distanceMeters: distanceMeters,
      pace500mSeconds: pace500mSeconds,
      powerWatts: powerWatts,
      totalCalories: totalCalories,
      heartRate: heartRate,
      elapsedSeconds: elapsedSeconds,
    );
  }
}
