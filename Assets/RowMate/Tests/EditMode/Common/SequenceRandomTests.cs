using System;
using NUnit.Framework;
using RowMate.Core.Common;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Common
{
    // AC-BASE-07: el azar sale de una fuente reemplazable (IRandom). SequenceRandom fija los valores; sin valores,
    // cualquier llamada falla (así se prueba que "sin ruido" no se consume azar).
    [TestFixture]
    public sealed class SequenceRandomTests
    {
        [Test, Category("AC-BASE-07")]
        public void ReturnsTheValuesInOrder()
        {
            IRandom random = new SequenceRandom(0.25, 0.5, 0.75);
            Assert.That(random.NextDouble(), Is.EqualTo(0.25));
            Assert.That(random.NextDouble(), Is.EqualTo(0.5));
            Assert.That(random.NextDouble(), Is.EqualTo(0.75));
            Assert.That(((SequenceRandom)random).Calls, Is.EqualTo(3));
            Assert.That(((SequenceRandom)random).Remaining, Is.EqualTo(0));
        }

        [Test, Category("AC-BASE-07")]
        public void FailsWhenTheSequenceIsExhausted()
        {
            var random = new SequenceRandom(0.1);
            random.NextDouble();
            Assert.Throws<InvalidOperationException>(() => random.NextDouble());
        }

        [Test, Category("AC-BASE-07")]
        public void EmptySequenceFailsOnTheFirstCall()
        {
            var random = new SequenceRandom();
            Assert.Throws<InvalidOperationException>(() => random.NextDouble());
            Assert.That(random.Calls, Is.EqualTo(0));
        }

        [Test, Category("AC-BASE-07")]
        public void SystemRandomWithASeedIsRepeatableAndInRange()
        {
            var a = new SystemRandom(42);
            var b = new SystemRandom(42);
            for (var i = 0; i < 100; i++)
            {
                var x = a.NextDouble();
                Assert.That(x, Is.EqualTo(b.NextDouble()));
                Assert.That(x, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }

            var unseeded = new SystemRandom();
            for (var i = 0; i < 100; i++)
            {
                Assert.That(unseeded.NextDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }
    }
}
