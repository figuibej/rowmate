// Vectores de sesión, telemetría y estadísticas (WP-C, 02-tech-design §6.3) -> Assets/RowMate/Tests/Golden/oracle/session.json.
// Corre el código del legacy (SOLO LECTURA): package:rowmate/core/models/workout_session.dart (solo importa dart:).
// Secciones:
// - p99Index (tabla [n, index]): índice del P99 para n de 1 a 1100, medido con SessionStats.compute sobre vatios
//   1..n (todos distintos y > 0, así p99Watts = index + 1).
// - stats: los casos de AC-RUT-18 con nombre; points = [elapsed, strokeRate (bits), strokeCount, distance, pace,
//   watts, calories, heartRate] y el resultado completo de compute (p99Spm por bits) con sus textos.
// - randomPoints / randomStats (tablas, columna "case"): 50 listas de Random(99) de hasta 200 puntos, con ceros
//   y negativos (que el P99 filtra) y SPM arbitrarios.
// - formats / formatSweep: splitFormatted y durationFormatted de SessionStats y durationFormatted de WorkoutSession.
// - toMap: WorkoutSession.toMap() con fechas DateTime.utc (AC-RUT-16-04); los instantes van en microsegundos desde 1970.
// - fromMap: WorkoutSession.fromMap() de registros con zona (Z u offset) y de uno con solo started_at (AC-RUT-16-05).
//   El resultado guarda el instante (microsegundos) y no la hora local, que depende de la zona del equipo.
// El id de la actividad del servicio de subida no se nombra acá: la guardia R-02 de run-oracle.ps1 rechaza ese
// nombre en cualquier script. En toMap sale igual (vacío) porque se recorren todas las columnas; su mapeo 1 a 1
// lo prueban los tests de C# (WorkoutSessionTests) con valores literales.
// Se corre con tools/oracle/run-oracle.ps1 -Only session_vectors (guardia del legacy de §6.1).
import 'dart:math';

import 'package:rowmate/core/models/workout_session.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/session_vectors.dart';

// [elapsed, strokeRate, strokeCount, distance, pace, watts, calories, heartRate]
DataPoint _p(List<num> v) => DataPoint(
      sessionId: 1,
      elapsedSeconds: v[0].toInt(),
      strokeRate: v[1].toDouble(),
      strokeCount: v[2].toInt(),
      distanceMeters: v[3].toInt(),
      pace500mSeconds: v[4].toInt(),
      powerWatts: v[5].toInt(),
      calories: v[6].toInt(),
      heartRate: v[7].toInt(),
    );

DataPoint _watts(int w) => DataPoint(sessionId: 1, elapsedSeconds: 0, powerWatts: w);
DataPoint _split(int s) => DataPoint(sessionId: 1, elapsedSeconds: 0, pace500mSeconds: s);
DataPoint _spm(double x) => DataPoint(sessionId: 1, elapsedSeconds: 0, strokeRate: x);
DataPoint _hr(int hr) => DataPoint(sessionId: 1, elapsedSeconds: 0, heartRate: hr);

List<Object?> _pointRow(DataPoint p) => [
      p.elapsedSeconds, f64bits(p.strokeRate), p.strokeCount, p.distanceMeters, p.pace500mSeconds, p.powerWatts,
      p.calories, p.heartRate,
    ];

Map<String, Object?> _statsMap(SessionStats s) => {
      'p99Watts': s.p99Watts,
      'p99Spm': f64(s.p99Spm),
      'p99SplitSeconds': s.p99SplitSeconds,
      'totalDistance': s.totalDistance,
      'totalTimeSeconds': s.totalTimeSeconds,
      'totalCalories': s.totalCalories,
      'avgHeartRate': s.avgHeartRate,
      'maxHeartRate': s.maxHeartRate,
      'hasHeartRate': s.hasHeartRate,
      'splitFormatted': s.splitFormatted,
      'durationFormatted': s.durationFormatted,
    };

Map<String, List<DataPoint>> _statsCases() => {
      'empty': <DataPoint>[],
      'watts1to361': [for (var i = 1; i <= 361; i++) _watts(i)],
      'watts_0_100_200_300': [for (final x in [0, 100, 200, 300]) _watts(x)],
      'split_130_0_125_140': [for (final x in [130, 0, 125, 140]) _split(x)],
      'watts_200_150': [for (final x in [200, 150]) _watts(x)],
      'spm_24.5_23.0_25.5': [for (final x in [24.5, 23.0, 25.5]) _spm(x)],
      'hundredPoints': [
        for (var i = 0; i < 100; i++)
          DataPoint(
            sessionId: 1,
            elapsedSeconds: i,
            powerWatts: 2 * (i + 1),
            pace500mSeconds: 100 + i,
            strokeRate: 20 + 0.1 * i,
          ),
      ],
      'totals': [
        _p([5, 0, 0, 20, 0, 0, 1, 0]),
        _p([10, 0, 0, 45, 0, 0, 2, 0]),
        _p([3, 0, 0, 0, 0, 0, 0, 0]),
        _p([615, 0, 0, 2000, 0, 0, 120, 0]),
      ],
      'hr_0_120_150_0_141': [for (final x in [0, 120, 150, 0, 141]) _hr(x)],
      'hr_0_0': [for (final x in [0, 0]) _hr(x)],
      'hr_120_121': [for (final x in [120, 121]) _hr(x)],
      'hr_120_121_121': [for (final x in [120, 121, 121]) _hr(x)],
      'hr_100_101_101_101': [for (final x in [100, 101, 101, 101]) _hr(x)],
      'duration3661': [_p([3661, 0, 0, 0, 0, 0, 0, 0])],
      'negativesAndZeros': [
        _p([1, -2.5, 3, -10, -20, -150, -1, -60]),
        _p([2, 0, 0, 0, 0, 0, 0, 0]),
        _p([3, 18.5, 4, 12, 140, 90, 1, 101]),
      ],
    };

void main(List<String> args) {
  final a = GoldenArgs.parse(args);

  // ---- índice del P99 ----
  final p99Index = <List<Object?>>[];
  for (var n = 1; n <= 1100; n++) {
    final s = SessionStats.compute([for (var i = 1; i <= n; i++) _watts(i)]);
    p99Index.add([n, s.p99Watts - 1]);
  }

  // ---- casos con nombre ----
  final stats = <Map<String, Object?>>[];
  _statsCases().forEach((name, points) {
    stats.add({
      'name': name,
      'points': [for (final p in points) _pointRow(p)],
      ...(_statsMap(SessionStats.compute(points))),
    });
  });

  // ---- listas aleatorias ----
  final rnd = Random(99);
  final randomPoints = <List<Object?>>[];
  final randomStats = <List<Object?>>[];
  int pick(int zeroPercent, int min, int max) => rnd.nextInt(100) < zeroPercent ? 0 : min + rnd.nextInt(max - min + 1);
  for (var caseId = 0; caseId < 50; caseId++) {
    final n = 1 + rnd.nextInt(200);
    final points = <DataPoint>[];
    for (var i = 0; i < n; i++) {
      final spmRoll = rnd.nextInt(10);
      final spm = spmRoll == 0 ? 0.0 : (spmRoll == 1 ? -rnd.nextDouble() * 5 : rnd.nextDouble() * 40);
      points.add(DataPoint(
        sessionId: 1,
        elapsedSeconds: pick(5, -5, 4000),
        strokeRate: spm,
        strokeCount: pick(5, 0, 2000),
        distanceMeters: pick(5, -10, 12000),
        pace500mSeconds: pick(15, -30, 400),
        powerWatts: pick(15, -50, 600),
        calories: pick(10, -2, 900),
        heartRate: pick(30, -5, 210),
      ));
    }
    for (final p in points) {
      randomPoints.add([caseId, ..._pointRow(p)]);
    }
    final s = SessionStats.compute(points);
    randomStats.add([
      caseId, n, s.p99Watts, f64bits(s.p99Spm), s.p99SplitSeconds, s.totalDistance, s.totalTimeSeconds,
      s.totalCalories, s.avgHeartRate, s.maxHeartRate, s.hasHeartRate, s.splitFormatted, s.durationFormatted,
    ]);
  }

  // ---- formatos (SessionStats y WorkoutSession) ----
  final epoch = DateTime.utc(2026);
  Map<String, Object?> fmt(int seconds) => {
        'seconds': seconds,
        'splitFormatted': SessionStats(p99SplitSeconds: seconds).splitFormatted,
        'statsDurationFormatted': SessionStats(totalTimeSeconds: seconds).durationFormatted,
        'sessionDurationFormatted': WorkoutSession(startedAt: epoch, totalTimeSeconds: seconds).durationFormatted,
      };
  final formats = [
    for (final s in [-3661, -65, -1, 0, 1, 59, 60, 61, 125, 133, 599, 600, 3599, 3600, 3661, 5999, 6000, 35999, 36000, 86399, 86400, 100000])
      fmt(s),
  ];
  final formatSweep = <List<Object?>>[];
  for (final s in [for (var i = -100; i <= 4000; i++) i, for (var i = 35990; i <= 36010; i++) i, 86400]) {
    formatSweep.add([
      s,
      SessionStats(p99SplitSeconds: s).splitFormatted,
      SessionStats(totalTimeSeconds: s).durationFormatted,
      WorkoutSession(startedAt: epoch, totalTimeSeconds: s).durationFormatted,
    ]);
  }

  // ---- toMap (fechas en UTC con Z; µs solo si no son 0) ----
  Map<String, Object?> toMapCase(String name, WorkoutSession s) {
    final m = s.toMap();
    return {
      'name': name,
      'input': {
        'id': s.id,
        'routineId': s.routineId,
        'routineName': s.routineName,
        'startedAtMicros': s.startedAt.microsecondsSinceEpoch,
        'finishedAtMicros': s.finishedAt?.microsecondsSinceEpoch,
        'totalDistanceMeters': s.totalDistanceMeters,
        'totalTimeSeconds': s.totalTimeSeconds,
        'avgPowerWatts': s.avgPowerWatts,
        'avgStrokeRate': f64(s.avgStrokeRate),
        'totalCalories': s.totalCalories,
      },
      'map': {
        for (final e in m.entries) e.key: e.value is double ? f64(e.value as double) : e.value,
      },
    };
  }

  final toMap = [
    toMapCase('micros', WorkoutSession(startedAt: DateTime.utc(2026, 10, 27, 9, 37, 38, 628, 537))),
    toMapCase('millis', WorkoutSession(startedAt: DateTime.utc(2026, 10, 27, 9, 37, 38, 628))),
    toMapCase('wholeSecond', WorkoutSession(startedAt: DateTime.utc(2026, 10, 27, 9, 37, 38))),
    toMapCase(
        'full',
        WorkoutSession(
          id: 7,
          routineId: 3,
          routineName: 'Escalera SPM',
          startedAt: DateTime.utc(2026, 1, 2, 3, 4, 5, 6, 7),
          finishedAt: DateTime.utc(2026, 1, 2, 3, 34, 5),
          totalDistanceMeters: 6123,
          totalTimeSeconds: 1800,
          avgPowerWatts: 198,
          avgStrokeRate: 29.8,
          totalCalories: 412,
        )),
    toMapCase('microsOnly', WorkoutSession(startedAt: DateTime.utc(1999, 12, 31, 23, 59, 59, 0, 1))),
  ];

  // ---- fromMap (lectura tolerante; .toLocal() del instante) ----
  Map<String, Object?> fromMapCase(String name, Map<String, dynamic> map) {
    final s = WorkoutSession.fromMap(map);
    return {
      'name': name,
      'map': {for (final e in map.entries) e.key: e.value is double ? f64(e.value as double) : e.value},
      'result': {
        'id': s.id,
        'routineId': s.routineId,
        'routineName': s.routineName,
        'startedAtMicros': s.startedAt.microsecondsSinceEpoch,
        'startedAtIsUtc': s.startedAt.isUtc,
        'finishedAtMicros': s.finishedAt?.microsecondsSinceEpoch,
        'totalDistanceMeters': s.totalDistanceMeters,
        'totalTimeSeconds': s.totalTimeSeconds,
        'avgPowerWatts': s.avgPowerWatts,
        'avgStrokeRate': f64(s.avgStrokeRate),
        'totalCalories': s.totalCalories,
        'durationFormatted': s.durationFormatted,
      },
    };
  }

  final fromMap = [
    fromMapCase('onlyStartedAt', {'started_at': '2026-10-27T09:37:38.628Z'}),
    fromMapCase('micros', {'started_at': '2026-10-27T09:37:38.628537Z'}),
    fromMapCase('sevenDecimals', {'started_at': '2026-10-27T09:37:38.1234567Z'}),
    fromMapCase('offset', {'started_at': '2026-10-27T06:37:38-03:00', 'finished_at': '2026-10-27T07:07:38.5-03:00'}),
    fromMapCase('full', {
      'id': 7,
      'routine_id': 3,
      'routine_name': 'Escalera SPM',
      'started_at': '2026-01-02T03:04:05.006007Z',
      'finished_at': '2026-01-02T03:34:05.000Z',
      'total_distance_meters': 6123,
      'total_time_seconds': 1800,
      'avg_power_watts': 198,
      'avg_stroke_rate': 29.8,
      'total_calories': 412,
    }),
    fromMapCase('nulls', {
      'id': null,
      'routine_id': null,
      'routine_name': null,
      'started_at': '2026-10-27T09:37:38Z',
      'finished_at': null,
      'total_distance_meters': null,
      'total_time_seconds': null,
      'avg_power_watts': null,
      'avg_stroke_rate': null,
      'total_calories': null,
    }),
  ];

  writeGolden('session.json', {
    'header': header(generator, a.legacyCommit),
    'p99Index': table(['n', 'index'], p99Index),
    'stats': stats,
    'randomPoints': table(
        ['case', 'elapsedSeconds', 'strokeRate', 'strokeCount', 'distanceMeters', 'pace500mSeconds', 'powerWatts', 'calories', 'heartRate'],
        randomPoints),
    'randomStats': table([
      'case', 'count', 'p99Watts', 'p99Spm', 'p99SplitSeconds', 'totalDistance', 'totalTimeSeconds', 'totalCalories',
      'avgHeartRate', 'maxHeartRate', 'hasHeartRate', 'splitFormatted', 'durationFormatted',
    ], randomStats),
    'formats': formats,
    'formatSweep': table(['seconds', 'splitFormatted', 'statsDurationFormatted', 'sessionDurationFormatted'], formatSweep),
    'toMap': toMap,
    'fromMap': fromMap,
  }, args);
}
