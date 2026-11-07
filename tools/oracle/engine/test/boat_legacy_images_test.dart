// Las referencias del legacy LEG-01 y LEG-02 de la It. 7 (07-remero-palada/02-tech-design §5, §6.2; CAP-19): la escena del lago
// dibujada con el codigo del legacy, sin tocarlo, como la LEG-02 de la 6a (SkyPainter, el respaldo de WaterPainter y ShorePainter,
// V390, 12 h, distancia 920, tiempo de escena 3,2 s) mas el BoatPainter con strokePhase 0 (LEG-01, catch) y 0,36 (LEG-02, finish),
// sin charcos. Escribe los PNG en la carpeta de ROWMATE_LEG_OUT; lo corre tools/oracle/run-oracle.ps1 -Stroke dentro de la guardia
// del legacy.
import 'dart:io';
import 'dart:ui' as ui;

import 'package:flutter/material.dart' hide TimeOfDay;
import 'package:flutter_test/flutter_test.dart';
import 'package:rowmate/features/workout/scene/environment.dart';
import 'package:rowmate/features/workout/scene/painters/boat_painter.dart';
import 'package:rowmate/features/workout/scene/painters/shore_painter.dart';
import 'package:rowmate/features/workout/scene/painters/sky_painter.dart';
import 'package:rowmate/features/workout/scene/painters/water_painter.dart';
import 'package:rowmate/features/workout/scene/scene_camera.dart';
import 'package:rowmate/features/workout/scene/scene_state.dart';
import 'package:rowmate/features/workout/scene/time_of_day.dart';

const double sceneTime = 3.2;
const double distance = 920;
const double hour = 12;

ui.Picture paintLeg(double strokePhase) {
  const size = Size(390, 844);
  final env = Environment.of(EnvironmentId.lake);
  final cam = SceneCamera(size);
  final palette = TimeOfDay.paletteFor(hour);
  final light = TimeOfDay.lightFor(hour, cam);
  final state = SceneState()
    ..distance = distance
    ..time = sceneTime
    ..strokePhase = strokePhase;
  final props = env.visibleProps(distance);
  final recorder = ui.PictureRecorder();
  final canvas = Canvas(recorder);
  canvas.drawRect(Offset.zero & size, Paint()..color = const Color(0xFF000000));
  SkyPainter(
    camera: cam, palette: palette, light: light, hour: hour,
    environment: env, distance: distance, time: sceneTime,
  ).paint(canvas, size);
  WaterPainter(shader: null, camera: cam, state: state, palette: palette, light: light, environment: env).paint(canvas, size);
  ShorePainter(camera: cam, props: props, distance: distance, palette: palette, time: sceneTime).paint(canvas, size);
  BoatPainter(camera: cam, state: state, palette: palette, light: light).paint(canvas, size);
  return recorder.endRecording();
}

Future<void> writePng(ui.Picture picture, File file) async {
  final image = await picture.toImage(390, 844);
  final bytes = await image.toByteData(format: ui.ImageByteFormat.png);
  image.dispose();
  picture.dispose();
  file.parent.createSync(recursive: true);
  file.writeAsBytesSync(bytes!.buffer.asUint8List(bytes.offsetInBytes, bytes.lengthInBytes));
  stdout.writeln('[oracle] ${file.uri.pathSegments.last}: ${file.lengthSync()} bytes');
}

void main() {
  test('referencias del legacy LEG-01 y LEG-02 (It. 7)', () async {
    final outDir = Platform.environment['ROWMATE_LEG_OUT'];
    if (outDir == null) {
      fail('falta ROWMATE_LEG_OUT: correlo con tools/oracle/run-oracle.ps1 -Stroke');
    }
    await writePng(paintLeg(0), File('$outDir/LEG-01.png'));
    await writePng(paintLeg(0.36), File('$outDir/LEG-02.png'));
  });
}
