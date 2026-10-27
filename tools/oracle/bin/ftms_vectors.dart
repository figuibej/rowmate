// Vectores del decodificador FTMS Rower Data (WP-B, 02-tech-design §6.3) -> Assets/RowMate/Tests/Golden/oracle/ftms.json.
// Importa core/bluetooth/ftms_parser.dart del legacy en solo lectura (solo importa dart: y core/models/rowing_data.dart).
// Secciones: los UUID, los paquetes con nombre de AC-BLE-02-02 a -16 y 1 000 paquetes aleatorios de Random(2026)
// (banderas de 13 bits al azar, largo 0-24) para el fuzz de paridad.
// Se corre con tools/oracle/run-oracle.ps1 -Only ftms_vectors (guardia del legacy de §6.1).
import 'dart:math';

import 'package:rowmate/core/bluetooth/ftms_parser.dart';
import 'package:rowmate/core/models/rowing_data.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/ftms_vectors.dart';

List<int> hex(String spaced) =>
    spaced.isEmpty ? <int>[] : spaced.split(' ').map((b) => int.parse(b, radix: 16)).toList();

Map<String, Object?>? out(RowingData? d) => d == null
    ? null
    : {
        'strokeRate': f64(d.strokeRate),
        'strokeCount': d.strokeCount,
        'distanceMeters': d.distanceMeters,
        'pace500mSeconds': d.pace500mSeconds,
        'powerWatts': d.powerWatts,
        'totalCalories': d.totalCalories,
        'heartRate': d.heartRate,
        'elapsedSeconds': d.elapsedSeconds,
      };

void main(List<String> args) {
  final a = GoldenArgs.parse(args);

  // ---- Paquetes con nombre (01-po-spec §4.2): [AC, bytes] ----
  final named = <List<String>>[
    ['AC-BLE-02-02', '2C 00 30 2A 01 E2 01 00 85 00 96 00'],
    ['AC-BLE-02-03', '05 00 10 27 00'],
    ['AC-BLE-02-04', '11 00 78 00'],
    ['AC-BLE-02-05', '19 00 00 00 7D 00'],
    ['AC-BLE-02-06', '19 00 85 00 7D 00'],
    ['AC-BLE-02-07', '01 0B 2D 00 00 00 00 8C 2C 01'],
    ['AC-BLE-02-08', '00 00 30'],
    ['AC-BLE-02-09', '2C'],
    ['AC-BLE-02-10', '00 00 30'],
    ['AC-BLE-02-10', '00 00 F6'],
    ['AC-BLE-02-11', '21 00 F6 FF'],
    ['AC-BLE-02-11', '61 00 00 00 64 00'],
    ['AC-BLE-02-11', '61 00 32 00 64 00'],
    ['AC-BLE-02-12', '07 00 14 10 27 00'],
    ['AC-BLE-02-12', '81 01 05 00 2D 00 00 00 00'],
    ['AC-BLE-02-12', '01 0C 03 2C 01'],
    ['AC-BLE-02-12', '01 10 3C 00'],
    ['AC-BLE-02-13', '01 01 2D 00'],
    ['AC-BLE-02-13', '2C 00 30 2A 01 E2 01 00 85 00 96 00 FF FF'],
    ['AC-BLE-02-13', '00 00'],
    ['AC-BLE-02-14', '01 01 FF FF FF FF FF'],
    ['AC-BLE-02-14', '01 08 FF FF FF FF FF FF FF FF'],
    ['AC-BLE-02-14', '0D 00 FF FF FF FF FF'],
    ['AC-BLE-02-14', '01 02 FF'],
    ['AC-BLE-02-15', '11 00 78 00'],
    ['AC-BLE-02-16', ''],
  ];
  final packets = <Map<String, Object?>>[
    for (final p in named) {'ac': p[0], 'bytes': p[1], 'out': out(FtmsParser.parseRowerData(hex(p[1])))},
  ];

  // ---- Fuzz: 1 000 paquetes de Random(2026); celdas escalares (ok = 0 si no hay lectura) ----
  final r = Random(2026);
  final fuzz = <List<Object?>>[];
  for (var i = 0; i < 1000; i++) {
    final length = r.nextInt(25);
    final bytes = <int>[];
    if (length >= 2) {
      final flags = r.nextInt(1 << 13);
      bytes.add(flags & 0xFF);
      bytes.add(flags >> 8);
    }
    while (bytes.length < length) {
      bytes.add(r.nextInt(256));
    }
    final d = FtmsParser.parseRowerData(bytes);
    fuzz.add([
      bytesHex(bytes),
      d == null ? 0 : 1,
      f64bits(d?.strokeRate ?? 0.0),
      d?.strokeCount ?? 0,
      d?.distanceMeters ?? 0,
      d?.pace500mSeconds ?? 0,
      d?.powerWatts ?? 0,
      d?.totalCalories ?? 0,
      d?.heartRate ?? 0,
      d?.elapsedSeconds ?? 0,
    ]);
  }

  writeGolden('ftms.json', {
    'header': header(generator, a.legacyCommit),
    'uuids': [
      {
        'ftmsServiceUuid': FtmsParser.ftmsServiceUuid,
        'rowerDataUuid': FtmsParser.rowerDataUuid,
        'controlPointUuid': FtmsParser.controlPointUuid,
      }
    ],
    'packets': packets,
    'fuzz': table([
      'bytes', 'ok', 'strokeRate', 'strokeCount', 'distanceMeters', 'pace500mSeconds', 'powerWatts', 'totalCalories',
      'heartRate', 'elapsedSeconds',
    ], fuzz),
  }, args);
}
