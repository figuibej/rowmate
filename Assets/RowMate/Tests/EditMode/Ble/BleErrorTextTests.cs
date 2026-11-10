using System;
using NUnit.Framework;
using RowMate.App.Device;
using RowMate.Core.Ble;
using RowMate.Tests.EditMode.Localization;

namespace RowMate.Tests.EditMode.Ble
{
    // BE · El detalle de la falla sin el prefijo de la operación (It. 14, #44, AC-DEBT-04; DT14-27).
    [TestFixture]
    public sealed class BleErrorTextTests
    {
        private static readonly DeviceState Empty = new DeviceState(BleStatus.Disconnected, BleAdapterState.On, RowMate.Core.Models.RowingData.Empty,
            new BleScanResult[0], null, null);

        [Test, Category("AC-DEBT-04")]
        public void DropsTheOperationPrefix()
        {
            Assert.That(BleErrorText.ForDisplay("scan: error 2"), Is.EqualTo("error 2"));
            Assert.That(BleErrorText.ForDisplay("connect: timed out after 15 s"), Is.EqualTo("timed out after 15 s"));
            Assert.That(BleErrorText.ForDisplay("discover: no FTMS"), Is.EqualTo("no FTMS"));
            Assert.That(BleErrorText.ForDisplay("subscribe: denied"), Is.EqualTo("denied"));
            Assert.That(BleErrorText.ForDisplay("setNotify: not connected"), Is.EqualTo("not connected"));
            Assert.That(BleErrorText.ForDisplay(new BleException("scan", "error 2", 2).Message), Is.EqualTo("error 2"));
        }

        [Test, Category("AC-DEBT-04")]
        public void LeavesOtherTextsAlone()
        {
            Assert.That(BleErrorText.ForDisplay("error 2"), Is.EqualTo("error 2"));
            Assert.That(BleErrorText.ForDisplay("Scan: x"), Is.EqualTo("Scan: x"), "solo las operaciones de los servicios, en minúsculas");
            Assert.That(BleErrorText.ForDisplay("scanner: x"), Is.EqualTo("scanner: x"));
            Assert.That(BleErrorText.ForDisplay("scan: scan: x"), Is.EqualTo("scan: x"), "un solo prefijo");
            Assert.Throws<ArgumentNullException>(() => BleErrorText.ForDisplay(null!));
        }

        [Test, Category("AC-DEBT-04")]
        public void BannerShowsOnlyTheSystemDetail()
        {
            var scan = Empty with { Error = new BleException("scan", "error 2", 2), ErrorKind = DeviceErrorKind.Scan };
            Assert.That(DeviceScreenRules.DisplayErrorText(scan, LocalizationFixture.Es), Is.EqualTo("Error al escanear: error 2"));
            Assert.That(DeviceScreenRules.DisplayErrorText(scan, LocalizationFixture.En), Is.EqualTo("Scan error: error 2"));
            Assert.That(DeviceScreenRules.ErrorText(scan, LocalizationFixture.Es), Is.EqualTo("Error al escanear: scan: error 2"), "ErrorText no cambia (paridad)");
            var connect = Empty with { Error = new BleException("connect", "timed out after 15 s"), ErrorKind = DeviceErrorKind.Connect };
            Assert.That(DeviceScreenRules.DisplayErrorText(connect, LocalizationFixture.Es), Is.EqualTo("Error al conectar: timed out after 15 s"));
            Assert.That(DeviceScreenRules.DisplayErrorText(Empty, LocalizationFixture.Es), Is.Null);
        }
    }
}
