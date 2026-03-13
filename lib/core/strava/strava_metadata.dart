import 'dart:convert';
import '../models/workout_session.dart';
import '../models/interval_step.dart';

/// Metadata de rutina para serializar/deserializar en descripción de Strava
class StravaMetadata {
  final int? routineId;
  final String? routineName;
  final List<IntervalMetadata>? intervals;

  const StravaMetadata({
    this.routineId,
    this.routineName,
    this.intervals,
  });

  /// Serializar con niveles de complejidad decrecientes hasta que quepa en el límite
  static String encode(WorkoutSession session, List<DataPoint> points, List<IntervalStep>? steps) {
    // Nivel 1: Información completa (más detallada)
    final level1 = _encodeLevel1(session, steps);
    if (level1.length <= 200) return level1; // ~255 caracteres con el wrapper HTML

    // Nivel 2: Solo intervalos básicos (sin targets)
    final level2 = _encodeLevel2(session, points);
    if (level2.length <= 200) return level2;

    // Nivel 3: Solo índices de paso (para líneas verticales)
    final level3 = _encodeLevel3(points);
    if (level3.length <= 200) return level3;

    // Nivel 4: Fallback vacío
    return '';
  }

  /// Nivel 1: Rutina completa con targets
  static String _encodeLevel1(WorkoutSession session, List<IntervalStep>? steps) {
    if (session.routineId == null || steps == null || steps.isEmpty) {
      return _encodeLevel2(session, []);
    }

    final metadata = {
      'v': 1, // versión del formato
      'r': session.routineId,
      'n': session.routineName,
      'i': steps.map((s) => {
        't': s.type.name[0], // w=warmup, r=rest, k=work, c=cooldown
        'd': s.durationSeconds,
        'm': s.distanceMeters,
        'pw': s.targetWattsMin,
        'Pw': s.targetWattsMax,
        's': s.targetSpm,
        'p': s.targetSplitSeconds,
      }).toList(),
    };

    return base64Url.encode(utf8.encode(json.encode(metadata)));
  }

  /// Nivel 2: Solo tipos de intervalo sin targets
  static String _encodeLevel2(WorkoutSession session, List<DataPoint> points) {
    // Agrupar data points por stepIndex para extraer intervalos
    final intervalTypes = <String>[];
    int? lastStepIdx;

    for (final p in points) {
      if (p.stepIndex != null && p.stepIndex != lastStepIdx) {
        intervalTypes.add(p.stepType?[0] ?? 'w'); // Primera letra del tipo
        lastStepIdx = p.stepIndex;
      }
    }

    if (intervalTypes.isEmpty) return _encodeLevel3(points);

    final metadata = {
      'v': 2,
      'r': session.routineId,
      'n': session.routineName,
      'i': intervalTypes.join(''),
    };

    return base64Url.encode(utf8.encode(json.encode(metadata)));
  }

  /// Nivel 3: Solo cantidad de intervalos (para líneas verticales)
  static String _encodeLevel3(List<DataPoint> points) {
    final uniqueSteps = points
        .where((p) => p.stepIndex != null)
        .map((p) => p.stepIndex!)
        .toSet()
        .length;

    if (uniqueSteps == 0) return '';

    final metadata = {'v': 3, 'c': uniqueSteps};
    return base64Url.encode(utf8.encode(json.encode(metadata)));
  }

  /// Parsear metadata desde descripción de Strava
  static StravaMetadata? decode(String? description) {
    if (description == null || description.isEmpty) return null;

    // Buscar comentario HTML con metadata
    final match = RegExp(r'<!-- ROWMATE:([A-Za-z0-9_-]+) -->').firstMatch(description);
    if (match == null) return null;

    try {
      final encoded = match.group(1)!;
      final decoded = utf8.decode(base64Url.decode(encoded));
      final json = jsonDecode(decoded) as Map<String, dynamic>;
      final version = json['v'] as int?;

      if (version == 1) {
        return _decodeLevel1(json);
      } else if (version == 2) {
        return _decodeLevel2(json);
      } else if (version == 3) {
        // Nivel 3 solo tiene contador, no suficiente info para reconstruir
        return null;
      }
    } catch (e) {
      // Si falla el parsing, simplemente no hay metadata
      return null;
    }

    return null;
  }

  static StravaMetadata _decodeLevel1(Map<String, dynamic> json) {
    final intervals = (json['i'] as List?)
        ?.map((i) => IntervalMetadata(
              type: _typeFromChar(i['t'] as String),
              durationSeconds: i['d'] as int?,
              distanceMeters: i['m'] as int?,
              targetWattsMin: i['pw'] as int?,
              targetWattsMax: i['Pw'] as int?,
              targetSpm: i['s'] as int?,
              targetSplitSeconds: i['p'] as int?,
            ))
        .toList();

    return StravaMetadata(
      routineId: json['r'] as int?,
      routineName: json['n'] as String?,
      intervals: intervals,
    );
  }

  static StravaMetadata _decodeLevel2(Map<String, dynamic> json) {
    final typesStr = json['i'] as String?;
    if (typesStr == null) return StravaMetadata(routineId: json['r'] as int?);

    final intervals = typesStr.split('').map((char) =>
      IntervalMetadata(type: _typeFromChar(char))
    ).toList();

    return StravaMetadata(
      routineId: json['r'] as int?,
      routineName: json['n'] as String?,
      intervals: intervals,
    );
  }

  static String _typeFromChar(String char) {
    switch (char) {
      case 'w': return 'warmup';
      case 'k': return 'work';
      case 'r': return 'rest';
      case 'c': return 'cooldown';
      default: return 'work';
    }
  }

  /// Crear descripción completa con metadata oculta
  static String buildDescription(WorkoutSession session, List<DataPoint> points,
      List<IntervalStep>? steps, {String? userDescription}) {
    final encoded = encode(session, points, steps);
    if (encoded.isEmpty) return userDescription ?? '';

    final metadata = '<!-- ROWMATE:$encoded -->';
    return userDescription != null && userDescription.isNotEmpty
        ? '$userDescription\n\n$metadata'
        : metadata;
  }
}

/// Metadata simplificada de un intervalo
class IntervalMetadata {
  final String type;
  final int? durationSeconds;
  final int? distanceMeters;
  final int? targetWattsMin;
  final int? targetWattsMax;
  final int? targetSpm;
  final int? targetSplitSeconds;

  const IntervalMetadata({
    required this.type,
    this.durationSeconds,
    this.distanceMeters,
    this.targetWattsMin,
    this.targetWattsMax,
    this.targetSpm,
    this.targetSplitSeconds,
  });
}
