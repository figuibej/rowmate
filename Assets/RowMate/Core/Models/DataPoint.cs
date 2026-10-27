namespace RowMate.Core.Models
{
    // Punto de telemetría de una sesión (F-RUT-17; legacy: lib/core/models/workout_session.dart:140-182).
    // Sesión y tiempo transcurrido son obligatorios y sin valor por defecto (el tiempo es el DEL MONITOR,
    // workout_provider.dart:162, :261; D-12). SPM, paladas, distancia, split, potencia, calorías y pulso: 0 por defecto.
    // Índice y tipo del paso: vacíos por defecto (y siempre en entrenamiento libre). Id vacío hasta guardarse.
    public sealed record DataPoint
    {
        public DataPoint(long sessionId, int elapsedSeconds)
        {
            SessionId = sessionId;
            ElapsedSeconds = elapsedSeconds;
        }

        public long? Id { get; init; }

        public long SessionId { get; init; }

        public int ElapsedSeconds { get; init; }

        public double StrokeRate { get; init; }

        public int StrokeCount { get; init; }

        public int DistanceMeters { get; init; }

        public int Pace500mSeconds { get; init; }

        public int PowerWatts { get; init; }

        public int Calories { get; init; }

        // 0 = sin dato.
        public int HeartRate { get; init; }

        // Índice del paso en la rutina EXPANDIDA (no en la plantilla: DC-48, AC-RUT-17-02).
        public int? StepIndex { get; init; }

        // Nombre guardado del tipo ("warmup", "work", "rest", "cooldown"), como texto igual que el legacy (D-12).
        public string? StepTypeName { get; init; }

        // Marca el punto con el paso en curso de la rutina expandida.
        public DataPoint WithStep(int flattenedIndex, StepType type)
        {
            return this with { StepIndex = flattenedIndex, StepTypeName = StepTypeNames.ToStorage(type) };
        }
    }
}
