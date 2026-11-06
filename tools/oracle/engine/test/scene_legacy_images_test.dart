// Las referencias del legacy LEG-01 a LEG-07 (06a-escena-lago/02-tech-design §5.3, 01-po-spec §7): la escena del lago
// dibujada con el código del legacy, sin tocarlo: SkyPainter (con paintHorizon del lago), el respaldo de WaterPainter
// (shader: null) y ShorePainter, sin el bote, a densidad 1, con el tiempo de escena de las tomas (3,2 s). Las versiones
// "con marcas" (LEG-02, -05 y -07, como CAP-02, -05 y -07) agregan una marca de 4 dp en la base de cada objeto del legacy
// (cam.project(x, 0, relZ)). Escribe los PNG en la carpeta de ROWMATE_LEG_OUT; lo corre tools/oracle/run-oracle.ps1 -Scene
// dentro de la guardia del legacy.
import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart' hide TimeOfDay;
import 'package:flutter_test/flutter_test.dart';
import 'package:rowmate/features/workout/scene/environment.dart';
import 'package:rowmate/features/workout/scene/painters/shore_painter.dart';
import 'package:rowmate/features/workout/scene/painters/sky_painter.dart';
import 'package:rowmate/features/workout/scene/painters/water_painter.dart';
import 'package:rowmate/features/workout/scene/scene_camera.dart';
import 'package:rowmate/features/workout/scene/scene_state.dart';
import 'package:rowmate/features/workout/scene/time_of_day.dart';

/// Una referencia: id, tamaño en dp, distancia de escena, hora y si lleva la versión con marcas.
class Leg {
  const Leg(this.id, this.width, this.height, this.distance, this.hour, this.marks);
  final String id;
  final double width;
  final double height;
  final double distance;
  final double hour;
  final bool marks;
}

const legs = <Leg>[
  Leg('LEG-01', 390, 844, 920, 6, false),
  Leg('LEG-02', 390, 844, 920, 12, true),
  Leg('LEG-03', 390, 844, 920, 18, false),
  Leg('LEG-04', 390, 844, 920, 22, false),
  Leg('LEG-05', 844, 390, 920, 12, true),
  Leg('LEG-06', 844, 390, 920, 22, false),
  Leg('LEG-07', 390, 844, 0, 12, true),
];

const double sceneTime = 3.2;

ui.Picture paintLeg(Leg leg, {required bool withMarks}) {
  final size = Size(leg.width, leg.height);
  final env = Environment.of(EnvironmentId.lake);
  final cam = SceneCamera(size);
  final palette = TimeOfDay.paletteFor(leg.hour);
  final light = TimeOfDay.lightFor(leg.hour, cam);
  final state = SceneState()
    ..distance = leg.distance
    ..time = sceneTime;
  final props = env.visibleProps(leg.distance);
  final recorder = ui.PictureRecorder();
  final canvas = Canvas(recorder);
  canvas.drawRect(Offset.zero & size, Paint()..color = const Color(0xFF000000));
  SkyPainter(
    camera: cam, palette: palette, light: light, hour: leg.hour,
    environment: env, distance: leg.distance, time: sceneTime,
  ).paint(canvas, size);
  WaterPainter(shader: null, camera: cam, state: state, palette: palette, light: light, environment: env).paint(canvas, size);
  ShorePainter(camera: cam, props: props, distance: leg.distance, palette: palette, time: sceneTime).paint(canvas, size);
  if (withMarks) {
    // Una marca de 4 dp (un cuadrado magenta con borde negro) en la base de cada objeto del legacy.
    final fill = Paint()..color = const Color(0xFFFF00FF);
    final edge = Paint()
      ..color = const Color(0xFF000000)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;
    for (final p in props) {
      final base = cam.project(p.x, 0, p.z - leg.distance);
      if (base == null) continue;
      final r = Rect.fromCenter(center: base, width: 4, height: 4);
      canvas.drawRect(r, fill);
      canvas.drawRect(r, edge);
    }
  }
  return recorder.endRecording();
}

Future<void> writePng(ui.Picture picture, int width, int height, File file) async {
  final image = await picture.toImage(width, height);
  final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
  image.dispose();
  picture.dispose();
  file.parent.createSync(recursive: true);
  file.writeAsBytesSync(bytes!.buffer.asUint8List(bytes.offsetInBytes, bytes.lengthInBytes));
  stdout.writeln('[oracle] ${file.uri.pathSegments.last}: ${file.lengthSync()} bytes');
}

void main() {
  test('referencias del legacy LEG-01 a LEG-07 (It. 6a)', () async {
    final outDir = Platform.environment['ROWMATE_LEG_OUT'];
    if (outDir == null) {
      fail('falta ROWMATE_LEG_OUT: correlo con tools/oracle/run-oracle.ps1 -Scene');
    }
    for (final leg in legs) {
      final w = leg.width.toInt();
      final h = leg.height.toInt();
      await writePng(paintLeg(leg, withMarks: false), w, h, File('$outDir/${leg.id}.png'));
      if (leg.marks) {
        await writePng(paintLeg(leg, withMarks: true), w, h, File('$outDir/${leg.id}-marcas.png'));
      }
    }
  });
}
