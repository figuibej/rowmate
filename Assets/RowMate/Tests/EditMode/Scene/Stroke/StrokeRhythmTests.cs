using NUnit.Framework;
using RowMate.Core.Scene.Stroke;

namespace RowMate.Tests.EditMode.Scene.Stroke
{
    // SRH · El ritmo según el SPM (Q-70; AC-SCN-04-05; ANIMATION_BIBLE §3.2): la tabla de la spec (± 0,001), los topes, la
    // monotonía y la ida y vuelta φ ↔ c.
    [TestFixture]
    public sealed class StrokeRhythmTests
    {
        [TestCase(12, 0.230, Category = "AC-SCN-04-05")]
        [TestCase(18, 0.315, Category = "AC-SCN-04-05")]
        [TestCase(20, 0.337, Category = "AC-SCN-04-05")]
        [TestCase(24, 0.373, Category = "AC-SCN-04-05")]
        [TestCase(28, 0.399, Category = "AC-SCN-04-05")]
        [TestCase(30, 0.409, Category = "AC-SCN-04-05")]
        [TestCase(32, 0.415, Category = "AC-SCN-04-05")]
        [TestCase(36, 0.420, Category = "AC-SCN-04-05")]
        public void TheRatioOfTheTable(double spm, double ratio)
        {
            Assert.That(StrokeRhythm.DriveRatio(spm), Is.EqualTo(ratio).Within(0.001));
        }

        [TestCase(20, 1.011, 1.989, Category = "AC-SCN-04-05")]
        [TestCase(30, 0.817, 1.183, Category = "AC-SCN-04-05")]
        public void DriveAndRecoveryOfSuaveAndFuerte(double spm, double drive, double recovery)
        {
            var cycle = 60 / spm;
            var r = StrokeRhythm.DriveRatio(spm);
            Assert.That(r * cycle, Is.EqualTo(drive).Within(0.001));
            Assert.That((1 - r) * cycle, Is.EqualTo(recovery).Within(0.001));
        }

        [Test, Category("AC-SCN-04-05")]
        public void TheCapsHold()
        {
            Assert.That(StrokeRhythm.DriveRatio(4), Is.EqualTo(0.22));
            Assert.That(StrokeRhythm.DriveRatio(80), Is.EqualTo(0.48));
            Assert.That(StrokeRhythm.DriveSeconds(0), Is.EqualTo(1.15));
            Assert.That(StrokeRhythm.DriveSeconds(60), Is.EqualTo(0.60));
        }

        [Test, Category("AC-SCN-04-05")]
        public void TheMappingIsContinuousMonotonicAndInvertible()
        {
            foreach (var r in new[] { 0.22, 0.30, 0.373, 0.409, 0.48 })
            {
                Assert.That(StrokeRhythm.ToCanonical(0, r), Is.EqualTo(0));
                Assert.That(StrokeRhythm.ToCanonical(r, r), Is.EqualTo(0.40).Within(1e-12));
                Assert.That(StrokeRhythm.ToCanonical(1, r), Is.EqualTo(1).Within(1e-12));
                var previous = -1.0;
                for (var i = 0; i <= 1000; i++)
                {
                    var phi = i / 1000.0;
                    var c = StrokeRhythm.ToCanonical(phi, r);
                    Assert.That(c, Is.GreaterThanOrEqualTo(previous), "monótona");
                    Assert.That(i == 0 || c - previous < 0.003, Is.True, "continua");
                    Assert.That(StrokeRhythm.FromCanonical(c, r), Is.EqualTo(phi).Within(1e-12), "ida y vuelta");
                    previous = c;
                }
            }
        }
    }
}
