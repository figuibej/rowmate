// Oraculo de la palada del legacy (07-remero-palada/02-tech-design §6.2): corre StrokeCycle, la geometria del BoatPainter y
// SceneState del legacy sin tocarlos y escribe Assets/RowMate/Tests/Golden/oracle/scene_stroke.json con ../../lib/golden.dart. Lo
// corre tools/oracle/run-oracle.ps1 -Stroke, que pasa la carpeta de salida y el commit del legacy en ROWMATE_ORACLE_OUT y
// ROWMATE_LEGACY_COMMIT, dentro de la guardia del legacy.
//
// Lo que se lee del legacy tal cual: StrokeCycle (las 1 001 fases), BoatPainter.seatZ / handle / bladeTip (publicos y estaticos) y
// SceneState.update con pasos de 0,05 s (24 spm; 24 spm y despues 0 spm; 24 spm y despues pausa) y de 1/60 s (24 spm): la fase, el
// cabeceo, la velocidad, la distancia y los charcos de cada paso.
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:rowmate/features/workout/scene/painters/boat_painter.dart';
import 'package:rowmate/features/workout/scene/scene_state.dart';
import 'package:rowmate/features/workout/scene/stroke_cycle.dart';

import '../../lib/golden.dart';

const String generator = 'tools/oracle/engine/test/stroke_vectors_test.dart';

String b(double x) => f64bits(x);

double phaseAt(int i) => i / 1000;

Map<String, Object?> strokeVectors(String commit) {
  final cycle = <List<Object?>>[];
  final boat = <List<Object?>>[];
  for (var i = 0; i <= 1000; i++) {
    final p = phaseAt(i);
    cycle.add([
      i, b(p), b(StrokeCycle.legs(p)), b(StrokeCycle.back(p)), b(StrokeCycle.arms(p)), b(StrokeCycle.torsoLean(p)),
      StrokeCycle.bladeInWater(p), b(StrokeCycle.oarSweep(p)),
    ]);
    final hl = BoatPainter.handle(-1, p);
    final hr = BoatPainter.handle(1, p);
    final tl = BoatPainter.bladeTip(-1, p);
    final tr = BoatPainter.bladeTip(1, p);
    boat.add([
      i, b(p), b(BoatPainter.seatZ(p)),
      b(hl.$1), b(hl.$2), b(hl.$3), b(hr.$1), b(hr.$2), b(hr.$3),
      b(tl.$1), b(tl.$2), b(tl.$3), b(tr.$1), b(tr.$2), b(tr.$3),
    ]);
  }

  final runs = <List<Object?>>[];
  void record(String name, SceneState s, int step) {
    final puddles = s.puddles.map((p) => '${b(p.x)}:${b(p.worldZ)}:${b(p.age)}').join(';');
    runs.add([name, step, b(s.strokePhase), b(s.pitch), b(s.speed), b(s.distance), b(s.time), s.puddles.length, puddles]);
  }

  void run(String name, List<(int steps, double dt, double pace, double spm, bool active)> legs) {
    final s = SceneState();
    var k = 0;
    for (final leg in legs) {
      for (var i = 0; i < leg.$1; i++) {
        s.update(dt: leg.$2, pace500m: leg.$3, spm: leg.$4, isActive: leg.$5);
        k++;
        record(name, s, k);
      }
    }
  }

  run('rowing24', [(200, 0.05, 107, 24, true)]);
  run('closing', [(10, 0.05, 107, 24, true), (120, 0.05, 107, 0, true)]);
  run('paused', [(10, 0.05, 107, 24, true), (40, 0.05, 107, 24, false)]);
  run('frames60', [(360, 1 / 60, 125, 24, true)]);

  return {
    'header': header(generator, commit),
    'cycle': table(['i', 'p', 'legs', 'back', 'arms', 'torsoLean', 'bladeIn', 'oarSweep'], cycle),
    'boat': table(['i', 'p', 'seatZ', 'hlx', 'hly', 'hlz', 'hrx', 'hry', 'hrz', 'tlx', 'tly', 'tlz', 'trx', 'try', 'trz'], boat),
    'runs': table(['run', 'step', 'phase', 'pitch', 'speed', 'distance', 'time', 'puddles', 'list'], runs),
  };
}

void main() {
  test('vectores de la palada del legacy (It. 7, scene_stroke.json)', () {
    final outDir = Platform.environment['ROWMATE_ORACLE_OUT'];
    final commit = Platform.environment['ROWMATE_LEGACY_COMMIT'];
    if (outDir == null || commit == null) {
      fail('faltan ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT: correlo con tools/oracle/run-oracle.ps1 -Stroke');
    }
    // Control antes de escribir: los casos de stroke_cycle_test.dart y scene_state_test.dart.
    expect(StrokeCycle.legs(0.36), 1);
    expect(StrokeCycle.bladeInWater(0.39), isFalse);
    writeGolden('scene_stroke.json', strokeVectors(commit), ['--out', outDir, '--legacy-commit', commit]);
  });
}
