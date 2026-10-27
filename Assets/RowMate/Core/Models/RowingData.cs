using RowMate.Core.Text;

namespace RowMate.Core.Models
{
    // Lectura en vivo del remo: los 8 campos que decodifica FtmsParser y que emite el simulador (F-BLE-01).
    // Todos enteros salvo el ritmo; una lectura vacía tiene los 8 en 0. Pulso 0 = "sin dato", no "0 bpm".
    // legacy: lib/core/models/rowing_data.dart:2-62
    public sealed record RowingData
    {
        // = const RowingData() del legacy (rowing_data.dart:12-21).
        public static readonly RowingData Empty = new RowingData();

        // spm, con decimales (FTMS: byte crudo × 0,5; simulador: 1 decimal como Dart).
        public double StrokeRate { get; init; }

        // Paladas totales.
        public int StrokeCount { get; init; }

        // Metros recorridos.
        public int DistanceMeters { get; init; }

        // Segundos por 500 m (≤ 0 = sin split).
        public int Pace500mSeconds { get; init; }

        // Vatios (FTMS los manda con signo, sin filtrar: Q-38).
        public int PowerWatts { get; init; }

        // kcal totales.
        public int TotalCalories { get; init; }

        // bpm (0 = sin dato: no hay banda ni pulso del monitor).
        public int HeartRate { get; init; }

        // Segundos transcurridos.
        public int ElapsedSeconds { get; init; }

        // "--:--" si es ≤ 0, si no MM:SS (legacy: rowing_data.dart:24-29). Regla compartida (AC-BLE-01-06).
        public string Pace500mFormatted => TimeFormats.SplitOrDashes(Pace500mSeconds);

        // H:MM:SS con al menos 1 hora, si no MM:SS (legacy: rowing_data.dart:32-40). Regla compartida (AC-BLE-01-06).
        public string ElapsedFormatted => TimeFormats.Elapsed(ElapsedSeconds);

        // = copyWith del legacy (rowing_data.dart:42-62): null = conservar. Un campo que no se nombra nunca se borra.
        public RowingData CopyWith(
            double? strokeRate = null,
            int? strokeCount = null,
            int? distanceMeters = null,
            int? pace500mSeconds = null,
            int? powerWatts = null,
            int? totalCalories = null,
            int? heartRate = null,
            int? elapsedSeconds = null)
        {
            return new RowingData
            {
                StrokeRate = strokeRate ?? StrokeRate,
                StrokeCount = strokeCount ?? StrokeCount,
                DistanceMeters = distanceMeters ?? DistanceMeters,
                Pace500mSeconds = pace500mSeconds ?? Pace500mSeconds,
                PowerWatts = powerWatts ?? PowerWatts,
                TotalCalories = totalCalories ?? TotalCalories,
                HeartRate = heartRate ?? HeartRate,
                ElapsedSeconds = elapsedSeconds ?? ElapsedSeconds,
            };
        }

        // El pulsómetro reemplaza solo el pulso de la lectura del remo (= copyWith(heartRate: bpm)).
        public RowingData WithHeartRate(int bpm)
        {
            return CopyWith(heartRate: bpm);
        }
    }
}
