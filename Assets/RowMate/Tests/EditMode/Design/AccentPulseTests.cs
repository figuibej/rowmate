using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // AccentPulse = el destello del acento de una tarjeta al llegar un dato que cambió su texto (04-design-notes §5.2, DN-07).
    [TestFixture]
    public sealed class AccentPulseTests
    {
        [Test, Category("AC-BLE-11-06")]
        public void FromFullToSixtyIn300ms()
        {
            // Borde al 100 % en el cuadro de la lectura y de vuelta al 60 % de reposo antes de 300 ms (DV-02).
            Assert.That(AccentPulse.K(0), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(AccentPulse.K(150), Is.EqualTo(0.8).Within(1e-6), "ease.pulse es simétrica: a la mitad, la mitad");
            Assert.That(AccentPulse.K(299.999), Is.EqualTo(0.6).Within(1e-4));
            Assert.That(AccentPulse.K(300), Is.EqualTo(0.6));
            Assert.That(AccentPulse.K(10000), Is.EqualTo(0.6), "reposo");
            Assert.That(AccentPulse.K(-1), Is.EqualTo(0.6), "sin dato, reposo");
            Assert.That(AccentPulse.RestK, Is.EqualTo(0.6));
            Assert.That(AccentPulse.IsFinished(300), Is.True);
            Assert.That(AccentPulse.IsFinished(299), Is.False);
            Assert.That(MotionSpecs.AccentFlashMs, Is.LessThanOrEqualTo(300.0), "dura menos de 300 ms");
        }

        [Test, Category("AC-BLE-11-06")]
        public void MonotonicFade()
        {
            var previous = AccentPulse.K(0);
            for (var t = 10; t <= 300; t += 10)
            {
                var k = AccentPulse.K(t);
                Assert.That(k, Is.LessThanOrEqualTo(previous + 1e-12));
                Assert.That(k, Is.InRange(0.6, 1.0));
                previous = k;
            }
        }

        [Test, Category("AC-BLE-11-06")]
        public void GlowFollowsTheAccent()
        {
            // Brillo = 0,14·k: 0,14 en el dato y 0,084 en reposo.
            Assert.That(AccentPulse.GlowOpacity(0), Is.EqualTo(0.14).Within(1e-12));
            Assert.That(AccentPulse.GlowOpacity(5000), Is.EqualTo(0.084).Within(1e-12));
        }
    }
}
