using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // IM · El seno propio del ícono (It. 14, DT14-01, QA-G14-05): contra Math.Sin con tolerancia 1e-9 en todo el rango que usa la
    // geometría, y valores fijos bit a bit (solo + y * en double: los mismos en dotnet, Mono y el CI).
    [TestFixture]
    public sealed class IconMathTests
    {
        [Test, Category("AC-APP-16-08")]
        public void MatchesTheLibraryWithinOneNanounit()
        {
            var worst = 0.0;
            for (var i = -40000; i <= 40000; i++)
            {
                var x = i * 0.0025; // −100 a 100
                worst = Math.Max(worst, Math.Abs(IconMath.Sin(x) - Math.Sin(x)));
            }

            Assert.That(worst, Is.LessThan(1e-9));
        }

        [Test, Category("AC-APP-16-08")]
        public void FixedValuesBitForBit()
        {
            Assert.That(IconMath.Sin(0), Is.EqualTo(0.0));
            Assert.That(IconMath.Sin(IconMath.HalfPi), Is.EqualTo(1.0).Within(1e-11));
            // Bit a bit (el mismo en dotnet, Mono y el CI): sin(1) y sin(2π·0,3).
            Assert.That(BitConverter.DoubleToInt64Bits(IconMath.Sin(1.0)).ToString("x16"), Is.EqualTo("3feaed548f090cd5"));
            Assert.That(BitConverter.DoubleToInt64Bits(IconMath.Sin(IconMath.TwoPi * 0.3)).ToString("x16"), Is.EqualTo("3fee6f0e13445037"));
            // Simetría exacta: sin(−x) = −sin(x) con la misma reducción.
            for (var i = 1; i < 200; i++)
            {
                var x = i * 0.137;
                Assert.That(IconMath.Sin(-x), Is.EqualTo(-IconMath.Sin(x)), "x = " + x);
            }
        }

        [Test, Category("AC-APP-16-08")]
        public void SmoothStepAndClamp()
        {
            Assert.That(IconMath.SmoothStep(-1), Is.EqualTo(0));
            Assert.That(IconMath.SmoothStep(0.5), Is.EqualTo(0.5));
            Assert.That(IconMath.SmoothStep(2), Is.EqualTo(1));
            Assert.That(IconMath.Clamp01(-0.1), Is.EqualTo(0));
            Assert.That(IconMath.Clamp01(1.1), Is.EqualTo(1));
        }
    }
}
