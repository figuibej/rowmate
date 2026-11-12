using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Scene
{
    // SKA · Las proporciones del disco y del halo que son contrato (QA-6a-PAR-10; AC-SCN-11-05, AC-SCN-10-07; DV-63): halo = 4 × el
    // disco de cada astro; disco de la luna = 0,8 × el del sol (y su halo, 0,8 × el del sol); los alfas del halo 0,7 / 0,25 / 0.
    [TestFixture]
    public sealed partial class SkyArtTests
    {
        [Test, Category("AC-SCN-11-05")]
        public void HaloIsFourTimesTheDisk()
        {
            Assert.That(SkyArt.HaloScale, Is.EqualTo(4));
            Assert.That(CelestialPlacement.HaloRadiusRad(false), Is.EqualTo(4 * CelestialPlacement.DiskRadiusRad(false)).Within(1e-15));
            Assert.That(CelestialPlacement.HaloRadiusRad(true), Is.EqualTo(4 * CelestialPlacement.DiskRadiusRad(true)).Within(1e-15));
        }

        [Test, Category("AC-SCN-11-05"), Category("AC-SCN-10-07")]
        public void TheMoonIsEightTenthsOfTheSun()
        {
            Assert.That(SkyArt.MoonDiskScale, Is.EqualTo(0.8));
            Assert.That(CelestialPlacement.DiskRadiusRad(true), Is.EqualTo(0.8 * CelestialPlacement.DiskRadiusRad(false)).Within(1e-15));
            Assert.That(CelestialPlacement.HaloRadiusRad(true), Is.EqualTo(0.8 * CelestialPlacement.HaloRadiusRad(false)).Within(1e-15));
            Assert.That(SceneMath.RadToDeg(CelestialPlacement.DiskRadiusRad(true)), Is.EqualTo(1.28).Within(1e-12));
        }

        [Test, Category("AC-SCN-11-05")]
        public void TheHaloAlphasAreTheLegacyOnes()
        {
            Assert.That(SkyArt.HaloAlphas, Is.EqualTo(new[] { 0.7, 0.25, 0.0 }));
            Assert.That(CelestialMask.HaloAlpha(0), Is.EqualTo(0.7));
            Assert.That(CelestialMask.HaloAlpha(0.5), Is.EqualTo(0.25));
            Assert.That(CelestialMask.HaloAlpha(1), Is.EqualTo(0));
            Assert.That(CelestialMask.HaloAlpha(0.25), Is.EqualTo(0.475).Within(1e-12), "lineal entre las paradas");
        }

        [TestCase("V390", 21.8, Category = "AC-SCN-11-05")]
        [TestCase("H844", 21.8, Category = "AC-SCN-11-05")]
        [TestCase("TV", 23.0, Category = "AC-SCN-11-05")]
        [TestCase("TH", 23.0, Category = "AC-SCN-11-05")]
        [TestCase("W1280", 40.2, Category = "AC-SCN-11-05")]
        public void TheSunDiskInDp(string size, double diameter)
        {
            var cam = SceneSeeds.Camera(size);
            var d = 2 * cam.Focal * Math.Tan(CelestialPlacement.DiskRadiusRad(false));
            Assert.That(d, Is.EqualTo(diameter).Within(0.2));
            var halo = 2 * cam.Focal * Math.Tan(CelestialPlacement.HaloRadiusRad(false));
            if (size == "V390")
            {
                Assert.That(halo, Is.EqualTo(87).Within(1), "el halo a 6,4° (≈ 87 dp en V390)");
            }
        }

        [Test, Category("AC-SCN-11-05")]
        public void TheDiskShinesInHdr()
        {
            var noon = CelestialPlacement.Evaluate(12, 0.94, CelestialSpec.Default);
            var night = CelestialPlacement.Evaluate(22, 0.94, CelestialSpec.Default);
            Assert.That(CelestialPlacement.DiskHdr(noon), Is.EqualTo(8).Within(1e-12), "3 + 5·e");
            Assert.That(CelestialPlacement.DiskHdr(night), Is.EqualTo(1.6));
        }
    }

    // SKB · La curva del telón del cielo que copia el shader Sky (QA-6a-PAR-10; AC-SCN-11-02; DV-66, DN6a-09).
    [TestFixture]
    public sealed class SkyBackdropTests
    {
        [Test, Category("AC-SCN-11-02")]
        public void TheEndsAreExactlyThePalette()
        {
            Assert.That(SkyBackdrop.Gradient(0), Is.EqualTo(0.0), "skyHorizon en el horizonte");
            Assert.That(SkyBackdrop.Gradient(1), Is.EqualTo(1.0), "skyTop en el borde de arriba");
            var p = SceneTimeOfDay.PaletteFor(12);
            Assert.That(SkyBackdrop.Color(p, 0), Is.EqualTo(p.SkyHorizon));
            Assert.That(SkyBackdrop.Color(p, 1), Is.EqualTo(p.SkyTop));
        }

        [Test, Category("AC-SCN-11-02")]
        public void TheLightGathersNearTheHorizon()
        {
            Assert.That(SkyBackdrop.Gradient(0.25), Is.EqualTo(0.4375).Within(1e-15), "a un cuarto, el 44 %");
            Assert.That(SkyBackdrop.Gradient(0.5), Is.EqualTo(0.75).Within(1e-15));
            // Las sondas de contrato de 04-design-notes §10.4 (CAP-02, V390, 12 h): a media altura #2C70BE.
            var mid = SkyBackdrop.Color(SceneTimeOfDay.PaletteFor(12), 0.5);
            Assert.That(mid.ToHex(), Is.EqualTo("#2C70BE"));
            Assert.That(SkyBackdrop.Gradient(0.976), Is.EqualTo(0.9994).Within(0.0001));
        }

        [Test, Category("AC-SCN-11-02")]
        public void TheCurveNeverGoesBack()
        {
            var prev = SkyBackdrop.Gradient(0);
            for (var i = 1; i <= 10000; i++)
            {
                var g = SkyBackdrop.Gradient(i / 10000.0);
                Assert.That(g, Is.GreaterThanOrEqualTo(prev));
                prev = g;
            }
        }

        [Test, Category("AC-SCN-11-02")]
        public void TheFractionIsClamped()
        {
            Assert.That(SkyBackdrop.Fraction(-0.1, 0.889), Is.EqualTo(0));
            Assert.That(SkyBackdrop.Fraction(2, 0.889), Is.EqualTo(1));
            Assert.That(SkyBackdrop.Fraction(0.4445, 0.889), Is.EqualTo(0.5).Within(1e-12));
            Assert.That(SkyBackdrop.Gradient(-1), Is.EqualTo(0));
            Assert.That(SkyBackdrop.Gradient(2), Is.EqualTo(1));
        }
    }

    // CMK · Lo que tapa el astro a las estrellas (QA-G6aT-08, DT6a-17; AC-SCN-11-01): 0 adentro del disco, 1 − el alfa del halo
    // adentro del halo, 1 afuera; continua en el borde.
    [TestFixture]
    public sealed class CelestialMaskTests
    {
        private static readonly double Disk = CelestialPlacement.DiskRadiusRad(false);
        private static readonly double Halo = CelestialPlacement.HaloRadiusRad(false);

        [Test, Category("AC-SCN-11-01")]
        public void InsideTheDiskNoStar()
        {
            Assert.That(CelestialMask.StarAttenuation(0, Disk, Halo, 1), Is.EqualTo(0));
            Assert.That(CelestialMask.StarAttenuation(Disk * 0.5, Disk, Halo, 1), Is.EqualTo(0));
            Assert.That(CelestialMask.StarAttenuation(Disk - SceneMath.DegToRad(SkyArt.DiskEdgeDeg), Disk, Halo, 1), Is.EqualTo(0));
            // También del lado oscuro de la luna (la máscara es por el disco, no por la parte iluminada).
            var moonDisk = CelestialPlacement.DiskRadiusRad(true);
            Assert.That(CelestialMask.StarAttenuation(moonDisk * 0.3, moonDisk, CelestialPlacement.HaloRadiusRad(true), 0.4), Is.EqualTo(0));
        }

        [Test, Category("AC-SCN-11-01")]
        public void InsideTheHaloLikeTheLegacyHaloOnTop()
        {
            // A la mitad del radio del halo del sol, × 0,75 (el alfa 0,25 del legacy).
            Assert.That(CelestialMask.StarAttenuation(Halo * 0.5, Disk, Halo, 1), Is.EqualTo(0.75).Within(1e-12));
            Assert.That(CelestialMask.StarAttenuation(Halo * 0.5, Disk, Halo, 0.5), Is.EqualTo(1 - 0.25 * 0.5).Within(1e-12), "× la visibilidad");
            Assert.That(CelestialMask.StarAttenuation(Halo * 0.75, Disk, Halo, 1), Is.EqualTo(1 - 0.125).Within(1e-12));
        }

        [Test, Category("AC-SCN-11-01")]
        public void OutsideTheHaloUntouched()
        {
            Assert.That(CelestialMask.StarAttenuation(Halo, Disk, Halo, 1), Is.EqualTo(1));
            Assert.That(CelestialMask.StarAttenuation(Halo * 3, Disk, Halo, 1), Is.EqualTo(1));
        }

        [Test, Category("AC-SCN-11-01")]
        public void ContinuousAtTheEdges()
        {
            var prev = CelestialMask.StarAttenuation(0, Disk, Halo, 1);
            for (var i = 1; i <= 20000; i++)
            {
                var a = Halo * 1.1 * i / 20000;
                var v = CelestialMask.StarAttenuation(a, Disk, Halo, 1);
                Assert.That(Math.Abs(v - prev), Is.LessThan(0.01), "sin saltos en el borde del disco ni del halo");
                prev = v;
            }
        }
    }
}
