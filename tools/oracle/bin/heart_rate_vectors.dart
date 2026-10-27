// Vectores del decodificador Heart Rate Measurement 0x2A37 (WP-B, 02-tech-design §6.3)
// -> Assets/RowMate/Tests/Golden/oracle/heart_rate.json.
// Importa core/bluetooth/heart_rate_parser.dart del legacy en solo lectura (solo importa dart:).
// Secciones: los UUID, los paquetes de AC-HRM-01 (bpm y contacto, o null) y 500 paquetes aleatorios de Random(2037).
// Se corre con tools/oracle/run-oracle.ps1 -Only heart_rate_vectors (guardia del legacy de §6.1).
import 'dart:math';

import 'package:rowmate/core/bluetooth/heart_rate_parser.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/heart_rate_vectors.dart';

Map<String, Object?>? out(HeartRateReading? r) => r == null ? null : {'bpm': r.bpm, 'contact': r.sensorContact};

// Contacto como entero para la tabla: -1 = no informa (null), 0 = sin contacto, 1 = con contacto.
int contactCode(bool? c) => c == null ? -1 : (c ? 1 : 0);

void main(List<String> args) {
  final a = GoldenArgs.parse(args);

  // ---- Paquetes de AC-HRM-01 (notación del test legacy: banderas en hex, pulso en decimal) ----
  final named = <List<Object>>[
    ['AC-HRM-01-02', [0x00, 72]],
    ['AC-HRM-01-03', [0x01, 0x2C, 0x01]],
    ['AC-HRM-01-04', [0x02, 70]],
    ['AC-HRM-01-04', [0x04, 0]],
    ['AC-HRM-01-04', [0x06, 65]],
    ['AC-HRM-01-05', [0x18, 80, 0x10, 0x00, 0x00, 0x04]],
    ['AC-HRM-01-06', <int>[]],
    ['AC-HRM-01-06', [0x00]],
    ['AC-HRM-01-06', [0x01, 72]],
    ['AC-HRM-01-07', [0x00, 0]],
    ['AC-HRM-01-07', [0x00, 255]],
    ['AC-HRM-01-07', [0x01, 0xFF, 0xFF]],
    ['AC-HRM-01-08', [0xE0, 90, 1, 2, 3]],
    ['AC-HRM-01-08', [0x01, 72, 0]],
    ['AC-HRM-01-09', [0x07, 0x2C, 0x01]],
  ];
  final packets = <Map<String, Object?>>[
    for (final p in named)
      {'ac': p[0], 'bytes': bytesHex(p[1] as List<int>), 'out': out(HeartRateParser.parse(p[1] as List<int>))},
  ];

  // ---- Fuzz: 500 paquetes de Random(2037), largo 0-8 y banderas de 8 bits al azar ----
  final r = Random(2037);
  final fuzz = <List<Object?>>[];
  for (var i = 0; i < 500; i++) {
    final length = r.nextInt(9);
    final bytes = <int>[for (var j = 0; j < length; j++) r.nextInt(256)];
    final reading = HeartRateParser.parse(bytes);
    fuzz.add([bytesHex(bytes), reading == null ? 0 : 1, reading?.bpm ?? 0, contactCode(reading?.sensorContact)]);
  }

  writeGolden('heart_rate.json', {
    'header': header(generator, a.legacyCommit),
    'uuids': [
      {'serviceUuid': HeartRateParser.serviceUuid, 'measurementUuid': HeartRateParser.measurementUuid}
    ],
    'packets': packets,
    'fuzz': table(['bytes', 'ok', 'bpm', 'contact'], fuzz),
  }, args);
}
