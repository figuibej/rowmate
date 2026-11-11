using NUnit.Framework;
using RowMate.Core.Text;

namespace RowMate.Tests.EditMode.Text
{
    // LT · El tracking de los rótulos del HUD por contenido (It. 15, #76, AC-D15-24, DT15-10; arte A15-07): 0,04 em con alguna
    // minúscula, 0,08 em todo en mayúsculas, igual en es y en.
    [TestFixture]
    public sealed class LabelTrackingTests
    {
        [TestCase("Split 500m", true)]
        [TestCase("Vatios", true)]
        [TestCase("Watts", true)]
        [TestCase("kcal", true)]
        [TestCase("SPM", false)]
        [TestCase("BPM", false)]
        [TestCase("EN MARCHA", false)]
        [TestCase("500", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        [TestCase("PAUSADO·ñ", true)]
        [Category("AC-D15-24")]
        public void ByContent(string? text, bool mixed)
        {
            Assert.That(LabelTracking.HasLowercase(text), Is.EqualTo(mixed));
            Assert.That(LabelTracking.TrackingEm(text), Is.EqualTo(mixed ? 0.04 : 0.08));
        }

        [Test, Category("AC-D15-24")]
        public void TheModifierClass()
        {
            Assert.That(LabelTracking.MixedClass, Is.EqualTo("rm-hud-card__label--mixed"));
            Assert.That(LabelTracking.MixedEm, Is.EqualTo(0.04));
            Assert.That(LabelTracking.UpperEm, Is.EqualTo(0.08));
        }
    }
}
