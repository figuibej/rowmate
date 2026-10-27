// Vectores de la física del remo simulado (WP-B, 02-tech-design §6.3) -> Assets/RowMate/Tests/Golden/oracle/simulator.json.
// Importa core/dev/rowing_simulator.dart del legacy en solo lectura (solo importa dart: y core/models/rowing_data.dart).
// Secciones: valores iniciales y topes; paceForWatts/caloriesPerSecond para W en [1; 600] (bits); sin ruido a 150 W/24 spm
// los 600 ticks; sin ruido a 30/14, 203/30 y 500/40 (120 ticks); parar y reanudar (AC-BLE-14-06); y la reproducción del
// ruido: SeqRandom implementa Random con una secuencia fija de nextDouble() (guardada por bits en cada tick que la
// consume), así C# con SequenceRandom tiene que dar los mismos ticks (fórmulas y orden de llamadas, AC-BLE-14-09).
// Se corre con tools/oracle/run-oracle.ps1 -Only simulator_vectors (guardia del legacy de §6.1).
import 'dart:math';

import 'package:rowmate/core/dev/rowing_simulator.dart';
import 'package:rowmate/core/models/rowing_data.dart';

import '../lib/golden.dart';

const generator = 'tools/oracle/bin/simulator_vectors.dart';

/// Random con una secuencia fija de nextDouble(); registra cuántos valores se consumieron.
class SeqRandom implements Random {
  final List<double> values;
  int calls = 0;

  SeqRandom(this.values);

  @override
  double nextDouble() {
    if (calls >= values.length) throw StateError('SeqRandom: se agotó la secuencia');
    return values[calls++];
  }

  @override
  int nextInt(int max) => throw UnsupportedError('SeqRandom.nextInt');

  @override
  bool nextBool() => throw UnsupportedError('SeqRandom.nextBool');
}

List<Object?> tickCells(RowingData d, RowingSimulator sim) => [
      f64bits(d.strokeRate),
      d.strokeCount,
      d.distanceMeters,
      d.pace500mSeconds,
      d.powerWatts,
      d.totalCalories,
      d.heartRate,
      d.elapsedSeconds,
      sim.heartRate,
    ];

const tickColumns = [
  'strokeRate', 'strokeCount', 'distanceMeters', 'pace500mSeconds', 'powerWatts', 'totalCalories', 'heartRate',
  'elapsedSeconds', 'exposedHeartRate',
];

Map<String, Object?> tickObject(RowingData d, RowingSimulator sim) => {
      'strokeRate': f64(d.strokeRate),
      'strokeCount': d.strokeCount,
      'distanceMeters': d.distanceMeters,
      'pace500mSeconds': d.pace500mSeconds,
      'powerWatts': d.powerWatts,
      'totalCalories': d.totalCalories,
      'heartRate': d.heartRate,
      'elapsedSeconds': d.elapsedSeconds,
      'exposedHeartRate': sim.heartRate,
    };

void main(List<String> args) {
  final a = GoldenArgs.parse(args);

  // ---- Valores iniciales (AC-BLE-14-01) ----
  final fresh = RowingSimulator(noise: false);
  final initial = [
    {
      'targetWatts': fresh.targetWatts,
      'targetSpm': fresh.targetSpm,
      'rowing': fresh.rowing,
      'noise': fresh.noise,
      'heartRate': fresh.heartRate,
      'minWatts': RowingSimulator.minWatts,
      'maxWatts': RowingSimulator.maxWatts,
      'minSpm': RowingSimulator.minSpm,
      'maxSpm': RowingSimulator.maxSpm,
    }
  ];

  // ---- Topes (AC-BLE-14-02) ----
  final clamp = <Map<String, Object?>>[];
  for (final v in [-5, 0, 1, 29, 30, 31, 150, 203, 499, 500, 501, 9999]) {
    final sim = RowingSimulator(noise: false)..targetWatts = v;
    clamp.add({'property': 'targetWatts', 'value': v, 'result': sim.targetWatts});
  }
  for (final v in [-5, 0, 1, 13, 14, 15, 24, 39, 40, 41, 99]) {
    final sim = RowingSimulator(noise: false)..targetSpm = v;
    clamp.add({'property': 'targetSpm', 'value': v, 'result': sim.targetSpm});
  }

  // ---- Fórmulas de Concept2 (AC-BLE-14-03/04): W en [1; 600] ----
  final concept2 = <List<Object?>>[];
  for (var w = 1; w <= 600; w++) {
    final pace = RowingSimulator.paceForWatts(w);
    concept2.add([w, f64bits(pace), pace.round(), f64bits(RowingSimulator.caloriesPerSecond(w))]);
  }

  // ---- Sin ruido a 150 W / 24 spm: 600 ticks (AC-BLE-14-05) ----
  final noNoise600 = <List<Object?>>[];
  final sim600 = RowingSimulator(noise: false)
    ..targetWatts = 150
    ..targetSpm = 24;
  for (var t = 1; t <= 600; t++) {
    final d = sim600.tick();
    noNoise600.add([t, ...tickCells(d, sim600)]);
  }

  // ---- Sin ruido a 30/14, 203/30 y 500/40: 120 ticks cada uno ----
  final noNoiseTargets = <List<Object?>>[];
  for (final target in [
    [30, 14],
    [203, 30],
    [500, 40],
  ]) {
    final sim = RowingSimulator(noise: false)
      ..targetWatts = target[0]
      ..targetSpm = target[1];
    for (var t = 1; t <= 120; t++) {
      final d = sim.tick();
      noNoiseTargets.add([target[0], target[1], t, ...tickCells(d, sim)]);
    }
  }

  // ---- Parar y reanudar (AC-BLE-14-06): 11 ticks remando, 1 sin remar, 1 remando ----
  final stopResume = <Map<String, Object?>>[];
  final simStop = RowingSimulator(noise: false);
  for (var t = 1; t <= 13; t++) {
    simStop.rowing = t != 12;
    final d = simStop.tick();
    stopResume.add({'tick': t, 'rowing': simStop.rowing, 'out': tickObject(d, simStop)});
  }

  // ---- Reproducción del ruido (AC-BLE-14-09) ----
  // Secuencias: Random(1414) para 200 W / 24 spm (150 ticks remando, 20 sin remar, 30 remando) y extremos
  // (0.0 y el mayor double menor que 1) con 30/14 y 500/40 (20 ticks remando cada uno).
  final noiseReplay = <List<Object?>>[];
  final seeded = Random(1414);
  final cases = <List<Object>>[
    ['200/24', 200, 24, 200, List<double>.generate(400, (_) => seeded.nextDouble())],
    [
      '30/14 extremos', 30, 14, 20,
      [for (var i = 0; i < 40; i++) [0.0, 0.9999999999999999, 0.5, 0.25, 0.75][i % 5]],
    ],
    [
      '500/40 extremos', 500, 40, 20,
      [for (var i = 0; i < 40; i++) [0.9999999999999999, 0.0, 0.625, 0.475, 0.5][i % 5]],
    ],
  ];
  for (final c in cases) {
    final name = c[0] as String;
    final tw = c[1] as int;
    final ts = c[2] as int;
    final ticks = c[3] as int;
    final random = SeqRandom(c[4] as List<double>);
    final sim = RowingSimulator(random: random)
      ..targetWatts = tw
      ..targetSpm = ts;
    for (var t = 1; t <= ticks; t++) {
      sim.rowing = name != '200/24' || t <= 150 || t > 170;
      final before = random.calls;
      final d = sim.tick();
      final consumed = random.values.sublist(before, random.calls);
      if (consumed.length != (sim.rowing ? 2 : 0)) {
        throw StateError('$name tick $t: consumió ${consumed.length} valores');
      }
      noiseReplay.add([
        name, tw, ts, t, sim.rowing ? 1 : 0,
        consumed.isEmpty ? null : f64bits(consumed[0]),
        consumed.isEmpty ? null : f64bits(consumed[1]),
        ...tickCells(d, sim),
      ]);
    }
  }

  writeGolden('simulator.json', {
    'header': header(generator, a.legacyCommit),
    'initial': initial,
    'clamp': clamp,
    'concept2': table(['watts', 'pace', 'paceRounded', 'kcalPerSecond'], concept2),
    'noNoise600': table(['tick', ...tickColumns], noNoise600),
    'noNoiseTargets': table(['targetWatts', 'targetSpm', 'tick', ...tickColumns], noNoiseTargets),
    'stopResume': stopResume,
    'noiseReplay': table(['case', 'targetWatts', 'targetSpm', 'tick', 'rowing', 'rWatts', 'rSpm', ...tickColumns], noiseReplay),
  }, args);
}
