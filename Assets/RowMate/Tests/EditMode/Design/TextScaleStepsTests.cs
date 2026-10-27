using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // TextScaleSteps = la escala de letra del sistema cuantizada a 0,85 / 1 / 1,15 / 1,3, con tope 1,3 (AC-APP-05-05, P-03).
    [TestFixture]
    public sealed class TextScaleStepsTests
    {
        [TestCase(1.0, 1.0, Category = "AC-APP-05-05")]
        [TestCase(0.85, 0.85, Category = "AC-APP-05-05")]
        [TestCase(1.15, 1.15, Category = "AC-APP-05-05")]
        [TestCase(1.3, 1.3, Category = "AC-APP-05-05")]
        [TestCase(1.1500000357627869, 1.15, Category = "AC-APP-05-05")]
        [TestCase(1.5, 1.3, Category = "AC-APP-05-05")]
        [TestCase(2.0, 1.3, Category = "AC-APP-05-05")]
        [TestCase(2.25, 1.3, Category = "AC-APP-05-05")]
        [TestCase(0.5, 0.85, Category = "AC-APP-05-05")]
        [TestCase(1.05, 1.0, Category = "AC-APP-05-05")]
        [TestCase(1.075, 1.15, Category = "AC-APP-05-05")]
        [TestCase(1.25, 1.3, Category = "AC-APP-05-05")]
        [TestCase(0.0, 1.0, Category = "AC-APP-05-05")]
        [TestCase(-1.0, 1.0, Category = "AC-APP-05-05")]
        [TestCase(double.NaN, 1.0, Category = "AC-APP-05-05")]
        public void QuantizeToTheNearestStepWithTheCap(double system, double expected)
        {
            // Más grande que 1,3 → 1,3 (no más); empate → el más grande; inválido → 1.
            Assert.That(TextScaleSteps.Quantize(system), Is.EqualTo(expected));
        }

        [Test, Category("AC-APP-05-05")]
        public void ClassNames()
        {
            Assert.That(TextScaleSteps.ClassName(0.85), Is.EqualTo("rm-text-scale-085"));
            Assert.That(TextScaleSteps.ClassName(1.0), Is.EqualTo("rm-text-scale-100"));
            Assert.That(TextScaleSteps.ClassName(1.15), Is.EqualTo("rm-text-scale-115"));
            Assert.That(TextScaleSteps.ClassName(1.3), Is.EqualTo("rm-text-scale-130"));
            Assert.That(TextScaleSteps.ClassName(TextScaleSteps.Quantize(1.7)), Is.EqualTo("rm-text-scale-130"));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => TextScaleSteps.ClassName(1.2), "solo escalones");
        }

        [Test, Category("AC-APP-05-05")]
        public void FourStepsInOrder()
        {
            Assert.That(TextScaleSteps.Steps, Is.EqualTo(new[] { 0.85, 1.0, 1.15, 1.3 }));
            Assert.That(TextScaleSteps.Max, Is.EqualTo(1.3), "P-03");
        }
    }
}
