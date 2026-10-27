using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // Easing = cubic-bezier de los EasingToken (ANIMATION_BIBLE §1.2) para el motion por código (D-14).
    [TestFixture]
    public sealed class EasingTests
    {
        private static readonly EasingToken[] Curves =
        {
            Tokens.EaseEnter, Tokens.EaseExit, Tokens.EaseMove, Tokens.EasePulse, Tokens.EaseEmphasis, Tokens.EaseLinear,
        };

        [Test]
        public void EndpointsAndClamping()
        {
            foreach (var curve in Curves)
            {
                Assert.That(Easing.Evaluate(curve, 0), Is.EqualTo(0.0), curve.Name);
                Assert.That(Easing.Evaluate(curve, 1), Is.EqualTo(1.0), curve.Name);
                Assert.That(Easing.Evaluate(curve, -0.5), Is.EqualTo(0.0), curve.Name + " antes de empezar");
                Assert.That(Easing.Evaluate(curve, 3), Is.EqualTo(1.0), curve.Name + " después de terminar");
                Assert.That(Easing.Evaluate(curve, double.NaN), Is.EqualTo(0.0));
            }
        }

        [Test]
        public void LinearIsIdentity()
        {
            for (var i = 0; i <= 10; i++)
            {
                Assert.That(Easing.Evaluate(Tokens.EaseLinear, i / 10.0), Is.EqualTo(i / 10.0).Within(1e-12));
            }
        }

        [Test]
        public void SymmetricCurvesPassThroughTheMiddle()
        {
            // ease.move (0.65, 0, 0.35, 1) y ease.pulse (0.37, 0, 0.63, 1) son simétricas respecto de (0,5; 0,5).
            Assert.That(Easing.Evaluate(Tokens.EaseMove, 0.5), Is.EqualTo(0.5).Within(1e-6));
            Assert.That(Easing.Evaluate(Tokens.EasePulse, 0.5), Is.EqualTo(0.5).Within(1e-6));
            for (var i = 1; i < 10; i++)
            {
                var t = i / 10.0;
                Assert.That(Easing.Evaluate(Tokens.EaseMove, t) + Easing.Evaluate(Tokens.EaseMove, 1 - t), Is.EqualTo(1.0).Within(1e-6));
            }
        }

        [Test]
        public void MatchesABisectionReference()
        {
            foreach (var curve in Curves)
            {
                for (var i = 1; i < 50; i++)
                {
                    var t = i / 50.0;
                    Assert.That(Easing.Evaluate(curve, t), Is.EqualTo(Reference(curve, t)).Within(1e-6), curve.Name + " t=" + i + "/50");
                }
            }
        }

        [Test]
        public void EnterIsFastAtTheStartAndExitAtTheEnd()
        {
            // ease.enter (ease-out-cubic aproximada) va adelante; ease.exit (ease-in-cubic) atrás.
            Assert.That(Easing.Evaluate(Tokens.EaseEnter, 0.25), Is.GreaterThan(0.5));
            Assert.That(Easing.Evaluate(Tokens.EaseExit, 0.75), Is.LessThan(0.5));
            Assert.That(Easing.Evaluate(Tokens.EaseEnter, 0.5), Is.EqualTo(0.875).Within(0.03), "≈ 1 − (1 − t)³");
        }

        [Test]
        public void MonotonicExceptEmphasis()
        {
            foreach (var curve in new[] { Tokens.EaseEnter, Tokens.EaseExit, Tokens.EaseMove, Tokens.EasePulse, Tokens.EaseLinear })
            {
                var previous = 0.0;
                for (var i = 1; i <= 200; i++)
                {
                    var value = Easing.Evaluate(curve, i / 200.0);
                    Assert.That(value, Is.GreaterThanOrEqualTo(previous - 1e-9), curve.Name);
                    previous = value;
                }
            }

            var overshoot = 0.0;
            for (var i = 1; i < 100; i++)
            {
                overshoot = System.Math.Max(overshoot, Easing.Evaluate(Tokens.EaseEmphasis, i / 100.0));
            }

            Assert.That(overshoot, Is.GreaterThan(1.0), "ease.emphasis rebota (y1 = 1,56)");
        }

        // x(s) = t por bisección pura (200 pasos) y y(s).
        private static double Reference(EasingToken curve, double t)
        {
            double low = 0, high = 1, s = 0.5;
            for (var i = 0; i < 200; i++)
            {
                s = (low + high) / 2;
                if (Bezier(curve.X1, curve.X2, s) < t)
                {
                    low = s;
                }
                else
                {
                    high = s;
                }
            }

            return Bezier(curve.Y1, curve.Y2, s);
        }

        private static double Bezier(double p1, double p2, double s)
        {
            var u = 1 - s;
            return 3 * u * u * s * p1 + 3 * u * s * s * p2 + s * s * s;
        }
    }
}
