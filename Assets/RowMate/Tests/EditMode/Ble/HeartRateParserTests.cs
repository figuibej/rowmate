using System;
using System.Collections.Generic;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Common.Json;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // HeartRateParser = lib/core/bluetooth/heart_rate_parser.dart:20-40 (F-HRM-01). Los casos LT-10 a LT-15 son los de
    // test/heart_rate_parser_test.dart, 1 a 1, con su nombre original.
    [TestFixture]
    public sealed class HeartRateParserTests
    {
        // legacy: test/heart_rate_parser_test.dart:5 'bpm en uint8 sin información de contacto'
        [TestCase(TestName = "bpm en uint8 sin información de contacto", Category = "LT-10,AC-HRM-01-02")]
        public void Uint8WithoutContactInformation()
        {
            var r = HeartRateParser.Parse(new byte[] { 0x00, 72 });
            Assert.That(r, Is.Not.Null);
            Assert.That(r!.Bpm, Is.EqualTo(72));
            Assert.That(r.SensorContact, Is.Null);
        }

        // legacy: test/heart_rate_parser_test.dart:11 'bpm en uint16 little-endian'
        [TestCase(TestName = "bpm en uint16 little-endian", Category = "LT-11,AC-HRM-01-03")]
        public void Uint16LittleEndian()
        {
            var r = HeartRateParser.Parse(new byte[] { 0x01, 0x2C, 0x01 }); // 0x012C = 300
            Assert.That(r, Is.Not.Null);
            Assert.That(r!.Bpm, Is.EqualTo(300));
        }

        // legacy: test/heart_rate_parser_test.dart:16 'bits de contacto: 01 = no soportado, 10 = sin contacto, 11 = con contacto'
        [TestCase(TestName = "bits de contacto: 01 = no soportado, 10 = sin contacto, 11 = con contacto", Category = "LT-12,AC-HRM-01-04")]
        public void ContactBits()
        {
            Assert.That(Parse(0x02, 70).SensorContact, Is.Null);
            Assert.That(Parse(0x04, 0).SensorContact, Is.False);
            Assert.That(Parse(0x06, 65).SensorContact, Is.True);
            Assert.That(Parse(0x06, 65).Bpm, Is.EqualTo(65));
        }

        // legacy: test/heart_rate_parser_test.dart:23 'ignora Energy Expended y RR intervals'
        [TestCase(TestName = "ignora Energy Expended y RR intervals", Category = "LT-13,AC-HRM-01-05")]
        public void IgnoresEnergyExpendedAndRrIntervals()
        {
            // flags 0x18 = bit3 (energía) + bit4 (RR); bpm 80; energía 0x0010; RR 0x0400
            var r = HeartRateParser.Parse(new byte[] { 0x18, 80, 0x10, 0x00, 0x00, 0x04 });
            Assert.That(r, Is.Not.Null);
            Assert.That(r!.Bpm, Is.EqualTo(80));
        }

        // legacy: test/heart_rate_parser_test.dart:29 'bytes vacíos o truncados devuelven null'
        [TestCase(TestName = "bytes vacíos o truncados devuelven null", Category = "LT-14,AC-HRM-01-06")]
        public void EmptyOrTruncatedGiveNull()
        {
            Assert.That(HeartRateParser.Parse(new byte[0]), Is.Null);
            Assert.That(HeartRateParser.Parse(new byte[] { 0x00 }), Is.Null);
            Assert.That(HeartRateParser.Parse(new byte[] { 0x01, 72 }), Is.Null, "uint16 necesita 3 bytes");
        }

        // legacy: test/heart_rate_parser_test.dart:35 'UUIDs del perfil'
        [TestCase(TestName = "UUIDs del perfil", Category = "LT-15,AC-HRM-01-01")]
        public void ProfileUuids()
        {
            Assert.That(HeartRateParser.ServiceUuid, Is.EqualTo("0000180d-0000-1000-8000-00805f9b34fb"));
            Assert.That(HeartRateParser.MeasurementUuid, Is.EqualTo("00002a37-0000-1000-8000-00805f9b34fb"));
        }

        [Test, Category("AC-HRM-01-01")]
        public void ProfileUuidsAsBleUuid()
        {
            Assert.That(HeartRateParser.Service, Is.EqualTo(BleUuid.FromShort(0x180D)));
            Assert.That(HeartRateParser.Measurement, Is.EqualTo(BleUuid.FromShort(0x2A37)));
            Assert.That(HeartRateParser.Service.ToString(), Is.EqualTo(HeartRateParser.ServiceUuid));
            Assert.That(HeartRateParser.Measurement.ToString(), Is.EqualTo(HeartRateParser.MeasurementUuid));
        }

        [Test, Category("AC-HRM-01-01"), Category("AC-BASE-03")]
        public void UuidsMatchOracle()
        {
            var uuids = GoldenVectors.Load("heart_rate.json").Section("uuids").Items[0];
            Assert.That(HeartRateParser.ServiceUuid, Is.EqualTo(uuids["serviceUuid"].AsString()));
            Assert.That(HeartRateParser.MeasurementUuid, Is.EqualTo(uuids["measurementUuid"].AsString()));
        }

        // AC-HRM-01-06: el formato de 16 bits con 2 bytes se descarta, no "cae" a 8 bits.
        [Test, Category("AC-HRM-01-06")]
        public void Uint16WithTwoBytesDoesNotFallBackToUint8()
        {
            for (var flags = 0; flags <= 0xFF; flags++)
            {
                var two = HeartRateParser.Parse(new byte[] { (byte)flags, 72 });
                if ((flags & 0x01) != 0)
                {
                    Assert.That(two, Is.Null, "flags " + flags);
                }
                else
                {
                    Assert.That(two, Is.Not.Null, "flags " + flags);
                    Assert.That(two!.Bpm, Is.EqualTo(72));
                }

                Assert.That(HeartRateParser.Parse(new byte[] { (byte)flags }), Is.Null, "solo banderas");
            }

            Assert.That(HeartRateParser.Parse(ReadOnlySpan<byte>.Empty), Is.Null);
        }

        // AC-HRM-01-07 (Q-48): sin validación de rango.
        [Test, Category("AC-HRM-01-07")]
        public void NoRangeValidation()
        {
            Assert.That(Parse(0x00, 0).Bpm, Is.EqualTo(0));
            Assert.That(Parse(0x00, 255).Bpm, Is.EqualTo(255));
            Assert.That(Parse(0x01, 0xFF, 0xFF).Bpm, Is.EqualTo(65535));
        }

        // AC-HRM-01-08: los bits reservados no cambian nada y los bytes de sobra se ignoran.
        [Test, Category("AC-HRM-01-08")]
        public void ReservedBitsAndTrailingBytes()
        {
            Assert.That(Parse(0xE0, 90, 1, 2, 3), Is.EqualTo(new HeartRateReading(90, null)));
            Assert.That(Parse(0x01, 72, 0), Is.EqualTo(new HeartRateReading(72, null)));
        }

        // AC-HRM-01-09: 16 bits con contacto.
        [Test, Category("AC-HRM-01-09")]
        public void Uint16WithContact()
        {
            Assert.That(Parse(0x07, 0x2C, 0x01), Is.EqualTo(new HeartRateReading(300, true)));
        }

        [Test, Category("AC-HRM-01-02")]
        public void SpanAndArrayGiveTheSameReading()
        {
            var bytes = new byte[] { 0x06, 65, 0xAA };
            Assert.That(HeartRateParser.Parse(new ReadOnlySpan<byte>(bytes, 0, 2)), Is.EqualTo(HeartRateParser.Parse(bytes)));
            Assert.Throws<ArgumentNullException>(() => HeartRateParser.Parse((byte[])null!));
        }

        // Los paquetes de AC-HRM-01 contra el legacy (bpm y contacto, o null).
        [Test, Category("AC-BASE-03"), Category("AC-HRM-01-02"), Category("AC-HRM-01-03"), Category("AC-HRM-01-04")]
        [Category("AC-HRM-01-05"), Category("AC-HRM-01-06"), Category("AC-HRM-01-07"), Category("AC-HRM-01-08"), Category("AC-HRM-01-09")]
        public void MatchesOracle()
        {
            var covered = new HashSet<string>(StringComparer.Ordinal);
            foreach (var packet in GoldenVectors.Load("heart_rate.json").Section("packets").Items)
            {
                var hex = packet["bytes"].AsString();
                var label = packet["ac"].AsString() + " [" + hex + "]";
                covered.Add(packet["ac"].AsString());
                var actual = HeartRateParser.Parse(HexBytes.Parse(hex));
                var expected = packet["out"];
                if (expected.IsNull)
                {
                    Assert.That(actual, Is.Null, label);
                    continue;
                }

                Assert.That(actual, Is.EqualTo(new HeartRateReading((int)expected["bpm"].AsLong(), Contact(expected["contact"]))), label);
            }

            for (var ac = 2; ac <= 9; ac++)
            {
                Assert.That(covered.Contains("AC-HRM-01-0" + ac), Is.True, "heart_rate.json no tiene paquetes de AC-HRM-01-0" + ac);
            }
        }

        // Fuzz de paridad: 500 paquetes aleatorios de Random(2037) del oráculo (largo 0-8).
        [Test, Category("AC-BASE-03"), Category("AC-HRM-01-06")]
        public void MatchesOracle_Fuzz()
        {
            var rows = GoldenVectors.CheckRows("heart_rate.json", "fuzz", row =>
            {
                var actual = HeartRateParser.Parse(HexBytes.Parse(row.Text("bytes")));
                if (row.Int("ok") == 0)
                {
                    return actual == null ? null : "esperado null, obtenido " + actual;
                }

                var code = row.Int("contact");
                var expected = new HeartRateReading((int)row.Int("bpm"), code < 0 ? (bool?)null : code == 1);
                return actual == expected ? null : "esperado " + expected + ", obtenido " + (actual == null ? "null" : actual.ToString());
            });
            Assert.That(rows, Is.EqualTo(500));
        }

        private static HeartRateReading Parse(params byte[] bytes)
        {
            var reading = HeartRateParser.Parse(bytes);
            Assert.That(reading, Is.Not.Null, "se esperaba una lectura de " + HexBytes.Format(bytes));
            return reading!;
        }

        private static bool? Contact(JsonNode node)
        {
            return node.IsNull ? (bool?)null : node.AsBool();
        }
    }
}
