using System;
using NUnit.Framework;
using RowMate.Core.Ble;

namespace RowMate.Tests.EditMode.Ble
{
    // BleUuid = Guid de flutter_blue_plus_platform_interface 9.0.3 (lib/src/guid.dart:24-90): igualdad por str128.
    [TestFixture]
    public sealed class BleUuidTests
    {
        // AC-BLE-03-04: 16, 32 y 128 bits, mayúsculas o minúsculas, son el mismo servicio.
        [TestCase("180d", Category = "AC-BLE-03-04")]
        [TestCase("180D", Category = "AC-BLE-03-04")]
        [TestCase("0000180d", Category = "AC-BLE-03-04")]
        [TestCase("0000180d-0000-1000-8000-00805f9b34fb", Category = "AC-BLE-03-04")]
        [TestCase("0000180D-0000-1000-8000-00805F9B34FB", Category = "AC-BLE-03-04")]
        public void ShortThirtyTwoAndLongFormsAreEqual(string text)
        {
            var heartRate = BleUuid.FromShort(0x180D);
            var parsed = BleUuid.Parse(text);
            Assert.That(parsed, Is.EqualTo(heartRate));
            Assert.That(parsed == heartRate, Is.True);
            Assert.That(parsed != heartRate, Is.False);
            Assert.That(parsed.GetHashCode(), Is.EqualTo(heartRate.GetHashCode()));
            Assert.That(parsed.ToString(), Is.EqualTo("0000180d-0000-1000-8000-00805f9b34fb"));
        }

        [Test, Category("AC-BLE-03-04")]
        public void FtmsServiceInBothForms()
        {
            Assert.That(BleUuid.Parse("1826"), Is.EqualTo(BleUuid.Parse("00001826-0000-1000-8000-00805f9b34fb")));
            Assert.That(BleUuid.From32(0x00001826), Is.EqualTo(BleUuid.FromShort(0x1826)));
        }

        [Test, Category("AC-BLE-03-04")]
        public void DifferentServicesAreDifferent()
        {
            Assert.That(BleUuid.Parse("180d"), Is.Not.EqualTo(BleUuid.Parse("180e")));
            Assert.That(BleUuid.Parse("180d"), Is.Not.EqualTo(BleUuid.Parse("1826")));
            Assert.That(BleUuid.Parse("febe"), Is.Not.EqualTo(BleUuid.Parse("0000180d-0000-1000-8000-00805f9b34fb")));
            Assert.That(BleUuid.Parse("0000180d-0000-1000-8000-00805f9b34fc"), Is.Not.EqualTo(BleUuid.FromShort(0x180D)));
        }

        [TestCase("180D", "180d")]
        [TestCase("0000180d-0000-1000-8000-00805f9b34fb", "180d")]
        [TestCase("12345678", "12345678")]
        [TestCase("12340000-0000-1000-8000-00805f9b34fb", "12340000")]
        [TestCase("6e400001-b5a3-f393-e0a9-e50e24dcca9e", "6e400001-b5a3-f393-e0a9-e50e24dcca9e")]
        [TestCase("6E400001B5A3F393E0A9E50E24DCCA9E", "6e400001-b5a3-f393-e0a9-e50e24dcca9e")]
        public void ShortStringIsLikeFlutterBluePlusStr(string text, string expected)
        {
            Assert.That(BleUuid.Parse(text).ToShortString(), Is.EqualTo(expected));
        }

        [Test]
        public void EmptyTextIsTheEmptyUuid()
        {
            // Guid('') de FBP = List.filled(16, 0).
            Assert.That(BleUuid.Parse(""), Is.EqualTo(BleUuid.Empty));
            Assert.That(BleUuid.Empty.ToString(), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
            Assert.That(BleUuid.Empty.ToShortString(), Is.EqualTo("00000000-0000-0000-0000-000000000000"));
            Assert.That(BleUuid.TryParse("", out var empty), Is.True);
            Assert.That(empty, Is.EqualTo(BleUuid.Empty));
        }

        [Test]
        public void DashesAreIgnoredAnywhere()
        {
            // replaceAll('-', '') de guid.dart:29.
            Assert.That(BleUuid.Parse("18-0d"), Is.EqualTo(BleUuid.FromShort(0x180D)));
        }

        [TestCase("18")]
        [TestCase("180")]
        [TestCase("180d0")]
        [TestCase("180d00")]
        [TestCase("g80d")]
        [TestCase("----")]
        [TestCase("0000180d-0000-1000-8000-00805f9b34fb00")]
        [TestCase("{0000180d-0000-1000-8000-00805f9b34fb}")]
        public void InvalidTextIsAFormatError(string text)
        {
            Assert.Throws<FormatException>(() => BleUuid.Parse(text));
            Assert.That(BleUuid.TryParse(text, out _), Is.False);
        }

        [Test]
        public void TryParseOfNullIsFalse()
        {
            Assert.That(BleUuid.TryParse(null, out var value), Is.False);
            Assert.That(value, Is.EqualTo(BleUuid.Empty));
            Assert.Throws<ArgumentNullException>(() => BleUuid.Parse(null!));
        }

        [Test]
        public void HighAndLowHoldThe128Bits()
        {
            var uuid = BleUuid.Parse("0000180d-0000-1000-8000-00805f9b34fb");
            Assert.That(uuid.High, Is.EqualTo(0x0000180D00001000UL));
            Assert.That(uuid.Low, Is.EqualTo(0x800000805F9B34FBUL));
            Assert.That(new BleUuid(uuid.High, uuid.Low), Is.EqualTo(uuid));
        }
    }
}
