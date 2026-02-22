import 'interval_step.dart';

/// Una rutina de entrenamiento con sus pasos (intervalos/series/descanso)
class Routine {
  final int? id;
  final String name;
  final String description;
  final DateTime createdAt;
  final List<IntervalStep> steps;

  const Routine({
    this.id,
    required this.name,
    this.description = '',
    required this.createdAt,
    this.steps = const [],
  });

  /// Duración total estimada en segundos (solo pasos con duración fija)
  int get totalDurationSeconds =>
      steps.fold(0, (sum, s) => sum + (s.durationSeconds ?? 0));

  /// Expande las series en pasos individuales secuenciales
  List<IntervalStep> get flattenedSteps {
    final result = <IntervalStep>[];
    int i = 0;
    while (i < steps.length) {
      final step = steps[i];
      if (step.groupId == null) {
        result.add(step);
        i++;
      } else {
        // Recolectar todos los pasos de este grupo
        final gid = step.groupId!;
        final repeat = step.groupRepeatCount ?? 1;
        final group = <IntervalStep>[];
        while (i < steps.length && steps[i].groupId == gid) {
          group.add(steps[i]);
          i++;
        }
        // Repetir el grupo N veces
        for (var r = 0; r < repeat; r++) {
          result.addAll(group);
        }
      }
    }
    return result;
  }

  /// Resumen breve de la rutina
  String get summary {
    final workSteps = steps.where((s) => s.type == StepType.work).length;
    final restSteps = steps.where((s) => s.type == StepType.rest).length;
    if (workSteps == 0) return '${steps.length} pasos';
    return '$workSteps series · $restSteps descansos';
  }

  Map<String, dynamic> toMap() => {
    if (id != null) 'id': id,
    'name': name,
    'description': description,
    'created_at': createdAt.toIso8601String(),
  };

  factory Routine.fromMap(Map<String, dynamic> map,
      {List<IntervalStep> steps = const []}) =>
      Routine(
        id: map['id'] as int?,
        name: map['name'] as String,
        description: map['description'] as String? ?? '',
        createdAt: DateTime.parse(map['created_at'] as String),
        steps: steps,
      );

  Routine copyWith({
    int? id,
    String? name,
    String? description,
    DateTime? createdAt,
    List<IntervalStep>? steps,
  }) =>
      Routine(
        id: id ?? this.id,
        name: name ?? this.name,
        description: description ?? this.description,
        createdAt: createdAt ?? this.createdAt,
        steps: steps ?? this.steps,
      );
}
