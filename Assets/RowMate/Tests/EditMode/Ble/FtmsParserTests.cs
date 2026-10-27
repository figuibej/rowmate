using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Models;
using RowMate.Core.Text;
using RowMate.Tests.EditMode.Models;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // FtmsParser = lib/core/bluetooth/ftms_parser.dart:26-140 (F-BLE-02). Paquetes en hex, un byte por grupo (spec §0.2).
    [TestFixture]
    public sealed class FtmsParserTests
    {
        // AC-BLE-02-01: servicio FTMS, Rower Data 0x2AD1 (no 0x2AD2, DC-01) y Control Point (ftms_parser.dart:22-24).
        [Test, Category("AC-BLE-02-01")]
        public void Uuids()
        {
            Assert.That(FtmsUuids.ServiceText, Is.EqualTo("00001826-0000-1000-8000-00805f9b34fb"));
            Assert.That(FtmsUuids.RowerDataText, Is.EqualTo("00002ad1-0000-1000-8000-00805f9b34fb"));
            Assert.That(FtmsUuids.ControlPointText, Is.EqualTo("00002ad9-0000-1000-8000-00805f9b34fb"));
            Assert.That(FtmsUuids.Service, Is.EqualTo(BleUuid.FromShort(0x1826)));
            Assert.That(FtmsUuids.RowerData, Is.EqualTo(BleUuid.FromShort(0x2AD1)));
            Assert.That(FtmsUuids.RowerData, Is.Not.EqualTo(BleUuid.FromShort(0x2AD2)), "DC-01: 0x2AD1, no 0x2AD2");
            Assert.That(FtmsUuids.ControlPoint, Is.EqualTo(BleUuid.FromShort(0x2AD9)));
            Assert.That(FtmsUuids.Service.ToString(), Is.EqualTo(FtmsUuids.ServiceText));
            Assert.That(FtmsUuids.RowerData.ToString(), Is.EqualTo(FtmsUuids.RowerDataText));
            Assert.That(FtmsUuids.ControlPoint.ToString(), Is.EqualTo(FtmsUuids.ControlPointText));
        }

        [Test, Category("AC-BLE-02-01"), Category("AC-BASE-03")]
        public void UuidsMatchOracle()
        {
            var uuids = GoldenVectors.Load("ftms.json").Section("uuids").Items[0];
            Assert.That(FtmsUuids.ServiceText, Is.EqualTo(uuids["ftmsServiceUuid"].AsString()));
            Assert.That(FtmsUuids.RowerDataText, Is.EqualTo(uuids["rowerDataUuid"].AsString()));
            Assert.That(FtmsUuids.ControlPointText, Is.EqualTo(uuids["controlPointUuid"].AsString()));
        }

        // AC-BLE-02-02 a AC-BLE-02-09: los 8 paquetes de referencia (spec §4.2). spm null = no hay lectura.
        // Cada caso escribe su línea de la demo (02-tech-design §8.7).
        [TestCase("2C 00 30 2A 01 E2 01 00 85 00 96 00", 24.0, 298, 482, 133, 150, 0, 0, 0, Category = "AC-BLE-02-02,Demo")]
        [TestCase("05 00 10 27 00", 0.0, 0, 10000, 0, 0, 0, 0, 0, Category = "AC-BLE-02-03,Demo")]
        [TestCase("11 00 78 00", 0.0, 0, 0, 120, 0, 0, 0, 0, Category = "AC-BLE-02-04,Demo")]
        [TestCase("19 00 00 00 7D 00", 0.0, 0, 0, 125, 0, 0, 0, 0, Category = "AC-BLE-02-05,Demo")]
        [TestCase("19 00 85 00 7D 00", 0.0, 0, 0, 133, 0, 0, 0, 0, Category = "AC-BLE-02-06,Demo")]
        [TestCase("01 0B 2D 00 00 00 00 8C 2C 01", 0.0, 0, 0, 0, 0, 45, 140, 300, Category = "AC-BLE-02-07,Demo")]
        [TestCase("00 00 30", 24.0, 0, 0, 0, 0, 0, 0, 0, Category = "AC-BLE-02-08,Demo")]
        [TestCase("2C", null, 0, 0, 0, 0, 0, 0, 0, Category = "AC-BLE-02-09,Demo")]
        public void ReferencePackets(string hex, double? spm, int strokes, int meters, int split, int watts, int kcal, int bpm, int seconds)
        {
            var actual = FtmsParser.ParseRowerData(HexBytes.Parse(hex));
            TestContext.Out.WriteLine(DemoLine(hex, actual));
            if (spm == null)
            {
                Assert.That(actual, Is.Null, "menos de 2 bytes: no hay lectura");
                return;
            }

            var expected = Reading(spm.Value, strokes, meters, split, watts, kcal, bpm, seconds);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test, Category("AC-BLE-02-02"), Category("AC-BLE-02-06"), Category("AC-BLE-02-07")]
        public void ReferenceSplitTexts()
        {
            // El split entre paréntesis de la tabla de la spec es su texto (AC-BLE-01-02).
            Assert.That(Parse("2C 00 30 2A 01 E2 01 00 85 00 96 00").Pace500mFormatted, Is.EqualTo("02:13"));
            Assert.That(Parse("05 00 10 27 00").Pace500mFormatted, Is.EqualTo("--:--"));
            Assert.That(Parse("11 00 78 00").Pace500mFormatted, Is.EqualTo("02:00"));
            Assert.That(Parse("19 00 00 00 7D 00").Pace500mFormatted, Is.EqualTo("02:05"));
            Assert.That(Parse("19 00 85 00 7D 00").Pace500mFormatted, Is.EqualTo("02:13"));
            Assert.That(Parse("01 0B 2D 00 00 00 00 8C 2C 01").ElapsedFormatted, Is.EqualTo("05:00"));
            Assert.That(Parse("00 00 30").Pace500mFormatted, Is.EqualTo("--:--"));
        }

        // AC-BLE-02-10: el ritmo es el byte crudo × 0,5 (ftms_parser.dart:46).
        [Test, Category("AC-BLE-02-10")]
        public void StrokeRateIsHalfRaw()
        {
            Assert.That(Parse("00 00 30").StrokeRate, Is.EqualTo(24.0));
            Assert.That(Parse("00 00 F6").StrokeRate, Is.EqualTo(123.0));
            for (var b = 0; b <= 255; b++)
            {
                var reading = Parse(new byte[] { 0x00, 0x00, (byte)b });
                Assert.That(reading.StrokeRate, Is.EqualTo(b * 0.5), "byte " + b);
            }
        }

        // AC-BLE-02-11: potencia sint16; la promedio se usa SOLO si la instantánea es 0 (ftms_parser.dart:83-94).
        [Test, Category("AC-BLE-02-11")]
        public void SignedPowerAndAverageOnlyIfZero()
        {
            var negative = Parse("21 00 F6 FF");
            Assert.That(negative, Is.EqualTo(Reading(0.0, 0, 0, 0, -10, 0, 0, 0)));
            Assert.That(RowingFormats.Watts(negative.PowerWatts), Is.EqualTo("-10"));
            Assert.That(Parse("61 00 00 00 64 00").PowerWatts, Is.EqualTo(100), "instantánea 0: se usa la promedio");
            Assert.That(Parse("61 00 32 00 64 00").PowerWatts, Is.EqualTo(50), "instantánea distinta de 0: la promedio se ignora");
            Assert.That(Parse("41 00 9C FF").PowerWatts, Is.EqualTo(-100), "solo promedio, con signo");
        }

        // AC-BLE-02-12: los bits 1, 7 y 10 se saltean (sin verificar bytes); el bit 12 se ignora.
        [TestCase("07 00 14 10 27 00", 0.0, 0, 10000, 0, 0, 0, 0, 0, Category = "AC-BLE-02-12")]
        [TestCase("81 01 05 00 2D 00 00 00 00", 0.0, 0, 0, 0, 0, 45, 0, 0, Category = "AC-BLE-02-12")]
        [TestCase("01 0C 03 2C 01", 0.0, 0, 0, 0, 0, 0, 0, 300, Category = "AC-BLE-02-12")]
        [TestCase("01 10 3C 00", 0.0, 0, 0, 0, 0, 0, 0, 0, Category = "AC-BLE-02-12")]
        public void SkippedFields(string hex, double spm, int strokes, int meters, int split, int watts, int kcal, int bpm, int seconds)
        {
            Assert.That(Parse(hex), Is.EqualTo(Reading(spm, strokes, meters, split, watts, kcal, bpm, seconds)));
        }

        // AC-BLE-02-13: truncados y sobrantes se procesan igual; 2 bytes alcanzan.
        [TestCase("01 01 2D 00", 0.0, 0, 0, 0, 0, 45, 0, 0, Category = "AC-BLE-02-13")]
        [TestCase("2C 00 30 2A 01 E2 01 00 85 00 96 00 FF FF", 24.0, 298, 482, 133, 150, 0, 0, 0, Category = "AC-BLE-02-13")]
        [TestCase("00 00", 0.0, 0, 0, 0, 0, 0, 0, 0, Category = "AC-BLE-02-13")]
        public void TruncatedAndTrailingBytes(string hex, double spm, int strokes, int meters, int split, int watts, int kcal, int bpm, int seconds)
        {
            var actual = FtmsParser.ParseRowerData(HexBytes.Parse(hex));
            Assert.That(actual, Is.Not.Null, "se procesa igual: no se descarta");
            Assert.That(actual, Is.EqualTo(Reading(spm, strokes, meters, split, watts, kcal, bpm, seconds)));
        }

        [Test, Category("AC-BLE-02-13")]
        public void NoPacketThrows()
        {
            // Ningún paquete raro produce un error: todas las banderas de 13 bits (y con los bits altos), largos 0-24,
            // con bytes en 0x00 y en 0xFF. Más de 2 bytes nunca da null.
            var parsed = 0;
            var wrong = new List<string>();
            var fills = new byte[] { 0x00, 0xFF };
            for (var flags = 0; flags <= 0xFFFF; flags += flags < 0x2000 ? 1 : 0x0FFF)
            {
                for (var length = 0; length <= 24; length++)
                {
                    for (var f = 0; f < fills.Length; f++)
                    {
                        var bytes = new byte[length];
                        for (var i = 0; i < length; i++)
                        {
                            bytes[i] = fills[f];
                        }

                        if (length >= 1)
                        {
                            bytes[0] = (byte)(flags & 0xFF);
                        }

                        if (length >= 2)
                        {
                            bytes[1] = (byte)(flags >> 8);
                        }

                        var reading = FtmsParser.ParseRowerData(bytes);
                        if ((reading == null) != (length < 2) && wrong.Count < 20)
                        {
                            wrong.Add(HexBytes.Format(bytes));
                        }

                        parsed++;
                    }
                }
            }

            Assert.That(wrong, Is.Empty, "con 2 bytes o más siempre hay lectura; con menos, nunca");
            Assert.That(parsed, Is.GreaterThan(400000));
        }

        // AC-BLE-02-14 (Q-38): los valores crudos se toman tal cual (65535 de "no disponible", potencia negativa).
        [Test, Category("AC-BLE-02-14")]
        public void RawUnavailableValues()
        {
            Assert.That(Parse("01 01 FF FF FF FF FF").TotalCalories, Is.EqualTo(65535));
            Assert.That(Parse("01 08 FF FF FF FF FF FF FF FF").ElapsedSeconds, Is.EqualTo(65535));
            var distanceAndPace = Parse("0D 00 FF FF FF FF FF");
            Assert.That(distanceAndPace.DistanceMeters, Is.EqualTo(16777215));
            Assert.That(distanceAndPace.Pace500mSeconds, Is.EqualTo(65535));
            Assert.That(Parse("01 02 FF").HeartRate, Is.EqualTo(255));
            Assert.That(Parse("21 00 F6 FF").PowerWatts, Is.EqualTo(-10), "la potencia negativa también pasa tal cual");
        }

        // AC-BLE-02-15: un campo ausente vale 0; no se conserva el valor del paquete anterior (ftms_parser.dart:33-40).
        [Test, Category("AC-BLE-02-15")]
        public void MissingFieldIsZeroNotPrevious()
        {
            var first = Parse("2C 00 30 2A 01 E2 01 00 85 00 96 00");
            Assert.That(first.DistanceMeters, Is.EqualTo(482));
            Assert.That(first.PowerWatts, Is.EqualTo(150));
            Assert.That(first.StrokeRate, Is.EqualTo(24.0));

            var next = Parse("11 00 78 00");
            Assert.That(next.DistanceMeters, Is.EqualTo(0));
            Assert.That(next.PowerWatts, Is.EqualTo(0));
            Assert.That(next.StrokeRate, Is.EqualTo(0.0));
            Assert.That(next.Pace500mSeconds, Is.EqualTo(120));
        }

        // AC-BLE-02-16: una notificación de 0 bytes no genera lectura ni error (ble_service.dart:292; ftms_parser.dart:27).
        [Test, Category("AC-BLE-02-16")]
        public void EmptyNotificationGivesNoReading()
        {
            Assert.That(FtmsParser.ParseRowerData(new byte[0]), Is.Null);
            Assert.That(FtmsParser.ParseRowerData(ReadOnlySpan<byte>.Empty), Is.Null);
            Assert.That(FtmsParser.ParseRowerData(HexBytes.Parse("")), Is.Null);
        }

        [Test, Category("AC-BLE-02-02")]
        public void SpanAndArrayGiveTheSameReading()
        {
            var bytes = HexBytes.Parse("2C 00 30 2A 01 E2 01 00 85 00 96 00 FF FF");
            Assert.That(FtmsParser.ParseRowerData(new ReadOnlySpan<byte>(bytes, 0, 12)), Is.EqualTo(Parse("2C 00 30 2A 01 E2 01 00 85 00 96 00")));
            Assert.Throws<ArgumentNullException>(() => FtmsParser.ParseRowerData((byte[])null!));
        }

        // Todos los paquetes con nombre de AC-BLE-02-02 a -16 contra el legacy (8 campos; el ritmo por bits).
        [Test, Category("AC-BASE-03"), Category("AC-BLE-02-02"), Category("AC-BLE-02-03"), Category("AC-BLE-02-04")]
        [Category("AC-BLE-02-05"), Category("AC-BLE-02-06"), Category("AC-BLE-02-07"), Category("AC-BLE-02-08")]
        [Category("AC-BLE-02-09"), Category("AC-BLE-02-10"), Category("AC-BLE-02-11"), Category("AC-BLE-02-12")]
        [Category("AC-BLE-02-13"), Category("AC-BLE-02-14"), Category("AC-BLE-02-15"), Category("AC-BLE-02-16")]
        public void MatchesOracle()
        {
            var packets = GoldenVectors.Load("ftms.json").Section("packets").Items;
            var covered = new HashSet<string>(StringComparer.Ordinal);
            foreach (var packet in packets)
            {
                var hex = packet["bytes"].AsString();
                var label = packet["ac"].AsString() + " " + hex;
                covered.Add(packet["ac"].AsString());
                var actual = FtmsParser.ParseRowerData(HexBytes.Parse(hex));
                var expected = packet["out"];
                if (expected.IsNull)
                {
                    Assert.That(actual, Is.Null, label);
                    continue;
                }

                Assert.That(actual, Is.Not.Null, label);
                RowingDataTests.AssertFields(actual!, expected, label);
            }

            for (var ac = 2; ac <= 16; ac++)
            {
                var id = "AC-BLE-02-" + ac.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(covered.Contains(id), Is.True, "ftms.json no tiene paquetes de " + id);
            }
        }

        // Fuzz de paridad: 1 000 paquetes aleatorios de Random(2026) del oráculo (banderas de 13 bits, largo 0-24).
        [Test, Category("AC-BASE-03"), Category("AC-BLE-02-13")]
        public void MatchesOracle_Fuzz()
        {
            var nulls = 0;
            var rows = GoldenVectors.CheckRows("ftms.json", "fuzz", row =>
            {
                var actual = FtmsParser.ParseRowerData(HexBytes.Parse(row.Text("bytes")));
                if (row.Int("ok") == 0)
                {
                    nulls++;
                    return actual == null ? null : "esperado null, obtenido " + actual;
                }

                if (actual == null)
                {
                    return "esperado una lectura, obtenido null";
                }

                var expected = new RowingData
                {
                    StrokeRate = row.F64("strokeRate"),
                    StrokeCount = (int)row.Int("strokeCount"),
                    DistanceMeters = (int)row.Int("distanceMeters"),
                    Pace500mSeconds = (int)row.Int("pace500mSeconds"),
                    PowerWatts = (int)row.Int("powerWatts"),
                    TotalCalories = (int)row.Int("totalCalories"),
                    HeartRate = (int)row.Int("heartRate"),
                    ElapsedSeconds = (int)row.Int("elapsedSeconds"),
                };
                return actual == expected ? null : "esperado " + expected + ", obtenido " + actual;
            });
            Assert.That(rows, Is.EqualTo(1000));
            Assert.That(nulls, Is.GreaterThan(0), "el fuzz tiene que incluir paquetes descartados");
            Assert.That(nulls, Is.LessThan(rows), "el fuzz tiene que incluir lecturas");
        }

        private static RowingData Parse(string hex)
        {
            return Parse(HexBytes.Parse(hex));
        }

        private static RowingData Parse(byte[] bytes)
        {
            var reading = FtmsParser.ParseRowerData(bytes);
            Assert.That(reading, Is.Not.Null, "se esperaba una lectura de " + HexBytes.Format(bytes));
            return reading!;
        }

        private static RowingData Reading(double spm, int strokes, int meters, int split, int watts, int kcal, int bpm, int seconds)
        {
            return new RowingData
            {
                StrokeRate = spm,
                StrokeCount = strokes,
                DistanceMeters = meters,
                Pace500mSeconds = split,
                PowerWatts = watts,
                TotalCalories = kcal,
                HeartRate = bpm,
                ElapsedSeconds = seconds,
            };
        }

        // Línea de la demo (02-tech-design §8.7), con Fmt/TimeFormats (cultura invariante).
        private static string DemoLine(string hex, RowingData? d)
        {
            if (d == null)
            {
                return "[DEMO] FTMS " + hex + " -> sin lectura";
            }

            return "[DEMO] FTMS " + hex + " -> spm=" + RowingFormats.Spm(d.StrokeRate)
                + " paladas=" + Fmt.Integer(d.StrokeCount)
                + " m=" + Fmt.Integer(d.DistanceMeters)
                + " split=" + Fmt.Integer(d.Pace500mSeconds) + " (" + d.Pace500mFormatted + ")"
                + " W=" + Fmt.Integer(d.PowerWatts)
                + " kcal=" + Fmt.Integer(d.TotalCalories)
                + " bpm=" + Fmt.Integer(d.HeartRate)
                + " t=" + Fmt.Integer(d.ElapsedSeconds) + " (" + d.ElapsedFormatted + ")";
        }
    }
}
