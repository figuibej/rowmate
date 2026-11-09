using System;
using NUnit.Framework;
using RowMate.Core.Ble;
using RowMate.Tests.Support;

namespace RowMate.Tests.EditMode.Ble
{
    // StaleWatchdog = lib/core/bluetooth/reconnect_loop.dart:75-91 con el reloj de prueba (LT-145), por plazo (DT11-02).
    [TestFixture]
    public sealed class StaleWatchdogTests
    {
        // legacy: test/reconnect_loop_test.dart:157 'dispara tras el timeout sin touch, y touch lo rearma'
        [TestCase(TestName = "dispara tras el timeout sin touch, y touch lo rearma", Category = "LT-145,AC-HRM-05-01")]
        public void LT145_FiresAt19OnceStopPrevents()
        {
            var scheduler = new FakeScheduler();
            var stale = 0;
            var w = new StaleWatchdog(scheduler, TimeSpan.FromSeconds(10), () => stale++);
            w.Touch();
            scheduler.Advance(TimeSpan.FromSeconds(9));
            w.Touch();
            scheduler.Advance(TimeSpan.FromSeconds(9));
            Assert.That(stale, Is.EqualTo(0));
            scheduler.Advance(TimeSpan.FromSeconds(1));
            Assert.That(stale, Is.EqualTo(1));
            scheduler.Advance(TimeSpan.FromSeconds(30));
            Assert.That(stale, Is.EqualTo(1), "dispara una sola vez por touch");
            w.Touch();
            w.Stop();
            scheduler.Advance(TimeSpan.FromSeconds(30));
            Assert.That(stale, Is.EqualTo(1));
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
        }

        // DT11-02 (AC-HRM-05-01): mil lecturas seguidas no agendan mil temporizadores: queda uno solo, que se reagenda al vencer.
        [Test, Category("AC-HRM-05-01")]
        public void TouchDoesNotAllocateTimers()
        {
            var scheduler = new FakeScheduler();
            var stale = 0;
            var w = new StaleWatchdog(scheduler, TimeSpan.FromSeconds(10), () => stale++);
            for (var i = 0; i < 1000; i++)
            {
                w.Touch();
                Assert.That(scheduler.PendingTimers, Is.EqualTo(1));
                scheduler.Advance(TimeSpan.FromMilliseconds(100));
            }

            Assert.That(stale, Is.EqualTo(0), "cada 100 ms nunca vence");
            Assert.That(w.IsArmed, Is.True);
            scheduler.Advance(TimeSpan.FromMilliseconds(9899));
            Assert.That(stale, Is.EqualTo(0));
            scheduler.Advance(TimeSpan.FromMilliseconds(1));
            Assert.That(stale, Is.EqualTo(1), "10 s exactos después de la última lectura");
            Assert.That(w.IsArmed, Is.False);
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
            w.Touch();
            w.Dispose();
            Assert.That(scheduler.PendingTimers, Is.EqualTo(0));
        }
    }
}
