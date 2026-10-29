// Oráculo del motor legacy (02-tech-design §3.7 de la It. 4a; sonda S4-a, Anexo A): corre el WorkoutProvider del legacy,
// sin tocarlo, con el remo de prueba R-4 de la spec (§0.4), el pulsómetro de test/workout_provider_hr_test.dart:11-77 y
// una base que solo anota (nunca abre SQLite). Escribe Assets/RowMate/Tests/Golden/oracle/workout_engine.json con
// ../../lib/golden.dart. Lo corre tools/oracle/run-oracle.ps1 -Engine, que pasa la carpeta de salida y el commit del
// legacy en ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT.
//
// Cada escenario: los puntos (sin id), la fila del alta y la del cierre (sin fechas ni id: el legacy toma la hora real),
// los resultados de objetivos, los cambios de paso [t en ms, segundos activos, paso] y el orden de las operaciones.
import 'dart:async';
import 'dart:io';

import 'package:fake_async/fake_async.dart';
import 'package:flutter_blue_plus/flutter_blue_plus.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:rowmate/core/bluetooth/ble_service.dart';
import 'package:rowmate/core/bluetooth/heart_rate_service.dart';
import 'package:rowmate/core/database/database_service.dart';
import 'package:rowmate/core/models/interval_step.dart';
import 'package:rowmate/core/models/routine.dart';
import 'package:rowmate/core/models/rowing_data.dart';
import 'package:rowmate/core/models/workout_session.dart';
import 'package:rowmate/features/workout/workout_provider.dart';

import '../../lib/golden.dart';

const String generator = 'tools/oracle/engine/test/engine_vectors_test.dart';

// El remo como broadcast sin estado inicial (los dobles de test/workout_provider_hr_test.dart).
class _FakeBle implements BleService {
  final data = StreamController<RowingData>.broadcast();
  @override
  Stream<RowingData> get dataStream => data.stream;
  @override
  Stream<BleStatus> get statusStream => const Stream.empty();
  @override
  Stream<List<ScanResult>> get devicesStream => const Stream.empty();
  @override
  Stream<List<int>> get rawBytesStream => const Stream.empty();
  @override
  Stream<BluetoothAdapterState> get adapterStateStream => const Stream.empty();
  @override
  BluetoothAdapterState get adapterState => BluetoothAdapterState.on;
  @override
  BleStatus get status => BleStatus.connected;
  @override
  String? get connectedDeviceName => 'fake';
  @override
  Future<void> startScan({Duration timeout = const Duration(seconds: 10)}) async {}
  @override
  Future<void> stopScan() async {}
  @override
  Future<void> connect(BluetoothDevice device) async {}
  @override
  Future<void> disconnect() async {}
  @override
  void dispose() => data.close();
}

class _FakeHrm implements HeartRateService {
  final bpmCtrl = StreamController<int>.broadcast();
  final statusCtrl = StreamController<HrmStatus>.broadcast();
  @override
  Stream<int> get bpmStream => bpmCtrl.stream;
  @override
  Stream<HrmStatus> get statusStream => statusCtrl.stream;
  @override
  Stream<List<ScanResult>> get devicesStream => const Stream.empty();
  @override
  HrmStatus get status => HrmStatus.connected;
  @override
  int get bpm => 0;
  @override
  String? get connectedDeviceName => null;
  @override
  String? get rememberedDeviceName => null;
  @override
  bool get isRetrying => false;
  @override
  Future<void> startScan({Duration timeout = const Duration(seconds: 10)}) async {}
  @override
  Future<void> stopScan() async {}
  @override
  Future<void> connect(BluetoothDevice device) async {}
  @override
  Future<void> autoConnect() async {}
  @override
  Future<void> disconnect() async {}
  @override
  Future<void> forget() async {}
  @override
  void dispose() {
    bpmCtrl.close();
    statusCtrl.close();
  }
}

// La base que anota en orden lo que el motor le pide.
class _CaptureDb extends DatabaseService {
  final ops = <String>[];
  final sessions = <Map<String, dynamic>>[];
  final points = <Map<String, dynamic>>[];
  final closes = <Map<String, dynamic>>[];
  int _next = 1;

  @override
  Future<int> insertSession(WorkoutSession session) async {
    ops.add('insertSession');
    sessions.add(Map<String, dynamic>.from(session.toMap()));
    return _next++;
  }

  @override
  Future<void> insertDataPoints(List<DataPoint> list) async {
    for (final p in list) {
      ops.add('insertDataPoint');
      points.add(Map<String, dynamic>.from(p.toMap()));
    }
  }

  @override
  Future<void> updateSession(WorkoutSession session) async {
    ops.add('updateSession');
    closes.add(Map<String, dynamic>.from(session.toMap()));
  }
}

typedef Reading = RowingData Function(int k);

// R-4 (spec §0.4): L(k) en el instante k - 0,5 s.
RowingData r4(int k) => RowingData(
      elapsedSeconds: 300 + k,
      strokeCount: 100 + k,
      distanceMeters: 1000 + 4 * k,
      pace500mSeconds: 125,
      powerWatts: 150,
      strokeRate: 24.0,
      totalCalories: 40 + k ~/ 10,
      heartRate: 0,
    );

Reading r4spm(double spm) => (k) => r4(k).copyWith(strokeRate: spm);

// R-4R: el monitor se reinicia en L(10). R-4Z: L(10) sin distancia.
RowingData r4r(int k) => k >= 10 ? r4(k).copyWith(distanceMeters: 4 * (k - 10)) : r4(k);

RowingData r4z(int k) => k == 10 ? r4(k).copyWith(distanceMeters: 0) : r4(k);

IntervalStep st(StepType t, {int? dur, int? dist, int? wMin, int? spm, int? pSpm, String? g, int? rep}) => IntervalStep(
      routineId: 1,
      order: 0,
      type: t,
      durationSeconds: dur,
      distanceMeters: dist,
      targetWattsMin: wMin,
      targetSpm: spm,
      progressionSpm: pSpm,
      groupId: g,
      groupRepeatCount: rep,
    );

// Las rutinas de F-4A (Tests/Support/RoutineSeeds.cs), con el id con que las guarda la prueba de C#.
Routine corta() => Routine(
      id: 7,
      name: 'Corta',
      createdAt: DateTime.utc(2026, 10, 26),
      steps: [
        st(StepType.work, dur: 10, spm: 18, pSpm: 2, g: 'g1', rep: 2),
        st(StepType.rest, dur: 5, g: 'g1', rep: 2),
      ],
    );

Routine prueba4a() => Routine(
      id: 8,
      name: 'Prueba 4a',
      createdAt: DateTime.utc(2026, 10, 27),
      steps: [
        st(StepType.warmup, dur: 30),
        st(StepType.work, dur: 60, wMin: 160, spm: 22, pSpm: 2, g: 'g2', rep: 2),
        st(StepType.rest, dur: 30, g: 'g2', rep: 2),
        st(StepType.cooldown, dur: 30),
      ],
    );

Routine distancia100() => Routine(
      id: 9,
      name: 'Distancia 100',
      createdAt: DateTime.utc(2026, 10, 25),
      steps: [st(StepType.work, dist: 100), st(StepType.rest, dur: 5)],
    );

// Los dobles a f64 (bits exactos); el resto, tal cual. Sin las fechas ni el id (el legacy usa la hora real).
Map<String, Object?> row(Map<String, dynamic> map) {
  final out = <String, Object?>{};
  for (final entry in map.entries) {
    if (entry.key == 'id' || entry.key == 'started_at' || entry.key == 'finished_at') continue;
    final v = entry.value;
    out[entry.key] = v is double ? f64(v) : v;
  }
  return out;
}

Map<String, Object?> run(
  String name, {
  required Routine? routine,
  required String readingName,
  required Reading reading,
  required int finishAtMs, // desde el inicio; < 0 = no se termina a mano
  int runUntilMs = -1,
  List<List<int>> pausesMs = const [], // [inicio, duración] en ms desde el inicio
}) {
  final db = _CaptureDb();
  final steps = <List<Object?>>[];
  final events = <List<Object?>>[];
  late Map<String, Object?> out;
  fakeAsync((async) {
    final ble = _FakeBle();
    final hrm = _FakeHrm();
    final wp = WorkoutProvider(ble, hrm, db);
    // L(0) en el instante -0,5: el reloj falso arranca en 0 = instante -0,5.
    var k = 0;
    ble.data.add(reading(0));
    async.flushMicrotasks();
    Timer.periodic(const Duration(seconds: 1), (_) {
      k++;
      ble.data.add(reading(k));
    });
    async.elapse(const Duration(milliseconds: 500));
    // Instante 0: el inicio.
    if (routine == null) {
      wp.startFree();
    } else {
      wp.startWithRoutine(routine);
    }
    async.flushMicrotasks();
    var lastStep = -2;
    var finishedAtMs = -1;
    final endMs = runUntilMs > 0 ? runUntilMs : (finishAtMs > 0 ? finishAtMs : 400000);
    var tMs = 0;
    final pauseEvents = <List<int>>[];
    for (final p in pausesMs) {
      pauseEvents.add([p[0], 0]);
      pauseEvents.add([p[0] + p[1], 1]);
    }
    pauseEvents.sort((a, b) => a[0].compareTo(b[0]));
    while (tMs < endMs) {
      var nextMs = (tMs ~/ 100) * 100 + 100; // de a 100 ms
      for (final e in pauseEvents) {
        if (e[0] > tMs && e[0] < nextMs) nextMs = e[0];
      }
      if (finishAtMs > tMs && finishAtMs < nextMs) nextMs = finishAtMs;
      if (nextMs > endMs) nextMs = endMs;
      async.elapse(Duration(milliseconds: nextMs - tMs));
      tMs = nextMs;
      for (final e in pauseEvents) {
        if (e[0] == tMs) {
          if (e[1] == 0) {
            wp.pause();
            events.add([tMs, 'pause', wp.totalElapsedSeconds]);
          } else {
            wp.resume();
            events.add([tMs, 'resume', wp.totalElapsedSeconds]);
          }
        }
      }
      final idx = wp.stepProgress?.stepIndex ?? -1;
      if (idx != lastStep) {
        steps.add([tMs, wp.totalElapsedSeconds, idx]);
        lastStep = idx;
      }
      if (finishAtMs > 0 && tMs == finishAtMs && wp.phase != WorkoutPhase.finished) {
        wp.finish();
        async.flushMicrotasks();
        events.add([tMs, 'finish', wp.totalElapsedSeconds]);
      }
      if (wp.phase == WorkoutPhase.finished) {
        if (finishedAtMs < 0) finishedAtMs = tMs;
        async.flushMicrotasks();
        break;
      }
    }
    final targets = <List<Object?>>[];
    if (routine != null) {
      for (var i = 0; i < 64; i++) {
        final r = wp.targets.resultOf(i);
        if (r != null) targets.add([i, r.name]);
      }
    }
    out = {
      'name': name,
      'routine': routine?.name,
      'routineId': routine?.id,
      'reading': readingName,
      'finishAtMs': finishAtMs,
      'runUntilMs': endMs,
      'pausesMs': pausesMs,
      'phase': wp.phase.name,
      'total': wp.totalElapsedSeconds,
      'finishedAtMs': finishedAtMs,
      'lastFinishedSessionId': wp.lastFinishedSessionId,
      'steps': steps,
      'events': events,
      'ops': db.ops,
      'session': db.sessions.isEmpty ? null : row(db.sessions.first),
      'points': [for (final p in db.points) row(p)],
      'close': db.closes.isEmpty ? null : row(db.closes.first),
      'closeHasFinishedAt': db.closes.isNotEmpty && db.closes.first['finished_at'] != null,
      'targets': targets,
    };
  });
  return out;
}

void main() {
  test('vectores del motor legacy (It. 4a, workout_engine.json)', () {
    final outDir = Platform.environment['ROWMATE_ORACLE_OUT'];
    final commit = Platform.environment['ROWMATE_LEGACY_COMMIT'];
    if (outDir == null || commit == null) {
      fail('faltan ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT: correlo con tools/oracle/run-oracle.ps1 -Engine');
    }
    final scenarios = <Map<String, Object?>>[
      run('free_r4_finish_12_3', routine: null, readingName: 'R-4', reading: r4, finishAtMs: 12300),
      run('corta_r4', routine: corta(), readingName: 'R-4', reading: r4, finishAtMs: -1),
      run('corta_r4_19', routine: corta(), readingName: 'R-4(19)', reading: r4spm(19), finishAtMs: -1),
      run('prueba4a_r4', routine: prueba4a(), readingName: 'R-4', reading: r4, finishAtMs: -1),
      run('dist100_r4', routine: distancia100(), readingName: 'R-4', reading: r4, finishAtMs: -1, runUntilMs: 60000),
      run('dist100_r4r', routine: distancia100(), readingName: 'R-4R', reading: r4r, finishAtMs: -1, runUntilMs: 300000),
      run('dist100_r4z', routine: distancia100(), readingName: 'R-4Z', reading: r4z, finishAtMs: -1, runUntilMs: 60000),
      run('dist100_r4_pause', routine: distancia100(), readingName: 'R-4', reading: r4, finishAtMs: -1, runUntilMs: 80000, pausesMs: [
        [10000, 20000]
      ]),
      run('free_r4_pause_q58', routine: null, readingName: 'R-4', reading: r4, finishAtMs: 43000, pausesMs: [
        [10700, 30000]
      ]),
      run('free_r4_pause_sampling', routine: null, readingName: 'R-4', reading: r4, finishAtMs: 42500, pausesMs: [
        [7000, 30000]
      ]),
      run('free_r4_short', routine: null, readingName: 'R-4', reading: r4, finishAtMs: 3000),
    ];
    writeGolden('workout_engine.json', {'header': header(generator, commit), 'scenarios': scenarios},
        ['--out', outDir, '--legacy-commit', commit]);
  });
}
