using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // F-RUT-18 (+ F-HIS-04): estadísticas de sesión (legacy: workout_session.dart:71-136; test/session_stats_test.dart).
    // Casos 12 a 15 de RUT §5.3.
    [TestFixture]
    public sealed class SessionStatsTests
    {
        // Fixture del test legacy: DataPoint(sessionId: 1, elapsedSeconds: 0, heartRate: hr) (test/session_stats_test.dart:4).
        private static DataPoint P(int hr)
        {
            return new DataPoint(1, 0) { HeartRate = hr };
        }

        private static List<DataPoint> Watts(params int[] values)
        {
            var result = new List<DataPoint>();
            foreach (var v in values)
            {
                result.Add(new DataPoint(1, 0) { PowerWatts = v });
            }

            return result;
        }

        private static List<DataPoint> Splits(params int[] values)
        {
            var result = new List<DataPoint>();
            foreach (var v in values)
            {
                result.Add(new DataPoint(1, 0) { Pace500mSeconds = v });
            }

            return result;
        }

        private static List<DataPoint> HundredPoints()
        {
            // Vatios 2, 4, …, 200; split 100…199; SPM 20 + 0,1·i (i = 0…99) (AC-RUT-18-04).
            var result = new List<DataPoint>();
            for (var i = 0; i < 100; i++)
            {
                result.Add(new DataPoint(1, i) { PowerWatts = 2 * (i + 1), Pace500mSeconds = 100 + i, StrokeRate = 20 + 0.1 * i });
            }

            return result;
        }

        // LT-17 (= AC-RUT-18-06).
        // legacy: test/session_stats_test.dart:7 'avgHeartRate y maxHeartRate ignoran los ceros'
        [TestCase(TestName = "avgHeartRate y maxHeartRate ignoran los ceros")]
        [Category("LT-17"), Category("AC-RUT-18-06")]
        public void LegacyHeartRateIgnoresZeros()
        {
            var s = SessionStats.Compute(new[] { P(0), P(120), P(150), P(0), P(141) });
            Assert.That(s.AvgHeartRate, Is.EqualTo(137)); // (120 + 150 + 141) / 3
            Assert.That(s.MaxHeartRate, Is.EqualTo(150));
            Assert.That(s.HasHeartRate, Is.True);
        }

        // LT-18 (= AC-RUT-18-07).
        // legacy: test/session_stats_test.dart:14 'sin pulso quedan en 0'
        [TestCase(TestName = "sin pulso quedan en 0")]
        [Category("LT-18"), Category("AC-RUT-18-07")]
        public void LegacyNoHeartRate()
        {
            var s = SessionStats.Compute(new[] { P(0), P(0) });
            Assert.That(s.AvgHeartRate, Is.EqualTo(0));
            Assert.That(s.MaxHeartRate, Is.EqualTo(0));
            Assert.That(s.HasHeartRate, Is.False);
        }

        // LT-19 (parte de AC-RUT-18-01).
        // legacy: test/session_stats_test.dart:21 'sin puntos quedan en 0'
        [TestCase(TestName = "sin puntos quedan en 0")]
        [Category("LT-19"), Category("AC-RUT-18-01")]
        public void LegacyNoPoints()
        {
            Assert.That(SessionStats.Compute(new DataPoint[0]).HasHeartRate, Is.False);
        }

        // AC-RUT-18-01: sin puntos todo vale 0, sin pulso, split "--:--" y duración "00:00".
        [Test, Category("AC-RUT-18-01")]
        public void NoPoints()
        {
            var s = SessionStats.Compute(new List<DataPoint>());
            Assert.That(s, Is.EqualTo(SessionStats.Empty));
            Assert.That(s.P99Watts, Is.EqualTo(0));
            Assert.That(s.P99Spm, Is.EqualTo(0.0));
            Assert.That(s.P99SplitSeconds, Is.EqualTo(0));
            Assert.That(s.TotalDistance + s.TotalTimeSeconds + s.TotalCalories + s.AvgHeartRate + s.MaxHeartRate, Is.EqualTo(0));
            Assert.That(s.HasHeartRate, Is.False);
            Assert.That(s.SplitFormatted, Is.EqualTo("--:--"));
            Assert.That(s.DurationFormatted, Is.EqualTo("00:00"));
            Assert.Throws<ArgumentNullException>(() => SessionStats.Compute(null!));
        }

        // AC-RUT-18-02 (caso 12): índice ⌊(n − 1) · 0,99⌋ en double, 0-based.
        [TestCase(1, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        [TestCase(10, 8)]
        [TestCase(100, 98)]
        [TestCase(101, 99)]
        [TestCase(102, 99)]
        [TestCase(361, 356)]
        [TestCase(1000, 989)]
        [Category("AC-RUT-18-02")]
        public void P99Index(int n, int index)
        {
            Assert.That(SessionStats.Percentile99Index(n), Is.EqualTo(index));
        }

        [Test, Category("AC-RUT-18-02")]
        public void P99OfOneTo361Is357()
        {
            var values = new int[361];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = i + 1;
            }

            Assert.That(SessionStats.Compute(Watts(values)).P99Watts, Is.EqualTo(357));
            Assert.That(SessionStats.Percentile99(new double[0]), Is.EqualTo(0.0), "sin valores, 0");
            Assert.Throws<ArgumentOutOfRangeException>(() => SessionStats.Percentile99Index(0));
        }

        // AC-RUT-18-02 + AC-BASE-03: índice del legacy para n de 1 a 1100.
        [Test, Category("AC-RUT-18-02"), Category("AC-BASE-03")]
        public void P99IndexMatchesOracle()
        {
            var count = GoldenVectors.CheckRows("session.json", "p99Index", row =>
            {
                var actual = SessionStats.Percentile99Index((int)row.Int("n"));
                return actual == row.Int("index") ? null : "esperado " + row.Int("index") + ", obtenido " + actual;
            });
            Assert.That(count, Is.EqualTo(1100));
        }

        // AC-RUT-18-03 (caso 12) y AC-RUT-18-04: ejemplos de la spec, con la línea de la demo (02-tech-design §8.7).
        [Test, Category("AC-RUT-18-03"), Category("AC-RUT-18-04"), Category("Demo")]
        public void P99Examples()
        {
            var cases = new List<KeyValuePair<string, List<DataPoint>>>
            {
                new KeyValuePair<string, List<DataPoint>>("vatios [0, 100, 200, 300]", Watts(0, 100, 200, 300)),
                new KeyValuePair<string, List<DataPoint>>("split [130, 0, 125, 140]", Splits(130, 0, 125, 140)),
                new KeyValuePair<string, List<DataPoint>>("vatios [200, 150]", Watts(200, 150)),
                new KeyValuePair<string, List<DataPoint>>("100 puntos (2..200 W, split 100..199, SPM 20 + 0,1·i)", HundredPoints()),
            };
            var results = new List<SessionStats>();
            foreach (var c in cases)
            {
                var s = SessionStats.Compute(c.Value);
                results.Add(s);
                TestContext.Out.WriteLine("[DEMO] P99 " + c.Key + " -> W=" + Fmt.Integer(s.P99Watts) + " spm=" + Fmt.DartToString(s.P99Spm)
                    + " split=" + Fmt.Integer(s.P99SplitSeconds) + " (" + s.SplitFormatted + ")");
            }

            Assert.That(results[0].P99Watts, Is.EqualTo(200));
            Assert.That(results[1].P99SplitSeconds, Is.EqualTo(130), "casi el más lento");
            Assert.That(results[1].SplitFormatted, Is.EqualTo("02:10"));
            Assert.That(results[2].P99Watts, Is.EqualTo(150), "con n = 2, el menor");
            Assert.That(results[3].P99Watts, Is.EqualTo(198));
            Assert.That(results[3].P99SplitSeconds, Is.EqualTo(198));
            Assert.That(results[3].SplitFormatted, Is.EqualTo("03:18"));
            Assert.That(Fmt.DartToString(results[3].P99Spm), Is.EqualTo("29.8"));
        }

        // AC-RUT-18-04: vatios y split enteros; SPM decimal sin redondear.
        [Test, Category("AC-RUT-18-04")]
        public void P99Decimals()
        {
            var spm = new List<DataPoint>();
            foreach (var x in new[] { 24.5, 23.0, 25.5 })
            {
                spm.Add(new DataPoint(1, 0) { StrokeRate = x });
            }

            Assert.That(SessionStats.Compute(spm).P99Spm, Is.EqualTo(24.5));
            var hundred = SessionStats.Compute(HundredPoints());
            Assert.That(hundred.P99Watts, Is.EqualTo(198));
            Assert.That(hundred.P99SplitSeconds, Is.EqualTo(198));
            Assert.That(hundred.P99Spm, Is.EqualTo(29.8).Within(1e-9));
            Assert.That(typeof(SessionStats).GetProperty("P99Watts")!.PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(typeof(SessionStats).GetProperty("P99SplitSeconds")!.PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(typeof(SessionStats).GetProperty("P99Spm")!.PropertyType, Is.EqualTo(typeof(double)));
        }

        // AC-RUT-18-05 (caso 13): distancia, tiempo y calorías = el máximo de cada columna (ceros incluidos, cualquier orden).
        [Test, Category("AC-RUT-18-05")]
        public void TotalsAreMaxima()
        {
            var points = new[]
            {
                new DataPoint(1, 5) { DistanceMeters = 20, Calories = 1 },
                new DataPoint(1, 10) { DistanceMeters = 45, Calories = 2 },
                new DataPoint(1, 3) { DistanceMeters = 0, Calories = 0 },
                new DataPoint(1, 615) { DistanceMeters = 2000, Calories = 120 },
            };
            var s = SessionStats.Compute(points);
            Assert.That(s.TotalDistance, Is.EqualTo(2000));
            Assert.That(s.TotalTimeSeconds, Is.EqualTo(615));
            Assert.That(s.DurationFormatted, Is.EqualTo("10:15"));
            Assert.That(s.TotalCalories, Is.EqualTo(120));
            var reversed = new List<DataPoint>(points);
            reversed.Reverse();
            Assert.That(SessionStats.Compute(reversed), Is.EqualTo(s), "el orden no importa");
            var negative = SessionStats.Compute(new[] { new DataPoint(1, -5) { DistanceMeters = -3, Calories = -1 } });
            Assert.That(new[] { negative.TotalTimeSeconds, negative.TotalDistance, negative.TotalCalories }, Is.EqualTo(new[] { -5, -3, -1 }),
                "el máximo de una columna con un solo valor negativo es ese valor (reduce del legacy)");
        }

        // AC-RUT-18-08 (caso 14): promedio redondeado al entero más cercano, la mitad sube.
        [Test, Category("AC-RUT-18-08")]
        public void HeartRateRounding()
        {
            Assert.That(SessionStats.Compute(new[] { P(120), P(121) }).AvgHeartRate, Is.EqualTo(121), "120,5 → 121");
            Assert.That(SessionStats.Compute(new[] { P(120), P(121), P(121) }).AvgHeartRate, Is.EqualTo(121));
            Assert.That(SessionStats.Compute(new[] { P(100), P(101), P(101), P(101) }).AvgHeartRate, Is.EqualTo(101));
            Assert.That(SessionStats.Compute(new[] { P(100), P(100), P(101) }).AvgHeartRate, Is.EqualTo(100), "100,33 → 100");
            Assert.That(SessionStats.Compute(new[] { P(0), P(-5), P(90) }).AvgHeartRate, Is.EqualTo(90), "solo los > 0");
        }

        // AC-RUT-18-09 (caso 15): split "--:--" si ≤ 0 y si no MM:SS; la duración como AC-BLE-01-03.
        [Test, Category("AC-RUT-18-09")]
        public void Formats()
        {
            Assert.That(new SessionStats { P99SplitSeconds = 0 }.SplitFormatted, Is.EqualTo("--:--"));
            Assert.That(new SessionStats { P99SplitSeconds = -5 }.SplitFormatted, Is.EqualTo("--:--"));
            Assert.That(new SessionStats { P99SplitSeconds = 130 }.SplitFormatted, Is.EqualTo("02:10"));
            Assert.That(new SessionStats { P99SplitSeconds = 6000 }.SplitFormatted, Is.EqualTo("100:00"));
            Assert.That(SessionStats.Compute(new[] { new DataPoint(1, 3661) }).DurationFormatted, Is.EqualTo("1:01:01"));
            Assert.That(new SessionStats { TotalTimeSeconds = 59 }.DurationFormatted, Is.EqualTo("00:59"));
        }

        // AC-BLE-01-06: SessionStats y WorkoutSession usan las reglas compartidas de TimeFormats (una sola implementación).
        [Test, Category("AC-BLE-01-06")]
        public void FormatsUseSharedTimeRules()
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            for (var s = -4000; s <= 90000; s += 7)
            {
                Assert.That(new SessionStats { P99SplitSeconds = s }.SplitFormatted, Is.EqualTo(TimeFormats.SplitOrDashes(s)));
                Assert.That(new SessionStats { TotalTimeSeconds = s }.DurationFormatted, Is.EqualTo(TimeFormats.Elapsed(s)));
                Assert.That(new WorkoutSession(start) { TotalTimeSeconds = s }.DurationFormatted, Is.EqualTo(TimeFormats.Elapsed(s)));
            }
        }

        // Todo F-RUT-18 contra el oráculo: casos con nombre, 50 listas aleatorias y los formatos.
        [Test, Category("AC-RUT-18-01"), Category("AC-RUT-18-03"), Category("AC-RUT-18-04"), Category("AC-RUT-18-05"), Category("AC-RUT-18-06"),
         Category("AC-RUT-18-07"), Category("AC-RUT-18-08"), Category("AC-RUT-18-09"), Category("AC-BASE-03")]
        public void MatchesOracle()
        {
            var golden = GoldenVectors.Load("session.json");
            var problems = new List<string>();
            foreach (var item in golden.Section("stats").Items)
            {
                var points = new List<DataPoint>();
                foreach (var p in item["points"].Items)
                {
                    points.Add(PointFrom(p.Items));
                }

                Compare(problems, item["name"].AsString(), SessionStats.Compute(points), item);
            }

            var randomPoints = new Dictionary<long, List<DataPoint>>();
            foreach (var row in GoldenVectors.Rows("session.json", "randomPoints"))
            {
                var id = row.Int("case");
                if (!randomPoints.TryGetValue(id, out var list))
                {
                    list = new List<DataPoint>();
                    randomPoints[id] = list;
                }

                list.Add(new DataPoint(1, (int)row.Int("elapsedSeconds"))
                {
                    StrokeRate = row.F64("strokeRate"), StrokeCount = (int)row.Int("strokeCount"), DistanceMeters = (int)row.Int("distanceMeters"),
                    Pace500mSeconds = (int)row.Int("pace500mSeconds"), PowerWatts = (int)row.Int("powerWatts"), Calories = (int)row.Int("calories"),
                    HeartRate = (int)row.Int("heartRate"),
                });
            }

            var randomCases = 0;
            foreach (var row in GoldenVectors.Rows("session.json", "randomStats"))
            {
                randomCases++;
                var id = row.Int("case");
                var points = randomPoints[id];
                Assert.That(points.Count, Is.EqualTo(row.Int("count")), "random" + id);
                var s = SessionStats.Compute(points);
                var name = "random" + id;
                Check(problems, name, "p99Watts", row.Int("p99Watts"), s.P99Watts);
                Check(problems, name, "p99Spm", Bits(row.F64("p99Spm")), Bits(s.P99Spm));
                Check(problems, name, "p99SplitSeconds", row.Int("p99SplitSeconds"), s.P99SplitSeconds);
                Check(problems, name, "totalDistance", row.Int("totalDistance"), s.TotalDistance);
                Check(problems, name, "totalTimeSeconds", row.Int("totalTimeSeconds"), s.TotalTimeSeconds);
                Check(problems, name, "totalCalories", row.Int("totalCalories"), s.TotalCalories);
                Check(problems, name, "avgHeartRate", row.Int("avgHeartRate"), s.AvgHeartRate);
                Check(problems, name, "maxHeartRate", row.Int("maxHeartRate"), s.MaxHeartRate);
                Check(problems, name, "hasHeartRate", row.Cell("hasHeartRate").AsBool(), s.HasHeartRate);
                Check(problems, name, "splitFormatted", row.Text("splitFormatted"), s.SplitFormatted);
                Check(problems, name, "durationFormatted", row.Text("durationFormatted"), s.DurationFormatted);
            }

            Assert.That(randomCases, Is.EqualTo(50));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // AC-RUT-18-09 + AC-RUT-16-03 + AC-BASE-03: splitFormatted y durationFormatted del legacy.
        [Test, Category("AC-RUT-18-09"), Category("AC-RUT-16-03"), Category("AC-BLE-01-06"), Category("AC-BASE-03")]
        public void FormatsMatchOracle()
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var problems = new List<string>();
            foreach (var item in GoldenVectors.Load("session.json").Section("formats").Items)
            {
                var seconds = (int)item["seconds"].AsLong();
                Check(problems, "formats", "split(" + seconds + ")", item["splitFormatted"].AsString(), new SessionStats { P99SplitSeconds = seconds }.SplitFormatted);
                Check(problems, "formats", "statsDuration(" + seconds + ")", item["statsDurationFormatted"].AsString(), new SessionStats { TotalTimeSeconds = seconds }.DurationFormatted);
                Check(problems, "formats", "sessionDuration(" + seconds + ")", item["sessionDurationFormatted"].AsString(),
                    new WorkoutSession(start) { TotalTimeSeconds = seconds }.DurationFormatted);
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            GoldenVectors.CheckRows("session.json", "formatSweep", row =>
            {
                var s = (int)row.Int("seconds");
                var split = new SessionStats { P99SplitSeconds = s }.SplitFormatted;
                var duration = new SessionStats { TotalTimeSeconds = s }.DurationFormatted;
                var session = new WorkoutSession(start) { TotalTimeSeconds = s }.DurationFormatted;
                return split == row.Text("splitFormatted") && duration == row.Text("statsDurationFormatted") && session == row.Text("sessionDurationFormatted")
                    ? null
                    : "obtenido split \"" + split + "\", duración \"" + duration + "\", sesión \"" + session + "\"";
            });
        }

        private static DataPoint PointFrom(IReadOnlyList<JsonNode> v)
        {
            // [elapsed, strokeRate (bits), strokeCount, distance, pace, watts, calories, heartRate]
            return new DataPoint(1, (int)v[0].AsLong())
            {
                StrokeRate = v[1].AsF64(), StrokeCount = (int)v[2].AsLong(), DistanceMeters = (int)v[3].AsLong(), Pace500mSeconds = (int)v[4].AsLong(),
                PowerWatts = (int)v[5].AsLong(), Calories = (int)v[6].AsLong(), HeartRate = (int)v[7].AsLong(),
            };
        }

        private static void Compare(List<string> problems, string name, SessionStats s, JsonNode expected)
        {
            Check(problems, name, "p99Watts", expected["p99Watts"].AsLong(), s.P99Watts);
            Check(problems, name, "p99Spm", Bits(expected["p99Spm"].AsF64()), Bits(s.P99Spm));
            Check(problems, name, "p99SplitSeconds", expected["p99SplitSeconds"].AsLong(), s.P99SplitSeconds);
            Check(problems, name, "totalDistance", expected["totalDistance"].AsLong(), s.TotalDistance);
            Check(problems, name, "totalTimeSeconds", expected["totalTimeSeconds"].AsLong(), s.TotalTimeSeconds);
            Check(problems, name, "totalCalories", expected["totalCalories"].AsLong(), s.TotalCalories);
            Check(problems, name, "avgHeartRate", expected["avgHeartRate"].AsLong(), s.AvgHeartRate);
            Check(problems, name, "maxHeartRate", expected["maxHeartRate"].AsLong(), s.MaxHeartRate);
            Check(problems, name, "hasHeartRate", expected["hasHeartRate"].AsBool(), s.HasHeartRate);
            Check(problems, name, "splitFormatted", expected["splitFormatted"].AsString(), s.SplitFormatted);
            Check(problems, name, "durationFormatted", expected["durationFormatted"].AsString(), s.DurationFormatted);
        }

        private static void Check(List<string> problems, string name, string field, object expected, object actual)
        {
            if (expected is long e && actual is int a)
            {
                if (e != a)
                {
                    problems.Add(name + "." + field + ": legacy " + e + ", obtenido " + a);
                }

                return;
            }

            if (!Equals(expected, actual))
            {
                problems.Add(name + "." + field + ": legacy " + expected + ", obtenido " + actual);
            }
        }

        private static string Bits(double x)
        {
            return GoldenJsonExtensions.ToF64Bits(x) + " (" + Fmt.DartToString(x) + ")";
        }
    }
}
