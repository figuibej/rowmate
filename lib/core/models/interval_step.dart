enum StepType { warmup, work, rest, cooldown }

extension StepTypeLabel on StepType {
  String get label {
    switch (this) {
      case StepType.warmup:
        return 'Calentamiento';
      case StepType.work:
        return 'Trabajo';
      case StepType.rest:
        return 'Descanso';
      case StepType.cooldown:
        return 'Enfriamiento';
    }
  }

  bool get isRest => this == StepType.rest;
}

/// Un paso dentro de una rutina (intervalo de trabajo, descanso, etc.)
class IntervalStep {
  final int? id;
  final int routineId;
  final int order;
  final StepType type;

  // Duración: uno de los dos debe estar definido
  final int? durationSeconds;   // duración fija
  final int? distanceMeters;    // distancia fija

  // Objetivos opcionales
  final int? targetWattsMin;
  final int? targetWattsMax;
  final int? targetSpm;            // strokes per minute objetivo
  final int? targetSplitSeconds;   // pace 500m objetivo (segundos)

  // Agrupamiento en serie
  final String? groupId;           // UUID del grupo (null = paso suelto)
  final int? groupRepeatCount;     // repeticiones del grupo

  const IntervalStep({
    this.id,
    required this.routineId,
    required this.order,
    required this.type,
    this.durationSeconds,
    this.distanceMeters,
    this.targetWattsMin,
    this.targetWattsMax,
    this.targetSpm,
    this.targetSplitSeconds,
    this.groupId,
    this.groupRepeatCount,
  });

  bool get isTimeBased => durationSeconds != null;
  bool get isDistanceBased => distanceMeters != null;

  String get durationLabel {
    if (durationSeconds != null) {
      final m = durationSeconds! ~/ 60;
      final s = durationSeconds! % 60;
      if (m > 0 && s > 0) return '${m}m ${s}s';
      if (m > 0) return '${m}m';
      return '${s}s';
    }
    if (distanceMeters != null) return '${distanceMeters}m';
    return '?';
  }

  String get targetLabel {
    final parts = <String>[];
    if (targetWattsMin != null && targetWattsMax != null) {
      parts.add('${targetWattsMin}-${targetWattsMax}W');
    } else if (targetWattsMin != null) {
      parts.add('>${targetWattsMin}W');
    }
    if (targetSpm != null) parts.add('${targetSpm} spm');
    if (targetSplitSeconds != null) {
      final m = targetSplitSeconds! ~/ 60;
      final s = targetSplitSeconds! % 60;
      parts.add('${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}/500m');
    }
    return parts.join(' · ');
  }

  Map<String, dynamic> toMap() => {
    if (id != null) 'id': id,
    'routine_id': routineId,
    'step_order': order,
    'type': type.name,
    'duration_seconds': durationSeconds,
    'distance_meters': distanceMeters,
    'target_watts_min': targetWattsMin,
    'target_watts_max': targetWattsMax,
    'target_spm': targetSpm,
    'target_split_seconds': targetSplitSeconds,
    'group_id': groupId,
    'group_repeat_count': groupRepeatCount,
  };

  factory IntervalStep.fromMap(Map<String, dynamic> map) => IntervalStep(
    id: map['id'] as int?,
    routineId: map['routine_id'] as int,
    order: map['step_order'] as int,
    type: StepType.values.firstWhere((e) => e.name == map['type']),
    durationSeconds: map['duration_seconds'] as int?,
    distanceMeters: map['distance_meters'] as int?,
    targetWattsMin: map['target_watts_min'] as int?,
    targetWattsMax: map['target_watts_max'] as int?,
    targetSpm: map['target_spm'] as int?,
    targetSplitSeconds: map['target_split_seconds'] as int?,
    groupId: map['group_id'] as String?,
    groupRepeatCount: map['group_repeat_count'] as int?,
  );

  IntervalStep copyWith({
    int? id,
    int? routineId,
    int? order,
    StepType? type,
    int? durationSeconds,
    int? distanceMeters,
    int? targetWattsMin,
    int? targetWattsMax,
    int? targetSpm,
    int? targetSplitSeconds,
    String? groupId,
    int? groupRepeatCount,
  }) =>
      IntervalStep(
        id: id ?? this.id,
        routineId: routineId ?? this.routineId,
        order: order ?? this.order,
        type: type ?? this.type,
        durationSeconds: durationSeconds ?? this.durationSeconds,
        distanceMeters: distanceMeters ?? this.distanceMeters,
        targetWattsMin: targetWattsMin ?? this.targetWattsMin,
        targetWattsMax: targetWattsMax ?? this.targetWattsMax,
        targetSpm: targetSpm ?? this.targetSpm,
        targetSplitSeconds: targetSplitSeconds ?? this.targetSplitSeconds,
        groupId: groupId ?? this.groupId,
        groupRepeatCount: groupRepeatCount ?? this.groupRepeatCount,
      );
}
