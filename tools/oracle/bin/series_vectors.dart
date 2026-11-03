// Vectores del SeriesTracker del legacy (It. 5, WP-A; 02-tech-design §2.9; AC-WKT-07-12)
// -> Assets/RowMate/Tests/Golden/oracle/series_tracker.json.
// Corre el código del legacy (SOLO LECTURA): package:rowmate/features/workout/series_tracker.dart, que solo importa
// core/models/interval_step.dart y core/models/routine.dart (y estos solo dart:).
// Secciones:
// - legacyCases: los 12 casos de test/series_tracker_test.dart, con sus alimentadores (_Feeder, :20-42, y _Raw, :133-152)
//   reescritos acá (son privados del test). El de RepSummary (LT-54) va como "summary".
// - scripts: las rutinas de F-5 con los remos de la spec §0.4, las pausas y el paso de 0 s. Las ENTRADAS del tracker (qué
//   paso, qué lectura y qué reloj recibe en cada segundo) salen del modelo de tiempo de la spec §0.4 con la regla de pasos de
//   la APP (DV-26: el paso por distancia cierra con los metros saneados), escrita acá; SeriesGoldenTests comprueba del lado de
//   C# que el motor de la app da exactamente estas entradas (DT5-07).
// Cada caso o guion: "steps" (índice -> [tipo, grupo, rep, repCount], con los pasos y las posiciones del legacy) y "ops", una
// tabla con una fila por llamada al tracker: op ("b" begin, "r" rebase o, en un update, el índice del paso como entero), raw,
// t (null en rebase) y el estado DESPUÉS de la llamada: cm (currentLapMeters), cs (currentLapSeconds: el double de Dart, o
// un entero JSON si es entero), rm (repMeters) y x, lo que cambió del resto del estado respecto de la fila anterior ("" si
// nada; la primera fila lo trae todo):
//   w<0|1> (isWorkStep) · L<segundos de cada vuelta, separados por coma> · s<inSeries 0|1>,<rep>,<repCount> ·
//   P<rep>:<total>:<trabajo>:<segundos>:<split o vacío>, separadas por "|" (las anteriores, la más reciente primero);
// separados por ";". Los segundos van con el toString() de Dart (ida y vuelta exacta).
// Se corre con tools/oracle/run-oracle.ps1 -Only series_vectors (guardia del legacy de §6.1).
import 'package:rowmate/core/models/interval_step.dart';
import 'package:rowmate/core/models/routine.dart';
import 'package:rowmate/features/workout/series_tracker.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/series_vectors.dart';

// ── Grabador: el tracker del legacy y una fila por llamada ──

class _Steps {
  final Map<int, List<Object?>> byIndex = {};

  void note(int index, IntervalStep step, StepPosition pos) {
    final row = <Object?>[step.type.name, pos.groupId, pos.rep, pos.repCount];
    final known = byIndex[index];
    if (known == null) {
      byIndex[index] = row;
      return;
    }
    for (var i = 0; i < row.length; i++) {
      if (known[i] != row[i]) {
        throw StateError('el paso $index cambió: $known -> $row');
      }
    }
  }

  List<List<Object?>> toList() {
    final keys = byIndex.keys.toList()..sort();
    return [
      for (final k in keys) [k, ...byIndex[k]!],
    ];
  }
}

class _Rec {
  final SeriesTracker tracker = SeriesTracker();
  final _Steps steps = _Steps();
  final List<List<Object?>> rows = [];
  String? _work;
  String? _laps;
  String? _series;
  String? _prev;

  void begin(int raw, int t) {
    tracker.begin(raw, elapsedSeconds: t);
    _row('b', raw, t);
  }

  void rebase(int raw) {
    tracker.rebase(raw);
    _row('r', raw, null);
  }

  void update(int index, IntervalStep step, StepPosition pos, int raw, int t) {
    steps.note(index, step, pos);
    tracker.update(stepIndex: index, step: step, position: pos, distanceMeters: raw, elapsedSeconds: t);
    _row(index, raw, t);
  }

  void _row(Object op, int raw, int? t) {
    final s = tracker;
    final work = 'w${s.isWorkStep ? 1 : 0}';
    final laps = 'L${s.laps.map((l) => l.seconds.toString()).join(',')}';
    final series = 's${s.inSeries ? 1 : 0},${s.rep},${s.repCount}';
    final prev = 'P${s.previousReps.map(_summary).join('|')}';
    final changed = <String>[];
    if (work != _work) changed.add(work);
    if (laps != _laps) changed.add(laps);
    if (series != _series) changed.add(series);
    if (prev != _prev) changed.add(prev);
    _work = work;
    _laps = laps;
    _series = series;
    _prev = prev;
    // Los números de vuelta son 1, 2, 3… en orden (el tracker los numera así): se comprueba acá para no guardarlos.
    for (var i = 0; i < s.laps.length; i++) {
      if (s.laps[i].number != i + 1) throw StateError('vuelta ${s.laps[i].number} en la posición $i');
    }
    rows.add([op, raw, t, s.currentLapMeters, _compact(s.currentLapSeconds), s.repMeters, changed.join(';')]);
  }

  // Un double entero va como entero JSON ("15" y no "15.0"): el mismo valor, más corto (el tope de 1 MiB por archivo). Los
  // demás, con el toString() de Dart, que vuelve exacto.
  static Object _compact(double x) {
    if (x.isFinite && x == x.truncateToDouble() && x.abs() < 1e15) return x.toInt();
    return x;
  }

  static String _summary(RepSummary r) {
    final split = r.workSplitSeconds;
    return '${r.rep}:${r.totalMeters}:${r.workMeters}:${r.workSeconds}:${split == null ? '' : split.toString()}';
  }

  Map<String, Object?> body() => {
        'steps': steps.toList(),
        'ops': table(['op', 'raw', 't', 'cm', 'cs', 'rm', 'x'], rows),
      };
}

// ── Los 12 casos de test/series_tracker_test.dart ──

const _g = 'g1';

IntervalStep _work(int meters, {String? group = _g}) => IntervalStep(
    routineId: 1, order: 0, type: StepType.work, distanceMeters: meters, groupId: group, groupRepeatCount: 5);

IntervalStep _cool(int seconds, {String? group = _g}) => IntervalStep(
    routineId: 1, order: 1, type: StepType.cooldown, durationSeconds: seconds, groupId: group, groupRepeatCount: 5);

StepPosition _pos(int rep, {String? group = _g}) => (groupId: group, rep: rep, repCount: group == null ? 1 : 5);

// :20-42: lecturas de 1 s a velocidad constante (t = 1, 2, 3…; distancia ⌊d⌋).
class _Feeder {
  _Feeder(this.rec) {
    rec.begin(0, 0);
  }

  final _Rec rec;
  int t = 0;
  double d = 0;

  void run(int stepIndex, IntervalStep step, StepPosition pos, {required int seconds, required double speed}) {
    for (var i = 0; i < seconds; i++) {
      t++;
      d += speed;
      rec.update(stepIndex, step, pos, d.floor(), t);
    }
  }
}

// :133-152: lecturas crudas de a 1 s.
class _Raw {
  _Raw(this.rec, this.step, this.pos, {int startRaw = 0}) {
    rec.begin(startRaw, 0);
  }

  final _Rec rec;
  final IntervalStep step;
  StepPosition pos;
  int t = 0;

  void feed(int raw, {int stepIndex = 0}) {
    t++;
    rec.update(stepIndex, step, pos, raw, t);
  }
}

Map<String, Object?> _legacyCase(String name, String legacy, void Function(_Rec rec) run) {
  final rec = _Rec();
  run(rec);
  return {'name': name, 'legacy': legacy, ...rec.body()};
}

List<Map<String, Object?>> _legacyCases() => [
      _legacyCase('una lectura de 0 dentro del paso no genera metros negativos ni dobles', 'test/series_tracker_test.dart:155', (rec) {
        final r = _Raw(rec, _work(2000), _pos(1));
        for (var i = 1; i <= 250; i++) {
          r.feed(i * 4);
        }
        r.feed(0);
        for (var raw = 1004; raw <= 1100; raw += 4) {
          r.feed(raw);
        }
        r.pos = _pos(2);
        r.feed(1104, stepIndex: 1);
      }),
      _legacyCase('un primer salto desde 0 no crea vueltas falsas', 'test/series_tracker_test.dart:180', (rec) {
        final r = _Raw(rec, _work(2000), _pos(1));
        r.feed(5000);
        for (var i = 1; i <= 140; i++) {
          r.feed(5000 + i * 4);
        }
      }),
      _legacyCase('un reinicio del monitor no genera valores negativos', 'test/series_tracker_test.dart:191', (rec) {
        final r = _Raw(rec, _work(2000), _pos(1));
        for (var i = 1; i <= 200; i++) {
          r.feed(i * 4);
        }
        for (var i = 0; i <= 60; i++) {
          r.feed(i * 4);
        }
      }),
      _legacyCase('rebase no cuenta el salto al reanudar', 'test/series_tracker_test.dart:211', (rec) {
        final r = _Raw(rec, _work(2000), _pos(1));
        for (var i = 1; i <= 5; i++) {
          r.feed(i * 4);
        }
        rec.rebase(9000);
        r.feed(9004);
      }),
      _legacyCase('interpola el tiempo de la vuelta entre lecturas', 'test/series_tracker_test.dart:46', (rec) {
        _Feeder(rec).run(0, _work(1000), _pos(1), seconds: 170, speed: 3);
      }),
      _legacyCase('dos vueltas dentro del mismo paso', 'test/series_tracker_test.dart:59', (rec) {
        _Feeder(rec).run(0, _work(2000), _pos(1), seconds: 220, speed: 5);
      }),
      _legacyCase('un paso de trabajo de menos de 500 m no genera vueltas', 'test/series_tracker_test.dart:65', (rec) {
        _Feeder(rec).run(0, _work(400), _pos(1), seconds: 80, speed: 5);
      }),
      _legacyCase('el enfriamiento no genera vueltas pero suma metros a la repetición', 'test/series_tracker_test.dart:71', (rec) {
        final f = _Feeder(rec);
        f.run(0, _work(500), _pos(1), seconds: 100, speed: 5);
        f.run(1, _cool(60), _pos(1), seconds: 60, speed: 2);
      }),
      _legacyCase('al cambiar de repetición guarda el resumen de la anterior', 'test/series_tracker_test.dart:85', (rec) {
        final f = _Feeder(rec);
        f.run(0, _work(500), _pos(1), seconds: 100, speed: 5);
        f.run(1, _cool(60), _pos(1), seconds: 60, speed: 2);
        f.run(2, _work(500), _pos(2), seconds: 10, speed: 5);
      }),
      _legacyCase('guarda solo las últimas 3 repeticiones, la más reciente primero', 'test/series_tracker_test.dart:102', (rec) {
        final f = _Feeder(rec);
        for (var rep = 1; rep <= 5; rep++) {
          f.run((rep - 1) * 2, _work(500), _pos(rep), seconds: 100, speed: 5);
          f.run((rep - 1) * 2 + 1, _cool(10), _pos(rep), seconds: 10, speed: 2);
        }
      }),
      _legacyCase('una serie nueva o un paso suelto vacían la comparación', 'test/series_tracker_test.dart:112', (rec) {
        final f = _Feeder(rec);
        f.run(0, _work(500), _pos(1), seconds: 100, speed: 5);
        f.run(1, _work(500), _pos(2), seconds: 10, speed: 5);
        f.run(2, _work(500, group: 'g2'), _pos(1, group: 'g2'), seconds: 10, speed: 5);
        f.run(3, _cool(60, group: null), _pos(1, group: null), seconds: 10, speed: 2);
      }),
    ];

// :126: el resumen sin metros de trabajo no tiene split.
List<Map<String, Object?>> _summaries() {
  const r = RepSummary(rep: 1, totalMeters: 100, workMeters: 0, workSeconds: 0);
  const s = RepSummary(rep: 1, totalMeters: 620, workMeters: 500, workSeconds: 100);
  return [
    {
      'name': 'una repetición sin metros de trabajo no tiene split',
      'legacy': 'test/series_tracker_test.dart:126',
      'rep': r.rep,
      'total': r.totalMeters,
      'work': r.workMeters,
      'seconds': f64(r.workSeconds),
      'split': r.workSplitSeconds == null ? null : f64(r.workSplitSeconds!),
    },
    {
      'name': 'el split de LT-51',
      'legacy': 'test/series_tracker_test.dart:99',
      'rep': s.rep,
      'total': s.totalMeters,
      'work': s.workMeters,
      'seconds': f64(s.workSeconds),
      'split': s.workSplitSeconds == null ? null : f64(s.workSplitSeconds!),
    },
  ];
}

// ── F-5 (spec §0.4): las rutinas, con los mismos ids de serie que Tests/Support/RoutineSeeds.F5 ──

const w = StepType.warmup;
const k = StepType.work;
const r = StepType.rest;
const c = StepType.cooldown;

// "g{milisegundos}_{n}" (routine_editor_screen.dart:111), con la fecha de creación de la plantilla: 2026-10-<day> 13:00 UTC
// más los minutos.
String _seriesId(int day, int minutes, int counter) =>
    'g${DateTime.utc(2026, 10, day, 13, minutes).millisecondsSinceEpoch}_$counter';

IntervalStep _s(StepType type, {int? dur, int? dist, String? g, int? rep}) => IntervalStep(
    routineId: 1, order: 0, type: type, durationSeconds: dur, distanceMeters: dist, groupId: g, groupRepeatCount: rep);

Routine _routine(String name, List<IntervalStep> steps) => Routine(
      name: name,
      createdAt: DateTime.utc(2026),
      steps: [for (var i = 0; i < steps.length; i++) steps[i].copyWith(order: i)],
    );

Map<String, Routine> _routines() {
  final series600 = _seriesId(29, 8, 0);
  final cincoReps = _seriesId(29, 5, 0);
  final dosA = _seriesId(29, 4, 0);
  final dosB = _seriesId(29, 4, 1);
  final piramide = _seriesId(22, 0, 0);
  final corta = _seriesId(26, 0, 0);
  final cero = _seriesId(29, 9, 0);
  return {
    'Series 600': _routine('Series 600', [
      _s(w, dur: 30),
      _s(k, dist: 600, g: series600, rep: 3),
      _s(r, dur: 20, g: series600, rep: 3),
      _s(c, dur: 30),
    ]),
    'Trabajo 1000': _routine('Trabajo 1000', [_s(k, dist: 1000)]),
    'Fondo 3000': _routine('Fondo 3000', [_s(k, dist: 3000)]),
    'Cinco reps': _routine('Cinco reps', [
      _s(k, dist: 500, g: cincoReps, rep: 5),
      _s(c, dur: 10, g: cincoReps, rep: 5),
    ]),
    'Dos series': _routine('Dos series', [
      _s(k, dur: 60, g: dosA, rep: 2),
      _s(k, dur: 60, g: dosB, rep: 2),
      _s(r, dur: 30),
    ]),
    // Las copias (de F-LISTA y F-4A): los mismos pasos que en Tests/Support/RoutineSeeds (sin los objetivos, que el tracker
    // no mira) y los mismos ids de serie.
    'Pirámide 500': _routine('Pirámide 500', [
      _s(w, dur: 600),
      _s(k, dist: 500, g: piramide, rep: 4),
      _s(k, dist: 250, g: piramide, rep: 4),
      _s(r, dur: 90, g: piramide, rep: 4),
      _s(c, dur: 300),
    ]),
    'Corta': _routine('Corta', [
      _s(k, dur: 10, g: corta, rep: 2),
      _s(r, dur: 5, g: corta, rep: 2),
    ]),
    // El paso de 0 s (spec AC-WKT-07-12): termina en el mismo segundo en que entra; el siguiente recibe su lectura en el
    // segundo siguiente.
    'Paso cero': _routine('Paso cero', [
      _s(w, dur: 3),
      _s(k, dur: 0, g: cero, rep: 2),
      _s(k, dist: 100, g: cero, rep: 2),
      _s(r, dur: 0, g: cero, rep: 2),
      _s(c, dur: 3),
    ]),
  };
}

// ── Los remos (spec §0.4): la distancia de L(k); null = esa lectura no llega ──

typedef _Rower = int? Function(int k);

final Map<String, _Rower> _rowers = {
  'R-4': (k) => 1000 + 4 * k,
  'R-3': (k) => 1000 + 3 * k,
  'R-4A': (k) => k <= 250 ? 1000 + 4 * k : 2000 + 5 * (k - 250),
  'R-4R': (k) => k >= 10 ? 4 * (k - 10) : 1000 + 4 * k,
  'R-4Z': (k) => k == 10 ? 0 : 1000 + 4 * k,
  'R-4D': (k) => k >= 101 && k <= 103 ? null : 1000 + 4 * k,
  'R-4P': (k) => 1000,
  'R-4X': (k) => 16777215,
};

// ── El modelo de tiempo de la spec §0.4 con la regla de pasos de la app (DV-26) ──
// - L(k) llega en el instante k − 0,5 (L(0) antes de iniciar); la lectura vigente en el instante w (ms) es la última L(k)
//   que llegó con k·1000 − 500 ≤ w (las que faltan no cuentan: sigue la anterior).
// - La sesión empieza en el instante 0 con begin(L vigente, 0). El segundo n del reloj activo cae en el instante n; una
//   pausa a los p ms corta el tramo (el segundo que cae justo en p se procesa antes) y al reanudar en q ms: rebase(L
//   vigente en q) y el segundo siguiente cae en q + 1000 (Q-58: la fracción se pierde).
// - Cada segundo: update(paso, L vigente, reloj activo) y después el fin del paso con la regla de la app: por tiempo,
//   segundos del paso ≥ duración; por distancia, metros SANEADOS del paso ≥ distancia (la misma suma que hace el tracker,
//   copiada acá: el motor de la app la lee del tracker); sin meta, nunca. El último paso termina la rutina.
// - until: con un remo parado, los pasos por distancia no terminan nunca: el guion corta en ese segundo.
Map<String, Object?> _script(String name, Routine template, String rowerName, {List<List<int>> pausesMs = const [], int? until}) {
  final rower = _rowers[rowerName]!;
  final steps = template.flattenedSteps;
  final positions = template.flattenedStepPositions;
  final rec = _Rec();

  int readingAt(int wMs) {
    for (var kk = (wMs + 500) ~/ 1000; kk >= 0; kk--) {
      final v = rower(kk);
      if (v != null) return v;
    }
    throw StateError('sin lecturas a los $wMs ms');
  }

  // La copia de la suma saneada (series_tracker.dart:118-127): la que lee el paso por distancia de la app.
  var lastRaw = 0.0;
  var meters = 0.0;
  var lastTime = 0.0;
  void sanitize(int raw, int t) {
    final dt = t - lastTime;
    final rawDelta = raw - lastRaw;
    final maxDelta = SeriesTracker.maxSpeedMetersPerSecond * (dt < 1 ? 1 : dt);
    final dd = (rawDelta < 0 || rawDelta > maxDelta) ? 0.0 : rawDelta;
    meters = meters + dd;
    lastRaw = raw.toDouble();
    lastTime = t.toDouble();
  }

  final raw0 = readingAt(0);
  rec.begin(raw0, 0);
  lastRaw = raw0.toDouble();
  meters = 0;
  lastTime = 0;

  final pauses = [...pausesMs]..sort((a, b) => a[0].compareTo(b[0]));
  var pauseIndex = 0;
  var segmentStart = 0;
  var segmentSeconds = 0;
  var total = 0;
  var stepIndex = 0;
  var stepElapsed = 0;
  var stepStartMeters = 0.0;
  var ended = 'finished';
  if (steps.isEmpty) throw StateError('$name: rutina sin pasos');
  while (true) {
    final due = segmentStart + (segmentSeconds + 1) * 1000;
    if (pauseIndex < pauses.length && pauses[pauseIndex][0] < due) {
      final resume = pauses[pauseIndex][1];
      pauseIndex++;
      final raw = readingAt(resume);
      rec.rebase(raw);
      lastRaw = raw.toDouble();
      segmentStart = resume;
      segmentSeconds = 0;
      continue;
    }

    segmentSeconds++;
    total++;
    stepElapsed++;
    final raw = readingAt(due);
    rec.update(stepIndex, steps[stepIndex], positions[stepIndex], raw, total);
    sanitize(raw, total);
    final step = steps[stepIndex];
    bool completed;
    if (step.durationSeconds != null) {
      completed = stepElapsed >= step.durationSeconds!;
    } else if (step.distanceMeters != null) {
      completed = meters - stepStartMeters >= step.distanceMeters!;
    } else {
      completed = false;
    }

    if (completed) {
      if (stepIndex + 1 < steps.length) {
        stepIndex++;
        stepElapsed = 0;
        stepStartMeters = meters;
      } else {
        break;
      }
    }

    if (until != null && total >= until) {
      ended = 'until';
      break;
    }
  }

  return {
    'name': name,
    'routine': template.name,
    'rower': rowerName,
    'pausesMs': pausesMs,
    'until': until,
    'end': ended,
    'seconds': total,
    ...rec.body(),
  };
}

List<Map<String, Object?>> _scripts() {
  final routines = _routines();
  final result = <Map<String, Object?>>[];
  // Con un remo parado (R-4P) o en sus extremos (R-4X, la distancia quieta) los pasos por distancia no terminan: el guion
  // corta en estos segundos (las rutinas por tiempo terminan solas).
  const stuckUntil = {'Series 600': 120, 'Cinco reps': 60, 'Pirámide 500': 660};
  for (final routine in ['Series 600', 'Cinco reps', 'Dos series', 'Pirámide 500', 'Corta']) {
    for (final rower in ['R-4', 'R-3', 'R-4A', 'R-4R', 'R-4Z', 'R-4D', 'R-4P', 'R-4X']) {
      final stuck = rower == 'R-4P' || rower == 'R-4X';
      result.add(_script('$routine · $rower', routines[routine]!, rower, until: stuck ? stuckUntil[routine] : null));
    }
  }

  // AC-WKT-07-06 y AC-WKT-18-04: Trabajo 1000 con los remos que fallan y con R-3; Fondo 3000 con R-4A.
  for (final rower in ['R-4', 'R-3', 'R-4R', 'R-4Z', 'R-4D']) {
    result.add(_script('Trabajo 1000 · $rower', routines['Trabajo 1000']!, rower));
  }
  result.add(_script('Fondo 3000 · R-4A', routines['Fondo 3000']!, 'R-4A'));

  // Las pausas: la larga de AC-WKT-07-07 (20,7 a 50,7 s), una de 1 s, una que cruza el cambio de paso (el trabajo terminó
  // a los 180 y el descanso recibe su primera lectura después de reanudar) y otra con un paso por tiempo (el segundo 60,
  // que cierra el trabajo, cae después de la pausa).
  result.add(_script('Trabajo 1000 · R-4 · pausa 20,7-50,7', routines['Trabajo 1000']!, 'R-4', pausesMs: [
    [20700, 50700]
  ]));
  result.add(_script('Series 600 · R-4 · pausa 100,2-101,2', routines['Series 600']!, 'R-4', pausesMs: [
    [100200, 101200]
  ]));
  result.add(_script('Series 600 · R-4 · pausa 180,3-200,3', routines['Series 600']!, 'R-4', pausesMs: [
    [180300, 200300]
  ]));
  result.add(_script('Dos series · R-4 · pausa 59,7-75,2', routines['Dos series']!, 'R-4', pausesMs: [
    [59700, 75200]
  ]));
  result.add(_script('Cinco reps · R-4D · dos pausas', routines['Cinco reps']!, 'R-4D', pausesMs: [
    [99300, 100300],
    [134700, 160700],
  ]));

  // El paso de 0 s.
  result.add(_script('Paso cero · R-4', routines['Paso cero']!, 'R-4'));
  return result;
}

List<Map<String, Object?>> _templates() {
  final out = <Map<String, Object?>>[];
  _routines().forEach((name, routine) {
    out.add({
      'name': name,
      'steps': [
        for (final s in routine.steps) [s.type.name, s.durationSeconds, s.distanceMeters, s.groupId, s.groupRepeatCount]
      ],
    });
  });
  return out;
}

void main(List<String> args) {
  final a = GoldenArgs.parse(args);
  writeGolden('series_tracker.json', {
    'header': header(generator, a.legacyCommit),
    'constants': {
      'lapMeters': SeriesTracker.lapMeters,
      'maxPreviousReps': SeriesTracker.maxPreviousReps,
      'maxSpeedMetersPerSecond': f64(SeriesTracker.maxSpeedMetersPerSecond),
    },
    'summaries': _summaries(),
    'legacyCases': _legacyCases(),
    'routines': _templates(),
    'scripts': _scripts(),
  }, args);
}
