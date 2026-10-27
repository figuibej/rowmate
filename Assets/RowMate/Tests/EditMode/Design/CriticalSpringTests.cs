using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // CriticalSpring = resorte críticamente amortiguado (ζ = 1, ANIMATION_BIBLE §1.2): píldora de la barra y escala de FitBox.
    [TestFixture]
    public sealed class CriticalSpringTests
    {
        [TestCase(22.0, 0.26)]
        [TestCase(12.0, 0.49)]
        [TestCase(6.0, 0.97)]
        public void ReachesNinetyEightPercentAtTheBibleTime(double omega, double seconds)
        {
            // spring.snappy 22 → ≈ 0,26 s; spring.smooth 12 → ≈ 0,49 s; spring.gentle 6 → ≈ 0,97 s (desde el reposo).
            var spring = new CriticalSpring(0);
            var time = 0.0;
            while (spring.Position < 0.98)
            {
                spring.Step(1.0 / 600, 1, omega);
                time += 1.0 / 600;
            }

            Assert.That(time, Is.EqualTo(seconds).Within(0.02));
        }

        [Test]
        public void NoOvershootFromRest()
        {
            var spring = new CriticalSpring(0);
            for (var i = 0; i < 600; i++)
            {
                spring.Step(1.0 / 60, 100, MotionSpecs.PillOmega);
                Assert.That(spring.Position, Is.LessThanOrEqualTo(100 + 1e-9), "sin rebote");
            }

            Assert.That(spring.IsSettled(100), Is.True);
        }

        [Test]
        public void IndependentOfTheFrameRate()
        {
            // Solución exacta: un paso de 0,1 s = seis de 1/60 s = cien de 1/1000 s.
            var one = new CriticalSpring(0, 3);
            one.Step(0.1, 10, 12);
            var six = new CriticalSpring(0, 3);
            for (var i = 0; i < 6; i++)
            {
                six.Step(0.1 / 6, 10, 12);
            }

            var hundred = new CriticalSpring(0, 3);
            for (var i = 0; i < 100; i++)
            {
                hundred.Step(0.001, 10, 12);
            }

            Assert.That(six.Position, Is.EqualTo(one.Position).Within(1e-9));
            Assert.That(six.Velocity, Is.EqualTo(one.Velocity).Within(1e-9));
            Assert.That(hundred.Position, Is.EqualTo(one.Position).Within(1e-9));
        }

        [Test]
        public void KeepsItsVelocityWhenTheTargetMoves()
        {
            // Un segundo toque a mitad de camino no frena la píldora en seco (04-design-notes §5.4).
            var spring = new CriticalSpring(0);
            for (var i = 0; i < 6; i++)
            {
                spring.Step(1.0 / 60, 100, MotionSpecs.PillOmega);
            }

            var velocity = spring.Velocity;
            Assert.That(velocity, Is.GreaterThan(0));
            spring.Step(1.0 / 60, 300, MotionSpecs.PillOmega);
            Assert.That(spring.Velocity, Is.GreaterThan(velocity), "sigue desde su velocidad hacia el destino nuevo");
        }

        [Test]
        public void InvalidStepsAndSnap()
        {
            var spring = new CriticalSpring(5, 2);
            spring.Step(0, 10, 12);
            spring.Step(-1, 10, 12);
            spring.Step(0.1, 10, 0);
            Assert.That(spring.Position, Is.EqualTo(5.0));
            Assert.That(spring.Velocity, Is.EqualTo(2.0));
            spring.SnapTo(10);
            Assert.That(spring.Position, Is.EqualTo(10.0));
            Assert.That(spring.Velocity, Is.EqualTo(0.0));
            Assert.That(MotionSpecs.PillOmega, Is.EqualTo(22.0));
            Assert.That(MotionSpecs.FitScaleOmega, Is.EqualTo(12.0));
            Assert.That(Math.Abs(spring.Position - 10), Is.LessThan(1e-12));
        }
    }
}
