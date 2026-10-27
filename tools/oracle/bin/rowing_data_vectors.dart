// Vectores de RowingData y de los textos de la grilla de Dispositivo (WP-B, 02-tech-design §6.3)
// -> Assets/RowMate/Tests/Golden/oracle/rowing_data.json.
// Importa el modelo del legacy en solo lectura (core/models/rowing_data.dart solo importa dart:) y reproduce las
// expresiones de la grilla tal cual las escribe lib/features/device/device_screen.dart:253-258 sobre ese modelo.
// Se corre con tools/oracle/run-oracle.ps1 -Only rowing_data_vectors (guardia del legacy de §6.1).
import 'package:rowmate/core/models/rowing_data.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/rowing_data_vectors.dart';

Map<String, Object?> fields(RowingData d) => {
      'strokeRate': f64(d.strokeRate),
      'strokeCount': d.strokeCount,
      'distanceMeters': d.distanceMeters,
      'pace500mSeconds': d.pace500mSeconds,
      'powerWatts': d.powerWatts,
      'totalCalories': d.totalCalories,
      'heartRate': d.heartRate,
      'elapsedSeconds': d.elapsedSeconds,
      'pace500mFormatted': d.pace500mFormatted,
      'elapsedFormatted': d.elapsedFormatted,
    };

// Textos de la grilla (device_screen.dart:253-258), con las mismas expresiones del legacy.
String spmText(RowingData d) => d.strokeRate.toStringAsFixed(1);
String wattsText(RowingData d) => d.powerWatts.toString();
String metersText(RowingData d) => '${d.distanceMeters}m';
String heartRateText(RowingData d) => d.heartRate.toString();

void main(List<String> args) {
  final a = GoldenArgs.parse(args);

  // ---- pace500mFormatted: tabla de AC-BLE-01-02 (incluida en el barrido [-10; 7000]) y bordes ----
  final paceSeconds = <int>[
    for (var s = -10; s <= 7000; s++) s,
    9999, 35999, 36000, 65535, 99999, 359999, 360000, 1000000,
  ];
  final pace = <List<Object?>>[
    for (final s in paceSeconds) [s, RowingData(pace500mSeconds: s).pace500mFormatted],
  ];

  // ---- elapsedFormatted: tabla de AC-BLE-01-03, {0…4000} ∪ {3599…3700} ∪ {35990…36010, 86400}, negativos y bordes ----
  final elapsedSeconds = <int>{
    for (var s = -120; s <= 4000; s++) s,
    for (var s = 35990; s <= 36010; s++) s,
    86399, 86400, 86401, 65535, 359999, 360000, 1000000,
  }.toList()
    ..sort();
  final elapsed = <List<Object?>>[
    for (final s in elapsedSeconds) [s, RowingData(elapsedSeconds: s).elapsedFormatted],
  ];

  // ---- spmText: toStringAsFixed(1) de los valores de la spec, k/20 (spm de 0 a 60) y los del FTMS (byte · 0.5) ----
  final spmValues = <double>[
    24.0, 23.5, 24.25, 24.35, 24.45, 23.95, 0.0, -0.0, 123.0, 127.5, 0.05, 0.15, 0.25, 99.95,
    for (var k = 0; k <= 1200; k++) k / 20,
    for (var b = 0; b <= 255; b++) b * 0.5,
  ];
  final spm = <List<Object?>>[
    for (final x in spmValues) [f64bits(x), spmText(RowingData(strokeRate: x))],
  ];

  // ---- wattsText / heartRateText / metersText ----
  final wattsValues = <int>[for (var w = -600; w <= 1000; w++) w, -32768, 32767, 65535];
  final watts = <List<Object?>>[
    for (final w in wattsValues) [w, wattsText(RowingData(powerWatts: w))],
  ];
  final hrValues = <int>[for (var b = 0; b <= 255; b++) b, 300, 65535];
  final heartRate = <List<Object?>>[
    for (final b in hrValues) [b, heartRateText(RowingData(heartRate: b))],
  ];
  final metersValues = <int>[
    for (var m = 0; m <= 2000; m++) m,
    652, 9999, 10000, 42195, 100000, 1000000, 16777215,
  ];
  final meters = <List<Object?>>[
    for (final m in metersValues) [m, metersText(RowingData(distanceMeters: m))],
  ];

  // ---- empty y copyWith (AC-BLE-01-01, AC-BLE-01-04) ----
  const base = RowingData(strokeRate: 24.0, powerWatts: 150);
  final copyWith = <Map<String, Object?>>[
    {'case': 'heartRate=140', 'out': fields(base.copyWith(heartRate: 140))},
    {'case': 'sin argumentos', 'out': fields(base.copyWith())},
    {'case': 'strokeRate=23.5', 'out': fields(base.copyWith(strokeRate: 23.5))},
    {'case': 'distanceMeters=482', 'out': fields(base.copyWith(distanceMeters: 482))},
    {'case': 'heartRate=0', 'out': fields(base.copyWith(heartRate: 0))},
  ];

  writeGolden('rowing_data.json', {
    'header': header(generator, a.legacyCommit),
    'empty': [fields(const RowingData())],
    'copyWith': copyWith,
    'pace500mFormatted': table(['s', 'out'], pace),
    'elapsedFormatted': table(['s', 'out'], elapsed),
    'spmText': table(['x', 'out'], spm),
    'wattsText': table(['w', 'out'], watts),
    'heartRateText': table(['bpm', 'out'], heartRate),
    'metersText': table(['m', 'out'], meters),
  }, args);
}
