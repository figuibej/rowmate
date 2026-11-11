using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RowMate.Core.Export;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Export
{
    // TB · El archivo TCX (F-STR-09; 12-exportar-tcx/01-po-spec §3, §6; 02-tech-design §2.1; legacy: tcx_builder.dart:7-80):
    // golden de T-1 byte a byte, horas, vueltas, campos de la vuelta y del punto, Notes al final, sin metadatos y rápido.
    [TestFixture]
    public sealed class TcxBuilderTests
    {
        private static string Build(TcxSeed seed, TimeZoneInfo? zone = null, bool simulator = false)
        {
            return TcxBuilder.Build(seed.Session, seed.Points, new TcxOptions(zone ?? TestZones.UtcMinus3, simulator));
        }

        private static string GoldenPath => RepoPaths.Combine("Assets", "RowMate", "Tests", "Golden", "tcx", "T-1.tcx");

        private static string[] Lines(string xml)
        {
            return xml.Split('\n');
        }

        [Test, Category("AC-STR-09-01")]
        public void T1EqualsGoldenByteForByte()
        {
            var expected = File.ReadAllBytes(GoldenPath);
            var actual = TcxDocument.Encode(Build(TcxSeeds.T1));
            Assert.That(Encoding.UTF8.GetString(actual), Is.EqualTo(Encoding.UTF8.GetString(expected)), "el texto del golden");
            Assert.That(actual, Is.EqualTo(expected), "byte a byte");
        }

        [Test, Category("AC-STR-09-01")]
        public void Utf8NoBomLfFinalNewline()
        {
            var bytes = TcxDocument.Encode(Build(TcxSeeds.T1));
            Assert.That(bytes[0], Is.EqualTo((byte)'<'), "sin BOM");
            Assert.That(bytes.Contains((byte)'\r'), Is.False, "sin \\r");
            Assert.That(bytes[bytes.Length - 1], Is.EqualTo((byte)'\n'), "línea final \\n");
            Assert.That(bytes[bytes.Length - 2], Is.EqualTo((byte)'>'));
            var golden = File.ReadAllBytes(GoldenPath);
            Assert.That(golden.Contains((byte)'\r'), Is.False, "el golden se versiona con LF (.gitattributes *.tcx eol=lf)");
            Assert.That(golden[0], Is.EqualTo((byte)'<'), "el golden sin BOM");
            var text = Encoding.UTF8.GetString(bytes);
            Assert.That(text, Does.StartWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"));
            foreach (var line in Lines(text))
            {
                var indent = line.Length - line.TrimStart(' ').Length;
                Assert.That(indent % 2, Is.EqualTo(0), "sangría de 2 espacios por nivel: " + line);
            }
        }

        [Test, Category("AC-STR-09-02")]
        public void IdLapAndPointTimesFromStart()
        {
            var lines = Lines(Build(TcxSeeds.T1));
            Assert.That(lines[4], Is.EqualTo("      <Id>2026-11-05T19:02:00-03:00</Id>"), "inicio sin los 750 ms");
            Assert.That(lines[5], Is.EqualTo("      <Lap StartTime=\"2026-11-05T19:02:05-03:00\">"), "inicio + segundos del primer punto");
            var times = lines.Where(l => l.Contains("<Time>")).Select(l => l.Trim()).ToArray();
            Assert.That(times, Is.EqualTo(new[]
            {
                "<Time>2026-11-05T19:02:05-03:00</Time>", "<Time>2026-11-05T19:02:10-03:00</Time>", "<Time>2026-11-05T19:02:15-03:00</Time>",
                "<Time>2026-11-05T19:02:20-03:00</Time>", "<Time>2026-11-05T19:02:25-03:00</Time>", "<Time>2026-11-05T19:02:30-03:00</Time>",
            }));
        }

        [Test, Category("AC-STR-09-04")]
        public void SameFileWithoutRoutine()
        {
            // Las vueltas no dependen de la rutina (DV-118): la misma sesión con otra rutina, sin rutina o con la rutina borrada
            // (otro id) da el mismo archivo salvo Notes.
            var seed = TcxSeeds.T1;
            var a = TcxBuilder.Build(seed.Session, seed.Points, new TcxOptions(TestZones.UtcMinus3, false));
            var b = TcxBuilder.Build(seed.Session with { RoutineId = 9999 }, seed.Points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(b, Is.EqualTo(a));
            Assert.That(Occurrences(a, "<Lap "), Is.EqualTo(3));
        }

        [Test, Category("AC-STR-09-05")]
        public void LapFieldsInOrder()
        {
            var lines = Lines(Build(TcxSeeds.T1));
            Assert.That(lines.Skip(5).Take(7), Is.EqualTo(new[]
            {
                "      <Lap StartTime=\"2026-11-05T19:02:05-03:00\">",
                "        <TotalTimeSeconds>10</TotalTimeSeconds>",
                "        <DistanceMeters>42</DistanceMeters>",
                "        <Calories>3</Calories>",
                "        <Intensity>Active</Intensity>",
                "        <TriggerMethod>Manual</TriggerMethod>",
                "        <Track>",
            }));
        }

        [Test, Category("AC-STR-09-05")]
        public void SinglePointLapZeroZero()
        {
            var xml = Build(TcxSeeds.T1);
            var lastLap = xml.Substring(xml.LastIndexOf("<Lap ", StringComparison.Ordinal));
            Assert.That(lastLap, Does.Contain("<TotalTimeSeconds>0</TotalTimeSeconds>\n        <DistanceMeters>0</DistanceMeters>"));
        }

        [Test, Category("AC-STR-09-05"), Category("AC-D15-14")]
        public void NegativeDistanceKept()
        {
            // El monitor reinicia la distancia: los Trackpoint llevan la distancia del monitor tal cual (DV-127). La vuelta, que
            // en el legacy daba −15, desde la It. 15 sale en 0 (AC-D15-14, DV-144; cambió con su AC, gate m-01).
            var seed = TcxSeeds.T1;
            var points = new List<DataPoint>(seed.Points) { [1] = seed.Points[1] with { DistanceMeters = 0 } };
            points[2] = seed.Points[2] with { DistanceMeters = 5 };
            var xml = TcxBuilder.Build(seed.Session, points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(xml, Does.Not.Contain("<DistanceMeters>-"), "ninguna distancia negativa");
            Assert.That(xml, Does.Contain("      <Lap StartTime=\"2026-11-05T19:02:05-03:00\">\n        <TotalTimeSeconds>10</TotalTimeSeconds>\n"
                + "        <DistanceMeters>0</DistanceMeters>\n"), "la vuelta del reinicio, en 0");
            var distances = Trackpoints(xml).Select(p => Between(p, "<DistanceMeters>", "</DistanceMeters>")).ToArray();
            Assert.That(distances.Take(3), Is.EqualTo(new[] { "20", "0", "5" }), "los Trackpoint, como los dio el monitor");
        }

        [Test, Category("AC-D15-14")]
        public void NegativeLapDistanceIsZero()
        {
            // Reinicio a mitad de la vuelta 2 (20 → 25 s): último − primero < 0 → 0; la vuelta 1 no cambia.
            var seed = TcxSeeds.T1;
            var points = new List<DataPoint>(seed.Points);
            points[4] = seed.Points[4] with { DistanceMeters = 3 };
            var xml = TcxBuilder.Build(seed.Session, points, new TcxOptions(TestZones.UtcMinus3, false));
            var laps = Lines(xml).Where(l => l.StartsWith("        <DistanceMeters>", StringComparison.Ordinal)).ToArray();
            Assert.That(laps, Is.EqualTo(new[]
            {
                "        <DistanceMeters>42</DistanceMeters>", "        <DistanceMeters>0</DistanceMeters>", "        <DistanceMeters>0</DistanceMeters>",
            }));
            Assert.That(Trackpoints(xml)[4], Does.Contain("<DistanceMeters>3</DistanceMeters>"), "el punto, tal cual");

            // Sin reinicio, nada cambia en las vueltas (T-1 contra el golden en T1EqualsGoldenByteForByte).
            foreach (var other in new[] { TcxSeeds.T2, TcxSeeds.T3, TcxSeeds.T5, TcxSeeds.T6, TcxSeeds.ThreeHours })
            {
                Assert.That(Build(other), Does.Not.Contain("<DistanceMeters>-"), other.Name);
            }
        }

        [Test, Category("AC-STR-09-05")]
        public void GapBetweenLapsNotCounted()
        {
            // Vuelta 1: 5 → 15 s (10 s, 42 m); vuelta 2: 20 → 25 s (5 s, 20 m): los 5 s y 18 m entre 15 y 20 no se cuentan.
            var lines = Lines(Build(TcxSeeds.T1));
            var totals = lines.Where(l => l.StartsWith("        <TotalTimeSeconds>", StringComparison.Ordinal)).ToArray();
            var distances = lines.Where(l => l.StartsWith("        <DistanceMeters>", StringComparison.Ordinal)).ToArray();
            Assert.That(totals, Is.EqualTo(new[]
            {
                "        <TotalTimeSeconds>10</TotalTimeSeconds>", "        <TotalTimeSeconds>5</TotalTimeSeconds>",
                "        <TotalTimeSeconds>0</TotalTimeSeconds>",
            }));
            Assert.That(distances, Is.EqualTo(new[]
            {
                "        <DistanceMeters>42</DistanceMeters>", "        <DistanceMeters>20</DistanceMeters>", "        <DistanceMeters>0</DistanceMeters>",
            }));
        }

        [Test, Category("AC-STR-09-08")]
        public void OptionalFieldsOnlyWhenPositive()
        {
            var xml = Build(TcxSeeds.T1);
            var points = Trackpoints(xml);
            Assert.That(points.Length, Is.EqualTo(6));
            Assert.That(points[0], Does.Not.Contain("HeartRateBpm"), "pulso 0");
            Assert.That(points[1], Does.Not.Contain("Extensions"), "0 W");
            Assert.That(points[2], Does.Not.Contain("Cadence"), "0 spm");
            Assert.That(points[4], Does.Not.Contain("HeartRateBpm").And.Not.Contain("Extensions"));
            Assert.That(points.All(p => p.Contains("<DistanceMeters>")), Is.True, "la distancia siempre");
            var zero = TcxBuilder.Build(TcxSeeds.T6.Session, TcxSeeds.T6.Points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(Trackpoints(zero)[0], Does.Contain("<DistanceMeters>10</DistanceMeters>"));
            var withZeroDistance = TcxBuilder.Build(TcxSeeds.T5.Session, TcxSeeds.T5.Points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(Trackpoints(withZeroDistance)[0], Does.Contain("<DistanceMeters>0</DistanceMeters>"), "también 0");
            Assert.That(points[0], Does.Contain(
                "            <Extensions>\n              <ns3:TPX>\n                <ns3:Watts>150</ns3:Watts>\n              </ns3:TPX>\n            </Extensions>\n"),
                "las 5 líneas de la potencia");
            Assert.That(points[1], Does.Contain("            <HeartRateBpm><Value>140</Value></HeartRateBpm>\n"));
        }

        [TestCase(24.4, "24")]
        [TestCase(24.6, "25")]
        [TestCase(22.5, "23")]
        [TestCase(23.5, "24")]
        [TestCase(0.3, "0")]
        [TestCase(0.5, "1")]
        [Category("AC-STR-09-08")]
        public void CadenceHalfAwayFromZero(double spm, string cadence)
        {
            var seed = TcxSeeds.T2;
            var points = new[] { seed.Points[0] with { StrokeRate = spm } };
            var xml = TcxBuilder.Build(seed.Session, points, new TcxOptions(TestZones.Utc, false));
            Assert.That(xml, Does.Contain("            <Cadence>" + cadence + "</Cadence>\n"), "SPM crudo > 0 (m-04)");
        }

        [Test, Category("AC-STR-09-08")]
        public void NoPaceSpeedStrokesPosition()
        {
            var xml = Build(TcxSeeds.ThreeHours);
            foreach (var forbidden in new[] { "Speed", "Pace", "Stroke", "Position", "Latitude", "AltitudeMeters", "RunCadence" })
            {
                Assert.That(xml, Does.Not.Contain(forbidden), forbidden);
            }
        }

        [Test, Category("AC-STR-09-08")]
        public void PointsInMonitorOrder()
        {
            var xml = Build(TcxSeeds.T1);
            var distances = Trackpoints(xml).Select(p => Between(p, "<DistanceMeters>", "</DistanceMeters>")).ToArray();
            Assert.That(distances, Is.EqualTo(new[] { "20", "41", "62", "80", "100", "121" }), "en el orden recibido (AC-HIS-19-02)");
        }

        [Test, Category("AC-STR-09-09")]
        public void NotesAfterLastLap()
        {
            var lines = Lines(Build(TcxSeeds.T1));
            var notes = Array.IndexOf(lines, "      <Notes>Escalera &amp; &lt;Co&gt; &quot;A&quot; &apos;B&apos;</Notes>");
            Assert.That(notes, Is.GreaterThan(0));
            Assert.That(lines[notes - 1], Is.EqualTo("      </Lap>"), "después de la última vuelta (DV-117)");
            Assert.That(lines[notes + 1], Is.EqualTo("      <Creator xsi:type=\"Device_t\">"), "y antes de Creator (It. 15, AC-D15-15)");
            Assert.That(lines.Count(l => l.Contains("<Notes>")), Is.EqualTo(1));
        }

        [Test, Category("AC-STR-09-09")]
        public void NoNotesForFreeOrBlank()
        {
            Assert.That(Build(TcxSeeds.T2), Does.Not.Contain("<Notes>"), "una libre");
            var blank = TcxSeeds.T3;
            var xml = TcxBuilder.Build(blank.Session with { RoutineName = "   " }, blank.Points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(xml, Does.Not.Contain("<Notes>"), "un nombre solo de espacios");
            var empty = TcxBuilder.Build(blank.Session with { RoutineName = "" }, blank.Points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(empty, Does.Not.Contain("<Notes>"), "un nombre vacío");
        }

        [Test, Category("AC-STR-09-09"), Category("AC-STR-09-15")]
        public void SimNotes()
        {
            Assert.That(Build(TcxSeeds.T3, simulator: true), Does.Contain("      <Notes>[SIM] Corta</Notes>\n"));
            Assert.That(Build(TcxSeeds.T2, simulator: true), Does.Contain("      <Notes>[SIM]</Notes>\n"), "una libre del simulador");
            Assert.That(Build(TcxSeeds.T1, simulator: true),
                Does.Contain("<Notes>[SIM] Escalera &amp; &lt;Co&gt; &quot;A&quot; &apos;B&apos;</Notes>"));
            Assert.That(Build(TcxSeeds.T3), Does.Not.Contain("[SIM]"), "el build normal nunca la lleva");
        }

        [Test, Category("AC-STR-09-09")]
        public void NoRowmateComment()
        {
            var xml = Build(TcxSeeds.T1);
            Assert.That(xml, Does.Not.Contain("<!--"));
            Assert.That(xml, Does.Not.Contain("ROWMATE"));
            Assert.That(xml.IndexOf("<Notes>", StringComparison.Ordinal), Is.GreaterThan(xml.LastIndexOf("</Lap>", StringComparison.Ordinal)));
        }

        [Test, Category("AC-STR-09-09")]
        public void NotesStripInvalidXmlChars()
        {
            var seed = TcxSeeds.T3;
            var xml = TcxBuilder.Build(seed.Session with { RoutineName = "A\u0001B\tC\uFFFE" }, seed.Points, new TcxOptions(TestZones.UtcMinus3, false));
            Assert.That(xml, Does.Contain("      <Notes>AB\tC</Notes>\n"));
        }

        [Test, Category("AC-STR-09-10")]
        public void T6OneLapZeros()
        {
            var lines = Lines(Build(TcxSeeds.T6));
            Assert.That(lines.Count(l => l.Contains("<Lap ")), Is.EqualTo(1));
            Assert.That(lines, Does.Contain("        <TotalTimeSeconds>0</TotalTimeSeconds>"));
            Assert.That(lines, Does.Contain("        <DistanceMeters>0</DistanceMeters>"));
            Assert.That(lines, Does.Contain("        <Calories>0</Calories>"), "tiempo total 0 → 0 kcal (DV-119)");
        }

        [Test, Category("AC-STR-09-10")]
        public void NoPointsThrows()
        {
            Assert.Throws<ArgumentException>(() =>
                TcxBuilder.Build(TcxSeeds.T1.Session, new DataPoint[0], new TcxOptions(TestZones.UtcMinus3, false)));
        }

        [Test, Category("AC-STR-09-07")]
        public void CutSessionCaloriesFromPoints()
        {
            Assert.That(Build(TcxSeeds.T3), Does.Contain("        <Calories>3</Calories>\n"), "T-3: 3 × 3 / (15 / 5) = 3");
        }

        [Test, Category("AC-STR-09-11")]
        public void Deterministic()
        {
            var seed = TcxSeeds.ThreeHours;
            var a = TcxBuilder.Build(seed.Session, seed.Points, new TcxOptions(TestZones.Madrid, false));
            var b = TcxBuilder.Build(seed.Session, seed.Points, new TcxOptions(TestZones.Madrid, false));
            Assert.That(b, Is.EqualTo(a));
            Assert.That(Build(TcxSeeds.T1), Is.EqualTo(Build(TcxSeeds.T1)));
        }

        [Test, Category("AC-D15-15")]
        public void CreatorClosesActivity()
        {
            // Después de Notes (T-1) o de la última vuelta (T-2, libre): exactamente estas líneas y después </Activity>.
            var expected = new[]
            {
                "      <Creator xsi:type=\"Device_t\">", "        <Name>RowMate</Name>", "        <UnitId>0</UnitId>", "        <ProductID>0</ProductID>",
                "        <Version>", "          <VersionMajor>1</VersionMajor>", "          <VersionMinor>0</VersionMinor>",
                "          <BuildMajor>0</BuildMajor>", "          <BuildMinor>0</BuildMinor>", "        </Version>", "      </Creator>", "    </Activity>",
            };
            foreach (var seed in new[] { TcxSeeds.T1, TcxSeeds.T2 })
            {
                var lines = Lines(Build(seed));
                var creator = Array.IndexOf(lines, expected[0]);
                Assert.That(creator, Is.GreaterThan(0), seed.Name);
                Assert.That(lines.Skip(creator).Take(expected.Length), Is.EqualTo(expected), seed.Name);
                Assert.That(lines.Count(l => l.Contains("<Creator")), Is.EqualTo(1), seed.Name);
            }

            var t1 = Lines(Build(TcxSeeds.T1));
            Assert.That(t1[Array.IndexOf(t1, expected[0]) - 1], Does.StartWith("      <Notes>"), "T-1: después de Notes");
            var t2 = Lines(Build(TcxSeeds.T2));
            Assert.That(t2[Array.IndexOf(t2, expected[0]) - 1], Is.EqualTo("      </Lap>"), "T-2 es libre (sin Notes): después de la vuelta");
            var two = TcxBuilder.Build(TcxSeeds.T2.Session, TcxSeeds.T2.Points, new TcxOptions(TestZones.UtcMinus3, false, "2.3"));
            Assert.That(two, Does.Contain("          <VersionMajor>2</VersionMajor>\n          <VersionMinor>3</VersionMinor>\n"
                + "          <BuildMajor>0</BuildMajor>\n          <BuildMinor>0</BuildMinor>\n"));
        }

        [TestCase("1.0.0", 1, 0, 0, 0)]
        [TestCase("2.3", 2, 3, 0, 0)]
        [TestCase("1.2.3.4.5", 1, 2, 3, 4)]
        [TestCase("1.4.0-beta", 1, 4, 0, 0)]
        [TestCase("", 0, 0, 0, 0)]
        [TestCase(null, 0, 0, 0, 0)]
        [TestCase("x.7", 0, 7, 0, 0)]
        [TestCase("99999.1", 65535, 1, 0, 0)]
        [Category("AC-D15-15")]
        public void VersionPartsFromAppVersion(string? version, int major, int minor, int buildMajor, int buildMinor)
        {
            Assert.That(TcxBuilder.VersionParts(version), Is.EqualTo(new[] { major, minor, buildMajor, buildMinor }));
            Assert.That(new TcxOptions(TestZones.Utc, false).AppVersion, Is.EqualTo("1.0.0"), "por defecto");
        }

        [Test, Category("AC-STR-09-12"), Category("AC-D15-17")]
        public void ThreeHoursUnder200Ms()
        {
            // m-09: una corrida de calentamiento y la mediana de 5. It. 15 (#71, AC-D15-17): el primer Build es el calentamiento (y
            // mide el tamaño); antes del bucle se junta la basura de las otras pruebas, así una recolección ajena no cae dentro de
            // la mediana. El umbral no cambia.
            var seed = TcxSeeds.ThreeHours;
            Assert.That(seed.Points.Count, Is.EqualTo(2160));
            var options = new TcxOptions(TestZones.UtcMinus3, false);
            var size = TcxDocument.Encode(TcxBuilder.Build(seed.Session, seed.Points, options)).Length;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var times = new double[5];
            for (var i = 0; i < times.Length; i++)
            {
                var watch = Stopwatch.StartNew();
                TcxDocument.Encode(TcxBuilder.Build(seed.Session, seed.Points, options));
                watch.Stop();
                times[i] = watch.Elapsed.TotalMilliseconds;
            }

            Array.Sort(times);
            Assert.That(times[2], Is.LessThanOrEqualTo(200), "mediana de 5: " + string.Join(", ", times));
            Assert.That(size, Is.GreaterThan(2160 * 200), "todos los campos");
            Assert.That(Occurrences(TcxBuilder.Build(seed.Session, seed.Points, options), "<Lap "), Is.EqualTo(19), "un paso cada 10 min (0 a 18)");
        }

        private static string[] Trackpoints(string xml)
        {
            var result = new List<string>();
            var from = 0;
            while (true)
            {
                var start = xml.IndexOf("          <Trackpoint>", from, StringComparison.Ordinal);
                if (start < 0)
                {
                    return result.ToArray();
                }

                var end = xml.IndexOf("</Trackpoint>", start, StringComparison.Ordinal);
                result.Add(xml.Substring(start, end - start));
                from = end;
            }
        }

        private static string Between(string text, string open, string close)
        {
            var start = text.IndexOf(open, StringComparison.Ordinal) + open.Length;
            return text.Substring(start, text.IndexOf(close, start, StringComparison.Ordinal) - start);
        }

        private static int Occurrences(string text, string needle)
        {
            var count = 0;
            for (var i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + 1, StringComparison.Ordinal))
            {
                count++;
            }

            return count;
        }
    }
}
