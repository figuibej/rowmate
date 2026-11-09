using System;

namespace RowMate.Tests.Support
{
    // Zonas horarias de las pruebas del TCX (It. 12; 02-tech-design §6.2, m-10), todas con CreateCustomTimeZone: iguales en .NET 8
    // y en Mono, sin depender de la base de zonas del equipo.
    // - UtcMinus3 = HistorySeeds.Zone (la de las pruebas, spec §0); Utc; Kolkata +05:30; Kathmandu +05:45; StJohns −03:30 FIJA (el
    //   5/11/2026 ya no rige el horario de verano, que terminó el 1/11).
    // - Madrid: +01:00 con horario de verano (+1 h) del último domingo de marzo a las 02:00 (hora estándar) al último domingo de
    //   octubre a las 03:00 (hora de verano), la regla de Europe/Madrid.
    public static class TestZones
    {
        public static readonly TimeZoneInfo UtcMinus3 = HistorySeeds.Zone;

        public static readonly TimeZoneInfo Utc =
            TimeZoneInfo.CreateCustomTimeZone("RowMate-UTC", TimeSpan.Zero, "UTC", "UTC");

        public static readonly TimeZoneInfo Kolkata =
            TimeZoneInfo.CreateCustomTimeZone("RowMate-Asia-Kolkata", new TimeSpan(5, 30, 0), "Asia/Kolkata (prueba)", "IST");

        public static readonly TimeZoneInfo Kathmandu =
            TimeZoneInfo.CreateCustomTimeZone("RowMate-Asia-Kathmandu", new TimeSpan(5, 45, 0), "Asia/Kathmandu (prueba)", "NPT");

        public static readonly TimeZoneInfo StJohns =
            TimeZoneInfo.CreateCustomTimeZone("RowMate-America-St_Johns", -new TimeSpan(3, 30, 0), "America/St_Johns (prueba)", "NST");

        public static readonly TimeZoneInfo Madrid = CreateMadrid();

        private static TimeZoneInfo CreateMadrid()
        {
            var start = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 5, DayOfWeek.Sunday);
            var end = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 3, 0, 0), 10, 5, DayOfWeek.Sunday);
            var rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2000, 1, 1), new DateTime(2099, 12, 31),
                TimeSpan.FromHours(1), start, end);
            return TimeZoneInfo.CreateCustomTimeZone("RowMate-Europe-Madrid", TimeSpan.FromHours(1), "Europe/Madrid (prueba)", "CET", "CEST",
                new[] { rule });
        }
    }
}
