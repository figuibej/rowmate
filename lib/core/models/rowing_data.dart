/// Métricas en tiempo real del rower (parseadas desde FTMS 0x2AD2)
class RowingData {
  final double strokeRate;     // spm
  final int strokeCount;       // total de remadas
  final int distanceMeters;    // metros recorridos
  final int pace500mSeconds;   // segundos por 500m
  final int powerWatts;        // vatios
  final int totalCalories;     // kcal
  final int heartRate;         // bpm (0 si no hay banda)
  final int elapsedSeconds;    // tiempo total en segundos

  const RowingData({
    this.strokeRate = 0,
    this.strokeCount = 0,
    this.distanceMeters = 0,
    this.pace500mSeconds = 0,
    this.powerWatts = 0,
    this.totalCalories = 0,
    this.heartRate = 0,
    this.elapsedSeconds = 0,
  });

  /// Pace formateado como MM:SS
  String get pace500mFormatted {
    if (pace500mSeconds <= 0) return '--:--';
    final m = pace500mSeconds ~/ 60;
    final s = pace500mSeconds % 60;
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  /// Tiempo formateado como H:MM:SS o MM:SS
  String get elapsedFormatted {
    final h = elapsedSeconds ~/ 3600;
    final m = (elapsedSeconds % 3600) ~/ 60;
    final s = elapsedSeconds % 60;
    if (h > 0) {
      return '$h:${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
    }
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  RowingData copyWith({
    double? strokeRate,
    int? strokeCount,
    int? distanceMeters,
    int? pace500mSeconds,
    int? powerWatts,
    int? totalCalories,
    int? heartRate,
    int? elapsedSeconds,
  }) {
    return RowingData(
      strokeRate: strokeRate ?? this.strokeRate,
      strokeCount: strokeCount ?? this.strokeCount,
      distanceMeters: distanceMeters ?? this.distanceMeters,
      pace500mSeconds: pace500mSeconds ?? this.pace500mSeconds,
      powerWatts: powerWatts ?? this.powerWatts,
      totalCalories: totalCalories ?? this.totalCalories,
      heartRate: heartRate ?? this.heartRate,
      elapsedSeconds: elapsedSeconds ?? this.elapsedSeconds,
    );
  }
}
