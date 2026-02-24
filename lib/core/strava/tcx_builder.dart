import '../models/workout_session.dart';

/// Genera un archivo TCX XML a partir de una sesión y sus data points
class TcxBuilder {
  static String build(WorkoutSession session, List<DataPoint> points) {
    final startIso = session.startedAt.toUtc().toIso8601String();
    final buf = StringBuffer();

    buf.writeln('<?xml version="1.0" encoding="UTF-8"?>');
    buf.writeln('<TrainingCenterDatabase xmlns="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"'
        ' xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"'
        ' xmlns:ns3="http://www.garmin.com/xmlschemas/ActivityExtension/v2">');
    buf.writeln('  <Activities>');
    buf.writeln('    <Activity Sport="Other">');
    buf.writeln('      <Id>$startIso</Id>');

    // Un solo Lap con los totales
    buf.writeln('      <Lap StartTime="$startIso">');
    buf.writeln('        <TotalTimeSeconds>${session.totalTimeSeconds}</TotalTimeSeconds>');
    buf.writeln('        <DistanceMeters>${session.totalDistanceMeters}</DistanceMeters>');
    buf.writeln('        <Calories>${session.totalCalories}</Calories>');
    buf.writeln('        <Intensity>Active</Intensity>');
    buf.writeln('        <TriggerMethod>Manual</TriggerMethod>');
    buf.writeln('        <Track>');

    for (final p in points) {
      final pointTime = session.startedAt.add(Duration(seconds: p.elapsedSeconds));
      final timeIso = pointTime.toUtc().toIso8601String();

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
    buf.writeln('    </Activity>');
    buf.writeln('  </Activities>');
    buf.writeln('</TrainingCenterDatabase>');

    return buf.toString();
  }
}
