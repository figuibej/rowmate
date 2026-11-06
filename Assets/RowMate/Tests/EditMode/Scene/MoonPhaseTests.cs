using System;
using NUnit.Framework;
using RowMate.Core.Scene;
using RowMate.Core.Scene.Art;

namespace RowMate.Tests.EditMode.Scene
{
    // MOO · La fase de la luna (06a-escena-lago/02-tech-design §2.6; AC-SCN-10-07; DV-57; 00-deuda-diseno §4.3).
    [TestFixture]
    public sealed class MoonPhaseTests
    {
        [Test, Category("AC-SCN-10-07")]
        public void TheModel()
        {
            Assert.That(MoonPhase.SynodicDays, Is.EqualTo(29.530588853));
            Assert.That(MoonPhase.ReferenceNewMoon, Is.EqualTo(new DateTimeOffset(2000, 1, 6, 18, 14, 0, TimeSpan.Zero)));
            var reference = MoonPhase.At(MoonPhase.ReferenceNewMoon);
            Assert.That(reference.AgeDays, Is.EqualTo(0).Within(1e-9));
            Assert.That(reference.Illumination, Is.EqualTo(0).Within(1e-12));
        }

        // Los tres vectores del Designer (moon.py), con ± 1 d contra las fases reales.
        [TestCase(2024, 1, 25, 17, 54, true, Category = "AC-SCN-10-07")]
        [TestCase(2024, 4, 8, 18, 21, false, Category = "AC-SCN-10-07")]
        [TestCase(2025, 1, 13, 22, 27, true, Category = "AC-SCN-10-07")]
        public void RealPhasesWithinOneDay(int year, int month, int day, int hour, int minute, bool full)
        {
            var p = MoonPhase.At(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero));
            var expectedAge = full ? MoonPhase.SynodicDays / 2 : 0;
            var error = Math.Abs(p.AgeDays - expectedAge);
            error = Math.Min(error, MoonPhase.SynodicDays - error);
            Assert.That(error, Is.LessThanOrEqualTo(1.0));
            Assert.That(p.Illumination, full ? (NUnit.Framework.Constraints.IResolveConstraint)Is.GreaterThan(0.99) : Is.LessThan(0.01));
        }

        [Test, Category("AC-SCN-10-07")]
        public void TheTestDateIsAWaningGibbousAt94Percent()
        {
            // Reloj de prueba: 2026-10-28 13:00 UTC (§0.4).
            var p = MoonPhase.At(new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero));
            Assert.That(p.Illumination, Is.EqualTo(0.94).Within(0.02));
            Assert.That(p.IsWaning, Is.True, "menguante: la parte oscura a la derecha en el norte");
            Assert.That(p.AgeDays, Is.GreaterThan(MoonPhase.SynodicDays / 2));
        }

        [TestCase(null, false, Category = "AC-SCN-10-07")]
        [TestCase("", false, Category = "AC-SCN-10-07")]
        [TestCase("AR", true, Category = "AC-SCN-10-07")]
        [TestCase("ar", true, Category = "AC-SCN-10-07")]
        [TestCase("UY", true, Category = "AC-SCN-10-07")]
        [TestCase("ZA", true, Category = "AC-SCN-10-07")]
        [TestCase("AU", true, Category = "AC-SCN-10-07")]
        [TestCase("ES", false, Category = "AC-SCN-10-07")]
        [TestCase("US", false, Category = "AC-SCN-10-07")]
        [TestCase("MX", false, Category = "AC-SCN-10-07")]
        public void TheHemisphere(string? region, bool south)
        {
            // Sin región, el norte (también en las pruebas); con una del sur, el lado iluminado espejado.
            Assert.That(MoonPhase.IsSouthernRegion(region), Is.EqualTo(south));
        }

        [TestCase(0.0, 0.4, Category = "AC-SCN-10-07")]
        [TestCase(0.5, 0.7, Category = "AC-SCN-10-07")]
        [TestCase(1.0, 1.0, Category = "AC-SCN-10-07")]
        public void TheHaloFadesWithLessLitMoon(double illumination, double factor)
        {
            Assert.That(MoonPhase.HaloFactor(illumination), Is.EqualTo(factor).Within(1e-12));
            Assert.That(CelestialPlacement.HaloFactor(true, illumination), Is.EqualTo(factor).Within(1e-12));
            Assert.That(CelestialPlacement.HaloFactor(false, illumination), Is.EqualTo(1), "el del sol, a pleno");
        }

        [Test, Category("AC-SCN-10-07")]
        public void IlluminationIsSmoothThroughTheMonth()
        {
            var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var prev = MoonPhase.At(start);
            for (var h = 1; h <= 24 * 60; h++)
            {
                var p = MoonPhase.At(start.AddHours(h));
                Assert.That(Math.Abs(p.Illumination - prev.Illumination), Is.LessThan(0.03));
                Assert.That(p.Illumination, Is.InRange(0.0, 1.0));
                prev = p;
            }

            Assert.That(SkyArt.MoonHaloBase + SkyArt.MoonHaloPerIllumination, Is.EqualTo(1.0));
        }
    }
}
