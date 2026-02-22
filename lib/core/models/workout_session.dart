/// Sesión de entrenamiento completada
class WorkoutSession {
  final int? id;
  final int? routineId;
  final String? routineName;
  final DateTime startedAt;
  final DateTime? finishedAt;
  final int totalDistanceMeters;
  final int totalTimeSeconds;
  final int avgPowerWatts;
  final double avgStrokeRate;
  final int totalCalories;

  const WorkoutSession({
    this.id,
    this.routineId,
    this.routineName,
    required this.startedAt,
    this.finishedAt,
    this.totalDistanceMeters = 0,
    this.totalTimeSeconds = 0,
    this.avgPowerWatts = 0,
    this.avgStrokeRate = 0,
    this.totalCalories = 0,
  });

  String get durationFormatted {
    final h = totalTimeSeconds ~/ 3600;
    final m = (totalTimeSeconds % 3600) ~/ 60;
    final s = totalTimeSeconds % 60;
    if (h > 0) {
      return '$h:${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
    }
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  Map<String, dynamic> toMap() => {
    if (id != null) 'id': id,
    'routine_id': routineId,
    'routine_name': routineName,
    'started_at': startedAt.toIso8601String(),
    'finished_at': finishedAt?.toIso8601String(),
    'total_distance_meters': totalDistanceMeters,
    'total_time_seconds': totalTimeSeconds,
    'avg_power_watts': avgPowerWatts,
    'avg_stroke_rate': avgStrokeRate,
    'total_calories': totalCalories,
  };

  factory WorkoutSession.fromMap(Map<String, dynamic> map) => WorkoutSession(
    id: map['id'] as int?,
    routineId: map['routine_id'] as int?,
    routineName: map['routine_name'] as String?,
    startedAt: DateTime.parse(map['started_at'] as String),
    finishedAt: map['finished_at'] != null
        ? DateTime.parse(map['finished_at'] as String)
        : null,
    totalDistanceMeters: map['total_distance_meters'] as int? ?? 0,
    totalTimeSeconds: map['total_time_seconds'] as int? ?? 0,
    avgPowerWatts: map['avg_power_watts'] as int? ?? 0,
    avgStrokeRate: (map['avg_stroke_rate'] as num?)?.toDouble() ?? 0,
    totalCalories: map['total_calories'] as int? ?? 0,
  );
}

/// Punto de telemetría individual dentro de una sesión
class DataPoint {
  final int? id;
  final int sessionId;
  final int elapsedSeconds;
  final double strokeRate;
  final int strokeCount;
  final int distanceMeters;
  final int pace500mSeconds;
  final int powerWatts;
  final int calories;
  final int heartRate;
  final int? stepIndex;
  final String? stepType;    // warmup, work, rest, cooldown

  const DataPoint({
    this.id,
    required this.sessionId,
    required this.elapsedSeconds,
    this.strokeRate = 0,
    this.strokeCount = 0,
    this.distanceMeters = 0,
    this.pace500mSeconds = 0,
    this.powerWatts = 0,
    this.calories = 0,
    this.heartRate = 0,
    this.stepIndex,
    this.stepType,
  });

  Map<String, dynamic> toMap() => {
    if (id != null) 'id': id,
    'session_id': sessionId,
    'elapsed_seconds': elapsedSeconds,
    'stroke_rate': strokeRate,
    'stroke_count': strokeCount,
    'distance_meters': distanceMeters,
    'pace_500m_seconds': pace500mSeconds,
    'power_watts': powerWatts,
    'calories': calories,
    'heart_rate': heartRate,
    'step_index': stepIndex,
    'step_type': stepType,
  };
}
