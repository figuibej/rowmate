using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Platform.Display;

namespace RowMate.Tests.UnityEditMode
{
    // GTI · El nivel inicial (06a-escena-lago/02-tech-design §4.2; ADR 018; ronda 1 de la verificación, QA-V6a-REN-09): Alto en escritorio
    // con GPU dedicada, Medio con los gráficos integrados conocidos (y en la gama media de teléfono), Bajo con las GPU chicas o poca
    // memoria. Los nombres son de modelos de catálogo, armados acá.
    public sealed class GraphicsTierTests
    {
        [Test, Category("AC-SCN-01-13")]
        public void IntegratedDesktopGraphicsStartInMedium()
        {
            foreach (var name in new[] { "Intel(R) " + "UHD Graphics 620", "Intel(R) " + "Iris(R) Xe Graphics", "AMD Radeon(TM) " + "Graphics",
                         "Microsoft Basic Render " + "Driver" })
            {
                Assert.That(GraphicsTier.Choose(false, name, 16000), Is.EqualTo(QualityTier.Medium), name);
            }
        }

        [Test, Category("AC-SCN-01-13")]
        public void DedicatedDesktopGraphicsStartInHigh()
        {
            foreach (var name in new[] { "NVIDIA GeForce " + "RTX 3060", "AMD Radeon " + "RX 6600", "Intel(R) Arc(TM) " + "A770 Graphics", "Apple " + "M2", "" })
            {
                Assert.That(GraphicsTier.Choose(false, name, 16000), Is.EqualTo(QualityTier.High), name);
            }
        }

        [Test, Category("AC-SCN-01-13")]
        public void PhonesKeepTheirTiers()
        {
            Assert.That(GraphicsTier.Choose(true, "Mali-G52 " + "MC2", 6000), Is.EqualTo(QualityTier.Low));
            Assert.That(GraphicsTier.Choose(true, "Adreno (TM) " + "642L", 3000), Is.EqualTo(QualityTier.Low), "menos de 4 GB");
            Assert.That(GraphicsTier.Choose(true, "Mali-G68 " + "MC4", 6000), Is.EqualTo(QualityTier.Medium));
        }
    }
}
