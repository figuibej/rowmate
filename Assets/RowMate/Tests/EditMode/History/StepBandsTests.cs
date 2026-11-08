using System.Linq;
using NUnit.Framework;
using RowMate.Core.Design;
using RowMate.Core.History;
using RowMate.Core.Models;

namespace RowMate.Tests.EditMode.History
{
    // BA · Las franjas (AC-HIS-14-01, -02; sd:313-332, :410-418): el bucle del legacy, null → work, los puntos sin índice no
    // cortan y un paso sin puntos no tiene franja; la paleta es la de stepColor.
    [TestFixture]
    public sealed class StepBandsTests
    {
        private static DataPoint P(int t, int? step, string? type)
        {
            return new DataPoint(1, t) { StepIndex = step, StepTypeName = type };
        }

        [Test, Category("AC-HIS-14-01")]
        public void LegacyLoop()
        {
            var bands = StepBands.Build(new[]
            {
                P(5, 0, "warmup"), P(10, 0, "warmup"), P(15, null, null), P(20, 2, null), P(25, 2, "rest"), P(30, 3, "sprint"), P(35, null, null),
            });
            Assert.That(bands.Select(b => b.StepIndex), Is.EqualTo(new[] { 0, 2, 3 }), "el paso 1 sin puntos no tiene franja");
            Assert.That(bands.Select(b => b.TypeName), Is.EqualTo(new[] { "warmup", "work", "sprint" }), "el tipo del punto que abre; null → work");
            Assert.That(bands.Select(b => b.Start), Is.EqualTo(new double[] { 5, 20, 30 }));
            Assert.That(bands.Select(b => b.End), Is.EqualTo(new double[] { 20, 30, 35 }), "sin índice no corta; la última cierra en el último punto");
        }

        [Test, Category("AC-HIS-14-01")]
        public void NoIndexNoBands()
        {
            Assert.That(StepBands.Build(new[] { P(5, null, null), P(10, null, null) }), Is.Empty);
            Assert.That(StepBands.Build(new DataPoint[0]), Is.Empty);
            Assert.That(StepBands.Build(new[] { P(5, 4, "work") }).Single().End, Is.EqualTo(5), "un solo punto: franja de largo 0");
        }

        [TestCase("work", "#EF476F")]
        [TestCase("rest", "#06D6A0")]
        [TestCase("warmup", "#FFD166")]
        [TestCase("cooldown", "#118AB2")]
        [TestCase("sprint", "#607D8B")]
        [TestCase(null, "#607D8B")]
        [Category("AC-HIS-14-02")]
        public void Palette(string? type, string hex)
        {
            Assert.That(StepBandPalette.For(type).Rgb.ToHex(), Is.EqualTo(hex));
            Assert.That(ChartSpecs.BandAlpha, Is.EqualTo(0.10));
        }
    }
}
