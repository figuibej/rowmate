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

        // It. 4b (QA-V4b-MOT-01): Pad2(value) en buffer desde offset, sin asignar. Devuelve cuántos caracteres escribió.
        public static int WritePad2(long value, char[] buffer, int offset)
        {
            if (value >= 0 && value < 10)
            {
                if (buffer == null)
                {
                    throw new System.ArgumentNullException(nameof(buffer));
                }

                if (offset < 0 || offset + 2 > buffer.Length)
                {
                    throw new System.ArgumentOutOfRangeException(nameof(offset));
                }

                buffer[offset] = '0';
                buffer[offset + 1] = (char)('0' + (int)value);
                return 2;
            }

            return Fmt.WriteInteger(value, buffer, offset);
        }

        // Caracteres que puede ocupar Elapsed: las horas de long.MaxValue (16 cifras) y ":MM:SS".
        public const int ElapsedMaxChars = 22;

        // It. 4b (QA-V4b-MOT-01): los mismos caracteres que Elapsed(seconds), en buffer desde offset y sin asignar (el conteo del
        // diálogo de completado). Devuelve cuántos escribió.
        public static int WriteElapsed(long seconds, char[] buffer, int offset)
        {
            var h = DartMath.TruncDiv(seconds, 3600);
            var m = DartMath.TruncDiv(DartMath.Mod(seconds, 3600), 60);
            var s = DartMath.Mod(seconds, 60);
            var at = offset;
            if (h > 0)
            {
                at += Fmt.WriteInteger(h, buffer, at);
                buffer[at++] = ':';
            }

            at += WritePad2(m, buffer, at);
            buffer[at++] = ':';
            at += WritePad2(s, buffer, at);
            return at - offset;
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
