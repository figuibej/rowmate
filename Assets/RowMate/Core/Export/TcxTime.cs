using System;
using System.Globalization;
using System.Text;

namespace RowMate.Core.Export
{
    // Fecha y hora del TCX (AC-STR-09-02, -03; legacy: _formatWithTimezone, tcx_builder.dart:130-143):
    // AAAA-MM-DDTHH:MM:SS±HH:MM en la hora local de la zona, con el desfase vigente EN ESE INSTANTE (DT12-02: Dart suma los
    // segundos sobre el instante y cada DateTime local trae su propio timeZoneOffset). Sin milisegundos (se truncan), año con 4
    // dígitos, el resto con 2; el signo '-' solo si el desfase es negativo (cero lleva '+'), horas = |desfase| en horas enteras y
    // minutos = |desfase| en minutos % 60.
    public static class TcxTime
    {
        public static string Format(DateTimeOffset instant, TimeZoneInfo zone)
        {
            var builder = new StringBuilder(25);
            Append(builder, instant, zone);
            return builder.ToString();
        }

        // La hora local del instante en la zona (con su desfase).
        public static DateTimeOffset ToLocal(DateTimeOffset instant, TimeZoneInfo zone)
        {
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }

            return TimeZoneInfo.ConvertTime(instant, zone);
        }

        public static void Append(StringBuilder builder, DateTimeOffset instant, TimeZoneInfo zone)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var local = ToLocal(instant, zone);
            var offset = local.Offset;
            Pad(builder, local.Year, 4);
            builder.Append('-');
            Pad(builder, local.Month, 2);
            builder.Append('-');
            Pad(builder, local.Day, 2);
            builder.Append('T');
            Pad(builder, local.Hour, 2);
            builder.Append(':');
            Pad(builder, local.Minute, 2);
            builder.Append(':');
            Pad(builder, local.Second, 2);
            builder.Append(offset < TimeSpan.Zero ? '-' : '+');
            // offset.inHours.abs() y offset.inMinutes.abs() % 60 (enteros truncados, como Duration de Dart).
            var totalMinutes = (long)Math.Abs(offset.TotalMinutes);
            Pad(builder, (int)(totalMinutes / 60), 2);
            builder.Append(':');
            Pad(builder, (int)(totalMinutes % 60), 2);
        }

        // = n.toString().padLeft(width, '0') para n ≥ 0.
        internal static void Pad(StringBuilder builder, int value, int width)
        {
            var digits = 1;
            for (var rest = value / 10; rest > 0; rest /= 10)
            {
                digits++;
            }

            for (var i = digits; i < width; i++)
            {
                builder.Append('0');
            }

            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
