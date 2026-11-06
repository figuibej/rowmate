// Oráculo de la escena del legacy (06a-escena-lago/02-tech-design §6.2): corre el código de la escena del legacy (que importa
// dart:ui, por eso con Flutter) sin tocarlo y escribe los vectores de Assets/RowMate/Tests/Golden/oracle/scene_*.json con
// ../../lib/golden.dart. Lo corre tools/oracle/run-oracle.ps1 -Scene, que pasa la carpeta de salida y el commit del legacy en
// ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT, dentro de la guardia del legacy.
//
// Lo que se lee del legacy tal cual: Random (la VM de Dart), TimeOfDay.paletteFor / lightFor / sunElevation / isNight,
// SceneCamera, Environment.propsForSegment / visibleProps (también con un Environment de prueba, el del tope), horizonColor,
// Environment.tintWater y las posiciones que dibujan SkyPainter (las 80 estrellas: drawCircle recibe el centro y el radio en
// double) y WaterPainter sin shader (las 10 líneas: drawLine recibe los extremos en double).
// Lo que es privado del legacy y se copia con su cita (la VM de Dart hace la cuenta; Paint guarda el color en float32, así que
// el color de un dibujo no se puede leer sin perder bits): el alfa de las estrellas, las nubes (x e y), el perfil de las
// cordilleras y sus picos nevados, el tinte y la niebla de la orilla (_c) y el alfa de las líneas del agua.
import 'dart:io';
import 'dart:math' as math;
import 'dart:typed_data';

import 'package:flutter/material.dart' hide TimeOfDay;
import 'package:flutter_test/flutter_test.dart';
import 'package:rowmate/features/workout/scene/environment.dart';
import 'package:rowmate/features/workout/scene/painters/sky_painter.dart';
import 'package:rowmate/features/workout/scene/painters/water_painter.dart';
import 'package:rowmate/features/workout/scene/scene_camera.dart';
import 'package:rowmate/features/workout/scene/scene_state.dart';
import 'package:rowmate/features/workout/scene/time_of_day.dart';

import '../../lib/golden.dart';

const String generator = 'tools/oracle/engine/test/scene_vectors_test.dart';

// Los siete tamaños de la spec (§0.3), en dp.
const sizes = <(String, double, double)>[
  ('V390', 390, 844),
  ('H844', 844, 390),
  ('TV', 411, 914),
  ('P360', 360, 800),
  ('TH', 914, 411),
  ('L640', 640, 360),
  ('W1280', 1280, 720),
];

/// La hora armada como el legacy (scene_view.dart:84-85): hora + minuto/60.
double hourOfMinute(int minute) => (minute ~/ 60) + (minute % 60) / 60;

List<double> rgb(Color c) => [c.r, c.g, c.b];

String b(double x) => f64bits(x);

// ── scene_random.json ──────────────────────────────────────────────────────────────────────────────────────────────────

const randomSeeds = <int>[0, 1, 17, 42, -1, 7919, 55450, 1000020, (1 << 40) + 3];

Map<String, Object?> randomVectors(String commit) {
  final rows = <List<Object?>>[];
  for (final seed in randomSeeds) {
    for (final op in ['double', 'int1000', 'int2p26', 'int7', 'bool']) {
      final rnd = math.Random(seed);
      final values = <String>[];
      for (var i = 0; i < 64; i++) {
        switch (op) {
          case 'double':
            values.add(b(rnd.nextDouble()));
          case 'int1000':
            values.add('${rnd.nextInt(1000)}');
          case 'int2p26':
            values.add('${rnd.nextInt(1 << 26)}');
          case 'int7':
            values.add('${rnd.nextInt(7)}');
          case 'bool':
            values.add(rnd.nextBool() ? '1' : '0');
        }
      }
      rows.add([seed, op, values.join(' ')]);
    }
  }
  return {
    'header': header(generator, commit),
    'sequences': table(['seed', 'op', 'values'], rows),
  };
}

// ── scene_palette.json y scene_light.json ──────────────────────────────────────────────────────────────────────────────

Map<String, Object?> paletteVectors(String commit) {
  final rows = <List<Object?>>[];
  for (var m = 0; m < 1440; m++) {
    final h = hourOfMinute(m);
    final p = TimeOfDay.paletteFor(h);
    rows.add([
      m,
      b(h),
      for (final c in [p.skyTop, p.skyHorizon, p.waterNear, p.waterFar, p.sunColor, p.sunGlow, p.fog]) ...rgb(c).map(b),
      b(p.ambient),
      TimeOfDay.isNight(h) ? 1 : 0,
      b(TimeOfDay.sunElevation(h)),
    ]);
  }
  final columns = ['minute', 'hour'];
  for (final name in ['skyTop', 'skyHorizon', 'waterNear', 'waterFar', 'sunColor', 'sunGlow', 'fog']) {
    columns.addAll(['${name}R', '${name}G', '${name}B']);
  }
  columns.addAll(['ambient', 'isNight', 'sunElevation']);
  return {
    'header': header(generator, commit),
    'minutes': table(columns, rows),
  };
}

Map<String, Object?> lightVectors(String commit) {
  final rows = <List<Object?>>[];
  final cams = [for (final s in sizes) SceneCamera(Size(s.$2, s.$3))];
  for (var m = 0; m < 1440; m++) {
    final h = hourOfMinute(m);
    final row = <Object?>[m, b(h)];
    double? strength;
    bool? moon;
    final xy = <String>[];
    for (final cam in cams) {
      final l = TimeOfDay.lightFor(h, cam);
      strength ??= l.strength;
      moon ??= l.isMoon;
      expect(l.strength, strength);
      xy.addAll([b(l.position.dx), b(l.position.dy)]);
    }
    row.addAll([b(strength!), moon! ? 1 : 0, ...xy]);
    rows.add(row);
  }
  final columns = ['minute', 'hour', 'strength', 'isMoon'];
  for (final s in sizes) {
    columns.addAll(['x${s.$1}', 'y${s.$1}']);
  }
  return {
    'header': header(generator, commit),
    'minutes': table(columns, rows),
  };
}

// ── scene_sky.json ─────────────────────────────────────────────────────────────────────────────────────────────────────

/// Canvas que anota lo que necesita el oráculo y descarta el resto.
class _RecordingCanvas implements Canvas {
  final circles = <(Offset, double)>[];
  final lines = <(Offset, Offset)>[];

  @override
  void drawCircle(Offset c, double radius, Paint paint) => circles.add((c, radius));

  @override
  void drawLine(Offset p1, Offset p2, Paint paint) => lines.add((p1, p2));

  @override
  dynamic noSuchMethod(Invocation invocation) => null;
}

// sky_painter.dart:29-33 (privado): (x, y relativa, tamaño, velocidad).
const cloudData = <(double, double, double, double)>[
  (0.08, 0.10, 34.0, 1.0), (0.26, 0.18, 22.0, 1.4), (0.42, 0.07, 40.0, 0.8),
  (0.58, 0.22, 26.0, 1.2), (0.72, 0.12, 36.0, 0.9), (0.88, 0.20, 24.0, 1.3),
  (0.97, 0.05, 30.0, 1.1),
];

/// sky_painter.dart:95-96 (privado, copiado): la x del legacy con la deriva por distancia y la y.
(double, double) cloudXY(int i, double w, double hz, double distance, double time) {
  final (fx, fy, _, speed) = cloudData[i];
  final x = ((fx + distance * 0.0004 * speed + time * 0.004 * speed) % 1.2) * w - w * 0.1;
  final y = hz * (0.12 + fy * 0.6);
  return (x, y);
}

/// lake_environment.dart:62 (privado, copiado): el perfil de una cordillera en u.
double legacyProfile(double u, double phase) =>
    0.45 + 0.35 * math.sin(u * 6.3 + phase) + 0.2 * math.sin(u * 15.1 + phase * 2);

/// shore_painter.dart:45-50 (privado, copiado): el tinte por la luz ambiente y la niebla de distancia.
Color shoreColor(ScenePalette palette, Color c, double relZ) {
  final a = palette.ambient;
  final lit = Color.from(alpha: 1, red: c.r * a, green: c.g * a, blue: c.b * math.min(1.0, a + 0.12));
  final f = ((relZ - 80) / 320).clamp(0.0, 1.0);
  return Color.lerp(lit, palette.fog, f * 0.8)!;
}

Map<String, Object?> skyVectors(String commit) {
  final env = Environment.of(EnvironmentId.lake);

  // Las 80 estrellas, tal como las dibuja SkyPainter (22 h: oscuridad 1; las primeras 80 drawCircle son las estrellas).
  final stars = <List<Object?>>[];
  for (final s in sizes) {
    final size = Size(s.$2, s.$3);
    final cam = SceneCamera(size);
    final canvas = _RecordingCanvas();
    SkyPainter(
      camera: cam, palette: TimeOfDay.paletteFor(22), light: TimeOfDay.lightFor(22, cam), hour: 22,
      environment: env, distance: 0, time: 0,
    ).paint(canvas, size);
    expect(canvas.circles.length, greaterThanOrEqualTo(82));
    for (var i = 0; i < 80; i++) {
      final (c, r) = canvas.circles[i];
      stars.add([s.$1, i, b(c.dx), b(c.dy), b(r)]);
    }
    // El astro: las dos drawCircle siguientes van en la posición de lightFor.
    expect(canvas.circles[80].$1, TimeOfDay.lightFor(22, cam).position);
  }

  // Las 7 nubes: la x del legacy (con la deriva por distancia) y la y, a 0 y 1234 m y con t 0 y 3,2 s.
  final clouds = <List<Object?>>[];
  for (final s in sizes) {
    final cam = SceneCamera(Size(s.$2, s.$3));
    for (final distance in [0.0, 1234.0]) {
      for (final time in [0.0, 3.2]) {
        for (var i = 0; i < 7; i++) {
          final (x, y) = cloudXY(i, s.$2, cam.horizonY, distance, time);
          clouds.add([s.$1, b(distance), b(time), i, b(x), b(y)]);
        }
      }
    }
  }

  // El color y el alfa del cuerpo de las nubes y el alfa de su sombra, por hora entera (sky_painter.dart:91-93).
  final cloudColors = <List<Object?>>[];
  for (var h = 0; h <= 24; h++) {
    final p = TimeOfDay.paletteFor(h.toDouble());
    final body = Color.lerp(Colors.white, p.skyHorizon, 0.25)!;
    cloudColors.add([h, ...rgb(body).map(b), b(0.08 + 0.5 * p.ambient * p.ambient), b(0.12 * p.ambient)]);
  }

  // horizonColor de las dos cordilleras y la nieve, por hora entera (environment.dart:127-129; lake_environment.dart:52-56, :78).
  final horizon = <List<Object?>>[];
  for (var h = 0; h <= 24; h++) {
    final p = TimeOfDay.paletteFor(h.toDouble());
    final far = horizonColor(p, const Color(0xFF5C6E8A), 1.0);
    final near = horizonColor(p, const Color(0xFF2F4A3A), 0.3);
    final snow = Color.lerp(const Color(0xFFF2F6FA), p.fog, 0.3)!;
    horizon.add([h, ...rgb(far).map(b), ...rgb(near).map(b), ...rgb(snow).map(b)]);
  }

  // El perfil de las cordilleras en u de −0,5 a 2,0, con la fase del lejano (0) y la del cercano (3,1).
  final profile = <List<Object?>>[];
  for (final phase in [0.0, 3.1]) {
    for (var j = -50; j <= 200; j++) {
      final u = j / 100;
      profile.add([b(phase), b(u), b(legacyProfile(u, phase))]);
    }
  }

  // Los picos nevados del legacy (lake_environment.dart:63-84, copiado): índices de los 48 pasos con nieve, por tamaño y
  // distancia (el perfil se corre con distance·0,004·60).
  final snowPeaks = <List<Object?>>[];
  for (final s in sizes) {
    final w = s.$2;
    final hMax = s.$3 * 0.12;
    for (final distance in [0.0, 920.0, 1234.0]) {
      final shift = distance * 0.004;
      const steps = 48;
      final heights = <double>[];
      for (var i = 0; i <= steps; i++) {
        final x = w * i / steps;
        final u = (x + shift * 60) / w * 1.0;
        heights.add(hMax * (0.45 + 0.35 * math.sin(u * 6.3 + 0.0) + 0.2 * math.sin(u * 15.1 + 0.0 * 2)));
      }
      final peaks = <int>[];
      for (var i = 1; i < steps; i++) {
        if (heights[i] > hMax * 0.75 && heights[i] >= heights[i - 1] && heights[i] >= heights[i + 1]) peaks.add(i);
      }
      snowPeaks.add([s.$1, b(distance), peaks.join(' ')]);
    }
  }

  // El tinte y la niebla de la orilla (_c) para 6 colores de referencia, 25 horas y 9 distancias.
  const shoreColors = <String, Color>{
    'trunk': Color(0xFF5D4037),
    'pine0': Color(0xFF1B5E20),
    'cabin': Color(0xFF6D4C41),
    'roof': Color(0xFF4E342E),
    'pier': Color(0xFF8D6E63),
    'window': Color(0xFF90CAF9),
  };
  final shore = <List<Object?>>[];
  for (var h = 0; h <= 24; h++) {
    final p = TimeOfDay.paletteFor(h.toDouble());
    for (final relZ in [1.5, 10.0, 79.5, 80.0, 120.0, 240.0, 333.3, 400.0, 520.0]) {
      for (final entry in shoreColors.entries) {
        final c = shoreColor(p, entry.value, relZ);
        shore.add([h, b(relZ), entry.key, ...rgb(c).map(b)]);
      }
    }
  }

  return {
    'header': header(generator, commit),
    'stars': table(['size', 'i', 'x', 'y', 'r'], stars),
    'clouds': table(['size', 'distance', 'time', 'i', 'x', 'y'], clouds),
    'cloudColors': table(['hour', 'bodyR', 'bodyG', 'bodyB', 'bodyAlpha', 'shadeAlpha'], cloudColors),
    'horizon': table(['hour', 'farR', 'farG', 'farB', 'nearR', 'nearG', 'nearB', 'snowR', 'snowG', 'snowB'], horizon),
    'profile': table(['phase', 'u', 'p'], profile),
    'snowPeaks': table(['size', 'distance', 'peaks'], snowPeaks),
    'shore': table(['hour', 'relZ', 'color', 'r', 'g', 'b'], shore),
  };
}

// ── scene_lake.json ────────────────────────────────────────────────────────────────────────────────────────────────────

final ByteData _canon = ByteData(8);

/// Bytes canónicos de un objeto (los mismos en C#): tipo (1 byte), x, z y escala (bits de double, big-endian) y semilla
/// (int32 big-endian).
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

/// FNV-1a de 64 bits de los bytes canónicos de un segmento, en 16 hex.
String fnv1a64(List<ShoreProp> props) {
  var h = 0xcbf29ce484222325;
  const prime = 0x100000001b3;
  for (final p in props) {
    for (final byte in canonicalBytes(p)) {
      h ^= byte;
      h *= prime;
    }
  }
  // toUnsigned(64) no sirve en la VM (un int de 64 bits no tiene lugar para el bit de signo): las dos mitades de 32 bits.
  return (h >>> 32).toRadixString(16).padLeft(8, '0') + (h & 0xFFFFFFFF).toRadixString(16).padLeft(8, '0');
}

List<Object?> propRow(ShoreProp p) => [p.kind.name, b(p.x), b(p.z), b(p.scale), p.seed];

/// Escenario de prueba del tope (AC-SCN-14-05): la función visibleProps del legacy sobre un Environment con muchas boyas
/// (segmentos 0 a 19) y muchos hitos (desde el 20). Las z son distintas alrededor de cualquier corte (QA-G6aT-15 a). El mismo
/// escenario está en C# (Tests/Support/SceneSeeds.cs, CapEnvironment): cualquier cambio va en los dos lados.
class CapEnvironment extends Environment {
  const CapEnvironment();

  static const int cap = 40;
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
    final buoys = buoyHeavy ? 12 : 2;
    for (var i = 0; i < buoys; i++) {
      final z = z0 + i * (50 / buoys) + 0.25 + rnd.nextDouble() * 1.5;
      out.add(ShoreProp(x: (i.isEven ? -1 : 1) * (1 + rnd.nextDouble() * 6), z: z, kind: PropKind.laneBuoy, seed: i));
    }
    final marks = buoyHeavy ? 2 : 9;
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

Map<String, Object?> lakeVectors(String commit) {
  final env = Environment.of(EnvironmentId.lake);
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
    for (var i = 0; i < props.length; i++) {
      visible.add([b(d), i, ...propRow(props[i])]);
    }
  }

  const cap = CapEnvironment();
  final capSegments = <List<Object?>>[];
  for (var k = 0; k <= 41; k++) {
    final props = cap.propsForSegment(k);
    for (var i = 0; i < props.length; i++) {
      capSegments.add([k, i, ...propRow(props[i])]);
    }
  }
  final capVisible = <List<Object?>>[];
  for (final d in capDistances) {
    final props = cap.visibleProps(d);
    expect(props.length, lessThanOrEqualTo(CapEnvironment.cap));
    for (var i = 0; i < props.length; i++) {
      capVisible.add([b(d), i, ...propRow(props[i])]);
    }
  }

  return {
    'header': header(generator, commit),
    'segments': table(['k', 'i', 'kind', 'x', 'z', 'scale', 'seed'], segments),
    'digests': table(['k', 'count', 'fnv'], digests),
    'visible': table(['distance', 'i', 'kind', 'x', 'z', 'scale', 'seed'], visible),
    'capSegments': table(['k', 'i', 'kind', 'x', 'z', 'scale', 'seed'], capSegments),
    'capVisible': table(['distance', 'i', 'kind', 'x', 'z', 'scale', 'seed'], capVisible),
  };
}

// ── scene_water.json ───────────────────────────────────────────────────────────────────────────────────────────────────

Map<String, Object?> waterVectors(String commit) {
  final env = Environment.of(EnvironmentId.lake);
  final bands = <List<Object?>>[];
  for (var h = 0; h <= 24; h++) {
    final p = TimeOfDay.paletteFor(h.toDouble());
    final near = env.tintWater(p.waterNear);
    final far = env.tintWater(p.waterFar);
    final top = Color.lerp(far, p.fog, 0.5)!;
    bands.add([h, ...rgb(top).map(b), ...rgb(far).map(b), ...rgb(near).map(b)]);
  }

  final lines = <List<Object?>>[];
  for (final s in sizes.where((s) => s.$1 == 'V390' || s.$1 == 'H844')) {
    final size = Size(s.$2, s.$3);
    final cam = SceneCamera(size);
    for (final hour in [6.0, 12.0, 18.0, 22.0]) {
      final palette = TimeOfDay.paletteFor(hour);
      final light = TimeOfDay.lightFor(hour, cam);
      for (final distance in [0.0, 3.0, 920.0, 1234.0, 4096.0, 42195.0]) {
        final state = SceneState()..distance = distance;
        final canvas = _RecordingCanvas();
        WaterPainter(shader: null, camera: cam, state: state, palette: palette, light: light, environment: env)
            .paint(canvas, size);
        expect(canvas.lines.length, 10);
        for (var i = 0; i < 10; i++) {
          final (p1, p2) = canvas.lines[i];
          expect(p1.dy, p2.dy);
          // water_painter.dart:98-101 (privado, copiado): t y el alfa de la línea.
          final t = ((i / 10) + (state.distance * 0.02) % 0.1) % 1.0;
          final alpha = 0.05 + 0.1 * (1 - t) * light.strength;
          lines.add([
            s.$1, b(hour), b(distance), i, b(p1.dy), b(p1.dx), b(p2.dx), b(t), b(alpha), b(light.strength),
            b(light.position.dx),
          ]);
        }
      }
    }
  }
  return {
    'header': header(generator, commit),
    'bands': table(['hour', 'topR', 'topG', 'topB', 'midR', 'midG', 'midB', 'bottomR', 'bottomG', 'bottomB'], bands),
    'lines': table(['size', 'hour', 'distance', 'i', 'y', 'x0', 'x1', 't', 'alpha', 'strength', 'centerX'], lines),
  };
}

void main() {
  test('vectores de la escena del legacy (It. 6a, scene_*.json)', () {
    final outDir = Platform.environment['ROWMATE_ORACLE_OUT'];
    final commit = Platform.environment['ROWMATE_LEGACY_COMMIT'];
    if (outDir == null || commit == null) {
      fail('faltan ROWMATE_ORACLE_OUT y ROWMATE_LEGACY_COMMIT: correlo con tools/oracle/run-oracle.ps1 -Scene');
    }
    final args = ['--out', outDir, '--legacy-commit', commit];
    // Los vectores dorados de la spec (AC-SCN-14-02), como control antes de escribir nada.
    final r42 = math.Random(42);
    expect(r42.nextDouble(), 0.15092545597797424);
    expect(r42.nextDouble(), 0.6041479725361217);
    expect(r42.nextInt(1000), 77);
    expect(math.Random(55450).nextDouble(), 0.854835531354132);
    writeGolden('scene_random.json', randomVectors(commit), args);
    writeGolden('scene_palette.json', paletteVectors(commit), args);
    writeGolden('scene_light.json', lightVectors(commit), args);
    writeGolden('scene_sky.json', skyVectors(commit), args);
    writeGolden('scene_lake.json', lakeVectors(commit), args);
    writeGolden('scene_water.json', waterVectors(commit), args);
  });
}

