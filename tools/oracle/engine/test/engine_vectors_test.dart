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

// R-4/2s (QA-V4a-L01): un monitor que actualiza la distancia cada 2 s a 5,5 m/s (split 1:31): +11 m en los k pares.
RowingData r4every2s(int k) => r4(k).copyWith(distanceMeters: 1000 + 11 * (k ~/ 2));

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

// ── Guiones (QA-V4a-L02, 05-qa-lens-paridad §2 y §4 de la It. 4a; hechos al empezar la 4b) ──
//
// Los caminos que la lente de paridad comprobó a mano contra este mismo WorkoutProvider: pasos de 0 s y de duración negativa,
// paso sin meta con Terminar, terminar en pausa, último paso por distancia, tiempo y distancia a la vez, pulso del sensor en
// los puntos, pausa antes del primer segundo, pausa que cruza un cambio de paso, dos sesiones seguidas, varias pausas en un
// mismo segundo, terminar en el segundo del cambio, último paso de 0 s, terminar en un múltiplo de 5 y el monitor que informa
// la distancia cada 2 s (QA-V4a-L01). Un guion es una lista de acciones [ms, tipo, argumento] desde el instante 0:
//   free · routine <nombre> · pause · resume · finish · reset · bpm <int> · status <HrmStatus>
// Las rutinas van en el mismo escenario (con sus pasos), así WorkoutGoldenTests corre exactamente lo mismo en el motor nuevo.
// "reset" es el toque del diálogo de completado o la vuelta a Workout del legacy. It. 4b (AC-WKT-21-13; 02-tech-design §2.13):
// con la rutina terminada sola el motor nuevo también queda terminado hasta ese toque (AcknowledgeCompletion, DT4a-15); a
// mano vuelve solo a reposo al cerrar, y en los guiones el reset de un terminar a mano va en el mismo instante.
// Se anota, en la grilla de 100 ms: los cambios de paso [t, segundos activos, paso] (-99 = stepProgress lanza: DV-37, la
// pantalla del legacy falla con un paso de duración negativa), las acciones con su reloj y su fase, el instante en que la
// rutina termina sola, los objetivos en ese instante, el orden de las operaciones de la base, las altas, los puntos y los
// cierres (sin fechas ni id).

Map<String, Object?> stepJson(IntervalStep s) => {
      'type': s.type.name,
      'dur': s.durationSeconds,
      'dist': s.distanceMeters,
      'wMin': s.targetWattsMin,
      'spm': s.targetSpm,
      'pSpm': s.progressionSpm,
      'g': s.groupId,
      'rep': s.groupRepeatCount,
    };

Map<String, Object?> routineJson(Routine r) => {
      'id': r.id,
      'name': r.name,
      'createdAt': r.createdAt.toUtc().toIso8601String(),
      'steps': [for (final s in r.steps) stepJson(s)],
    };

Routine mk(int id, String name, List<IntervalStep> steps) =>
    Routine(id: id, name: name, createdAt: DateTime.utc(2026, 10, 26), steps: steps);

Map<String, Object?> script(String name, String readingName, Reading reading, List<List<Object?>> actions, int endMs) {
  final db = _CaptureDb();
  final steps = <List<Object?>>[];
  final events = <List<Object?>>[];
  final targets = <List<Object?>>[];
  final finishedAt = <int>[];
  final routines = <String, Object?>{};
  for (final a in actions) {
    if (a[1] == 'routine') {
      final r = a[2] as Routine;
      routines[r.name] = routineJson(r);
    }
  }
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
    var tMs = 0;
    var lastStep = -2;
    var wasFinished = false;
    void doActions() {
      for (final a in actions) {
        if (a[0] != tMs) continue;
        final kind = a[1] as String;
        switch (kind) {
          case 'free':
            wp.startFree();
            break;
          case 'routine':
            wp.startWithRoutine(a[2] as Routine);
            break;
          case 'pause':
            wp.pause();
            break;
          case 'resume':
            wp.resume();
            break;
          case 'finish':
            if (wp.phase != WorkoutPhase.finished) wp.finish();
            break;
          case 'reset':
            wp.reset();
            break;
          case 'bpm':
            hrm.bpmCtrl.add(a[2] as int);
            break;
          case 'status':
            hrm.statusCtrl.add(a[2] as HrmStatus);
            break;
          default:
            throw ArgumentError('acción desconocida: $kind');
        }
        async.flushMicrotasks();
        events.add([tMs, kind, wp.totalElapsedSeconds, wp.phase.name]);
      }
    }

    doActions();
    while (tMs < endMs) {
      var nextMs = (tMs ~/ 100) * 100 + 100; // de a 100 ms
      for (final a in actions) {
        final at = a[0] as int;
        if (at > tMs && at < nextMs) nextMs = at;
      }
      if (nextMs > endMs) nextMs = endMs;
      async.elapse(Duration(milliseconds: nextMs - tMs));
      tMs = nextMs;
      int idx;
      try {
        idx = wp.stepProgress?.stepIndex ?? -1;
      } catch (e) {
        // DV-37: con un paso de duración negativa, el clamp(0, d) de stepProgress lanza (workout_provider.dart:103).
        idx = -99;
      }
      if (idx != lastStep) {
        steps.add([tMs, wp.totalElapsedSeconds, idx]);
        lastStep = idx;
      }
      if (wp.phase == WorkoutPhase.finished && !wasFinished) {
        // La rutina terminó sola (los Terminar a mano de los guiones van con su reset en el mismo instante).
        wasFinished = true;
        finishedAt.add(tMs);
        async.flushMicrotasks();
        for (var i = 0; i < 64; i++) {
          final r = wp.targets.resultOf(i);
          if (r != null) targets.add([finishedAt.length, i, r.name]);
        }
      }
      if (wp.phase != WorkoutPhase.finished) wasFinished = false;
      doActions();
    }
    async.flushMicrotasks();
  });
  return {
    'name': name,
    'reading': readingName,
    'routines': routines,
    'actions': [
      for (final a in actions)
        [
          a[0],
          a[1],
          if (a.length > 2) a[2] is Routine ? (a[2] as Routine).name : (a[2] is HrmStatus ? (a[2] as HrmStatus).name : a[2]),
        ]
    ],
    'endMs': endMs,
    'steps': steps,
    'events': events,
    'finishedAtMs': finishedAt,
    'ops': db.ops,
    'sessions': [for (final s in db.sessions) row(s)],
    'points': [for (final p in db.points) row(p)],
    'closes': [for (final c in db.closes) row(c)],
    'closesHaveFinishedAt': [for (final c in db.closes) c['finished_at'] != null],
    'targets': targets,
  };
}

List<Map<String, Object?>> scripts() {
  final zero = mk(21, 'Ceros', [st(StepType.work, dur: 0), st(StepType.rest, dur: 0), st(StepType.work, dur: 7)]);
  final neg = mk(22, 'Negativo', [st(StepType.work, dur: -5), st(StepType.rest, dur: 3)]);
  final noGoal = mk(23, 'Sin meta', [st(StepType.work)]);
  final distLast = mk(24, 'Distancia al final', [st(StepType.warmup, dur: 3), st(StepType.work, dist: 50)]);
  final timeAndDist = mk(25, 'Tiempo y distancia', [st(StepType.work, dur: 7, dist: 1000), st(StepType.rest, dur: 2)]);
  final lastZero = mk(26, 'Ultimo cero', [st(StepType.work, dur: 3), st(StepType.rest, dur: 0)]);
  return [
    script('zero_steps', 'R-4', r4, [
      [0, 'routine', zero]
    ], 15000),
    script('neg_step', 'R-4', r4, [
      [0, 'routine', neg]
    ], 10000),
    script('nogoal_finish', 'R-4', r4, [
      [0, 'routine', noGoal],
      [12300, 'finish'],
      [12300, 'reset']
    ], 13000),
    script('finish_in_pause', 'R-4', r4, [
      [0, 'routine', corta()],
      [12400, 'pause'],
      [20000, 'finish'],
      [20000, 'reset']
    ], 21000),
    script('dist_last', 'R-4', r4, [
      [0, 'routine', distLast]
    ], 30000),
    script('time_and_distance', 'R-4', r4, [
      [0, 'routine', timeAndDist]
    ], 15000),
    script('hr_points', 'R-4', r4, [
      [0, 'free'],
      [2200, 'bpm', 120],
      [7200, 'bpm', 130],
      [11200, 'status', HrmStatus.disconnected],
      [16000, 'finish'],
      [16000, 'reset']
    ], 17000),
    script('pause_early', 'R-4', r4, [
      [0, 'free'],
      [50, 'pause'],
      [2050, 'resume'],
      [8000, 'finish'],
      [8000, 'reset']
    ], 9000),
    script('corta_pause_across_change', 'R-4', r4, [
      [0, 'routine', corta()],
      [9500, 'pause'],
      [15300, 'resume']
    ], 45000),
    script('two_sessions', 'R-4', r4, [
      [0, 'free'],
      [6000, 'finish'],
      [6000, 'reset'],
      [9000, 'routine', corta()]
    ], 45000),
    script('pause_resume_jitter', 'R-4', r4, [
      [0, 'free'],
      [3200, 'pause'],
      [3400, 'resume'],
      [3600, 'pause'],
      [3800, 'resume'],
      [12000, 'finish'],
      [12000, 'reset']
    ], 13000),
    script('finish_at_step_change', 'R-4', r4, [
      [0, 'routine', corta()],
      [10000, 'finish'],
      [10000, 'reset']
    ], 11000),
    script('last_step_zero', 'R-4', r4, [
      [0, 'routine', lastZero]
    ], 8000),
    script('free_finish_at_5', 'R-4', r4, [
      [0, 'free'],
      [5000, 'finish'],
      [5000, 'reset']
    ], 6000),
    script('dist100_every_2s', 'R-4/2s', r4every2s, [
      [0, 'routine', distancia100()]
    ], 60000),
    // It. 4b (AC-WKT-21-13): la rutina termina sola a los 30 s, llega un bpm a los 32 y el reset (el Terminar del diálogo)
    // a los 34: entre el fin y el reset la fase es finished, el reloj queda en 30, no hay puntos y los objetivos siguen.
    script('completed_hold_reset', 'R-4', r4, [
      [0, 'routine', corta()],
      [32000, 'bpm', 128],
      [34000, 'reset']
    ], 36000),
  ];
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
    writeGolden('workout_engine.json', {'header': header(generator, commit), 'scenarios': scenarios, 'scripts': scripts()},
        ['--out', outDir, '--legacy-commit', commit]);
  });
}
