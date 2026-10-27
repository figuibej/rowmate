using System;
using NUnit.Framework;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Text
{
    // IsoDate = toIso8601String() y DateTime.parse(...).toLocal() de Dart (ADR 012 §3; legacy: workout_session.dart:43-44, :57-60).
    // Parte de AC-RUT-16-04 que es de WP-A (el codec de la sesión es de WP-C).
    [TestFixture]
    public sealed class IsoDateTests
    {
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Zona fija UTC−3 (como el test de la sesión): no depende del equipo.
        private static readonly TimeZoneInfo UtcMinus3 =
            TimeZoneInfo.CreateCustomTimeZone("RowMate/UTC-3", TimeSpan.FromHours(-3), "UTC-3", "UTC-3");

        [Test, Category("AC-RUT-16-04")]
        public void FormatUtcAddsMicrosecondsOnlyWhenNotZero()
        {
            var baseTime = new DateTimeOffset(2026, 10, 27, 9, 37, 38, 628, TimeSpan.Zero);
            Assert.That(IsoDate.FormatUtc(baseTime.AddTicks(537 * 10)), Is.EqualTo("2026-10-27T09:37:38.628537Z"));
            Assert.That(IsoDate.FormatUtc(baseTime), Is.EqualTo("2026-10-27T09:37:38.628Z"));
            Assert.That(IsoDate.FormatUtc(new DateTimeOffset(2026, 10, 27, 9, 37, 38, TimeSpan.Zero)), Is.EqualTo("2026-10-27T09:37:38.000Z"));
        }

        [Test, Category("AC-RUT-16-04")]
        public void FormatUtcConvertsFromAnyOffsetAndTruncatesTicks()
        {
            var local = new DateTimeOffset(2026, 10, 27, 6, 37, 38, 628, TimeSpan.FromHours(-3)).AddTicks(5379);
            Assert.That(IsoDate.FormatUtc(local), Is.EqualTo("2026-10-27T09:37:38.628537Z"));
            Assert.That(IsoDate.FormatUtc(new DateTimeOffset(999, 1, 2, 3, 4, 5, TimeSpan.Zero)), Is.EqualTo("0999-01-02T03:04:05.000Z"));
        }

        [Test, Category("AC-RUT-16-04")]
        public void FormatLocalIsTheWallClockWithoutZone()
        {
            var local = new DateTimeOffset(2026, 10, 27, 6, 37, 38, 628, TimeSpan.FromHours(-3));
            Assert.That(IsoDate.FormatLocal(local), Is.EqualTo("2026-10-27T06:37:38.628"));
            Assert.That(IsoDate.FormatLocal(local.AddTicks(10)), Is.EqualTo("2026-10-27T06:37:38.628001"));
        }

        [Test, Category("AC-RUT-16-04")]
        public void ParseWithZoneIsAnInstantConvertedToTheLocalZone()
        {
            var parsed = IsoDate.Parse("2026-10-27T09:37:38.628537Z", UtcMinus3);
            Assert.That(parsed.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
            Assert.That(parsed.DateTime, Is.EqualTo(new DateTime(2026, 10, 27, 6, 37, 38, 628).AddTicks(5370)));
            Assert.That(IsoDate.FormatUtc(parsed), Is.EqualTo("2026-10-27T09:37:38.628537Z"));
        }

        [Test, Category("AC-RUT-16-04")]
        public void ParseWithoutZoneIsTheLocalWallClock()
        {
            var parsed = IsoDate.Parse("2026-10-27T09:37:38", UtcMinus3);
            Assert.That(parsed.DateTime, Is.EqualTo(new DateTime(2026, 10, 27, 9, 37, 38)));
            Assert.That(parsed.Offset, Is.EqualTo(TimeSpan.FromHours(-3)));
        }

        [Test, Category("AC-RUT-16-04")]
        public void ParseTruncatesToMicroseconds()
        {
            // DateTime.parse con 7 decimales trunca a .123456 (03-qa-harmony §4, fila 18).
            var parsed = IsoDate.Parse("2026-10-27T09:37:38.1234567Z", UtcMinus3);
            Assert.That(parsed.UtcDateTime.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234560L));
        }

        [TestCase("")]
        [TestCase("2026-10-27T")]
        [TestCase("2026/10/27")]
        [TestCase("27-10-2026")]
        [TestCase("2026-10-27T09:37:38+3")]
        [TestCase("2026-10-27Z")]
        [TestCase("0000-01-01T00:00:00Z")]
        public void ParseRejectsInvalidText(string text)
        {
            Assert.Throws<FormatException>(() => IsoDate.Parse(text, UtcMinus3));
        }

        [Test, Category("AC-RUT-16-04")]
        public void RoundTripThroughUtcText()
        {
            var original = new DateTimeOffset(2026, 3, 4, 5, 6, 7, 89, TimeSpan.FromHours(-3)).AddTicks(120);
            var text = IsoDate.FormatUtc(original);
            Assert.That(IsoDate.FormatUtc(IsoDate.Parse(text, UtcMinus3)), Is.EqualTo(text));
        }

        [Test, Category("AC-RUT-16-04"), Category("AC-BASE-03")]
        public void MatchesOracle_FormatUtc()
        {
            GoldenVectors.CheckRows("fmt.json", "iso8601", row =>
            {
                var instant = Epoch.AddTicks(row.Int("micros") * 10);
                var expected = row.Text("out");
                var actual = IsoDate.FormatUtc(instant);
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
        }

        [Test, Category("AC-RUT-16-04"), Category("AC-BASE-03")]
        public void MatchesOracle_FormatLocal()
        {
            GoldenVectors.CheckRows("fmt.json", "iso8601Local", row =>
            {
                var value = new DateTimeOffset((int)row.Int("year"), (int)row.Int("month"), (int)row.Int("day"),
                    (int)row.Int("hour"), (int)row.Int("minute"), (int)row.Int("second"), (int)row.Int("millisecond"),
                    TimeSpan.FromHours(-3)).AddTicks(row.Int("microsecond") * 10);
                var expected = row.Text("out");
                var actual = IsoDate.FormatLocal(value);
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
        }

        [Test, Category("AC-RUT-16-04"), Category("AC-BASE-03")]
        public void MatchesOracle_ParseWithZone()
        {
            GoldenVectors.CheckRows("fmt.json", "isoParse", row =>
            {
                var expected = row.Int("micros");
                var parsed = IsoDate.Parse(row.Text("text"), UtcMinus3);
                var actual = (parsed.UtcTicks - Epoch.UtcTicks) / 10;
                return actual == expected ? null : "esperado " + expected + " µs, obtenido " + actual + " µs";
            });
        }

        [Test, Category("AC-RUT-16-04"), Category("AC-BASE-03")]
        public void MatchesOracle_ParseWithoutZone()
        {
            GoldenVectors.CheckRows("fmt.json", "isoParseLocal", row =>
            {
                var wall = IsoDate.Parse(row.Text("text"), UtcMinus3).DateTime;
                var micro = (wall.Ticks / 10) % 1000;
                var same = wall.Year == row.Int("year") && wall.Month == row.Int("month") && wall.Day == row.Int("day")
                    && wall.Hour == row.Int("hour") && wall.Minute == row.Int("minute") && wall.Second == row.Int("second")
                    && wall.Millisecond == row.Int("millisecond") && micro == row.Int("microsecond");
                return same ? null : "obtenido " + IsoDate.FormatLocal(new DateTimeOffset(wall, TimeSpan.Zero));
            });
        }

        [Test, Category("AC-RUT-16-04"), Category("AC-BASE-03")]
        public void MatchesOracle_Invalid()
        {
            GoldenVectors.CheckRows("fmt.json", "isoParseInvalid", row =>
            {
                try
                {
                    var parsed = IsoDate.Parse(row.Text("text"), UtcMinus3);
                    return "se esperaba FormatException y dio " + IsoDate.FormatUtc(parsed);
                }
                catch (FormatException)
                {
                    return null;
                }
            });
        }
    }
}
