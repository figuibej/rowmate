using System;
using NUnit.Framework;
using RowMate.Core.Common.Json;
using RowMate.Core.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Models
{
    // RowingData = lib/core/models/rowing_data.dart:2-62 (F-BLE-01, + F-RUT-15).
    [TestFixture]
    public sealed class RowingDataTests
    {
        // AC-BLE-01-01: 8 campos, todos enteros salvo el ritmo; la lectura vacía tiene los 8 en 0, "--:--" y "00:00".
        [Test, Category("AC-BLE-01-01")]
        public void EmptyReadingIsZeroWithDashesAndZeroTime()
        {
            foreach (var empty in new[] { RowingData.Empty, new RowingData() })
            {
                Assert.That(empty.StrokeRate, Is.EqualTo(0.0));
                Assert.That(empty.StrokeCount, Is.EqualTo(0));
                Assert.That(empty.DistanceMeters, Is.EqualTo(0));
                Assert.That(empty.Pace500mSeconds, Is.EqualTo(0));
                Assert.That(empty.PowerWatts, Is.EqualTo(0));
                Assert.That(empty.TotalCalories, Is.EqualTo(0));
                Assert.That(empty.HeartRate, Is.EqualTo(0), "pulso 0 = sin dato");
                Assert.That(empty.ElapsedSeconds, Is.EqualTo(0));
                Assert.That(empty.Pace500mFormatted, Is.EqualTo("--:--"));
                Assert.That(empty.ElapsedFormatted, Is.EqualTo("00:00"));
            }

            Assert.That(new RowingData(), Is.EqualTo(RowingData.Empty), "igualdad por valor");
        }

        [Test, Category("AC-BLE-01-01")]
        public void EightFieldsAndOnlyStrokeRateIsDecimal()
        {
            // Los 8 campos de la lectura, con sus tipos (rowing_data.dart:3-10).
            var expected = new[]
            {
                "StrokeRate:Double", "StrokeCount:Int32", "DistanceMeters:Int32", "Pace500mSeconds:Int32", "PowerWatts:Int32",
                "TotalCalories:Int32", "HeartRate:Int32", "ElapsedSeconds:Int32",
            };
            var actual = new System.Collections.Generic.List<string>();
            foreach (var property in typeof(RowingData).GetProperties())
            {
                if (property.CanWrite)
                {
                    actual.Add(property.Name + ":" + property.PropertyType.Name);
                }
            }

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [Test, Category("AC-BLE-01-01"), Category("AC-BASE-03")]
        public void EmptyMatchesOracle()
        {
            var empty = GoldenVectors.Load("rowing_data.json").Section("empty").Items[0];
            AssertFields(RowingData.Empty, empty, "RowingData()");
        }

        // AC-BLE-01-02 (caso 15): "--:--" si es ≤ 0, si no MM:SS con cero a la izquierda; desde 6000 s, 3 cifras de minutos.
        [TestCase(-5, "--:--", Category = "AC-BLE-01-02")]
        [TestCase(0, "--:--", Category = "AC-BLE-01-02")]
        [TestCase(1, "00:01", Category = "AC-BLE-01-02")]
        [TestCase(59, "00:59", Category = "AC-BLE-01-02")]
        [TestCase(60, "01:00", Category = "AC-BLE-01-02")]
        [TestCase(120, "02:00", Category = "AC-BLE-01-02")]
        [TestCase(125, "02:05", Category = "AC-BLE-01-02")]
        [TestCase(133, "02:13", Category = "AC-BLE-01-02")]
        [TestCase(599, "09:59", Category = "AC-BLE-01-02")]
        [TestCase(600, "10:00", Category = "AC-BLE-01-02")]
        [TestCase(5999, "99:59", Category = "AC-BLE-01-02")]
        [TestCase(6000, "100:00", Category = "AC-BLE-01-02")]
        [TestCase(6001, "100:01", Category = "AC-BLE-01-02")]
        public void SplitText(int seconds, string expected)
        {
            Assert.That(new RowingData { Pace500mSeconds = seconds }.Pace500mFormatted, Is.EqualTo(expected));
        }

        [Test, Category("AC-BLE-01-02"), Category("AC-BASE-03")]
        public void MatchesOracle_Split()
        {
            // Barrido [-10; 7000] y bordes de pace500mFormatted del legacy.
            var rows = GoldenVectors.CheckRows("rowing_data.json", "pace500mFormatted", row =>
            {
                var expected = row.Text("out");
                var actual = new RowingData { Pace500mSeconds = (int)row.Int("s") }.Pace500mFormatted;
                return actual == expected ? null : "esperado \"" + expected + "\", obtenido \"" + actual + "\"";
            });
            Assert.That(rows, Is.GreaterThan(7000));
        }

        // AC-BLE-01-03 (caso 15): H:MM:SS con la hora sin cero a la izquierda si hay al menos 1 hora; si no MM:SS.
        [TestCase(0, "00:00", Category = "AC-BLE-01-03")]
        [TestCase(1, "00:01", Category = "AC-BLE-01-03")]
        [TestCase(59, "00:59", Category = "AC-BLE-01-03")]
        [TestCase(60, "01:00", Category = "AC-BLE-01-03")]
        [TestCase(61, "01:01", Category = "AC-BLE-01-03")]
        [TestCase(3599, "59:59", Category = "AC-BLE-01-03")]
        [TestCase(3600, "1:00:00", Category = "AC-BLE-01-03")]
        [TestCase(3661, "1:01:01", Category = "AC-BLE-01-03")]
        [TestCase(36000, "10:00:00", Category = "AC-BLE-01-03")]
        [TestCase(86400, "24:00:00", Category = "AC-BLE-01-03")]
        public void ElapsedText(int seconds, string expected)
        {
            Assert.That(new RowingData { ElapsedSeconds = seconds }.ElapsedFormatted, Is.EqualTo(expected));
        }

        [Test, Category("AC-BLE-01-03"), Category("AC-BASE-03")]
        public void MatchesOracle_Elapsed()
        {
            // {0…4000} ∪ {3599…3700} ∪ {35990…36010, 86400}, negativos y bordes de elapsedFormatted del legacy.
            var rows = GoldenVectors.CheckRows("rowing_data.json", "elapsedFormatted", row =>
            {
                var expected = row.Text("out");
                var actual = new RowingData { ElapsedSeconds = (int)row.Int("s") }.ElapsedFormatted;
                return actual == expected ? null : "esperado \"" + expected + "\", obtenido \"" + actual + "\"";
            });
            Assert.That(rows, Is.GreaterThan(4000));
        }

        // AC-BLE-01-04: copiar cambiando solo el pulso deja los otros 7 campos; un campo no nombrado nunca se borra.
        [Test, Category("AC-BLE-01-04")]
        public void CopyWithChangesOnlyNamedField()
        {
            var reading = new RowingData { StrokeRate = 24.0, PowerWatts = 150 };

            var copy = reading.CopyWith(heartRate: 140);
            Assert.That(copy.HeartRate, Is.EqualTo(140));
            Assert.That(copy.StrokeRate, Is.EqualTo(24.0));
            Assert.That(copy.PowerWatts, Is.EqualTo(150));
            Assert.That(copy.StrokeCount, Is.EqualTo(0));
            Assert.That(copy.DistanceMeters, Is.EqualTo(0));
            Assert.That(copy.Pace500mSeconds, Is.EqualTo(0));
            Assert.That(copy.TotalCalories, Is.EqualTo(0));
            Assert.That(copy.ElapsedSeconds, Is.EqualTo(0));
            Assert.That(reading.HeartRate, Is.EqualTo(0), "la original no cambia");

            // El pulsómetro reemplaza solo el pulso.
            Assert.That(reading.WithHeartRate(140), Is.EqualTo(copy));
            Assert.That(reading.CopyWith(), Is.EqualTo(reading), "sin argumentos = misma lectura");
        }

        [Test, Category("AC-BLE-01-04")]
        public void EachNamedFieldChangesAlone()
        {
            var full = new RowingData
            {
                StrokeRate = 24.5, StrokeCount = 298, DistanceMeters = 482, Pace500mSeconds = 133, PowerWatts = 150,
                TotalCalories = 45, HeartRate = 140, ElapsedSeconds = 300,
            };

            Assert.That(full.CopyWith(strokeRate: 30.0), Is.EqualTo(full with { StrokeRate = 30.0 }));
            Assert.That(full.CopyWith(strokeCount: 1), Is.EqualTo(full with { StrokeCount = 1 }));
            Assert.That(full.CopyWith(distanceMeters: 2), Is.EqualTo(full with { DistanceMeters = 2 }));
            Assert.That(full.CopyWith(pace500mSeconds: 3), Is.EqualTo(full with { Pace500mSeconds = 3 }));
            Assert.That(full.CopyWith(powerWatts: -4), Is.EqualTo(full with { PowerWatts = -4 }));
            Assert.That(full.CopyWith(totalCalories: 5), Is.EqualTo(full with { TotalCalories = 5 }));
            Assert.That(full.CopyWith(heartRate: 0), Is.EqualTo(full with { HeartRate = 0 }), "0 se nombra: no es 'conservar'");
            Assert.That(full.CopyWith(elapsedSeconds: 7), Is.EqualTo(full with { ElapsedSeconds = 7 }));
            Assert.That(full.CopyWith(), Is.EqualTo(full));
        }

        [Test, Category("AC-BLE-01-04"), Category("AC-BASE-03")]
        public void CopyWithMatchesOracle()
        {
            // Los casos de copyWith del legacy sobre una lectura con 24.0 spm y 150 W.
            var reading = new RowingData { StrokeRate = 24.0, PowerWatts = 150 };
            var cases = GoldenVectors.Load("rowing_data.json").Section("copyWith").Items;
            Assert.That(cases.Count, Is.GreaterThanOrEqualTo(5));
            foreach (var c in cases)
            {
                var name = c["case"].AsString();
                RowingData copy;
                switch (name)
                {
                    case "heartRate=140":
                        copy = reading.CopyWith(heartRate: 140);
                        break;
                    case "sin argumentos":
                        copy = reading.CopyWith();
                        break;
                    case "strokeRate=23.5":
                        copy = reading.CopyWith(strokeRate: 23.5);
                        break;
                    case "distanceMeters=482":
                        copy = reading.CopyWith(distanceMeters: 482);
                        break;
                    case "heartRate=0":
                        copy = reading.CopyWith(heartRate: 0);
                        break;
                    default:
                        Assert.Fail("caso de copyWith desconocido en rowing_data.json: " + name);
                        return;
                }

                AssertFields(copy, c["out"], name);
            }
        }

        // Compara los 8 campos (el ritmo por bits) y los dos textos con un objeto del oráculo.
        internal static void AssertFields(RowingData actual, JsonNode expected, string label)
        {
            Assert.That(BitConverter.DoubleToInt64Bits(actual.StrokeRate), Is.EqualTo(BitConverter.DoubleToInt64Bits(expected["strokeRate"].AsF64())),
                label + ": strokeRate");
            Assert.That(actual.StrokeCount, Is.EqualTo(expected["strokeCount"].AsLong()), label + ": strokeCount");
            Assert.That(actual.DistanceMeters, Is.EqualTo(expected["distanceMeters"].AsLong()), label + ": distanceMeters");
            Assert.That(actual.Pace500mSeconds, Is.EqualTo(expected["pace500mSeconds"].AsLong()), label + ": pace500mSeconds");
            Assert.That(actual.PowerWatts, Is.EqualTo(expected["powerWatts"].AsLong()), label + ": powerWatts");
            Assert.That(actual.TotalCalories, Is.EqualTo(expected["totalCalories"].AsLong()), label + ": totalCalories");
            Assert.That(actual.HeartRate, Is.EqualTo(expected["heartRate"].AsLong()), label + ": heartRate");
            Assert.That(actual.ElapsedSeconds, Is.EqualTo(expected["elapsedSeconds"].AsLong()), label + ": elapsedSeconds");
            if (expected.TryGet("pace500mFormatted", out var pace))
            {
                Assert.That(actual.Pace500mFormatted, Is.EqualTo(pace.AsString()), label + ": pace500mFormatted");
            }

            if (expected.TryGet("elapsedFormatted", out var elapsed))
            {
                Assert.That(actual.ElapsedFormatted, Is.EqualTo(elapsed.AsString()), label + ": elapsedFormatted");
            }
        }
    }
}
