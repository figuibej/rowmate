using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RowMate.Core.Text
{
    // Fechas ISO-8601 con la semántica de DateTime de Dart (ADR 012 §3; legacy: lib/core/models/workout_session.dart:43-44, :57-60).
    public static class IsoDate
    {
        private const long TicksPerMicrosecond = 10;

        // Gramática de DateTime.parse (dart-sdk/lib/core/date_time.dart, _parseFormat), con [0-9] en vez de \d
        // (en .NET \d acepta dígitos Unicode) y \z en vez de $ (en .NET $ acepta un salto de línea final).
        private static readonly Regex ParseFormat = new Regex(
            @"^([+-]?[0-9]{4,6})-?([0-9]{2})-?([0-9]{2})" +
            @"(?:[ T]([0-9]{2})(?::?([0-9]{2})(?::?([0-9]{2})(?:[.,]([0-9]+))?)?)?" +
            @"( ?[zZ]| ?([-+])([0-9]{2})(?::?([0-9]{2}))?)?)?\z",
            RegexOptions.CultureInvariant);

        // = value.toUtc().toIso8601String(): yyyy-MM-ddTHH:mm:ss.mmm + uuu SOLO si los microsegundos no son 0 + Z.
        // Los 100 ns sobrantes de .NET se truncan (Dart no los tiene).
        public static string FormatUtc(DateTimeOffset value)
        {
            return Format(value.UtcDateTime, true);
        }

        // = toIso8601String() de un DateTime local de Dart: la hora de pared del offset, sin zona.
        public static string FormatLocal(DateTimeOffset value)
        {
            return Format(value.DateTime, false);
        }

        // = DateTime.parse(text).toLocal(): con zona (Z, ±HH, ±HHmm, ±HH:mm) es un instante que se convierte a
        // localZone; sin zona es la hora de pared de localZone. Los decimales de segundo se truncan a microsegundos.
        // Como Dart, los campos fuera de rango se normalizan (mes 13 = enero del año siguiente). Texto inválido o
        // fuera del rango de DateTimeOffset → FormatException.
        public static DateTimeOffset Parse(string text, TimeZoneInfo localZone)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (localZone == null)
            {
                throw new ArgumentNullException(nameof(localZone));
            }

            var match = ParseFormat.Match(text);
            if (!match.Success)
            {
                throw new FormatException("IsoDate.Parse: formato de fecha inválido: \"" + text + "\".");
            }

            var years = int.Parse(match.Groups[1].Value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var month = IntOrZero(match.Groups[2]);
            var day = IntOrZero(match.Groups[3]);
            var hour = IntOrZero(match.Groups[4]);
            var minute = IntOrZero(match.Groups[5]);
            var second = IntOrZero(match.Groups[6]);
            var micros = MilliAndMicroseconds(match.Groups[7]);
            var hasZone = match.Groups[8].Success;
            if (hasZone && match.Groups[9].Success)
            {
                var sign = match.Groups[9].Value == "-" ? -1 : 1;
                var minuteDifference = IntOrZero(match.Groups[11]) + 60 * IntOrZero(match.Groups[10]);
                minute -= sign * minuteDifference;
            }

            DateTime wall;
            try
            {
                if (years < 1 || years > 9999)
                {
                    throw new ArgumentOutOfRangeException(nameof(text));
                }

                // Normaliza como el constructor DateTime(y, m, d, h, min, s, ms, us) de Dart.
                var ticks = (((long)hour * 60 + minute) * 60 + second) * TimeSpan.TicksPerSecond + micros * TicksPerMicrosecond;
                wall = new DateTime(years, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
                    .AddMonths(month - 1)
                    .AddDays(day - 1)
                    .AddTicks(ticks);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new FormatException("IsoDate.Parse: fecha fuera de rango: \"" + text + "\".");
            }

            if (hasZone)
            {
                return TimeZoneInfo.ConvertTime(new DateTimeOffset(wall, TimeSpan.Zero), localZone);
            }

            return new DateTimeOffset(wall, localZone.GetUtcOffset(wall));
        }

        private static string Format(DateTime value, bool utc)
        {
            var year = value.Year;
            if (year < 0 || year > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "IsoDate: año fuera de [0; 9999].");
            }

            var micros = (int)((value.Ticks / TicksPerMicrosecond) % 1000);
            var length = 23 + (micros != 0 ? 3 : 0) + (utc ? 1 : 0);
            var chars = new char[length];
            Write(chars, 0, year, 4);
            chars[4] = '-';
            Write(chars, 5, value.Month, 2);
            chars[7] = '-';
            Write(chars, 8, value.Day, 2);
            chars[10] = 'T';
            Write(chars, 11, value.Hour, 2);
            chars[13] = ':';
            Write(chars, 14, value.Minute, 2);
            chars[16] = ':';
            Write(chars, 17, value.Second, 2);
            chars[19] = '.';
            Write(chars, 20, value.Millisecond, 3);
            var pos = 23;
            if (micros != 0)
            {
                Write(chars, pos, micros, 3);
                pos += 3;
            }

            if (utc)
            {
                chars[pos] = 'Z';
            }

            return new string(chars);
        }

        private static void Write(char[] chars, int start, int value, int width)
        {
            for (var i = width - 1; i >= 0; i--)
            {
                chars[start + i] = (char)('0' + value % 10);
                value /= 10;
            }
        }

        private static int IntOrZero(Group group)
        {
            return group.Success ? int.Parse(group.Value, NumberStyles.None, CultureInfo.InvariantCulture) : 0;
        }

        // Como parseMilliAndMicroseconds de Dart: solo las primeras 6 cifras, el resto se descarta.
        private static long MilliAndMicroseconds(Group group)
        {
            if (!group.Success)
            {
                return 0;
            }

            var digits = group.Value;
            long result = 0;
            for (var i = 0; i < 6; i++)
            {
                result *= 10;
                if (i < digits.Length)
                {
                    result += digits[i] - '0';
                }
            }

            return result;
        }
    }
}
