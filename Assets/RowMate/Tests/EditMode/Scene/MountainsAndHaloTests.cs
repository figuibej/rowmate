using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;

namespace RowMate.Tests.EditMode.Scene
{
    // LKA · #3 (QA-V6a-12; AC-6B-04; 06b-agua-avanzada/02-tech-design §2.3; 00-arte-agua §10): los flancos de las cordilleras con más
    // contraste, menos bruma en la base y la envolvente izquierda más baja, sin cambiar DV-65 ni los colores del contrato. Las sondas
    // (≥ 1,3:1, ≤ 30 %) son de las capturas (WP-B).
    [TestFixture]
    public sealed class LakeArtTests
    {
        [Test, Category("AC-6B-04")]
        public void TheMountainsStandOut()
        {
            Assert.That(LakeArt.FlankToSun, Is.EqualTo(0.18), "era 0,12");
            Assert.That(LakeArt.FlankAway, Is.EqualTo(-0.16), "era −0,10");
            Assert.That(LakeArt.BaseFog, Is.EqualTo(0.10), "era 0,15");
            Assert.That(LakeArt.LeftEnvelopeSlope, Is.EqualTo(0.14), "era 0,20: en TH el bosque cercano deja ver el cordón");
            // Lo del contrato no cambia: el alto máximo (DV-65), los tonos del legacy y la profundidad.
            Assert.That(LakeArt.PeakScreenFraction, Is.EqualTo(0.12));
            Assert.That(LakeArt.MaxPeakDeg, Is.EqualTo(9));
            Assert.That(LakeArt.FarTone, Is.EqualTo(0xFF5C6E8Au));
            Assert.That(LakeArt.NearTone, Is.EqualTo(0xFF2F4A3Au));
            Assert.That(LakeArt.FarDepth, Is.EqualTo(1.0));
            Assert.That(ShaderArt.Constants()["kRmFlankToSun"], Is.EqualTo(0.18), "el shader Horizon lo lee de acá");
            Assert.That(ShaderArt.Constants()["kRmHorizonBaseFog"], Is.EqualTo(0.10));
        }
    }

    // SKA · #3: el brillo del cielo alrededor del sol sube de 0,35 a 0,45 con la hora dorada (g > 0,5), sin saltos; los halos del legacy no
    // cambian (AC-6B-04 c; la sonda de 2 y 6 diámetros es de CAP-03). La otra parte de la clase (el contrato del disco y del halo) está en
    // SkyContractTests.cs.
    public sealed partial class SkyArtTests
    {
        [Test, Category("AC-6B-04")]
        public void TheGoldenGlowIsStronger()
        {
            Assert.That(SkyArt.GlowStrengthAt(0), Is.EqualTo(0.35), "a las 12 h no cambia");
            Assert.That(SkyArt.GlowStrengthAt(0.5), Is.EqualTo(0.35));
            Assert.That(SkyArt.GlowStrengthAt(1), Is.EqualTo(0.45).Within(1e-12), "a las 6 y a las 18 h (g ≈ 1)");
            var last = SkyArt.GlowStrengthAt(0);
            for (var g = 0.0; g <= 1.0; g += 0.01)
            {
                var s = SkyArt.GlowStrengthAt(g);
                Assert.That(s, Is.GreaterThanOrEqualTo(last));
                Assert.That(s - last, Is.LessThan(0.005), "sin saltos");
                last = s;
            }

            Assert.That(ShaderArt.MaterialDefaults()["Sky._RmGlowParams"][3], Is.EqualTo(0.10).Within(1e-12), "el extra dorado en el material");
            Assert.That(ShaderArt.Constants()["kRmGlowGoldenFrom"], Is.EqualTo(0.5));
            Assert.That(SkyArt.HaloAlphas, Is.EqualTo(new[] { 0.7, 0.25, 0 }), "los halos del legacy no cambian");
            Assert.That(SkyArt.HaloScale, Is.EqualTo(4));
        }
    }
}
