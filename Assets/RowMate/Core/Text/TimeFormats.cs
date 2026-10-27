namespace RowMate.Core.Text
{
    // Única implementación de los formatos MM:SS y H:MM:SS del legacy (AC-BLE-01-06): RowingData, SessionStats,
    // WorkoutSession y StepLabels la llaman. Usa ~/ y % de Dart (DartMath), así los negativos dan lo mismo.
    // legacy: lib/core/models/rowing_data.dart:24-40, workout_session.dart:29-37 y :94-109, interval_step.dart:87-91.
    public static class TimeFormats
    {
        public const string NoSplit = "--:--";

        // = value.toString().padLeft(2, '0'): "5" → "05"; "-1" queda "-1" (ya tiene 2 caracteres).
        public static string Pad2(long value)
        {
            return Fmt.Integer(value).PadLeft(2, '0');
        }

        // Pad2(t ~/ 60) + ":" + Pad2(t % 60). MinSec(-65) = "-1:55", MinSec(-5) = "00:55", MinSec(6000) = "100:00".
        public static string MinSec(long totalSeconds)
        {
            return Pad2(DartMath.TruncDiv(totalSeconds, 60)) + ":" + Pad2(DartMath.Mod(totalSeconds, 60));
        }

        // Split: ≤ 0 → "--:--"; si no, MM:SS (legacy: rowing_data.dart:24-29, workout_session.dart:94-99).
        public static string SplitOrDashes(long seconds)
        {
            return seconds <= 0 ? NoSplit : MinSec(seconds);
        }

        // Tiempo: h ≥ 1 → "{h}:{MM}:{SS}", si no "{MM}:{SS}" (legacy: rowing_data.dart:32-40, workout_session.dart:29-37).
        public static string Elapsed(long seconds)
        {
            var h = DartMath.TruncDiv(seconds, 3600);
            var m = DartMath.TruncDiv(DartMath.Mod(seconds, 3600), 60);
            var s = DartMath.Mod(seconds, 60);
            if (h > 0)
            {
                return Fmt.Integer(h) + ":" + Pad2(m) + ":" + Pad2(s);
            }

            return Pad2(m) + ":" + Pad2(s);
        }
    }
}
