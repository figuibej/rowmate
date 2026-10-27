using System;
using NUnit.Framework;
using RowMate.Core.Design;

namespace RowMate.Tests.EditMode.Design
{
    // FramePacingPolicy = 60 fps mientras algo se anima y hasta 1 s después; si no, 30 fps (ARCHITECTURE §13.1).
    [TestFixture]
    public sealed class FramePacingPolicyTests
    {
        [Test]
        public void ThirtyAtRestAndSixtyWithADemand()
        {
            var policy = new FramePacingPolicy();
            Assert.That(policy.RenderFrameInterval(0), Is.EqualTo(FramePacingPolicy.ThirtyFps), "al arrancar, sin demandas");
            policy.Acquire();
            Assert.That(policy.RenderFrameInterval(0.1), Is.EqualTo(FramePacingPolicy.SixtyFps));
            Assert.That(policy.RenderFrameInterval(5.0), Is.EqualTo(FramePacingPolicy.SixtyFps), "mientras dure");
        }

        [Test]
        public void StaysAtSixtyOneSecondAfterTheLastDemand()
        {
            var policy = new FramePacingPolicy();
            policy.Acquire();
            policy.Acquire();
            Assert.That(policy.RenderFrameInterval(1.0), Is.EqualTo(1));
            policy.Release();
            Assert.That(policy.RenderFrameInterval(1.1), Is.EqualTo(1), "queda una demanda");
            policy.Release();
            Assert.That(policy.Demands, Is.EqualTo(0));
            Assert.That(policy.RenderFrameInterval(1.2), Is.EqualTo(1), "recién liberada: sigue a 60");
            Assert.That(policy.RenderFrameInterval(2.05), Is.EqualTo(1), "menos de 1 s después del último cuadro con demandas (1,1)");
            Assert.That(policy.RenderFrameInterval(2.2), Is.EqualTo(2), "1 s sin demandas: 30 fps");
        }

        [Test]
        public void ABriefDemandBetweenFramesCounts()
        {
            var policy = new FramePacingPolicy();
            policy.RenderFrameInterval(0);
            policy.Acquire();
            policy.Release();
            Assert.That(policy.RenderFrameInterval(0.02), Is.EqualTo(1));
            Assert.That(policy.RenderFrameInterval(0.5), Is.EqualTo(1));
            Assert.That(policy.RenderFrameInterval(1.03), Is.EqualTo(2));
        }

        [Test]
        public void ReleaseWithoutAcquireThrows()
        {
            var policy = new FramePacingPolicy();
            Assert.Throws<InvalidOperationException>(() => policy.Release());
            Assert.Throws<ArgumentOutOfRangeException>(() => new FramePacingPolicy(-1));
            var immediate = new FramePacingPolicy(0);
            immediate.Acquire();
            immediate.Release();
            Assert.That(immediate.RenderFrameInterval(0), Is.EqualTo(1));
            Assert.That(immediate.RenderFrameInterval(0.0001), Is.EqualTo(2));
        }
    }
}
