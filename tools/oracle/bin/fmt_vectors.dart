// Vectores de Fmt / DartMath / IsoDate (WP-A, 02-tech-design §6.3) -> Assets/RowMate/Tests/Golden/oracle/fmt.json.
// Solo importa dart:* y ../lib/golden.dart: la paridad es con la VM de Dart misma (toStringAsFixed, round,
// toString, %, ~/, toIso8601String, DateTime.parse), que es lo que usa el legacy.
// Se corre con tools/oracle/run-oracle.ps1 (guardia del legacy de §6.1).
import 'dart:math';
import 'dart:typed_data';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/fmt_vectors.dart';

final ByteData _bd = ByteData(8);

double fromBits(int high, int low) {
  _bd.setUint32(0, high);
  _bd.setUint32(4, low);
  return _bd.getFloat64(0);
}

double fromBits64(int bits) {
  _bd.setInt64(0, bits);
  return _bd.getFloat64(0);
}

void main(List<String> args) {
  final a = GoldenArgs.parse(args);
  final r = Random(1234);

  // Tablas de AC-BASE-01, QA_PLAN_REVIEW §3.2 y ADR 012 §5 (sin repetidos).
  final specTable = <double>[
    0.15, 0.25, 0.35, 24.45, 24.25, 23.95, 1.05, -0.05, 2.5, 0.5, 1.5, -0.5, // AC-BASE-01
    0.45, 99.95, -2.5, 1.005, 123.456, -0.04, -0.0, 0.0001, 24.35, 120.5, // ADR 012 §5
    0.49999999999999994, 4503599627370495.5, -1.5, 0.0,
  ];
  final specials = <double>[-0.0, 5e-324, 1e20, 1e21, -1e21, double.nan, double.infinity, double.negativeInfinity,
    2.2250738585072014e-308, 1.7976931348623157e308, 9007199254740993.0, 0.1, 1 / 3];
  final randoms = List<double>.generate(500, (_) => r.nextDouble() * 1000 - 500);

  // ---- toStringAsFixed: [x, n, out] ----
  final fixed = <List<Object?>>[];
  for (final x in [...specTable, ...specials]) {
    for (var n = 0; n <= 3; n++) {
      fixed.add([f64bits(x), n, x.toStringAsFixed(n)]);
    }
  }
  for (final x in [0.1, 1 / 3, 1e20, 5e-324, 123.456, -0.0, 2.5, 24.25, 1.7976931348623157e308, 9.5e20]) {
    for (final n in [5, 10, 17, 20]) {
      fixed.add([f64bits(x), n, x.toStringAsFixed(n)]);
    }
  }
  for (var k = -1000; k <= 1000; k++) {
    final x = k / 100;
    for (var n = 0; n <= 1; n++) {
      fixed.add([f64bits(x), n, x.toStringAsFixed(n)]);
    }
  }
  for (var k = -1000; k <= 1000; k++) {
    final x = k / 1000;
    fixed.add([f64bits(x), 2, x.toStringAsFixed(2)]);
  }
  for (var k = 200; k <= 1200; k++) {
    final x = k / 20;
    fixed.add([f64bits(x), 1, x.toStringAsFixed(1)]);
  }
  for (final x in randoms) {
    for (var n = 0; n <= 3; n++) {
      fixed.add([f64bits(x), n, x.toStringAsFixed(n)]);
    }
  }

  // ---- round: [x, out] ----
  final round = <List<Object?>>[];
  for (final x in specTable) {
    round.add([f64bits(x), x.round()]);
  }
  for (var k = -200; k <= 200; k++) {
    round.add([f64bits(k / 2), (k / 2).round()]);
    round.add([f64bits(k * 0.05), (k * 0.05).round()]);
  }
  for (final x in randoms) {
    round.add([f64bits(x), x.round()]);
  }

  // ---- parseFixed: [x, n, out] (out por bits) = double.parse(x.toStringAsFixed(n)) ----
  final parseFixed = <List<Object?>>[];
  for (final x in [...specTable, ...specials]) {
    for (var n = 0; n <= 3; n++) {
      parseFixed.add([f64bits(x), n, f64bits(double.parse(x.toStringAsFixed(n)))]);
    }
  }
  for (var k = 200; k <= 1200; k++) {
    final x = k / 20;
    parseFixed.add([f64bits(x), 1, f64bits(double.parse(x.toStringAsFixed(1)))]);
  }
  for (final x in randoms) {
    for (var n = 0; n <= 3; n++) {
      parseFixed.add([f64bits(x), n, f64bits(double.parse(x.toStringAsFixed(n)))]);
    }
  }

  // ---- toString: [x, out] ----
  final toStr = <List<Object?>>[];
  final named = <double>[
    24.0, 0.1, 1e21, 1e-7, 0.000001, -0.0, 100 / 3, // ADR 012 §5
    5e-324, pow(2.0, -44).toDouble(), pow(2.0, 89).toDouble(), pow(2.0, -25).toDouble(), 1.7976931348623157e308,
    pow(2.0, 66).toDouble(), 0.86, 1.5, 2.5, 0.3, 0.92, 0.18, 0.0, 123456789012345680000.0, 1e22, 1e-6, 1e-5,
    double.nan, double.infinity, double.negativeInfinity,
  ];
  for (final x in named) {
    toStr.add([f64bits(x), x.toString()]);
  }
  for (var i = 0; i < 500; i++) {
    final x = pow(10.0, -9 + r.nextDouble() * 31).toDouble();
    toStr.add([f64bits(x), x.toString()]);
  }
  var bitPatterns = 0;
  while (bitPatterns < 500) {
    final x = fromBits(r.nextInt(1 << 32), r.nextInt(1 << 32));
    if (!x.isFinite) continue;
    toStr.add([f64bits(x), x.toString()]);
    bitPatterns++;
  }

  // ---- toStringPow2: las 2 098 potencias de 2 (QA-04), construidas por bits ----
  final pow2 = <List<Object?>>[];
  for (var j = 0; j < 52; j++) {
    final x = fromBits64(1 << j); // subnormales 2^(j - 1074)
    pow2.add([f64bits(x), x.toString()]);
  }
  for (var be = 1; be <= 2046; be++) {
    final x = fromBits64(be << 52); // normales 2^(be - 1023)
    pow2.add([f64bits(x), x.toString()]);
  }

  // ---- toStringTies: 500 empates exactos b + 0.25 y b + 0.75, b en [2^50; 2^51) (QA-04) ----
  final ties = <List<Object?>>[];
  for (var i = 0; i < 250; i++) {
    final base = (1 << 50) + r.nextInt(1 << 30) * 1024 + r.nextInt(1024);
    ties.add([f64bits(base + 0.25), (base + 0.25).toString()]);
    ties.add([f64bits(base + 0.75), (base + 0.75).toString()]);
  }

  // ---- mod (double), modInt, truncDiv (int) y truncDivF64 (double ~/ double) ----
  final mod = <List<Object?>>[];
  final truncDivF64 = <List<Object?>>[];
  mod.add([f64bits(-7.5), f64bits(4096.0), f64bits(-7.5 % 4096.0)]);
  for (var k = -30; k <= 30; k++) {
    final x = k * 0.75;
    for (final m in [4096.0, 0.5, 60.0, -7.5, 3.0]) {
      mod.add([f64bits(x), f64bits(m), f64bits(x % m)]);
    }
    for (final m in [2.0, -2.0, 0.5, 60.0, 3.0]) {
      truncDivF64.add([f64bits(x), f64bits(m), x ~/ m]);
    }
  }
  final modInt = <List<Object?>>[];
  final truncDiv = <List<Object?>>[];
  for (var x = -70; x <= 70; x++) {
    for (final m in [1, 3, 7, 60, -7, -60, 3600]) {
      modInt.add([x, m, x % m]);
      truncDiv.add([x, m, x ~/ m]);
    }
  }

  // ---- iso8601: [micros, out] = DateTime.utc(...).toIso8601String() ----
  final iso = <List<Object?>>[];
  final isoUtc = <DateTime>[
    DateTime.utc(2026, 10, 27, 9, 37, 38, 628, 537),
    DateTime.utc(2026, 10, 27, 9, 37, 38, 628),
    DateTime.utc(2026, 10, 27, 9, 37, 38),
    DateTime.utc(1970, 1, 1),
    DateTime.utc(999, 12, 31, 23, 59, 59, 999, 999),
    DateTime.utc(2000, 2, 29, 12, 0, 0, 0, 1),
    DateTime.utc(9999, 12, 31, 23, 59, 59, 999, 999),
    DateTime.utc(1, 1, 1),
  ];
  for (var i = 0; i < 60; i++) {
    isoUtc.add(DateTime.utc(2026, 1 + i % 12, 1 + i % 28, i % 24, i, i, i * 7 % 1000, i % 3 == 0 ? 0 : i * 13 % 1000));
  }
  for (final d in isoUtc) {
    iso.add([d.microsecondsSinceEpoch, d.toIso8601String()]);
  }

  // ---- iso8601Local: campos de un DateTime local y su toIso8601String() (sin zona) ----
  final isoLocal = <List<Object?>>[];
  for (var i = 0; i < 40; i++) {
    final d = DateTime(2025 + i % 3, 1 + i % 12, 1 + (i * 3) % 28, (i * 5) % 24, (i * 7) % 60, (i * 11) % 60,
        i % 4 == 0 ? 0 : (i * 37) % 1000, i % 2 == 0 ? 0 : (i * 53) % 1000);
    isoLocal.add([d.year, d.month, d.day, d.hour, d.minute, d.second, d.millisecond, d.microsecond, d.toIso8601String()]);
  }

  // ---- isoParse: textos con zona -> instante (microsecondsSinceEpoch) ----
  final withZone = <String>[
    '2026-10-27T09:37:38.628537Z', '2026-10-27T09:37:38.628Z', '2026-10-27T09:37:38.000Z', '2026-10-27T09:37:38Z',
    '2026-10-27T09:37:38.1234567Z', '2026-10-27T09:37:38.123456789+03:00', '2026-10-27 09:37:38-0300',
    '20261027T093738Z', '2026-10-27T09:37Z', '2026-10-27T09Z', '2026-10-27T23:59:59.999999-12:00',
    '2026-12-31T23:30:00-01:30', '2026-01-01T00:00:00.5z', '2026-10-27T09:37:38,5Z', '2026-10-27T09:37:38 Z',
    '2024-02-29T12:00:00+14:00', '2026-13-01T00:00:00Z', '2026-02-30T00:00:00Z', '2026-10-27T24:00:00Z',
    '+002026-10-27T09:37:38Z', '2026-10-27T09:37:38+05', '2026-10-27T09:37:38 +0530', '2026-10-27T09:37:38.0Z',
    '2026-10-27T09:37:38.000001Z', '1970-01-01T00:00:00Z', '1969-12-31T23:59:59.999999Z',
  ];
  final isoParse = <List<Object?>>[];
  for (final s in withZone) {
    isoParse.add([s, DateTime.parse(s).microsecondsSinceEpoch]);
  }

  // ---- isoParseLocal: textos sin zona -> campos de la hora de pared ----
  final withoutZone = <String>[
    '2026-10-27', '2026-10-27T09:37:38', '2026-10-27 09:37:38.628537', '2026-10-27T09:37:38.1234567', '20261027',
    '2026-10-27T09', '2026-10-27T09:37', '2026-13-05T10:00:00', '2026-02-29', '2026-10-27T09:37:38,25',
    '2026-10-27T093738', '2026-01-31T23:59:60',
  ];
  final isoParseLocal = <List<Object?>>[];
  for (final s in withoutZone) {
    final d = DateTime.parse(s);
    isoParseLocal.add([s, d.year, d.month, d.day, d.hour, d.minute, d.second, d.millisecond, d.microsecond]);
  }

  // ---- isoParseInvalid: textos que DateTime.tryParse rechaza ----
  final invalid = <String>[
    '', 'abc', '2026-1-01', '2026-10-27T9:37', '2026-10-27T', '2026-10-27T09:37:38.Z', '2026/10/27',
    '2026-10-27T09:37:38+3', '2026-10-27Z', ' 2026-10-27', '2026-10-27 ', '2026-10-27T09:37:38\n',
  ];
  final isoParseInvalid = <List<Object?>>[];
  for (final s in invalid) {
    if (DateTime.tryParse(s) != null) {
      throw StateError('se esperaba que DateTime.tryParse rechace "$s"');
    }
    isoParseInvalid.add([s]);
  }

  writeGolden('fmt.json', {
    'header': header(generator, a.legacyCommit),
    'toStringAsFixed': table(['x', 'n', 'out'], fixed),
    'round': table(['x', 'out'], round),
    'parseFixed': table(['x', 'n', 'out'], parseFixed),
    'toString': table(['x', 'out'], toStr),
    'toStringPow2': table(['x', 'out'], pow2),
    'toStringTies': table(['x', 'out'], ties),
    'mod': table(['a', 'b', 'out'], mod),
    'modInt': table(['a', 'b', 'out'], modInt),
    'truncDiv': table(['a', 'b', 'out'], truncDiv),
    'truncDivF64': table(['a', 'b', 'out'], truncDivF64),
    'iso8601': table(['micros', 'out'], iso),
    'iso8601Local': table(['year', 'month', 'day', 'hour', 'minute', 'second', 'millisecond', 'microsecond', 'out'], isoLocal),
    'isoParse': table(['text', 'micros'], isoParse),
    'isoParseLocal': table(['text', 'year', 'month', 'day', 'hour', 'minute', 'second', 'millisecond', 'microsecond'], isoParseLocal),
    'isoParseInvalid': table(['text'], isoParseInvalid),
  }, args);
}
