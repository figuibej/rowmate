// Vectores de pasos y rutinas (WP-C, 02-tech-design §6.3) -> Assets/RowMate/Tests/Golden/oracle/routines.json.
// Corre el código del legacy (SOLO LECTURA): package:rowmate/core/models/interval_step.dart y routine.dart
// (solo importan dart: y rutas relativas de core/models).
// Secciones:
// - templates: plantillas con nombre (T1, casos 1-8 de RUT §5.3, AC-RUT-04-05, AC-RUT-06-10 a -13 y las 9 de
//   AC-RUT-05-02). Para cada una: steps (IntervalStep.toMap()), flattenedSteps (toMap() de cada paso expandido),
//   positions ([groupId, rep, repCount]), labels ([durationLabel, targetLabel] de cada paso expandido), summary
//   legacy y los conteos total/work/rest.
// - durationLabel / targetLabel: los casos de AC-RUT-02-04, AC-RUT-03 y AC-RUT-04 (incluidos negativos y progresiones).
// - durationSweep (tabla): durationLabel para durationSeconds de -200 a 7300.
// - targetFuzz (tabla): targetLabel de 400 pasos aleatorios de Random(2027) (cualquier campo puede faltar).
// - fuzzTemplates / fuzzFlattened / fuzzCounts (tablas): 80 plantillas aleatorias de Random(4242) con series
//   contiguas y no contiguas, repeticiones nulas, 0 y negativas, y progresiones; con la columna "case".
// Se corre con tools/oracle/run-oracle.ps1 -Only routine_vectors (guardia del legacy de §6.1).
import 'dart:math';

import 'package:rowmate/core/models/interval_step.dart';
import 'package:rowmate/core/models/routine.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/routine_vectors.dart';

// Paso de plantilla: routineId 1 y order = posición en la plantilla (la asigna _t).
IntervalStep _s(StepType type,
        {int? dur,
        int? dist,
        int? wMin,
        int? wMax,
        int? spm,
        int? split,
        String? g,
        int? rep,
        int? pSpm,
        int? pW,
        int? pSplit}) =>
    IntervalStep(
      routineId: 1,
      order: 0,
      type: type,
      durationSeconds: dur,
      distanceMeters: dist,
      targetWattsMin: wMin,
      targetWattsMax: wMax,
      targetSpm: spm,
      targetSplitSeconds: split,
      groupId: g,
      groupRepeatCount: rep,
      progressionSpm: pSpm,
      progressionWatts: pW,
      progressionSplitSeconds: pSplit,
    );

List<IntervalStep> _t(List<IntervalStep> steps) => [
      for (var i = 0; i < steps.length; i++) steps[i].copyWith(order: i),
    ];

const w = StepType.warmup;
const k = StepType.work;
const r = StepType.rest;
const c = StepType.cooldown;

Map<String, List<IntervalStep>> _templates() => {
      // F-RUT-06 / F-RUT-07 (01-po-spec §4.11, §4.12; T1 = test/routine_positions_test.dart:7-16)
      'T1': _t([
        _s(w, dur: 300),
        _s(k, dist: 500, g: 'a', rep: 3),
        _s(c, dur: 60, g: 'a', rep: 3),
        _s(c, dur: 300),
      ]),
      'case1': _t([
        _s(k, dur: 300, spm: 18, pSpm: 2, g: 'a', rep: 3),
        _s(r, dur: 120, g: 'a', rep: 3),
      ]),
      'case2': _t([_s(k, dur: 300, wMin: 140, wMax: 160, pW: 10, g: 'a', rep: 3)]),
      'case3': _t([_s(k, dur: 300, wMin: 150, spm: 22, pW: 10, pSpm: 2, g: 'a', rep: 3)]),
      'case4': _t([_s(k, dur: 300, split: 120, pSplit: -2, g: 'a', rep: 3)]),
      'case5': _t([_s(k, dur: 300, pSpm: 2, g: 'a', rep: 3)]),
      'case6': _t([_s(k, dur: 300, spm: 20, pSpm: 2)]),
      'case7a': _t([_s(k, dur: 60, g: 'a')]),
      'case7b': _t([
        _s(w, dur: 60),
        _s(k, dur: 60, g: 'a', rep: 0),
        _s(c, dur: 60),
      ]),
      'case7c': _t([_s(k, dur: 60, g: 'a', rep: -1)]),
      'case7d': _t([_s(k, dur: 60, g: 'a', rep: 0)]),
      'case8': _t([
        _s(k, dur: 60, g: 'a', rep: 2),
        _s(k, dur: 30, g: 'b', rep: 3),
      ]),
      'rut04_05': _t([_s(k, dur: 300, wMin: 140, wMax: 160, pW: 10, g: 'a', rep: 2)]),
      'rut06_10': _t([
        _s(k, dur: 60, g: 'a', rep: 2),
        _s(r, dur: 30, g: 'a', rep: 5),
      ]),
      'rut06_11': _t([
        _s(k, dur: 60, g: 'a', rep: 2),
        _s(r, dur: 30),
        _s(k, dur: 60, g: 'a', rep: 2),
      ]),
      'rut06_12': _t([_s(k, dur: 60, spm: 4, pSpm: -3, g: 'a', rep: 3)]),
      'rut06_13': _t([
        _s(k, dur: 90, dist: 400, wMin: 100, wMax: 120, spm: 20, split: 130, g: 'g', rep: 2, pSpm: 1, pW: 5, pSplit: -1),
        _s(r, dist: 250, g: 'g', rep: 2, pSpm: 3, pW: -4, pSplit: 2),
      ]),
      // F-RUT-05 (AC-RUT-05-02, caso 11)
      'sum_warmup': _t([_s(w, dur: 300)]),
      'sum_warmup_cooldown': _t([_s(w, dur: 300), _s(c, dur: 300)]),
      'sum_rest_rest': _t([_s(r, dur: 60), _s(r, dur: 60)]),
      'sum_work': _t([_s(k, dur: 60)]),
      'sum_warmup_series3_cooldown': _t([
        _s(w, dur: 300),
        _s(k, dist: 500, g: 'a', rep: 3),
        _s(r, dur: 60, g: 'a', rep: 3),
        _s(c, dur: 300),
      ]),
      'sum_2work_1rest': _t([_s(k, dur: 60), _s(k, dur: 60), _s(r, dur: 60)]),
      'sum_1work_2rest': _t([_s(k, dur: 60), _s(r, dur: 60), _s(r, dur: 60)]),
      'sum_4work_3rest': _t([
        _s(k, dur: 60),
        _s(r, dur: 30),
        _s(k, dur: 60),
        _s(r, dur: 30),
        _s(k, dur: 60),
        _s(r, dur: 30),
        _s(k, dur: 60),
      ]),
      'sum_empty': <IntervalStep>[],
    };

Map<String, Object?> _stepMap(IntervalStep s) => s.toMap();

List<IntervalStep> _durationCases() => [
      for (final d in [0, 45, 59, 60, 61, 90, 120, 3600, 3725, 7200, -30, -60, -90, -1, -59, -61, -120]) _s(k, dur: d),
      for (final m in [500, 2000, 10000, 0, -5]) _s(k, dist: m),
      _s(k),
      _s(k, dur: 90, dist: 500),
    ];

List<IntervalStep> _targetCases() => [
      _s(k),
      _s(k, wMin: 150),
      _s(k, wMin: 120, wMax: 180),
      _s(k, wMax: 180),
      _s(k, spm: 22),
      _s(k, split: 59),
      _s(k, split: 125),
      _s(k, split: 600),
      _s(k, split: 6000),
      _s(k, wMin: 120, wMax: 180, spm: 22, split: 125),
      _s(k, pSpm: 2),
      _s(k, spm: 20, pSpm: -1),
      _s(k, pW: 10),
      _s(k, pSplit: 3),
      _s(k, pSpm: 0, pW: 0, pSplit: 0),
      _s(k, wMax: 180, pW: 10),
      _s(k, wMin: 120, wMax: 180, spm: 22, split: 125, pSpm: 2, pW: 5, pSplit: -2),
      _s(k, split: 0),
      _s(k, split: -5),
      _s(k, split: -65),
      _s(k, wMin: 0),
      _s(k, wMin: -10, wMax: 5),
      _s(k, spm: 0),
      _s(k, wMin: 150, wMax: 170, pW: 10),
      _s(k, dur: 300, spm: 18, pSpm: 2),
    ];

int? _maybe(Random rnd, double pNull, int min, int max) =>
    rnd.nextDouble() < pNull ? null : min + rnd.nextInt(max - min + 1);

void main(List<String> args) {
  final a = GoldenArgs.parse(args);

  final templates = <Map<String, Object?>>[];
  _templates().forEach((name, steps) {
    final routine = Routine(name: name, createdAt: DateTime.utc(2026), steps: steps);
    final flat = routine.flattenedSteps;
    final positions = routine.flattenedStepPositions;
    if (positions.length != flat.length) {
      throw StateError('$name: posiciones y pasos expandidos no están alineados');
    }
    templates.add({
      'name': name,
      'steps': [for (final s in steps) _stepMap(s)],
      'flattenedSteps': [for (final s in flat) _stepMap(s)],
      'positions': [for (final p in positions) [p.groupId, p.rep, p.repCount]],
      'labels': [for (final s in flat) [s.durationLabel, s.targetLabel]],
      'summary': routine.summary,
      'total': steps.length,
      'work': steps.where((s) => s.type == StepType.work).length,
      'rest': steps.where((s) => s.type == StepType.rest).length,
    });
  });

  final durationLabel = [
    for (final s in _durationCases())
      {'durationSeconds': s.durationSeconds, 'distanceMeters': s.distanceMeters, 'label': s.durationLabel},
  ];
  final targetLabel = [
    for (final s in _targetCases()) {'step': _stepMap(s), 'label': s.targetLabel},
  ];

  final durationSweep = <List<Object?>>[
    for (var d = -200; d <= 7300; d++) [d, _s(k, dur: d).durationLabel],
  ];

  // targetLabel de pasos aleatorios: cada campo falta con probabilidad 0,35.
  final rndT = Random(2027);
  final targetFuzz = <List<Object?>>[];
  for (var i = 0; i < 400; i++) {
    final s = _s(k,
        wMin: _maybe(rndT, 0.35, -20, 400),
        wMax: _maybe(rndT, 0.35, -20, 400),
        spm: _maybe(rndT, 0.35, -5, 45),
        split: _maybe(rndT, 0.35, -200, 6100),
        pSpm: _maybe(rndT, 0.35, -5, 5),
        pW: _maybe(rndT, 0.35, -20, 20),
        pSplit: _maybe(rndT, 0.35, -10, 10));
    targetFuzz.add([
      s.targetWattsMin, s.targetWattsMax, s.targetSpm, s.targetSplitSeconds,
      s.progressionSpm, s.progressionWatts, s.progressionSplitSeconds, s.targetLabel,
    ]);
  }

  // Plantillas aleatorias para la expansión, las posiciones y el resumen.
  final rnd = Random(4242);
  final fuzzTemplates = <List<Object?>>[];
  final fuzzFlattened = <List<Object?>>[];
  final fuzzCounts = <List<Object?>>[];
  const types = StepType.values;
  const groups = [null, null, 'a', 'b', 'c'];
  const repeats = [null, -1, 0, 1, 2, 3, 4];
  List<Object?> row(int caseId, int index, IntervalStep s) => [
        caseId, index, s.order, s.type.name, s.durationSeconds, s.distanceMeters, s.targetWattsMin, s.targetWattsMax,
        s.targetSpm, s.targetSplitSeconds, s.groupId, s.groupRepeatCount, s.progressionSpm, s.progressionWatts,
        s.progressionSplitSeconds,
      ];
  for (var caseId = 0; caseId < 80; caseId++) {
    final n = rnd.nextInt(8);
    final steps = <IntervalStep>[];
    for (var i = 0; i < n; i++) {
      steps.add(IntervalStep(
        routineId: 1,
        order: i,
        type: types[rnd.nextInt(types.length)],
        durationSeconds: _maybe(rnd, 0.3, -30, 4000),
        distanceMeters: _maybe(rnd, 0.6, 0, 10000),
        targetWattsMin: _maybe(rnd, 0.5, -10, 300),
        targetWattsMax: _maybe(rnd, 0.6, -10, 400),
        targetSpm: _maybe(rnd, 0.5, 0, 40),
        targetSplitSeconds: _maybe(rnd, 0.6, -60, 300),
        groupId: groups[rnd.nextInt(groups.length)],
        groupRepeatCount: repeats[rnd.nextInt(repeats.length)],
        progressionSpm: _maybe(rnd, 0.5, -3, 3),
        progressionWatts: _maybe(rnd, 0.5, -10, 10),
        progressionSplitSeconds: _maybe(rnd, 0.5, -3, 3),
      ));
    }
    final routine = Routine(name: 'fuzz$caseId', createdAt: DateTime.utc(2026), steps: steps);
    for (var i = 0; i < steps.length; i++) {
      fuzzTemplates.add(row(caseId, i, steps[i]));
    }
    final flat = routine.flattenedSteps;
    final positions = routine.flattenedStepPositions;
    if (positions.length != flat.length) {
      throw StateError('fuzz$caseId: posiciones y pasos expandidos no están alineados');
    }
    for (var i = 0; i < flat.length; i++) {
      fuzzFlattened.add([...row(caseId, i, flat[i]), positions[i].groupId, positions[i].rep, positions[i].repCount]);
    }
    fuzzCounts.add([
      caseId,
      steps.length,
      steps.where((s) => s.type == StepType.work).length,
      steps.where((s) => s.type == StepType.rest).length,
      flat.length,
      routine.summary,
    ]);
  }

  const stepColumns = [
    'case', 'index', 'order', 'type', 'durationSeconds', 'distanceMeters', 'targetWattsMin', 'targetWattsMax',
    'targetSpm', 'targetSplitSeconds', 'groupId', 'groupRepeatCount', 'progressionSpm', 'progressionWatts',
    'progressionSplitSeconds',
  ];

  writeGolden('routines.json', {
    'header': header(generator, a.legacyCommit),
    'templates': templates,
    'durationLabel': durationLabel,
    'targetLabel': targetLabel,
    'durationSweep': table(['durationSeconds', 'label'], durationSweep),
    'targetFuzz': table([
      'targetWattsMin', 'targetWattsMax', 'targetSpm', 'targetSplitSeconds',
      'progressionSpm', 'progressionWatts', 'progressionSplitSeconds', 'label',
    ], targetFuzz),
    'fuzzTemplates': table(stepColumns, fuzzTemplates),
    'fuzzFlattened': table([...stepColumns, 'posGroupId', 'posRep', 'posRepCount'], fuzzFlattened),
    'fuzzCounts': table(['case', 'total', 'work', 'rest', 'flattened', 'summary'], fuzzCounts),
  }, args);
}
