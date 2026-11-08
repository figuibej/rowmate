// Las referencias del legacy LEG-01 a LEG-03 de la It. 8 (08-entornos-selector/02-tech-design §6, §7.2): río a 250 m, costa a
// 500 m y regata a 180 m, en V390 a las 12 h, dibujadas con el código del legacy sin tocarlo: SkyPainter (con paintHorizon del
// escenario), el respaldo de WaterPainter (shader: null) y ShorePainter, sin el bote, con el tiempo de escena de las tomas
// (3,2 s). Cada una sale también "con marcas": una marca de 4 dp en la base de cada objeto (cam.project(x, 0, relZ)), la que
// comparan CAP-44 a CAP-46. Escribe los PNG en la carpeta de ROWMATE_LEG_OUT; lo corre tools/oracle/run-oracle.ps1
// -Environments dentro de la guardia del legacy.
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

class Leg {
  const Leg(this.id, this.environment, this.distance);
  final String id;
  final EnvironmentId environment;
  final double distance;
}

const legs = <Leg>[
  Leg('LEG-01', EnvironmentId.river, 250),
  Leg('LEG-02', EnvironmentId.coast, 500),
  Leg('LEG-03', EnvironmentId.regatta, 180),
];

const double sceneTime = 3.2;
const double hour = 12;
const Size size = Size(390, 844);

ui.Picture paintLeg(Leg leg, {required bool withMarks}) {
  final env = Environment.of(leg.environment);
  final cam = SceneCamera(size);
  final palette = TimeOfDay.paletteFor(hour);
  final light = TimeOfDay.lightFor(hour, cam);
  final state = SceneState()
    ..distance = leg.distance
    ..time = sceneTime;
  final props = env.visibleProps(leg.distance);
  final recorder = ui.PictureRecorder();
  final canvas = Canvas(recorder);
  canvas.drawRect(Offset.zero & size, Paint()..color = const Color(0xFF000000));
  SkyPainter(
    camera: cam, palette: palette, light: light, hour: hour,
    environment: env, distance: leg.distance, time: sceneTime,
  ).paint(canvas, size);
  WaterPainter(shader: null, camera: cam, state: state, palette: palette, light: light, environment: env).paint(canvas, size);
  ShorePainter(camera: cam, props: props, distance: leg.distance, palette: palette, time: sceneTime).paint(canvas, size);
  if (withMarks) {
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

Future<void> writePng(ui.Picture picture, File file) async {
  final image = await picture.toImage(size.width.toInt(), size.height.toInt());
  final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
  image.dispose();
  picture.dispose();
  file.parent.createSync(recursive: true);
  file.writeAsBytesSync(bytes!.buffer.asUint8List(bytes.offsetInBytes, bytes.lengthInBytes));
  stdout.writeln('[oracle] ${file.uri.pathSegments.last}: ${file.lengthSync()} bytes');
}

void main() {
  test('referencias del legacy LEG-01 a LEG-03 (It. 8)', () async {
    final outDir = Platform.environment['ROWMATE_LEG_OUT'];
    if (outDir == null) {
      fail('falta ROWMATE_LEG_OUT: correlo con tools/oracle/run-oracle.ps1 -Environments');
    }
    for (final leg in legs) {
      await writePng(paintLeg(leg, withMarks: false), File('$outDir/${leg.id}.png'));
      await writePng(paintLeg(leg, withMarks: true), File('$outDir/${leg.id}-marcas.png'));
    }
  });
}
