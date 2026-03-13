import '../models/workout_session.dart';
import '../models/interval_step.dart';
import 'strava_metadata.dart';

/// Genera un archivo TCX XML a partir de una sesión y sus data points
class TcxBuilder {
  static String build(
    WorkoutSession session,
    List<DataPoint> points, {
    List<IntervalStep>? steps,
    String? description,
  }) {
    // session.startedAt ya está en hora local (del dispositivo)
    // Usamos la hora local con el offset para que Strava interprete correctamente la zona horaria
    final startIso = _formatWithTimezone(session.startedAt);
    final buf = StringBuffer();

    buf.writeln('<?xml version="1.0" encoding="UTF-8"?>');
    buf.writeln('<TrainingCenterDatabase xmlns="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"'
        ' xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"'
        ' xmlns:ns3="http://www.garmin.com/xmlschemas/ActivityExtension/v2">');
    buf.writeln('  <Activities>');
    buf.writeln('    <Activity Sport="Other">');
    buf.writeln('      <Id>$startIso</Id>');

    // Generar metadata para descripción
    final desc = StravaMetadata.buildDescription(session, points, steps, userDescription: description);
    if (desc.isNotEmpty) {
      buf.writeln('      <Notes>${_escapeXml(desc)}</Notes>');
    }

    // Agrupar puntos por intervalo para crear múltiples laps
    final laps = _groupPointsByInterval(session, points, steps);

    for (final lap in laps) {
      buf.writeln('      <Lap StartTime="${lap.startTimeIso}">');
      buf.writeln('        <TotalTimeSeconds>${lap.durationSeconds}</TotalTimeSeconds>');
      buf.writeln('        <DistanceMeters>${lap.distanceMeters}</DistanceMeters>');
      buf.writeln('        <Calories>${lap.calories}</Calories>');
      buf.writeln('        <Intensity>Active</Intensity>');
      buf.writeln('        <TriggerMethod>Manual</TriggerMethod>');
      buf.writeln('        <Track>');

      for (final p in lap.points) {
        final pointTime = session.startedAt.add(Duration(seconds: p.elapsedSeconds));
        final timeIso = _formatWithTimezone(pointTime);

        buf.writeln('          <Trackpoint>');
        buf.writeln('            <Time>$timeIso</Time>');
        buf.writeln('            <DistanceMeters>${p.distanceMeters}</DistanceMeters>');

        if (p.heartRate > 0) {
          buf.writeln('            <HeartRateBpm><Value>${p.heartRate}</Value></HeartRateBpm>');
        }

        if (p.strokeRate > 0) {
          buf.writeln('            <Cadence>${p.strokeRate.round()}</Cadence>');
        }

        if (p.powerWatts > 0) {
          buf.writeln('            <Extensions>');
          buf.writeln('              <ns3:TPX>');
          buf.writeln('                <ns3:Watts>${p.powerWatts}</ns3:Watts>');
          buf.writeln('              </ns3:TPX>');
          buf.writeln('            </Extensions>');
        }

        buf.writeln('          </Trackpoint>');
      }

      buf.writeln('        </Track>');
      buf.writeln('      </Lap>');
    }

    buf.writeln('    </Activity>');
    buf.writeln('  </Activities>');
    buf.writeln('</TrainingCenterDatabase>');

    return buf.toString();
  }

  /// Agrupa data points en laps por intervalo
  static List<_LapData> _groupPointsByInterval(
    WorkoutSession session,
    List<DataPoint> points,
    List<IntervalStep>? steps,
  ) {
    if (points.isEmpty) return [];

    // Si no hay información de intervalos, crear un solo lap
    final hasIntervals = points.any((p) => p.stepIndex != null);
    if (!hasIntervals) {
      return [_LapData.fromPoints(session, points, null)];
    }

    // Agrupar por stepIndex
    final laps = <_LapData>[];
    List<DataPoint> currentLapPoints = [];
    int? currentStepIdx;

    for (final p in points) {
      if (p.stepIndex != currentStepIdx && currentLapPoints.isNotEmpty) {
        laps.add(_LapData.fromPoints(session, currentLapPoints, steps?[currentStepIdx!]));
        currentLapPoints = [];
      }
      currentLapPoints.add(p);
      currentStepIdx = p.stepIndex;
    }

    // Último lap
    if (currentLapPoints.isNotEmpty) {
      laps.add(_LapData.fromPoints(session, currentLapPoints, steps?[currentStepIdx!]));
    }

    return laps;
  }

  /// Escapa caracteres especiales XML
  static String _escapeXml(String text) {
    return text
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&apos;');
  }

  /// Formatea una fecha en ISO 8601 con offset de zona horaria
  /// Ejemplo: 2024-03-11T19:51:00-03:00
  static String _formatWithTimezone(DateTime dt) {
    final offset = dt.timeZoneOffset;
    final hours = offset.inHours.abs().toString().padLeft(2, '0');
    final minutes = (offset.inMinutes.abs() % 60).toString().padLeft(2, '0');
    final sign = offset.isNegative ? '-' : '+';

    return '${dt.year.toString().padLeft(4, '0')}-'
        '${dt.month.toString().padLeft(2, '0')}-'
        '${dt.day.toString().padLeft(2, '0')}T'
        '${dt.hour.toString().padLeft(2, '0')}:'
        '${dt.minute.toString().padLeft(2, '0')}:'
        '${dt.second.toString().padLeft(2, '0')}'
        '$sign$hours:$minutes';
  }
}

/// Helper class para representar un lap
class _LapData {
  final String startTimeIso;
  final int durationSeconds;
  final int distanceMeters;
  final int calories;
  final List<DataPoint> points;

  _LapData({
    required this.startTimeIso,
    required this.durationSeconds,
    required this.distanceMeters,
    required this.calories,
    required this.points,
  });

  factory _LapData.fromPoints(
    WorkoutSession session,
    List<DataPoint> points,
    IntervalStep? step,
  ) {
    if (points.isEmpty) {
      throw ArgumentError('Cannot create lap from empty points');
    }

    final first = points.first;
    final last = points.last;
    final startTime = session.startedAt.add(Duration(seconds: first.elapsedSeconds));
    final duration = last.elapsedSeconds - first.elapsedSeconds;
    final distance = last.distanceMeters - first.distanceMeters;

    // Estimar calorías proporcionales
    final totalPoints = points.length;
    final estimatedCalories = (session.totalCalories * totalPoints / (session.totalTimeSeconds / 5)).round();

    return _LapData(
      startTimeIso: TcxBuilder._formatWithTimezone(startTime),
      durationSeconds: duration,
      distanceMeters: distance,
      calories: estimatedCalories,
      points: points,
    );
  }
}
