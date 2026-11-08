using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Models;

namespace RowMate.Tests.EditMode.Ble
{
    // BHX y compañía: el hex del diagnóstico (device_provider.dart:98-100, ble_service.dart:225, :293), la máscara de las direcciones
    // del registro (DT10-05) y la medición de la cadencia del monitor (QA-V4a-L01, 10a 02-tech-design §7).
    [TestFixture]
    public sealed class BleSupportTests
    {
        [Test, Category("AC-BLE-12-02")]
        public void HexIsLowercaseTwoDigitsSpaceSeparated()
        {
            Assert.That(BleHex.Format(new byte[] { 0x2C, 0x00, 0x30, 0xFF, 0x0A }), Is.EqualTo("2c 00 30 ff 0a"));
            Assert.That(BleHex.Format(new byte[] { 0x07 }), Is.EqualTo("07"));
            Assert.That(BleHex.Format(new byte[0]), Is.EqualTo(""));
            Assert.That(BleHex.ForDiagnostics(null), Is.EqualTo("(sin datos)"));
            Assert.That(BleHex.ForDiagnostics(new byte[0]), Is.EqualTo("(sin datos)"));
            Assert.That(BleHex.ForDiagnostics(new byte[] { 0xAB }), Is.EqualTo("ab"));
        }

        [Test, Category("AC-BLE-SEC-01")]
        public void AddressesAreMaskedInTheLog()
        {
            Assert.That(BleIds.Mask("AA:BB:CC:00:06:70"), Is.EqualTo("AA:BB:…:70"));
            Assert.That(BleIds.Mask("ABCDEFGH"), Is.EqualTo("AB…GH"));
            Assert.That(BleIds.Mask("AB"), Is.EqualTo("…"));
            Assert.That(BleIds.Mask(null), Is.EqualTo("?"));
        }

        [Test, Category("AC-WKT-03-11")]
        public void CadenceMeasuresPacketsDistanceAndStrokes()
        {
            // Un paquete por segundo y la distancia cada 2 s (+9 m): intervalo p50 1 s, distancia cada 2 s, 4,5 m/s; una palada nueva
            // cada 2 paquetes con su hora y su potencia.
            var cadence = new BleCadence();
            for (var k = 0; k <= 10; k++)
            {
                cadence.Add(k, new RowingData { DistanceMeters = 1000 + 9 * (k / 2), StrokeCount = 100 + k / 2, PowerWatts = 150 + k });
            }

            Assert.That(cadence.Packets, Is.EqualTo(11));
            Assert.That(cadence.PacketPercentile(0.5), Is.EqualTo(1.0));
            Assert.That(cadence.PacketBucket(1), Is.EqualTo(10), "10 intervalos entre 0,75 y 1,5 s");
            Assert.That(cadence.DistancePercentile(0.5), Is.EqualTo(2.0));
            Assert.That(cadence.DistanceBucket(2), Is.EqualTo(5), "5 cambios entre 1,5 y 2,5 s");
            Assert.That(cadence.MaxSpeed, Is.EqualTo(4.5).Within(1e-9));
            Assert.That(cadence.StrokeChanges, Is.EqualTo(5));
            var report = cadence.Report();
            Assert.That(report, Does.StartWith("Cadencia: paquetes=11 intervalo p50=1.00s p95=1.00s [0/10/0/0] distancia cada p50=2.00s"));
            Assert.That(report, Does.Contain("maxVel=4.50m/s paladas=5 (2.00s 152W, 4.00s 154W"));
            cadence.Reset();
            Assert.That(cadence.Packets, Is.EqualTo(0));
            Assert.That(cadence.Report(), Does.Contain("intervalo p50=- p95=-"));
        }
    }
}
