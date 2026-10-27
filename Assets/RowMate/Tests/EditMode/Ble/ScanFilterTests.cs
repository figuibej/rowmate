using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Core.Common;

namespace RowMate.Tests.EditMode.Ble
{
    // ScanFilter = lib/core/bluetooth/scan_filter.dart:12-20 (F-BLE-03). Los casos LT-08 y LT-09 son los de
    // test/scan_filter_test.dart, 1 a 1, con su nombre original. La igualdad de UUID es la de BleUuid (Guid de FBP 9.0.3).
    [TestFixture]
    public sealed class ScanFilterTests
    {
        private static readonly BleUuid Hrs = BleUuid.Parse(HeartRateParser.ServiceUuid);

        // legacy: test/scan_filter_test.dart:24 'con filtro en el cliente solo pasan los que anuncian el servicio'
        [TestCase(TestName = "con filtro en el cliente solo pasan los que anuncian el servicio", Category = "LT-08,AC-BLE-03-02")]
        public void ClientSideFilterOnlyAcceptsAdvertisers()
        {
            Assert.That(ScanFilter.Accept(Result(), Hrs, clientSide: true), Is.False);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("febe")), Hrs, clientSide: true), Is.False);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("180d")), Hrs, clientSide: true), Is.True,
                "el UUID de 16 bits equivale al de 128");
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("febe"), Hrs), Hrs, clientSide: true), Is.True);
        }

        // legacy: test/scan_filter_test.dart:32 'sin filtro en el cliente (el sistema ya filtró) pasa todo'
        [TestCase(TestName = "sin filtro en el cliente (el sistema ya filtró) pasa todo", Category = "LT-09,AC-BLE-03-03")]
        public void WithoutClientSideFilterEverythingPasses()
        {
            Assert.That(ScanFilter.Accept(Result(), Hrs, clientSide: false), Is.True);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("febe")), Hrs, clientSide: false), Is.True);
        }

        // AC-BLE-03-01: se filtra en la app solo en Windows; en Android, iOS y macOS confía en el filtro del sistema.
        [Test, Category("AC-BLE-03-01")]
        public void ClientSideOnlyOnWindows()
        {
            Assert.That(ScanFilter.IsClientSide(PlatformKind.Windows), Is.True);
            Assert.That(ScanFilter.IsClientSide(PlatformKind.Android), Is.False);
            Assert.That(ScanFilter.IsClientSide(PlatformKind.Ios), Is.False);
            Assert.That(ScanFilter.IsClientSide(PlatformKind.MacOs), Is.False);

            // Compuesto como lo usan los servicios (ble_service.dart:106, heart_rate_service.dart:136).
            var headphones = Result(BleUuid.Parse("febe"));
            Assert.That(ScanFilter.Accept(headphones, Hrs, ScanFilter.IsClientSide(PlatformKind.Windows)), Is.False);
            Assert.That(ScanFilter.Accept(headphones, Hrs, ScanFilter.IsClientSide(PlatformKind.Android)), Is.True);
            Assert.That(ScanFilter.Accept(headphones, Hrs, ScanFilter.IsClientSide(PlatformKind.Ios)), Is.True);
            Assert.That(ScanFilter.Accept(headphones, Hrs, ScanFilter.IsClientSide(PlatformKind.MacOs)), Is.True,
                "iOS/macOS: el servicio puede viajar en los overflow UUIDs");
        }

        [Test, Category("AC-BLE-03-01"), Ignore("NO VERIFICABLE → [Device] It. 10a/11 (búsqueda real en Windows y Android, S-1 opcional); [Apple] It. 10b")]
        public void RealPlatformScan()
        {
        }

        // AC-BLE-03-02: alcanza con que uno de los anunciados sea el buscado.
        [Test, Category("AC-BLE-03-02")]
        public void AnyAdvertisedServiceIsEnough()
        {
            Assert.That(ScanFilter.AdvertisesService(Result(), Hrs), Is.False);
            Assert.That(ScanFilter.AdvertisesService(Result(BleUuid.Parse("febe")), Hrs), Is.False);
            Assert.That(ScanFilter.AdvertisesService(Result(BleUuid.Parse("180d")), Hrs), Is.True);
            Assert.That(ScanFilter.AdvertisesService(Result(BleUuid.Parse("febe"), BleUuid.Parse("0000180d-0000-1000-8000-00805f9b34fb")), Hrs), Is.True);
            Assert.That(ScanFilter.AdvertisesService(Result(BleUuid.Parse("1826"), BleUuid.Parse("febe"), BleUuid.Parse("180D")), Hrs), Is.True);
        }

        // AC-BLE-03-04: todas las formas del mismo servicio pasan el filtro (comparación de 128 bits sin mayúsculas).
        [TestCase("180d", Category = "AC-BLE-03-04")]
        [TestCase("180D", Category = "AC-BLE-03-04")]
        [TestCase("0000180d", Category = "AC-BLE-03-04")]
        [TestCase("0000180d-0000-1000-8000-00805f9b34fb", Category = "AC-BLE-03-04")]
        [TestCase("0000180D-0000-1000-8000-00805F9B34FB", Category = "AC-BLE-03-04")]
        public void EveryFormOfTheServicePasses(string advertised)
        {
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse(advertised)), Hrs, clientSide: true), Is.True);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse(advertised)), HeartRateParser.Service, clientSide: true), Is.True);
        }

        // AC-BLE-03-05: la misma regla para el remo (FTMS): un pulsómetro no aparece en la lista del remo en Windows.
        [Test, Category("AC-BLE-03-05")]
        public void SameRuleForRower()
        {
            var ftms = FtmsUuids.Service;
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("1826")), ftms, clientSide: true), Is.True);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("180d")), ftms, clientSide: true), Is.False);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("180d"), BleUuid.Parse("1826")), ftms, clientSide: true), Is.True);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("00001826-0000-1000-8000-00805f9b34fb")), ftms, clientSide: true), Is.True);
            Assert.That(ScanFilter.Accept(Result(BleUuid.Parse("180d")), ftms, clientSide: false), Is.True, "sin filtro en la app pasa todo");
        }

        [Test, Category("AC-BLE-03-02")]
        public void ResultWithoutServiceListIsNotAnAdvertiser()
        {
            var noList = new BleScanResult("AA:BB:CC:DD:EE:FF", null, null, -50, null!);
            Assert.That(ScanFilter.AdvertisesService(noList, Hrs), Is.False);
            Assert.That(ScanFilter.Accept(noList, Hrs, clientSide: true), Is.False);
            Assert.That(ScanFilter.Accept(noList, Hrs, clientSide: false), Is.True);
        }

        // = _result(uuids) del test legacy (scan_filter_test.dart:6-19): mismo equipo, nombre vacío y RSSI -50.
        private static BleScanResult Result(params BleUuid[] uuids)
        {
            return new BleScanResult("AA:BB:CC:DD:EE:FF", null, "", -50, uuids);
        }
    }
}
