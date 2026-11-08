using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // Los números del arte del Bluetooth real (It. 10a; 00-arte-bluetooth §1-§5 y §9; BleMotionSpecs).
    [TestFixture]
    public sealed class BleMotionSpecsTests
    {
        // CAP-05 (§9): a los 1 200 ms, radios 67, 60 y 37 con u = 0,75, 0,42 y 0,08; alfas 0,11, 0,26 y 0,41.
        [Test, Category("A10-03"), Category("AC-BLE-05-04")]
        public void SonarAtCap05()
        {
            var radii = new[] { 67.4, 60.1, 37.3 };
            var alphas = new[] { 0.11, 0.26, 0.41 };
            for (var i = 0; i < BleMotionSpecs.SonarRings; i++)
            {
                Assert.That(BleMotionSpecs.SonarRingPhase(1200, i, out var u), Is.True, "anillo " + i);
                Assert.That(BleMotionSpecs.SonarRingRadius(u), Is.EqualTo(radii[i]).Within(0.15), "radio " + i);
                Assert.That(BleMotionSpecs.SonarRingAlphaAt(u), Is.EqualTo(alphas[i]).Within(0.01), "alfa " + i);
            }
        }

        [Test, Category("A10-03")]
        public void RingsStartStaggeredWithoutHalfwayRingsAtFrameZero()
        {
            Assert.That(BleMotionSpecs.SonarRingPhase(0, 0, out var u0), Is.True);
            Assert.That(u0, Is.EqualTo(0));
            Assert.That(BleMotionSpecs.SonarRingPhase(0, 1, out _), Is.False);
            Assert.That(BleMotionSpecs.SonarRingPhase(532, 1, out _), Is.False);
            Assert.That(BleMotionSpecs.SonarRingPhase(533, 1, out var u1), Is.True);
            Assert.That(u1, Is.EqualTo(0));
            Assert.That(BleMotionSpecs.SonarRingPhase(1065, 2, out _), Is.False);
            Assert.That(BleMotionSpecs.SonarRingPhase(1600, 0, out var again), Is.True);
            Assert.That(again, Is.EqualTo(0), "período de 1 600");
            Assert.That(BleMotionSpecs.SonarRingPhase(100, 3, out _), Is.False, "solo 3 anillos periódicos");
        }

        [Test, Category("A10-03")]
        public void RingRadiusAndAlphaRanges()
        {
            Assert.That(BleMotionSpecs.SonarRingRadius(0), Is.EqualTo(28));
            Assert.That(BleMotionSpecs.SonarRingRadius(1), Is.EqualTo(68));
            Assert.That(BleMotionSpecs.SonarRingRadius(-1), Is.EqualTo(28));
            Assert.That(BleMotionSpecs.SonarRingAlphaAt(0), Is.EqualTo(0.45));
            Assert.That(BleMotionSpecs.SonarRingAlphaAt(1), Is.EqualTo(0));
            Assert.That(BleMotionSpecs.SonarBandDp, Is.EqualTo(8 + BleMotionSpecs.SonarBoxDp + 8));
            Assert.That(BleMotionSpecs.SonarRingToRadiusDp, Is.EqualTo(BleMotionSpecs.SonarBoxDp / 2), "el anillo llega al borde de la caja");
            Assert.That(BleMotionSpecs.SonarRingFromRadiusDp, Is.EqualTo(BleMotionSpecs.SonarDiscDp / 2), "sale del borde del disco");
        }

        [Test, Category("A10-03")]
        public void DiscBreathesInPhaseWithTheFirstRing()
        {
            Assert.That(BleMotionSpecs.SonarDiscScale(0), Is.EqualTo(1).Within(1e-9));
            Assert.That(BleMotionSpecs.SonarDiscScale(800), Is.EqualTo(1.03).Within(1e-9));
            Assert.That(BleMotionSpecs.SonarDiscScale(1600), Is.EqualTo(1).Within(1e-9));
            for (var t = 0.0; t < 3200; t += 17)
            {
                var s = BleMotionSpecs.SonarDiscScale(t);
                Assert.That(s, Is.InRange(1.0, 1.03 + 1e-9));
            }
        }

        [Test, Category("A10-07")]
        public void StateIconBreathesTwiceAsSlow()
        {
            Assert.That(BleMotionSpecs.StateBreathMs, Is.EqualTo(2 * BleMotionSpecs.SonarPeriodMs));
            Assert.That(BleMotionSpecs.StateBreathOpacity(0), Is.EqualTo(1).Within(1e-9), "CAP-01 a -03: en reposo (opacidad 1)");
            Assert.That(BleMotionSpecs.StateBreathOpacity(1600), Is.EqualTo(0.72).Within(1e-9));
            Assert.That(BleMotionSpecs.StateBreathOpacity(3200), Is.EqualTo(1).Within(1e-9));
        }

        [Test, Category("A10-07")]
        public void WakeTimelineFitsInSevenHundredTwenty()
        {
            Assert.That(BleMotionSpecs.WakeGlyphEndMs, Is.LessThanOrEqualTo(BleMotionSpecs.WakeColorEndMs));
            Assert.That(BleMotionSpecs.WakeOutStartMs, Is.EqualTo(BleMotionSpecs.WakeDiscEndMs));
            Assert.That(BleMotionSpecs.WakeOutEndMs - BleMotionSpecs.WakeOutStartMs, Is.EqualTo(120));
            Assert.That(BleMotionSpecs.WakeInEndMs - BleMotionSpecs.WakeOutEndMs, Is.EqualTo(240));
            Assert.That(BleMotionSpecs.ReducedWakeOutEndMs - BleMotionSpecs.ReducedWakeGlyphMs, Is.EqualTo(120));
            Assert.That(BleMotionSpecs.ReducedWakeInEndMs - BleMotionSpecs.ReducedWakeOutEndMs, Is.EqualTo(120));
        }

        [Test, Category("A10-05")]
        public void StepsAndReadyCheck()
        {
            Assert.That(BleMotionSpecs.StepOutMs, Is.EqualTo(80));
            Assert.That(BleMotionSpecs.StepInEndMs, Is.EqualTo(200));
            Assert.That(BleMotionSpecs.CheckTickEndMs, Is.EqualTo(240), "la retención de 240 ms de la vista");
            Assert.That(BleMotionSpecs.CheckTickEndMs + MotionSpecs.ViewInEndMs, Is.EqualTo(BleMotionSpecs.ReadyTotalMs), "D10-04: 600 ms");
            Assert.That(BleMotionSpecs.Segment(40, 0, 80), Is.EqualTo(0.5));
            Assert.That(BleMotionSpecs.Segment(-1, 0, 80), Is.EqualTo(0));
            Assert.That(BleMotionSpecs.Segment(300, 80, 200), Is.EqualTo(1));
        }

        [Test, Category("A10-04")]
        public void CardsStaggerEveryThirtyFiveUpToEight()
        {
            Assert.That(BleMotionSpecs.CardDelayMs(0), Is.EqualTo(0));
            Assert.That(BleMotionSpecs.CardDelayMs(1), Is.EqualTo(35));
            Assert.That(BleMotionSpecs.CardDelayMs(7), Is.EqualTo(245));
            Assert.That(BleMotionSpecs.CardDelayMs(20), Is.EqualTo(245), "8 como máximo");
            Assert.That(BleMotionSpecs.CardMinHeightDp + BleMotionSpecs.CardGapDp, Is.EqualTo(72));
        }

        [Test, Category("A10-11")]
        public void DebugDotBeatsOncePerPacket()
        {
            Assert.That(BleMotionSpecs.DebugDotOpacity(0), Is.EqualTo(1));
            Assert.That(BleMotionSpecs.DebugDotOpacity(150), Is.EqualTo(0.35).Within(1e-9));
            Assert.That(BleMotionSpecs.DebugDotOpacity(300), Is.EqualTo(1));
            Assert.That(BleMotionSpecs.DebugDotOpacity(5000), Is.EqualTo(1), "quieto hasta el próximo paquete");
        }
    }
}
