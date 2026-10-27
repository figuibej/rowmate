using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // Regla de contacto con la piel (AC-HRM-04-02). legacy: lib/core/bluetooth/heart_rate_service.dart:321-326:
    // reading.sensorContact == false ? 0 : reading.bpm.
    [TestFixture]
    public sealed class HeartRateContactTests
    {
        [TestCase(72, false, 0, Category = "AC-HRM-04-02")]
        [TestCase(72, true, 72, Category = "AC-HRM-04-02")]
        [TestCase(72, null, 72, Category = "AC-HRM-04-02")]
        [TestCase(0, true, 0, Category = "AC-HRM-04-02")]
        [TestCase(0, null, 0, Category = "AC-HRM-04-02")]
        [TestCase(0, false, 0, Category = "AC-HRM-04-02")]
        [TestCase(300, null, 300, Category = "AC-HRM-04-02")]
        public void ContactRule(int bpm, bool? contact, int expected)
        {
            // "sin contacto" → 0; "con contacto" o "no informa" → el pulso leído, aunque sea 0 (sin validar rango, Q-48).
            Assert.That(HeartRateContact.PublishedBpm(new HeartRateReading(bpm, contact)), Is.EqualTo(expected));
        }

        [Test, Category("AC-HRM-04-02")]
        public void FromParsedPackets()
        {
            // Paquetes 0x2A37 reales: flags 0x06 = contacto soportado y detectado; 0x04 = soportado y sin contacto.
            var withContact = HeartRateParser.Parse(HexBytes.Parse("06 48"));
            var withoutContact = HeartRateParser.Parse(HexBytes.Parse("04 48"));
            var notReported = HeartRateParser.Parse(HexBytes.Parse("00 48"));
            Assert.That(withContact, Is.Not.Null);
            Assert.That(withoutContact, Is.Not.Null);
            Assert.That(notReported, Is.Not.Null);
            Assert.That(HeartRateContact.PublishedBpm(withContact!), Is.EqualTo(72));
            Assert.That(HeartRateContact.PublishedBpm(withoutContact!), Is.EqualTo(0));
            Assert.That(HeartRateContact.PublishedBpm(notReported!), Is.EqualTo(72));
        }
    }
}
