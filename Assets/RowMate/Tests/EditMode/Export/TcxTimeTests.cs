using System;
using System.Linq;
using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // TT · Fecha y hora del TCX (AC-STR-09-02, -03; legacy: _formatWithTimezone, tcx_builder.dart:130-143).
    [TestFixture]
    public sealed class TcxTimeTests
    {
        [Test, Category("AC-STR-09-02")]
        public void TruncatesMilliseconds()
        {
            var instant = new DateTimeOffset(2026, 11, 5, 22, 2, 0, 750, TimeSpan.Zero);
            Assert.That(TcxTime.Format(instant, TestZones.UtcMinus3), Is.EqualTo("2026-11-05T19:02:00-03:00"));
            Assert.That(TcxTime.Format(instant.AddMilliseconds(249), TestZones.UtcMinus3), Is.EqualTo("2026-11-05T19:02:00-03:00"),
                "999 ms: trunca, no redondea");
        }

        [Test, Category("AC-STR-09-02")]
        public void PadsYearToFour()
        {
            Assert.That(TcxTime.Format(new DateTimeOffset(987, 1, 2, 3, 4, 5, TimeSpan.Zero), TestZones.Utc), Is.EqualTo("0987-01-02T03:04:05+00:00"));
            Assert.That(TcxTime.Format(new DateTimeOffset(2026, 3, 9, 7, 8, 9, TimeSpan.Zero), TestZones.Utc), Is.EqualTo("2026-03-09T07:08:09+00:00"));
        }

        [Test, Category("AC-STR-09-03")]
        public void T2InUtcKolkataKathmanduStJohns()
        {
            var start = TcxSeeds.T2.Session.StartedAt;
            Assert.That(TcxTime.Format(start, TestZones.Utc), Is.EqualTo("2026-11-05T22:02:00+00:00"), "cero lleva +");
            Assert.That(TcxTime.Format(start, TestZones.Kolkata), Is.EqualTo("2026-11-06T03:32:00+05:30"));
            Assert.That(TcxTime.Format(start, TestZones.Kathmandu), Is.EqualTo("2026-11-06T03:47:00+05:45"));
            Assert.That(TcxTime.Format(start, TestZones.StJohns), Is.EqualTo("2026-11-05T18:32:00-03:30"));
            // El instante no depende del desfase con que llega (la base devuelve la hora local).
            Assert.That(TcxTime.Format(start.ToOffset(TimeSpan.FromHours(9)), TestZones.Kolkata), Is.EqualTo("2026-11-06T03:32:00+05:30"));
        }

        [Test, Category("AC-STR-09-03")]
        public void T2MEachInstantHasItsOwnOffset()
        {
            var seed = TcxSeeds.T2M;
            var start = seed.Session.StartedAt;
            Assert.That(TcxTime.Format(start, TestZones.Madrid), Is.EqualTo("2026-10-25T02:59:50+02:00"));
            Assert.That(TcxTime.Format(start.AddSeconds(5), TestZones.Madrid), Is.EqualTo("2026-10-25T02:59:55+02:00"));
            Assert.That(TcxTime.Format(start.AddSeconds(15), TestZones.Madrid), Is.EqualTo("2026-10-25T02:00:05+01:00"));
            var xml = TcxBuilder.Build(seed.Session, seed.Points, new TcxOptions(TestZones.Madrid, false));
            Assert.That(xml, Does.Contain("      <Id>2026-10-25T02:59:50+02:00</Id>\n"));
            Assert.That(xml.Split('\n').Where(l => l.Contains("<Time>")).Select(l => l.Trim()).ToArray(), Is.EqualTo(new[]
            {
                "<Time>2026-10-25T02:59:55+02:00</Time>", "<Time>2026-10-25T02:00:05+01:00</Time>",
            }));
            // Y el inicio del horario de verano (29/03/2026, 02:00 → 03:00): 00:59:59Z = 01:59:59+01:00, 01:00:00Z = 03:00:00+02:00.
            Assert.That(TcxTime.Format(new DateTimeOffset(2026, 3, 29, 0, 59, 59, TimeSpan.Zero), TestZones.Madrid), Is.EqualTo("2026-03-29T01:59:59+01:00"));
            Assert.That(TcxTime.Format(new DateTimeOffset(2026, 3, 29, 1, 0, 0, TimeSpan.Zero), TestZones.Madrid), Is.EqualTo("2026-03-29T03:00:00+02:00"));
        }
    }
}
