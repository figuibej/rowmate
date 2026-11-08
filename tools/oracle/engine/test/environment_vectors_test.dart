// Oráculo de los escenarios río, costa y regata del legacy (08-entornos-selector/02-tech-design §7.2): corre el código de la
// escena del legacy sin tocarlo y escribe Assets/RowMate/Tests/Golden/oracle/scene_{river,coast,regatta,horizons}.json con
// ../../lib/golden.dart. Lo corre tools/oracle/run-oracle.ps1 -Environments, que pasa la carpeta de salida y el commit del
// legacy en ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT, dentro de la guardia del legacy.
//
// Lo que se lee del legacy tal cual: Environment.propsForSegment / visibleProps de los tres escenarios (y de un Environment de
// prueba con los topes 60 y 200), WaterPainter.uniforms (LT-124), Environment.tintWater, horizonColor y lo que dibuja
// paintHorizon (los rectángulos de la regata y del skyline del río, con un Canvas que anota).
// Lo que es privado del legacy y se copia con su cita: el skyline del río (river_environment.dart:46-71) y el acantilado
// lejano de la costa (coast_environment.dart:51-66), comparados antes con lo que dibuja el legacy donde se puede.
import 'dart:io';
import 'dart:math' as math;
import 'dart:typed_data';

import 'package:flutter/material.dart' hide TimeOfDay;
import 'package:flutter_test/flutter_test.dart';
import 'package:rowmate/features/workout/scene/environment.dart';
import 'package:rowmate/features/workout/scene/painters/water_painter.dart';
import 'package:rowmate/features/workout/scene/scene_camera.dart';
import 'package:rowmate/features/workout/scene/scene_state.dart';
import 'package:rowmate/features/workout/scene/time_of_day.dart';

import '../../lib/golden.dart';

const String generator = 'tools/oracle/engine/test/environment_vectors_test.dart';

const sizes = <(String, double, double)>[
  ('V390', 390, 844),
  ('H844', 844, 390),
  ('TV', 411, 914),
  ('P360', 360, 800),
  ('TH', 914, 411),
  ('L640', 640, 360),
  ('W1280', 1280, 720),
];

String b(double x) => f64bits(x);

List<double> rgb(Color c) => [c.r, c.g, c.b];

// ── Bytes canónicos y FNV-1a (los mismos que scene_vectors_test.dart y SceneGolden.cs) ─────────────────────────────────

final ByteData _canon = ByteData(8);

List<int> canonicalBytes(ShoreProp p) {
  final out = <int>[p.kind.index];
  for (final v in [p.x, p.z, p.scale]) {
    _canon.setFloat64(0, v);
    for (var i = 0; i < 8; i++) {
      out.add(_canon.getUint8(i));
    }
  }
  _canon.setInt32(0, p.seed);
  for (var i = 0; i < 4; i++) {
    out.add(_canon.getUint8(i));
  }
  return out;
}

String fnv1a64(List<ShoreProp> props) {
  var h = 0xcbf29ce484222325;
  const prime = 0x100000001b3;
  for (final p in props) {
    for (final byte in canonicalBytes(p)) {
      h ^= byte;
      h *= prime;
    }
  }
  return (h >>> 32).toRadixString(16).padLeft(8, '0') + (h & 0xFFFFFFFF).toRadixString(16).padLeft(8, '0');
}

List<Object?> propRow(ShoreProp p) => [p.kind.name, b(p.x), b(p.z), b(p.scale), p.seed];

/// Olvida la caché estática del legacy para un índice de escenario: visibleProps lejos evicta todo lo de atrás
/// (environment.dart:94, :116-120). Hace falta porque el escenario de prueba comparte el índice de la regata.
void forget(Environment env) => env.visibleProps(1e7);

// ── Escenario de prueba de los topes 60 y 200 (AC-SCN-14-05, cambia; AC-SCN-14-09) ─────────────────────────────────────

/// Como el CapEnvironment de scene_vectors_test.dart pero con el tope y las densidades como parámetros: boyas de sobra en los
/// segmentos 0 a 19 y hitos de sobra desde el 20, con z distintas alrededor de cualquier corte. El mismo escenario está en C#
/// (Tests/Support/SceneSeeds.cs, ToppedEnvironment): cualquier cambio va en los dos lados. Usa el índice de la regata.
class ToppedEnvironment extends Environment {
  const ToppedEnvironment(this.cap, this.heavyBuoys, this.lightBuoys, this.heavyMarks, this.lightMarks);

  final int cap;
  final int heavyBuoys;
  final int lightBuoys;
  final int heavyMarks;
  final int lightMarks;

  static const landmarks = [
    PropKind.grandstand, PropKind.finishTower, PropKind.flag, PropKind.distanceMarker, PropKind.lamp, PropKind.pine,
  ];

  @override
  EnvironmentId get id => EnvironmentId.regatta;
  @override
  int get maxVisibleProps => cap;
  @override
  double get waveAmplitude => 0;
  @override
  double get waveScale => 1;

  @override
  List<ShoreProp> generate(int k, math.Random rnd) {
    final out = <ShoreProp>[];
    final z0 = k * Environment.segmentLength;
    final buoyHeavy = k < 20;
    final buoys = buoyHeavy ? heavyBuoys : lightBuoys;
    for (var i = 0; i < buoys; i++) {
      final z = z0 + i * (50 / buoys) + 0.25 + rnd.nextDouble() * 1.5;
      out.add(ShoreProp(x: (i.isEven ? -1 : 1) * (1 + rnd.nextDouble() * 6), z: z, kind: PropKind.laneBuoy, seed: i));
    }
    final marks = buoyHeavy ? heavyMarks : lightMarks;
    for (var i = 0; i < marks; i++) {
      final z = z0 + i * (50 / marks) + 2.0 + rnd.nextDouble() * 2.0;
      final kind = landmarks[rnd.nextInt(landmarks.length)];
      out.add(ShoreProp(x: (i.isEven ? -1 : 1) * (10 + rnd.nextDouble() * 8), z: z, kind: kind, seed: k * 100 + i));
    }
    return out;
  }

  @override
  void paintHorizon(Canvas canvas, Size size, SceneCamera cam, ScenePalette p, double distance, double time) {}
}

const visibleDistances = <double>[0, 1, 49.5, 50, 920, 1234, 5000, 42195];
const capDistances = <double>[0, 137.5, 600, 850.25, 1200, 1630.5];

Map<String, Object?> cappedVectors(ToppedEnvironment env) {
  forget(env);
  final segments = <List<Object?>>[];
  for (var k = 0; k <= 41; k++) {
    final props = env.propsForSegment(k);
    for (var i = 0; i < props.length; i++) {
      segments.add([k, i, ...propRow(props[i])]);
    }
  }
  final visible = <List<Object?>>[];
  for (final d in capDistances) {
    final props = env.visibleProps(d);
    expect(props.length, env.cap);
    for (var i = 0; i < props.length; i++) {
      visible.add([b(d), i, ...propRow(props[i])]);
    }
  }
  forget(env);
  return {
    'capSegments': table(['k', 'i', 'kind', 'x', 'z', 'scale', 'seed'], segments),
    'capVisible': table(['distance', 'i', 'kind', 'x', 'z', 'scale', 'seed'], visible),
  };
}

// ── scene_{river,coast,regatta}.json ───────────────────────────────────────────────────────────────────────────────────

Map<String, Object?> environmentVectors(String commit, EnvironmentId id, ToppedEnvironment? topped) {
  final env = Environment.of(id);
  forget(env);
  final segments = <List<Object?>>[];
  for (var k = 0; k < 100; k++) {
    final props = env.propsForSegment(k);
    for (var i = 0; i < props.length; i++) {
      segments.add([k, i, ...propRow(props[i])]);
    }
  }
  final digests = <List<Object?>>[];
  for (var k = 0; k <= 2000; k++) {
    final props = env.propsForSegment(k);
    digests.add([k, props.length, fnv1a64(props)]);
  }
  final visible = <List<Object?>>[];
  for (final d in visibleDistances) {
    final props = env.visibleProps(d);
    expect(props.length, lessThanOrEqualTo(env.maxVisibleProps));
    for (var i = 0; i < props.length; i++) {
      visible.add([b(d), i, ...propRow(props[i])]);
    }
  }
  forget(env);

  // Los 28 uniformes del agua (water_painter.dart:32-60) en V390 y H844, 4 horas, velocidad 2 (LT-124: H844, costa, 12 h).
  final uniforms = <List<Object?>>[];
  for (final s in sizes.where((s) => s.$1 == 'V390' || s.$1 == 'H844')) {
    final cam = SceneCamera(Size(s.$2, s.$3));
    for (final hour in [6.0, 12.0, 18.0, 22.0]) {
      final state = SceneState()..speed = 2;
      final u = WaterPainter.uniforms(
        camera: cam, state: state, palette: TimeOfDay.paletteFor(hour), light: TimeOfDay.lightFor(hour, cam), environment: env,
      );
      expect(u.length, 28);
      uniforms.add([s.$1, b(hour), u.map(b).join(' ')]);
    }
  }

  // Los tres tramos del agua degradada con el tinte del escenario (water_painter.dart:79-90), por hora entera.
  final bands = <List<Object?>>[];
  for (var h = 0; h <= 24; h++) {
    final p = TimeOfDay.paletteFor(h.toDouble());
    final near = env.tintWater(p.waterNear);
    final far = env.tintWater(p.waterFar);
    final top = Color.lerp(far, p.fog, 0.5)!;
    bands.add([h, ...rgb(top).map(b), ...rgb(far).map(b), ...rgb(near).map(b)]);
  }

  final body = <String, Object?>{
    'header': header(generator, commit),
    'constants': table(['maxVisibleProps', 'waveAmplitude', 'waveScale'], [
      [env.maxVisibleProps, b(env.waveAmplitude), b(env.waveScale)],
    ]),
    'segments': table(['k', 'i', 'kind', 'x', 'z', 'scale', 'seed'], segments),
    'digests': table(['k', 'count', 'fnv'], digests),
    'visible': table(['distance', 'i', 'kind', 'x', 'z', 'scale', 'seed'], visible),
    'uniforms': table(['size', 'hour', 'values'], uniforms),
    'bands': table(['hour', 'topR', 'topG', 'topB', 'midR', 'midG', 'midB', 'bottomR', 'bottomG', 'bottomB'], bands),
  };
  if (topped != null) body.addAll(cappedVectors(topped));
  return body;
}

// ── scene_horizons.json ────────────────────────────────────────────────────────────────────────────────────────────────

/// Canvas que anota los rectángulos (los de paintHorizon) y descarta el resto.
class _RectCanvas implements Canvas {
  final rects = <Rect>[];

  @override
  void drawRect(Rect rect, Paint paint) => rects.add(rect);

  @override
  dynamic noSuchMethod(Invocation invocation) => null;
}

/// river_environment.dart:46-71 (privado, copiado): una columna del skyline en una ventana de alto hMax; la máscara de
/// ventanas, celda por celda en el orden del legacy (filas de arriba a abajo, columnas de izquierda a derecha).
(double, double, int, int, String) skylineColumn(int i, double hMax, double horizonY) {
  const bw = 34.0;
  final rnd = math.Random(i * 31 + 7);
  final h = hMax * (0.3 + rnd.nextDouble() * 0.7);
  final bwidth = bw * (0.6 + rnd.nextDouble() * 0.4);
  var rows = 0;
  var cols = 0;
  final bits = StringBuffer();
  for (var wy = horizonY - h + 6; wy < horizonY - 4; wy += 7) {
    rows++;
    var c = 0;
    for (var wx = 4.0; wx < bwidth - 4; wx += 8) {
      c++;
      bits.write(rnd.nextDouble() < 0.5 ? '1' : '0');
    }
    cols = c;
  }
  return (h, bwidth, rows, cols, bits.toString());
}

Map<String, Object?> horizonVectors(String commit) {
  final tv = SceneCamera(const Size(411, 914));
  final hMaxRiver = 914 * 0.12;

  // Control: lo que dibuja el legacy a 0 m y a las 22 h en TV (cuerpos y ventanas) coincide con la copia.
  final canvas = _RectCanvas();
  Environment.of(EnvironmentId.river)
      .paintHorizon(canvas, const Size(411, 914), tv, TimeOfDay.paletteFor(22), 0, 0);
  var r = 0;
  for (var i = 0; i < (411 / 34).ceil() + 2; i++) {
    final (h, bwidth, rows, cols, bits) = skylineColumn(i, hMaxRiver, tv.horizonY);
    final body = canvas.rects[r++];
    expect(body.left, closeTo(i * 34.0, 1e-9));
    expect(body.top, closeTo(tv.horizonY - h, 1e-9));
    expect(body.width, closeTo(bwidth, 1e-9));
    r += bits.split('').where((c) => c == '1').length;
    expect(rows * cols, bits.length);
  }
  expect(r, canvas.rects.length);

  final skyline = <List<Object?>>[];
  for (var i = 0; i < 64; i++) {
    final (h, bwidth, rows, cols, bits) = skylineColumn(i, hMaxRiver, tv.horizonY);
    skyline.add([i, b(h / hMaxRiver), b(bwidth / 34.0), rows, cols, bits]);
  }

  // coast_environment.dart:56-63 (privado, copiado): el perfil del acantilado lejano en fracción de hMax, sin fase (DT8-09).
  final cliff = <List<Object?>>[];
  for (var i = 0; i <= 20; i++) {
    final fade = 1 - i / 20;
    cliff.add([i, b(fade * (0.6 + 0.4 * math.sin(i * 1.7 + 0.5 + 0.0)))]);
  }

  // Los rectángulos del fondo de la regata, tal como los dibuja el legacy en TV a 0 m (tierra, tribunas, torres).
  final regatta = <List<Object?>>[];
  final rc = _RectCanvas();
  Environment.of(EnvironmentId.regatta).paintHorizon(rc, const Size(411, 914), tv, TimeOfDay.paletteFor(12), 0, 0);
  for (var i = 0; i < rc.rects.length; i++) {
    final q = rc.rects[i];
    regatta.add([i, b(q.left), b(q.top), b(q.width), b(q.height)]);
  }

  // horizonColor de los tonos de los tres escenarios por hora entera (environment.dart:127-129).
  const tones = <String, (Color, double)>{
    'riverBody': (Color(0xFF3A4660), 0.8),
    'coastCliff': (Color(0xFF4A3F3A), 0.9),
    'regattaLand': (Color(0xFF55704A), 0.9),
    'regattaStand': (Color(0xFF6E7B8C), 0.8),
    'regattaTower': (Color(0xFFB8C0CC), 0.8),
    'flag0': (Color(0xFFE53935), 0.5),
    'flag1': (Color(0xFFFFB300), 0.5),
    'flag2': (Color(0xFF1E88E5), 0.5),
    'flag3': (Color(0xFF43A047), 0.5),
  };
  final colors = <List<Object?>>[];
  for (var h = 0; h <= 24; h++) {
    final p = TimeOfDay.paletteFor(h.toDouble());
    for (final e in tones.entries) {
      colors.add([h, e.key, ...rgb(horizonColor(p, e.value.$1, e.value.$2)).map(b), b(p.ambient)]);
    }
  }

  return {
    'header': header(generator, commit),
    'skyline': table(['i', 'height', 'width', 'rows', 'cols', 'windows'], skyline),
    'coastCliff': table(['step', 'height'], cliff),
    'regattaRects': table(['i', 'left', 'top', 'width', 'height'], regatta),
    'colors': table(['hour', 'tone', 'r', 'g', 'b', 'ambient'], colors),
  };
}

void main() {
  test('vectores de los escenarios del legacy (It. 8, scene_{river,coast,regatta,horizons}.json)', () {
    final outDir = Platform.environment['ROWMATE_ORACLE_OUT'];
    final commit = Platform.environment['ROWMATE_LEGACY_COMMIT'];
    if (outDir == null || commit == null) {
      fail('faltan ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT: correlo con tools/oracle/run-oracle.ps1 -Environments');
    }
    final args = ['--out', outDir, '--legacy-commit', commit];
    // Control de los casos del legacy antes de escribir (environment_test.dart:60, water_painter_test.dart:27).
    final river = Environment.of(EnvironmentId.river);
    expect([for (var k = 0; k < 32; k++) if (river.propsForSegment(k).any((p) => p.kind == PropKind.bridge)) k], [5, 21]);
    writeGolden('scene_river.json', environmentVectors(commit, EnvironmentId.river, null), args);
    writeGolden('scene_coast.json',
        environmentVectors(commit, EnvironmentId.coast, const ToppedEnvironment(60, 18, 3, 3, 14)), args);
    writeGolden('scene_regatta.json',
        environmentVectors(commit, EnvironmentId.regatta, const ToppedEnvironment(200, 60, 10, 10, 45)), args);
    writeGolden('scene_horizons.json', horizonVectors(commit), args);
  });
}
