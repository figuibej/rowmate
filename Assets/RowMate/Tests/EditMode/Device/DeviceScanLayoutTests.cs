using NUnit.Framework;
using RowMate.App.Device;

namespace RowMate.Tests.EditMode.Device
{
    // La disposición de la vista de búsqueda (It. 11, A11-05, DT11-14; cierra m-06 de la 10a) en los cinco perfiles (dp).
    [TestFixture]
    public sealed class DeviceScanLayoutTests
    {
        [TestCase("TV", 411, 914, DeviceScanLayoutKind.Stacked, 0)]
        [TestCase("P360", 360, 640, DeviceScanLayoutKind.Stacked, 0)]
        [TestCase("W1280", 1280, 720, DeviceScanLayoutKind.Stacked, 0)]
        [TestCase("TH", 914, 411, DeviceScanLayoutKind.TwoColumns, 365.6)]
        [TestCase("L640", 640, 360, DeviceScanLayoutKind.SingleScroll, 0)]
        [Category("AC-HRM-13-13")]
        public void FiveProfiles(string profile, double width, double height, DeviceScanLayoutKind kind, double right)
        {
            var layout = DeviceScanLayout.For(width, height);
            Assert.That(layout.Kind, Is.EqualTo(kind), profile);
            Assert.That(layout.RightColumnDp, Is.EqualTo(right).Within(1e-9), profile);
        }

        // Los bordes: 560 de alto ya es normal; 840 de ancho ya es de dos columnas; la columna derecha entre 360 y 440.
        [Test, Category("AC-HRM-13-13")]
        public void Edges()
        {
            Assert.That(DeviceScanLayout.For(900, 560).Kind, Is.EqualTo(DeviceScanLayoutKind.Stacked));
            Assert.That(DeviceScanLayout.For(900, 559.9).Kind, Is.EqualTo(DeviceScanLayoutKind.TwoColumns));
            Assert.That(DeviceScanLayout.For(840, 400).Kind, Is.EqualTo(DeviceScanLayoutKind.TwoColumns));
            Assert.That(DeviceScanLayout.For(839.9, 400).Kind, Is.EqualTo(DeviceScanLayoutKind.SingleScroll));
            Assert.That(DeviceScanLayout.For(840, 400).RightColumnDp, Is.EqualTo(360), "0,4 × 840 = 336 → 360");
            Assert.That(DeviceScanLayout.For(1400, 400).RightColumnDp, Is.EqualTo(440), "0,4 × 1400 = 560 → 440");
            Assert.That(DeviceScanLayout.For(1000, 400).RightColumnDp, Is.EqualTo(400).Within(1e-9));
            Assert.That(DeviceScanLayout.For(1000, 400), Is.EqualTo(new DeviceScanLayout(DeviceScanLayoutKind.TwoColumns, 400)));
        }
    }
}
