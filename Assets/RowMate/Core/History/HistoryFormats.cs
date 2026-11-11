using System;
using RowMate.Core.Text;

namespace RowMate.Core.History
{
    // Formatos del historial y del detalle (F-HIS-05; spec §4; AC-HIS-05-01 a -04, AC-HIS-13-04, -05, -07). Todos con la
    // aritmética de Dart (DartMath: ~/ trunca, % nunca negativo) y sin cultura: iguales en es y en (Q-06). Los de la sesión
    // (duración y split, MM:SS) son SessionStats.DurationFormatted y SplitFormatted (TimeFormats), que no se repiten acá.
    // legacy: history_screen.dart:74 (hs), session_detail_screen.dart:218-224, :254-261, :434-461, :501-510 (sd).
    public static class HistoryFormats
    {
        // "--:-- s": el tooltip del split en un punto sin ritmo (Q-24, AC-HIS-13-07).
        public const string NoPaceTooltip = TimeFormats.NoSplit + " s";

        // Caracteres que puede ocupar WriteTooltip: '-', 20 cifras, ".x", " " y "bpm".
        public const int TooltipMaxChars = 32;

        // hs:74, DateFormat('dd/MM/yyyy  HH:mm') sobre la hora local (dos espacios, 24 h): por componentes, sin cultura.
        public static string CardDate(DateTimeOffset local)
        {
            return Pad(local.Day, 2) + "/" + Pad(local.Month, 2) + "/" + Pad(local.Year, 4) + "  " + Pad(local.Hour, 2) + ":"
                + Pad(local.Minute, 2);
        }

        // sd:221: '${d ~/ 60}:${(d % 60).padLeft(2, '0')}' (minutos sin relleno): 45 → "0:45", 3600 → "60:00".
        public static string StepDuration(int seconds)
        {
            return MinutesColonSeconds(seconds);
        }

        // It. 15 (#75, AC-D15-23, DV-141): el split de la tarjeta y del resumen en m:ss (2:13, 10:05), como las filas por paso, los
        // ejes y el tooltip; "--:--" sin dato (≤ 0). Cambio frente al legacy (workout_session.dart:94-98, mm:ss); Dispositivo y el
        // HUD siguen con SessionStats.SplitFormatted (mm:ss).
        public static string SummarySplit(long seconds)
        {
            return seconds <= 0 ? TimeFormats.NoSplit : MinutesColonSeconds(seconds);
        }

        // sd:254: '$durStr  ·  ${dist}m' (dos espacios a cada lado del punto medio).
        public static string StepLine1(int seconds, int meters)
        {
            return StepDuration(seconds) + "  ·  " + Fmt.Integer(meters) + "m";
        }

        // sd:259: '${avgWatts.round()}W'.
        public static string StepWatts(int watts)
        {
            return Fmt.Integer(watts) + "W";
        }

        // sd:260: '${avgSpm.toStringAsFixed(1)} spm'.
        public static string StepSpm(double spm)
        {
            return Fmt.Fixed(spm, 1) + " spm";
        }

        // sd:222-224, :261: '$splitMin:${splitSec}/500m', sin caso especial para 0 ("0:00/500m").
        public static string StepSplit(int seconds)
        {
            return MinutesColonSeconds(seconds) + "/500m";
        }

        // sd:434-436: value ~/ 60 y (value % 60).toInt(), m:ss sin horas (3900 → "65:00").
        public static string AxisTime(double seconds)
        {
            var minutes = DartMath.TruncDiv(seconds, 60.0);
            var rest = (long)Math.Truncate(DartMath.Mod(seconds, 60.0));
            return Fmt.Integer(minutes) + ":" + TimeFormats.Pad2(rest);
        }

        // sd:459-461: '${value.toInt()}' (entero truncado).
        public static string AxisValue(double value)
        {
            return Fmt.Integer(Truncate(value));
        }

        // sd:447-456: el ritmo real (lento − y).toInt() en m:ss (DT9-08).
        public static string AxisPace(double slowestPace, double value)
        {
            return MinutesColonSeconds(Truncate(slowestPace - value));
        }

        // sd:501-510 + Q-24: "{valor con 1 decimal} {unidad}"; en el split, el ritmo del punto en m:ss + " s", o "--:-- s" si
        // no tiene ritmo. raw es el valor ORIGINAL del punto (el ritmo, no el invertido). Sin asignar: escribe en buffer desde
        // offset y devuelve cuántos caracteres escribió (DT9-12).
        public static int WriteTooltip(ChartMetric metric, double raw, char[] buffer, int offset)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            var at = offset;
            if (metric == ChartMetric.Split)
            {
                if (raw <= 0)
                {
                    return Copy(NoPaceTooltip, buffer, at) - offset;
                }

                var pace = Truncate(raw);
                at += Fmt.WriteInteger(DartMath.TruncDiv(pace, 60), buffer, at);
                at = Put(buffer, at, ':');
                at += TimeFormats.WritePad2(DartMath.Mod(pace, 60), buffer, at);
                at = Put(buffer, at, ' ');
                at = Put(buffer, at, 's');
                return at - offset;
            }

            if (!Fmt.TryFormatFixed(raw, 1, new Span<char>(buffer, at, buffer.Length - at), out var written))
            {
                throw new ArgumentException("HistoryFormats.WriteTooltip: el texto no entra en el buffer.", nameof(buffer));
            }

            at += written;
            at = Put(buffer, at, ' ');
            at = Copy(ChartMetrics.Unit(metric), buffer, at);
            return at - offset;
        }

        // Lo mismo como texto (pruebas y lugares que no son por cuadro).
        public static string Tooltip(ChartMetric metric, double raw)
        {
            var buffer = new char[TooltipMaxChars];
            var count = WriteTooltip(metric, raw, buffer, 0);
            return new string(buffer, 0, count);
        }

        // '${t ~/ 60}:${(t % 60).toString().padLeft(2, '0')}' con int.
        private static string MinutesColonSeconds(long seconds)
        {
            return Fmt.Integer(DartMath.TruncDiv(seconds, 60)) + ":" + TimeFormats.Pad2(DartMath.Mod(seconds, 60));
        }

        // double.toInt() de Dart: trunca hacia cero.
        private static long Truncate(double value)
        {
            return (long)Math.Truncate(value);
        }

        // padLeft de Dart sobre el entero (DateFormat rellena con ceros hasta el ancho del patrón).
        private static string Pad(int value, int width)
        {
            return Fmt.Integer(value).PadLeft(width, '0');
        }

        private static int Put(char[] buffer, int at, char c)
        {
            if (at >= buffer.Length)
            {
                throw new ArgumentException("HistoryFormats.WriteTooltip: el texto no entra en el buffer.", nameof(buffer));
            }

            buffer[at] = c;
            return at + 1;
        }

        private static int Copy(string text, char[] buffer, int at)
        {
            for (var i = 0; i < text.Length; i++)
            {
                at = Put(buffer, at, text[i]);
            }

            return at;
        }
    }
}
